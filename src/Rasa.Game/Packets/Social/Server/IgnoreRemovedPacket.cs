namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;

    public class IgnoreRemovedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IgnoreRemoved;

        public uint AccountId { get; set; }

        public IgnoreRemovedPacket(uint accountId)
        {
            AccountId = accountId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(AccountId);
        }
    }
}
