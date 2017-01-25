namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LoginOkPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LoginOk;

        public string PlayerName { get; set; }

        public LoginOkPacket(string playerName)
        {
            PlayerName = playerName;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(PlayerName);
        }
    }
}
