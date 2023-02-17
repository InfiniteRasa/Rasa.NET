namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ReplyPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Reply;

        public string Reciver { get; set; }
        public string Message { get; set; }
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            if (pr.PeekType() == PythonType.UnicodeString)
                Reciver = pr.ReadUnicodeString();
            else
                Reciver = pr.ReadString();

            Message = pr.ReadUnicodeString();
        }
    }
}
