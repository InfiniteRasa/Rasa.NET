namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class PartyChatPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyChat;

        public string Message {get; set;}
        public string Sender { get; set;}
        public ulong SenderUserId { get; set;}
        public ulong SenderEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            
            if (pr.PeekType() == PythonType.UnicodeString)
                Message= pr.ReadUnicodeString();
            else
                Message= pr.ReadString();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUnicodeString(Sender);
            pw.WriteUnicodeString(Message);
            pw.WriteULong(SenderUserId);
            pw.WriteULong(SenderEntityId);
        }
    }
}
