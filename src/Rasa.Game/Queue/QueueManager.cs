using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Queue
{
    using Config;
    using Game;
    using Networking;

    public delegate void RedirectDelegate(QueueClient client);

    public class QueueManager
    {
        private readonly Queue<QueueClient> _queuedClients = new Queue<QueueClient>();

        public Server Server { get; }
        public LengthedSocket Socket { get; }
        public int QueuedClients => _queuedClients.Count;
        public RedirectDelegate OnRedirect { get; set; }
        public QueueConfig Config => Server.Config.QueueConfig;

        public QueueManager(Server server)
        {
            Server = server;

            Socket = new LengthedSocket(SizeType.Dword, false);
            Socket.OnError += OnError;
            Socket.OnAccept += OnAccept;
            Socket.Bind(new IPEndPoint(IPAddress.Any, Config.Port));
            Socket.Listen(Config.Backlog);

            Socket.AcceptAsync();
        }

        private static void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        private void OnAccept(LengthedSocket socket)
        {
            lock (_queuedClients)
                _queuedClients.Enqueue(new QueueClient(this, socket));
        }

        public void AdvanceQueue(uint freeSlots)
        {
            if (QueuedClients == 0)
                return;

            lock (_queuedClients)
            {
                for (var i = 0; i < freeSlots; ++i)
                {
                    var client = _queuedClients.Dequeue();

                    client.Redirect(Server.PublicAddress, Server.Config.GameConfig.Port, 0, 0);
                }
            }
        }

        public void Update()
        {
            if (QueuedClients == 0)
                return;

            lock (_queuedClients)
            {
                var position = 1;

                foreach (var client in _queuedClients)
                    client.SendPositionUpdate(position++, 100); // TODO: estimated time calculation
            }
        }

        //client.SendPositionUpdate(_queuedClients.Count, 200); // TODO: estimated time calculation
        // TODO: move after authenticated
    }
}
