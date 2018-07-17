namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponAmmoInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponAmmoInfo;

        public int AmmoInfo { get; set; }

        public WeaponAmmoInfoPacket(int ammoInfo)
        {
            AmmoInfo = ammoInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(AmmoInfo);
        }
    }
}
