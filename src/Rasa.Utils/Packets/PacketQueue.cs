using System.Collections.Generic;

namespace Rasa.Packets
{
    using Networking;

    public class PacketQueue : IPacketQueue
    {
        private readonly Queue<QueuedPacket> _incomingQueue;
        private readonly Queue<QueuedPacket> _outgoingQueue;

        public PacketQueue()
        {
            _incomingQueue = new Queue<QueuedPacket>();
            _outgoingQueue = new Queue<QueuedPacket>();
        }

        public void EnqueueIncoming(INetworkClient client, IBasePacket data)
        {
            var packet = new QueuedPacket
            {
                Client = client,
                Packet = data
            };

            lock (_incomingQueue)
                _incomingQueue.Enqueue(packet);
        }

        public QueuedPacket PopIncoming()
        {
            lock (_incomingQueue)
                return _incomingQueue.Count > 0 ?_incomingQueue.Dequeue() : null;
        }

        public void EnqueueOutgoing(INetworkClient client, IBasePacket data)
        {
            var packet = new QueuedPacket
            {
                Client = client,
                Packet = data
            };

            lock (_outgoingQueue)
                _outgoingQueue.Enqueue(packet);
        }

        public QueuedPacket PopOutgoing()
        {
            lock (_outgoingQueue)
                return _outgoingQueue.Count > 0 ? _outgoingQueue.Dequeue() : null;
        }
    }
}
