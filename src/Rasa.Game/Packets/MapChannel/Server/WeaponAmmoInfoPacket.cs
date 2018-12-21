namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponAmmoInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponAmmoInfo;

        public uint AmmoInfo { get; set; }

        public WeaponAmmoInfoPacket(uint ammoInfo)
        {
            AmmoInfo = ammoInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(AmmoInfo);
        }
    }
}
