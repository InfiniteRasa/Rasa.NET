using System;
using System.IO;

namespace Rasa.Packets
{
    using Data;
    using Memory;

    public abstract class PythonPacket : IOpcodedPacket<GameOpcode>
    {
        public abstract GameOpcode Opcode { get; }

        /// <summary>The message this packet last went out in (MessageFor).</summary>
        private Protocol.CallMethodMessage _message;

        /// <summary>
        /// The message to send this packet on an entity in. A packet sent to one client after
        /// another, as every broadcast loop does, goes in the one message, which is encoded once
        /// for all of them (IBroadcastMessage). A new one is made for another entity, and once
        /// the message has been encoded: whoever is sent the packet from then on is sent it as it
        /// is then.
        /// </summary>
        internal Protocol.CallMethodMessage MessageFor(ulong entityId)
        {
            var message = _message;

            if (message == null || message.EntityId != entityId || message.IsEncoded)
                _message = message = new Protocol.CallMethodMessage(entityId, this);

            return message;
        }

        public void Read(BinaryReader br)
        {
            using (var pr = new PythonReader(br))
                Read(pr);
        }

        public void Write(BinaryWriter bw)
        {
            using (var pw = new PythonWriter(bw))
                Write(pw);
        }

        public abstract void Read(PythonReader pr);
        public abstract void Write(PythonWriter pw);
    }

    public abstract class ClientPythonPacket : PythonPacket
    {
        public override sealed void Write(PythonWriter pw)
        {
            throw new InvalidOperationException();
        }
    }

    public abstract class ServerPythonPacket : PythonPacket
    {
        public override sealed void Read(PythonReader pw)
        {
            throw new InvalidOperationException();
        }
    }
}
