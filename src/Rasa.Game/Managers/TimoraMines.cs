using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Structures;

    using T = Services.Preloader.TimoraMinesContent;
    using M = Services.Preloader.Missions.Timora.TimoraMinesMissionsV1;
    using E = Services.Preloader.Missions.Timora.TimoraMinesMissionsV1.Events;
    using S = Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;

    /// <summary>
    /// What Timora Mines does, in each copy of it (map 1348, a squad instance), on what
    /// TimoraMinesContent puts down, for the missions TimoraMinesMissionsV1 authors:
    ///
    ///  - The drills. Each stands beside its charge, which takes a detonator; going off, the charge
    ///    destroys the drill within its blast. A drill down stays down in the copy, and counts for
    ///    Disable Production in the Mine for every member on it. So does each of the nine fuel
    ///    pumps shot down.
    ///  - The Main Fuel Pump and its three fuel lakes. A lake shot burns for
    ///    <see cref="BurnMs"/>; the burning lakes heat the pump, and <see cref="HeatToIgnite"/>
    ///    lake-milliseconds of it in one burn - two lakes or three together, never one - set it
    ///    off. It stays down and counts for Fuel Lake Sabotage. Nothing burning, the heat is gone.
    ///  - The security system. The first lake lit while it has not been disarmed engages it: a
    ///    force field over every lake puts out what burns and smothers whatever is lit under it,
    ///    and the three consoles in the Command Center light up. A lit console used switches its
    ///    lake's field off; all three off disarm the system for good in the copy.
    ///  - The pump's blast calls an ambush at the basin.
    ///  - Kearney's escort (Central Bound Patrol). Whoever has the escort to do calls four AFS
    ///    soldiers out at the camp, who follow them; one of them within <see cref="ArrivalRadius"/>
    ///    of Field Ranger Sanchez is the escort done, for everyone on it. All four dead first fails
    ///    it. One escort at a time in a copy.
    ///  - The Eloh Shelter's ambush (Mining for Information). Whoever has the ambush to beat sets
    ///    off its first wave; each wave dead calls the next; the third dead is the shelter
    ///    defended, for everyone on it. The mission's own clock fails it; no one left on it, the
    ///    ambush stops.
    ///  - Stolen History's pieces: the Map Cipher's crate here and the Galactic Map's in Minos
    ///    Caverns open only to whoever is to find that piece.
    ///
    /// What the world has done goes to every member waiting on it once a second, so a member who
    /// takes the mission up later is caught up. Kept by the copy and not saved.
    /// </summary>
    public static class TimoraMines
    {
        public enum Security { Idle, Engaged, Disarmed }

        public enum AmbushPhase { Idle, Running }

        /// <summary>How long a lit fuel lake burns.</summary>
        public const long BurnMs = 45_000;

        /// <summary>The heat that sets the Main Fuel Pump off: lake-milliseconds of burning in one burn.</summary>
        public const long HeatToIgnite = 75_000;

        /// <summary>How near Field Ranger Sanchez a soldier of the escort is brought to him.</summary>
        public const float ArrivalRadius = 12f;

        /// <summary>Further than this from the one they follow, or where they cannot walk to them, the escort is put beside them.</summary>
        public const float EscortWarpDistance = 40f;

        /// <summary>How often what the world has done goes to the members waiting on it.</summary>
        public const long CatchUpMs = 1000;

        public sealed class State
        {
            internal bool Set;
            public HashSet<uint> DrillsDown { get; } = new();
            public HashSet<uint> PumpsDown { get; } = new();

            public Security Security { get; internal set; }
            public bool[] ConsoleLit { get; } = new bool[T.Lakes.Length];
            public long[] BurnsUntil { get; } = new long[T.Lakes.Length];
            public long Heat { get; internal set; }
            public bool MainPumpDown { get; internal set; }
            internal long LastBurnAt;
            internal ulong LastIgniter;

            public uint EscortFor { get; internal set; }
            internal bool EscortSeen;
            public int EscortsDelivered { get; internal set; }

            public AmbushPhase Ambush { get; internal set; }
            public uint Wave { get; internal set; }
            internal bool WaveSeen;
            public int AmbushesDefended { get; internal set; }

            internal readonly ulong[] ShotBy = new ulong[T.Lakes.Length];
            internal readonly HashSet<uint> ChargesBlown = new();
            internal readonly HashSet<uint> LakesDown = new();
            internal readonly HashSet<uint> OneShot = new();
            internal readonly HashSet<string> Told = new();
            internal long NextCatchUpAt;
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsMines(MapChannel map) => map?.MapInfo?.MapContextId == T.Map;

        public static State StateOf(MapChannel map) => IsMines(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>Every copy forgotten (tests).</summary>
        public static void Reset() => States.Clear();

        #region The worker

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var state = StateOf(map);

            if (state == null)
                return;

            var now = Now();
            state.Set = true;

            var members = Members(map);

            Charges(map, state);
            Destroyed(map, state);
            Lakes(map, state, now);
            Escort(map, state, members);
            Ambush(map, state, members);
            OneShots(map, state);

            if (now >= state.NextCatchUpAt)
            {
                state.NextCatchUpAt = now + CatchUpMs;
                CatchUp(state, members);
            }
        }

        #endregion

        #region The drills and the pumps

        /// <summary>A charge gone off: its drill, within its blast, destroyed by whoever set it.</summary>
        private static void Charges(MapChannel map, State state)
        {
            foreach (var drill in T.Drills)
            {
                var charge = PlacedUsables.LiveOf(map, T.ChargeId(drill.Number));

                if (charge == null)
                    continue;

                if (charge.State != UseObjectState.BombStateDetonated)
                {
                    state.ChargesBlown.Remove(drill.Number);
                    continue;
                }

                if (!state.ChargesBlown.Add(drill.Number))
                    continue;

                var by = charge.ArmedBy != 0 && EntityManager.Instance.Players.TryGetValue(charge.ArmedBy, out var player) ? player : null;
                var live = PlacedDestructibles.LiveOf(map, T.DrillId(drill.Number));

                if (live != null && !live.IsDown && Vector3.Distance(live.Object.Position, charge.Object.Position) <= charge.Placement.Radius)
                    PlacedDestructibles.Destroy(live, by);
            }
        }

        /// <summary>The drills and pumps down: kept down in the copy, and remembered for the members on them.</summary>
        private static void Destroyed(MapChannel map, State state)
        {
            foreach (var drill in T.Drills)
                if (PlacedDestructibles.LiveOf(map, T.DrillId(drill.Number)) is { IsDown: true } live)
                {
                    live.RespawnAt = long.MaxValue;
                    if (state.DrillsDown.Add(drill.Number))
                        Tell(map, $"A Bane mining drill is destroyed ({state.DrillsDown.Count} of {T.Drills.Length}).");
                }

            for (uint pump = 1; pump <= T.PumpHouses.Length; pump++)
                if (PlacedDestructibles.LiveOf(map, T.PumpHouseId(pump)) is { IsDown: true } live)
                {
                    live.RespawnAt = long.MaxValue;
                    state.PumpsDown.Add(pump);
                }
        }

        #endregion

        #region The Main Fuel Pump, its lakes and its security

        private static void Lakes(MapChannel map, State state, long now)
        {
            var pump = PlacedDestructibles.LiveOf(map, T.MainPump);

            if (pump == null)
                return;

            if (pump.IsDown)
            {
                pump.RespawnAt = long.MaxValue;
                state.MainPumpDown = true;
            }

            foreach (var lake in T.Lakes)
            {
                var index = (int)lake.Number - 1;
                var fuel = PlacedDestructibles.LiveOf(map, T.FuelId(lake.Number));

                if (fuel == null)
                    continue;

                if (!fuel.IsDown)
                {
                    state.LakesDown.Remove(lake.Number);
                    continue;
                }

                // Its own clock: back when its burn is over, or at once when it is smothered.
                fuel.RespawnAt = long.MaxValue;

                if (state.LakesDown.Add(lake.Number))
                    Lit(map, state, lake, now, state.ShotBy[index]);
                else if (state.BurnsUntil[index] != 0 && now >= state.BurnsUntil[index])
                    PutOut(map, state, lake);
            }

            if (state.MainPumpDown)
                return;

            // The heat of what burns since the last look; nothing burning, none of it is left.
            var burning = state.BurnsUntil.Count(until => until > now);

            if (burning == 0)
                state.Heat = 0;
            else if (state.LastBurnAt != 0)
                state.Heat += burning * Math.Max(0, now - state.LastBurnAt);

            state.LastBurnAt = burning == 0 ? 0 : now;

            if (state.Heat >= HeatToIgnite)
                Ignite(map, state, pump);
        }

        /// <summary>One of the copy's rows destroyed (PlacedDestructibles), by whom: a lake's igniter is remembered for the pump.</summary>
        public static void Destroyed(MapChannel map, uint rowId, Actor by)
        {
            if (StateOf(map) is not { } state)
                return;

            foreach (var lake in T.Lakes)
                if (rowId == T.FuelId(lake.Number))
                    state.ShotBy[lake.Number - 1] = by is Manifestation player ? player.EntityId : 0;
        }

        /// <summary>A lake shot down: alight, or - security engaging or its field up - smothered.</summary>
        private static void Lit(MapChannel map, State state, T.Lake lake, long now, ulong by)
        {
            var index = (int)lake.Number - 1;

            if (state.MainPumpDown)
            {
                PutOut(map, state, lake);
                return;
            }

            if (state.Security == Security.Idle)
            {
                Engage(map, state);
                return;
            }

            if (state.Security == Security.Engaged && state.ConsoleLit[index])
            {
                PutOut(map, state, lake);
                return;
            }

            state.BurnsUntil[index] = now + BurnMs;
            state.LastIgniter = by;
            SetUsable(map, T.FireId(lake.Number), UseObjectState.TsState1);
        }

        /// <summary>The lake back, its fire out.</summary>
        private static void PutOut(MapChannel map, State state, T.Lake lake)
        {
            state.BurnsUntil[lake.Number - 1] = 0;
            state.LakesDown.Remove(lake.Number);
            SetUsable(map, T.FireId(lake.Number), UseObjectState.TsState0);

            if (PlacedDestructibles.LiveOf(map, T.FuelId(lake.Number)) is { IsDown: true } fuel)
                PlacedDestructibles.Respawn(fuel);
        }

        /// <summary>The security system engaged (or a GM's .timora security): the fields up, the fires out, the consoles lit.</summary>
        public static void Engage(MapChannel map, State state)
        {
            if (state.Security != Security.Idle)
                return;

            state.Security = Security.Engaged;
            state.Heat = 0;

            foreach (var lake in T.Lakes)
            {
                state.ConsoleLit[lake.Number - 1] = true;
                SetUsable(map, T.ConsoleId(lake.Number), UseObjectState.TsState1);
                SetUsable(map, T.FieldId(lake.Number), (UseObjectState)T.FieldUp);
                PutOut(map, state, lake);
            }

            Tell(map, "Timora security engaged: force fields seal the fuel lakes. Their consoles are in the Timora Command Center.");
        }

        /// <summary>A lit console switched off: its lake's field down, and all three off the system disarmed.</summary>
        private static void SwitchOff(MapChannel map, State state, T.Lake lake)
        {
            state.ConsoleLit[lake.Number - 1] = false;
            SetUsable(map, T.FieldId(lake.Number), (UseObjectState)T.FieldDown);

            if (state.ConsoleLit.Any(lit => lit))
            {
                Tell(map, $"The {lake.Where} fuel lake's force field is down.");
                return;
            }

            state.Security = Security.Disarmed;
            Tell(map, "Timora security is disarmed: the fuel lakes are open.");
        }

        /// <summary>The heat enough (or a GM's .timora pump): the pump goes up, and its ambush comes.</summary>
        public static void Ignite(MapChannel map, State state, PlacedDestructibles.Live pump)
        {
            if (pump == null || state.MainPumpDown)
                return;

            var by = state.LastIgniter != 0 && EntityManager.Instance.Players.TryGetValue(state.LastIgniter, out var player) ? player : null;

            PlacedDestructibles.Destroy(pump, by);
            pump.RespawnAt = long.MaxValue;
            state.MainPumpDown = true;
            state.Heat = 0;

            foreach (var lake in T.Lakes)
                PutOut(map, state, lake);

            WakeOnce(map, state, T.PumpAmbushPool);
            WakeOnce(map, state, T.PumpAmbushPool + 1);
            Tell(map, "The Main Fuel Pump goes up in flames! Bane security converges on the basin.");
        }

        #endregion

        #region Kearney's escort

        private static void Escort(MapChannel map, State state, List<Client> members)
        {
            var pool = Pool(map, T.EscortPool);

            if (pool == null)
                return;

            var soldiers = Creatures(map).Where(creature => ReferenceEquals(creature.SpawnPool, pool)).ToList();
            var owner = members.FirstOrDefault(client => client.Player.Id == state.EscortFor);

            if (owner == null || !Wants(owner.Player, M.CentralBound, 1))
            {
                if (state.EscortFor != 0)
                    Release(pool);

                state.EscortFor = 0;
                state.EscortSeen = false;

                owner = members.FirstOrDefault(client => Wants(client.Player, M.CentralBound, 1));

                if (owner == null)
                    return;

                state.EscortFor = owner.Player.Id;
                pool.ScenarioMissionId = M.CentralBound;
                pool.ScenarioOwnerCharacterId = owner.Player.Id;
                pool.FollowOwnerCharacterId = owner.Player.Id;

                if (soldiers.Count == 0)
                    WakeOnce(map, state, T.EscortPool);

                Logger.WriteLog(LogType.Debug, $"Timora {map.InstanceId}: Kearney's escort goes with {owner.Player.FamilyName}.");
            }

            if (soldiers.Count > 0)
            {
                if (!state.EscortSeen)
                    foreach (var soldier in soldiers)
                        CreatureManager.PublishEscortStatus(map, soldier, true);

                state.EscortSeen = true;
            }
            else if (state.EscortSeen && pool.QueuedCreatures == 0 && pool.DropshipQueue == 0)
            {
                // All of them dead before Sanchez: the escort is lost.
                foreach (var client in members.Where(client => Wants(client.Player, M.CentralBound, 1)))
                    Emit(client, M.CentralBound, E.Escort, E.Lost, once: true, state);

                Tell(map, "Kearney's escort is lost.");
                Release(pool);
                state.EscortFor = 0;
                state.EscortSeen = false;
                return;
            }

            var sanchez = Creatures(map).FirstOrDefault(creature => creature.DbId == T.Sanchez)?.Position ?? T.SanchezAt;

            if (soldiers.Any(soldier => Vector3.Distance(soldier.Position, sanchez) <= ArrivalRadius))
            {
                foreach (var client in members.Where(client => Wants(client.Player, M.CentralBound, 1)))
                    Emit(client, M.CentralBound, E.Escort, E.Arrived, once: true, state);

                Release(pool);

                foreach (var soldier in soldiers)
                    BehaviorManager.Instance.SetActionAnchor(soldier, soldier.Position);

                state.EscortFor = 0;
                state.EscortSeen = false;
                state.EscortsDelivered++;
                return;
            }

            // Too far behind, or where they cannot walk: put beside the one they follow.
            var to = owner.Player.Position;

            foreach (var soldier in soldiers)
            {
                if (Vector3.Distance(soldier.Position, to) <= EscortWarpDistance && map.NavMesh?.Connected(soldier.Position, to) != false)
                    continue;

                var beside = NavMeshManager.RandomPointAround(map, to, 3f) is { } near && Vector3.Distance(near, to) <= 5f ? near : to;
                beside = NavMeshManager.SnapToGround(map, beside);

                if (BehaviorManager.Instance.RestoreScriptedPose(map, soldier, beside, Math.Atan2(to.X - beside.X, to.Z - beside.Z)))
                {
                    soldier.Controller.ScriptedMove = null;
                    soldier.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
                }
            }
        }

        private static void Release(SpawnPool pool)
        {
            pool.FollowOwnerCharacterId = 0;
            pool.ScenarioOwnerCharacterId = 0;
            pool.ScenarioMissionId = 0;
        }

        #endregion

        #region The Eloh Shelter's ambush

        private static void Ambush(MapChannel map, State state, List<Client> members)
        {
            var waiting = members.Where(client => Wants(client.Player, M.MiningForInformation, 2)).ToList();

            if (state.Ambush == AmbushPhase.Idle)
            {
                if (waiting.Count == 0)
                    return;

                state.Ambush = AmbushPhase.Running;
                CallWave(map, state, 1);
                Tell(map, "Ambush! The Bane are coming for the Eloh Shelter.");
                return;
            }

            if (waiting.Count == 0)
            {
                // The clock ran out on everyone, or they gave it up: the waves stop.
                StopAmbush(map, state);
                return;
            }

            var pool = Pool(map, T.AmbushPool(state.Wave));

            if (pool == null)
            {
                StopAmbush(map, state);
                return;
            }

            if (pool.AliveCreatures > 0)
                state.WaveSeen = true;

            if (pool.AliveCreatures > 0 || pool.QueuedCreatures > 0 || pool.DropshipQueue > 0 || !(state.WaveSeen || pool.HasSpawned))
                return;

            if (state.Wave < T.AmbushWaves)
            {
                CallWave(map, state, state.Wave + 1);
                Tell(map, $"Another wave of Bane comes for the Eloh Shelter ({state.Wave} of {T.AmbushWaves}).");
                return;
            }

            foreach (var client in waiting)
                Emit(client, M.MiningForInformation, E.Ambush, E.Defended, once: true, state);

            state.AmbushesDefended++;
            Tell(map, "The Eloh Shelter is held.");
            StopAmbush(map, state);
        }

        private static void CallWave(MapChannel map, State state, uint wave)
        {
            state.Wave = wave;
            state.WaveSeen = false;

            if (Pool(map, T.AmbushPool(wave)) is { } pool)
                pool.HasSpawned = false;

            WakeOnce(map, state, T.AmbushPool(wave));
        }

        private static void StopAmbush(MapChannel map, State state)
        {
            if (state.Wave != 0 && Pool(map, T.AmbushPool(state.Wave)) is { } pool)
            {
                pool.Woken = false;
                state.OneShot.Remove(pool.DbId);
            }

            state.Ambush = AmbushPhase.Idle;
            state.Wave = 0;
            state.WaveSeen = false;
        }

        #endregion

        #region Catching up

        private static void CatchUp(State state, List<Client> members)
        {
            foreach (var client in members)
            {
                var player = client.Player;

                if (Wants(player, M.DisableProduction, 1))
                {
                    Detonators(client, state);

                    foreach (var drill in state.DrillsDown)
                        Emit(client, M.DisableProduction, E.Drills, drill, once: true, state);
                }

                if (Wants(player, M.DisableProduction, 2))
                    foreach (var pump in state.PumpsDown)
                        Emit(client, M.DisableProduction, E.Pumps, pump, once: true, state);

                if (state.MainPumpDown && Wants(player, M.FuelLake, 1))
                    Emit(client, M.FuelLake, E.MainPump, E.PumpDestroyed, once: true, state);
            }
        }

        /// <summary>
        /// Morrow's detonators to whoever has the drills to blow and none, once for their
        /// assignment. Given as ordinary items rather than issued as the mission's: a charge takes
        /// a detonator out of the pack (PlacedUsables), and a mission's own item is not taken so.
        /// </summary>
        private static void Detonators(Client client, State state)
        {
            var player = client.Player;

            if (!player.Missions.TryGetValue(M.DisableProduction, out var log) ||
                InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)T.DetonatorClass, includeMissionItems: true) > 0)
                return;

            var key = $"{player.Id}:{log.AssignmentId}:detonators";

            if (state.Told.Contains(key) || ItemManager.Instance.CreateFromTemplateId(T.DetonatorTemplate, M.Detonators) is not { } item)
                return;

            if (InventoryManager.Instance.GrantItemToInventory(client, item) == null)
            {
                EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                CommunicatorManager.ShowPlayerMessage(client, PlayerMessage.PmInventoryFull);
                return;
            }

            state.Told.Add(key);
            CommunicatorManager.Instance.SystemMessage(client, $"Field Ranger Morrow's detonators: {M.Detonators}, one for each drill's charge and a spare.");
        }

        #endregion

        #region Usables

        /// <summary>
        /// Whether a player may use one of the mines' usables now (PlacedUsables.RefusalOf) - or one
        /// of Stolen History's crates, here or in Minos Caverns: null if they may, or why not.
        /// </summary>
        public static PlayerMessage? RefusalOf(Client client, PlacedUsables.Live live)
        {
            var player = client?.Player;
            var placement = live?.Placement;

            if (player == null || placement == null || placement.IsPutDown)
                return null;

            if (placement.MapContextId == M.MinosMap && placement.Id == M.MinosMapChest)
                return Wants(player, S.StolenHistory, 1) ? null : PlayerMessage.PmUseObjectNotUsable;

            if (placement.MapContextId != T.Map)
                return null;

            if (placement.Id == T.CipherCrate)
                return Wants(player, S.StolenHistory, 2) ? null : PlayerMessage.PmUseObjectNotUsable;

            var state = StateOf(live.Map);

            if (state == null)
                return null;

            foreach (var lake in T.Lakes)
                if (placement.Id == T.ConsoleId(lake.Number))
                    return state.ConsoleLit[lake.Number - 1] ? null : PlayerMessage.PmUseObjectNotUsable;

            foreach (var drill in T.Drills)
                if (placement.Id == T.ChargeId(drill.Number) && live.State != UseObjectState.BombStateArmed &&
                    (state.DrillsDown.Contains(drill.Number) || PlacedDestructibles.LiveOf(live.Map, T.DrillId(drill.Number)) is { IsDown: true }))
                    return PlayerMessage.PmUseObjectNotUsable;

            return null;
        }

        /// <summary>A player has used one of the mines' usables (PlacedUsables.UseRecovery), to <paramref name="next"/>.</summary>
        public static void Used(Client client, PlacedUsables.Live live, UseObjectState next)
        {
            var state = StateOf(live?.Map);

            if (state == null || client?.Player == null)
                return;

            foreach (var lake in T.Lakes)
                if (live.Placement.Id == T.ConsoleId(lake.Number) && next == UseObjectState.TsState0 && state.ConsoleLit[lake.Number - 1])
                    SwitchOff(live.Map, state, lake);
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead &&
                    client.Player.State != CharacterState.Dying).ToList()
            ?? new List<Client>();

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        private static SpawnPool Pool(MapChannel map, uint id) => map.SpawnPools.FirstOrDefault(pool => pool.DbId == id);

        /// <summary>A scripted pool set going until it has put its creatures down (OneShots).</summary>
        private static void WakeOnce(MapChannel map, State state, uint id)
        {
            if (Pool(map, id) is not { } pool || pool.AliveCreatures > 0 || pool.QueuedCreatures > 0)
                return;

            pool.Woken = true;
            state.OneShot.Add(id);
        }

        private static void OneShots(MapChannel map, State state)
        {
            foreach (var id in state.OneShot.ToList())
                if (Pool(map, id) is not { } pool)
                    state.OneShot.Remove(id);
                else if (pool.AliveCreatures > 0 || pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                {
                    pool.Woken = false;
                    state.OneShot.Remove(id);
                }
        }

        private static void SetUsable(MapChannel map, uint id, UseObjectState state)
        {
            if (PlacedUsables.LiveOf(map, id) is { } live && live.State != state)
                PlacedUsables.SetState(live, state, 0);
        }

        private static void Tell(MapChannel map, string text)
        {
            foreach (var client in Members(map))
                CommunicatorManager.Instance.SystemMessage(client, text);
        }

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>A scenario event to the player's mission; with <paramref name="once"/>, not again for the same assignment.</summary>
        private static void Emit(Client client, uint missionId, uint scenarioId, uint eventId, bool once, State state)
        {
            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = $"{player.Id}:{log.AssignmentId}:{missionId}:{scenarioId}:{eventId}";

            if (once && state.Told.Contains(key))
                return;

            if (MissionApplication.Instance?.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId) == true)
                state.Told.Add(key);
        }

        #endregion

        #region A GM's

        /// <summary>
        /// .timora: what this copy has done, and a GM's hand in it.
        ///   .timora                - the drills, the pumps, the security, the lakes, the escort, the ambush
        ///   .timora drills         - every drill destroyed
        ///   .timora pumps          - every fuel pump destroyed
        ///   .timora security       - the security system engaged
        ///   .timora disarm         - every console switched off
        ///   .timora burn           - every open lake lit
        ///   .timora pump           - the Main Fuel Pump set off
        ///   .timora reset          - the copy as it was
        /// </summary>
        public static IEnumerable<string> Command(MapChannel map, Manifestation gm, string[] parts)
        {
            var state = StateOf(map);

            if (state == null)
            {
                yield return "Not in Timora Mines.";
                yield break;
            }

            var now = Now();
            var sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

            switch (sub)
            {
                case "drills":
                    foreach (var drill in T.Drills)
                        PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, T.DrillId(drill.Number)), gm);
                    Destroyed(map, state);
                    break;

                case "pumps":
                    for (uint pump = 1; pump <= T.PumpHouses.Length; pump++)
                        PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, T.PumpHouseId(pump)), gm);
                    Destroyed(map, state);
                    break;

                case "security":
                    Engage(map, state);
                    break;

                case "disarm":
                    foreach (var lake in T.Lakes.Where(lake => state.ConsoleLit[lake.Number - 1]))
                        SwitchOff(map, state, lake);
                    if (state.Security == Security.Idle)
                        state.Security = Security.Disarmed;
                    break;

                case "burn":
                    foreach (var lake in T.Lakes)
                        PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, T.FuelId(lake.Number)), gm);
                    Lakes(map, state, now);
                    break;

                case "pump":
                    Ignite(map, state, PlacedDestructibles.LiveOf(map, T.MainPump));
                    break;

                case "reset":
                    foreach (var drill in T.Drills)
                        PlacedDestructibles.Respawn(PlacedDestructibles.LiveOf(map, T.DrillId(drill.Number)));
                    for (uint pump = 1; pump <= T.PumpHouses.Length; pump++)
                        PlacedDestructibles.Respawn(PlacedDestructibles.LiveOf(map, T.PumpHouseId(pump)));
                    PlacedDestructibles.Respawn(PlacedDestructibles.LiveOf(map, T.MainPump));
                    foreach (var lake in T.Lakes)
                    {
                        PutOut(map, state, lake);
                        state.ConsoleLit[lake.Number - 1] = false;
                        SetUsable(map, T.ConsoleId(lake.Number), UseObjectState.TsState0);
                        SetUsable(map, T.FieldId(lake.Number), (UseObjectState)T.FieldDown);
                    }
                    StopAmbush(map, state);
                    state.DrillsDown.Clear();
                    state.PumpsDown.Clear();
                    state.MainPumpDown = false;
                    state.Security = Security.Idle;
                    state.Heat = 0;
                    state.Told.Clear();
                    break;
            }

            yield return $"Timora {map.InstanceId}: drills {state.DrillsDown.Count}/{T.Drills.Length} down, pumps {state.PumpsDown.Count}/{T.PumpHouses.Length} down, " +
                $"the Main Fuel Pump {(state.MainPumpDown ? "destroyed" : "standing")}.";
            yield return $"Security {state.Security}, consoles lit: {string.Join(", ", T.Lakes.Select(lake => $"{lake.Where} {(state.ConsoleLit[lake.Number - 1] ? "on" : "off")}"))}; " +
                $"lakes burning: {state.BurnsUntil.Count(until => until > now)}, heat {state.Heat / 1000} of {HeatToIgnite / 1000} lake-seconds.";
            yield return $"Escort {(state.EscortFor != 0 ? $"with character {state.EscortFor}" : "at the camp")}, {state.EscortsDelivered} delivered; " +
                $"ambush {state.Ambush}{(state.Ambush == AmbushPhase.Running ? $", wave {state.Wave} of {T.AmbushWaves}" : "")}, {state.AmbushesDefended} held.";
        }

        #endregion
    }
}
