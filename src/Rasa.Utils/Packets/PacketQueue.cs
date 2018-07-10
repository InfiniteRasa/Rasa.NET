using System.Collections.Generic;

namespace Rasa.Packets
{
    public class PacketQueue : IPacketQueue
    {
        private readonly Queue<IBasePacket> _incomingQueue;
        private readonly Queue<IBasePacket> _outgoingQueue;

        public PacketQueue()
        {
            _incomingQueue = new Queue<IBasePacket>();
            _outgoingQueue = new Queue<IBasePacket>();
        }

        public void EnqueueIncoming(IBasePacket data)
        {
            lock (_incomingQueue)
                _incomingQueue.Enqueue(data);
        }

        public IBasePacket PopIncoming()
        {
            lock (_incomingQueue)
                return _incomingQueue.Count > 0 ?_incomingQueue.Dequeue() : null;
        }

        public void EnqueueOutgoing(IBasePacket data)
        {
            lock (_outgoingQueue)
                _outgoingQueue.Enqueue(data);
        }

        public IBasePacket PopOutgoing()
        {
            lock (_outgoingQueue)
                return _outgoingQueue.Count > 0 ? _outgoingQueue.Dequeue() : null;
        }
    }
}
