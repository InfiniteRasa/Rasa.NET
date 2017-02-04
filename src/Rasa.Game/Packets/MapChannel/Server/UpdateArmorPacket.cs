namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateArmorPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateArmor;

        public double Current { get; set; }
        public double CurrentMax { get; set; }
        public double RefreshAmount { get; set; }
        public int WhoId { get; set; }

        public UpdateArmorPacket(double curent, double curentMax, double refreshAmount, int whoId)
        {
            Current = curent;
            CurrentMax = curentMax;
            RefreshAmount = refreshAmount;
            WhoId = whoId;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteDouble(Current);
            pw.WriteDouble(CurrentMax);
            pw.WriteDouble(RefreshAmount);
            pw.WriteInt(WhoId);
        }
    }
}
