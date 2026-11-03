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
    using Structures.World;

    using C = Services.Preloader.EnergyWeaponCenterContent;

    /// <summary>
    /// What the Energy Weapon Center (2107) does that its rows do not: each copy of it - a
    /// squad's instance, or the map's own channel - runs its story for whoever is in it
    /// (EnergyWeaponCenterContent has the people, the Bane and the missions). The world is the
    /// copy's, one for everyone in it, and an event is everyone's in the copy who has its
    /// objective open.
    ///
    ///  - The ways: sealed Bane force fields across the tunnel west of the cross (to the research
    ///    lab), across the service corridor from the generator room up to the lab, and across the
    ///    cross's north branch (to the command chamber). Shots do nothing to them. A fourth,
    ///    across the corridor out to the yard, never fails: the yard is seen, not reached.
    ///  - The reactor tubes (It's All In the Timing): each tube's plasma reactor charges for
    ///    <see cref="ChargeMs"/> - its alarm light on, and a warning to whoever is about the tubes
    ///    - then fires down the tube for <see cref="FireMs"/>, the north tube's and the south's by
    ///    turns, <see cref="CycleMs"/> round. A living player in a firing tube's beam - its length,
    ///    within <see cref="BeamHalfWidth"/> of its axis, not down in a pit under its couplings or
    ///    in a side room - takes <see cref="BeamDamage"/> each <see cref="PulseMs"/>, Laser, from
    ///    the reactor: enough to kill.
    ///  - The primary generator at the tubes' far end: 20000 hit
    ///    points (C.GeneratorHitPoints), Lightning (Electrical) <see cref="LightningTimes"/> times as hard (Struck). Down,
    ///    it stays down: the reactors stop, the tunnel's and the corridor's fields fail, and it is
    ///    the objective.
    ///  - Tesla Inversion: the testing station in the lab's south gallery gives a member on it the
    ///    datablade; the Tesla Control Console at the lab's window takes it from whoever holds one
    ///    (PmMissingReqItem without). The alarm sounds and brings the facility guards to the lab,
    ///    and the coil prototypes on the test range outside the window zap every Bane of the
    ///    facility within <see cref="ZapRadius"/>, one jolt of <see cref="ZapMin"/> to
    ///    <see cref="ZapMax"/> each <see cref="ZapEveryMs"/>, for <see cref="InversionMs"/>; then
    ///    they burn out.
    ///  - Look Before You Sabotage: a security monitor of the north gallery's bank, used, is the
    ///    network accessed; Demolitionist Engelsen's body on the warehouse's crate stacks gives a
    ///    member the charges they lack of three. A charge set in the bomb at a fuel tank
    ///    (PlacedUsables) is that tank's count, and cannot be taken out again; it goes off
    ///    <see cref="FuseSeconds"/> s later and takes the tank with it. The three blown bring the
    ///    facility guards to the warehouse.
    ///  - The Strongest Link: Linker Commander Casso takes no harm while any of his minions' pools
    ///    stands (C.MinionPools: spawned, with one alive, or not yet spawned). Dead, he is the
    ///    objective, and the north branch's field fails.
    /// What is destroyed stays down while the copy stands. Members who come later are given what
    /// the copy has done; a squad whose member has shut the reactors down or beaten Casso before
    /// has those ways open again in a new copy. A copy closed and opened again starts over.
    /// </summary>
    public static class EnergyWeaponCenter
    {
        public const int UseWindupMs = 2_000;

        public const long CycleMs = 16_000, ChargeMs = 3_000, FireMs = 2_000, PulseMs = 1_000;

        /// <summary>When in the cycle each tube's reactor starts to charge: the north first, the south half a cycle on.</summary>
        public const long NorthChargesAt = 0, SouthChargesAt = 8_000;

        public const float BeamHalfWidth = 3.5f, BeamFloor = 99f, BeamTop = 110f;
        public const int BeamDamage = 3_000;

        /// <summary>How near the tubes a member hears a reactor charging.</summary>
        public static readonly (float MinX, float MaxX, float MinZ, float MaxZ) TubesArea = (120f, 300f, -90f, -10f);

        public const int LightningTimes = 3;

        public const long InversionMs = 60_000, ZapEveryMs = 2_000;
        public const float ZapRadius = 25f;
        public const int ZapMin = 900, ZapMax = 1_300;

        public const uint FuseSeconds = 15, ChargeDamageMin = 1_500, ChargeDamageMax = 2_500;
        public const float ChargeRadius = 10f;

        /// <summary>How near the command chamber a member is told of Casso's link, and how often.</summary>
        public const float ChamberRadius = 40f;
        public const long LinkToldEveryMs = 30_000;

        public const long RetryMs = 10_000;

        /// <summary>TESLA_COIL_ZAP, as TeslaCoils puts it on a player.</summary>
        public const int ZapEffect = TeslaCoils.EffectTypeId;

        public enum Kind { Datablade, Console, Monitor, Corpse }

        public enum Phase { Idle, Charging, Firing }

        /// <summary>One of the copy's usables: what it is, and which.</summary>
        internal sealed class Usable
        {
            public Kind Kind;
            public int Index;
            public DynamicObject Object;
        }

        /// <summary>A reactor tube: its axis, its light, its fire, where it is in its cycle.</summary>
        internal sealed class Tube
        {
            public string Name;
            public float Z;
            public long ChargesAt;
            public PlacedUsables.Live Light, Fire;
            public Phase Phase;
            public long NextPulseAt;
        }

        /// <summary>One copy of the facility, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public long CycleFrom;
            public PlacedDestructibles.Live TunnelField, LabField, CommandField, YardField, Generator;
            public readonly PlacedDestructibles.Live[] Tanks = new PlacedDestructibles.Live[C.Tanks];
            public readonly PlacedUsables.Live[] Charges = new PlacedUsables.Live[C.Tanks];
            public readonly List<PlacedDestructibles.Live> Coils = new();
            public readonly List<Usable> Usables = new();
            public readonly List<Tube> Tubes = new();

            public bool ReactorsDown, Inverted, CoilsBurnt, NetworkAccessed, ChargesFound, CassoDown, CommandOpen, WarehouseAlarmed;
            public readonly bool[] Armed = new bool[C.Tanks];
            public long InvertedAt, NextZapAt;
            public ulong InvertedBy;
            public readonly Dictionary<ulong, GameEffect> Zapped = new();

            public Creature Casso;
            public bool CassoLinked = true;
            public long LinkToldAt = long.MinValue;

            public readonly HashSet<uint> OneShot = new();

            /// <summary>Events already given, by player, assignment and event: given once; one that changed nothing is tried again later.</summary>
            public readonly Dictionary<string, long> Given = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A random number from the first to the second, both in; a test's to replace.</summary>
        public static Func<long, long, long> Roll { get; set; } = (min, max) => Random.Shared.NextInt64(min, max + 1);

        /// <summary>Whether a map channel is a copy of the facility.</summary>
        public static bool IsCenter(MapChannel map) => map?.MapInfo?.MapContextId == C.Center;

        internal static State StateOf(MapChannel map) => IsCenter(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsCenter(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state, now);

            var members = Members(map);

            CatchUpWays(map, state, members);
            Generator(map, state, members);
            Reactors(map, state, members, now);
            Coils(map, state, members, now);
            Tanks(map, state, members);
            Casso(map, state, members, now);
            KeepDown(state);
            OneShots(map, state);

            if (members.Count > 0)
                Progress(state, members, now);
        }

        #region Set up

        /// <summary>The copy's fields, generator, tanks and their charges, reactors, coils and usables, put down once.</summary>
        private static void SetUp(MapChannel map, State state, long now)
        {
            state.Set = true;
            state.CycleFrom = now;

            state.TunnelField = Field(map, C.TunnelField, C.TunnelFieldYaw);
            state.LabField = Field(map, C.LabField, C.LabFieldYaw);
            state.CommandField = Field(map, C.CommandField, C.CommandFieldYaw);
            state.YardField = Field(map, C.YardField, C.YardFieldYaw);

            state.Generator = PlacedDestructibles.PutDown(map, (EntityClasses)C.GeneratorClass, Ground(map, C.Generator), 0, C.GeneratorHitPoints);

            for (var i = 0; i < C.Tanks; i++)
            {
                state.Tanks[i] = PlacedDestructibles.PutDown(map, (EntityClasses)C.TankClass, Ground(map, C.TankSpots[i]), 0, C.TankHitPoints);
                state.Charges[i] = Charge(map, C.ChargeSpots[i], $"Energy Weapon Center: the charge at fuel tank {i + 1}");
            }

            foreach (var spot in C.Coils)
                if (PlacedDestructibles.PutDown(map, (EntityClasses)C.CoilClass, Ground(map, spot), 0) is { } coil)
                    state.Coils.Add(coil);

            state.Tubes.Add(TubeOf(map, "north", C.NorthTubeZ, NorthChargesAt, C.NorthReactor));
            state.Tubes.Add(TubeOf(map, "south", C.SouthTubeZ, SouthChargesAt, C.SouthReactor));

            Add(state, Kind.Datablade, 0, Place(map, "ewc:datablade", C.DispenserClass, Ground(map, C.Datablade), 0, UseObjectState.IdStateActive));
            Add(state, Kind.Console, 0, Place(map, "ewc:tesla-console", C.ConsoleClass, Ground(map, C.Console), -1.5708, UseObjectState.IaStateActive));

            for (var i = 0; i < C.Monitors.Length; i++)
                Add(state, Kind.Monitor, i, Place(map, $"ewc:monitor-{i}", C.MonitorClasses[i], Ground(map, C.Monitors[i]), 3.1416, UseObjectState.TsState0));

            Add(state, Kind.Corpse, 0, Place(map, "ewc:engelsen", C.CorpseClass, Ground(map, C.Engelsen), 0.8, UseObjectState.IdStateActive));

            Logger.WriteLog(LogType.Debug, $"Energy Weapon Center {map.InstanceId}: four fields, the generator, {C.Tanks} tanks and charges, " +
                $"{state.Coils.Count} coils, two reactors and {state.Usables.Count} usables put down.");
        }

        /// <summary>A sealed field of the Bane wall's minor entrance: it stops players and takes nothing from shots.</summary>
        private static PlacedDestructibles.Live Field(MapChannel map, Vector3 at, float yaw)
        {
            var live = PlacedDestructibles.PutDown(map, (EntityClasses)C.FieldClass, at, yaw, side: WorldDestructibleEntry.SideBane, @sealed: true);

            if (live?.Field != null)
                live.Field.Sealed = true;

            return live;
        }

        /// <summary>The bomb a charge is set in, at a fuel tank: one charge, its fuse, its blast.</summary>
        private static PlacedUsables.Live Charge(MapChannel map, Vector3 at, string comment)
        {
            var ground = Ground(map, at);
            var bomb = PlacedUsables.PutDown(map, new WorldUsableEntry
            {
                ClassId = C.BombClass,
                PosX = ground.X,
                PosY = ground.Y,
                PosZ = ground.Z,
                Usable = 1,
                ItemClassId = C.ChargeClass,
                ItemCount = 1,
                IntervalMinSeconds = FuseSeconds,
                ResetSeconds = 86_400,
                DamageMin = ChargeDamageMin,
                DamageMax = ChargeDamageMax,
                Radius = ChargeRadius,
                Comment = comment
            }, out var why);

            if (bomb == null)
                Logger.WriteLog(LogType.Error, $"Energy Weapon Center {map.InstanceId}: no charge at {at}: {why}.");

            return bomb;
        }

        /// <summary>A tube's reactor: its alarm light, and its fire, which goes off only as the beam fires.</summary>
        private static Tube TubeOf(MapChannel map, string name, float z, long chargesAt, Vector3 reactor)
        {
            var tube = new Tube { Name = name, Z = z, ChargesAt = chargesAt };

            tube.Light = PlacedUsables.PutDown(map, new WorldUsableEntry
            {
                ClassId = C.AlarmLightClass, PosX = reactor.X, PosY = reactor.Y, PosZ = reactor.Z,
                Comment = $"Energy Weapon Center: the {name} reactor's alarm light"
            }, out _);

            tube.Fire = PlacedUsables.PutDown(map, new WorldUsableEntry
            {
                ClassId = C.ReactorFireClass, PosX = reactor.X, PosY = reactor.Y, PosZ = reactor.Z,
                DamageMin = 1, DamageMax = 1, Radius = 0.1, Comment = $"Energy Weapon Center: the {name} reactor firing"
            }, out _);

            // It goes off when the beam fires, and not of itself.
            if (tube.Fire != null)
                tube.Fire.NextBlastAt = long.MaxValue;

            return tube;
        }

        private static void Add(State state, Kind kind, int index, DynamicObject obj) =>
            state.Usables.Add(new Usable { Kind = kind, Index = index, Object = obj });

        /// <summary>A usable of the copy.</summary>
        private static DynamicObject Place(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, position, rotation, key, true, UseWindupMs);
            obj.StateId = initial;
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        /// <summary>What is destroyed in the facility stays so: it is never due back.</summary>
        private static void KeepDown(State state)
        {
            foreach (var live in state.Tanks.Concat(state.Coils).Concat(new[]
                     {
                         state.TunnelField, state.LabField, state.CommandField, state.Generator
                     }))
                if (live != null && live.IsDown && live.RespawnAt != long.MaxValue)
                    live.RespawnAt = long.MaxValue;
        }

        #endregion

        #region Use

        /// <summary>Whether a finished use is of one of the copy's usables (ActorActionManager.PerformRecovery).</summary>
        public static bool IsUseOf(MapChannel map, ActionData action) =>
            action != null && StateOf(map) is { } state && state.Usables.Any(usable => usable.Object.EntityId == action.SourceId);

        /// <summary>
        /// A use about to start (DynamicObjectManager.RequestUseObjectPacket): the message it is
        /// refused with, or null. The testing station is for a member on Tesla Inversion without
        /// the datablade; the console wants the datablade, once; the body has nothing for one who
        /// holds three charges.
        /// </summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var state = StateOf(client?.Player?.MapChannel);
            var usable = UsableOf(state, obj);

            if (usable == null)
                return null;

            switch (usable.Kind)
            {
                case Kind.Datablade when !client.Player.Missions.ContainsKey(C.TeslaInversion) || Datablades(client) > 0:
                    return PlayerMessage.PmUseObjectNotUsable;
                case Kind.Console when state.Inverted:
                    return PlayerMessage.PmUseObjectNotUsable;
                case Kind.Console when Datablades(client) == 0:
                    return PlayerMessage.PmMissingReqItem;
                case Kind.Corpse when Charges(client) >= C.Tanks:
                    return PlayerMessage.PmUseObjectNotUsable;
                default:
                    return null;
            }
        }

        /// <summary>
        /// A use about to start of one of the copy's placed usables (PlacedUsables.RefusalOf): a
        /// charge set at a tank cannot be taken out again, and one at a tank blown is spent.
        /// </summary>
        public static PlayerMessage? RefusalOf(Client client, PlacedUsables.Live live)
        {
            var state = StateOf(live?.Map);

            if (state == null)
                return null;

            var i = Array.IndexOf(state.Charges, live);

            if (i < 0)
                return null;

            return live.State != UseObjectState.BombStateDisarmed || state.Tanks[i] is { IsDown: true } ? PlayerMessage.PmUseObjectNotUsable : null;
        }

        /// <summary>A finished use of one of the copy's usables: what it does.</summary>
        public static void UseRecovery(MapChannel map, ActionData action)
        {
            var state = StateOf(map);
            var usable = state?.Usables.FirstOrDefault(entry => entry.Object.EntityId == action.SourceId);

            if (usable == null)
                return;

            var obj = usable.Object;
            var client = obj.TriggeredByPlayers.FirstOrDefault(c => c.Player == action.Actor);

            if (client == null)
                return;

            obj.TriggeredByPlayers.Remove(client);

            if (action.IsInrerrupted || Refusal(client, obj) != null)
                return;

            if (!MapInstanceScope.Share(map, client.Player, obj) ||
                Vector3.Distance(client.Player.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} was no longer at object {obj.EntityId} when the use finished; nothing done.");
                return;
            }

            var now = Now();

            switch (usable.Kind)
            {
                case Kind.Datablade:
                    Give(client, C.DatabladeTemplate, 1);
                    CommunicatorManager.Instance.SystemMessage(client, "You pull a datablade from the testing station.");
                    break;

                case Kind.Console:
                    if (InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)C.DatabladeClass, 1) != 0)
                        return;
                    Invert(map, state, client, now);
                    break;

                case Kind.Monitor:
                    Show(map, obj, obj.StateId == UseObjectState.TsState0 ? UseObjectState.TsState1 : UseObjectState.TsState0, client.Player.EntityId);
                    state.NetworkAccessed = true;
                    CommunicatorManager.Instance.SystemMessage(client,
                        "The security feed: Demolitionist Engelsen's body lies on the crate stacks of the warehouse's storeroom, his charges still on him.");
                    Record(state, client, C.LookBeforeYouSabotage, C.NetworkAccessed, now);
                    break;

                case Kind.Corpse:
                    Give(client, C.ChargeTemplate, C.Tanks - Charges(client));
                    state.ChargesFound = true;
                    CommunicatorManager.Instance.SystemMessage(client, "You take Demolitionist Engelsen's explosive charges.");
                    Record(state, client, C.LookBeforeYouSabotage, C.ChargesFound, now);
                    break;
            }
        }

        private static Usable UsableOf(State state, DynamicObject obj) =>
            state == null || obj == null ? null : state.Usables.FirstOrDefault(usable => ReferenceEquals(usable.Object, obj));

        private static uint Datablades(Client client) =>
            InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.DatabladeClass, includeMissionItems: true);

        private static uint Charges(Client client) =>
            InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.ChargeClass, includeMissionItems: true);

        private static void Show(MapChannel map, DynamicObject obj, UseObjectState next, ulong actor)
        {
            obj.StateId = next;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actor, next, 0));
        }

        private static void Give(Client client, uint template, uint count)
        {
            if (count == 0)
                return;

            var item = ItemManager.Instance.CreateFromTemplateId(template, count);

            if (item != null)
                InventoryManager.Instance.GrantItemToInventory(client, item);
        }

        #endregion

        #region It's All In the Timing

        /// <summary>Whether the copy's reactors are down, for a test or a GM.</summary>
        public static bool ReactorsDown(MapChannel map) => StateOf(map)?.ReactorsDown == true;

        /// <summary>
        /// A hit on one of the server's destructibles (PracticeTargetManager.RecordHit): the
        /// damage it takes. The primary generator takes Lightning <see cref="LightningTimes"/> times.
        /// </summary>
        public static int Struck(MapChannel map, DynamicObject target, int damage, DamageType damageType)
        {
            var state = StateOf(map);

            if (state?.Generator == null || damageType != DamageType.Electrical || !ReferenceEquals(target, state.Generator.Object))
                return damage;

            return (int)Math.Min(int.MaxValue, (long)damage * LightningTimes);
        }

        private static void Generator(MapChannel map, State state, List<Client> members)
        {
            if (!state.ReactorsDown && state.Generator is { IsDown: true })
                ShutDown(map, state, members);
        }

        /// <summary>The generator down (or a squad's who has done it before): the reactors stop, and the lab's two ways open.</summary>
        private static void ShutDown(MapChannel map, State state, List<Client> members)
        {
            state.ReactorsDown = true;

            if (state.Generator != null && !state.Generator.IsDown)
                PlacedDestructibles.Destroy(state.Generator, null);

            foreach (var field in new[] { state.TunnelField, state.LabField })
                if (field != null && !field.IsDown)
                    PlacedDestructibles.Destroy(field, null);

            foreach (var tube in state.Tubes)
            {
                tube.Phase = Phase.Idle;
                Light(tube, false);
            }

            KeepDown(state);

            foreach (var member in members)
                CommunicatorManager.Instance.SystemMessage(member, "The primary generator is down: both plasma reactors go dark, and the force fields to the research lab fail.");

            Logger.WriteLog(LogType.Debug, $"Energy Weapon Center {map.InstanceId}: the generator is down; the reactors are off and the lab is open.");
        }

        /// <summary>Where a tube is in its cycle at the time.</summary>
        public static Phase PhaseOf(long sinceFrom, long chargesAt)
        {
            var t = ((sinceFrom - chargesAt) % CycleMs + CycleMs) % CycleMs;

            return t < ChargeMs ? Phase.Charging : t < ChargeMs + FireMs ? Phase.Firing : Phase.Idle;
        }

        /// <summary>Whether a point is in a tube's beam: its length, near its axis, above its pits.</summary>
        public static bool InBeam(float tubeZ, Vector3 at) =>
            at.X >= C.TubeWest && at.X <= C.TubeEast && Math.Abs(at.Z - tubeZ) <= BeamHalfWidth && at.Y >= BeamFloor && at.Y <= BeamTop;

        private static bool AboutTheTubes(Vector3 at) =>
            at.X >= TubesArea.MinX && at.X <= TubesArea.MaxX && at.Z >= TubesArea.MinZ && at.Z <= TubesArea.MaxZ;

        private static void Reactors(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.ReactorsDown)
                return;

            foreach (var tube in state.Tubes)
            {
                var phase = PhaseOf(now - state.CycleFrom, tube.ChargesAt);

                if (phase != tube.Phase)
                {
                    tube.Phase = phase;

                    switch (phase)
                    {
                        case Phase.Charging:
                            Light(tube, true);
                            foreach (var member in members.Where(member => AboutTheTubes(member.Player.Position)))
                                CommunicatorManager.Instance.SystemMessage(member, $"Warning: the {tube.Name} plasma reactor is powering up!");
                            break;
                        case Phase.Firing:
                            tube.NextPulseAt = now;
                            break;
                        default:
                            Light(tube, false);
                            break;
                    }
                }

                if (tube.Phase == Phase.Firing && now >= tube.NextPulseAt)
                {
                    tube.NextPulseAt = now + PulseMs;
                    Fire(map, tube, members);
                }
            }
        }

        private static void Light(Tube tube, bool on)
        {
            var state = on ? UseObjectState.TsState1 : UseObjectState.TsState0;

            if (tube.Light != null && tube.Light.State != state)
                PlacedUsables.SetState(tube.Light, state, 0);
        }

        /// <summary>The reactor fires down its tube: its fire, and the beam on every living player in it.</summary>
        private static void Fire(MapChannel map, Tube tube, List<Client> members)
        {
            var fire = tube.Fire;

            if (fire == null)
                return;

            CellManager.Instance.CellCallMethod(map, fire.Object, new UsePacket(0, UseObjectState.ReState0, 0));
            fire.RearmAt = Now() + PlacedUsables.RearmMs;

            var packet = new PerformObjectDamagePacket(fire.Placement.ActionArg, fire.Object.EntityId);

            foreach (var client in members)
            {
                var player = client.Player;

                if (player.State == CharacterState.Dying || !InBeam(tube.Z, player.Position) ||
                    !player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                    continue;

                var amount = GameEffectManager.ApplyResist(player, BeamDamage, out var resisted, DamageType.Laser);
                var taken = ActorManager.Instance.Damage(map, player, amount, null, out var outcome, DamageType.Laser);

                packet.Hits.Add(new PerformObjectDamagePacket.Hit
                {
                    TargetId = player.EntityId,
                    DamageType = DamageType.Laser,
                    Amount = outcome.Delivered,
                    Resisted = resisted,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    DeathBlow = taken > 0 && player.Attributes.TryGetValue(Attributes.Health, out var after) && after.Current <= 0
                });
            }

            CellManager.Instance.CellCallMethod(map, fire.Object, packet);
        }

        #endregion

        #region Tesla Inversion

        /// <summary>Whether the copy's coils have been turned, for a test or a GM.</summary>
        public static bool Inverted(MapChannel map) => StateOf(map)?.Inverted == true;

        /// <summary>The datablade in the console: the alarm, and the coils on their makers.</summary>
        private static void Invert(MapChannel map, State state, Client by, long now)
        {
            state.Inverted = true;
            state.InvertedAt = now;
            state.InvertedBy = by.Player.EntityId;
            state.NextZapAt = now;

            if (state.Usables.FirstOrDefault(usable => usable.Kind == Kind.Console) is { } console)
                Show(map, console.Object, UseObjectState.IaStateAccepted, by.Player.EntityId);

            WakeOnce(map, state, C.LabAlarmPool);
            Record(state, by, C.TeslaInversion, C.Inverted, now);

            foreach (var member in Members(map))
                CommunicatorManager.Instance.SystemMessage(member,
                    "Alarms sound through the facility! Out on the test range the Tesla coil prototypes turn on their makers.");

            Logger.WriteLog(LogType.Debug, $"Energy Weapon Center {map.InstanceId}: {by.Player.FamilyName} turns the coils.");
        }

        /// <summary>Turned, the coils zap the Bane about them until they burn out.</summary>
        private static void Coils(MapChannel map, State state, List<Client> members, long now)
        {
            if (!state.Inverted || state.CoilsBurnt)
                return;

            if (now - state.InvertedAt >= InversionMs)
            {
                BurnOut(map, state, members);
                return;
            }

            if (now < state.NextZapAt)
                return;

            state.NextZapAt = now + ZapEveryMs;

            var bane = Creatures(map).Where(creature => Array.IndexOf(C.Hostiles, creature.DbId) >= 0).ToList();

            // A kill is whoever turned the coils', while they are in the copy; the world's otherwise.
            var by = members.FirstOrDefault(client => client.Player.EntityId == state.InvertedBy)?.Player;

            foreach (var coil in state.Coils.Where(coil => !coil.IsDown))
                foreach (var creature in bane.Where(creature => Alive(creature) &&
                             Vector2.Distance(new Vector2(creature.Position.X, creature.Position.Z), new Vector2(coil.Object.Position.X, coil.Object.Position.Z)) <= ZapRadius &&
                             Math.Abs(creature.Position.Y - coil.Object.Position.Y) <= 16f))
                    Zap(map, state, coil.Object, creature, by);
        }

        /// <summary>
        /// A jolt from a coil: from nobody, so nobody is in a fight for it and the Bane turn on
        /// nobody; at none left, the creature is killed, as <paramref name="by"/>'s kill.
        /// </summary>
        private static void Zap(MapChannel map, State state, DynamicObject coil, Creature creature, Manifestation by)
        {
            if (!state.Zapped.TryGetValue(creature.EntityId, out var zap) || !creature.ActiveEffects.ContainsKey(zap.EffectId))
            {
                zap = new GameEffect
                {
                    TypeId = ZapEffect,
                    EffectId = GameEffectManager.Instance.NextEffectId(map),
                    EffectLevel = TeslaCoils.EffectLevel,
                    SourceId = coil.EntityId,
                    IsBuff = false,
                    Environmental = true
                };

                GameEffectManager.Instance.Attach(map, creature, zap);
                state.Zapped[creature.EntityId] = zap;
            }

            var rolled = (int)Math.Clamp(Roll(ZapMin, ZapMax), ZapMin, ZapMax);
            var taken = ActorManager.Instance.Damage(map, creature, rolled, null, out var outcome, DamageType.Electrical, isPeriodic: true);
            var dead = creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0;

            if (creature.ActiveEffects.ContainsKey(zap.EffectId))
                Tick(map, creature, zap, outcome, taken > 0 && dead);

            if (dead && creature.State != CharacterState.Dead)
                CreatureManager.Instance.HandleCreatureKill(map, creature, by);
        }

        private static void Tick(MapChannel map, Creature creature, GameEffect zap, DamageOutcome outcome, bool deathBlow)
        {
            var tick = new GameEffectTickPacket(zap.EffectId, GameEffectTickPacket.TickKind.Damage);

            tick.Entries.Add(new TickEntry
            {
                EntityId = creature.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                DamageType = DamageType.Electrical,
                DeathBlow = deathBlow
            });

            CellManager.Instance.CellCallMethod(map, creature, tick);
        }

        /// <summary>The coils overloaded: they blow, and the zaps come off whoever they were on.</summary>
        private static void BurnOut(MapChannel map, State state, List<Client> members)
        {
            state.CoilsBurnt = true;

            foreach (var coil in state.Coils.Where(coil => !coil.IsDown))
                PlacedDestructibles.Destroy(coil, null);

            foreach (var (entityId, zap) in state.Zapped)
                if (EntityManager.Instance.Creatures.TryGetValue(entityId, out var creature) && creature.ActiveEffects.ContainsKey(zap.EffectId))
                    GameEffectManager.Instance.DettachEffect(map, creature, zap);

            state.Zapped.Clear();
            KeepDown(state);

            foreach (var member in members)
                CommunicatorManager.Instance.SystemMessage(member, "The Tesla coil prototypes overload and burn out.");
        }

        #endregion

        #region Look Before You Sabotage

        /// <summary>How many of the fuel tanks are blown, for a test or a GM.</summary>
        public static int TanksBlown(MapChannel map) => StateOf(map)?.Tanks.Count(tank => tank is { IsDown: true }) ?? 0;

        /// <summary>A charge set is its tank's count; gone off, it takes its tank with it; the three blown bring the guards.</summary>
        private static void Tanks(MapChannel map, State state, List<Client> members)
        {
            for (var i = 0; i < C.Tanks; i++)
            {
                var charge = state.Charges[i];
                var tank = state.Tanks[i];

                if (charge == null)
                    continue;

                if (charge.State is UseObjectState.BombStateArmed or UseObjectState.BombStateDetonated)
                    state.Armed[i] = true;

                if (charge.State == UseObjectState.BombStateDetonated && tank is { IsDown: false })
                {
                    var by = charge.ArmedBy != 0 && EntityManager.Instance.Players.TryGetValue(charge.ArmedBy, out var player) ? player : null;
                    PlacedDestructibles.Destroy(tank, by);
                }
            }

            if (state.WarehouseAlarmed || state.Tanks.Any(tank => tank is not { IsDown: true }))
                return;

            state.WarehouseAlarmed = true;
            WakeOnce(map, state, C.WarehouseAlarmPool);

            foreach (var member in members)
                CommunicatorManager.Instance.SystemMessage(member, "The fuel reserves go up! Bane guards come running to the warehouse.");
        }

        #endregion

        #region The Strongest Link

        /// <summary>The copy's Casso, alive; null if he is not.</summary>
        public static Creature CassoOf(MapChannel map) => Creatures(map).FirstOrDefault(creature => creature.DbId == C.Casso);

        /// <summary>Whether the copy's Casso is still tied to his minions, for a test or a GM.</summary>
        public static bool CassoLinked(MapChannel map) => StateOf(map)?.CassoLinked == true;

        private static void Casso(MapChannel map, State state, List<Client> members, long now)
        {
            var casso = CassoOf(map);

            if (casso != null)
                state.Casso = casso;
            else if (state.Casso != null && !Alive(state.Casso))
            {
                state.Casso = null;
                Defeated(map, state, members);
            }

            var linked = C.MinionPools.Any(id => Pool(map, id) is { } pool && !Cleared(pool));

            if (linked != state.CassoLinked)
            {
                state.CassoLinked = linked;

                if (!linked && casso != null)
                    foreach (var member in members.Where(member => Vector3.Distance(member.Player.Position, casso.Position) <= ChamberRadius))
                        CommunicatorManager.Instance.SystemMessage(member, "Casso's link to his minions is broken: he can be hurt now!");
            }

            if (casso == null)
                return;

            casso.ImmuneToAllDamage = linked;

            if (!linked || now - state.LinkToldAt < LinkToldEveryMs)
                return;

            var near = members.Where(member => Vector3.Distance(member.Player.Position, casso.Position) <= ChamberRadius).ToList();

            if (near.Count == 0)
                return;

            state.LinkToldAt = now;

            foreach (var member in near)
                CommunicatorManager.Instance.SystemMessage(member,
                    "Linker Commander Casso is tied into the base's network: nothing hurts him while his minions stand.");
        }

        /// <summary>Casso dead: the objective, and the way back south.</summary>
        private static void Defeated(MapChannel map, State state, List<Client> members)
        {
            state.CassoDown = true;
            OpenCommand(state);

            foreach (var member in members)
                CommunicatorManager.Instance.SystemMessage(member, "Linker Commander Casso is dead. The force field to the cross fails: report to Sergeant Wolfe.");

            Logger.WriteLog(LogType.Debug, $"Energy Weapon Center {map.InstanceId}: Casso is dead.");
        }

        private static void OpenCommand(State state)
        {
            state.CommandOpen = true;

            if (state.CommandField != null && !state.CommandField.IsDown)
                PlacedDestructibles.Destroy(state.CommandField, null);

            KeepDown(state);
        }

        /// <summary>
        /// Whether a pool is cleared: in a squad instance, dead by its clock (which is kept);
        /// otherwise spawned and nothing of it left. One that has nowhere to stand is cleared.
        /// </summary>
        private static bool Cleared(SpawnPool pool)
        {
            if (pool.AliveCreatures > 0 || pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                return false;

            return pool.NoWalkableGround || pool.ClearedAtUtcMs != 0 || pool.HasSpawned;
        }

        #endregion

        #region Catching up

        /// <summary>A squad whose member has shut the reactors down, or beaten Casso, has those ways open again.</summary>
        private static void CatchUpWays(MapChannel map, State state, List<Client> members)
        {
            if (!state.ReactorsDown && members.Any(client => Done(client.Player, C.AllInTheTiming, C.ShutDownReactors)))
                ShutDown(map, state, members);

            if (!state.CommandOpen && (state.CassoDown || members.Any(client => Done(client.Player, C.TheStrongestLink, C.DefeatCasso))))
                OpenCommand(state);
        }

        /// <summary>What the copy has done, to every member waiting on it.</summary>
        private static void Progress(State state, List<Client> members, long now)
        {
            foreach (var client in members)
            {
                var player = client.Player;

                if (state.ReactorsDown && Wants(player, C.AllInTheTiming, C.ShutDownReactors))
                    Record(state, client, C.AllInTheTiming, C.ReactorsDown, now);

                if (state.Inverted && Wants(player, C.TeslaInversion, C.TriggerInversion))
                    Record(state, client, C.TeslaInversion, C.Inverted, now);

                if (state.NetworkAccessed && Wants(player, C.LookBeforeYouSabotage, C.AccessNetwork))
                    Record(state, client, C.LookBeforeYouSabotage, C.NetworkAccessed, now);

                if (state.ChargesFound && Wants(player, C.LookBeforeYouSabotage, C.RetrieveCharges))
                    Record(state, client, C.LookBeforeYouSabotage, C.ChargesFound, now);

                if (Wants(player, C.LookBeforeYouSabotage, C.PlaceCharges))
                    for (uint tank = 0; tank < C.Tanks; tank++)
                        if (state.Armed[tank])
                            Record(state, client, C.LookBeforeYouSabotage, C.FirstTank + tank, now, C.TanksScenario);

                if (state.CassoDown && Wants(player, C.TheStrongestLink, C.DefeatCasso))
                    Record(state, client, C.TheStrongestLink, C.CassoDefeated, now);
            }
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

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

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>Whether the player has done the objective: the mission done, or the objective in it.</summary>
        private static bool Done(Manifestation player, uint missionId, uint objectiveId) =>
            player.MissionHistory.TryGetValue(missionId, out var history) && history is MissionState.Completed or MissionState.Success ||
            player.Missions.TryGetValue(missionId, out var log) &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Completed;

        /// <summary>The event to the player's mission: once for an assignment, or again a while after one that changed nothing.</summary>
        private static void Record(State state, Client client, uint missionId, uint eventId, long now, uint scenarioId = C.Scenario)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = $"{client.Player.Id}:{log.AssignmentId}:{missionId}:{scenarioId}:{eventId}";

            if (state.Given.TryGetValue(key, out var retryAt) && now < retryAt)
                return;

            state.Given[key] = MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId)
                ? long.MaxValue
                : now + RetryMs;
        }

        /// <summary>The navmesh under a spot, if it is near enough; the spot as it is if not.</summary>
        private static Vector3 Ground(MapChannel map, Vector3 spot) => NavMeshManager.SnapToGround(map, spot);

        /// <summary>Alive and still in the world as itself.</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        #endregion

        #region A GM's

        /// <summary>
        /// .ewc: what this copy has done, and a GM's hand in it.
        ///   .ewc            - the reactors, the coils, the tanks, Casso
        ///   .ewc generator  - the generator down: the reactors off, the lab's ways open
        ///   .ewc invert     - the coils turned on the Bane
        ///   .ewc tanks      - the three fuel tanks blown
        ///   .ewc casso      - Casso's link broken
        /// </summary>
        public static IEnumerable<string> Command(MapChannel map, Manifestation gm, string[] parts)
        {
            var state = StateOf(map);

            if (state == null)
            {
                yield return "Not in the Energy Weapon Center.";
                yield break;
            }

            var now = Now();

            if (!state.Set)
                SetUp(map, state, now);

            var members = Members(map);

            switch (parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty)
            {
                case "generator":
                    if (!state.ReactorsDown)
                        ShutDown(map, state, members);
                    break;

                case "invert":
                    if (!state.Inverted && members.FirstOrDefault(client => ReferenceEquals(client.Player, gm)) is { } client)
                        Invert(map, state, client, now);
                    break;

                case "tanks":
                    foreach (var tank in state.Tanks.Where(tank => tank is { IsDown: false }))
                        PlacedDestructibles.Destroy(tank, gm);
                    for (var i = 0; i < C.Tanks; i++)
                        state.Armed[i] = true;
                    Tanks(map, state, members);
                    break;

                case "casso":
                    foreach (var id in C.MinionPools)
                        if (Pool(map, id) is { } pool)
                            foreach (var minion in Creatures(map).Where(creature => creature.SpawnPool == pool).ToList())
                                CreatureManager.Instance.HandleCreatureKill(map, minion, gm);
                    break;
            }

            var cycle = state.Tubes.Select(tube => $"{tube.Name} {PhaseOf(now - state.CycleFrom, tube.ChargesAt)}");

            yield return $"Energy Weapon Center {map.InstanceId}: reactors {(state.ReactorsDown ? "down" : string.Join(", ", cycle))}; " +
                $"coils {(state.CoilsBurnt ? "burnt out" : state.Inverted ? "turned" : "standing")}.";
            yield return $"Monitors {(state.NetworkAccessed ? "used" : "unused")}, Engelsen's charges {(state.ChargesFound ? "taken" : "on him")}, " +
                $"tanks {state.Armed.Count(armed => armed)}/{C.Tanks} set, {state.Tanks.Count(tank => tank is { IsDown: true })} blown.";
            yield return $"Casso {(state.CassoDown ? "dead" : state.CassoLinked ? "linked to his minions" : "unlinked")}; " +
                $"north branch {(state.CommandOpen ? "open" : "shut")}.";
        }

        #endregion

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();
    }
}
