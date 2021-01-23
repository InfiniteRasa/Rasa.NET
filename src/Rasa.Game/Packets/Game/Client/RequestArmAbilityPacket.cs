namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestArmAbilityPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestArmAbility;

        public int AbilityDrawerSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            AbilityDrawerSlot = pr.ReadInt();
        }
    }
}
