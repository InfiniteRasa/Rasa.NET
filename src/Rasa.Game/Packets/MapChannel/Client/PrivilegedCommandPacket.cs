namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PrivilegedCommandPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PrivilegedCommand;
        
        public string Command { get; set; }
        public string Args { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Command = pr.ReadUnicodeString();
            Args = pr.ReadUnicodeString();
        }
    }
}
