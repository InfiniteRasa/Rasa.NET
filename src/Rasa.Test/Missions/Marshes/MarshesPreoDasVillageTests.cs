using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using N = MarshesPreoDasVillageNpcsV1;
    using S = MarshesNeverSurrenderV1;
    using P = MarshesPickingUpThePiecesV1;
    using H = MarshesCleaningHouseV2;

    /// <summary>
    /// Marshes set 15, P'reo Das (part 2) in the Village Ruins (squad instance 1743), played through as a player plays it:
    /// Warrior Tola's Never Surrender (891: three waves of Thrax in P'reo Das, Kirii), Shaman Rakla's Picking Up the Pieces
    /// (892: the relics of six fallen) and Elder Kirii's Cleaning House (894: four pumps blown with his explosives, three camps'
    /// prisoners freed, optional survivors, out to Tola).
    ///
    /// Why these tests: the scenes are bound to a squad copy that outlives a test (the run's own actors are looked for); 891's
    /// waves come on an area and on timers and are counted by their deaths; 892's relics are given by a scene and counted
    /// on a hidden tally; 894's pumps take an item at an acceptor, its prisoners share a client package with the survivors of
    /// their camp and follow the player. A wave that never comes, a relic counted twice, a pump blown without an explosive, a
    /// camp counted by its survivor, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesPreoDasVillageTests
    {
        private const uint Never = S.MissionId, Pieces = P.MissionId, Cleaning = H.MissionId;

        /// <summary>
        /// Tola, Kirii and Rakla stand in the copy with the client's names, level 35 and their packages, walked to from the
        /// arrival; Tola offers 891 and Rakla 892 with nothing before them; Kirii offers 894 only after 891.
        /// </summary>
        [TestMethod]
        public void TheNpcsStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            foreach (var (pool, name, package) in new[]
            {
                (N.Tola, N.TolaName, N.TolaPackage), (N.Kirii, N.KiriiName, N.KiriiPackage), (N.Rakla, N.RaklaName, N.RaklaPackage)
            })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual((name, N.Level, package), (npc.NameId, npc.Level, npc.Npc?.NpcPackageId), $"{pool}");
                AssertOnGround(copy, npc.Position, $"{pool}");
                AssertPath(copy, DoorEdge, npc.Position, $"{pool} from the door");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
            }
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Tola), Never), "Tola offers 891");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Rakla), Pieces), "Rakla offers 892");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Kirii), Cleaning), "894 needs 891");
        }

        /// <summary>
        /// 891: taken at Tola; no Thrax before P'reo Das; walking in brings the first wave of four, then five after 40 s and six
        /// after 80 s, each on the walked-to ground and hostile; the kills count once each (a reload kept), 15 complete 1; Kirii
        /// pays once; Kirii then offers 894.
        /// </summary>
        [TestMethod]
        public void NeverSurrenderHoldsBackThreeWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            PlayNeverSurrender(harness, copy, reload: true);
            var kirii = Giver(copy, N.Kirii);
            FailedTurnInPaysNothing(harness, kirii, Never, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kirii, Never, 0, S.Experience, S.Credits);
            Assert.AreEqual(S.StandardMedPacks, WildernessRanjaGorgeTests.Held(harness, S.StandardMedPack), "the chosen stack");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kirii, Cleaning), "Kirii offers 894 after 891");
        }

        /// <summary>
        /// To-fix 34: the waves come to the player and fight him - here the first, from the village's north and west edges to
        /// where the tester stood in P'reo Das (99, -237); and they pass by a GM who is invisible (.invisible: "creatures do
        /// not notice you"), standing at their places, as the tester's account did (.invisible on at 22:32, the session on to
        /// 02:24, so through 891 at 01:27).
        /// </summary>
        [TestMethod]
        public void NeverSurrendersWavesComeAtThePlayerButPassAnInvisibleGmBy()
        {
            foreach (var invisible in new[] { false, true })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                EnterMarshes(harness);
                var copy = IntoRuins(harness);
                Accept(harness, Giver(copy, N.Tola), Never);
                Tick(harness, copy, 250);
                harness.Client.Player.GmInvisible = invisible;
                var stand = new Vector3(99f, (float)S.VillageAt.Y, -237f);
                stand = copy.NavMesh.Nearest(stand) ?? stand;
                Assert.IsLessThan(2f, Vector3.Distance(stand, new Vector3(99f, (float)S.VillageAt.Y, -237f)), $"the tester's spot on the ground: {stand}");
                Enter(harness, stand);
                Tick(harness, copy, 250);
                var wave = Enumerable.Range(0, S.Waves[0].Members.Length).Select(member => SceneCreature(harness, copy, Never, S.WaveRole(0, member))).ToArray();
                Assert.IsTrue(wave.All(thrax => thrax != null), "the first wave comes");
                var places = wave.Select(thrax => thrax.Position).ToArray();
                for (var waited = 0; waited < 30000; waited += 250)
                {
                    harness.MoveTo(stand);
                    Tick(harness, copy, 250);
                }
                for (var member = 0; member < wave.Length; member++)
                {
                    var thrax = wave[member];
                    var near = Vector2.Distance(new Vector2(thrax.Position.X, thrax.Position.Z), new Vector2(stand.X, stand.Z));
                    if (invisible)
                    {
                        Assert.IsLessThan(3f, Vector3.Distance(places[member], thrax.Position), $"member {member + 1} stands at its place");
                        Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, thrax.Controller.CurrentAction, $"member {member + 1}: no fight");
                    }
                    else
                    {
                        Assert.IsLessThan(25f, near, $"member {member + 1} came to the player: {near:0.0} m off, from {places[member]} to {thrax.Position}");
                        Assert.AreEqual(BehaviorManager.BehaviorActionFighting, thrax.Controller.CurrentAction, $"member {member + 1} fights");
                        Assert.AreEqual(harness.Client.Player.EntityId, thrax.Controller.ActionFighting.TargetEntityId, $"member {member + 1}: the player");
                    }
                }
            }
        }

        /// <summary>
        /// 892: taken at Rakla; six corpses with knapsacks on the walked-to ground (four in P'reo Das, two in Ra'a Das); each
        /// knapsack gives one relic once (a second use gives none), a reload kept; the sixth completes 1; Rakla takes the six
        /// and pays.
        /// </summary>
        [TestMethod]
        public void PickingUpThePiecesTakesTheRelicsOfTheFallen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            var rakla = Giver(copy, N.Rakla);
            Accept(harness, rakla, Pieces);
            Tick(harness, copy, 250);
            for (var index = 0; index < P.CorpsesAt.Length; index++)
            {
                var corpse = SceneObject(harness, copy, Pieces, P.CorpseRole(index));
                Assert.AreEqual(P.CorpseClass, (uint)corpse.EntityClassId);
                AssertOnGround(copy, corpse.Position, $"corpse {index + 1}");
                AssertPath(copy, DoorEdge, corpse.Position, $"corpse {index + 1} from the door");
                var region = index < 4 ? new Vector2(100.8f, -236.8f) : new Vector2(239.2f, -2.2f);
                Assert.IsLessThan(index < 4 ? 62f : 52.2f, Vector2.Distance(region, new Vector2(corpse.Position.X, corpse.Position.Z)),
                    index < 4 ? "in P'reo Das" : "in Ra'a Das");
            }
            for (var index = 0; index < P.CorpsesAt.Length; index++)
            {
                if (index == 3)
                    ReloadAndRecover(harness, copy);
                var pack = SceneObject(harness, copy, Pieces, P.PackRole(index));
                Use(harness, copy, pack, P.UseArgument);
                Use(harness, copy, pack, P.UseArgument);
                Assert.AreEqual((uint)index + 1, WildernessRanjaGorgeTests.Held(harness, P.RelicTemplate), $"relic {index + 1}, once");
                Assert.AreEqual(P.TakenState, (uint)pack.StateId);
                Assert.AreEqual(index < 5 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                    Objective(harness, Pieces, P.Retrieve), $"after relic {index + 1}");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Pieces].Completeable);
            FailedTurnInPaysNothing(harness, rakla, Pieces, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, rakla, Pieces, 1, P.Experience, P.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, P.RelicTemplate), "Rakla takes the relics");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, P.Rewards[1]), "the chosen staff");
        }

        /// <summary>892 abandoned: the relics taken go.</summary>
        [TestMethod]
        public void PickingUpThePiecesAbandonedTakesItsRelics()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            Accept(harness, Giver(copy, N.Rakla), Pieces);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Pieces, P.PackRole(0)), P.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, P.RelicTemplate));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Pieces));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, P.RelicTemplate), "the relic goes");
        }

        /// <summary>
        /// 894 after 891: taken at Kirii with four explosives; four pumps and their acceptors before Central Operations, on the
        /// walked-to ground; each acceptor takes one explosive once and blows its pump (8-11), and 1 counts the four. At each
        /// camp the prisoner and two Thrax guards; the prisoner and the survivor offer both of the camp's talks (one client
        /// package); a prisoner's talk completes the camp and sets him following, 6 counts the camps (the survivor's talk does
        /// not), a reload kept; the three camps open 12; the arrival completes 12; Tola pays, and the followers go.
        /// </summary>
        [TestMethod]
        public void CleaningHouseBlowsThePumpsAndBringsThePrisonersOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            PlayNeverSurrender(harness, copy, reload: false);
            var kirii = Giver(copy, N.Kirii);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kirii, Never, 0, S.Experience, S.Credits);

            Accept(harness, kirii, Cleaning);
            Tick(harness, copy, 250);
            Assert.AreEqual(H.Explosives, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate), "Kirii's explosives");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Cleaning, H.Escort));

            for (var index = 0; index < H.PumpRows.Length; index++)
            {
                var row = H.PumpRows[index];
                var pump = SceneObject(harness, copy, Cleaning, H.PumpRole(index));
                var acceptor = SceneObject(harness, copy, Cleaning, H.AcceptorRole(index));
                Assert.AreEqual((H.PumpClass, H.IntactState), ((uint)pump.EntityClassId, (uint)pump.StateId));
                AssertOnGround(copy, pump.Position, $"pump {index + 1}");
                AssertOnGround(copy, acceptor.Position, $"acceptor {index + 1}");
                AssertPath(copy, DoorEdge, acceptor.Position, $"acceptor {index + 1} from the door");
                Assert.IsLessThan(6f, Vector3.Distance(pump.Position, acceptor.Position), "the acceptor beside its pump");
                Use(harness, copy, acceptor, H.UseArgument);
                Use(harness, copy, acceptor, H.UseArgument);
                Assert.AreEqual(H.Explosives - (uint)index - 1, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate), $"pump {index + 1}: one explosive");
                Assert.AreEqual(H.DestroyedState, (uint)pump.StateId, $"pump {index + 1} blown");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Cleaning, row.Objective));
                Assert.AreEqual(index < 3 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed, Objective(harness, Cleaning, H.Pumps));
                if (index < 3)
                    Assert.AreEqual((uint)index + 1, Counter(harness, Cleaning, H.Pumps), "the pumps counted");
            }

            for (var index = 0; index < H.CampRows.Length; index++)
            {
                var camp = H.CampRows[index];
                if (index == 1)
                    ReloadAndRecover(harness, copy);
                for (var guard = 0; guard < camp.GuardsAt.Length; guard++)
                {
                    var thrax = SceneCreature(harness, copy, Cleaning, H.GuardRole(index, guard));
                    Assert.IsNotNull(thrax, $"camp {index + 1} guard {guard + 1}");
                    Assert.AreEqual(TargetCategory.Hostile, thrax.TargetCategory);
                    AssertOnGround(copy, thrax.Position, $"camp {index + 1} guard {guard + 1}");
                }
                var prisoner = SceneCreature(harness, copy, Cleaning, H.PrisonerRole(index, false));
                var survivor = SceneCreature(harness, copy, Cleaning, H.PrisonerRole(index, true));
                foreach (var (npc, what) in new[] { (prisoner, "prisoner"), (survivor, "survivor") })
                {
                    Assert.IsNotNull(npc, $"camp {index + 1}'s {what}");
                    Assert.AreEqual((N.PrisonerName, N.Level, camp.Package), (npc.NameId, npc.Level, npc.Npc?.NpcPackageId), $"camp {index + 1}'s {what}");
                    Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                    AssertOnGround(copy, npc.Position, $"camp {index + 1}'s {what}");
                    AssertPath(copy, DoorEdge, npc.Position, $"camp {index + 1}'s {what} from the door");
                    CollectionAssert.AreEquivalent(new[] { camp.Rescue, camp.Survivor }, Topics(harness, npc), $"camp {index + 1}'s {what}: both talks");
                }
                if (index == 0)
                    foreach (var npc in new[] { prisoner, survivor })
                        Unhurt(harness, copy, npc);
                Rescue(harness, copy, prisoner, camp.Rescue);
                Assert.AreEqual(harness.Client.Player.Id, prisoner.SpawnPool.FollowOwnerCharacterId, $"camp {index + 1}'s prisoner follows");
                Assert.AreEqual(index < 2 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed, Objective(harness, Cleaning, H.Camps));
                if (index < 2)
                    Assert.AreEqual((uint)index + 1, Counter(harness, Cleaning, H.Camps), "the camps counted");
                if (index == 0)
                {
                    // The first camp's survivor too: optional, counted for 14 only.
                    Rescue(harness, copy, survivor, camp.Survivor);
                    Assert.AreEqual(harness.Client.Player.Id, survivor.SpawnPool.FollowOwnerCharacterId, "the survivor follows");
                    Assert.AreEqual(1U, Counter(harness, Cleaning, H.Camps), "the survivor frees no camp");
                }
            }

            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Cleaning, H.Escort), "the camps open 12");
            Assert.IsFalse(harness.Client.Player.Missions[Cleaning].Completeable, "not before the way out");
            var tola = Giver(copy, N.Tola);
            Enter(harness, tola.Position);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Cleaning, H.Escort));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Cleaning, H.CampRows[1].Survivor), "an optional survivor left");
            Assert.IsTrue(harness.Client.Player.Missions[Cleaning].Completeable);

            FailedTurnInPaysNothing(harness, tola, Cleaning, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, tola, Cleaning, 2, H.Experience, H.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, H.ElectricChaingun), "the chosen chaingun");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate));
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Cleaning, H.PrisonerRole(0, false)), "the followers go at the turn-in");
        }

        /// <summary>894 abandoned: the explosives go; taken again, four.</summary>
        [TestMethod]
        public void CleaningHouseAbandonedTakesItsExplosives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            PlayNeverSurrender(harness, copy, reload: false);
            var kirii = Giver(copy, N.Kirii);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kirii, Never, 0, S.Experience, S.Credits);
            Accept(harness, kirii, Cleaning);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Cleaning, H.AcceptorRole(0)), H.UseArgument);
            Assert.AreEqual(H.Explosives - 1, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Cleaning));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate), "the explosives go");
            Accept(harness, Giver(copy, N.Kirii), Cleaning);
            Assert.AreEqual(H.Explosives, WildernessRanjaGorgeTests.Held(harness, H.ExplosiveTemplate), "four again");
        }

        /// <summary>
        /// 891 played to Kirii: taken at Tola, the waves brought in P'reo Das and killed (with <paramref name="reload"/> a reload
        /// in the second wave).
        /// </summary>
        private static void PlayNeverSurrender(WildernessRuntimeTestHarness harness, MapChannel copy, bool reload)
        {
            Accept(harness, Giver(copy, N.Tola), Never);
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Never, S.WaveRole(0, 0)), "no Thrax before P'reo Das");
            Enter(harness, ToVector(S.VillageAt));
            Tick(harness, copy, 250);
            var killed = 0;
            for (var wave = 0; wave < S.Waves.Length; wave++)
            {
                if (wave > 0)
                {
                    Assert.IsNull(SceneCreature(harness, copy, Never, S.WaveRole(wave, 0)), $"wave {wave + 1} not yet");
                    for (var waited = 0U; waited < S.Waves[wave].After - S.Waves[wave - 1].After; waited += 5000)
                        Tick(harness, copy, 5000);
                    Tick(harness, copy, 250);
                }
                var members = Enumerable.Range(0, S.Waves[wave].Members.Length)
                    .Select(member => SceneCreature(harness, copy, Never, S.WaveRole(wave, member))).ToArray();
                for (var member = 0; member < members.Length; member++)
                {
                    Assert.IsNotNull(members[member], $"wave {wave + 1} member {member + 1}");
                    Assert.AreEqual(TargetCategory.Hostile, members[member].TargetCategory);
                    Assert.AreEqual(S.Waves[wave].Members[member].Creature, members[member].DbId);
                    Assert.IsNotNull(members[member].SceneAttackOrder, $"wave {wave + 1} member {member + 1} sent at the player");
                }
                if (wave == 0)
                    foreach (var thrax in members)
                        AssertPath(copy, DoorEdge, thrax.Position, "a wave's place from the door");
                if (reload && wave == 1)
                    ReloadAndRecover(harness, copy);
                foreach (var thrax in Enumerable.Range(0, S.Waves[wave].Members.Length)
                    .Select(member => SceneCreature(harness, copy, Never, S.WaveRole(wave, member))))
                {
                    Kill(harness, copy, thrax);
                    killed++;
                    Assert.AreEqual(killed < S.Attackers ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                        Objective(harness, Never, S.HoldBack), $"after kill {killed}");
                }
            }
            Assert.AreEqual((int)S.Attackers, killed);
            Assert.IsTrue(harness.Client.Player.Missions[Never].Completeable);
        }

        /// <summary>The objectives of 894 the NPC offers to complete.</summary>
        private static uint[] Topics(WildernessRuntimeTestHarness harness, Creature npc)
        {
            var conversation = Conversation(harness, npc);
            Assert.IsTrue(conversation.TryGetValue(ConversationType.ObjectiveComplete, out var value), "a hand-in");
            return ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == Cleaning)
                .Select(topic => (uint)topic.ObjectiveId).ToArray();
        }

        /// <summary>The camp NPC's talk for <paramref name="objective"/>: done once, and he comes along.</summary>
        private static void Rescue(WildernessRuntimeTestHarness harness, MapChannel copy, Creature npc, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Cleaning, objective, 1), $"894.{objective}");
            Assert.AreEqual(1, harness.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Cleaning &&
                packet.ObjectiveId == objective), $"894.{objective} ObjectiveCompleted once");
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Cleaning, objective));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Cleaning, objective, 1), "and only once");
            harness.Drain();
            // Playtest note (b): he sets off a moment after the talk, so the talk window (closed by the client beyond about
            // 5 m) stays open while the player reads.
            Tick(harness, copy, 1000);
            Assert.AreNotEqual(harness.Client.Player.Id, npc.SpawnPool.FollowOwnerCharacterId, $"894.{objective}: not yet following");
            Assert.IsLessThan(5f, Vector3.Distance(npc.Position, harness.Client.Player.Position), $"894.{objective}: within talk reach");
            for (var second = 0; second < H.FollowDelayMilliseconds / 1000 + 1; second++)
                Tick(harness, copy, 1000);
        }

        /// <summary>A player's weapon hit on the NPC does no damage (an invulnerable scene actor).</summary>
        private static void Unhurt(WildernessRuntimeTestHarness harness, MapChannel map, Creature npc)
        {
            Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(npc), "invulnerable");
            var before = npc.Attributes[Attributes.Health].Current;
            Steady(harness);
            harness.MoveTo(npc.Position + new Vector3(0, 0, 2));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = npc.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, 5000);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
            Assert.AreEqual(before, npc.Attributes[Attributes.Health].Current, "no damage");
            Assert.AreNotEqual(CharacterState.Dead, npc.State);
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];
    }
}
