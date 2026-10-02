using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Api
{
    /// <summary>
    /// A small TCP listener of its own, apart from the game's sockets: one short exchange a
    /// connection, on the thread pool. What it answers is a subclass's.
    ///
    /// It is reachable by anybody who can reach the port, so every connection is held to a
    /// time limit, the number open at once is capped, and nothing a connection does gets
    /// further than that connection: a listener that fails takes neither the world nor the
    /// other listeners with it.
    /// </summary>
    public abstract class TcpService
    {
        /// <summary>How long a connection has to say what it wants and be answered, in milliseconds.</summary>
        public const int ExchangeTimeoutMs = 5000;

        /// <summary>The most connections served at once; one more is closed unanswered.</summary>
        public const int MaxConnections = 64;

        /// <summary>The least time between two log lines about refused connections, in milliseconds.</summary>
        private const int RefusalLogMs = 30000;

        private readonly object _sync = new object();
        private TcpListener _listener;
        private CancellationTokenSource _stop;
        private string _bound;
        private int _open;
        private long _lastRefusalLog = long.MinValue;
        private int _refusalsUnlogged;

        /// <summary>What the log calls this listener.</summary>
        protected abstract string Label { get; }

        /// <summary>Where it listens; null while it does not. A port of 0 asked for is the port the system gave.</summary>
        public IPEndPoint LocalEndPoint { get; private set; }

        public bool Running => LocalEndPoint != null;

        /// <summary>
        /// Listens on that address and port: nothing when it already does, and the old listener
        /// closed first when it listened somewhere else. False when the port could not be
        /// opened, which leaves the listener off.
        /// </summary>
        protected bool Listen(string bindAddress, int port)
        {
            lock (_sync)
            {
                var wanted = $"{bindAddress}:{port}";

                if (_listener != null && _bound == wanted)
                    return true;

                StopLocked();

                if (port < 0 || port > 65535)
                {
                    Logger.WriteLog(LogType.Error, $"{Label}: {port} is not a port. It is off.");
                    return false;
                }

                var address = IPAddress.Any;

                if (!string.IsNullOrWhiteSpace(bindAddress) && !IPAddress.TryParse(bindAddress.Trim(), out address))
                {
                    Logger.WriteLog(LogType.Error, $"{Label}: BindAddress \"{bindAddress}\" is not an address. It is off.");
                    return false;
                }

                try
                {
                    var listener = new TcpListener(address, port);

                    listener.Start(MaxConnections);

                    _listener = listener;
                    _stop = new CancellationTokenSource();
                    _bound = wanted;
                    LocalEndPoint = (IPEndPoint)listener.LocalEndpoint;

                    var stop = _stop.Token;

                    Task.Run(() => AcceptLoop(listener, stop));
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"{Label}: could not listen on {wanted}. It is off. {e.Message}");
                    StopLocked();
                    return false;
                }

                Logger.WriteLog(LogType.Network, $"*** {Label} listening on {LocalEndPoint}");

                return true;
            }
        }

        /// <summary>Closes the listener; connections being served are given up.</summary>
        public void Stop()
        {
            lock (_sync)
            {
                if (_listener != null)
                    Logger.WriteLog(LogType.Network, $"*** {Label} closed.");

                StopLocked();
            }
        }

        private void StopLocked()
        {
            try
            {
                _stop?.Cancel();
                _listener?.Stop();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"{Label}: closing the listener failed: {e.Message}");
            }

            _stop = null;
            _listener = null;
            _bound = null;
            LocalEndPoint = null;
        }

        private async Task AcceptLoop(TcpListener listener, CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await listener.AcceptTcpClientAsync(stop).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    if (stop.IsCancellationRequested)
                        return;

                    // One connection that failed on its way in is not the listener's end.
                    Refused($"an accept failed ({e.Message})");

                    try
                    {
                        await Task.Delay(100, stop).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    continue;
                }

                if (Interlocked.Increment(ref _open) > MaxConnections)
                {
                    Interlocked.Decrement(ref _open);
                    Refused($"more than {MaxConnections} connections at once");
                    client.Dispose();
                    continue;
                }

                _ = Task.Run(() => Serve(client, stop));
            }
        }

        private async Task Serve(TcpClient client, CancellationToken stop)
        {
            try
            {
                using (client)
                using (var limit = CancellationTokenSource.CreateLinkedTokenSource(stop))
                {
                    limit.CancelAfter(ExchangeTimeoutMs);
                    client.NoDelay = true;

                    var remote = IpAllowList.Normalize((client.Client.RemoteEndPoint as IPEndPoint)?.Address);

                    await Exchange(client.GetStream(), remote, limit.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Too slow, or the listener closed.
            }
            catch (IOException)
            {
                // The other end went away.
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"{Label}: a connection failed: {e}");
            }
            finally
            {
                Interlocked.Decrement(ref _open);
            }
        }

        /// <summary>One connection's exchange. The stream is closed when this returns or throws.</summary>
        protected abstract Task Exchange(Stream stream, IPAddress remote, CancellationToken limit);

        /// <summary>
        /// Notes a refusal in the log, at most one line every half minute however many there
        /// are: anybody can make this listener refuse them as often as they like.
        /// </summary>
        protected void Refused(string what)
        {
            var now = Environment.TickCount64;
            var last = Interlocked.Read(ref _lastRefusalLog);

            if (last != long.MinValue && now - last < RefusalLogMs)
            {
                Interlocked.Increment(ref _refusalsUnlogged);
                return;
            }

            if (Interlocked.CompareExchange(ref _lastRefusalLog, now, last) != last)
            {
                Interlocked.Increment(ref _refusalsUnlogged);
                return;
            }

            var unlogged = Interlocked.Exchange(ref _refusalsUnlogged, 0);

            Logger.WriteLog(LogType.Security, $"{Label}: refused {what}{(unlogged > 0 ? $" (and {unlogged} more refusals since the last line)" : "")}.");
        }
    }
}
