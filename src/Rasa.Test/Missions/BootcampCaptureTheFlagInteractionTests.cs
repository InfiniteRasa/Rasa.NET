using System;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCaptureTheFlagInteractionTests
    {
        private static readonly uint[] ForeanNames = { 7874, 7890, 7986 };

        [TestMethod]
        public void TheBridgeHasTheCorrectInfantryAndAfsFightingBeforeTheCaveExit()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var actors = Actors(harness);
            Assert.IsFalse(actors.Any(actor => actor.DbId is 520009 or 520010),
                "The Collector and The Dissector must not occupy the Capture the Flag bridge.");
            var infantry = actors.Where(actor => actor.NameId == 7674).ToArray();
            Assert.IsTrue(infantry.Length >= 2, "The bridge needs Thrax Infantry Initiates.");
            Assert.IsTrue(infantry.All(actor => (uint)actor.EntityClass == 29769));
            var soldiers = actors.Where(actor => actor.Faction == Factions.AFS &&
                actor.Position.X > 280 && actor.Position.X < 345 && actor.Position.Z > 50 && actor.Position.Z < 80).ToArray();
            Assert.IsTrue(soldiers.Length >= 2, "AFS soldiers must already be fighting on the bridge.");
            foreach (var actor in infantry.Concat(soldiers))
                AssertGrounded(harness, actor);
            var armorBefore = infantry.Concat(soldiers).Sum(actor => actor.Attributes[Attributes.Armor].Current);
            var healthBefore = infantry.Concat(soldiers).Sum(actor => actor.Attributes[Attributes.Health].Current);

            AdvanceCombat(harness, 24);

            Assert.IsTrue(infantry.Any(actor => actor.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting));
            Assert.IsTrue(soldiers.Any(actor => actor.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting));
            Assert.IsTrue(infantry.Concat(soldiers).Sum(actor => actor.Attributes[Attributes.Armor].Current) < armorBefore ||
                          infantry.Concat(soldiers).Sum(actor => actor.Attributes[Attributes.Health].Current) < healthBefore,
                "Both factions must exchange real damaging attacks, not just stand next to each other.");
        }

        [TestMethod]
        public void ThreeExistingForeansJoinAsMarkedEscortsWhenCaptureTheFlagIsAccepted()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var foreans = Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray();
            Assert.AreEqual(3, foreans.Length, "The Foreans must be present at the firing range before accepting the mission.");
            CollectionAssert.AreEquivalent(ForeanNames, foreans.Select(actor => actor.NameId).ToArray());
            Assert.IsTrue(foreans.All(actor => actor.SpawnPool.FollowOwnerCharacterId == 0));
            foreach (var forean in foreans)
                AssertGrounded(harness, forean);

            AcceptCapture(harness);

            CollectionAssert.AreEquivalent(foreans,
                Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray());
            Assert.IsTrue(foreans.All(actor =>
                actor.SpawnPool.FollowOwnerCharacterId == harness.Client.Player.Id &&
                actor.Controller.CurrentAction == BehaviorManager.BehaviorActionFollow));
            Assert.AreEqual(3, harness.Drain().OfType<UpdateEscortStatusPacket>().Count(packet => packet.IsEscort));
        }

        [TestMethod]
        public void BothSidesOfTheBridgeBattleRespawnWithoutBecomingEscorts()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var thrax = Actors(harness).First(actor => actor.DbId == 510216);
            var soldier = Actors(harness).First(actor => actor.DbId == 510217);
            foreach (var actor in new[] { thrax, soldier })
                KillWithPlayerMissile(harness, actor);

            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 19999);
            Assert.IsFalse(Actors(harness).Any(actor =>
                actor.State != CharacterState.Dead &&
                (ReferenceEquals(actor.SpawnPool, thrax.SpawnPool) || ReferenceEquals(actor.SpawnPool, soldier.SpawnPool))));
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 1);

            foreach (var previous in new[] { thrax, soldier })
            {
                var replacement = Actors(harness).Single(actor =>
                    actor.State != CharacterState.Dead && ReferenceEquals(actor.SpawnPool, previous.SpawnPool));
                Assert.AreNotEqual(previous.EntityId, replacement.EntityId);
                Assert.AreEqual(0U, replacement.SpawnPool.FollowOwnerCharacterId);
                AssertGrounded(harness, replacement);
            }
        }

        [TestMethod]
        public void EscortMarkersReturnOnVisibilityAndFreshReconnectWithoutDuplicatingForeans()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            AcceptCapture(harness);
            harness.Drain();
            harness.MovePlayerTo(Vector3.Zero);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            harness.MovePlayerTo(new Vector3(380, 120, 158));
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.AreEqual(3, harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Count(packet => packet.EntityData.OfType<UpdateEscortStatusPacket>().Any(status => status.IsEscort)));

            harness.ReconnectFresh();
            harness.MovePlayerTo(new Vector3(380, 120, 158));
            CellManager.Instance.UpdateVisibility(harness.Client);

            var foreans = Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray();
            Assert.AreEqual(3, foreans.Length);
            Assert.IsTrue(foreans.All(actor => actor.SpawnPool.FollowOwnerCharacterId == harness.Client.Player.Id));
            Assert.AreEqual(3, harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Count(packet => packet.EntityData.OfType<UpdateEscortStatusPacket>().Any(status => status.IsEscort)));
        }

        [TestMethod]
        public void ForeanEscortsFollowFromTheRangeToTheExistingCaveExit()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            AcceptCapture(harness);
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1994, 439, out var exit));
            var foreans = Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray();
            harness.MovePlayerTo(exit.Position);
            CellManager.Instance.UpdateVisibility(harness.Client);

            for (var tick = 0; tick < 1200 && foreans.Any(actor => Vector3.Distance(actor.Position, exit.Position) > 10); tick++)
                AdvanceCombat(harness, 1);

            foreach (var forean in foreans)
            {
                Assert.AreNotEqual(CharacterState.Dead, forean.State);
                Assert.IsTrue(Vector3.Distance(forean.Position, exit.Position) < 10,
                    $"Escort {forean.NameId} stopped at {forean.Position} before the cave exit {exit.Position}.");
            }
        }

        [TestMethod]
        public void ReconnectRestoresSurvivingForeansNearThePlayersSavedPosition()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ReachTizzik(harness);
            var fallen = Actors(harness).Single(actor => actor.NameId == 7874);
            KillWithPlayerMissile(harness, fallen);
            var savedPosition = new Vector3(100, 109.65f, 135);
            harness.MovePlayerTo(savedPosition);
            CellManager.Instance.UpdateVisibility(harness.Client);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => unit.Characters.SaveCharacter(harness.Client.Player));

            harness.ReconnectFresh();

            var survivors = Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray();
            CollectionAssert.AreEquivalent(new uint[] { 7890, 7986 }, survivors.Select(actor => actor.NameId).ToArray());
            foreach (var survivor in survivors)
            {
                Assert.IsTrue(Vector3.Distance(survivor.Position, savedPosition) < 8,
                    $"Escort {survivor.NameId} restarted at {survivor.Position}, away from its owner at {savedPosition}.");
                AssertGrounded(harness, survivor);
                Assert.AreEqual(harness.Client.Player.Id, survivor.SpawnPool.FollowOwnerCharacterId);
            }
        }

        [TestMethod]
        public void TizzikUsesRealCombatAndACompanionsKillingBlowAdvancesTheObjective()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ReachTizzik(harness);
            var tizzik = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.TizzikGiCreatureId);
            var forean = Actors(harness).First(actor => actor.SpawnPool.FollowOwnerCharacterId == harness.Client.Player.Id);
            harness.MovePlayerTo(tizzik.Position + new Vector3(0, 0, 8));
            CellManager.Instance.UpdateVisibility(harness.Client);
            var beforeArmor = harness.Client.Player.Attributes[Attributes.Armor].Current;
            var beforeHealth = harness.Client.Player.Attributes[Attributes.Health].Current;

            AdvanceCombat(harness, 24);

            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].Current < beforeArmor ||
                          harness.Client.Player.Attributes[Attributes.Health].Current < beforeHealth,
                "Tizzik must attack through the normal combat loop.");
            AssertGrounded(harness, tizzik);
            CreatureManager.Instance.SetLocation(forean, tizzik.Position + new Vector3(0, 0, 6), 0, 1985);
            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(forean, ActionId.WeaponAttack, 145, tizzik.EntityId, 0), 10000);
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);

            Assert.AreEqual(CharacterState.Dead, tizzik.State);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1994].Objectives[1].State,
                "The owned Forean escort's kill must count for the player.");
        }

        [TestMethod]
        public void OnlyTheOwningEscortCanCreditTizziksDefeat()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ReachTizzik(harness);
            var tizzik = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.TizzikGiCreatureId);
            var escort = Actors(harness).First(actor => ForeanNames.Contains(actor.NameId));
            escort.SpawnPool.FollowOwnerCharacterId = 999;
            CreatureManager.Instance.SetLocation(escort, tizzik.Position + new Vector3(0, 0, 6), 0, 1985);

            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(escort, ActionId.WeaponAttack, 145, tizzik.EntityId, 0), 10000);
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);

            Assert.AreEqual(CharacterState.Dead, tizzik.State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[1].State);
        }

        [TestMethod]
        public void YoungbloodArrivesAtTheBaseAndRemainsForTheNextMissionAfterReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ReachTizzik(harness);
            var tizzik = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.TizzikGiCreatureId);
            KillWithPlayerMissile(harness, tizzik);
            harness.UtcNow += TimeSpan.FromMilliseconds(6999);
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));
            Assert.IsFalse(Actors(harness).Any(actor => actor.DbId == BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId));
            harness.UtcNow += TimeSpan.FromMilliseconds(1);

            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            var youngblood = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            AssertGrounded(harness, youngblood);
            Assert.IsTrue(WorldTestContext.Drain(harness.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == youngblood.EntityId)
                .Select(message => message.Packet).OfType<NPCConversationStatusPacket>()
                .Any(packet => packet.ConvoStatusId == ConversationStatus.ObjectivComplete),
                "Youngblood must become speakable in the client without a reconnect.");
            harness.MovePlayerTo(youngblood);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, youngblood.EntityId, 1994, 3, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, youngblood.EntityId, 1994, null, null));
            harness.Manager.TickScenarios(harness.Client);
            Assert.IsTrue(Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId))
                .All(actor => actor.SpawnPool.FollowOwnerCharacterId == 0));

            harness.ReconnectFresh();

            youngblood = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            AssertGrounded(harness, youngblood);
            harness.MovePlayerTo(youngblood);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 1995));
            Assert.IsFalse(Actors(harness).Any(actor => actor.DbId == BootcampRuntimeTestHarness.TizzikGiCreatureId));
        }

        [TestMethod]
        public void ForeanEscortsCanFollowFromTheCaveExitToTheReclaimedBase()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ReachTizzik(harness);
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1994, 439, out var exit));
            var foreans = Actors(harness).Where(actor => ForeanNames.Contains(actor.NameId)).ToArray();
            Assert.AreEqual(3, foreans.Length);
            for (var index = 0; index < foreans.Length; index++)
            {
                var start = harness.BootcampMap.NavMesh.Nearest(exit.Position + new Vector3(index, 0, 0)).Value;
                CreatureManager.Instance.SetLocation(foreans[index], start, 0, 1985);
                var seed = CellManager.Instance.GetCellSeed(start);
                CreatureManager.Instance.CellUpdateLocation(harness.BootcampMap, foreans[index], seed & 0xFFFF, seed >> 16);
            }
            var destination = new Vector3(93.2f, 109.64925f, 137.5f);
            harness.MovePlayerTo(destination);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            var route = harness.BootcampMap.NavMesh.FindPath(exit.Position, destination, out var complete);

            AdvanceCombat(harness, 1200);

            foreach (var forean in foreans)
                Assert.IsTrue(Vector3.Distance(forean.Position, destination) < 10,
                    $"Escort {forean.NameId} stopped at {forean.Position}; route complete={complete}, route={string.Join("; ", route)}.");
        }

        private static void AcceptCapture(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
        }

        private static void ReachTizzik(BootcampRuntimeTestHarness.Harness harness)
        {
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            AcceptCapture(harness);
            var deSimone = Actors(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1994, 439, out var exit));
            var previousPosition = harness.Client.Player.Position;
            harness.MovePlayerTo(exit.Position);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(
                harness.Client, previousPosition, exit.Position));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1994].Objectives[2].State);
            BootcampRuntimeTestHarness.PrepareDirectDamageClient(harness.Client);
            harness.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
        }

        private static Creature[] Actors(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static void KillWithPlayerMissile(BootcampRuntimeTestHarness.Harness harness, Creature target)
        {
            harness.MovePlayerTo(target.Position + new Vector3(0, 0, 6));
            CellManager.Instance.UpdateVisibility(harness.Client);
            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, target.EntityId, 0), 10000);
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);
            Assert.AreEqual(CharacterState.Dead, target.State);
        }

        private static void AdvanceCombat(BootcampRuntimeTestHarness.Harness harness, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                MissileManager.Instance.DoWork(harness.BootcampMap, 250);
            }
        }

        private static void AssertGrounded(BootcampRuntimeTestHarness.Harness harness, Creature creature)
        {
            var ground = harness.BootcampMap.NavMesh.Nearest(creature.Position);
            Assert.IsTrue(ground.HasValue, $"No walkable surface for {creature.DbId} at {creature.Position}.");
            Assert.IsTrue(Vector3.Distance(ground.Value, creature.Position) < 0.5f,
                $"Creature {creature.DbId} at {creature.Position} must stand on {ground.Value}.");
        }
    }
}
