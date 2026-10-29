extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Fort Defiance's first missions, Plateau, played through as a player plays them: Mamede's The
    /// Defiant Ones (887), which opens Peterson's A Bridge Too Far (713) and The Beer Hunter (855) and
    /// Mauer's Crystal Blue Persuasion (954) and Alpha Mox (1027).
    ///
    /// Why these tests: as PlateauCampResistanceScroungerTests. The NPCs that moved stand where the client's texts
    /// put them; the Juggernaut's, the Mox's and the Alpha Male's drops are taken from the corpse as
    /// PlateauWedgeRockTests takes Vorvaak's; the plants are used as PlateauWedgeRockTests uses the Soyuz
    /// debris.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauFortDefianceTests
    {
        private const uint DefiantOnes = PlateauDefiantOnesV1.MissionId;
        private const uint BridgeTooFar = PlateauBridgeTooFarV1.MissionId;
        private const uint BeerHunter = PlateauBeerHunterV1.MissionId;
        private const uint CrystalBlue = PlateauCrystalBluePersuasionV1.MissionId;
        private const uint AlphaMox = PlateauAlphaMoxV1.MissionId;
        private const uint Peterson = PlateauFortDefianceNpcsV1.Peterson;          // creature and pool
        private const uint Mauer = PlateauFortDefianceNpcsV1.Mauer;
        private const uint McLaughlin = PlateauFortDefianceNpcsV1.McLaughlin;
        private const uint MamedePool = PlateauFortDefianceNpcsV1.MamedePool;
        private const uint Juggernaut = PlateauPrototypeJuggernautV1.Juggernaut;
        private const uint JuggernautPool = PlateauPrototypeJuggernautV1.Pool;
        private const uint Mox = PlateauMoxLairV1.Mox;
        private const uint AlphaMale = PlateauMoxLairV1.AlphaMale;              // creature and pool
        private const uint Root = PlateauBeerHunterV1.RootTemplate;

        [TestMethod]
        public void TheDefiantOnesSendsMamedesArrivalsToPetersonAndOpensFortDefiance()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, MamedePool, Peterson, Mauer, McLaughlin);
            var mamede = Living(plateau).Single(creature => creature.SpawnPool?.DbId == MamedePool && creature.Npc != null);
            Assert.AreEqual(PlateauFortDefianceNpcsV1.Mamede, mamede.DbId);
            var peterson = RequireNpc(plateau, Peterson);
            var mauer = RequireNpc(plateau, Mauer);
            var mclaughlin = RequireNpc(plateau, McLaughlin);
            AssertStands(plateau, mamede, PlateauFortDefianceNpcsV1.MamedeAt, "Mamede by the wormhole");
            foreach (var (npc, move) in new[] { peterson, mauer, mclaughlin }.Zip(PlateauFortDefianceNpcsV1.Moves))
            {
                Assert.AreEqual(move.Pool, npc.DbId);
                AssertStands(plateau, npc, move.At, $"{npc.DbId} where the client's text puts it");
            }

            Assert.IsFalse(Offers(harness, peterson, BridgeTooFar), "A Bridge Too Far waits for The Defiant Ones");
            Assert.IsFalse(Offers(harness, mauer, CrystalBlue), "Crystal Blue Persuasion waits for The Defiant Ones");
            var offer = WildernessRanjaGorgeTests.Offer(harness, mamede, DefiantOnes);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice: the consumables");
            CollectionAssert.AreEquivalent(
                new[] { (PlateauDefiantOnesV1.MedPack, PlateauDefiantOnesV1.MedPacks), (PlateauDefiantOnesV1.ResKit, 1U) },
                offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.AreEqual(PlateauDefiantOnesV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(mamede.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, mamede.EntityId, DefiantOnes));
            harness.Drain();

            Talk(harness, peterson, DefiantOnes, 1);
            Assert.IsTrue(harness.Client.Player.Missions[DefiantOnes].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, peterson, DefiantOnes, null,
                PlateauDefiantOnesV1.Experience, PlateauDefiantOnesV1.Credits);
            Assert.AreEqual(PlateauDefiantOnesV1.MedPacks, WildernessRanjaGorgeTests.Held(harness, PlateauDefiantOnesV1.MedPack));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauDefiantOnesV1.ResKit));

            Assert.IsTrue(Offers(harness, peterson, BridgeTooFar), "Peterson's line opens");
            Assert.IsTrue(Offers(harness, mauer, CrystalBlue), "Mauer's line opens");
            Assert.IsFalse(Offers(harness, peterson, BeerHunter), "The Beer Hunter waits for A Bridge Too Far");
            Assert.IsFalse(Offers(harness, mauer, AlphaMox), "Alpha Mox waits for Crystal Blue Persuasion");
        }

        [TestMethod]
        public void ABridgeTooFarTakesThePrototypeJuggernautsCpuToPeterson()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, DefiantOnes);
            Spawn(plateau, Peterson, JuggernautPool);
            var peterson = RequireNpc(plateau, Peterson);
            var juggernaut = Living(plateau).Single(creature => creature.DbId == Juggernaut);
            Assert.AreEqual(PlateauPrototypeJuggernautV1.JuggernautClass, (uint)juggernaut.EntityClass);
            AssertOnNavMesh(plateau, PlateauPrototypeJuggernautV1.At, "the Juggernaut's spawn");

            // The Juggernaut drops nothing before the mission.
            Assert.IsFalse(Holds(Kill(harness, plateau, Juggernaut), PlateauBridgeTooFarV1.CpuTemplate));
            Respawn(plateau, JuggernautPool);

            var offer = WildernessRanjaGorgeTests.Offer(harness, peterson, BridgeTooFar);
            WildernessRanjaGorgeTests.AssertSelectable(offer, BridgeTooFar,
                (PlateauBridgeTooFarV1.BioLegs, 23391), (PlateauBridgeTooFarV1.MechVest, 18068),
                (PlateauBridgeTooFarV1.StealthBoots, 19817), (PlateauBridgeTooFarV1.ReflectiveGloves, 18483));
            harness.MoveTo(peterson.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, peterson.EntityId, BridgeTooFar));
            harness.Drain();

            var corpse = Kill(harness, plateau, Juggernaut);
            Assert.IsTrue(Holds(corpse, PlateauBridgeTooFarV1.CpuTemplate), "the CPU in the wreck");
            PickUp(harness, corpse);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBridgeTooFarV1.CpuTemplate));
            Assert.IsTrue(harness.Client.Player.Missions[BridgeTooFar].Completeable);

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.IsTrue(harness.Client.Player.Missions[BridgeTooFar].Completeable);
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, peterson, BridgeTooFar, 1,
                PlateauBridgeTooFarV1.Experience, PlateauBridgeTooFarV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBridgeTooFarV1.MechVest), "the selected vest");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauBridgeTooFarV1.CpuTemplate), "Peterson keeps the CPU");
            Assert.IsTrue(Offers(harness, peterson, BeerHunter));
        }

        [TestMethod]
        public void TheBeerHunterGathersTenSaccharoRootsForMcLaughlinsBeer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, DefiantOnes, BridgeTooFar);
            Spawn(plateau, Peterson, McLaughlin);
            var peterson = RequireNpc(plateau, Peterson);
            var mclaughlin = RequireNpc(plateau, McLaughlin);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == BeerHunter), "no plant before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, peterson, BeerHunter);
            WildernessRanjaGorgeTests.AssertSelectable(offer, BeerHunter,
                (PlateauBeerHunterV1.PulseDisperser, 27162), (PlateauBeerHunterV1.CryogenicInjectorGun, 26980),
                (PlateauBeerHunterV1.IncendiaryLeechGun, 26996), (PlateauBeerHunterV1.IncendiaryShotgun, 27295));
            harness.MoveTo(peterson.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, peterson.EntityId, BeerHunter));
            harness.Drain();

            var plants = Enumerable.Range(1, PlateauBeerHunterV1.Plants.Length).Select(id => plateau.DynamicObjects.Single(obj =>
                obj.SceneMissionId == BeerHunter && obj.SceneActorRole == $"plant-{id}")).ToArray();
            for (var index = 0; index < plants.Length; index++)
            {
                var at = PlateauBeerHunterV1.Plants[index];
                Assert.AreEqual(PlateauBeerHunterV1.PlantClass, (uint)plants[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), plants[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, plants[index].SceneOwnerCharacterId, "the player's own");
                AssertOnNavMesh(plateau, (at.X, at.Y, at.Z), $"plant {index + 1}");
            }

            // Not before McLaughlin asks for them.
            harness.MoveTo(plants[0].Position + new Vector3(0, 0, 1));
            Use(harness, plateau, plants[0], PlateauBeerHunterV1.UseArgument, recovers: false);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Root));

            Talk(harness, mclaughlin, BeerHunter, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[BeerHunter].Objectives[2].State);
            for (var index = 0; index < plants.Length; index++)
            {
                harness.MoveTo(plants[index].Position + new Vector3(0, 0, 1));
                if (index == 0)
                    Use(harness, plateau, plants[index], 1, recovers: false);
                Use(harness, plateau, plants[index], PlateauBeerHunterV1.UseArgument, recovers: true);
                Assert.AreEqual((uint)index + 1, WildernessRanjaGorgeTests.Held(harness, Root));
                // The client has no counter text for 2: the roots show as its item counter (to-fix #38).
                var counter = harness.Drain().OfType<UpdateObjectiveItemCounterPacket>().Single(packet => packet.MissionId == BeerHunter);
                Assert.AreEqual((2U, PlateauBeerHunterV1.RootClass, (uint)index + 1, PlateauBeerHunterV1.Roots),
                    (counter.ObjectiveId, counter.ItemClassId, counter.CounterValue, counter.TargetValue), $"root {index + 1}");
                Assert.AreEqual((UseObjectState)PlateauBeerHunterV1.TakenState, plants[index].StateId);
                Assert.IsFalse(plants[index].IsEnabled, $"plant {index + 1} harvested once");
                if (index == 0)
                {
                    Use(harness, plateau, plants[0], PlateauBeerHunterV1.UseArgument, recovers: false);
                    Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Root), "one root a plant");
                }
                if (index == 4)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, Root));
                    harness.Drain();
                }
                if (index < plants.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[BeerHunter].Objectives[2].State);
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (harness.Client.Player.Missions[BeerHunter].Objectives[2].State,
                 harness.Client.Player.Missions[BeerHunter].Objectives[3].State));

            Talk(harness, mclaughlin, BeerHunter, 3);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Root), "McLaughlin brews with them");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[BeerHunter].Objectives[4].State);
            Talk(harness, peterson, BeerHunter, 4);
            Assert.IsTrue(harness.Client.Player.Missions[BeerHunter].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, peterson, BeerHunter, 2,
                PlateauBeerHunterV1.Experience, PlateauBeerHunterV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBeerHunterV1.IncendiaryLeechGun), "the selected leech gun");
        }

        [TestMethod]
        public void CrystalBluePersuasionTakesTwelveCrystalsFromTheMoxLairToMauer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, DefiantOnes);
            var pools = PlateauMoxLairV1.Pools.Select(pool => pool.Id).ToArray();
            Spawn(plateau, pools.Append(Mauer).ToArray());
            var mauer = RequireNpc(plateau, Mauer);
            foreach (var pool in PlateauMoxLairV1.Pools)
                AssertOnNavMesh(plateau, (pool.X, pool.Y, pool.Z), $"Mox pool {pool.Id} in the lair");
            Assert.IsTrue(Living(plateau).Count(creature => creature.DbId == Mox) >= 6, "two or three Mox a pool");

            var offer = WildernessRanjaGorgeTests.Offer(harness, mauer, CrystalBlue);
            CollectionAssert.AreEqual(new[]
            {
                (PlateauCrystalBluePersuasionV1.Cartridges, PlateauCrystalBluePersuasionV1.Ammunition),
                (PlateauCrystalBluePersuasionV1.PowerCells, PlateauCrystalBluePersuasionV1.Ammunition),
                (PlateauCrystalBluePersuasionV1.Canisters, PlateauCrystalBluePersuasionV1.Ammunition),
                (PlateauCrystalBluePersuasionV1.Rockets, PlateauCrystalBluePersuasionV1.Ammunition),
            }, offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(mauer.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, mauer.EntityId, CrystalBlue));
            harness.Drain();

            var crystal = PlateauCrystalBluePersuasionV1.CrystalTemplate;
            var kills = 0;
            var empty = 0;
            while (WildernessRanjaGorgeTests.Held(harness, crystal) < PlateauCrystalBluePersuasionV1.Crystals)
            {
                Assert.IsTrue(++kills <= 200, "twelve crystals in 200 kills at 50%");
                Assert.IsFalse(harness.Client.Player.Missions[CrystalBlue].Completeable);
                if (!Living(plateau).Any(creature => creature.DbId == Mox))
                    Respawn(plateau, pools);
                var corpse = Kill(harness, plateau, Mox);
                if (Holds(corpse, crystal))
                    PickUp(harness, corpse);
                else
                    empty++;
                harness.Drain();
            }
            Assert.IsTrue(empty > 0, "not every Mox has a crystal");
            Assert.IsTrue(harness.Client.Player.Missions[CrystalBlue].Completeable);

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.IsTrue(harness.Client.Player.Missions[CrystalBlue].Completeable);
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mauer, CrystalBlue, 0,
                PlateauCrystalBluePersuasionV1.Experience, PlateauCrystalBluePersuasionV1.Credits);
            Assert.AreEqual(PlateauCrystalBluePersuasionV1.Ammunition,
                WildernessRanjaGorgeTests.Held(harness, PlateauCrystalBluePersuasionV1.Cartridges), "the selected cartridges");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, crystal), "Mauer keeps the crystals");
            Assert.IsTrue(Offers(harness, mauer, AlphaMox));
        }

        [TestMethod]
        public void AlphaMoxTakesTheAlphaMalesCrystalFromTheBackOfTheLair()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, DefiantOnes, CrystalBlue);
            Spawn(plateau, Mauer, AlphaMale);
            var mauer = RequireNpc(plateau, Mauer);
            var pool = plateau.SpawnPools.Single(candidate => candidate.DbId == AlphaMale);
            var at = PlateauMoxLairV1.AlphaAt;
            Assert.IsTrue(Vector3.Distance(new Vector3((float)at.X, (float)at.Y, (float)at.Z), pool.Position) < 0.01f,
                $"at the lair's back: {pool.Position}");
            AssertOnNavMesh(plateau, at, "the Alpha Male");

            var offer = WildernessRanjaGorgeTests.Offer(harness, mauer, AlphaMox);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no item");
            harness.MoveTo(mauer.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, mauer.EntityId, AlphaMox));
            harness.Drain();

            var corpse = Kill(harness, plateau, AlphaMale);
            Assert.IsTrue(Holds(corpse, PlateauAlphaMoxV1.CrystalTemplate), "the giant crystal");
            PickUp(harness, corpse);
            Assert.IsTrue(harness.Client.Player.Missions[AlphaMox].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mauer, AlphaMox, null,
                PlateauAlphaMoxV1.Experience, PlateauAlphaMoxV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauAlphaMoxV1.CrystalTemplate), "Mauer keeps it");
        }

        /// <summary>The NPC stands at its pool's place, on the navmesh there.</summary>
        private static void AssertStands(MapChannel plateau, Creature npc, (double X, double Y, double Z) at, string what)
        {
            Assert.IsTrue(Vector2.Distance(new Vector2(npc.Position.X, npc.Position.Z), new Vector2((float)at.X, (float)at.Z)) < 0.5f,
                $"{what}: {npc.Position}");
            AssertOnNavMesh(plateau, at, what);
        }

        private static void AssertOnNavMesh(MapChannel plateau, (double X, double Y, double Z) at, string what)
        {
            var ground = plateau.NavMesh.GroundHeight(new Vector3((float)at.X, (float)at.Y, (float)at.Z));
            Assert.IsNotNull(ground, $"{what} has navmesh under it");
            Assert.IsTrue(System.Math.Abs(ground.Value - at.Y) < 0.5f, $"{what} at {at.Y}, ground {ground}");
        }

        private static void AssertOnNavMesh(MapChannel plateau, (float X, float Y, float Z) at, string what) =>
            AssertOnNavMesh(plateau, ((double)at.X, (double)at.Y, (double)at.Z), what);

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        /// <summary>The NPC's hand-in topic of this objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1),
                $"{mission}.{objective}");
            harness.Drain();
        }

        /// <summary>Kills a living creature of this kind as the player; returns its corpse.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, uint kind)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

        /// <summary>These pools' dead come back: their respawn time and more has passed.</summary>
        private static void Respawn(MapChannel plateau, params uint[] pools)
        {
            var all = plateau.SpawnPools.ToArray();
            try
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
            }
            finally
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all);
            }
        }

        /// <summary>A use of the object: the request, then its recovery if one is due.</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel plateau, DynamicObject obj, uint argument, bool recovers)
        {
            var count = plateau.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), plateau.PerformRecovery.Count, $"argument {argument}");
            if (!recovers)
                return;
            var recovery = plateau.PerformRecovery.Last();
            plateau.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(plateau, recovery);
        }
    }
}
