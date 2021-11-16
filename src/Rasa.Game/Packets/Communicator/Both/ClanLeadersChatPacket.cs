namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class ClanLeadersChatPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanLeadersChat;

        public uint ClanId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }

        public ClanLeadersChatPacket()
        {
        }

        public ClanLeadersChatPacket(string sender, string message)
        {
            Sender = sender;
            Message = message;
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
