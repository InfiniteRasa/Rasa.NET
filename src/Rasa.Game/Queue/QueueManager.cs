using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;

namespace Rasa.Queue
{
    using Config;
    using Data;
    using Game;
    using Networking;

    public delegate void RedirectDelegate(QueueClient client);

    public class QueueManager
    {
        /// <summary>
        /// Serializes queue membership changes with the client's disconnect transition. Code under
        /// this gate only reads capacity and changes client state or the in-memory queue; socket
        /// sends, callbacks, and client-list mutation happen after it is released. QueueClientState
        /// never acquires this gate, so the lock order is always queue gate then state gate, never
        /// the reverse.
        /// </summary>
        private readonly Queue<QueueClient> _queuedClients = new Queue<QueueClient>();
        private readonly Func<bool> _isFull;
        private readonly IPAddress _publicAddress;
        private readonly int _gamePort;
        private readonly Action _beforeAdmission;

        public List<QueueClient> Clients { get; } = new List<QueueClient>();

        public Server Server { get; }
        public LengthedSocket Socket { get; }
        public int QueuedClients
        {
            get
            {
                lock (_queuedClients)
                    return _queuedClients.Count;
            }
        }

        /// <summary>Queue connections handed off to the world port that are still open.</summary>
        public int RedirectingClients
        {
            get
            {
                lock (Clients)
                    return Clients.Count(c => c.State == QueueState.Redirecting);
            }
        }

        /// <summary>
        /// How long a handed-off client keeps its slot while nobody logs in at the world port.
        /// The client connects there straight away, before it loads anything, so this is
        /// generous; a redirect session on the world side lasts the same minute.
        /// </summary>
        private static readonly TimeSpan RedirectTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long a connection has to get through the key exchange and the queue login. The real
        /// client sends both as soon as it connects. A connection that has done neither holds a
        /// receive buffer from the pool the world port draws on too, and used to hold it for as
        /// long as it stayed open.
        /// </summary>
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Open queue connections allowed from one address. Generous for players sharing an
        /// address; small against the thousands it takes to empty the shared buffer pool.
        /// </summary>
        private const int MaxConnectionsPerAddress = 32;

        private long _nextRefusalLogTick;
        private int _refusalsSinceLog;

        /// <summary>
        /// The account has logged in at the world port: its queue connection, if still open,
        /// stops counting as a slot.
        /// </summary>
        public void Arrived(uint userId)
        {
            lock (Clients)
                foreach (var client in Clients)
                    if (client.UserId == userId && client.State == QueueState.Redirecting)
                        client.MarkArrived();
        }

        /// <summary>
        /// Closes handed-off connections whose account never turned up at the world port, so a
        /// client that took the handoff and went away does not hold a slot for as long as it
        /// keeps the socket open.
        /// </summary>
        private void ExpireRedirects()
        {
            List<QueueClient> expired;
            var cutoff = DateTime.Now - RedirectTimeout;

            lock (Clients)
                expired = Clients.Where(c => c.State == QueueState.Redirecting && c.RedirectTime < cutoff).ToList();

            // QueueClient.Close removes the client from Clients, so close outside the lock.
            foreach (var client in expired)
            {
                Logger.WriteLog(LogType.Network, $"Queue client for account {client.UserId} was handed off {RedirectTimeout.TotalSeconds:F0} s ago and never logged in; closing it.");
                client.Close();
            }
        }

        /// <summary>Closes connections that have not finished the handshake within HandshakeTimeout.</summary>
        private void ExpireHandshakes()
        {
            List<QueueClient> expired;
            var cutoff = DateTime.Now - HandshakeTimeout;

            lock (Clients)
                expired = Clients.Where(c => (c.State == QueueState.Authenticating || c.State == QueueState.Authenticated)
                                             && c.ConnectedTime < cutoff).ToList();

            if (expired.Count > 0)
                Logger.WriteLog(LogType.Network, $"Closing {expired.Count} queue connection(s) that did not log in within {HandshakeTimeout.TotalSeconds:F0} s.");

            // QueueClient.Close removes the client from Clients, so close outside the lock.
            foreach (var client in expired)
                client.Close();
        }

        /// <summary>
        /// Closes the other connections for the same account that are still waiting - in the queue
        /// or handed off - so an account holds one place, and one slot, at a time.
        /// </summary>
        public void CloseEarlierConnections(QueueClient current)
        {
            List<QueueClient> earlier;

            lock (Clients)
                earlier = Clients.Where(c => c != current && c.UserId == current.UserId
                                             && (c.State == QueueState.InQueue || c.State == QueueState.Redirecting)).ToList();

            foreach (var client in earlier)
                client.Close();
        }

        /// <summary>Closes every queue connection belonging to an account.</summary>
        public void Disconnect(uint userId)
        {
            List<QueueClient> matches;

            lock (Clients)
                matches = Clients.Where(c => c.UserId == userId && c.State != QueueState.Disconnected).ToList();

            // QueueClient.Close removes the client from Clients, so close outside the lock.
            foreach (var client in matches)
                client.Close();
        }

        /// <summary>Accounts with a live, authenticated queue connection.</summary>
        public HashSet<uint> ConnectedUserIds()
        {
            lock (Clients)
                return new HashSet<uint>(Clients
                    .Where(c => c.State == QueueState.Authenticated || c.State == QueueState.InQueue || c.State == QueueState.Redirecting)
                    .Select(c => c.UserId));
        }
        public RedirectDelegate OnRedirect { get; set; }
        public QueueConfig Config { get; }

