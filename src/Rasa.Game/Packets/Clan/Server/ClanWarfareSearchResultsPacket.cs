using System.Collections.Generic;

namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ClanWarfareSearchResults (681), on SysEntity.ClientWargameManagerId: the answer to a
    /// <see cref="Client.ClanWarfareSearchPacket"/>. client/wargame.py
    /// Recv_ClanWarfareSearchResults(requestId, warfareMatches) posts UI_UPDATE_CLAN_WARFARE_MATCH
    /// to the search window, which re-enables its Search button and lists the clans.
    ///
    /// warfareMatches is <c>{clanId: (clanName, memberCount, avgLevel, isOnline)}</c> - four
    /// values, though both docstrings say three: clanwarsearch._BuildClanList unpacks four and
    /// would throw on fewer. A clan with isOnline false is drawn in the disabled colour. The
    /// window leaves out the searcher's own clan itself, and shows nothing to a player in no clan.
    ///
    /// The window means to drop an answer older than the newest it has had, but stores that under
    /// a misspelt name (__recivedRequestId), so it takes every answer; one answer per request
    /// keeps it right.
    /// </summary>
    public class ClanWarfareSearchResultsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanWarfareSearchResults;

        public class Match
        {
            public uint ClanId { get; set; }
            public string ClanName { get; set; }
            public int MemberCount { get; set; }
            public int AverageLevel { get; set; }
            public bool IsOnline { get; set; }
        }

        public int RequestId { get; }
        public List<Match> Matches { get; }

        public ClanWarfareSearchResultsPacket(int requestId, List<Match> matches)
        {
            RequestId = requestId;
            Matches = matches ?? new List<Match>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(RequestId);
            pw.WriteDictionary(Matches.Count);

            foreach (var match in Matches)
            {
                pw.WriteUInt(match.ClanId);
                pw.WriteTuple(4);
                pw.WriteUnicodeString(match.ClanName ?? "");
                pw.WriteInt(match.MemberCount);
                pw.WriteInt(match.AverageLevel);
                pw.WriteBool(match.IsOnline);
            }
        }
    }
}
