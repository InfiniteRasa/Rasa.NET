namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ChallengeClanToFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengeClanToFeud;

        public string ClanName { get; set; }
        public bool Invite { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = pr.ReadUnicodeString();
            Invite = pr.ReadBool();
        }
    }
}
