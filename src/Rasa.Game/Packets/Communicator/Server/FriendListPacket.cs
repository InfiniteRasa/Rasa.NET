namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class FriendListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FriendList;

        // FriendList does nothing on client side
        public object PlayersAndStatus { get; set; }    // unknown

        public FriendListPacket(object playersAndStatus)
        {
            PlayersAndStatus = playersAndStatus;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteNoneStruct();
        }
    }
}
