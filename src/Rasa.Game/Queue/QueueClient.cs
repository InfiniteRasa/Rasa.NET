using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Rasa.Queue
{
    using Data;
    using Memory;
    using Networking;
    using Packets.Queue.Client;
    using Packets.Queue.Server;

    public class QueueClient
    {
        public QueueManager Manager { get; }
        public LengthedSocket Socket { get; }

        /// <summary>
        /// Read on the main loop by every QueueManager pass and written on the socket threads as
        /// the handshake advances, so the transitions go through <see cref="QueueClientState"/>. They
        /// are single writes during key exchange. Admission, redirect, arrival, and disconnect use
        /// expected-state transitions because each competes with teardown or another manager pass.
        /// </summary>
        private readonly QueueClientState _state = new QueueClientState();
        private int _started;
        public QueueState State => _state.Value;
        public uint UserId { get; set; }
        public uint OneTimeKey { get; set; }
        public DateTime EnqueueTime { get; private set; }
        public DateTime DequeueTime { get; set; }

        /// <summary>When the handoff was sent; the slot it holds is given up if nobody arrives.</summary>
        public DateTime RedirectTime { get; private set; }

        public QueueClient(QueueManager manager, LengthedSocket socket)
            : this(manager, socket, true)
        {
        }

        internal QueueClient(QueueManager manager, LengthedSocket socket, bool start)
        {
            Manager = manager;
            Socket = socket;
            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;

            if (start)
                Start();
        }

        internal void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            Socket.ReceiveAsync();

            Socket.Send(new ServerKeyPacket
            {
                PublicKey = Manager.Config.PublicKey,
                Prime = Manager.Config.Prime,
                Generator = Manager.Config.Generator
            });
        }

        private void OnReceive(BufferData data)
        {
            // Runs on a socket completion thread: an exception escaping here is unhandled and
            // terminates the process, so a malformed or unexpected queue packet must only ever
            // cost this one connection.
            try
            {
                using var reader = data.GetReader();
                HandleReceive(reader);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error handling queue packet from {Socket.RemoteAddress}, disconnecting: {e}");
                Close();
            }
        }

        internal void HandleReceive(BinaryReader reader)
        {
            switch (State)
            {
                case QueueState.Authenticating:
                    var keyPacket = new ClientKeyPacket();

                    keyPacket.Read(reader);

                    if (keyPacket.PublicKey != Manager.Config.PublicKey)
                    {
                        Close();
                        return;
                    }

                    Socket.Send(new ClientKeyOkPacket());

                    SetState(QueueState.Authenticated);

                    break;

                case QueueState.Authenticated:
                    if (reader.ReadByte() != 7)
                        throw new InvalidDataException("Invalid queue opcode.");

                    var loginPacket = new QueueLoginPacket();

                    loginPacket.Read(reader);

                    UserId = loginPacket.UserId;
                    OneTimeKey = loginPacket.OneTimeKey;
                    Manager.Enqueue(this);
                    break;

                default:
                    throw new InvalidDataException("Received packet in an invalid queue state.");
            }
        }

        /// <summary>
        /// Advances the handshake, unless this connection has already gone. A socket thread that
        /// is mid-handshake when the main loop or the communicator closes the connection would
        /// otherwise put it back into a live state and leave it in the queue holding a slot, with
        /// a socket nobody can write to.
        /// </summary>
        private void SetState(QueueState state)
        {
            _state.TrySet(state);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close();
        }

        /// <summary>The socket gave up on this connection; the reason is already logged.</summary>
        private void OnDrop(string reason)
        {
            Close();
        }

        /// <summary>
        /// Reached from three threads: this connection's own socket threads (OnError, OnDrop and
        /// the receive handler's catch), the main loop (QueueManager expiring a redirect that
        /// nobody arrived for), and the auth communicator's thread (an account locked while it
        /// was waiting). Nothing used to stop two of them running the whole teardown, and the
        /// socket was closed before the state said so, leaving a window in which the main loop's
        /// next pass would write a position update to a socket that had already gone.
        /// </summary>
        public void Close()
        {
            if (!Manager.TryDisconnect(this))
                return;

            Socket.Close();

            Manager.Disconnect(this);
        }

        internal bool TryAdmit(bool queued, DateTime now)
        {
            if (!_state.TrySet(QueueState.Authenticated, queued ? QueueState.InQueue : QueueState.Redirecting))
                return false;

            EnqueueTime = now;
            if (!queued)
                RedirectTime = now;

            return true;
        }

        internal bool TryDisconnect()
        {
            return _state.TryDisconnect();
        }

        /// <summary>
        /// The account this connection was handed off for has logged in at the world port. From
        /// here it is a world client and counted as one; whether the client closes this socket
        /// now or keeps it open until it exits, it no longer holds a slot of its own. Before this
        /// a client that kept the queue socket open counted twice, and the server read as full
        /// at half its cap.
        /// </summary>
        internal void MarkArrived()
        {
            _state.TrySet(QueueState.Redirecting, QueueState.Arrived);
        }

        public void Redirect(IPAddress ip, int port)
        {
            if (!TryPrepareRedirect(DateTime.Now))
                return;

            SendRedirect(ip, port);
        }

        internal bool TryPrepareRedirect(DateTime now)
        {
            if (!_state.TrySet(QueueState.InQueue, QueueState.Redirecting))
                return false;

            RedirectTime = now;
            return true;
        }

        internal void SendRedirect(IPAddress ip, int port)
        {
            Socket.Send(new HandoffToGamePacket
            {
                OneTimeKey = OneTimeKey,
                ServerIp = ip,
                ServerPort = port,
                UserId = UserId
            });
        }

        public void SendPositionUpdate(int position, int estimatedTime)
        {
            Socket.Send(new QueuePositionPacket
            {
                Position = position,
                EstimatedTime = estimatedTime
            });
        }
    }

    internal sealed class QueueClientState
    {
        private readonly object _lock = new object();
        private QueueState _value;

        internal QueueState Value
        {
            get
            {
                lock (_lock)
                    return _value;
            }
        }

        internal bool TrySet(QueueState value)
        {
            lock (_lock)
            {
                if (_value == QueueState.Disconnected)
                    return false;

                _value = value;
                return true;
            }
        }

        internal bool TrySet(QueueState expected, QueueState value)
        {
            lock (_lock)
            {
                if (_value != expected)
                    return false;

                _value = value;
                return true;
            }
        }

        internal bool TryDisconnect()
        {
            lock (_lock)
            {
                if (_value == QueueState.Disconnected)
                    return false;

                _value = QueueState.Disconnected;
                return true;
            }
        }
    }
}
