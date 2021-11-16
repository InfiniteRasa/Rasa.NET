namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class FeudChallengeResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FeudChallengeResponse;

        public string ClanName { get; set; }
        public bool AcceptChalange { get; set; }
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = pr.ReadUnicodeString();
            AcceptChalange = pr.ReadBool();
        }
    }
}
