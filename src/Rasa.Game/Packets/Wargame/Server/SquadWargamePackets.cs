using System.Collections.Generic;

namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;
    using Structures;

    // The Squad Wargame challenge of client/wargame.py, to SysEntity.ClientWargameManagerId. The
    // rest of a squad wargame - started, scoreboard, timer, results, refusal, revocation - is the
    // duel's (DuelPackets) and the feud's (ClanFeudPackets) packets.

    /// <summary>
    /// ChallengedToWargameSquad (627): Recv_ChallengedToWargameSquad(wargameId, agressorName,
    /// agressorSquadInfo, timeMins, maxKills). To every member of the squad challenged: a status
    /// indicator that opens the challenge window (ui/wargamechallenge.py) - the challenging squad
    /// listed, its leader marked, and Accept / Decline for the squad leader - and the wargame's
    /// status, invited, with the challengers as the enemy team. agressorSquadInfo is a list of
    /// (userId, name, classId, level, isAfk), as a squad member's party tuple.
    /// </summary>
    public class ChallengedToWargameSquadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengedToWargameSquad;

        public uint WargameId { get; }
        public string AgressorName { get; }
        public IReadOnlyList<PartyMember> AgressorSquadInfo { get; }
        public int TimeMins { get; }
        public int MaxKills { get; }

        public ChallengedToWargameSquadPacket(uint wargameId, string agressorName, IReadOnlyList<PartyMember> agressorSquadInfo, int timeMins, int maxKills)
        {
            WargameId = wargameId;
            AgressorName = agressorName ?? "";
            AgressorSquadInfo = agressorSquadInfo ?? new List<PartyMember>();
            TimeMins = timeMins;
            MaxKills = maxKills;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(AgressorName);
            pw.WriteList(AgressorSquadInfo.Count);

            foreach (var member in AgressorSquadInfo)
                pw.WriteStruct(member);

            pw.WriteInt(TimeMins);
            pw.WriteInt(MaxKills);
        }
    }

    /// <summary>
    /// ChallengingToWargameSquad (636): Recv_ChallengingToWargameSquad(wargameId, targetName,
    /// targetSquadInfo, timeMins, maxKills). To every member of the challenging squad: a status
    /// indicator that opens the challenge window with the squad challenged listed and Revoke for the
    /// squad leader, and the wargame's status with them as the enemy team.
    /// </summary>
    public class ChallengingToWargameSquadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengingToWargameSquad;

        public uint WargameId { get; }
        public string TargetName { get; }
        public IReadOnlyList<PartyMember> TargetSquadInfo { get; }
        public int TimeMins { get; }
        public int MaxKills { get; }

        public ChallengingToWargameSquadPacket(uint wargameId, string targetName, IReadOnlyList<PartyMember> targetSquadInfo, int timeMins, int maxKills)
        {
            WargameId = wargameId;
            TargetName = targetName ?? "";
            TargetSquadInfo = targetSquadInfo ?? new List<PartyMember>();
            TimeMins = timeMins;
            MaxKills = maxKills;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(TargetName);
            pw.WriteList(TargetSquadInfo.Count);

            foreach (var member in TargetSquadInfo)
                pw.WriteStruct(member);

            pw.WriteInt(TimeMins);
            pw.WriteInt(MaxKills);
        }
    }
}
