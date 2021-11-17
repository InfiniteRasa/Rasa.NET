namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;
    using Structures;

    public class FriendLoggedInPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FriendLoggedIn;

        public Friend Friend { get; set; }

        public FriendLoggedInPacket(Friend friend)
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
