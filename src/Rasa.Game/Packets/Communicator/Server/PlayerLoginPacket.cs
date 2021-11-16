namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class PlayerLoginPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerLogin;

        public string PlayerName { get; set; }
        public bool DisplayMessage { get; set; }

        public PlayerLoginPacket(string playerName, bool displayMessage)
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
