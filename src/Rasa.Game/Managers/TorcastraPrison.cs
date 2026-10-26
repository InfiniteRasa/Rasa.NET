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
    using Packets.MapChannel.Server;
    using Structures;

    using T = Services.Preloader.TorcastraPrisonContent;
    using M = Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;
    using E = Services.Preloader.Missions.Torcastra.TorcastraMissionsV1.Events;

    /// <summary>
    /// What Torcastra Prison does, in each copy of it (map 1349, a squad instance), on what
    /// TorcastraPrisonContent puts down, for the missions TorcastraMissionsV1 authors - as the
    /// wiki's walkthrough tells it:
    ///
    ///  - The alarm. The laser trip wire across the bridge at the Torcastra Gateway: a living
    ///    player crossing it, going south, raises the alarm whether or not anyone has a mission
    ///    for it. The alarm lights come on, the 20 minutes start, and the Stalker comes out past
    ///    the wire - unless the security beacon beside it was shot down first.
    ///  - The bunkers. A bunker is secured once every pool at it - its crew and the mortar on its
    ///    roof - has spawned and been killed. All four secured stand the alarm down: the lights
    ///    go out and there is no strike.
    ///  - The air strike. The 20 minutes up with the alarm still raised: a carpet bomb on every
    ///    living player on the plateau (between the wire and the prison wall), which kills, and
    ///    Securing Torcastra's alarm objective failed for whoever has it. The bunkers then come
    ///    back, the beacon stands again, and the wire is set for the next crossing.
    ///  - The gate. The Gatekeeper's key in the slot beside it takes the Bane force field across
    ///    the gate down for good; until then the field cannot be shot down. Opening it brings Lt.
    ///    Cisco up to the gate, the field medic and the vendor into the courtyard, and the
    ///    Juggernaut out at the prison door.
    ///  - The prison. Ranger Ferme's guards, once all three interrogators are dead, and her cell
    ///    console after that; the data storage centre's four units, which stay down once shot
    ///    down and are what the worm console waits for; the two data terminals; Airman Hamilton's
    ///    restraints, opened with the Cell Block Warden's pass key (his, once he is dead, to whoever
    ///    is to release Hamilton); Hamilton taken in (he follows
    ///    the player, and is delivered within <see cref="DeliveryRadius"/> of Cisco at the gate)
    ///    or killed, as the player answered him.
    ///  - The cell blocks. Each block's console is seven hex panels. With a player near, it shows
    ///    its sequence: the two that must not be touched light together, then the ones to press,
    ///    one by one, then the last. Pressing those (in any order) and then the last unlocks the
    ///    block: its panels stay lit, and its prisoners go free. Touching one of the two, or the
    ///    last too soon, calls the block's Bane security and the console starts over.
    ///  - Prison Riot is offered by radio to whoever here has done Information Sabotage.
    ///
    /// What the world has done goes to every player in the copy whose mission is waiting on it,
    /// once a second, so a player who takes a mission up later - or joins the squad - is caught
    /// up; what a player does at a console goes to that player. Kept by the copy and not saved:
    /// once the copy is unloaded it starts over, except that a squad that has opened the gate
    /// before has it open again as soon as one of them is in.
    /// </summary>
    public static class TorcastraPrison
    {
        public enum Alarm { Armed, Raised, StoodDown }

        public enum PuzzlePhase { Idle, Showing, Input, Unlocked }

        /// <summary>The trip wire: the line between its emitters, z, and how far across and up it reaches.</summary>
        public const float TripWireZ = -1.465f, TripWireMinX = -72f, TripWireMaxX = -52f, TripWireMinY = 40f, TripWireMaxY = 58f;

        /// <summary>Where the air strike falls: south of the wire, north of the prison wall, above the gorge.</summary>
        public const float PlateauSouthZ = -258f, PlateauMinX = -340f, PlateauMaxX = 180f, PlateauMinY = 40f;

        /// <summary>The bomb's fall, and how long the beacon stays after it so the blast plays.</summary>
        public const long StrikeFallMs = 2500, StrikeLingerMs = 2000;

        /// <summary>FIRE_SUPPORT_AIR_STRIKE_EFFECT, on Ability_Carpet_Bomb_Beacon (as AbilityManager.FireSupport).</summary>
        public const int AirStrikeEffect = 393;
        public const EntityClasses StrikeBeaconClass = (EntityClasses)26582;

        /// <summary>The strike's damage: more than anyone has.</summary>
        public const int StrikeDamage = 1_000_000;

        /// <summary>How near Lt. Cisco at the gate Hamilton is delivered.</summary>
        public const float DeliveryRadius = 8f;

        /// <summary>How near a console's panels a player sets it showing its sequence.</summary>
        public const float ConsoleRadius = 8f;

        /// <summary>The sequence's timing: the two together, each panel to press, the last, and the gap after each.</summary>
        public const long ShowAvoidMs = 1500, ShowPressMs = 900, ShowLastMs = 1500, ShowGapMs = 400;

        /// <summary>After a wrong panel, how long before the console shows its sequence again.</summary>
        public const long ConsoleRestartMs = 5000;

        /// <summary>How often what the world has done goes to the players waiting on it.</summary>
        public const long CatchUpMs = 1000;

        /// <summary>How long before Prison Riot is offered again to someone who did not take it.</summary>
        public const long OfferAgainMs = 60_000;

        /// <summary>A block's sequence, panels numbered from the player's side, left to right, top to bottom.</summary>
        public sealed record Sequence(uint[] Avoid, uint[] Press, uint Last);

        /// <summary>The west and central blocks' (1-3): the wiki's.</summary>
        public static readonly Sequence West = new(new uint[] { 2, 7 }, new uint[] { 1, 3, 5, 6 }, 4);

        /// <summary>The east blocks' (4, 5): the west's mirrored, as the wiki has them inverted.</summary>
        public static readonly Sequence East = new(new uint[] { 1, 6 }, new uint[] { 2, 5, 3, 7 }, 4);

        public static Sequence SequenceOf(uint block) => block <= 3 ? West : East;

        public sealed class Block
        {
            public Block(T.CellBlock cells) => Cells = cells;

            public T.CellBlock Cells { get; }
            public uint Number => Cells.Number;
            public PuzzlePhase Phase { get; internal set; }
            public HashSet<uint> Pressed { get; } = new();
            internal List<(long At, uint[] Panels, bool On)> Script { get; } = new();
            internal long ShowAgainAt { get; set; }

            /// <summary>The middle of its seven panels.</summary>
            public Vector3 Console => new((float)(Cells.X + Cells.Side * T.PanelOffset), (float)(Cells.FloorY + T.PanelHeight), (float)Cells.Z);
        }

        internal sealed class Strike
        {
            public Manifestation Victim;
            public DynamicObject Beacon;
            public int EffectId;
            public long ExplodeAt;
            public long RemoveAt;
        }

        public sealed class State
        {
            internal bool Set;
            public Alarm Alarm { get; internal set; }
            public long RaisedAt { get; internal set; }
            public long StrikeAt { get; internal set; }
            public bool BeaconDown { get; internal set; }
            public bool[] Secured { get; } = new bool[4];
            public bool GateOpen { get; internal set; }
            public bool GuardsCleared { get; internal set; }
            public bool CellOpen { get; internal set; }
            public bool WardenDown { get; internal set; }
            public bool[] Storage { get; } = new bool[4];   // units A, B, panels A, B
            public Block[] Blocks { get; } = T.CellBlocks.Select(cells => new Block(cells)).ToArray();
            public uint HamiltonWith { get; internal set; }
            public int Strikes { get; internal set; }

            /// <summary>The bunkers' pools seen with something alive since they were last set back.</summary>
            internal readonly HashSet<uint> SeenAlive = new();
            internal bool BunkersSetBack;
            internal readonly List<Strike> Falling = new();
            internal readonly Dictionary<uint, Vector3> LastAt = new();
            internal readonly HashSet<string> Told = new();
            internal readonly Dictionary<uint, long> OfferedAt = new();
            internal readonly HashSet<uint> OneShot = new();
            internal long NextCatchUpAt;
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsPrison(MapChannel map) => map?.MapInfo?.MapContextId == T.Map;

        public static State StateOf(MapChannel map) => IsPrison(map) ? States.GetValue(map, _ => new State()) : null;

        public static bool OnPlateau(Vector3 at) =>
            at.Z < TripWireZ && at.Z > PlateauSouthZ && at.X > PlateauMinX && at.X < PlateauMaxX && at.Y > PlateauMinY;

        /// <summary>Whether a step from one point to the next crosses the trip wire, going south.</summary>
        public static bool CrossesTripWire(Vector3 from, Vector3 to) =>
            from.Z > TripWireZ && to.Z <= TripWireZ &&
            to.X >= TripWireMinX && to.X <= TripWireMaxX && to.Y >= TripWireMinY && to.Y <= TripWireMaxY;

        #region The worker

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var state = StateOf(map);

            if (state == null)
                return;

            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            var members = Members(map);

            TripWire(map, state, members, now);
            WatchWorld(map, state, now);
            AirStrike(map, state, members, now);
            Consoles(map, state, members, now);
            Hamilton(map, state, members);
            OneShots(map, state);

            if (now >= state.NextCatchUpAt)
            {
                state.NextCatchUpAt = now + CatchUpMs;
                CatchUp(map, state, members, now);
            }
        }

        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            // The gate's field cannot be shot down: the key is what opens it.
            if (PlacedDestructibles.LiveOf(map, T.GateField)?.Field is { } field)
                field.Sealed = true;
        }

        #endregion

        #region The alarm

        private static void TripWire(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var client in members)
            {
                var player = client.Player;
                var at = player.Position;

                if (state.LastAt.TryGetValue(player.Id, out var was) && state.Alarm == Alarm.Armed && CrossesTripWire(was, at))
                    RaiseAlarm(map, state, now, player);

                state.LastAt[player.Id] = at;
            }
        }

        /// <summary>The wire is crossed (or a GM's .torcastra alarm): the lights, the clock, and the Stalker if the beacon stands.</summary>
        public static void RaiseAlarm(MapChannel map, State state, long now, Manifestation by)
        {
            if (state.Alarm != Alarm.Armed)
                return;

            state.Alarm = Alarm.Raised;
            state.RaisedAt = now;
            state.StrikeAt = now + M.AlarmSeconds * 1000L;

            SetUsable(map, T.TripWire, UseObjectState.TsState0);
            Lights(map, true);

            if (!state.BeaconDown)
                WakeOnce(map, state, T.StalkerPool);

            Tell(map, "Torcastra security has been alerted. Secure the four bunkers to stand the alarm down before the air strike: 20 minutes.");
            Logger.WriteLog(LogType.Debug, $"Torcastra {map.InstanceId}: the alarm raised by {by?.FamilyName ?? "a GM"}{(state.BeaconDown ? ", the beacon down" : "")}.");
        }

        /// <summary>The bunkers, the beacon, the storage, Ferme's guards: what has happened to them.</summary>
        private static void WatchWorld(MapChannel map, State state, long now)
        {
            var beacon = PlacedDestructibles.LiveOf(map, T.SecurityBeacon);

            if (beacon != null && beacon.IsDown)
            {
                state.BeaconDown = true;
                beacon.RespawnAt = long.MaxValue;
            }

            for (var i = 0; i < T.Bunkers.Length; i++)
            {
                var pools = PoolsOf(T.Bunkers[i]).Select(id => Pool(map, id)).Where(pool => pool != null).ToList();

                foreach (var pool in pools.Where(pool => pool.AliveCreatures > 0 || pool.QueuedCreatures > 0))
                    state.SeenAlive.Add(pool.DbId);

                if (!state.Secured[i] && pools.Count > 0 && pools.All(pool => Cleared(state, pool)))
                {
                    state.Secured[i] = true;
                    Tell(map, $"The {T.Bunkers[i].Name} bunker is secured.");
                }
            }

            if (state.Alarm == Alarm.Raised && state.Secured.All(secured => secured))
            {
                state.Alarm = Alarm.StoodDown;
                Lights(map, false);
                Tell(map, "All four bunkers are secured: the alarm is stood down.");
            }

            var storage = new[] { T.StorageUnitA, T.StorageUnitB, T.StoragePanelA, T.StoragePanelB };

            for (var i = 0; i < storage.Length; i++)
            {
                var live = PlacedDestructibles.LiveOf(map, storage[i]);

                if (live != null && live.IsDown)
                {
                    state.Storage[i] = true;
                    live.RespawnAt = long.MaxValue;
                }
            }

            if (!state.GuardsCleared && Pool(map, T.InterrogatorPool) is { } guards && Cleared(state, guards))
                state.GuardsCleared = true;

            if (!state.WardenDown && Pool(map, T.WardenPool) is { } warden && Cleared(state, warden))
                state.WardenDown = true;
        }

        private static IEnumerable<uint> PoolsOf(T.Bunker bunker) => bunker.CrewPools.Append(bunker.MortarPool);

        /// <summary>
        /// Whether a pool has been killed: in a squad instance, dead by its clock (which is kept);
        /// otherwise spawned and nothing of it left. Once its bunker has been set back, only once
        /// it has come back and been killed again.
        /// </summary>
        private static bool Cleared(State state, SpawnPool pool)
        {
            if (pool.AliveCreatures > 0 || pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                return false;

            if (pool.NoWalkableGround)
                return true;

            if (state.BunkersSetBack && T.Bunkers.Any(bunker => PoolsOf(bunker).Contains(pool.DbId)))
                return state.SeenAlive.Contains(pool.DbId);

            return pool.ClearedAtUtcMs != 0 || pool.HasSpawned || state.SeenAlive.Contains(pool.DbId);
        }

        private static void AirStrike(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var strike in state.Falling.ToList())
            {
                if (strike.ExplodeAt != 0 && now >= strike.ExplodeAt)
                {
                    strike.ExplodeAt = 0;
                    Explode(map, strike);
                    strike.RemoveAt = now + StrikeLingerMs;
                }

                if (strike.RemoveAt != 0 && now >= strike.RemoveAt)
                {
                    CellManager.Instance.CellCallMethod(strike.Beacon, new GameEffectDetachedPacket { EffectId = strike.EffectId });
                    CellManager.Instance.RemoveFromWorld(map, strike.Beacon);
                    state.Falling.Remove(strike);
                }
            }

            if (state.Alarm == Alarm.Raised && now >= state.StrikeAt)
                CallStrike(map, state, members, now);
        }

        /// <summary>The time is up (or a GM's .torcastra strike): the bombs on the plateau, the objective failed, everything set back.</summary>
        public static void CallStrike(MapChannel map, State state, List<Client> members, long now)
        {
            state.Strikes++;

            foreach (var client in members)
            {
                var player = client.Player;

                // The alarm objective failed for whoever is waiting on it, wherever they stand.
                if (Wants(player, M.Securing, 9))
                    Emit(client, M.Securing, E.Plateau, E.AirStrike, once: false, state);

                if (!OnPlateau(player.Position))
                    continue;

                var beacon = new DynamicObject
                {
                    EntityClassId = StrikeBeaconClass,
                    Position = NavMeshManager.SnapToGround(map, player.Position),
                    MapContextId = map.MapInfo.MapContextId,
                    IsEnabled = false
                };

                CellManager.Instance.AddToWorld(map, beacon);

                var strike = new Strike
                {
                    Victim = player,
                    Beacon = beacon,
                    EffectId = GameEffectManager.Instance.NextEffectId(map),
                    ExplodeAt = now + StrikeFallMs
                };

                CellManager.Instance.CellCallMethod(beacon, new GameEffectAttachedPacket
                {
                    EffectTypeId = AirStrikeEffect,
                    EffectId = strike.EffectId,
                    EffectLevel = GameEffectFx.LevelFor(AirStrikeEffect, 6),
                    SourceId = player.EntityId,
                    Announced = true,
                    DamageType = (int)DamageType.Physical,
                    AttrId = 1,
                    IsActive = true,
                    IsDebuff = true,
                    IsNegativeEffect = true
                });

                state.Falling.Add(strike);
            }

            Tell(map, "The alarm was not stood down: the air strike is coming in on the plateau.");
            Logger.WriteLog(LogType.Debug, $"Torcastra {map.InstanceId}: the air strike, on {state.Falling.Count} player(s).");

            SetBack(map, state);
        }

        private static void Explode(MapChannel map, Strike strike)
        {
            var blast = new GameEffectAnnounceDamagePacket(strike.EffectId, "DoExplosion");
            var player = strike.Victim;

            if (player != null && ReferenceEquals(player.MapChannel, map) && player.State != CharacterState.Dead && player.State != CharacterState.Dying &&
                OnPlateau(player.Position) && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0)
            {
                var taken = ActorManager.Instance.Damage(map, player, StrikeDamage, null, out var outcome, DamageType.Physical);

                blast.Hits.Add(new TickEntry
                {
                    EntityId = player.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    DamageType = DamageType.Physical,
                    DeathBlow = taken > 0 && player.Attributes[Attributes.Health].Current <= 0
                });
            }

            CellManager.Instance.CellCallMethod(strike.Beacon, blast);
        }

        /// <summary>After a strike: the wire set again, the lights out, the bunkers and the beacon back.</summary>
        private static void SetBack(MapChannel map, State state)
        {
            state.Alarm = Alarm.Armed;
            state.RaisedAt = 0;
            state.StrikeAt = 0;
            Array.Clear(state.Secured, 0, state.Secured.Length);
            state.SeenAlive.Clear();
            state.BunkersSetBack = true;

            SetUsable(map, T.TripWire, UseObjectState.TsState1);
            Lights(map, false);

            foreach (var id in T.Bunkers.SelectMany(PoolsOf))
                if (Pool(map, id) is { } pool && pool.AliveCreatures == 0)
                {
                    map.SquadState?.PoolRespawned(pool);
                    pool.ClearedAtUtcMs = 0;
                    pool.UpdateTimer = pool.RespawnTime;
                }

            if (PlacedDestructibles.LiveOf(map, T.SecurityBeacon) is { } beacon && beacon.IsDown)
                PlacedDestructibles.Respawn(beacon);

            state.BeaconDown = false;
        }

        private static void Lights(MapChannel map, bool on)
        {
            foreach (var id in T.AlarmLights)
                SetUsable(map, id, on ? UseObjectState.TsState1 : UseObjectState.TsState0);
        }

        #endregion

        #region The gate

        /// <summary>The field down for good, and those who wait for it in.</summary>
        public static void OpenGate(MapChannel map, State state, ulong byEntityId)
        {
            if (state.GateOpen)
                return;

            state.GateOpen = true;

            if (PlacedDestructibles.LiveOf(map, T.GateField) is { } live && live.Field is { } field)
            {
                field.Sealed = false;

                if (!field.IsDestroyed)
                    ForceFields.Damage(field, field.Health, byEntityId);

                live.RespawnAt = long.MaxValue;
            }

            SetUsable(map, T.GateKeySlot, UseObjectState.IaStateAccepted);

            Wake(map, T.CiscoGatePool);
            foreach (var id in T.CourtyardPools)
                Wake(map, id);
            WakeOnce(map, state, T.JuggernautPool);

            Tell(map, "The gate is open. Lt. Cisco is moving up to it.");
        }

        #endregion

        #region Usables

        /// <summary>
        /// Whether a player may use one of the prison's usables now (PlacedUsables.RefusalOf):
        /// null if they may, or why not.
        /// </summary>
        public static PlayerMessage? RefusalOf(Client client, PlacedUsables.Live live)
        {
            var map = live?.Map;
            var state = StateOf(map);
            var player = client?.Player;

            if (state == null || player == null)
                return null;

            var id = live.Placement.Id;

            if (BlockOf(state, id, out var block, out _))
                return block.Phase switch
                {
                    PuzzlePhase.Input => null,
                    PuzzlePhase.Unlocked => PlayerMessage.PmUseObjectNotUsable,
                    _ => PlayerMessage.PmCannotPerformActionNow
                };

            switch (id)
            {
                case T.GateKeySlot:
                    return state.GateOpen ? PlayerMessage.PmUseObjectNotUsable : null;

                case T.FermeCellConsole:
                    return state.CellOpen ? PlayerMessage.PmUseObjectNotUsable :
                        state.GuardsCleared ? null : PlayerMessage.PmCannotPerformActionNow;

                case T.WormConsole:
                    if (!Wants(player, M.Sabotage, 3))
                        return PlayerMessage.PmUseObjectNotUsable;
                    return state.Storage.All(down => down) ? null : PlayerMessage.PmCannotPerformActionNow;

                case T.PrimaryTerminal:
                    return Wants(player, M.Sabotage, 6) ? null : PlayerMessage.PmUseObjectNotUsable;

                case T.SecondaryTerminal:
                    return Wants(player, M.Sabotage, 7) ? null : PlayerMessage.PmUseObjectNotUsable;

                case T.ResearchWardConsole:
                    return Wants(player, M.Keymaster, 5) ? null : PlayerMessage.PmUseObjectNotUsable;
            }

            return null;
        }

        /// <summary>A player has used one of the prison's usables (PlacedUsables.UseRecovery), to <paramref name="next"/>.</summary>
        public static void Used(Client client, PlacedUsables.Live live, UseObjectState next)
        {
            var map = live?.Map;
            var state = StateOf(map);

            if (state == null || client?.Player == null)
                return;

            var id = live.Placement.Id;

            if (BlockOf(state, id, out var block, out var panel))
            {
                Press(map, state, block, panel, client);
                return;
            }

            switch (id)
            {
                case T.GateKeySlot:
                    OpenGate(map, state, client.Player.EntityId);
                    live.ResetAt = 0;
                    break;

                case T.FermeCellConsole:
                    state.CellOpen = true;
                    live.ResetAt = 0;
                    Emit(client, M.InTerror, E.Ferme, E.CellOpened, once: true, state);
                    break;

                case T.WormConsole:
                    Emit(client, M.Sabotage, E.Data, E.WormUploaded, once: true, state);
                    break;

                case T.PrimaryTerminal:
                    Emit(client, M.Sabotage, E.Data, E.PrimaryTerminal, once: true, state);
                    break;

                case T.SecondaryTerminal:
                    Emit(client, M.Sabotage, E.Data, E.SecondaryTerminal, once: true, state);
                    break;

                case T.ResearchWardConsole:
                    Emit(client, M.Keymaster, E.Hamilton, E.Released, once: true, state);
                    break;
            }
        }

        #endregion

        #region The cell block consoles

        private static bool BlockOf(State state, uint usableId, out Block block, out uint panel)
        {
            block = null;
            panel = 0;

            if (usableId < T.FirstPanel || usableId >= T.FirstPanel + 10 * (uint)state.Blocks.Length)
                return false;

            var index = (usableId - T.FirstPanel) / 10;
            panel = (usableId - T.FirstPanel) % 10 + 1;

            if (panel > 7)
                return false;

            block = state.Blocks[index];
            return true;
        }

        private static void Consoles(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var block in state.Blocks)
            {
                switch (block.Phase)
                {
                    case PuzzlePhase.Idle:
                        if (now >= block.ShowAgainAt && members.Any(client => Vector3.Distance(client.Player.Position, block.Console) <= ConsoleRadius))
                            Show(map, block, now);
                        break;

                    case PuzzlePhase.Showing:
                        while (block.Script.Count > 0 && now >= block.Script[0].At)
                        {
                            var step = block.Script[0];
                            block.Script.RemoveAt(0);

                            foreach (var panel in step.Panels)
                                SetPanel(map, block, panel, step.On);
                        }

                        if (block.Script.Count == 0)
                            block.Phase = PuzzlePhase.Input;
                        break;
                }
            }
        }

        /// <summary>The console shows its sequence: the two not to touch, the ones to press, the last.</summary>
        public static void Show(MapChannel map, Block block, long now)
        {
            var sequence = SequenceOf(block.Number);
            var at = now;

            block.Phase = PuzzlePhase.Showing;
            block.Pressed.Clear();
            block.Script.Clear();

            AllPanels(map, block, false);

            block.Script.Add((at, sequence.Avoid, true));
            at += ShowAvoidMs;
            block.Script.Add((at, sequence.Avoid, false));
            at += ShowGapMs;

            foreach (var panel in sequence.Press)
            {
                block.Script.Add((at, new[] { panel }, true));
                at += ShowPressMs;
                block.Script.Add((at, new[] { panel }, false));
                at += ShowGapMs;
            }

            block.Script.Add((at, new[] { sequence.Last }, true));
            at += ShowLastMs;
            block.Script.Add((at, new[] { sequence.Last }, false));
        }

        private static void Press(MapChannel map, State state, Block block, uint panel, Client client)
        {
            if (block.Phase != PuzzlePhase.Input)
                return;

            var sequence = SequenceOf(block.Number);

            if (sequence.Press.Contains(panel))
            {
                // Pressed: it stays lit, a second press too.
                block.Pressed.Add(panel);
                SetPanel(map, block, panel, true);
                return;
            }

            if (panel == sequence.Last && sequence.Press.All(block.Pressed.Contains))
            {
                Unlock(map, state, block);
                return;
            }

            // One of the two, or the last too soon: Bane security, and the console starts over.
            WakeOnce(map, state, block.Cells.SecurityPool);
            AllPanels(map, block, false);
            block.Pressed.Clear();
            block.Phase = PuzzlePhase.Idle;
            block.ShowAgainAt = Now() + ConsoleRestartMs;

            CommunicatorManager.Instance.SystemMessage(client, $"Cell block {block.Number}: wrong panel. Bane security is on its way.");
        }

        /// <summary>The block is unlocked: its panels lit for good, its prisoners out.</summary>
        public static void Unlock(MapChannel map, State state, Block block)
        {
            block.Phase = PuzzlePhase.Unlocked;
            block.Script.Clear();
            AllPanels(map, block, true);

            foreach (var id in block.Cells.PrisonerPools)
            {
                if (Pool(map, id) is not { } pool)
                    continue;

                pool.Quieted = true;

                foreach (var prisoner in Creatures(map).Where(creature => creature.SpawnPool == pool).ToList())
                    CellManager.Instance.RemoveCreatureFromWorld(map, prisoner);
            }

            Tell(map, $"Cell block {block.Number} is unlocked.");
        }

        private static void AllPanels(MapChannel map, Block block, bool on)
        {
            for (uint panel = 1; panel <= 7; panel++)
                SetPanel(map, block, panel, on);
        }

        private static void SetPanel(MapChannel map, Block block, uint panel, bool on) =>
            SetUsable(map, T.PanelId(block.Number, panel), on ? UseObjectState.TsState1 : UseObjectState.TsState0);

        #endregion

        #region Airman Hamilton

        private static void Hamilton(MapChannel map, State state, List<Client> members)
        {
            var hamilton = Creatures(map).FirstOrDefault(creature => creature.DbId == T.Hamilton);

            if (hamilton == null)
            {
                state.HamiltonWith = 0;
                return;
            }

            if (state.HamiltonWith != 0)
            {
                var with = members.FirstOrDefault(client => client.Player.Id == state.HamiltonWith);

                // The one he follows has given the mission up: he stays where he is.
                if (with != null && !Wants(with.Player, M.Keymaster, 8))
                {
                    StopFollowing(state, hamilton);
                    return;
                }

                var cisco = Creatures(map).FirstOrDefault(creature => creature.DbId == T.CiscoAtGate);
                var to = cisco?.Position ?? new Vector3(-90f, 96.54f, -268f);

                if (with != null && Vector3.Distance(hamilton.Position, to) <= DeliveryRadius)
                {
                    Emit(with, M.Keymaster, E.Hamilton, E.Delivered, once: true, state);
                    StopFollowing(state, hamilton);
                }

                return;
            }

            foreach (var client in members)
            {
                var player = client.Player;

                if (Wants(player, M.Keymaster, 8))
                {
                    state.HamiltonWith = player.Id;

                    if (hamilton.SpawnPool != null)
                        hamilton.SpawnPool.FollowOwnerCharacterId = player.Id;

                    BehaviorManager.Instance.SetActionFollow(hamilton, player.EntityId);
                    return;
                }

                if (Wants(player, M.Keymaster, 7))
                {
                    CreatureManager.Instance.HandleCreatureKill(map, hamilton, player);
                    Emit(client, M.Keymaster, E.Hamilton, E.Killed, once: true, state);
                    return;
                }
            }
        }

        private static void StopFollowing(State state, Creature hamilton)
        {
            state.HamiltonWith = 0;

            if (hamilton.SpawnPool != null)
                hamilton.SpawnPool.FollowOwnerCharacterId = 0;

            BehaviorManager.Instance.SetActionAnchor(hamilton, hamilton.Position);
        }

        #endregion

        #region Catching up

        private static void CatchUp(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var client in members)
            {
                var player = client.Player;

                // A squad that has opened the gate before has it open again.
                if (!state.GateOpen && HasOpenedTheGate(player))
                    OpenGate(map, state, player.EntityId);

                if (state.Alarm != Alarm.Armed && Wants(player, M.Securing, 6))
                    Emit(client, M.Securing, E.Plateau, E.AlarmRaised, once: true, state);

                for (var i = 0; i < T.Bunkers.Length; i++)
                    if (state.Secured[i] && (Wants(player, M.Securing, T.Bunkers[i].Objective) || Wants(player, M.Securing, 9)))
                        Emit(client, M.Securing, E.Bunkers, T.Bunkers[i].Event, once: true, state);

                if (state.GateOpen && Wants(player, M.Securing, 4))
                    Emit(client, M.Securing, E.Plateau, E.GateOpened, once: true, state);

                if (state.GuardsCleared && Wants(player, M.InTerror, 1))
                    Emit(client, M.InTerror, E.Ferme, E.GuardsCleared, once: true, state);

                if (state.CellOpen && Wants(player, M.InTerror, 2))
                    Emit(client, M.InTerror, E.Ferme, E.CellOpened, once: true, state);

                for (uint unit = 0; unit < 2; unit++)
                    if (state.Storage[unit] && Wants(player, M.Sabotage, 2))
                        Emit(client, M.Sabotage, E.StorageUnits, unit + 1, once: true, state);

                if (state.Storage[2] && state.Storage[3] && Wants(player, M.Sabotage, 9))
                    Emit(client, M.Sabotage, E.StoragePanels, E.PanelsDestroyed, once: true, state);

                if (state.WardenDown && Wants(player, M.Keymaster, 5))
                    PassKey(client, state);

                foreach (var block in state.Blocks)
                    if (block.Phase == PuzzlePhase.Unlocked && Wants(player, M.Riot, block.Number))
                        Emit(client, M.Riot, E.CellBlocks, block.Number, once: true, state);

                OfferRiot(client, state, now);
            }
        }

        /// <summary>
        /// The Cell Block Warden dead: his pass key to whoever is to release Hamilton and has none,
        /// once for their assignment. Given rather than dropped as the mission's: a mission item
        /// drops only for an objective of picking it up, and releasing Hamilton is not one.
        /// </summary>
        private static void PassKey(Client client, State state)
        {
            var player = client.Player;

            if (InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)T.AccessKeyClass) > 0 ||
                !player.Missions.TryGetValue(M.Keymaster, out var log))
                return;

            var key = $"{player.Id}:{log.AssignmentId}:pass-key";

            if (state.Told.Contains(key) || ItemManager.Instance.CreateFromTemplateId(T.AccessKeyTemplate, 1) is not { } item)
                return;

            if (InventoryManager.Instance.GrantItemToInventory(client, item) == null)
            {
                EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                CommunicatorManager.ShowPlayerMessage(client, PlayerMessage.PmInventoryFull);
                return;
            }

            state.Told.Add(key);
            CommunicatorManager.Instance.SystemMessage(client, "You take the Cell Block Warden's pass key.");
        }

        private static bool HasOpenedTheGate(Manifestation player) =>
            Done(player, M.Securing) ||
            player.Missions.TryGetValue(M.Securing, out var log) &&
            log.Objectives.TryGetValue(4, out var gate) && gate.State == MissionObjectiveState.Completed;

        private static void OfferRiot(Client client, State state, long now)
        {
            var player = client.Player;

            if (!Done(player, M.Sabotage) || player.Missions.ContainsKey(M.Riot) || Done(player, M.Riot) ||
                state.OfferedAt.TryGetValue(player.Id, out var at) && now - at < OfferAgainMs)
                return;

            state.OfferedAt[player.Id] = now;
            MissionApplication.Instance?.Offers.TryOffer(client, M.Riot,
                Rasa.Missions.Definitions.MissionOfferSourceIdentity.ServerEvent(M.RiotOfferKey));
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

        /// <summary>A scripted pool set going, to stay going.</summary>
        private static void Wake(MapChannel map, uint id)
        {
            if (Pool(map, id) is { } pool)
                pool.Woken = true;
        }

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

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state is MissionState.Completed or MissionState.Success ||
            player.Missions.TryGetValue(missionId, out var log) && log.State is MissionState.Completed or MissionState.Success;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>
        /// A scenario event to the player's mission. With <paramref name="once"/>, not again for the
        /// same assignment once it has gone through.
        /// </summary>
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
        /// .torcastra: what this copy has done, and a GM's hand in it.
        ///   .torcastra                 - the alarm, the bunkers, the gate, the storage, the cell blocks
        ///   .torcastra alarm           - raises the alarm
        ///   .torcastra strike          - calls the air strike now
        ///   .torcastra secure &lt;n|all&gt;  - a bunker (1 NE, 2 NW, 3 SE, 4 SW), or all four, secured
        ///   .torcastra gate            - opens the gate
        ///   .torcastra unlock &lt;n|all&gt;  - a cell block, or all five, unlocked
        ///   .torcastra reset           - the copy as it was: everything set back
        /// </summary>
        public static IEnumerable<string> Command(MapChannel map, Manifestation gm, string[] parts)
        {
            var state = StateOf(map);

            if (state == null)
            {
                yield return "Not in Torcastra Prison.";
                yield break;
            }

            var now = Now();
            var sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;
            var arg = parts.Length > 2 ? parts[2].ToLowerInvariant() : string.Empty;

            switch (sub)
            {
                case "alarm":
                    RaiseAlarm(map, state, now, null);
                    break;

                case "strike":
                    if (state.Alarm != Alarm.Raised)
                        RaiseAlarm(map, state, now, null);
                    CallStrike(map, state, Members(map), now);
                    break;

                case "secure":
                    for (var i = 0; i < state.Secured.Length; i++)
                        if (arg == "all" || arg == (i + 1).ToString())
                            state.Secured[i] = true;
                    WatchWorld(map, state, now);
                    break;

                case "gate":
                    OpenGate(map, state, gm?.EntityId ?? 0);
                    break;

                case "unlock":
                    foreach (var block in state.Blocks)
                        if ((arg == "all" || arg == block.Number.ToString()) && block.Phase != PuzzlePhase.Unlocked)
                            Unlock(map, state, block);
                    break;

                case "reset":
                    SetBack(map, state);
                    state.BunkersSetBack = false;
                    state.GuardsCleared = state.CellOpen = false;
                    SetUsable(map, T.FermeCellConsole, UseObjectState.TsState0);
                    foreach (var block in state.Blocks)
                    {
                        block.Phase = PuzzlePhase.Idle;
                        block.Pressed.Clear();
                        block.Script.Clear();
                        AllPanels(map, block, false);
                        foreach (var id in block.Cells.PrisonerPools)
                            if (Pool(map, id) is { } pool)
                                pool.Quieted = false;
                    }
                    state.Told.Clear();
                    break;
            }

            yield return $"Torcastra {map.InstanceId}: alarm {state.Alarm}" +
                (state.Alarm == Alarm.Raised ? $", strike in {Math.Max(0, state.StrikeAt - now) / 1000} s" : "") +
                $", beacon {(state.BeaconDown ? "down" : "standing")}, {state.Strikes} strike(s).";
            yield return "Bunkers: " + string.Join(", ", T.Bunkers.Select((bunker, i) => $"{bunker.Name} {(state.Secured[i] ? "secured" : "held")}")) + ".";
            yield return $"Gate {(state.GateOpen ? "open" : "shut")}; Ferme's guards {(state.GuardsCleared ? "cleared" : "standing")}, her cell {(state.CellOpen ? "open" : "shut")}; " +
                $"storage units {Count(state.Storage.Take(2))}/2, panels {Count(state.Storage.Skip(2))}/2 down.";
            yield return "Cell blocks: " + string.Join(", ", state.Blocks.Select(block => $"{block.Number} {block.Phase}")) +
                (state.HamiltonWith != 0 ? $"; Hamilton following character {state.HamiltonWith}" : "") + ".";
        }

        private static int Count(IEnumerable<bool> flags) => flags.Count(flag => flag);

        #endregion

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();
    }
}
