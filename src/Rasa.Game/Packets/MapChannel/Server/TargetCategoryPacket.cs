namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TargetCategoryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TargetCategory;

        public int TargetCategory { get; set; }

        public TargetCategoryPacket(int targetCategory)
        {
            TargetCategory = targetCategory;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(TargetCategory);
        }
    }
}
