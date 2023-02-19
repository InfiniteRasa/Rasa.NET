namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class WhisperPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Whisper;

        public string Reciver { get; set; }
        public string Sender { get; set; }
        public ulong SenderEntityId { get; set; }
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

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUnicodeString(Sender);
            pw.WriteUnicodeString(Message);
            pw.WriteULong(SenderEntityId);
        }
    }
}
