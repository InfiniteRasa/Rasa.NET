namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;
    using Models;

    public class MoveMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.Move;

        public byte RawSubtype
        {
            get { return 0; }
            set { }
        } 

        public byte MinSubtype { get; } = 0;
        public byte MaxSubtype { get; } = 0;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.None;

        public byte UnkByte { get; set; }

        public Movement Movement { get; private set; }
        public MoveMessage()
        {
        }

        public void Read(ProtocolBufferReader reader)
        {
            UnkByte = reader.ReadByte();
            Movement = reader.ReadMovement();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            writer.WriteByte(UnkByte);
            writer.WriteMovementData(Movement); // TODO is this required?
        }
    }
}
