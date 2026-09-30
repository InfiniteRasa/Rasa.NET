using System.Collections.Generic;

namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    // The Clan Feud methods of client/wargame.py. All but WargameData go to
    // SysEntity.ClientWargameManagerId; WargameData goes to the actor it describes.

    /// <summary>
    /// ClanWargameTestSuccess (683): Recv_ClanWargameTestSuccess(clanName). The answer to a
    /// ChallengeClanToFeud with doInvite false (/feud): the challenge would be allowed, so the
    /// client opens the Declaration of War dialog with its Send button, naming this clan.
    /// </summary>
    public class ClanWargameTestSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanWargameTestSuccess;

        public string ClanName { get; }

        public ClanWargameTestSuccessPacket(string clanName)
        {
            ClanName = clanName ?? "";
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(ClanName);
        }
    }

    /// <summary>
    /// ClanWargameInviteReceived (682): Recv_ClanWargameInviteReceived(wargameId, clanName). To the
    /// challenged clan's leader: a permanent status indicator, keyed by the challenger's name, that
    /// opens the Declaration of War dialog with Accept and Cancel.
    /// </summary>
    public class ClanWargameInviteReceivedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanWargameInviteReceived;

        public uint WargameId { get; }
        public string ClanName { get; }

        public ClanWargameInviteReceivedPacket(uint wargameId, string clanName)
        {
            WargameId = wargameId;
            ClanName = clanName ?? "";
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(ClanName);
        }
    }

    /// <summary>
    /// AddToClanWargame (678): Recv_AddToClanWargame(wargameId, yourClanName, theirClanName,
    /// timeLeftSecs, yourKillCount, theirKillCount). Puts the feud in the wargame tracker and the
    /// social window's Clan Warfare list, active, with its clock and score. Sent again it only
    /// refreshes them: the list is rebuilt from the client's statuses each time.
    /// </summary>
    public class AddToClanWargamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddToClanWargame;

        public uint WargameId { get; }
        public string YourClanName { get; }
        public string TheirClanName { get; }
        public int TimeLeftSecs { get; }
        public int YourKills { get; }
        public int TheirKills { get; }

        public AddToClanWargamePacket(uint wargameId, string yourClanName, string theirClanName, int timeLeftSecs, int yourKills, int theirKills)
        {
            WargameId = wargameId;
            YourClanName = yourClanName ?? "";
            TheirClanName = theirClanName ?? "";
            TimeLeftSecs = timeLeftSecs;
            YourKills = yourKills;
            TheirKills = theirKills;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(YourClanName);
            pw.WriteUnicodeString(TheirClanName);
            pw.WriteInt(TimeLeftSecs);
            pw.WriteInt(YourKills);
            pw.WriteInt(TheirKills);
        }
    }

    /// <summary>RemoveFromClanWargame (679): Recv_RemoveFromClanWargame(wargameId) - the feud is inactive for this player and leaves the tracker and the list.</summary>
    public class RemoveFromClanWargamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveFromClanWargame;

        public uint WargameId { get; }

        public RemoveFromClanWargamePacket(uint wargameId)
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
    /// WargameVictory (608), WargameDefeat (607), WargameTied (634) and WargameCancelled (605),
    /// each (wargameId). For a feud the first three show the big "Feud Wargame" text and play
    /// the end sound, and say nothing in chat; WargameCancelled says PM_WARGAME_CANCELLED.
    /// </summary>
    public class WargameResultPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; }

        public uint WargameId { get; }

        private WargameResultPacket(GameOpcode opcode, uint wargameId)
        {
            Opcode = opcode;
            WargameId = wargameId;
        }

        public static WargameResultPacket Victory(uint wargameId) => new WargameResultPacket(GameOpcode.WargameVictory, wargameId);
        public static WargameResultPacket Defeat(uint wargameId) => new WargameResultPacket(GameOpcode.WargameDefeat, wargameId);
        public static WargameResultPacket Tied(uint wargameId) => new WargameResultPacket(GameOpcode.WargameTied, wargameId);
        public static WargameResultPacket Cancelled(uint wargameId) => new WargameResultPacket(GameOpcode.WargameCancelled, wargameId);

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WargameId);
        }
    }

    /// <summary>
    /// WargameScoreboard (632): Recv_WargameScoreboard(wargameId, yourKills, theirKills, victimId,
    /// killerId), the ids account ids (WargameStatus.UpdateScores keeps a tally per user id). For a
    /// feud it also updates the Clan Warfare list's score.
    /// </summary>
    public class WargameScoreboardPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameScoreboard;

        public uint WargameId { get; }
        public int YourKills { get; }
        public int TheirKills { get; }
        public uint VictimUserId { get; }
        public uint KillerUserId { get; }

        public WargameScoreboardPacket(uint wargameId, int yourKills, int theirKills, uint victimUserId, uint killerUserId)
        {
            WargameId = wargameId;
            YourKills = yourKills;
            TheirKills = theirKills;
            VictimUserId = victimUserId;
            KillerUserId = killerUserId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(WargameId);
            pw.WriteInt(YourKills);
            pw.WriteInt(TheirKills);
            pw.WriteUInt(VictimUserId);
            pw.WriteUInt(KillerUserId);
        }
    }

    /// <summary>
    /// DisplayWargameMessage (604): Recv_DisplayWargameMessage(msgId, args) - a player message in
    /// the system chat filter, its %(name)s fields filled from args.
    /// </summary>
    public class DisplayWargameMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayWargameMessage;

        public PlayerMessage Message { get; }
        public Dictionary<string, string> Args { get; }

        public DisplayWargameMessagePacket(PlayerMessage message, Dictionary<string, string> args = null)
        {
            Message = message;
            Args = args ?? new Dictionary<string, string>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt((int)Message);
            pw.WriteDictionary(Args.Count);

            foreach (var arg in Args)
            {
                pw.WriteString(arg.Key);
                pw.WriteUnicodeString(arg.Value ?? "");
            }
        }
    }

    /// <summary>
    /// WargameData (696), on the actor: Actor.Recv_WargameData(wargameData), the wargames the actor
    /// is in as <c>{wargameId: side}</c>. Two actors sharing a wargame id are allies if their sides
    /// are equal and enemies if not (GetWargameParticipantStatus): the overhead frame, the
    /// friendly-target check and the heal disc all read it. An empty dict is "in none".
    /// </summary>
    public class WargameDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameData;

        public Dictionary<uint, bool> Wargames { get; }

        public WargameDataPacket(Dictionary<uint, bool> wargames)
        {
            Wargames = wargames ?? new Dictionary<uint, bool>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(Wargames.Count);

            foreach (var wargame in Wargames)
            {
                pw.WriteUInt(wargame.Key);
                pw.WriteBool(wargame.Value);
            }
        }
    }
}
