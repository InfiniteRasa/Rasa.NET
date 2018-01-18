namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateCreditsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateCredits;

        public CurencyType Type { get; set; }
        public int Amount { get; set; }
        public int Delta { get; set; }

        public UpdateCreditsPacket(CurencyType type, int amount, int delta)
        {
            Type = type;
            Amount = amount;
            Delta = delta;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteInt(Amount);
            pw.WriteInt(0);     // ToDo
        }
    }
}