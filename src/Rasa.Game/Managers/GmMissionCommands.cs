using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;

    /// <summary>
    /// A GM's view of another player's missions, the GM Complete button in it, and giving missions.
    ///
    ///  - /usermissions and .usermissions [familyName | #characterId]: GmShowUserMissionsAck with
    ///    the player's mission log, which the client opens in the intel window. No argument is the
    ///    GM's own log. The retail command that asked for it has not survived; this is its stand-in.
    ///  - ForceCompleteObjective: that window's Complete button. The objective is completed through
    ///    MissionApplication.TryForceCompleteObjective and the GM is sent the log again, since the
    ///    client never refreshes the view on its own.
    ///  - .completeobjective missionId objectiveId [familyName | #characterId]: the same without the
    ///    window.
    ///  - /givemission: no argument opens the client's Give Mission picker (QAGiveMissionAck) on
    ///    the GM's own missions; the pick comes back as /givemission &lt;id&gt; and gives it to the GM
    ///    (MissionApplication.TryGiveMission).
    ///  - .givemission missionId [familyName | #characterId]: gives it to another player; no
    ///    argument opens the picker, as /givemission does.
    ///
    /// All at GameMaster. The player has to be in the world: an offline character's log is not
    /// loaded, and a completion has to reach their client.
    /// </summary>
    public static class GmMissionCommands
    {
        public const GmLevel Level = GmLevel.GameMaster;

        /// <summary>For tests: the clients looked in. Server.Clients when null.</summary>
        internal static Func<IReadOnlyList<Client>> OnlineClients;

        public static void ShowUserMissions(Client gm, string args)
        {
            if (gm?.Player == null)
                return;

            var target = Find(gm, args);
            if (target == null)
            {
                Say(gm, $"{args?.Trim()} is not in the world.");
                return;
            }

            if (!SendUserMissions(gm, target))
                Say(gm, $"{DisplayName(target)} has no missions to show.");
        }

        public static void ForceCompleteObjective(Client gm, ForceCompleteObjectivePacket packet)
        {
            if (gm?.Player == null || packet == null)
                return;

            if (!ChatCommandsManager.HasLevel(gm, Level))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {gm.AccountEntry?.Id} (level {gm.AccountEntry?.Level}) sent ForceCompleteObjective for character {packet.UserId}, mission {packet.MissionId}, objective {packet.ObjectiveId}, which needs {(byte)Level}");
                return;
            }

            var target = packet.UserId <= uint.MaxValue ? ByCharacterId((uint)packet.UserId) : null;
            if (target == null)
            {
                Say(gm, $"Character {packet.UserId} is not in the world.");
                return;
            }

            Complete(gm, target, packet.MissionId, packet.ObjectiveId);
            SendUserMissions(gm, target);
        }

        public static void CompleteObjective(Client gm, string[] parts)
        {
            if (gm?.Player == null)
                return;

            if (parts.Length < 3 || parts.Length > 4 ||
                !uint.TryParse(parts[1], out var missionId) || !uint.TryParse(parts[2], out var objectiveId))
            {
                Say(gm, "usage: .completeobjective missionId objectiveId [familyName | #characterId]");
                return;
            }

            var target = Find(gm, parts.Length == 4 ? parts[3] : null);
            if (target == null)
            {
                Say(gm, $"{parts[3]} is not in the world.");
                return;
            }

            Complete(gm, target, missionId, objectiveId);
        }

        /// <summary>/givemission [missionId]: the picker, or the mission for the GM themselves.</summary>
        public static void GiveMission(Client gm, string args)
        {
            if (gm?.Player == null)
                return;

            var text = args?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                ShowGiveMissionList(gm);
                return;
            }

            if (!uint.TryParse(text, out var missionId))
            {
                Say(gm, "usage: /givemission [missionId]");
                return;
            }

            Give(gm, gm, missionId);
        }

        /// <summary>.givemission [missionId [familyName | #characterId]].</summary>
        public static void GiveMission(Client gm, string[] parts)
        {
            if (gm?.Player == null)
                return;

            if (parts.Length == 1)
            {
                ShowGiveMissionList(gm);
                return;
            }

            if (parts.Length > 3 || !uint.TryParse(parts[1], out var missionId))
            {
                Say(gm, "usage: .givemission [missionId [familyName | #characterId]]");
                return;
            }

            var target = Find(gm, parts.Length == 3 ? parts[2] : null);
            if (target == null)
            {
                Say(gm, $"{parts[2]} is not in the world.");
                return;
            }

            Give(gm, target, missionId);
        }

        private static void ShowGiveMissionList(Client gm)
        {
            var missions = MissionApplication.Instance.GiveMissionList(gm.Player);
            if (missions.Count == 0)
            {
                Say(gm, "No operational missions are loaded.");
                return;
            }

            gm.CallMethod(SysEntity.ClientMethodId, new QAGiveMissionAckPacket(missions));
        }

        private static void Give(Client gm, Client target, uint missionId)
        {
            var by = $"GM {gm.Player.FamilyName} (account {gm.AccountEntry?.Id})";
            if (MissionApplication.Instance.TryGiveMission(target, missionId, by))
                Say(gm, $"Mission {missionId} given to {DisplayName(target)}.");
            else
                Say(gm, $"Mission {missionId} could not be given to {DisplayName(target)}: not an operational mission, already active, succeeded or completed, or their mission log is full.");
        }

        private static void Complete(Client gm, Client target, uint missionId, uint objectiveId)
        {
            var by = $"GM {gm.Player.FamilyName} (account {gm.AccountEntry?.Id})";
            if (MissionApplication.Instance.TryForceCompleteObjective(target, missionId, objectiveId, by))
                Say(gm, $"Mission {missionId} objective {objectiveId} completed for {DisplayName(target)}.");
            else
                Say(gm, $"Mission {missionId} objective {objectiveId} is not an incomplete objective of an active mission of {DisplayName(target)}.");
        }

        /// <summary>The player's log to the GM; false when there is nothing in it (the client opens nothing then).</summary>
        internal static bool SendUserMissions(Client gm, Client target)
        {
            var snapshot = MissionApplication.Instance.BuildStatusSnapshot(target.Player);
            if (snapshot.Count == 0)
                return false;

            gm.CallMethod(SysEntity.ClientMethodId,
                new GmShowUserMissionsAckPacket(target.Player.Id, DisplayName(target), snapshot));
            return true;
        }

        /// <summary>Nothing is the GM; "#id" a character id; anything else a family name.</summary>
        private static Client Find(Client gm, string args)
        {
            var name = args?.Trim();
            if (string.IsNullOrEmpty(name))
                return gm;

            if (name.StartsWith("#"))
                return uint.TryParse(name.Substring(1), out var characterId) ? ByCharacterId(characterId) : null;

            return Online().FirstOrDefault(client =>
                string.Equals(client.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static Client ByCharacterId(uint characterId) =>
            Online().FirstOrDefault(client => client.Player.Id == characterId);

        private static IEnumerable<Client> Online()
        {
            IReadOnlyList<Client> clients;
            if (OnlineClients != null)
                clients = OnlineClients();
            else
                lock (Server.Clients)
                    clients = Server.Clients.ToList();

            return clients.Where(client => client?.State == ClientState.Ingame && client.Player != null);
        }

        private static string DisplayName(Client client) =>
            $"{client.Player.Name} {client.Player.FamilyName}".Trim();

        private static void Say(Client gm, string text) =>
            CommunicatorManager.Instance.SystemMessage(gm, text);
    }
}
