using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessAliaOpeningTests
    {
        [TestMethod]
        public void SolisCheckInRequiresTheAssignedRangerToReachHisHut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteEscort(harness);
        }

        [TestMethod]
        public void TwoCharactersCanAcceptAndCompleteIndependentEscortsAtTheSameTime()
        {
            using var harness=WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(100,64,184,219);var rogers=harness.Npc(100);var moawi=harness.Npc(64);var solis=harness.Npc(184);
            harness.MoveTo(rogers.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,rogers.EntityId,1407));
            var first=AssignedRanger(harness,harness.Client.Player.Id);
            var second=harness.Context.CreateAdditionalClient(2,manager:harness.Manager);
            try
            {
                second.SetWorldPosition(rogers.Position,0);CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second,rogers.EntityId,1407),"Another character's active escort must not block acceptance.");
                var other=AssignedRanger(harness,second.Player.Id);
                Assert.AreNotEqual(first.EntityId,other.EntityId);
                Assert.AreNotEqual(first.SpawnPool.SceneRunId,other.SpawnPool.SceneRunId);
                Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map,WildernessOpeningWorldV1.RangerSpawnId));
                harness.MoveTo(moawi.Position);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client,moawi.EntityId,1407,1,1));
                second.SetWorldPosition(moawi.Position,0);CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(second,moawi.EntityId,1407,1,1));
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(second,solis.EntityId,1407,10,1));
                for(var tick=0;tick<900 && (harness.Client.Player.Missions[1407].Objectives[10].State!=MissionObjectiveState.Incomplete ||
                    second.Player.Missions[1407].Objectives[10].State!=MissionObjectiveState.Incomplete);tick++)
                {
                    harness.MoveTo(first.Position+new Vector3(0,0,1));
                    second.SetWorldPosition(other.Position+new Vector3(0,0,1),0);CellManager.Instance.UpdateVisibility(second);
                    harness.Tick();
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete,harness.Client.Player.Missions[1407].Objectives[10].State);
                Assert.AreEqual(MissionObjectiveState.Incomplete,second.Player.Missions[1407].Objectives[10].State);
                harness.MoveTo(solis.Position);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client,solis.EntityId,1407,10,1));
                Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client,solis.EntityId,1407,null));
                Assert.AreEqual(MissionState.Active,second.Player.Missions[1407].State,"First player's reward cannot complete the other's assignment.");
                second.SetWorldPosition(solis.Position,0);CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(second,solis.EntityId,1407,10,1));
                Assert.IsTrue(harness.Manager.CompleteOfferedMission(second,solis.EntityId,1407,null));
            }
            finally {CellManager.Instance.RemoveFromWorld(second);harness.Map.ClientList.Remove(second);}
        }

        [TestMethod]
        public void HistoricalPublicRangerReservationReproducesTheAcceptanceFailure()
        {
            using var harness=WildernessRuntimeTestHarness.Create(targetWorldMigration:"20270908000000_Cache_of_the_day_containers");
            harness.SpawnWorld(100,WildernessOpeningWorldV1.RangerSpawnId);var rogers=harness.Npc(100);harness.MoveTo(rogers.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,rogers.EntityId,1407));
            var second=harness.Context.CreateAdditionalClient(2,manager:harness.Manager);
            try
            {
                second.SetWorldPosition(rogers.Position,0);CellManager.Instance.UpdateVisibility(second);
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(second,rogers.EntityId,1407));
                Assert.IsFalse(second.Player.Missions.ContainsKey(1407));
            }
            finally{CellManager.Instance.RemoveFromWorld(second);harness.Map.ClientList.Remove(second);}
        }

        [TestMethod]
        public void ReconnectingKeepsTheSamePersonalAssignmentAndDoesNotCreateAnotherEscort()
        {
            using var harness=WildernessRuntimeTestHarness.Create();harness.SpawnWorld(100,64,184);
            harness.MoveTo(harness.Npc(100).Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,harness.Npc(100).EntityId,1407));
            var ranger=AssignedRanger(harness,harness.Client.Player.Id);var run=ranger.SpawnPool.SceneRunId;
            harness.MoveTo(harness.Npc(64).Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client,harness.Npc(64).EntityId,1407,1,1));
            for(var i=0;i<20;i++){harness.MoveTo(ranger.Position+new Vector3(0,0,1));harness.Tick();}
            var at=ranger.Position;var assignment=harness.Client.Player.Missions[1407].AssignmentId;
            var reconnected=harness.Context.CreateCompetingClient(harness.Manager);
            harness.Manager.PublishInitialState(reconnected);harness.Manager.PublishInitialState(reconnected);
            var resumed=AssignedRanger(harness,reconnected.Player.Id);
            Assert.AreEqual(run,resumed.SpawnPool.SceneRunId);Assert.AreEqual(assignment,reconnected.Player.Missions[1407].AssignmentId);
            Assert.IsTrue(Vector3.Distance(at,resumed.Position)<2,"Reconnect must not reset the route to its start.");
            for(var i=0;i<900 && reconnected.Player.Missions[1407].Objectives[10].State!=MissionObjectiveState.Incomplete;i++)
            {reconnected.SetWorldPosition(resumed.Position+new Vector3(0,0,1),0);CellManager.Instance.UpdateVisibility(reconnected);harness.Tick();}
            Assert.AreEqual(MissionObjectiveState.Incomplete,reconnected.Player.Missions[1407].Objectives[10].State);
        }

        [TestMethod]
        public void PersonalRangerDeathFailsOnlyItsOwnersEscort()
        {
            using var harness=WildernessRuntimeTestHarness.Create();harness.SpawnWorld(100);var rogers=harness.Npc(100);harness.MoveTo(rogers.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,rogers.EntityId,1407));
            var first=AssignedRanger(harness,harness.Client.Player.Id);
            var second=harness.Context.CreateAdditionalClient(2,manager:harness.Manager);
            try
            {
                second.SetWorldPosition(rogers.Position,0);CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second,rogers.EntityId,1407));var other=AssignedRanger(harness,second.Player.Id);
                first.Attributes[Attributes.Health].Current=0;harness.Creatures.HandleCreatureKill(harness.Map,first,null);harness.Tick();
                Assert.AreEqual(MissionObjectiveState.Failed,harness.Client.Player.Missions[1407].Objectives[10].State);
                Assert.AreEqual(MissionState.Active,second.Player.Missions[1407].State);
                Assert.AreNotEqual(MissionObjectiveState.Failed,second.Player.Missions[1407].Objectives[10].State);
                Assert.IsTrue(CreatureManager.IsLivingOnMap(harness.Map,other));
            }
            finally{CellManager.Instance.RemoveFromWorld(second);harness.Map.ClientList.Remove(second);}
        }

        [TestMethod]
        public void OldExclusiveReservationIsRetiredWithoutDiscardingTheAssignment()
        {
            using var harness=WildernessRuntimeTestHarness.Create();harness.SpawnWorld(100);harness.MoveTo(harness.Npc(100).Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,harness.Npc(100).EntityId,1407));
            var ranger=AssignedRanger(harness,harness.Client.Player.Id);var run=ranger.SpawnPool.SceneRunId;
            var assignment=harness.Client.Player.Missions[1407].AssignmentId;
            using(var unit=harness.CreateChar()) unit.ExecuteTransaction(()=>unit.CharacterMissions.Runtime.Add(new Rasa.Structures.Char.MissionActorLeaseEntry
            {MapKey=Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(harness.Map),SpawnKey="spawn:630001",RunId=run,ActorRole="ranger",Generation=ranger.SpawnPool.SceneGeneration}));
            var recovered=new Rasa.Game.Missions.World.PublicActorLeaseService(harness,()=>harness.Manager);recovered.Tick(harness.Map);
            using(var unit=harness.CreateChar())
            {Assert.AreEqual(0,unit.CharacterMissions.Runtime.Leases(run).Count);Assert.AreEqual("Running",unit.CharacterMissions.Runtime.Scene(run).Status);}
            Assert.AreEqual(assignment,harness.Client.Player.Missions[1407].AssignmentId);
            Assert.IsTrue(CreatureManager.IsLivingOnMap(harness.Map,ranger));
        }
        private static Rasa.Structures.Creature AssignedRanger(WildernessRuntimeTestHarness harness,uint owner)=>
            EntityManager.Instance.Creatures.Values.Single(c=>c.SpawnPool?.SceneActorRole=="ranger" &&
                c.SpawnPool.ScenarioMissionId==1407 && c.SpawnPool.ScenarioOwnerCharacterId==owner && CreatureManager.IsLivingOnMap(harness.Map,c));
        [TestMethod]
        public void OpeningChainRecognizesOwnedLogosAndCollectsRealThraxHeartLoot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteEscort(harness);
            var solis = harness.Npc(184);
            var apirka = harness.Npc(219);
            using (var unit = harness.CreateChar())
                unit.CharacterLogoses.SetLogos(1, 10);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, solis.EntityId, 1069));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1069].Objectives[1].State);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, solis.EntityId, 1069, 2, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, apirka.EntityId, 1069, 3, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, apirka.EntityId, 1069, 0));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, apirka.EntityId, 479));
            var alias = ItemManager.Instance.CreateFromTemplateId(16540, 12);
            Assert.IsNotNull(alias);
            Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, alias));
            Assert.AreEqual(0U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346],
                "An active same-class heart alias must not count as the authored template2285.");
            Assert.AreEqual(0U, HeldQuantity(harness, 2285));

            for (var count = 1U; count <= 12; count++)
            {
                var pool = harness.Map.SpawnPools.Single(pool => pool.DbId == 580019);
                harness.SpawnWorldAfter(pool.RespawnTime, 580019);
                var thrax = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .FirstOrDefault(creature => creature.DbId == 3 && creature.State != CharacterState.Dead);
                Assert.IsNotNull(thrax, $"No live Thrax at kill {count}; alive={pool.AliveCreatures}, queued={pool.QueuedCreatures}, " +
                    $"respawn={pool.RespawnTime}, update={pool.UpdateTimer}.");
                harness.MoveTo(thrax.Position);
                thrax.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(harness.Map, thrax, harness.Client.Player);
                var loot = harness.Map.LootDispensers[thrax.CorpseLootEntityId];
                Assert.IsNotNull(loot.LootItems.SingleOrDefault(item => item.ItemTemplateId == 2285));
                Assert.AreEqual(count - 1, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
                LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                    new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                Assert.AreEqual(count, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346],
                    $"Loot taken={loot.FullyLooted}, enabled={loot.IsLootable}, player HP=" +
                    $"{harness.Client.Player.Attributes[Attributes.Health].Current}, player state={harness.Client.Player.State}, " +
                    $"corpse HP={thrax.Attributes[Attributes.Health].Current}, corpse state={thrax.State}, " +
                    $"corpse age={thrax.Controller.DeadTime}, distance={Vector3.Distance(harness.Client.Player.Position, thrax.Position)}");
            }
            Assert.IsTrue(harness.Client.Player.Missions[479].Completeable);
            Assert.AreEqual(12U, HeldQuantity(harness, 2285));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, apirka.EntityId, 479, null));
            Assert.AreEqual(0U, HeldQuantity(harness, 2285));
            Assert.AreEqual(12U, HeldQuantity(harness, 16540));
            Assert.AreEqual(1U, HeldQuantity(harness, 20846));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, apirka.EntityId, 479, null));
            Assert.AreEqual(1U, HeldQuantity(harness, 20846));
        }

        private static uint HeldQuantity(WildernessRuntimeTestHarness harness, uint template)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Where(row => row.CharacterId == harness.Client.Player.Id)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (total, item) => total + item.StackSize);
        }

        private static void ReachSolis(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(100, 64, 184, 219, WildernessOpeningWorldV1.RangerSpawnId);
            var rogers = harness.Npc(100);
            var moawi = harness.Npc(64);
            var solis = harness.Npc(184);
            Assert.IsNull(harness.Npc(WildernessOpeningWorldV1.RangerSpawnId));
            var hut = solis.Position;

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, rogers.EntityId, 1407));
            var ranger = AssignedRanger(harness,harness.Client.Player.Id);
            var start = ranger.Position;
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, moawi.EntityId, 1407, 1, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, solis.EntityId, 1407, 10, 1),
                "Talking to Solis must not replace the escort.");
            for (var tick = 0; tick < 600 &&
                 harness.Client.Player.Missions[1407].Objectives[10].State != MissionObjectiveState.Incomplete; tick++)
            {
                harness.MoveTo(ranger.Position + new Vector3(0, 0, 1));
                harness.Tick();
            }
            Assert.IsTrue(Vector3.Distance(start, ranger.Position) > 50);
            Assert.IsTrue(Vector3.Distance(hut, solis.Position) < 0.5f,
                $"Solis left the authored check-in hut: start={hut}, current={solis.Position}.");
            Assert.IsTrue(Vector3.Distance(solis.Position, ranger.Position) < 2,
                $"Ranger ended at {ranger.Position}, Solis is at {solis.Position}; objective state is " +
                harness.Client.Player.Missions[1407].Objectives[10].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1407].Objectives[10].State);
        }

        private static void CompleteEscort(WildernessRuntimeTestHarness harness)
        {
            ReachSolis(harness);
            var solis = harness.Npc(184);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, solis.EntityId, 1407, 10, 1));
            Assert.IsTrue(harness.Client.Player.Missions[1407].Completeable);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, solis.EntityId, 1407, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[1407].State);
        }

        [TestMethod]
        public void MigratedOpeningUsesPublicWorldAndRealNpcSpawns()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            harness.SpawnWorld();

            foreach (var (spawnId, creatureId, packageId) in new[]
            {
                (100U, 100U, 116U),
                (64U, 38U, 113U),
                (184U, 42U, 168U),
                (219U, 43U, 112U)
            })
            {
                var npc = harness.Npc(spawnId);
                Assert.IsNotNull(npc, $"Migrated spawn {spawnId} must introduce a real mission NPC.");
                Assert.AreEqual(creatureId, npc.DbId);
                Assert.AreEqual(packageId, npc.Npc.NpcPackageId);
                var surface = harness.Map.NavMesh.GroundHeight(npc.Position);
                Assert.IsNotNull(surface);
                Assert.IsTrue(System.Math.Abs(surface.Value - npc.Position.Y) < 0.5f);
            }
            foreach (var missionId in new uint[] { 1407, 1069, 479, 1449 })
                Assert.IsTrue(harness.Manager.LoadedMissions.TryGetValue(missionId, out var mission) &&
                    mission.IsOperational, $"Opening mission {missionId} is not operational.");
            Assert.AreEqual(1, harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Count(creature => creature.DbId == 43),
                "Apirka must not also be spawned as a multi-member patrol.");
        }
    }
}
