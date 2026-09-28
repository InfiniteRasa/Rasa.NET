namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TargetCategoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TargetCategory;

        public TargetCategory TargetCategory { get; set; }

        public TargetCategoryPacket(Factions targetCategory) : this((TargetCategory)targetCategory)
        {
        }

        public TargetCategoryPacket(TargetCategory targetCategory)
        {
            TargetCategory = targetCategory;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)TargetCategory);
        }
    }
}
