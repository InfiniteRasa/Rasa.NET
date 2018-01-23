namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetKillStreakPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetKillStreak;

        public int Count { get; set; }

        public SetKillStreakPacket(int count)
        {
            Count = count;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Count);
        }
    }
}
