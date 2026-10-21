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
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Mount Reverence's Vogren missions, Plateau, played through as a player plays them: Caretaker
    /// Tendahi's A Voice From The Past (934), turned in at Vogren's hologram, which opens the hologram's
    /// Trinity Bridge Logos: Past (1044).
    ///
    /// Why these tests: as PlateauNewVelonVillageTests. The two NPCs are new and stand where TaRapedia
    /// puts them; the Logos is taken at the server's own shrine, as LogosShrineUseArgTests takes one, and a
    /// Logos the player already has counts, as WildernessLogosTests.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMountReverenceTests
    {
        private const uint VoiceFromThePast = PlateauVoiceFromThePastV1.MissionId;
        private const uint LogosPast = PlateauTrinityLogosPastV1.MissionId;
        private const uint Past = PlateauTrinityLogosPastV1.Past;

        [TestMethod]
        public void AVoiceFromThePastSendsTendahisVisitorToVogrensHologram()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var (tendahi, hologram) = Npcs(plateau);
            Assert.AreEqual(PlateauVogrenNpcsV1.TendahiName, tendahi.ActorName);
            Assert.AreEqual(PlateauVogrenNpcsV1.HologramName, hologram.ActorName);
            Assert.AreEqual(PlateauVogrenNpcsV1.HologramPackage, hologram.Npc.NpcPackageId);
            AssertStands(plateau, tendahi, PlateauVogrenNpcsV1.TendahiAt, "Tendahi in the hut");
            AssertStands(plateau, hologram, PlateauVogrenNpcsV1.HologramAt, "the hologram in the tomb");
            foreach (var pool in harness.World.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= PlateauTombMiasmasV1.FirstPool && row.Id <= PlateauTombMiasmasV1.LastPool).ToArray())
                foreach (var npc in new[] { tendahi, hologram })
                    Assert.IsTrue(Vector2.Distance(new Vector2(npc.Position.X, npc.Position.Z), new Vector2((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius
                        >= SpawnPoolManager.SafeClearance, $"miasma pool {pool.Id} keeps off {npc.DbId}");

            Assert.IsFalse(Offers(harness, hologram, LogosPast), "Past waits for A Voice From The Past");
            var offer = WildernessRanjaGorgeTests.Offer(harness, tendahi, VoiceFromThePast);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice: the consumables");
            CollectionAssert.AreEquivalent(
                new[] { (PlateauVoiceFromThePastV1.MedPack, PlateauVoiceFromThePastV1.MedPacks), (PlateauVoiceFromThePastV1.ResKit, 1U) },
                offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(tendahi.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, tendahi.EntityId, VoiceFromThePast));
            harness.Drain();

            AssertHandInTopic(Conversation(harness, hologram), VoiceFromThePast, 1);
            harness.MoveTo(hologram.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hologram.EntityId, VoiceFromThePast, 1, 1));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[VoiceFromThePast].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hologram, VoiceFromThePast, null,
                PlateauVoiceFromThePastV1.Experience, PlateauVoiceFromThePastV1.Credits);
            Assert.AreEqual(PlateauVoiceFromThePastV1.MedPacks, WildernessRanjaGorgeTests.Held(harness, PlateauVoiceFromThePastV1.MedPack));

            Assert.IsTrue(Offers(harness, hologram, LogosPast), "the hologram's line opens");
        }

        [TestMethod]
        public void TrinityBridgeLogosPastCompletesAtTheVaultsShrine()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, VoiceFromThePast);
            var (_, hologram) = Npcs(plateau);

            var offer = WildernessRanjaGorgeTests.Offer(harness, hologram, LogosPast);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no item: the Logos is the reward");
            harness.MoveTo(hologram.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, LogosPast));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[LogosPast].Completeable);

            var shrine = Shrine(harness, plateau);
            Assert.IsTrue(Vector2.Distance(new Vector2(shrine.Position.X, shrine.Position.Z), new Vector2(-120, -703)) < 1,
                $"the shrine in the vault: {shrine.Position}");
            UseShrine(harness, plateau, shrine);
            Assert.IsTrue(harness.Client.Player.Logos.Contains(Past));
            Assert.IsTrue(harness.Client.Player.Missions[LogosPast].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hologram, LogosPast, null,
                PlateauTrinityLogosPastV1.Experience, PlateauTrinityLogosPastV1.Credits);
        }

        [TestMethod]
        public void APastAlreadyLearnedCountsForTrinityBridgeLogosPast()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, VoiceFromThePast);
            var (_, hologram) = Npcs(plateau);
            UseShrine(harness, plateau, Shrine(harness, plateau));
            Assert.IsTrue(harness.Client.Player.Logos.Contains(Past));

            harness.MoveTo(hologram.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, LogosPast));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[LogosPast].Completeable, "the Logos the player has counts");
        }

        /// <summary>Tendahi and the hologram, spawned from their pools.</summary>
        private static (Creature Tendahi, Creature Hologram) Npcs(MapChannel plateau)
        {
            Spawn(plateau, PlateauVogrenNpcsV1.TendahiPool, PlateauVogrenNpcsV1.HologramPool);
            Creature Npc(uint pool, uint creature)
            {
                var npc = Living(plateau).Single(candidate => candidate.SpawnPool?.DbId == pool && candidate.Npc != null);
                Assert.AreEqual(creature, npc.DbId);
                return npc;
            }
            return (Npc(PlateauVogrenNpcsV1.TendahiPool, PlateauVogrenNpcsV1.Tendahi),
                Npc(PlateauVogrenNpcsV1.HologramPool, PlateauVogrenNpcsV1.Hologram));
        }

        /// <summary>The NPC stands at its pool's place, on the navmesh there.</summary>
        private static void AssertStands(MapChannel plateau, Creature npc, (double X, double Y, double Z) at, string what)
        {
            Assert.IsTrue(Vector2.Distance(new Vector2(npc.Position.X, npc.Position.Z), new Vector2((float)at.X, (float)at.Z)) < 0.5f,
                $"{what}: {npc.Position}");
            var ground = plateau.NavMesh.GroundHeight(new Vector3((float)at.X, (float)at.Y, (float)at.Z));
            Assert.IsNotNull(ground, $"{what} has navmesh under it");
            Assert.IsTrue(System.Math.Abs(ground.Value - at.Y) < 0.5f, $"{what} at {at.Y}, ground {ground}");
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        /// <summary>
        /// The server's shrine of Past, from the World's logos table, placed on the test Plateau as the harness
        /// places the Wilderness's (LogosManager.LogosInit, then the dynamic object worker): the harness made
        /// its shrines before this map was there.
        /// </summary>
        private static Logos Shrine(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            var entry = harness.World.LogosEntries.AsNoTracking().Single(row => row.Id == Past);
            Assert.AreEqual(PlateauMissionDataV1.MapContextId, entry.MapContextId);
            var shrine = new Logos(entry);
            plateau.DynamicObjects.Add(shrine);
            harness.Objects.DynamicObjectWorker(plateau, 0);
            harness.Drain();
            return shrine;
        }

        /// <summary>The shrine used, from where it stands, and the use's ten seconds run out (LogosShrineUseArgTests).</summary>
        private static void UseShrine(WildernessRuntimeTestHarness harness, MapChannel plateau, Logos shrine)
        {
            harness.MoveTo(shrine.Position);
            harness.Drain();
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = shrine.EntityId, ActionId = ActionId.UseObject, ActionArgId = DynamicObjectManager.LogosUseArgId
            });
            ActorActionManager.Instance.DoWork(plateau, 10001);
            harness.Drain();
        }
    }
}
