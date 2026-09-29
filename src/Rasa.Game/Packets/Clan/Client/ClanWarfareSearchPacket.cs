namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// ClanWarfareSearch (680), a world message from the clan war search window
    /// (client/ui/clanwarsearch.py OnSearch): <c>(requestId, nameContains, minLevel, maxLevel)</c>.
    /// Only a clan leader is offered the window - social window, Clans, Clan Warfare, Declare War
    /// (clan.CanClanChallenge, CLAN_RANK_TO_CHALLENGE).
    ///
    ///  - requestId: the window's own counter, one more each search, echoed in the answer;
    ///  - nameContains: the first word typed, or an empty unicode string;
    ///  - minLevel, maxLevel: 0 to 50, put in order when both are set; 0 is an empty or unreadable
    ///    box, so no limit on that side.
    ///
    /// Answered with <see cref="Server.ClanWarfareSearchResultsPacket"/>. Read leniently - a
    /// number of any width or a struct for the numbers, a byte or unicode string or None for the
    /// name - since the window is the only thing that fixes their types.
    /// </summary>
    public class ClanWarfareSearchPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanWarfareSearch;

        public int RequestId { get; set; }
        public string NameContains { get; set; } = "";
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            RequestId = ReadNumber(pr);
            NameContains = ReadText(pr);
            MinLevel = ReadNumber(pr);
            MaxLevel = ReadNumber(pr);
        }

        private static int ReadNumber(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.Int:
                    return pr.ReadInt();

                case PythonType.Long:
                    var value = pr.ReadLong();
                    return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;

                case PythonType.Structs:
                    return pr.ReadUnkStruct() == PythonStruct.True ? 1 : 0;

                default:
                    pr.SkipValue();
                    return 0;
            }
        }

        private static string ReadText(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.String:
                    return pr.ReadString() ?? "";

                case PythonType.UnicodeString:
                    return pr.ReadUnicodeString() ?? "";

                default:
                    pr.SkipValue();
                    return "";
            }
        }
    }
}
