namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class RequestLockboxTabPermissionsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLockboxTabPermissions;
        
        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
