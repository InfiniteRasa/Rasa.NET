using System.Collections.Generic;

namespace Rasa.Packets.Team.Server
{
    using Data;
    using Memory;

    // The team manager of client/team.py, to SysEntity.ClientTeamManagerId: who is on the
    // player's team on a battleground map, and the scorekeeper behind the battleground tracker
    // and the scoreboard window (client/scorekeeperclient.py, ui/scoreboardwindow.py,
    // ui/wargametracker.py). The client asks for none of it: the server puts a player on a team
    // and says so.

    /// <summary>
    /// SetNumberOfTeams (890): Recv_SetNumberOfTeams(numTeams). How many teams the map the player
    /// is on has; none anywhere else. The client's own rules read it - a Temporary Wormhole is
    /// refused to a player with no team on a map that has teams.
    /// </summary>
    public class SetNumberOfTeamsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetNumberOfTeams;

        public int Teams { get; }

        public SetNumberOfTeamsPacket(int teams)
        {
            Teams = teams;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Teams);
        }
    }

    /// <summary>
    /// JoinedTeam (10000122): Recv_JoinedTeam(teamId, members). The player is on the team
    /// (teamconstants: RED_TEAM 1, BLUE_TEAM 2) - "You joined %(team)s." - with these entity ids
    /// on it, their own left out by the client.
    /// </summary>
    public class JoinedTeamPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.JoinedTeam;

        public uint TeamId { get; }
        public List<ulong> Members { get; }

        public JoinedTeamPacket(uint teamId, List<ulong> members)
        {
            TeamId = teamId;
            Members = members ?? new List<ulong>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(TeamId);
            pw.WriteList(Members.Count);

            foreach (var member in Members)
                pw.WriteULong(member);
        }
    }

    /// <summary>LeftTeam (10000123): Recv_LeftTeam(). The player is on no team: "You left %(team)s."</summary>
    public class LeftTeamPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LeftTeam;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }

    /// <summary>AddTeamMember (10000117): Recv_AddTeamMember(characterId). Somebody else has joined the player's team; an entity id.</summary>
    public class AddTeamMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddTeamMember;

        public ulong EntityId { get; }

        public AddTeamMemberPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(EntityId);
        }
    }

    /// <summary>RemoveTeamMember (10000118): Recv_RemoveTeamMember(characterId). Somebody else has left the player's team.</summary>
    public class RemoveTeamMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveTeamMember;

        public ulong EntityId { get; }

        public RemoveTeamMemberPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(EntityId);
        }
    }

    /// <summary>
    /// ScoreBoardActive (877): Recv_ScoreBoardActive(bActive). A game has begun, and the
    /// scoreboard is cleared for it, or has ended, and what it showed is kept as the last game's.
    /// </summary>
    public class ScoreBoardActivePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ScoreBoardActive;

        public bool Active { get; }

        public ScoreBoardActivePacket(bool active)
        {
            Active = active;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(Active);
        }
    }

    /// <summary>
    /// ScoreBoardGameScore (880): Recv_ScoreBoardGameScore(remainingTime, cpData). The seconds
    /// left on the clock and who holds each control point, {controlPointId: teamId}, by the
    /// client's own control point ids (controlpointdata); a point nobody holds has None. The
    /// first of these on a map turns the battleground tracker on.
    /// </summary>
    public class ScoreBoardGameScorePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ScoreBoardGameScore;

        public int RemainingSeconds { get; }
        public Dictionary<uint, uint> ControlPoints { get; }

        public ScoreBoardGameScorePacket(int remainingSeconds, Dictionary<uint, uint> controlPoints)
        {
            RemainingSeconds = remainingSeconds;
            ControlPoints = controlPoints ?? new Dictionary<uint, uint>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(RemainingSeconds);
            pw.WriteDictionary(ControlPoints.Count);

            foreach (var point in ControlPoints)
            {
                pw.WriteUInt(point.Key);

                if (point.Value == 0)
                    pw.WriteNoneStruct();
                else
                    pw.WriteUInt(point.Value);
            }
        }
    }

    /// <summary>
    /// ScoreBoardIndividualUpdate (875): Recv_ScoreBoardIndividualUpdate(entityId,
    /// individualUpdate). One player's row, in shared/scorekeeperconstants.py's order: name,
    /// class, team, active, kills, deaths, damage, healing, captures, prestige. A row that is not
    /// active is hidden. The client's tracker writes kills and deaths into the row it was given
    /// (ScoreBoardTrackerUpdate), so the row is a list.
    /// </summary>
    public class ScoreBoardIndividualUpdatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ScoreBoardIndividualUpdate;

        public ulong EntityId { get; }
        public string Name { get; set; }
        public uint ClassId { get; set; }
        public uint TeamId { get; set; }
        public bool Active { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int Captures { get; set; }
        public int Prestige { get; set; }

        public ScoreBoardIndividualUpdatePacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(EntityId);
            pw.WriteList(10);
            pw.WriteUnicodeString(Name ?? "");
            pw.WriteUInt(ClassId);
            pw.WriteUInt(TeamId);
            pw.WriteBool(Active);
            pw.WriteInt(Kills);
            pw.WriteInt(Deaths);
            pw.WriteInt(Damage);
            pw.WriteInt(Healing);
            pw.WriteInt(Captures);
            pw.WriteInt(Prestige);
        }
    }

    /// <summary>WonBattleground (872), on the player's own manifestation: "You won the battleground match!"</summary>
    public class WonBattlegroundPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WonBattleground;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }

    /// <summary>LostBattleground (871), on the player's own manifestation: "You lost the battleground match."</summary>
    public class LostBattlegroundPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LostBattleground;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
