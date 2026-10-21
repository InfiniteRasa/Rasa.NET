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
    using Packets;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Models;
    using Services.Preloader;
    using Structures;

    using H = Services.Preloader.OjasaHiveContent;

    /// <summary>
    /// What the Ojasa Atta Hive (1865) does that its rows do not: each copy of it - a squad's
    /// instance, or the map's own channel - runs its story for whoever is in it
    /// (OjasaHiveContent has the people, the Atta and the missions).
    ///
    ///  - Its usables: the Brann corpse by the entrance (Something Smells), the two rusty bins of
    ///    the Brann camp (Brann Bacon), the Shocking Device by the eggs (Atta Ought To Do It).
    ///    The corpse and the bins are searched as any usable a mission wants is
    ///    (DynamicObjectManager.FootlockerRecovery); the device is this: a use shocks it, two
    ///    Young Atta hatch from the eggs, both dead is the objective for everyone on it, and it
    ///    can be used again <see cref="DeviceRearmMs"/> later once they are.
    ///  - John: at the camp, at the third cell's door, or in the Queen's chamber, where the story
    ///    of the squad member furthest behind is - the camp until Brann Bacon, the door until Atta
    ///    Ought To Do It is done, the chamber after. The hive's cells are not joined on the
    ///    navmesh (the bridges are not walkable to it), so he is put there, not walked; never in
    ///    a fight.
    ///  - Fried Atta's flamethrower test: a member on Protect John within
    ///    <see cref="WaveTriggerRadius"/> of him at the camp sets it off - six Atta from the
    ///    tunnel, at him. All of them dead with John above <see cref="JohnFailFraction"/> of his
    ///    health is the objective, for everyone on it; John lower, or the wave not done in
    ///    <see cref="WaveTimeoutMs"/>, and it is called off, John healed, and it can be set off
    ///    again <see cref="WaveRearmMs"/> later.
    ///  - Long Live the Queen: a member on Check on the Queen within <see cref="QueenRadius"/> of
    ///    her throne, up in the chamber, sees her (camera script 1) and has checked. The Atta and
    ///    Brann below her are brawling from then on, kept at it until a player's first hit on
    ///    one of them takes the other side: that side goes Friendly, the other Hostile and after
    ///    the hitter, and the choice is everyone's in the copy. The side taken winning is its
    ///    objective done; losing, the brawl starts again <see cref="BrawlResetMs"/> later.
    ///    Members who come later are given the copy's choice and outcome. Taking the Atta's side
    ///    turns the hive's Atta Friendly; when someone hands Long Live the Queen in, John turns on
    ///    those about him (<see cref="JohnCleanupRadius"/>).
    ///  - The sulfur lakes: a player in one for <see cref="LakeRescueMs"/> - their floors are
    ///    walkable and joined to nothing, so /stuck finds only the lake floor - is put back where
    ///    they last stood dry, or at the entrance.
    /// The guide's two bugs are not reproduced: the lakes are this, and entering the chamber
    /// before Long Live the Queen no longer loses the brawl, which waits for the mission.
    /// </summary>
    public static class OjasaHive
    {
        public const float WaveTriggerRadius = 30f;
        public const float JohnFailFraction = 0.15f;
        public const long WaveTimeoutMs = 300_000;
        public const long WaveRearmMs = 30_000;
        public const float QueenRadius = 70f;
        public const float QueenMinHeight = 100f;
        public const uint QueenCameraScript = 1;
        public const long DeviceShockMs = 2_000;
        public const long DeviceRearmMs = 60_000;
        public const long BrawlResetMs = 30_000;
        public const float JohnCleanupRadius = 60f;
        public const long LakeRescueMs = 8_000;
        public const float LakeAbove = 0.5f, LakeBelow = 3f;
        public const int UseWindupMs = 3_000;

        /// <summary>Where John is.</summary>
        public enum Stage { Camp, CellThree, Chamber }

        /// <summary>The side taken at the Queen.</summary>
        public enum Side { None, Atta, Brann }

        internal enum DeviceStage { Idle, Shocking, Spent }

        /// <summary>A sulfur lake: the world (x, z) of its footprint's far corner, its size, and its surface.</summary>
        public readonly struct Lake
        {
            public Lake(float x, float z, float size, float surface)
            {
                X = x;
                Z = z;
                Size = size;
                Surface = surface;
            }

            public float X { get; }
            public float Z { get; }
            public float Size { get; }
            public float Surface { get; }

            /// <summary>Whether a position is in it: over its footprint, at its surface or a little under.</summary>
            public bool Holds(Vector3 position) =>
                position.X <= X && position.X >= X - Size && position.Z <= Z && position.Z >= Z - Size &&
                position.Y <= Surface + LakeAbove && position.Y >= Surface - LakeBelow;
        }

        /// <summary>The hive's TerraAriekiSulfurPond placements (8056, 8058, 8057: 128, 64 and 32 m).</summary>
        public static readonly Lake[] Lakes =
        {
            new(28f, 70f, 128, 40f), new(28f, 198f, 128, 40f), new(155.5f, 70.6f, 128, 40f), new(155.5f, 198.6f, 128, 40f),
            new(220f, 402f, 32, 64f), new(364f, 168f, 64, 66f), new(364f, 232f, 64, 66f), new(372f, 118f, 64, 66f),
            new(436f, 118f, 64, 66f), new(436f, 182f, 64, 66f), new(-390f, -258f, 32, 90f), new(-397.9f, -261.5f, 64, 93.7f),
            new(-391.1f, -324.8f, 64, 93.7f), new(-334.2f, -257.5f, 64, 93.7f), new(-332.9f, -310.4f, 64, 93.7f), new(244f, 332f, 128, 94f),
            new(244f, 364f, 32, 94f), new(276f, 268f, 32, 94f), new(276f, 300f, 32, 94f), new(276f, 332f, 32, 94f),
            new(276f, 364f, 32, 94f), new(210f, -280f, 128, 98f), new(210f, -152f, 128, 98f), new(274f, -88f, 64, 98f),
            new(306f, -152f, 128, 98f), new(338f, -280f, 128, 98f), new(-374f, -394f, 128, 100f), new(-310f, -266f, 32, 100f),
            new(-278f, -298f, 32, 100f), new(-278f, -266f, 32, 100f), new(-246f, -298f, 32, 100f), new(-246f, -266f, 32, 100f),
            new(-246f, -138f, 128, 100f), new(-246f, -10f, 128, 100f), new(-214f, -106f, 32, 100f), new(-182f, -106f, 32, 100f),
            new(-118f, -394f, 128, 100f), new(-118f, -266f, 128, 100f), new(-118f, -138f, 128, 100f), new(-316f, -384f, 64, 104.8f)
        };

        /// <summary>The flamethrower test's Atta: where each comes out of the tunnel.</summary>
        private static readonly int[] WaveFrom = { 0, 0, 1, 1, 2, 3 };

        /// <summary>One copy of the hive, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public DynamicObject Corpse;
            public readonly List<DynamicObject> Bins = new();
            public DynamicObject Device;

            public Stage JohnStage = Stage.Camp;

            public readonly List<Creature> Wave = new();
            public long WaveStartedAt;
            public long WaveRearmAt;

            public DeviceStage DeviceStep = DeviceStage.Idle;
            public long DeviceAt;
            public ulong DeviceUser;
            public readonly List<Creature> Young = new();

            public readonly List<Creature> BrawlAtta = new();
            public readonly List<Creature> BrawlBrann = new();
            public Side Chosen = Side.None;
            public bool Won;
            public long BrawlResetAt;

            /// <summary>The Atta John has turned on: left Neutral when the hive's go over.</summary>
            public readonly HashSet<ulong> Cleanup = new();

            public readonly Dictionary<uint, long> WetSince = new();
            public readonly Dictionary<uint, Vector3> Dry = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();
        private static readonly Random Random = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether a map channel is a copy of the hive.</summary>
        public static bool IsHive(MapChannel map) => map?.MapInfo?.MapContextId == H.Hive;

        internal static State StateOf(MapChannel map) => IsHive(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>The copy's side at the Queen, for a test or a GM.</summary>
        public static Side ChosenSide(MapChannel map) => StateOf(map)?.Chosen ?? Side.None;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsHive(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            var members = Members(map);

            if (members.Count == 0)
                return;

            MoveJohn(map, state, members);
            FlamethrowerTest(map, state, members, now);
            Device(map, state, now);
            Queen(map, state, members, now);
            AttaOnSide(map, state);
            Rescue(map, state, members, now);
        }

        #region Set up

        /// <summary>The copy's usables, put down once; ones it already has (a copy made of a copy) are its own.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;
            state.Corpse = Usable(map, "ojasa:brann-corpse", H.BrannCorpseClass, H.BrannCorpse, 2.6, UseObjectState.TdStateClosed);

            for (var i = 0; i < H.BrannBins.Length; i++)
                state.Bins.Add(Usable(map, $"ojasa:brann-bin-{i}", H.BrannBinClass, H.BrannBins[i], i == 0 ? 0.4 : 3.5, UseObjectState.TdStateClosed));

            state.Device = Usable(map, "ojasa:shocking-device", H.ShockingDeviceClass, H.ShockingDevice, 2.0, UseObjectState.ThreeStateSwithState0);
            state.Device.NameOverrideId = H.ShockingDeviceName;
        }

        private static DynamicObject Usable(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial)
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

        #endregion

        #region John

        /// <summary>Where a player's story wants John.</summary>
        public static Stage StageFor(Manifestation player)
        {
            if (Done(player, H.AttaOughtToDoIt) || Active(player, H.LongLiveTheQueen))
                return Stage.Chamber;

            if (Done(player, H.BrannBacon) || Active(player, H.BrannBacon) || Active(player, H.AttaOughtToDoIt))
                return Stage.CellThree;

            return Stage.Camp;
        }

        public static Vector3 SpotOf(Stage stage) => stage switch
        {
            Stage.CellThree => H.JohnAtCellThree,
            Stage.Chamber => H.JohnAtChamber,
            _ => H.JohnAtCamp
        };

        private static void MoveJohn(MapChannel map, State state, List<Client> members)
        {
            var stage = members.Select(client => StageFor(client.Player)).Min();
            var john = John(map);

            state.JohnStage = stage;

            if (john == null || state.Wave.Count > 0 || john.Hate.Ranked().Any())
                return;

            var spot = SpotOf(stage);

            if (Vector3.Distance(john.Position, spot) < 3f)
                return;

            var facing = Math.Atan2(H.Throne.X - spot.X, H.Throne.Z - spot.Z);

            if (stage == Stage.Camp)
                facing = Math.Atan2(H.Entrance.X - spot.X, H.Entrance.Z - spot.Z);

            if (john.SpawnPool != null)
            {
                john.SpawnPool.Position = spot;
                john.SpawnPool.Rotation = facing;
            }

            if (BehaviorManager.Instance.RestoreScriptedPose(map, john, spot, facing))
            {
                // Put there, and his own again: he stands, and fights what comes.
                john.Controller.ScriptedMove = null;
                john.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            }
        }

        /// <summary>John, alive and in this copy; null if he is not.</summary>
        public static Creature John(MapChannel map) => Creatures(map).FirstOrDefault(creature => creature.DbId == H.John);

        #endregion

        #region Fried Atta

        private static void FlamethrowerTest(MapChannel map, State state, List<Client> members, long now)
        {
            var john = John(map);

            if (state.Wave.Count == 0)
            {
                if (john == null || now < state.WaveRearmAt || state.JohnStage != Stage.Camp)
                    return;

                if (!members.Any(client => Wants(client.Player, H.FriedAtta, 2) &&
                        Vector3.Distance(client.Player.Position, john.Position) <= WaveTriggerRadius))
                    return;

                state.WaveStartedAt = now;

                foreach (var from in WaveFrom)
                {
                    var atta = Spawn(map, H.WaveAtta, H.WaveSpawns[from], 2.5f, john.Position);

                    if (atta == null)
                        continue;

                    atta.Hate.Ensure(john.EntityId, Threat.NoticedThreat);
                    state.Wave.Add(atta);
                }

                Logger.WriteLog(LogType.Debug, $"Ojasa hive {map.InstanceId}: John's flamethrower test, {state.Wave.Count} Atta.");
                return;
            }

            var health = john != null && john.Attributes.TryGetValue(Attributes.Health, out var h) ? h : null;

            if (john == null || health == null || health.Current < health.CurrentMax * JohnFailFraction || now - state.WaveStartedAt > WaveTimeoutMs)
            {
                foreach (var atta in state.Wave.Where(Alive))
                    CellManager.Instance.RemoveCreatureFromWorld(map, atta);

                state.Wave.Clear();
                state.WaveRearmAt = now + WaveRearmMs;
                Heal(john);
                Logger.WriteLog(LogType.Debug, $"Ojasa hive {map.InstanceId}: John's flamethrower test called off.");
                return;
            }

            if (state.Wave.Any(Alive))
                return;

            state.Wave.Clear();
            state.WaveRearmAt = now + WaveRearmMs;
            Heal(john);

            foreach (var client in members.Where(client => Wants(client.Player, H.FriedAtta, 2)))
                Record(client, H.FriedAtta, H.TestScenario, H.WaveCleared);
        }

        #endregion

        #region Atta Ought To Do It

        /// <summary>
        /// A finished use of an object (DynamicObjectManager.FootlockerRecovery): true if it is the
        /// copy's Shocking Device, which this takes; it does nothing while it is still spent.
        /// </summary>
        public static bool Used(MapChannel map, Client client, DynamicObject obj)
        {
            var state = StateOf(map);

            if (state == null || obj == null || !ReferenceEquals(obj, state.Device) || client?.Player == null)
                return false;

            if (state.DeviceStep != DeviceStage.Idle)
                return true;

            state.DeviceStep = DeviceStage.Shocking;
            state.DeviceAt = Now() + DeviceShockMs;
            state.DeviceUser = client.Player.EntityId;
            Show(map, obj, UseObjectState.ThreeStateSwithState1, client.Player.EntityId);
            return true;
        }

        private static void Device(MapChannel map, State state, long now)
        {
            var device = state.Device;

            if (device == null || state.DeviceStep == DeviceStage.Idle)
                return;

            if (state.DeviceStep == DeviceStage.Shocking)
            {
                if (now < state.DeviceAt)
                    return;

                var user = EntityManager.Instance.Players.TryGetValue(state.DeviceUser, out var found) && found.MapChannel == map ? found : null;

                state.Young.Clear();

                for (var i = 0; i < 2; i++)
                {
                    var young = Spawn(map, H.YoungAtta, H.Eggs, 2f, user?.Position ?? device.Position);

                    if (young == null)
                        continue;

                    if (user != null && user.State != CharacterState.Dead)
                        young.Hate.Ensure(user.EntityId, Threat.NoticedThreat);

                    state.Young.Add(young);
                }

                state.DeviceStep = DeviceStage.Spent;
                state.DeviceAt = now + DeviceRearmMs;
                Show(map, device, UseObjectState.ThreeStateSwithState2, state.DeviceUser);
                return;
            }

            if (state.Young.Any(Alive))
                return;

            // Both dead: the objective, for everyone on it.
            if (state.Young.Count > 0)
            {
                foreach (var client in Members(map).Where(client => Wants(client.Player, H.AttaOughtToDoIt, 1)))
                    Record(client, H.AttaOughtToDoIt, H.DeviceScenario, H.HatchlingsDead);

                state.Young.Clear();
            }

            if (now < state.DeviceAt)
                return;

            state.DeviceStep = DeviceStage.Idle;
            Show(map, device, UseObjectState.ThreeStateSwithState0, state.DeviceUser);
        }

        private static void Show(MapChannel map, DynamicObject obj, UseObjectState next, ulong actor)
        {
            obj.StateId = next;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actor, next, 0));
        }

        #endregion

        #region Long Live the Queen

        private static void Queen(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var client in members.Where(client => Wants(client.Player, H.LongLiveTheQueen, 1)))
            {
                var at = client.Player.Position;

                if (at.Y < QueenMinHeight || Vector2.Distance(new Vector2(at.X, at.Z), new Vector2(H.Throne.X, H.Throne.Z)) > QueenRadius)
                    continue;

                CameraScripts.Run(client, QueenCameraScript);
                Record(client, H.LongLiveTheQueen, H.QueenScenario, H.QueenChecked);
            }

            if (state.BrawlResetAt != 0 && now >= state.BrawlResetAt)
                ResetBrawl(map, state);

            var deciding = members.Where(client => Wants(client.Player, H.LongLiveTheQueen, 4)).ToList();

            if (state.BrawlAtta.Count + state.BrawlBrann.Count == 0)
            {
                // A side taken stays taken: the copy's members have had their choice, and a brawl
                // the side lost starts again on the same sides.
                if (deciding.Count > 0 || state.Chosen != Side.None && !state.Won)
                    StartBrawl(map, state);

                if (state.Chosen != Side.None)
                    TakeSides(state, null);

                return;
            }

            if (state.Chosen == Side.None)
            {
                // Kept at it, and nobody the worse, until somebody takes a side.
                foreach (var atta in state.BrawlAtta.Where(Alive))
                {
                    Heal(atta);
                    foreach (var brann in state.BrawlBrann.Where(Alive))
                    {
                        atta.Hate.Ensure(brann.EntityId, Threat.NoticedThreat);
                        brann.Hate.Ensure(atta.EntityId, Threat.NoticedThreat);
                    }
                }

                foreach (var brann in state.BrawlBrann.Where(Alive))
                    Heal(brann);

                return;
            }

            var allies = state.Chosen == Side.Atta ? state.BrawlAtta : state.BrawlBrann;
            var opponents = state.Chosen == Side.Atta ? state.BrawlBrann : state.BrawlAtta;

            if (!state.Won && state.BrawlResetAt == 0)
            {
                if (!opponents.Any(Alive))
                    state.Won = true;
                else if (!allies.Any(Alive))
                    state.BrawlResetAt = now + BrawlResetMs;
            }

            // The copy's choice is everyone's in it, and its outcome.
            foreach (var client in deciding)
            {
                var choice = state.Chosen == Side.Atta ? H.ChoseAtta : H.ChoseBrann;
                var other = state.Chosen == Side.Atta ? 3U : 2U;

                if (Wants(client.Player, H.LongLiveTheQueen, other))
                    Record(client, H.LongLiveTheQueen, H.QueenScenario, choice);

                if (state.Won)
                    Record(client, H.LongLiveTheQueen, H.QueenScenario, state.Chosen == Side.Atta ? H.AttaWon : H.BrannWon);
            }
        }

        private static void StartBrawl(MapChannel map, State state)
        {
            foreach (var spot in H.BrawlAttaSpots)
                if (Spawn(map, H.BrawlAtta, spot, 0f, H.Brawl) is { } atta)
                    state.BrawlAtta.Add(atta);

            foreach (var spot in H.BrawlBrann)
                if (Spawn(map, H.BrannRaider, spot, 0f, H.Brawl) is { } brann)
                    state.BrawlBrann.Add(brann);

            Logger.WriteLog(LogType.Debug, $"Ojasa hive {map.InstanceId}: the Atta and the Brann are at it below the Queen.");
        }

        private static void ResetBrawl(MapChannel map, State state)
        {
            foreach (var creature in state.BrawlAtta.Concat(state.BrawlBrann).Where(Alive).ToList())
                CellManager.Instance.RemoveCreatureFromWorld(map, creature);

            state.BrawlAtta.Clear();
            state.BrawlBrann.Clear();
            state.BrawlResetAt = 0;
        }

        /// <summary>
        /// A creature took damage (CreatureManager.RecordCombatDamage): a player's first hit on
        /// one of the brawlers below the Queen takes the other side.
        /// </summary>
        public static void Hit(MapChannel map, Creature target, Actor source)
        {
            var state = StateOf(map);

            if (state == null || state.Chosen != Side.None || target == null || Pvp.Controller(source) is not Manifestation hitter)
                return;

            Side side;

            if (state.BrawlAtta.Contains(target))
                side = Side.Brann;
            else if (state.BrawlBrann.Contains(target))
                side = Side.Atta;
            else
                return;

            Choose(map, state, side, hitter);
        }

        private static void Choose(MapChannel map, State state, Side side, Manifestation hitter)
        {
            state.Chosen = side;
            TakeSides(state, hitter);
            Logger.WriteLog(LogType.Debug, $"Ojasa hive {map.InstanceId}: {hitter.FamilyName} takes the {side} side.");
        }

        /// <summary>The side taken Friendly, the other Hostile, and at each other - and at whoever chose, if anyone.</summary>
        private static void TakeSides(State state, Manifestation hitter)
        {
            var allies = state.Chosen == Side.Atta ? state.BrawlAtta : state.BrawlBrann;
            var opponents = state.Chosen == Side.Atta ? state.BrawlBrann : state.BrawlAtta;

            foreach (var ally in allies.Where(Alive))
                SetSide(ally, TargetCategory.Friendly);

            foreach (var opponent in opponents.Where(Alive))
            {
                SetSide(opponent, TargetCategory.Hostile);

                if (hitter != null)
                    opponent.Hate.Ensure(hitter.EntityId, Threat.NoticedThreat);
            }

            foreach (var ally in allies.Where(Alive))
                foreach (var opponent in opponents.Where(Alive))
                {
                    ally.Hate.Ensure(opponent.EntityId, Threat.NoticedThreat);
                    opponent.Hate.Ensure(ally.EntityId, Threat.NoticedThreat);
                }
        }

        /// <summary>The Atta have been helped: the hive's go over, those John has turned on aside.</summary>
        private static void AttaOnSide(MapChannel map, State state)
        {
            if (state.Chosen != Side.Atta)
                return;

            foreach (var creature in Creatures(map))
                if (creature.TargetCategory == TargetCategory.Hostile && H.HiveAtta.Contains(creature.DbId) &&
                    !state.Cleanup.Contains(creature.EntityId))
                    SetSide(creature, TargetCategory.Friendly);
        }

        /// <summary>
        /// A mission changed for a player (SceneApplication.MissionChanged): Long Live the Queen
        /// handed in after the Atta were helped, John turns on the Atta about him.
        /// </summary>
        public static void MissionChanged(Client client, uint missionId, string change)
        {
            var map = client?.Player?.MapChannel;
            var state = StateOf(map);

            if (state == null || missionId != H.LongLiveTheQueen || change != "Rewarded" || state.Chosen != Side.Atta)
                return;

            var john = John(map);

            if (john == null)
                return;

            foreach (var atta in Creatures(map).Where(creature =>
                         (H.HiveAtta.Contains(creature.DbId) || creature.DbId == H.BrawlAtta) &&
                         Vector3.Distance(creature.Position, john.Position) <= JohnCleanupRadius).ToList())
            {
                state.Cleanup.Add(atta.EntityId);
                SetSide(atta, TargetCategory.Neutral);
                atta.Hate.Ensure(john.EntityId, Threat.NoticedThreat);
                john.Hate.Ensure(atta.EntityId, Threat.NoticedThreat);
            }
        }

        private static void SetSide(Creature creature, TargetCategory category)
        {
            if (creature.TargetCategory == category)
                return;

            creature.TargetCategory = category;
            creature.Hate.Clear();
            CellManager.Instance.CellCallMethod(creature, new TargetCategoryPacket(category));
            BehaviorManager.Instance.StopFighting(creature);
        }

        #endregion

        #region The lakes

        public static bool InLake(Vector3 position) => Lakes.Any(lake => lake.Holds(position));

        private static void Rescue(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var client in members)
            {
                var player = client.Player;

                if (!InLake(player.Position))
                {
                    state.WetSince.Remove(player.Id);

                    // Stood on: the navmesh under them, not a jump over the water.
                    if (map.NavMesh?.GroundHeight(player.Position) is { } ground && Math.Abs(ground - player.Position.Y) <= SafetyFloor.GroundTolerance)
                        state.Dry[player.Id] = player.Position;

                    continue;
                }

                if (!state.WetSince.TryGetValue(player.Id, out var since))
                {
                    state.WetSince[player.Id] = now;
                    continue;
                }

                if (now - since < LakeRescueMs)
                    continue;

                state.WetSince.Remove(player.Id);

                var to = state.Dry.TryGetValue(player.Id, out var dry) ? dry : H.Entrance;

                player.PlaceAt(to);
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId,
                    new Movement(to, client.Movement?.ViewDirection ?? new Vector2(0f, 0f))), false);

                Logger.WriteLog(LogType.Debug, $"Character {player.Id} taken out of a sulfur lake in the Ojasa hive at {to}.");
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

        private static bool Active(Manifestation player, uint missionId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active;

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state == MissionState.Completed;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        private static void Record(Client client, uint missionId, uint scenarioId, uint eventId) =>
            MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId);

        /// <summary>A creature of the row, put on the ground near the spot, facing the other.</summary>
        private static Creature Spawn(MapChannel map, uint dbId, Vector3 spot, float spread, Vector3 facingTo)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
                return null;

            var at = spot;

            if (spread > 0 && NavMeshManager.RandomPointAround(map, spot, spread) is { } walkable &&
                Vector2.Distance(new Vector2(walkable.X, walkable.Z), new Vector2(spot.X, spot.Z)) <= spread + 1f)
                at = walkable;

            at = NavMeshManager.SnapToGround(map, at);

            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        private static void Heal(Creature creature)
        {
            if (creature == null || !creature.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current >= health.CurrentMax)
                return;

            health.Current = health.CurrentMax;
            CellManager.Instance.CellCallMethod(creature, new UpdateAttributesPacket(creature.Attributes, 0));
        }

        /// <summary>Alive and still in the world as itself (as FithikEggClusters counts its hatchlings).</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
