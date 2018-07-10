namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestCharacterNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCharacterName;

        public int Gender { get; set; }
        public int LangId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Gender = pr.ReadInt();
            LangId = pr.ReadInt();
        }
    }
}
