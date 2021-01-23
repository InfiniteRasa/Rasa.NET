namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestArmWeaponPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestArmWeapon;

        public uint RequestedWeaponDrawerSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            RequestedWeaponDrawerSlot = pr.ReadUInt();
        }
    }
}
