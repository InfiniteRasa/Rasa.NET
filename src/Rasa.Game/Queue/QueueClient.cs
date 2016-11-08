using System;
using System.Net;
using System.Net.Sockets;

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
        public QueueState State { get; private set; }
        public uint UserId { get; set; }
        public uint OneTimeKey { get; set; }
        public DateTime EnqueueTime { get; private set; }
        public DateTime DequeueTime { get; set; }

        public QueueClient(QueueManager manager, LengthedSocket socket)
        {
            Manager = manager;
            Socket = socket;
            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;

            Socket.ReceiveAsync();

            Socket.Send(new ServerKeyPacket
            {
                PublicKey = Manager.Config.PublicKey,
                Prime = Manager.Config.Prime,
                Generator = Manager.Config.Generator
            }, null);

            State = QueueState.Authenticating;
        }

        private void OnReceive(BufferData data)
        {
            switch (State)
            {
                case QueueState.Authenticating:
                    var keyPacket = new ClientKeyPacket();

                    keyPacket.Read(data.GetReader());

                    if (keyPacket.PublicKey != Manager.Config.PublicKey)
                    {
                        Close();
                        return;
                    }

                    Socket.Send(new ClientKeyOkPacket(), null);

                    State = QueueState.Authenticated;

                    break;

                case QueueState.Authenticated:
                    var loginPacket = new QueueLoginPacket();

                    loginPacket.Read(data.GetReader());

                    UserId = loginPacket.UserId;
                    OneTimeKey = loginPacket.OneTimeKey;
                    State = QueueState.InQueue;

                    Manager.Enqueue(this);
                    EnqueueTime = DateTime.Now;
                    break;

                default:
                    throw new Exception("Received packet in a invalid queue state!");
            }
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close();
        }

        public void Close()
        {
            Socket.Close();

            State = QueueState.Disconnected;

            Manager.Disconnect(this);
        }

        public void Redirect(IPAddress ip, int port)
        {
            State = QueueState.Redirecting;

            Socket.Send(new HandoffToGamePacket
            {
                OneTimeKey = OneTimeKey,
                ServerIp = ip,
                ServerPort = port,
                UserId = UserId
            }, null);
        }

        public void SendPositionUpdate(int position, int estimatedTime)
        {
            Socket.Send(new QueuePositionPacket
            {
                Position = position,
                EstimatedTime = estimatedTime
            }, null);
        }
    }
}
