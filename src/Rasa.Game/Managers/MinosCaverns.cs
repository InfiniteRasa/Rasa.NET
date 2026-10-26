using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// Minos Caverns' own story, in each copy of it: the two captives of Data Thieves (383), Recon
    /// Officer Karik and Ranger Zola, held in the Bane base's prison.
    ///
    /// Their rescue is the mission's second objective, Karik's conversation ("if you can lead us
    /// to the entrance to this place, we can find our way out of the caves"). A player who has it
    /// done and stands near them takes them with them: both follow that player as a mission's
    /// escorts do (SpawnPool.FollowOwnerCharacterId; BehaviorManager), through the base and the
    /// caverns on the navmesh, fighting nothing. At the entrance - the captive within
    /// <see cref="LeaveRadius"/> of where a player arrives - they leave: the escort icon goes, and
    /// they stop where they are. A posed NPC walks back to its post and takes its pose up again
    /// on its own (NpcPoses; BehaviorManager.TakePost), so they go back to the prison for the
    /// next squad. They go back the same way if their player dies, leaves the map, or gets more
    /// than <see cref="LostRadius"/> from them, or if one of them dies (its pool brings it back to
    /// the prison, no longer following anyone).
    ///
    /// A player walks them out once per copy of the caverns: the client's objective 6
    /// ('girls_leave') is this, not a thing of the journal.
    /// </summary>
    public static class MinosCaverns
    {
        public const uint Map = 1347;
        public const uint DataThieves = 383;
        public const uint RescueObjective = 2;

        /// <summary>Karik's and Zola's creature rows and spawn pools (MinosCavernsContent).</summary>
        public const uint Karik = 568011, Zola = 568012;
        public const uint KarikPool = 568042, ZolaPool = 568043;

        /// <summary>Where a player arrives from the Divide.</summary>
        public static readonly Vector3 Entrance = new(-15.8f, 29.5f, 122.6f);

        /// <summary>How near a captive must be to a player who freed them to go with them.</summary>
        public const float TakeAlongRadius = 12f;

        /// <summary>A captive this near the entrance has been led out.</summary>
        public const float LeaveRadius = 20f;

        /// <summary>A captive this far from the player it follows has lost them.</summary>
        public const float LostRadius = 60f;

        /// <summary>How often the copy is looked at.</summary>
        public const long EveryMs = 1000;

        public sealed class State
        {
            public long NextAt;

            /// <summary>Which player each captive follows, by creature: none when at home.</summary>
            public readonly Dictionary<Creature, uint> Following = new();

            /// <summary>The players who have had the captives walk out with them.</summary>
            public readonly HashSet<uint> LedOut = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsMinos(MapChannel map) => map?.MapInfo?.MapContextId == Map;

        public static State StateOf(MapChannel map) => IsMinos(map) ? States.GetValue(map, _ => new State()) : null;

        public static void Worker(MapChannel map)
        {
            if (!IsMinos(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (now < state.NextAt)
                return;

            state.NextAt = now + EveryMs;

            var captives = Captives(map);
            var members = Members(map);

            // Followers first: the ones whose player is gone, or who have reached the entrance, leave.
            foreach (var captive in state.Following.Keys.ToList())
            {
                var owner = members.FirstOrDefault(client => client.Player.Id == state.Following[captive]);

                if (!captives.Contains(captive))
                {
                    // Dead, or respawned as another creature: its pool follows nobody now.
                    state.Following.Remove(captive);
                    if (captive.SpawnPool != null && captive.SpawnPool.FollowOwnerCharacterId == owner?.Player.Id)
                        captive.SpawnPool.FollowOwnerCharacterId = 0;
                    continue;
                }

                if (owner == null || Vector3.Distance(owner.Player.Position, captive.Position) > LostRadius)
                {
                    Leave(map, state, captive, "lost their player");
                    continue;
                }

                if (Vector3.Distance(captive.Position, Entrance) <= LeaveRadius)
                {
                    state.LedOut.Add(owner.Player.Id);
                    Leave(map, state, captive, "led out");
                }
            }

            // The ones at home go with a player who has freed them and stands by them.
            foreach (var captive in captives.Where(captive => !state.Following.ContainsKey(captive)))
            {
                var rescuer = members.FirstOrDefault(client => Freed(client.Player) && !state.LedOut.Contains(client.Player.Id) &&
                    Vector3.Distance(client.Player.Position, captive.Position) <= TakeAlongRadius);

                if (rescuer != null)
                    Follow(map, state, captive, rescuer);
            }
        }

        /// <summary>Whether the player has Data Thieves with the rescue done.</summary>
        public static bool Freed(Manifestation player) =>
            player != null && player.Missions.TryGetValue(DataThieves, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(RescueObjective, out var objective) && objective.State == MissionObjectiveState.Completed;

        private static void Follow(MapChannel map, State state, Creature captive, Client rescuer)
        {
            if (captive.SpawnPool == null)
                return;

            state.Following[captive] = rescuer.Player.Id;
            captive.SpawnPool.FollowOwnerCharacterId = rescuer.Player.Id;
            captive.SpawnPool.ScenarioOwnerCharacterId = rescuer.Player.Id;
            captive.SpawnPool.ScenarioMissionId = DataThieves;
            NpcPoses.Drop(map, captive);
            BehaviorManager.Instance.SetActionFollow(captive, rescuer.Player.EntityId);
            CreatureManager.PublishEscortStatus(map, captive, true);
            Logger.WriteLog(LogType.Debug, $"Minos Caverns {map.InstanceId}: {captive.Name} follows character {rescuer.Player.Id}.");
        }

        private static void Leave(MapChannel map, State state, Creature captive, string why)
        {
            CreatureManager.PublishEscortStatus(map, captive, false);
            state.Following.Remove(captive);

            if (captive.SpawnPool != null)
            {
                captive.SpawnPool.FollowOwnerCharacterId = 0;
                captive.SpawnPool.ScenarioOwnerCharacterId = 0;
                captive.SpawnPool.ScenarioMissionId = 0;
            }

            // Idle where it stands: a posed NPC walks back to its post from here on its own.
            BehaviorManager.StartWandering(captive, false);
            Logger.WriteLog(LogType.Debug, $"Minos Caverns {map.InstanceId}: {captive.Name} {why} at {captive.Position}.");
        }

        /// <summary>Karik and Zola, alive and in this copy.</summary>
        public static List<Creature> Captives(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.DbId is Karik or Zola && Alive(creature)).ToList();

        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();
    }
}
