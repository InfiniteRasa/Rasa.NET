namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestFamilyNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestFamilyName;

        public int LangId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            LangId = pr.ReadInt();
        }
    }
}
