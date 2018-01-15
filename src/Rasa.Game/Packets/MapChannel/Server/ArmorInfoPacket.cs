namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ArmorInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ArmorInfo;

        public int CurrentHitPoints { get; set; }
        public int MaxHitPoints { get; set; }

        public ArmorInfoPacket(int current, int max)
        {
            CurrentHitPoints = current;
            MaxHitPoints = max;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(CurrentHitPoints);
            pw.WriteInt(MaxHitPoints);
        }
    }
}
