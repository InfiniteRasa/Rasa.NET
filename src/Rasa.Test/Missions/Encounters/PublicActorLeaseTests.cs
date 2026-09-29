using System;
using Rasa.Missions.Scenes;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Data;
    using Rasa.Game.Missions.World;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class PublicActorLeaseTests
    {
        [TestMethod]
        [DataRow(CharacterState.Dead, 0, false)]
        [DataRow(CharacterState.Dying, 100, false)]
        [DataRow(CharacterState.Idle, 0, false)]
        [DataRow(CharacterState.Idle, 100, true)]
        public void PublicReservationRejectsActorsWithoutLivingHealth(
            CharacterState state, int health, bool missingHealth)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var actor = AddPublicActor(context);
            actor.State = state;
            actor.Attributes[Rasa.Data.Attributes.Health].Current = health;
            if (missingHealth)
                actor.Attributes.Remove(Rasa.Data.Attributes.Health);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsNull(context.Manager.PublicActors.Handle(context.Map, 77));
            using var database = context.Open();
            Assert.AreEqual(0, database.Set<MissionActorLeaseEntry>().Count());
        }

        [TestMethod]
        public void RetainedCorpseDoesNotBlockItsLivingPublicReplacement()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var corpse = AddPublicActor(context);
            corpse.State = CharacterState.Dead;
            corpse.Attributes[Rasa.Data.Attributes.Health].Current = 0;
            var replacement = AddPublicActor(context);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, replacement.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            Assert.IsTrue(context.Manager.PublicActors.TryResolve(context.Map, handle, out var resolved));
            Assert.AreSame(replacement, resolved);
            Assert.IsTrue(context.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(corpse)),
                "Reservation must preserve PR105's retained corpse rather than deleting it to avoid ambiguity.");
        }

        [TestMethod]
        public void ConcurrentAcceptancesReserveOnePublicActorAndPublishOneSuccess()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var second = context.CreateAdditionalClient(2);
            var actor = AddPublicActor(context);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));
            var results = new bool[2];
            Parallel.Invoke(
                () => results[0] = context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321),
                () => results[1] = context.Manager.AcceptOfferedMission(second, actor.EntityId, 321));

            Assert.AreEqual(1, results.Count(accepted => accepted));
            Assert.AreEqual(1, context.Drain().Concat(MissionTestContext.Drain(second))
                .OfType<MissionGainedPacket>().Count());
            using var database = context.Open();
            Assert.AreEqual(1, database.Set<MissionActorLeaseEntry>().Count());
            Assert.AreEqual(1, database.Set<MissionSceneEntry>().Count());
            Assert.IsFalse(actor.IsInteractable);
        }

        [TestMethod]
        public void SaveFailureLeavesTheNpcAvailableAndTheMissionUnaccepted()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var actor = AddPublicActor(context);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));
            context.BeforeSave = _ => throw new DbUpdateException("Injected lease transaction failure.");

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsTrue(actor.IsInteractable);
            Assert.IsNull(actor.Controller.ScriptedMove);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
            context.BeforeSave = null;
            using (var database = context.Open())
            {
                Assert.AreEqual(0, database.Set<MissionSceneEntry>().Count());
                Assert.AreEqual(0, database.Set<MissionActorLeaseEntry>().Count());
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
        }

        [TestMethod]
        public void ResetReleasesForAnotherCharacterWithoutLettingOldHandlesMutateIt()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var second = context.CreateAdditionalClient(2);
            var actor = AddPublicActor(context);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var old = context.Manager.PublicActors.Handle(context.Map, 77);
            Assert.IsNotNull(old);
            Assert.IsTrue(context.Manager.PublicActors.BeginReset(context.Map, old.RunId, "Completed"));
            context.Manager.PublicActors.Tick(context.Map);
            Assert.IsTrue(actor.IsInteractable);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(second, actor.EntityId, 321));
            Assert.IsFalse(context.Manager.PublicActors.TryResolve(context.Map, old, out _));
            Assert.IsFalse(context.Manager.PublicActors.BeginReset(context.Map, old.RunId, "Stale"));
            Assert.IsFalse(actor.IsInteractable);
        }

        private static Creature AddPublicActor(MissionTestContext context)
        {
            var actor = context.AddNpc(77);
            actor.IsInteractable = true;
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = actor.Position
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            context.Drain();
            return actor;
        }
    }
}
