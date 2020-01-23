namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestVisualCombatModePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVisualCombatMode;

        public bool CombatMode { get; set; }

        public RequestVisualCombatModePacket()
        {
        }

        public RequestVisualCombatModePacket(bool combatMode)
        {
            CombatMode = combatMode;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CombatMode = pr.ReadBool();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(CombatMode);
        }
    }
}
