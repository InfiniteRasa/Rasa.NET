using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class CallMethodMessage : ISubtypedPacket<CallMethodMessageSubtype>, IBroadcastMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.CallMethod;
        public byte RawSubtype { get; set; }

        public CallMethodMessageSubtype Subtype
        {
            get { return (CallMethodMessageSubtype) RawSubtype; }
            set { RawSubtype = (byte) value; }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 4;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public ulong EntityId { get; private set; }
        public GameOpcode MethodId { get; set; }
        public string MethodName { get; set; }
        public uint UnknownValue { get; set; }
        public PythonPacket Packet { get; }

        private int _recipients;
        private byte[] _encoded;

        int IBroadcastMessage.Recipients => Volatile.Read(ref _recipients);

        void IBroadcastMessage.AddRecipient() => Interlocked.Increment(ref _recipients);

        byte[] IBroadcastMessage.Encoded
        {
            get => Volatile.Read(ref _encoded);
            set => Volatile.Write(ref _encoded, value);
        }

        /// <summary>Encoded for the clients it was handed to (IBroadcastMessage): not to be handed to another.</summary>
        internal bool IsEncoded => Volatile.Read(ref _encoded) != null;

        public CallMethodMessage(ulong entityId, PythonPacket packet)
        {
            EntityId = entityId;
            Packet = packet;
            Subtype = CallMethodMessageSubtype.MethodId;
            MethodId = packet.Opcode;
        }

        public CallMethodMessage(ulong entityId, string methodName, PythonPacket packet)
        {
            EntityId = entityId;
            MethodName = methodName;
            Packet = packet;
            Subtype = CallMethodMessageSubtype.MethodName;
        }

        public void Read(ProtocolBufferReader reader)
        {
            EntityId = reader.ReadULong();

            switch (Subtype)
            {
                case CallMethodMessageSubtype.MethodId:
                case CallMethodMessageSubtype.UnkPlusMethodId:
                    MethodId = (GameOpcode) reader.ReadUInt();
                    break;

                case CallMethodMessageSubtype.MethodName:
                case CallMethodMessageSubtype.UnkPlusMethodName:
                    MethodName = reader.ReadString();
                    break;

                default:
                    throw new Exception($"Invalid subtype ({Subtype}) for CallMethodMessage!");
            }

            reader.ReadArray(); // No need to properly implement this, it won't be called anyways, the Read function is only here for clarity

            if (Subtype == CallMethodMessageSubtype.UnkPlusMethodId || Subtype == CallMethodMessageSubtype.UnkPlusMethodName)
                UnknownValue = reader.ReadUInt();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            writer.WriteULong(EntityId);

            switch (Subtype)
            {
                case CallMethodMessageSubtype.MethodId:
                case CallMethodMessageSubtype.UnkPlusMethodId:
                    writer.WriteUInt((uint) MethodId);
                    break;

                case CallMethodMessageSubtype.MethodName:
                case CallMethodMessageSubtype.UnkPlusMethodName:
                    writer.WriteString(MethodName);
                    break;

                default:
                    throw new Exception($"Invalid subtype ({Subtype}) for CallMethodMessage!");
            }

            var buffer = ArrayPool<byte>.Shared.Rent(0x2000);

            using (var bw = new BinaryWriter(new MemoryStream(buffer)))
            {
                bw.Write((byte) 0x4F); // 'O' - Optimized Marshall Format

                Packet.Write(bw);

                bw.Write((byte) 0x66); // 'f' - python payload finish mark

                writer.WriteArray(buffer, 0, (int) bw.BaseStream.Position);
            }

            ArrayPool<byte>.Shared.Return(buffer);

            if (Subtype == CallMethodMessageSubtype.UnkPlusMethodId || Subtype == CallMethodMessageSubtype.UnkPlusMethodName)
                writer.WriteUInt(UnknownValue);
        }
    }
}
