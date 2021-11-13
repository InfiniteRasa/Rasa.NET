namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;

    public class FriendRemovedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FriendRemoved;

        public uint AccountId { get; set; }

        public FriendRemovedPacket(uint accountId)
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
