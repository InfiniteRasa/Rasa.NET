using System.Collections.Generic;
using System.Net;

namespace Rasa.Queue;

using Rasa.Config;
using Rasa.Data;
using Rasa.Game;
using Rasa.Networking;

public delegate void RedirectDelegate(QueueClient client);

public class QueueManager
{
    private readonly Queue<QueueClient> _queuedClients = new Queue<QueueClient>();

    public List<QueueClient> Clients { get; } = new List<QueueClient>();

    public Server Server { get; }
    public AsyncLengthedSocket Socket { get; } = new(AsyncLengthedSocket.HeaderSizeType.Dword, false);
    public int QueuedClients => _queuedClients.Count;
    public RedirectDelegate OnRedirect { get; set; }
    public QueueConfig Config => Server.Config.QueueConfig;

    public QueueManager(Server server)
    {
        Server = server;

        Socket.OnAccept += OnAccept;
        Socket.StartListening(new IPEndPoint(IPAddress.Any, Config.Port), Config.Backlog);
    }

    private void OnAccept(AsyncLengthedSocket socket)
    {
        lock (Clients)
            Clients.Add(new QueueClient(this, socket));
    }

    public void Disconnect(QueueClient client)
    {
        lock (Clients)
            Clients.Remove(client);
    }

    public void Enqueue(QueueClient client)
    {
        lock (_queuedClients)
        {
            if (!Server.IsFull)
            {
                // TODO: need a position update before redirect?
                client.Redirect(Server.PublicAddress, Server.Config.GameConfig.Port);
                return;
            }

            _queuedClients.Enqueue(client);

            var pos = 0;

            foreach (var c in _queuedClients)
            {
                if (c.State == QueueState.Disconnected)
                    continue;

                if (c == client)
                    c.SendPositionUpdate(pos, 10000 * pos); // TODO: proper estimated time calculation
                else
                    ++pos;
            }
        }
    }

    private void AdvanceQueue(int freeSlots)
    {
        if (QueuedClients == 0)
            return;

        lock (_queuedClients)
        {
            for (var i = 0; i < freeSlots && QueuedClients > 0;)
            {
                var client = _queuedClients.Dequeue();
                if (client.State == QueueState.Disconnected)
                    continue;

                client.Redirect(Server.PublicAddress, Server.Config.GameConfig.Port);
                ++i;
            }
        }
    }

    private void ClearDisconnected()
    {
        if (QueuedClients == 0)
            return;

        lock (_queuedClients)
        {
            do
            {
                var c = _queuedClients.Peek();
                if (c.State != QueueState.Disconnected)
                    break;

                _queuedClients.Dequeue();
            }
            while (QueuedClients > 0);
        }
    }

    public void Update(int freeSlots)
    {
        if (QueuedClients == 0)
            return;

        lock (_queuedClients)
        {
            ClearDisconnected();

            AdvanceQueue(freeSlots);

            var position = 0;

            foreach (var client in _queuedClients)
                if (client.State != QueueState.Disconnected)
                    client.SendPositionUpdate(position, 10000 * position++); // TODO: proper estimated time calculation
        }
    }
}
