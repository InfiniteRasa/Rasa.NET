using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Structures;
    using Structures.World;

    /// <summary>
    /// The pace the open world's spawn pools come back at once they have been cleared. A pool's row says how long it
    /// stays down (spawnpool.respown_time, in tenths of a second), and the rows say little of an MMO's time: the
    /// Wilderness's Bane snipers, mortars, Lightbenders, Machina and Predators, its three Pinhole Egg-Layers and
    /// Overseer Skeev two seconds; the battlefields' named bosses - Kennilaxx, Caretaker Mordra, the Overseers of the
    /// Ashen Desert and the Marshes - fifty. The original servers kept a named boss down five to ten minutes (the
    /// wiki: Kennilaxx "5-10 Minutes", Caretaker Mordra "approximately 10 minutes"), the rank and file a minute or
    /// two. So, on the battlefields (RespawnPaceConfig.Maps), with the section on:
    ///
    ///  - A pool that has been cleared - nothing of it alive, queued or on its way (SpawnPoolManager.Emptied) - stays
    ///    down at least as long as its kind asks (<see cref="KindOf(SpawnPool)"/>, <see cref="Floor"/>), drawn
    ///    between the kind's two numbers each time it goes down: the rank and file 60-90 s, elites 3-5 minutes, a
    ///    named boss 5-10 minutes, somebody to talk to half a minute. A row that says longer keeps its own time. The
    ///    pool's timer is started that far below zero (<see cref="Down"/>), so whatever puts a pool back on its feet
    ///    at once by setting its timer to its respawn time - a GM, a control point, a public encounter's reset -
    ///    still does.
    ///  - A pool that has lost some of its creatures and not all has what it lost set down again its respawn time
    ///    after the first loss (SpawnPoolManager.TopUp). Each kill makes that wait at least the pace of what was
    ///    killed (<see cref="Lost"/>): a camp's grunt is back in a minute or so, its commander, killed among
    ///    survivors, no sooner than an elite's time.
    ///  - A pool holding a creature some mission asks to be killed (<see cref="Kind.Mission"/>) keeps a mission's
    ///    pace whatever else it holds, so a second player on "Mama Miasma!" is not kept waiting a boss's time for
    ///    the egg-layers the first one killed.
    ///  - Not for a mission's or a scene's own pools, a public encounter's staged actor, escorts and followers, a
    ///    control point's garrison (which comes back together, ControlPoints.HoldsBack), a scripted pool, a private
    ///    or a squad's instance (which keep their own clocks), nor a map that is not listed.
    /// </summary>
    public static class RespawnPace
    {
        /// <summary>What a pool holds, as far as its pace goes: its strongest creature's. In order of precedence.</summary>
        public enum Kind : byte
        {
            Service,
            Trash,
            Elite,
            Boss,

            /// <summary>Something a mission asks to be killed: before every other kind.</summary>
            Mission
        }

        /// <summary>
        /// The fifteen battlefields: the maps the pace is kept on when RespawnPaceConfig.Maps names none. Not Bootcamp,
        /// whose bridge battle and Proving Grounds front are meant to come back at once, nor the wargames, nor the
        /// instances off a battlefield.
        /// </summary>
        public static readonly IReadOnlyCollection<uint> Battlefields = new HashSet<uint>
        {
            1148, // Concordia Divide
            1220, // Concordia Wilderness
            1244, // Concordia Palisades
            1304, // Valverde Pools
            1454, // Valverde Marshes
            1497, // Valverde Plateau
            1734, // Ligo Ashen Desert
            1759, // Torden Mires
            1761, // Torden Incline
            1764, // Torden Plains
            1911, // Ligo Thunderhead
            1993, // Ligo Burning Steps
            2028, // Torden Abyss
            2047, // Valverde Descent
            2051  // Howling Maw
        };

        /// <summary>A class the client names one of these is a boss's, flagged or not (Overseer Skeev's Miniboss).</summary>
        private static readonly string[] BossClassWords = { "_Boss", "Miniboss", "Superboss", "_Queen" };

        public static RespawnPaceConfig Config { get; set; } = new RespawnPaceConfig();

        private static RespawnPaceConfig Settings => Config ?? new RespawnPaceConfig();

        #region Kinds

        /// <summary>By map: how many of its loaded pools hold each creature row. Made when a map is first asked about.</summary>
        private static readonly Dictionary<uint, Dictionary<uint, int>> RowsInPools = new Dictionary<uint, Dictionary<uint, int>>();

        /// <summary>The creature rows and the entity classes some operational mission asks to be killed; read when first asked for.</summary>
        private static (HashSet<uint> Creatures, HashSet<uint> Classes)? _missionTargets;

        private static readonly object Lock = new object();

        /// <summary>
        /// The creature rows and the entity classes the missions ask to be killed (MissionProgressEventKind.CreatureKilled
        /// and CreatureClassKilled; a species' flag, "Kill 40 Xanx", names the rank and file and is left out). Kept as a
        /// call so that a test can stand in for the missions it has not loaded.
        /// </summary>
        internal static Func<(HashSet<uint> Creatures, HashSet<uint> Classes)> MissionTargets { get; set; } = LoadedMissionTargets;

        /// <summary>What the loaded missions (MissionApplication.LoadedMissions) ask to be killed; nothing when they cannot be read.</summary>
        internal static (HashSet<uint> Creatures, HashSet<uint> Classes) LoadedMissionTargets()
        {
            try
            {
                return TargetsOf(MissionApplication.Instance.LoadedMissions.Values);
            }
            catch (Exception exception)
            {
                Logger.WriteLog(LogType.Error, $"RespawnPace: the missions could not be read ({exception.Message}); no pool keeps a mission's pace.");
                return (new HashSet<uint>(), new HashSet<uint>());
            }
        }

        /// <summary>What the operational ones of <paramref name="missions"/> ask to be killed, from their objectives' progress rules.</summary>
        internal static (HashSet<uint> Creatures, HashSet<uint> Classes) TargetsOf(IEnumerable<Mission> missions)
        {
            var creatures = new HashSet<uint>();
            var classes = new HashSet<uint>();

            foreach (var mission in missions.Where(mission => mission != null && mission.IsOperational))
                foreach (var objective in mission.Objectives.Values)
                {
                    var rules = objective.ExecutableTransitions.Select(transition => transition.ProgressRule).Append(objective.ProgressRule);

                    foreach (var rule in rules.Where(rule => rule != null))
                    {
                        if (rule.Kind == MissionProgressEventKind.CreatureKilled)
                            creatures.UnionWith(rule.Subjects);
                        else if (rule.Kind == MissionProgressEventKind.CreatureClassKilled)
                            classes.UnionWith(rule.Subjects);
                    }
                }

            return (creatures, classes);
        }

        private static (HashSet<uint> Creatures, HashSet<uint> Classes) Targets()
        {
            lock (Lock)
                return _missionTargets ??= MissionTargets?.Invoke() ?? (new HashSet<uint>(), new HashSet<uint>());
        }

        /// <summary>
        /// What a creature is, for its pool's pace: something a mission asks to be killed; somebody to talk to (a friend
        /// with something to say, or with no attack); a boss by its flags (POWERLEVEL_6_BOSS, _7_SUPERBOSS, the
        /// UI_INDICATOR_ boss borders) or a class the client names a boss's (..._Boss, a Miniboss, a Queen) - a named
        /// boss when its row stands in one pool of its map alone, an elite when several camps share the row (the
        /// battlefields' Thrax Commanders); an elite by POWERLEVEL_4_LIEUTENANT; otherwise the rank and file.
        /// </summary>
        public static Kind KindOf(Creature template, uint mapContextId)
        {
            if (template == null)
                return Kind.Trash;

            var (creatures, classes) = Targets();

            if (creatures.Contains(template.DbId) || classes.Contains((uint)template.EntityClass))
                return Kind.Mission;

            if (template.TargetCategory == TargetCategory.Friendly && (template.Npc != null || template.Actions.Count == 0))
                return Kind.Service;

            var flagged = CreatureManager.HasFlag(template, CreatureFlag.Powerlevel6Boss) || CreatureManager.HasFlag(template, CreatureFlag.Powerlevel7Superboss)
                || CreatureManager.HasFlag(template, CreatureFlag.UiIndicatorBoss) || CreatureManager.HasFlag(template, CreatureFlag.UiIndicatorEpicBoss);
            var name = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(template.EntityClass, out var info) ? info?.ClassName ?? "" : "";

            if (flagged || BossClassWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                return PoolsHolding(mapContextId, template.DbId) <= 1 ? Kind.Boss : Kind.Elite;

            return CreatureManager.HasFlag(template, CreatureFlag.Powerlevel4Lieutenant) ? Kind.Elite : Kind.Trash;
        }

        /// <summary>A pool's kind: its strongest creature's, of those it may hold; the rank and file for a pool of none.</summary>
        public static Kind KindOf(SpawnPool pool)
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;
            Kind? kind = null;

            foreach (var slot in pool?.SpawnSlot ?? new List<SpawnPoolSlot>())
            {
                if (slot == null || slot.CountMax <= 0 || !creatures.TryGetValue(slot.CreatureId, out var template))
                    continue;

                var its = KindOf(template, pool.MapContextId);

                if (kind == null || its > kind)
                    kind = its;
            }

            return kind ?? Kind.Trash;
        }

        /// <summary>How many of a map's loaded pools hold a creature row (a count above 0).</summary>
        internal static int PoolsHolding(uint mapContextId, uint creatureId)
        {
            lock (Lock)
            {
                if (!RowsInPools.TryGetValue(mapContextId, out var rows))
                {
                    rows = new Dictionary<uint, int>();

                    foreach (var pool in SpawnPoolManager.Instance.LoadedSpawnPools.Values)
                    {
                        if (pool?.SpawnSlot == null || pool.MapContextId != mapContextId)
                            continue;

                        foreach (var id in pool.SpawnSlot.Where(s => s != null && s.CountMax > 0).Select(s => s.CreatureId).Distinct())
                            rows[id] = rows.TryGetValue(id, out var count) ? count + 1 : 1;
                    }

                    RowsInPools[mapContextId] = rows;
                }

                return rows.TryGetValue(creatureId, out var pools) ? pools : 0;
            }
        }

        /// <summary>What <see cref="PoolsHolding"/> and the missions' targets were found to be is found again when next asked for: after the pools or the missions have changed, and for tests.</summary>
        internal static void Forget()
        {
            lock (Lock)
            {
                RowsInPools.Clear();
                _missionTargets = null;
            }
        }

        /// <summary>The least and the most a pool of a kind stays down, in milliseconds (RespawnPaceConfig).</summary>
        public static (long Min, long Max) Floor(Kind kind)
        {
            var settings = Settings;
            var (min, max) = kind switch
            {
                Kind.Service => (settings.ServiceSecondsMin, settings.ServiceSecondsMax),
                Kind.Elite => (settings.EliteSecondsMin, settings.EliteSecondsMax),
                Kind.Boss => (settings.BossSecondsMin, settings.BossSecondsMax),
                Kind.Mission => (settings.MissionSecondsMin, settings.MissionSecondsMax),
                _ => (settings.TrashSecondsMin, settings.TrashSecondsMax)
            };

            min = Math.Max(0, min);

            return (min * 1000L, Math.Max(min, max) * 1000L);
        }

        /// <summary>A kind's floor drawn between its two numbers by <paramref name="roll"/> in [0, 1], in milliseconds.</summary>
        public static long Draw(Kind kind, double roll)
        {
            var (min, max) = Floor(kind);

            return min + (long)Math.Round(Math.Clamp(double.IsNaN(roll) ? 0 : roll, 0, 1) * (max - min));
        }

        #endregion

        #region Down

        /// <summary>Whether the pace is kept on a map channel: on, a listed map (the battlefields when none is), and neither a private instance nor a squad's.</summary>
        public static bool IsPaced(MapChannel map)
        {
            if (map?.MapInfo == null || map.IsPrivateInstance || map.IsSquadInstance || map.SquadState != null || !Settings.Enabled)
                return false;

            var id = map.MapInfo.MapContextId;
            var maps = Settings.Maps;

            return maps != null && maps.Count > 0 ? maps.Contains(id) : Battlefields.Contains(id);
        }

        /// <summary>
        /// Whether a pool keeps the pace: one of a paced map's (<see cref="IsPaced"/>) own, on its own timer - no
        /// garrison's, scripted pool's, mission's, scene's, escort's or public encounter's.
        /// </summary>
        public static bool Applies(SpawnPool pool)
        {
            if (pool == null || pool.Mode != SpawnPoolManager.ModeAutomatic || pool.IsGarrison || !IsPaced(pool.RuntimeMapChannel))
                return false;

            if (pool.ScenarioKey != null || pool.SceneRunId != null || pool.SpawnPolicy == MissionSpawnGroupPolicy.ScenarioControlled
                || pool.FollowOwnerCharacterId != 0 || pool.FollowTargetEntityId != 0)
                return false;

            return !SpawnPoolManager.Instance.IsStaged(pool.DbId);
        }

        /// <summary>
        /// The pool has just gone down - nothing of it alive, queued or on its way: how much longer than its row's
        /// time it stays down this time, in milliseconds - its kind's floor (<see cref="Draw"/>, by
        /// <paramref name="roll"/>) less the row's time; 0 for a pool that keeps no pace, or whose row says longer. How
        /// long it stays down in all is kept on it (SpawnPool.PaceDownMs), for a game master.
        /// </summary>
        public static long Down(SpawnPool pool, double roll)
        {
            if (pool == null)
                return 0;

            if (!Applies(pool))
            {
                pool.PaceDownMs = 0;
                return 0;
            }

            var kind = KindOf(pool);
            var floor = Draw(kind, roll);

            pool.PaceKind = kind;
            pool.PaceDownMs = Math.Max(pool.RespawnTime, floor);

            Logger.WriteLog(LogType.Debug, $"SpawnPool {pool.DbId} is down ({kind}): back in {pool.PaceDownMs / 1000.0:0.#} s; its row says {pool.RespawnTime / 1000.0:0.#} s.");

            return Math.Max(0, floor - pool.RespawnTime);
        }

        public static long Down(SpawnPool pool) => Down(pool, Random.Shared.NextDouble());

        /// <summary>
        /// One of a pool's creatures has been killed and some of it still stand (SpawnPoolManager.DecreaseAliveCreatureCount
        /// has started or kept the top-up's time): what it lost is set down again no sooner than the pace of what was
        /// killed - the pool's timer is put that far below its respawn time when it is not already further. Nothing for
        /// a pool that keeps no pace.
        /// </summary>
        public static void Lost(SpawnPool pool, Creature killed, double roll)
        {
            if (pool == null || killed == null || !Applies(pool))
                return;

            var floor = Draw(KindOf(killed, pool.MapContextId), roll);

            if (floor > pool.RespawnTime)
                pool.UpdateTimer = Math.Min(pool.UpdateTimer, pool.RespawnTime - floor);
        }

        public static void Lost(SpawnPool pool, Creature killed) => Lost(pool, killed, Random.Shared.NextDouble());

        #endregion

        #region For a game master

        /// <summary>What a pool is and when it comes back, for a game master (.respawn): a line or two.</summary>
        public static List<string> Describe(SpawnPool pool)
        {
            var lines = new List<string>();

            if (pool == null)
                return lines;

            var kind = KindOf(pool);
            var (min, max) = Floor(kind);
            var pace = Applies(pool) ? $"the pace {min / 1000}-{max / 1000} s" : "keeps no pace here";

            lines.Add($"pool {pool.DbId} ({kind}, {pool.SpawnSlot?.Sum(s => s?.CountMax ?? 0) ?? 0} at most): its row {pool.RespawnTime / 1000.0:0.#} s, {pace}; " +
                      $"alive {pool.AliveCreatures}, coming {pool.QueuedCreatures} in {pool.DropshipQueue} ship(s), corpses {pool.DeadCreatures}.");

            var left = Math.Max(0, pool.RespawnTime - pool.UpdateTimer);

            if (pool.AliveCreatures + pool.QueuedCreatures + pool.DropshipQueue > 0)
                lines.Add(left > 0 ? $"  up, short of some: the rest back in {left / 1000.0:0} s." : "  up.");
            else
                lines.Add($"  down: back in {left / 1000.0:0} s{(pool.PaceDownMs > 0 ? $" (down {pool.PaceDownMs / 1000.0:0} s this time, as {pool.PaceKind})" : "")}.");

            return lines;
        }

        #endregion
    }
}
