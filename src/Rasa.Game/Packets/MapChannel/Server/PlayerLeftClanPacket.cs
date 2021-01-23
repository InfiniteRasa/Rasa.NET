namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PlayerLeftClanPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.PlayerLeftClan;

        public ulong CharacterId { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public uint ClanId { get; }
        public bool WasKicked { get; }

        public PlayerLeftClanPacket(ulong characterId, string firstName, string familyName, uint clanId, bool wasKicked)
        {
            CharacterId = characterId;
            FirstName = firstName;
            LastName = familyName;
            ClanId = clanId;
            WasKicked = wasKicked;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteULong(CharacterId);
            pw.WriteString(FirstName);
            pw.WriteString(LastName);
            pw.WriteUInt(ClanId);
            pw.WriteBool(WasKicked);
        }
    }
}
