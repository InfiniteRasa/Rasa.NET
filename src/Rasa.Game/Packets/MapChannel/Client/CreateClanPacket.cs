namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CreateClanPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreateClan;

        public string ClanName { get; set; }
        public bool IsPvP { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = pr.ReadUnicodeString();
            IsPvP = pr.ReadBool();
        }
    }
}
