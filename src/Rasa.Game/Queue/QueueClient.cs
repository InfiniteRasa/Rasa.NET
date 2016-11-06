using System;
using System.Net;

namespace Rasa.Queue
{
    using Data;
    using Memory;
    using Networking;
    using Packets.Queue.Server;
    using Packets.Shared.Server;

    public class QueueClient
    {
        public QueueManager Manager { get; }
        public LengthedSocket Socket { get; }
        public QueueState State { get; private set; }

        public QueueClient(QueueManager manager, LengthedSocket socket)
        {
            Manager = manager;
            Socket = socket;
            Socket.OnReceive += OnReceive;

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
            // TODO
        }

        public void Redirect(IPAddress ip, int port, uint userId, uint oneTimeKey)
        {
            Socket.Send(new HandoffToGamePacket
            {
                OneTimeKey = oneTimeKey,
                ServerIp = BitConverter.ToUInt32(ip.GetAddressBytes(), 0),
                ServerPort = port,
                UserId = userId
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