        public QueueManager(Server server)
        {
            Server = server;
            Config = server.Config.QueueConfig;
            _isFull = () => server.IsFull;
            _publicAddress = server.PublicAddress;
            _gamePort = server.Config.GameConfig.Port;

            Socket = new LengthedSocket(SizeType.Dword, false);
            Socket.OnError += OnError;
            Socket.OnAccept += OnAccept;
            Socket.Bind(new IPEndPoint(IPAddress.Any, Config.Port));
            Socket.Listen(Config.Backlog);

            Socket.AcceptAsync();
        }

        internal QueueManager(
            QueueConfig config,
            Func<bool> isFull,
            IPAddress publicAddress,
            int gamePort,
            Action beforeAdmission)
        {
            Config = config;
            _isFull = isFull;
            _publicAddress = publicAddress;
            _gamePort = gamePort;
            _beforeAdmission = beforeAdmission;
        }

        private static void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        private void OnAccept(LengthedSocket socket)
        {
            Socket.AcceptAsync();
            AcceptClient(socket);
        }

        internal void AcceptClient(LengthedSocket socket)
        {
            var address = socket.RemoteAddress;
            var client = new QueueClient(this, socket, false);

            // Counted and added under the lock, so two accepts from one address cannot both
            // pass the check; started after it, because starting runs socket I/O whose
            // completion can run the whole handshake on this thread.
            lock (Clients)
            {
                if (address != null && Clients.Count(c => address.Equals(c.Socket.RemoteAddress)) >= MaxConnectionsPerAddress)
                    client = null;
                else
                    Clients.Add(client);
            }

            if (client == null)
            {
                socket.Close();
                ReportRefusal(address);
                return;
            }

            try
            {
                client.Start();
            }
            catch
            {
                client.Close();
                throw;
            }
        }

        /// <summary>
        /// One line for the first refusal and then at most one every five seconds with the count,
        /// since a flood of connections is exactly when a line apiece would hurt.
        /// </summary>
        private void ReportRefusal(IPAddress address)
        {
            var now = Environment.TickCount64;
            var count = System.Threading.Interlocked.Increment(ref _refusalsSinceLog);

            if (now < System.Threading.Interlocked.Read(ref _nextRefusalLogTick))
                return;

            System.Threading.Interlocked.Exchange(ref _nextRefusalLogTick, now + 5000);
            System.Threading.Interlocked.Exchange(ref _refusalsSinceLog, 0);

            Logger.WriteLog(LogType.Security, $"Refused a queue connection from {address}: {MaxConnectionsPerAddress} already open from it ({count} refused since the last of these).");
        }

        public void Disconnect(QueueClient client)
        {
            lock (Clients)
                Clients.Remove(client);
        }

        public void Enqueue(QueueClient client)
        {
            _beforeAdmission?.Invoke();
            var position = 0;

            // Read before the queue lock, not under it: IsFull counts Server.Clients, which the
            // world loop holds for its whole tick, and waiting for it while holding a queue lock
            // nests the queue's locks inside the world's in one place and outside them in
            // another. The count was only ever a snapshot, free to change the moment it was taken.
            var queued = _isFull();

            lock (_queuedClients)
            {

                if (!client.TryAdmit(queued, DateTime.Now))
                    return;

                if (queued)
                {
                    _queuedClients.Enqueue(client);

                    foreach (var queuedClient in _queuedClients)
                    {
                        if (queuedClient.State == QueueState.Disconnected)
                            continue;

                        if (queuedClient == client)
                            break;

                        ++position;
                    }
                }
            }

            if (!queued)
                client.SendRedirect(_publicAddress, _gamePort);
            else
                client.SendPositionUpdate(position, 10000 * position); // TODO: proper estimated time calculation
        }

        internal bool TryDisconnect(QueueClient client)
        {
            lock (_queuedClients)
                return client.TryDisconnect();
        }

        private List<QueueClient> AdvanceQueue(int freeSlots, DateTime now)
        {
            var redirects = new List<QueueClient>();

            for (var i = 0; i < freeSlots && _queuedClients.Count > 0;)
            {
                var client = _queuedClients.Dequeue();
                if (!client.TryPrepareRedirect(now))
                    continue;

                redirects.Add(client);
                ++i;
            }

            return redirects;
        }

        private void RemoveDisconnectedClients()
        {
            var count = _queuedClients.Count;

            for (var i = 0; i < count; ++i)
            {
                var client = _queuedClients.Dequeue();
                if (client.State != QueueState.Disconnected)
                    _queuedClients.Enqueue(client);
            }
        }

        public void Update(int freeSlots)
        {
            ExpireHandshakes();
            ExpireRedirects();
            List<QueueClient> redirects;
            List<(QueueClient Client, int Position)> positionUpdates;

            lock (_queuedClients)
            {
                if (_queuedClients.Count == 0)
                    return;

                RemoveDisconnectedClients();

                redirects = AdvanceQueue(freeSlots, DateTime.Now);

                var position = 0;
                positionUpdates = _queuedClients
                    .Where(client => client.State != QueueState.Disconnected)
                    .Select(client => (client, position++))
                    .ToList();
            }

            foreach (var client in redirects)
                client.SendRedirect(_publicAddress, _gamePort);

            foreach (var update in positionUpdates)
                update.Client.SendPositionUpdate(update.Position, 10000 * update.Position); // TODO: proper estimated time calculation
        }
    }
}
