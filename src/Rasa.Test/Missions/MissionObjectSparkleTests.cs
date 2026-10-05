using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    // The client's sparkle on an object a mission wants used (MissionObjects): UsableInfo's
    // missionActivated, for the player whose active objective a use of the object would move on.
    // Bootcamp's equipment crate is the object: mission 1992 objective 1 is completed by it,
    // and it is locked until Delessio's briefing (objective 4) makes that objective active.
    [TestClass]
    [DoNotParallelize]
    public class MissionObjectSparkleTests
    {
        private const uint Mission = 1992;
        private const uint CrateClass = 29877;

        [TestMethod]
        public void TheCrateSparklesWhileItsObjectiveIsActiveAndNotBeforeOrAfter()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var crate = Crate(harness);

            // Not on the mission: an object like any other.
            harness.Drain();
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.AreEqual(0u, MissionObjects.ActivationFor(harness.Client, crate, harness.Manager));
            Assert.AreEqual(0, harness.Client.MissionObjects.Count);

            // Accepted, the crate's objective not yet active: still nothing.
            AcceptGearingUp(harness);
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.AreEqual(0u, MissionObjects.ActivationFor(harness.Client, crate, harness.Manager));
            Assert.IsFalse(Infos(harness, crate).Any(info => info.MissionActivated != 0));

            // Delessio's briefing makes it the thing to do: in service, and mission activated.
            Brief(harness);

            Assert.AreEqual(Mission, MissionObjects.ActivationFor(harness.Client, crate, harness.Manager));
            var lit = Infos(harness, crate).Last();
            Assert.IsTrue(lit.Enabled);
            Assert.AreEqual(Mission, lit.MissionActivated);
            Assert.IsTrue(harness.Client.MissionObjects.Contains(crate.EntityId));

            // Opened and emptied: the objective is done and the effect is taken off - out of
            // service with no activation, which is what detaches it on the client.
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = crate.LootDispenserEntityId });
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Mission].Objectives[1].State);

            Assert.AreEqual(0u, MissionObjects.ActivationFor(harness.Client, crate, harness.Manager));
            var calls = Calls(harness, crate);
            var off = calls.FindLastIndex(packet => packet is UsableInfoPacket info && info.MissionActivated == 0 && !info.Enabled);
            Assert.IsTrue(off >= 0, "sent out of service with no activation");
            Assert.IsFalse(calls.Skip(off + 1).OfType<UsableInfoPacket>().Any(info => info.MissionActivated != 0));
            Assert.AreEqual(crate.IsEnabled, calls.Skip(off + 1).OfType<SetUsablePacket>().Any(packet => packet.IsEnabled),
                "and back in service if it is");
            Assert.IsFalse(harness.Client.MissionObjects.Contains(crate.EntityId));
        }

        [TestMethod]
        public void AClientThatMeetsTheCrateMidObjectiveIsGivenItSparkling()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var crate = Crate(harness);

            AcceptGearingUp(harness);
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Brief(harness);
            harness.Drain();
            harness.Client.MissionObjects.Clear();

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, crate);

            var made = harness.Drain().OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == crate.EntityId);
            Assert.AreEqual(Mission, made.EntityData.OfType<UsableInfoPacket>().Single().MissionActivated);
            Assert.IsTrue(harness.Client.MissionObjects.Contains(crate.EntityId));
        }

        [TestMethod]
        public void OnlyTheObjectsOfAnActiveObjectivesInteractionAreWanted()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var crate = Crate(harness);

            Assert.AreEqual(0, harness.Manager.WantedInteractions(harness.Client.Player).Count);

            AcceptGearingUp(harness);
            Assert.AreEqual(0, harness.Manager.WantedInteractions(harness.Client.Player).Count, "objective 1 is not active yet");

            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Brief(harness);

            var wanted = harness.Manager.WantedInteractions(harness.Client.Player);
            Assert.AreEqual(Mission, wanted[CrateClass]);
            Assert.AreEqual(1, wanted.Count);

            // An object of another class beside it is not.
            var other = harness.BootcampMap.DynamicObjects.First(candidate => (uint)candidate.EntityClassId != CrateClass);
            Assert.AreEqual(0u, MissionObjects.ActivationFor(harness.Client, other, harness.Manager));
        }

        #region Fixture

        private static DynamicObject Crate(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.DynamicObjects.Single(candidate => (uint)candidate.EntityClassId == CrateClass);

        private static void AcceptGearingUp(BootcampRuntimeTestHarness.Harness harness)
        {
            var mcAllister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);

            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(mcAllister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, mcAllister.EntityId, Mission));
            harness.Drain();
        }

        /// <summary>Delessio's gear briefing, objective 4: the crate's objective becomes active and the crate opens.</summary>
        private static void Brief(BootcampRuntimeTestHarness.Harness harness)
        {
            var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioPackageId);

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, Mission, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Mission].Objectives[1].State);
        }

        /// <summary>What the client has been sent on the crate since it was last read, in order.</summary>
        private static List<PythonPacket> Calls(BootcampRuntimeTestHarness.Harness harness, DynamicObject crate) =>
            WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == crate.EntityId).Select(message => message.Packet).ToList();

        private static List<UsableInfoPacket> Infos(BootcampRuntimeTestHarness.Harness harness, DynamicObject crate) =>
            Calls(harness, crate).OfType<UsableInfoPacket>().ToList();

        #endregion
    }
}
