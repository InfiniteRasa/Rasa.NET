namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;

    public class FriendLoggedOutPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FriendLoggedOut;

        public uint UserId { get; set; }

        public FriendLoggedOutPacket(uint userId)
        {
            UserId = userId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(UserId);
        }
    }
}
