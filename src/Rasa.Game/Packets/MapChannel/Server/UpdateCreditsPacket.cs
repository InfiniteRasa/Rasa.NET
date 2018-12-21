namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateCreditsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateCredits;

        public CurencyType Type { get; set; }
        public uint Amount { get; set; }
        public uint Delta { get; set; }

        public UpdateCreditsPacket(CurencyType type, uint amount, uint delta)
        {
            Type = type;
            Amount = amount;
            Delta = delta;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteUInt(Amount);
            pw.WriteUInt(0u);     // ToDo
        }
    }
}
