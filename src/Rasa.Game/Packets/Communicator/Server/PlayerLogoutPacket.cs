namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class PlayerLogoutPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerLogout;

        public string PlayerName { get; set; }
        public bool DisplayMessage { get; set; }

        public PlayerLogoutPacket(string playerName, bool displayMessage)
        {
            PlayerName = playerName;
            DisplayMessage = displayMessage;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(PlayerName);
            pw.WriteBool(DisplayMessage);
        }
    }
}
