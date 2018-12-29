namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponReadyPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponReady;

        public bool WeaponReady { get; set; }

        public WeaponReadyPacket(bool weaponReady)
        {
            WeaponReady = weaponReady;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(WeaponReady);
        }
    }
}
