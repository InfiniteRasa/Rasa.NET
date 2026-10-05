namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// "LeaveClan", (member.characterId, member.clanId): client/clan.py SendLeaveClan, from
    /// /clanleave and the clan window's Leave, for the client's own line of the roster.
    /// </summary>
    public class LeaveClanPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.LeaveClan;

        /// <summary>
        /// The client's id for the member, as it holds it: what the server sent the line with
        /// (ClanMemberData.Write), which is a Python long, and in a member's own line their
        /// manifestation's entity id and not their character id. It is not what says who
        /// leaves - whoever sent this does (ClanManager.LeaveClan).
        ///
        /// Read as an int alone, the long threw out of the decode, which closes the connection:
        /// nobody could leave a clan.
        /// </summary>
        public ulong CharacterId { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.PeekType() == PythonType.Long ? pr.ReadULong() : pr.ReadUInt();
            ClanId = pr.ReadUInt();
        }
    }
}
