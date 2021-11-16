namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class ClanChatPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanChat;
        
        public uint ClanId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }

        public ClanChatPacket()
        {
        }

        public ClanChatPacket(string sender, string message)
        {
            Message = message;
            Sender = sender;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanId = pr.ReadUInt();
            Message = pr.ReadUnicodeString();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(Sender);
            pw.WriteUnicodeString(Message);
        }
    }
}
