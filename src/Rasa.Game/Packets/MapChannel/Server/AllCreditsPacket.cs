namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AllCreditsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AllCredits;

        public int Credits { get; set; }
        public int Prestige { get; set; }

        public AllCreditsPacket(int creadits, int prestige)
        {
            Credits = creadits;
            Prestige = prestige;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(2);
            pw.WriteTuple(2);
            pw.WriteInt((int)CurencyType.Credits);
            pw.WriteInt(Credits);
            pw.WriteTuple(2);
            pw.WriteInt((int)CurencyType.Prestige);
            pw.WriteInt(Prestige);
        }
    }
}
