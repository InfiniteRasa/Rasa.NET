using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Missions.World;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    public partial class WildernessEscapeVelocityTests
    {
        private static Creature PersonalPierre(WildernessRuntimeTestHarness h, uint character = 1) =>
            h.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Single(c => c.SpawnPool?.SceneActorRole == "pierre" && c.SpawnPool.ScenarioOwnerCharacterId == character);

        [TestMethod]
        public void PersonalPierreRunsBothFixedRoutesAroundTheOrbAndDepartsWithoutReturningToPrison()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            var offer = Accept(h);
            var pierre = PersonalPierre(h, h.Client.Player.Id);
            var routes = RescueBinding(h).Routes;
            Assert.AreEqual(6f, routes["landing-zone"].Speed);
            Assert.AreEqual(6f, routes["boarding"].Speed);
            AssertGroundedRoute(h, pierre.Position, routes["landing-zone"]);
            AssertGroundedRoute(h, Position(routes["landing-zone"].Points.Last().Position), routes["boarding"]);
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(h.Client, offer));
            Assert.IsNull(NpcManager.NpcInReach(h.Client, offer.EntityId, null, "hidden prisoner"));
            h.Drain();
            new NpcManager(h, h.Manager).RequestNpcConverse(h.Client,
                new RequestNPCConversePacket { EntityId = offer.EntityId });
            Assert.IsFalse(h.Drain().OfType<ConversePacket>().Any(), "A stale public entity ID must not reopen the prison NPC.");
            Assert.IsFalse(CreatureGameplayRules.CanParticipateInCombat(offer));
            Assert.IsFalse(CreatureGameplayRules.CanParticipateInCombat(pierre));
            Assert.IsTrue(DamageImmunity.IsImmune(pierre, DamageType.Physical));
            h.Creatures.HandleCreatureKill(h.Map, pierre, null);
            Assert.AreNotEqual(CharacterState.Dead, pierre.State);
            var began = h.UtcNow;
            ReleaseForcefield(h);
            Assert.AreEqual(0U, pierre.SpawnPool.FollowOwnerCharacterId, "Fixed routes must never become follow-player AI.");
            Assert.AreEqual(h.Client.Player.Id, pierre.SpawnPool.RouteEscortOwnerCharacterId);
            Assert.IsTrue(CreatureGameplayRules.CanParticipateInCombat(pierre));
            var nearest = float.MaxValue;
            for (var tick = 0; tick < 1600 && !h.Client.Player.Missions[666].Completeable; tick++)
            {
                h.MoveTo(pierre.Position + new Vector3(0, 0, 1));
                h.Tick();
                var flat = new Vector2(pierre.Position.X, pierre.Position.Z);
                nearest = Math.Min(nearest, Vector2.Distance(flat, new Vector2(197.66f, -54.08f)));
            }
            Assert.IsTrue(h.Client.Player.Missions[666].Completeable, $"Stopped at {pierre.Position}");
            Console.WriteLine($"Pierre completed both running routes in {(h.UtcNow - began).TotalSeconds:F2} s; minimum orb clearance {nearest:F2} m.");
            Assert.IsTrue(nearest >= 4, $"Route crossed the orb: {nearest} m clearance");
            Assert.IsTrue((h.UtcNow - began).TotalSeconds < 300, "Running must leave time within the 420-second deadline.");
            h.Tick();
            Assert.IsNull(EntityManager.Instance.GetCreature(pierre.EntityId), "Boarded Pierre departs, rather than running back.");
            Assert.AreSame(offer, h.Npc(630070));
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(h.Client, offer));
            var rodriguez = h.Npc(172);
            h.MoveTo(rodriguez.Position);
            var npcs = new NpcManager(h, h.Manager);
            npcs.RequestNpcConverse(h.Client, new RequestNPCConversePacket { EntityId = rodriguez.EntityId });
            npcs.CompleteNPCMission(h.Client, new CompleteNPCMissionPacket
                { EntityId = rodriguez.EntityId, MissionId = 666, SelectionIdx = 0 });
            Assert.AreEqual(MissionState.Completed, h.Client.Player.Missions[666].State);
            var reconnect = h.Context.CreateCompetingClient(h.Manager);
            h.Manager.PublishInitialState(reconnect);
            Assert.IsTrue(reconnect.Player.MissionSuccessHistory.Contains(666));
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(reconnect, offer), "Completed rescue persists across hydration.");
            MissionTestContext.Drain(reconnect);
            h.Creatures.CreateCreatureOnClient(reconnect, offer);
            Assert.AreEqual(0, MissionTestContext.Drain(reconnect).Count);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(reconnect, rodriguez), "Ordinary NPCs must remain visible.");
        }

        [TestMethod]
        public void TwoPlayersCanRescuePierreIndependentlyAndOnlySeeTheirOwnEscort()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            var offer = Accept(h);
            var first = PersonalPierre(h, h.Client.Player.Id);
            var second = h.Context.CreateAdditionalClient(2, manager: h.Manager);
            try
            {
                second.SetWorldPosition(offer.Position, 0);
                CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(second, offer));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(second, first));
                var npcs = new NpcManager(h, h.Manager);
                npcs.RequestNpcConverse(second, new RequestNPCConversePacket { EntityId = offer.EntityId });
                npcs.AssignNPCMission(second, new AssignNPCMissionPacket { NpcEntityId = offer.EntityId, MissionId = 666 });
                Assert.AreEqual(MissionState.Active, second.Player.Missions[666].State);
                var other = PersonalPierre(h, second.Player.Id);
                Assert.AreNotEqual(first.EntityId, other.EntityId);
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(second, other));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(h.Client, other));
                MissionTestContext.Drain(second);
                h.Drain();
                h.Creatures.CreateCreatureOnClient(second, first);
                h.Creatures.CellDiscardCreaturesToClient(second, new System.Collections.Generic.List<Creature> { first });
                CellManager.Instance.CellCallMethod(first, new IsRunningPacket(true));
                Assert.AreEqual(0, MissionTestContext.Drain(second).Count, "Private creation/state must not reach a bystander.");
                Assert.IsTrue(h.Drain().OfType<IsRunningPacket>().Any());
                ReleaseForcefield(h);
                first.Attributes[Attributes.Health].Current = 0;
                h.Creatures.HandleCreatureKill(h.Map, first, null);
                h.Tick();
                Assert.AreEqual(MissionState.Failed, h.Client.Player.Missions[666].State);
                Assert.AreEqual(MissionState.Active, second.Player.Missions[666].State);
                Assert.IsTrue(CreatureManager.IsLivingOnMap(h.Map, other));
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(h.Client, offer), "Failed rescuers can retry.");
                MissionNpcVisibility.Refresh(h.Client);
                Assert.IsTrue(h.Client.VisibleMissionNpcs.Contains(offer.EntityId));
                Assert.IsNull(h.Manager.PublicActors.Handle(h.Map, 630070), "No global reservation may block another player.");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                h.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void PierreUpgradeRetiresAnOldExclusiveLeaseWithoutLosingTheRescue()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            Accept(h);
            var pierre = PersonalPierre(h, h.Client.Player.Id);
            var run = pierre.SpawnPool.SceneRunId;
            var assignment = h.Client.Player.Missions[666].AssignmentId;
            using (var unit = h.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Add(new Rasa.Structures.Char.MissionActorLeaseEntry
                {
                    MapKey = PublicActorLeaseService.MapKey(h.Map), SpawnKey = "spawn:630070",
                    RunId = run, ActorRole = "pierre", Generation = pierre.SpawnPool.SceneGeneration
                }));
            new PublicActorLeaseService(h, () => h.Manager).Tick(h.Map);
            using (var unit = h.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.Leases(run).Count);
                Assert.AreEqual("Running", unit.CharacterMissions.Runtime.Scene(run).Status);
            }
            Assert.AreEqual(assignment, h.Client.Player.Missions[666].AssignmentId);
            Assert.IsTrue(CreatureManager.IsLivingOnMap(h.Map, pierre));
        }

        [TestMethod]
        public void PersonalPierreKeepsTheOriginalDeadlineAcrossReconnectAndRestoresOfferOnExpiry()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            var offer = Accept(h);
            var due = h.UtcNow.AddSeconds(420);
            h.UtcNow = due.AddSeconds(-1);
            var reconnect = h.Context.CreateCompetingClient(h.Manager);
            h.Manager.PublishInitialState(reconnect);
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(reconnect, offer));
            using (var unit = h.CreateChar())
                Assert.AreEqual(due, unit.CharacterMissionDeadlines.Get(reconnect.Player.Id, 666).DueAtUtc);
            h.UtcNow = due;
            Assert.IsTrue(h.Manager.EvaluateDeadlines(reconnect));
            Assert.AreEqual(MissionState.Failed, reconnect.Player.Missions[666].State);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(reconnect, offer));
        }
    }
}
