namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestArmWeaponPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestArmWeapon;

        public int RequestedWeaponDrawerSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            RequestedWeaponDrawerSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
