extern alias RasaGame;

using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;
    using static PlateauValverdePoolsTests;

    /// <summary>
    /// Amee Corman's Weapons Trafficking (904), played through as a player plays it: taken at the Retread Camp in
    /// Valverde Pools, the five prototypes taken in order from five footlockers in the Lamnas Armory (squad instance
    /// 1465), handed to Amee.
    ///
    /// Why these tests: the mission is taken on one map and played in a squad copy of another; its five objects stand
    /// together and only one of them may give at a time. A footlocker off the navmesh or out of reach, one that gives out
    /// of order or twice, a prototype not taken at the turn-in or kept after an abandon would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauLamnasArmoryTests
    {
        private const uint WeaponsTrafficking = PlateauWeaponsTraffickingV1.MissionId;
        private const uint Armory = PlateauWeaponsTraffickingV1.MapContextId;

        [TestMethod]
        public void WeaponsTraffickingTakesTheFivePrototypesInOrderFromTheArmorysFootlockers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var pools = OntoPools(harness);
            var amee = Amee(pools);
            Assert.IsFalse(Offers(harness, amee, WeaponsTrafficking), "it waits for New Orders From The General");
            SeedCompleted(harness, PlateauNewOrdersFromTheGeneralV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, amee, WeaponsTrafficking);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauWeaponsTraffickingV1.IncendiaryBlade, PlateauWeaponsTraffickingV1.HealingDisc,
                    PlateauWeaponsTraffickingV1.CryogenicShotgun, PlateauWeaponsTraffickingV1.VirulentStaff
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, amee, WeaponsTrafficking);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == WeaponsTrafficking && obj.IsInWorld),
                "nothing of the scene in the Pools, where Amee is");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauWeaponsTraffickingV1.GetThem));

            var copy = IntoArmory(harness);
            var door = harness.Client.Player.Position;
            var lockers = Lockers(harness, copy);
            for (var index = 0; index < lockers.Length; index++)
            {
                Assert.AreEqual(PlateauWeaponsTraffickingV1.CrateClass, (uint)lockers[index].EntityClassId);
                Assert.AreEqual(PlateauWeaponsTraffickingV1.ReadyState, (uint)lockers[index].StateId);
                AssertStandsOnNavMesh(copy, lockers[index].Position, 0.5f);
                Assert.IsTrue(copy.NavMesh.FindPath(door, lockers[index].Position, out var complete) != null && complete, $"footlocker {index + 1} walked to");
            }

            // Not in order: the second gives nothing while the first is the open one.
            Use(harness, copy, lockers[1]);
            Assert.AreEqual(PlateauWeaponsTraffickingV1.ReadyState, (uint)lockers[1].StateId, "the second waits for the first");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauWeaponsTraffickingV1.Prototypes[1].Template));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauWeaponsTraffickingV1.Prototypes[1].Objective));

            for (var index = 0; index < lockers.Length; index++)
            {
                var prototype = PlateauWeaponsTraffickingV1.Prototypes[index];
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, prototype.Objective), $"prototype {index + 1} open");
                Use(harness, copy, lockers[index]);
                Assert.AreEqual(PlateauWeaponsTraffickingV1.TakenState, (uint)lockers[index].StateId, $"footlocker {index + 1} emptied");
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, prototype.Template), $"prototype {index + 1} held");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, prototype.Objective));
                Use(harness, copy, lockers[index]);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, prototype.Template), "one each");
                if (index == 2)
                {
                    // A reload halfway keeps the prototypes and the objectives.
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, prototype.Template), "after the reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete,
                        Objective(harness, PlateauWeaponsTraffickingV1.Prototypes[index + 1].Objective), "after the reload");
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauWeaponsTraffickingV1.GetThem));
            Assert.IsTrue(harness.Client.Player.Missions[WeaponsTrafficking].Completeable);

            OutOfArmory(harness, copy);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, amee, WeaponsTrafficking, 1,
                PlateauWeaponsTraffickingV1.Experience, PlateauWeaponsTraffickingV1.Credits);
            foreach (var prototype in PlateauWeaponsTraffickingV1.Prototypes)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, prototype.Template), $"Amee has the {prototype.Key}");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauWeaponsTraffickingV1.HealingDisc), "the second choice");
        }

        /// <summary>An abandon in the Armory takes the prototypes and the footlockers; taken again, all five are back.</summary>
        [TestMethod]
        public void WeaponsTraffickingAbandonedTakesThePrototypesAndTheFootlockers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var pools = OntoPools(harness);
            var amee = Amee(pools);
            SeedCompleted(harness, PlateauNewOrdersFromTheGeneralV1.MissionId);
            Accept(harness, amee, WeaponsTrafficking);
            var copy = IntoArmory(harness);
            var lockers = Lockers(harness, copy);
            Use(harness, copy, lockers[0]);
            Use(harness, copy, lockers[1]);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauWeaponsTraffickingV1.Prototypes[1].Template));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, WeaponsTrafficking));
            harness.Manager.Scenes.Tick(copy);   // the run's reset, as the map's tick does it
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(WeaponsTrafficking), "gone from the log");
            foreach (var prototype in PlateauWeaponsTraffickingV1.Prototypes)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, prototype.Template), $"the {prototype.Key} goes with the mission");
            Assert.IsFalse(lockers.Any(locker => locker.IsInWorld), "the footlockers go with it");

            OutOfArmory(harness, copy);
            Accept(harness, amee, WeaponsTrafficking);
            copy = IntoArmory(harness);
            foreach (var locker in Lockers(harness, copy))
                Assert.AreEqual(PlateauWeaponsTraffickingV1.ReadyState, (uint)locker.StateId, "five full footlockers again");
        }

        /// <summary>The prototypes are protected mission items: a destroy leaves them, so Amee always gets all five.</summary>
        [TestMethod]
        public void WeaponsTraffickingPrototypesCannotBeDestroyed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var pools = OntoPools(harness);
            var amee = Amee(pools);
            SeedCompleted(harness, PlateauNewOrdersFromTheGeneralV1.MissionId);
            Accept(harness, amee, WeaponsTrafficking);
            var copy = IntoArmory(harness);
            Use(harness, copy, Lockers(harness, copy)[0]);
            var template = PlateauWeaponsTraffickingV1.Prototypes[0].Template;
            var rifle = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == template);

            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = rifle.EntityId, Quantity = rifle.StackSize });
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), "the rifle stays");
            Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.Contains(rifle.EntityId), "in its slot");
        }

        private static MapChannel IntoArmory(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Armory))
                LoadMap(harness, Armory);
            var copy = ThroughTheDoor(harness, Armory);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        /// <summary>Back to the Pools, the scene detached from the copy as a map change does (PlateauVelonHollowBaneTests.Leave).</summary>
        private static void OutOfArmory(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            OntoPools(harness);
        }

        /// <summary>The five footlockers of this test's own run: a squad copy can still hold an earlier test's.</summary>
        private static DynamicObject[] Lockers(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, WeaponsTrafficking).Select(scene => scene.RunId).ToHashSet();
            return Enumerable.Range(0, PlateauWeaponsTraffickingV1.Prototypes.Length).Select(index =>
            {
                var role = PlateauWeaponsTraffickingV1.CrateRole(index);
                var obj = copy.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == WeaponsTrafficking &&
                    candidate.SceneActorRole == role && candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
                Assert.IsNotNull(obj, $"{role} is there");
                return obj;
            }).ToArray();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauTemporalChamberTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PlateauWeaponsTraffickingV1.UseArgument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint objective) =>
            harness.Client.Player.Missions[WeaponsTrafficking].Objectives[objective].State;

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
        }
    }
}
