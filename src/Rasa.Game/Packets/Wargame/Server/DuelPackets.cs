using System.Collections.Generic;

namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    // The duel methods of client/wargame.py, all to SysEntity.ClientWargameManagerId. The results
    // (WargameResultPacket), the scoreboard (WargameScoreboardPacket), messages
    // (DisplayWargameMessagePacket) and the sides (WargameDataPacket) are shared with the feuds.

    /// <summary>
    /// ChallengedToWargameDuel (603): Recv_ChallengedToWargameDuel(wargameId, agressorName). To the
    /// one challenged: the duel tutorial, a status indicator that opens the Accept / Decline dialog
    /// ("%(player)s has challenged you to a Wargame Duel."), and the wargame's status, invited.
    /// </summary>
    public class ChallengedToWargameDuelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengedToWargameDuel;

        public uint WargameId { get; }
        public string AgressorName { get; }

        public ChallengedToWargameDuelPacket(uint wargameId, string agressorName)
        {
            WargameId = wargameId;
            AgressorName = agressorName ?? "";
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(AgressorName);
        }
    }

    /// <summary>
    /// ChallengingToWargameDuel (635): Recv_ChallengingToWargameDuel(wargameId, targetName). To the
    /// challenger: the duel tutorial and a status indicator that opens the Revoke / Close dialog
    /// ("You are challenging %(player)s to a Wargame Duel.").
    /// </summary>
    public class ChallengingToWargameDuelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengingToWargameDuel;

        public uint WargameId { get; }
        public string TargetName { get; }

        public ChallengingToWargameDuelPacket(uint wargameId, string targetName)
        {
            WargameId = wargameId;
            TargetName = targetName ?? "";
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(TargetName);
        }
    }

    /// <summary>
    /// WargameChallengeRefused (637): Recv_WargameChallengeRefused(wargameId, yourPartyRefused) -
    /// the challenge's dialogs and indicators close. yourPartyRefused is true for the side that
    /// said no.
    /// </summary>
    public class WargameChallengeRefusedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeRefused;

        public uint WargameId { get; }
        public bool YourPartyRefused { get; }

        public WargameChallengeRefusedPacket(uint wargameId, bool yourPartyRefused)
        {
            WargameId = wargameId;
            YourPartyRefused = yourPartyRefused;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteBool(YourPartyRefused);
        }
    }

    /// <summary>
    /// WargameStarted (633): Recv_WargameStarted(wargameId, enemyUserIds) - the wargame is active:
    /// the tracker comes up, the challenge's dialogs and indicators close, and the start sound
    /// plays. enemyUserIds are account ids, as the scoreboard's are.
    /// </summary>
    public class WargameStartedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameStarted;

        public uint WargameId { get; }
        public IReadOnlyList<uint> EnemyUserIds { get; }

        public WargameStartedPacket(uint wargameId, IReadOnlyList<uint> enemyUserIds)
        {
            WargameId = wargameId;
            EnemyUserIds = enemyUserIds ?? new List<uint>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteList(EnemyUserIds.Count);

            foreach (var id in EnemyUserIds)
                pw.WriteUInt(id);
        }
    }

    /// <summary>
    /// RevokeWargameChallenge (630): Recv_RevokeWargameChallenge(wargameId) - a challenge is off:
    /// its dialogs and indicators close and its status is forgotten.
    /// </summary>
    public class RevokeWargameChallengePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RevokeWargameChallenge;

        public uint WargameId { get; }

        public RevokeWargameChallengePacket(uint wargameId)
        {
            WargameId = wargameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WargameId);
        }
    }

    /// <summary>
    /// RemoveFromWargame (715): Recv_RemoveFromWargame(wargameId) - this player is out of the
    /// wargame: it goes inactive, the tracker goes, and the client says PM_WARGAME_YOU_LEFT.
    /// </summary>
    public class RemoveFromWargamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveFromWargame;

        public uint WargameId { get; }

        public RemoveFromWargamePacket(uint wargameId)
        {
            WargameId = wargameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WargameId);
        }
    }

    /// <summary>DisplayWargameTimer (628): Recv_DisplayWargameTimer(wargameId, timeMs) - the tracker's clock: timeMs from now.</summary>
    public class DisplayWargameTimerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayWargameTimer;

        public uint WargameId { get; }
        public int TimeMs { get; }

        public DisplayWargameTimerPacket(uint wargameId, int timeMs)
        {
            WargameId = wargameId;
            TimeMs = timeMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteInt(TimeMs);
        }
    }

    /// <summary>SetWargameMaxKills (631): Recv_SetWargameMaxKills(wargameId, maxKills) - the tracker's kill target.</summary>
    public class SetWargameMaxKillsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetWargameMaxKills;

        public uint WargameId { get; }
        public int MaxKills { get; }

        public SetWargameMaxKillsPacket(uint wargameId, int maxKills)
        {
            WargameId = wargameId;
            MaxKills = maxKills;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteInt(MaxKills);
        }
    }
}
