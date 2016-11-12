namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestFamilyNamePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestFamilyName;

        public int LangId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            LangId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(LangId);
        }
    }
}
