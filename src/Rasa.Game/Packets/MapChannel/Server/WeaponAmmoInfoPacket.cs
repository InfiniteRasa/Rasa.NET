namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponAmmoInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponAmmoInfo;

        public int AmmoInfo { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(AmmoInfo);
        }
    }
}
