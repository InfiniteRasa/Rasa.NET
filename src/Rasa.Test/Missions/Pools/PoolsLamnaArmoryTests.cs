extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;

    /// <summary>
    /// The Lamna Armory's three missions (squad instance 1465), played through as a player plays them: Blow Them All Up
    /// (903: Herring's twenty weapons lockers), Bane Battle Plans (896: Arimoro's three computers) and Operation Overlord
    /// (898: Bromfield's three keys from three bosses, three key slots, the switch, Vanduahl and his Kael).
    ///
    /// Why these tests: the givers are new NPCs inside a squad copy and the missions are taken and played there; the
    /// lockers, computers and slots are the player's own objects in a copy that outlives a test; 898 chains hidden
    /// objectives (keys, slots, switch) and sets its Overlord down only at the switch. An object off the navmesh or out
    /// of reach, a slot usable before its keys, an Overlord there before the switch, a key or blueprint kept after the
    /// turn-in, or progress lost at a reload or a trip out of the Armory would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsLamnaArmoryTests
    {
        private const uint Armory = PoolsLamnaArmoryNpcsV1.MapContextId;
        private const uint Lockers = PoolsBlowThemAllUpV1.MissionId;
        private const uint Plans = PoolsBaneBattlePlansV1.MissionId;
        private const uint Overlord = PoolsOperationOverlordV1.MissionId;
        private const uint MissionItemFiller = 11519;   // as PlateauCampResistanceTests

        private static readonly Vector3 Door = ToVector(PoolsLamnaArmoryNpcsV1.DoorArrival);

        /// <summary>The three givers stand inside the entrance, on the navmesh, and each offers its mission with no requirement.</summary>
        [TestMethod]
        public void TheThreeGiversStandInsideTheEntranceAndOfferTheirMissions()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            foreach (var (pool, name, mission) in new[]
                {
                    (PoolsLamnaArmoryNpcsV1.Arimoro, PoolsLamnaArmoryNpcsV1.ArimoroName, Plans),
                    (PoolsLamnaArmoryNpcsV1.Bromfield, PoolsLamnaArmoryNpcsV1.BromfieldName, Overlord),
                    (PoolsLamnaArmoryNpcsV1.Herring, PoolsLamnaArmoryNpcsV1.HerringName, Lockers),
                })
            {
                var giver = Giver(copy, pool);
                Assert.AreEqual(name, giver.NameId);
                Assert.AreEqual(PoolsLamnaArmoryNpcsV1.GiverLevel, giver.Level);
                AssertOnTheGround(copy, giver.Position, $"giver {pool}");
                Assert.IsTrue(Vector3.Distance(Door, giver.Position) < 25, "just inside the entrance");
                Assert.IsTrue(Offers(harness, giver, mission), $"{pool} offers {mission}");
            }
            foreach (var (pool, at) in new[]
                {
                    (PoolsLamnaArmoryNpcsV1.Gaite, PoolsLamnaArmoryNpcsV1.GaiteAt), (PoolsLamnaArmoryNpcsV1.Kwel, PoolsLamnaArmoryNpcsV1.KwelAt),
                    (PoolsLamnaArmoryNpcsV1.Fearce, PoolsLamnaArmoryNpcsV1.FearceAt),
                })
            {
                var boss = Boss(harness, copy, pool);
                Assert.AreEqual(PoolsLamnaArmoryNpcsV1.BossLevel, boss.Level);
                Assert.AreEqual((int)PoolsLamnaArmoryNpcsV1.BossHitPoints, (int)boss.Attributes[Attributes.Health].Current);
                Assert.AreEqual(TargetCategory.Hostile, boss.TargetCategory);
                AssertOnTheGround(copy, ToVector(at), $"boss {pool}");
                AssertWalkedTo(copy, ToVector(at), $"boss {pool}");
            }
        }

        /// <summary>903: twenty lockers, each counted once; a reload and a trip out keep the count; Herring pays.</summary>
        [TestMethod]
        public void BlowThemAllUpCountsTheTwentyLockersAndHerringPays()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var herring = Giver(copy, PoolsLamnaArmoryNpcsV1.Herring);
            var offer = WildernessRanjaGorgeTests.Offer(harness, herring, Lockers);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsBlowThemAllUpV1.Chaingun, PoolsBlowThemAllUpV1.IncendiaryNetGun,
                    PoolsBlowThemAllUpV1.ExothermicPolarityGun, PoolsBlowThemAllUpV1.Shotgun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, herring, Lockers);

            var lockers = LockersOf(harness, copy);
            for (var index = 0; index < lockers.Length; index++)
            {
                var at = PoolsBlowThemAllUpV1.Lockers[index];
                Assert.AreEqual(PoolsBlowThemAllUpV1.LockerClass, (uint)lockers[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), lockers[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, lockers[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(copy, lockers[index].Position, lockers[index].SceneActorRole);
                AssertWalkedTo(copy, lockers[index].Position, lockers[index].SceneActorRole);
            }

            for (var index = 0; index < lockers.Length; index++)
            {
                Damage(harness, copy, lockers[index], 60);
                Assert.AreEqual((uint)index, Count(harness), "not at 60 of 100");
                Damage(harness, copy, lockers[index], 60);
                Assert.AreEqual((uint)index + 1, Count(harness), $"locker {index + 1} down");
                Damage(harness, copy, lockers[index], 100);
                Assert.AreEqual((uint)index + 1, Count(harness), "counted once");
                if (index == 9)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(10U, Count(harness), "kept at a reload");
                    OutOfArmory(harness, copy);
                    copy = IntoArmory(harness);
                    lockers = LockersOf(harness, copy);
                    Assert.AreEqual(10, lockers.Count(locker => locker.CurrentHitPoints == 0), "the ten stay destroyed");
                }
                if (index < lockers.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Lockers, PoolsBlowThemAllUpV1.Destroy));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Lockers, PoolsBlowThemAllUpV1.Destroy));
            Assert.IsTrue(harness.Client.Player.Missions[Lockers].Completeable);
            FailedTurnInPaysNothing(harness, herring, Lockers, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, herring, Lockers, 0,
                PoolsBlowThemAllUpV1.Experience, PoolsBlowThemAllUpV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBlowThemAllUpV1.Chaingun));
        }

        /// <summary>903 abandoned: the lockers go; taken again, twenty whole ones.</summary>
        [TestMethod]
        public void BlowThemAllUpAbandonedTakesTheLockers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var herring = Giver(copy, PoolsLamnaArmoryNpcsV1.Herring);
            Accept(harness, herring, Lockers);
            var lockers = LockersOf(harness, copy);
            Damage(harness, copy, lockers[0], 100);
            Assert.AreEqual(1U, Count(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Lockers));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(lockers.Any(locker => locker.IsInWorld), "the lockers go");
            Accept(harness, herring, Lockers);
            Assert.AreEqual(0U, Count(harness));
            Assert.IsTrue(LockersOf(harness, copy).All(locker => locker.CurrentHitPoints > 0), "twenty whole lockers");
        }

        /// <summary>896: the three computers in any order, one blueprint each; Arimoro takes the three and pays.</summary>
        [TestMethod]
        public void BaneBattlePlansTakesTheThreeBlueprintsInAnyOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var arimoro = Giver(copy, PoolsLamnaArmoryNpcsV1.Arimoro);
            var offer = WildernessRanjaGorgeTests.Offer(harness, arimoro, Plans);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsBaneBattlePlansV1.DirectRepairTool, PoolsBaneBattlePlansV1.ElectricPistol,
                    PoolsBaneBattlePlansV1.VirulentDisperser, PoolsBaneBattlePlansV1.EmpTorqueshellRifle
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, arimoro, Plans);

            var consoles = Enumerable.Range(0, PoolsBaneBattlePlansV1.Drives.Length)
                .Select(index => SceneObject(harness, copy, Plans, PoolsBaneBattlePlansV1.ConsoleRole(index))).ToArray();
            foreach (var console in consoles)
            {
                Assert.AreEqual(PoolsBaneBattlePlansV1.ConsoleClass, (uint)console.EntityClassId);
                AssertOnTheGround(copy, console.Position, console.SceneActorRole);
                AssertWalkedTo(copy, console.Position, console.SceneActorRole);
                Assert.IsTrue(Vector3.Distance(ToVector(PoolsLamnaArmoryNpcsV1.GaiteAt), console.Position) < 25, "in Gaite's room");
            }

            var order = new[] { 2, 0, 1 };
            for (var step = 0; step < order.Length; step++)
            {
                var index = order[step];
                Use(harness, copy, consoles[index]);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Plans, PoolsBaneBattlePlansV1.Drives[index].Objective));
                Assert.AreEqual((uint)step + 1, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), $"blueprint {step + 1}");
                Use(harness, copy, consoles[index]);
                Assert.AreEqual((uint)step + 1, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), "one each");
                if (step == 0)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Plans, PoolsBaneBattlePlansV1.Drives[index].Objective), "after a reload");
                }
                if (step < order.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Plans, PoolsBaneBattlePlansV1.All));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Plans, PoolsBaneBattlePlansV1.All));
            Assert.IsTrue(harness.Client.Player.Missions[Plans].Completeable);
            FailedTurnInPaysNothing(harness, arimoro, Plans, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, arimoro, Plans, 3,
                PoolsBaneBattlePlansV1.Experience, PoolsBaneBattlePlansV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), "Arimoro has the plans");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.EmpTorqueshellRifle));
        }

        /// <summary>
        /// 898: the slots and the switch wait; each boss gives its key; the slots take them; the switch sets Vanduahl and
        /// his Kael down; his death completes it; Bromfield pays and no key is left.
        /// </summary>
        [TestMethod]
        public void OperationOverlordTakesTheThreeKeysToTheSlotsThenTheSwitchThenTheOverlord()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var bromfield = Giver(copy, PoolsLamnaArmoryNpcsV1.Bromfield);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bromfield, Overlord);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsOperationOverlordV1.CryogenicInjectorGun, PoolsOperationOverlordV1.PulseChaingun,
                    PoolsOperationOverlordV1.ElectricRifle, PoolsOperationOverlordV1.RocketLauncher
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, bromfield, Overlord);

            var slots = Enumerable.Range(0, PoolsOperationOverlordV1.Keys.Length)
                .Select(index => SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SlotRole(index))).ToArray();
            var button = SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SwitchRole);
            foreach (var obj in slots.Append(button))
            {
                AssertOnTheGround(copy, obj.Position, obj.SceneActorRole);
                AssertWalkedTo(copy, obj.Position, obj.SceneActorRole);
            }
            Assert.AreEqual(PoolsOperationOverlordV1.SlotClass, (uint)slots[0].EntityClassId);
            Assert.AreEqual(PoolsOperationOverlordV1.SwitchClass, (uint)button.EntityClassId);
            Use(harness, copy, slots[0]);
            Use(harness, copy, button);
            Assert.AreEqual(PoolsOperationOverlordV1.SlotReadyState, (uint)slots[0].StateId, "no key yet");
            Assert.IsNull(SceneCreature(harness, copy, Overlord, PoolsOperationOverlordV1.OverlordRole), "no Overlord before the switch");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Overlord, PoolsOperationOverlordV1.KillOverlord));

            for (var index = 0; index < PoolsOperationOverlordV1.Keys.Length; index++)
            {
                var key = PoolsOperationOverlordV1.Keys[index];
                var corpse = Kill(harness, copy, Boss(harness, copy, key.Boss));
                Assert.IsNotNull(corpse, $"boss {key.Boss} leaves a corpse");
                Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == key.Template), $"{key.Key} on the boss");
                PickUp(harness, corpse);
                harness.Drain();
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, key.Template), key.Key);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, key.Objective));
                if (index < PoolsOperationOverlordV1.Keys.Length - 1)
                {
                    Use(harness, copy, slots[index]);
                    Assert.AreEqual(PoolsOperationOverlordV1.SlotReadyState, (uint)slots[index].StateId, "the slots wait for all three");
                }
            }

            for (var index = 0; index < slots.Length; index++)
            {
                Use(harness, copy, slots[index]);
                Assert.AreEqual(PoolsOperationOverlordV1.SlotSetState, (uint)slots[index].StateId, $"key {index + 1} in");
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsOperationOverlordV1.Keys[index].Template), "the slot takes it");
            }
            Assert.IsNull(SceneCreature(harness, copy, Overlord, PoolsOperationOverlordV1.OverlordRole), "still no Overlord");

            Use(harness, copy, button);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.KillOverlord));
            var overlord = SceneCreature(harness, copy, Overlord, PoolsOperationOverlordV1.OverlordRole);
            var kael = SceneCreature(harness, copy, Overlord, PoolsOperationOverlordV1.KaelRole);
            Assert.IsNotNull(overlord, "the switch sets Vanduahl down");
            Assert.IsNotNull(kael, "with his Kael");
            Assert.AreEqual(PoolsLamnaArmoryNpcsV1.VanduahlName, overlord.NameId);
            Assert.AreEqual((int)PoolsLamnaArmoryNpcsV1.VanduahlHitPoints, (int)overlord.Attributes[Attributes.Health].Current);
            Assert.AreEqual(PoolsLamnaArmoryNpcsV1.KaelLevel, kael.Level);
            foreach (var creature in new[] { overlord, kael })
            {
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory);
                AssertOnTheGround(copy, creature.Position, $"creature {creature.DbId}");
                AssertWalkedTo(copy, creature.Position, $"creature {creature.DbId}");
            }

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.KillOverlord), "after a reload");
            Kill(harness, copy, overlord);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, PoolsOperationOverlordV1.KillOverlord));
            Assert.IsTrue(harness.Client.Player.Missions[Overlord].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bromfield, Overlord, 1,
                PoolsOperationOverlordV1.Experience, PoolsOperationOverlordV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsOperationOverlordV1.PulseChaingun));
        }

        /// <summary>898: a key picked up, then an abandon: the key stays (a character-owned drop) and counts at the next accept.</summary>
        [TestMethod]
        public void OperationOverlordKeyHeldAtTheAcceptCounts()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var bromfield = Giver(copy, PoolsLamnaArmoryNpcsV1.Bromfield);
            Accept(harness, bromfield, Overlord);
            var security = PoolsOperationOverlordV1.Keys[0];
            PickUp(harness, Kill(harness, copy, Boss(harness, copy, security.Boss)));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, security.Template));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overlord));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, security.Template), "the key stays");
            Accept(harness, bromfield, Overlord);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, security.Objective), "it counts at the accept");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.Keys[1].Objective));
        }

        /// <summary>
        /// 898: all three keys held, then an abandon and a new accept: 2-4 count at the accept, and the chain (8, 9) opens
        /// the slots at once; the slots, the switch and the Overlord follow.
        /// </summary>
        [TestMethod]
        public void OperationOverlordAllThreeKeysHeldAtTheAcceptOpenTheSlots()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var bromfield = Giver(copy, PoolsLamnaArmoryNpcsV1.Bromfield);
            Accept(harness, bromfield, Overlord);
            for (var index = 0; index < PoolsOperationOverlordV1.Keys.Length; index++)
                TakeKey(harness, copy, index);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overlord));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();

            Accept(harness, bromfield, Overlord);
            Tick(harness, copy, 250);
            foreach (var key in PoolsOperationOverlordV1.Keys)
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, key.Objective), $"{key.Key} counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.KeysIn), "the slots open");
            for (var index = 0; index < PoolsOperationOverlordV1.Keys.Length; index++)
            {
                var slot = SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SlotRole(index));
                Use(harness, copy, slot);
                Assert.AreEqual(PoolsOperationOverlordV1.SlotSetState, (uint)slot.StateId, $"key {index + 1} in");
            }
            Use(harness, copy, SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SwitchRole));
            Tick(harness, copy, 250);
            var overlord = SceneCreature(harness, copy, Overlord, PoolsOperationOverlordV1.OverlordRole);
            Assert.IsNotNull(overlord, "the switch sets Vanduahl down");
            Kill(harness, copy, overlord);
            Assert.IsTrue(harness.Client.Player.Missions[Overlord].Completeable);
        }

        /// <summary>898: a key boss killed before the accept leaves no key; after the accept the next one does.</summary>
        [TestMethod]
        public void OperationOverlordBossKilledBeforeTheAcceptDropsNoKey()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var bromfield = Giver(copy, PoolsLamnaArmoryNpcsV1.Bromfield);
            var security = PoolsOperationOverlordV1.Keys[0];
            var corpse = Kill(harness, copy, Boss(harness, copy, security.Boss));
            Assert.IsFalse(corpse != null && corpse.LootItems.Any(item => item.ItemTemplateId == security.Template), "no key before the accept");

            Accept(harness, bromfield, Overlord);
            if (corpse != null)
                PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, security.Template), "nor on that corpse later");
            TakeKey(harness, copy, 0);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, security.Objective));
        }

        /// <summary>
        /// 898: one key in its slot, then an abandon: that key is gone and its boss must die again; a key not yet inserted
        /// stays and counts at the new accept; the slots are whole again.
        /// </summary>
        [TestMethod]
        public void OperationOverlordAbandonAfterAKeyIsInserted()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            var bromfield = Giver(copy, PoolsLamnaArmoryNpcsV1.Bromfield);
            Accept(harness, bromfield, Overlord);
            for (var index = 0; index < PoolsOperationOverlordV1.Keys.Length; index++)
                TakeKey(harness, copy, index);
            Use(harness, copy, SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SlotRole(0)));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsOperationOverlordV1.Keys[0].Template), "in the slot");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overlord));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Accept(harness, bromfield, Overlord);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.Keys[0].Objective), "its boss again");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, PoolsOperationOverlordV1.Keys[1].Objective), "held: counts");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overlord, PoolsOperationOverlordV1.Keys[2].Objective), "held: counts");
            Assert.AreEqual(PoolsOperationOverlordV1.SlotReadyState,
                (uint)SceneObject(harness, copy, Overlord, PoolsOperationOverlordV1.SlotRole(0)).StateId, "the slot is empty again");
            TakeKey(harness, copy, 0);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overlord, PoolsOperationOverlordV1.KeysIn), "then the slots open");
        }

        /// <summary>896: a full bag: the computer gives nothing and stays usable; with a slot free it gives once.</summary>
        [TestMethod]
        public void BaneBattlePlansComputerWaitsForAFreeSlot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoArmory(harness);
            Accept(harness, Giver(copy, PoolsLamnaArmoryNpcsV1.Arimoro), Plans);
            var console = SceneObject(harness, copy, Plans, PoolsBaneBattlePlansV1.ConsoleRole(0));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Use(harness, copy, console);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), "no room");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Plans, PoolsBaneBattlePlansV1.Drives[0].Objective));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, copy, console);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), "with room it gives");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Plans, PoolsBaneBattlePlansV1.Drives[0].Objective));
            Use(harness, copy, console);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneBattlePlansV1.Blueprint), "once");
        }

        /// <summary>Kills key <paramref name="index"/>'s boss and picks the key up from the corpse.</summary>
        private static void TakeKey(WildernessRuntimeTestHarness harness, MapChannel copy, int index)
        {
            var key = PoolsOperationOverlordV1.Keys[index];
            var corpse = Kill(harness, copy, Boss(harness, copy, key.Boss));
            Assert.IsNotNull(corpse, $"boss {key.Boss} leaves a corpse");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, key.Template), key.Key);
        }

        private static MapChannel IntoArmory(WildernessRuntimeTestHarness harness)
        {
            PlateauValverdePoolsTests.OntoPools(harness);
            if (!harness.Maps.MapChannelArray.ContainsKey(Armory))
                LoadMap(harness, Armory);
            var copy = ThroughTheDoor(harness, Armory);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        /// <summary>Back to the Pools, the scene detached from the copy as a map change does (PlateauLamnasArmoryTests).</summary>
        private static void OutOfArmory(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            PlateauValverdePoolsTests.OntoPools(harness);
        }

        /// <summary>The living creatures of this copy only (PlateauUstorYardTests.Here).</summary>
        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static Creature Giver(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var giver = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(giver, $"giver {pool} stands in the copy");
            return giver;
        }

        /// <summary>
        /// The boss of <paramref name="pool"/>, alive. A squad copy outlives a test, and a cleared pool in it comes back
        /// only after the squad's respawn time (SquadInstanceState): one killed by an earlier test is set back as the
        /// weekly reset does it, then spawned.
        /// </summary>
        private static Creature Boss(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead))
            {
                var spawnPool = copy.SpawnPools.Single(candidate => candidate.DbId == pool);
                spawnPool.ClearedAtUtcMs = 0;
                spawnPool.UpdateTimer = spawnPool.RespawnTime;
                Spawn(copy, pool);
            }
            var boss = Here(copy).FirstOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead);
            Assert.IsNotNull(boss, $"boss {pool} stands in the copy");
            return boss;
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>The object of this test's own run: a squad copy can still hold an earlier test's.</summary>
        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            var obj = map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        private static DynamicObject[] LockersOf(WildernessRuntimeTestHarness harness, MapChannel copy) =>
            Enumerable.Range(0, PoolsBlowThemAllUpV1.Lockers.Length)
                .Select(index => SceneObject(harness, copy, Lockers, PoolsBlowThemAllUpV1.LockerRole(index))).ToArray();

        private static uint Count(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Lockers].Objectives[PoolsBlowThemAllUpV1.Destroy].Counters[0];

        /// <summary>Kills the creature as the player and runs the scene on; returns its corpse, if it has one (PlateauUstorYardTests.Kill).</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
            return map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot) ? loot : null;
        }

        /// <summary>As PlateauUstorYardTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauUstorYardTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        /// <summary>A weapon hit on the object (PlateauUstorYardTests.Damage).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static void FailedTurnInPaysNothing(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException($"Injected {mission} turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static void AssertOnTheGround(MapChannel map, Vector3 position, string what)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static void AssertWalkedTo(MapChannel map, Vector3 position, string what) =>
            Assert.IsTrue(map.NavMesh.FindPath(Door, position, out var complete) != null && complete, $"{what} is walked to");

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
