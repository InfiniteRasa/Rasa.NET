namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestWeaponStowPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestWeaponStow;

        public int KeepAliveDelay { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
