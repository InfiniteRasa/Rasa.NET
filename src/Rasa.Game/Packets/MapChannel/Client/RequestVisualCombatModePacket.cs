namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVisualCombatModePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVisualCombatMode;

        public int CombatMode { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CombatMode = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
