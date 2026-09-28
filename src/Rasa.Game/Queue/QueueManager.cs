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
            var client = new QueueClient(this, socket, false);
            lock (Clients)
                Clients.Add(client);

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

        public void Disconnect(QueueClient client)
        {
            lock (Clients)
                Clients.Remove(client);
        }

        public void Enqueue(QueueClient client)
        {
            _beforeAdmission?.Invoke();
            var position = 0;
            bool queued;

            lock (_queuedClients)
            {
                queued = _isFull();

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
