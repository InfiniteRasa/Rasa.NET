namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;
    using Structures;

    public class FriendStatusUpdatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FriendAdded;

        public Friend Friend { get; set; }

        public FriendStatusUpdatePacket(Friend friend)
        {
            Friend = friend;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteStruct(Friend);
        }
    }
}
