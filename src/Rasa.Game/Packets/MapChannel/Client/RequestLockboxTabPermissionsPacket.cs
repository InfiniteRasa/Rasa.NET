namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestLockboxTabPermissionsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLockboxTabPermissions;
        
        // 0 elements
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
