using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A Polymorph's two revives (AbilityManager.MorphRevive): the Bane Caretaker's Resuscitate
    // and the Thrax Technician's Jumpstart, aimed by a player at their own side's dead.
    [TestClass]
    [DoNotParallelize]
    public class MorphReviveTests
    {
        private const uint MapId = 1220;
        private const uint RedClanId = 900041;
        private const uint BlueClanId = 900042;

        private const string Resuscitate = AbilityManager.CaretakerReviveModule;
        private const string Jumpstart = AbilityManager.TechnicianReviveModule;

        private Func<IEnumerable<(uint, uint, Vector3, string)>> _source;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;
        private readonly List<Creature> _creatures = new List<Creature>();

        [TestInitialize]
        public void UseTestHospitals()
        {
            _source = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;

            Hospitals.Source = () => new[] { (105u, MapId, new Vector3(-300, 0, 0), "Hospital: Twin Pillars") };
            Hospitals.IsSafeZone = id => id == 105;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();
        }

        [TestCleanup]
        public void RestoreHospitals()
        {
            Hospitals.Source = _source;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            Hospitals.Reset();

            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        public void TheTechnicianAndTheCaretakerHaveTheirRevivesInTheDrawer()
        {
            // "Ability: Deploy Turret / Repair (Machine Only) / Revive (Machine Only)".
            CollectionAssert.AreEqual(
                new[] { (ActionId.CrTechnicianTurret, 3u), (ActionId.CrTechnicianHeal, 5u), (ActionId.CrTechnicianRevive, 5u) },
                AbilityManager.MorphVariants[1868].Abilities);

            // "Ability: Heal (Biological Only) / Revive (Biological Only)", and the caretaker's attack.
            CollectionAssert.AreEqual(
                new[] { (ActionId.CrCaretakerHeal, 5u), (ActionId.CrCaretakerRevive, 5u), (ActionId.CrCaretakerAttack, 5u) },
                AbilityManager.MorphVariants[1858].Abilities);

            Assert.AreEqual(400, (int)ActionId.CrTechnicianRevive);
            Assert.AreEqual(242, (int)ActionId.CrCaretakerRevive);
        }

        [TestMethod]
        public void ResuscitateOffersADeadPlayerTheReviveAndTheyComeBackOnTakingIt()
        {
            using var world = new WorldTestContext();
            var caretaker = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            var standing = PlayerDeathTests.Player(world, 8, 0);

            caretaker.Player.Level = 9;
            Kill(world, mate);
            WorldTestContext.Drain(mate);

            Assert.IsTrue(AbilityManager.IsMorphRevivable(caretaker.Player, Resuscitate, mate.Player));
            Assert.IsFalse(AbilityManager.IsMorphRevivable(caretaker.Player, Jumpstart, mate.Player), "a player is no machine");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(caretaker.Player, Resuscitate, standing.Player), "alive");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(caretaker.Player, Resuscitate, caretaker.Player), "themselves");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(caretaker.Player, Resuscitate, null));

            // Jumpstart at them: performed, and nothing offered.
            var none = Revive(world, caretaker, Jumpstart, ActionId.CrTechnicianRevive, mate.Player.EntityId, 100, 100);

            Assert.AreEqual(0, none.Hits.Count);
            Assert.IsFalse(PlayerDeath.HasOffer(mate.Player, caretaker.Player.EntityId));

            // HEAL_AMOUNT 100 on the doubling-every-8-levels scale: 200 at level 9.
            var recovery = Revive(world, caretaker, Resuscitate, ActionId.CrCaretakerRevive, mate.Player.EntityId, 100, 100);

            Assert.AreEqual(AbilityRecoveryPacket.HitDataKind.Heal, recovery.Kind);
            Assert.AreEqual((mate.Player.EntityId, 0), (recovery.Hits.Single().EntityId, recovery.Hits.Single().Amount),
                "named, with no healing: the client's class draws nothing for a player until they take it");
            Assert.AreEqual(CharacterState.Dead, mate.Player.State, "theirs to take or leave");
            Assert.IsTrue(PlayerDeath.HasOffer(mate.Player, caretaker.Player.EntityId));

            var offer = Packets(mate).OfType<ReviveRequestInfoPacket>().Single();
            Assert.AreEqual(caretaker.Player.EntityId, offer.ReviverId);

            PlayerDeath.RequestRevive(mate, caretaker.Player.EntityId);

            Assert.AreNotEqual(CharacterState.Dead, mate.Player.State);
            Assert.AreEqual(200, mate.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(5f, mate.Player.Position.X, 0.01f, "where they lay");
        }

        [TestMethod]
        public void AnEnemysBodyIsNotOneToBringBack()
        {
            using var world = new WorldTestContext();
            var red = PlayerDeathTests.Player(world, 0, 0);
            var blue = PlayerDeathTests.Player(world, 6, 0);

            red.Player.ClanId = RedClanId;
            blue.Player.ClanId = BlueClanId;

            var online = world.Map.ClientList.ToList();

            lock (Server.Clients)
                Server.Clients.AddRange(online);

            ClanFeuds.Feud feud = null;

            try
            {
                Kill(world, blue);
                Assert.IsTrue(AbilityManager.IsMorphRevivable(red.Player, Resuscitate, blue.Player), "no quarrel between them");

                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = RedClanId, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = BlueClanId, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                Assert.IsFalse(AbilityManager.IsMorphRevivable(red.Player, Resuscitate, blue.Player));
                Assert.AreEqual(0, Revive(world, red, Resuscitate, ActionId.CrCaretakerRevive, blue.Player.EntityId, 100, 100).Hits.Count);
                Assert.IsFalse(PlayerDeath.HasOffer(blue.Player, red.Player.EntityId));
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in online)
                        Server.Clients.Remove(client);
            }
        }

        [TestMethod]
        public void EachBringsBackTheFriendlyDeadOfItsOwnKind()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 2, 0);

            var soldier = Corpse(world, 4, TargetCategory.Friendly);
            var turret = Corpse(world, 6, TargetCategory.Friendly, CreatureFlag.Mechanical);
            var machina = Corpse(world, 7, TargetCategory.Friendly, CreatureFlag.Machina);

            // Flesh for the Caretaker, machines for the Technician.
            Assert.IsTrue(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, soldier));
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Jumpstart, soldier), "Revive (Machine Only)");
            Assert.IsTrue(AbilityManager.IsMorphRevivable(player.Player, Jumpstart, turret));
            Assert.IsTrue(AbilityManager.IsMorphRevivable(player.Player, Jumpstart, machina));
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, turret), "Revive (Biological Only)");

            // What neither brings back.
            var bane = Corpse(world, 8, TargetCategory.Hostile);
            var alive = Corpse(world, 9, TargetCategory.Friendly);
            var summon = Corpse(world, 10, TargetCategory.Friendly);
            var scripted = Corpse(world, 11, TargetCategory.Friendly);
            var old = Corpse(world, 12, TargetCategory.Friendly);
            var destroyed = Corpse(world, 13, TargetCategory.Friendly);
            var owned = Corpse(world, 14, TargetCategory.Friendly);

            alive.State = CharacterState.Idle;
            summon.SpawnPool = null;
            scripted.IsScripted = true;
            old.Controller.DeadTime = CreatureSupport.ReviveWindowMs + 1;
            destroyed.CritKilled = true;
            owned.MasterEntityId = watcher.Player.EntityId;

            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, bane), "the other side's");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, alive));
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, summon), "a summon that was killed: nothing in the world to go back to");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, scripted));
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, old), "recently dead");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, destroyed), "a finishing move destroyed the body");
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, owned));

            old.Controller.DeadTime = CreatureSupport.ReviveWindowMs;
            Assert.IsTrue(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, old));

            // Resuscitate: up where it lay with the amount rolled, back in its pool's count, and everyone told.
            soldier.SpawnPool.AliveCreatures = 0;
            soldier.SpawnPool.DeadCreatures = 1;
            WorldTestContext.Drain(watcher);

            var recovery = Revive(world, player, Resuscitate, ActionId.CrCaretakerRevive, soldier.EntityId, 120, 120);

            Assert.AreEqual((soldier.EntityId, 120), (recovery.Hits.Single().EntityId, recovery.Hits.Single().Amount));
            Assert.AreNotEqual(CharacterState.Dead, soldier.State);
            Assert.AreEqual(120, soldier.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0, soldier.Controller.DeadTime);
            Assert.AreEqual((1, 0), (soldier.SpawnPool.AliveCreatures, soldier.SpawnPool.DeadCreatures));
            Assert.AreEqual(player.Player.EntityId, Packets(watcher).OfType<RevivedPacket>().Single().SourceId);
            Assert.IsFalse(AbilityManager.IsMorphRevivable(player.Player, Resuscitate, soldier), "up again");

            // Jumpstart: no more than the machine can hold.
            recovery = Revive(world, player, Jumpstart, ActionId.CrTechnicianRevive, turret.EntityId, 9000, 9000);

            Assert.AreEqual(500, recovery.Hits.Single().Amount);
            Assert.AreEqual(500, turret.Attributes[Attributes.Health].Current);
            Assert.AreNotEqual(CharacterState.Dead, turret.State);

            // The wrong kind, or one that got up in the windup: performed, and nobody gets up.
            Assert.AreEqual(0, Revive(world, player, Jumpstart, ActionId.CrTechnicianRevive, old.EntityId, 100, 100).Hits.Count);
            Assert.AreEqual(CharacterState.Dead, old.State);
            Assert.AreEqual(0, Revive(world, player, Jumpstart, ActionId.CrTechnicianRevive, turret.EntityId, 100, 100).Hits.Count);
            Assert.AreEqual(0, Revive(world, player, Resuscitate, ActionId.CrCaretakerRevive, 0, 100, 100).Hits.Count);
        }

        [TestMethod]
        public void TheRequestTakesADeadFriendAndNothingElse()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            // Jumpstart as the client's tables have it: 400 at its player-facing level 5.
            var manager = ToyTests.CreateManager(harness, 400, 5, 0);
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var near = player.Position + new Vector3(2, 0, 0);

            Assert.IsTrue(manager.TryGetAction(ActionId.CrTechnicianRevive, 5, out var module, out var info));
            Assert.AreEqual(Jumpstart, module);
            Assert.AreEqual(5, (int)info.MaxRange, "Jumpstart is used standing over the wreck");

            var wreck = Corpse(map, near, TargetCategory.Friendly, CreatureFlag.Mechanical);
            var running = Corpse(map, near, TargetCategory.Friendly, CreatureFlag.Mechanical);
            var bane = Corpse(map, near, TargetCategory.Hostile, CreatureFlag.Mechanical);
            var soldier = Corpse(map, near, TargetCategory.Friendly);
            var far = Corpse(map, player.Position + new Vector3(30, 0, 0), TargetCategory.Friendly, CreatureFlag.Mechanical);

            running.State = CharacterState.Idle;
            running.Attributes[Attributes.Health].Current = 500;

            bool Asked(ulong targetId)
            {
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(400, 5, 0, targetId: targetId));
                return map.PerformRecovery.Any(action => action.ActionId == ActionId.CrTechnicianRevive);
            }

            // Not a Technician: the ability is nobody's who has not the disguise.
            Assert.IsFalse(Asked(wreck.EntityId));

            player.MorphWeapon = new MorphWeaponItem();
            player.MorphAbilities = new List<(ActionId, uint)> { (ActionId.CrTechnicianRevive, 5u) };

            try
            {
                Assert.IsFalse(Asked(0), "nobody targeted");
                Assert.IsFalse(Asked(player.EntityId), "themselves");
                Assert.IsFalse(Asked(running.EntityId), "not dead");
                Assert.IsFalse(Asked(bane.EntityId), "the enemy's");
                Assert.IsFalse(Asked(soldier.EntityId), "no machine");
                Assert.IsFalse(Asked(far.EntityId), "out of reach");

                Assert.IsTrue(Asked(wreck.EntityId));
                Assert.AreEqual(CharacterState.Dead, wreck.State, "not until the windup is done");

                harness.Drain();
                ToyTests.Land(harness, manager, map.PerformRecovery.Single(action => action.ActionId == ActionId.CrTechnicianRevive));

                // HEAL_AMOUNT 100-100 at the performer's level 1.
                Assert.AreNotEqual(CharacterState.Dead, wreck.State);
                Assert.AreEqual(100, wreck.Attributes[Attributes.Health].Current);

                var recovery = harness.Drain().OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.CrTechnicianRevive);

                Assert.AreEqual(5u, recovery.ActionArgId);
                Assert.AreEqual((wreck.EntityId, 100), (recovery.Hits.Single().EntityId, recovery.Hits.Single().Amount));
            }
            finally
            {
                player.MorphWeapon = null;
                player.MorphAbilities = new List<(ActionId, uint)>();

                foreach (var cell in map.MapCellInfo.Cells.Values)
                    cell.CreatureList.RemoveAll(_creatures.Contains);
            }
        }

        #region Fixture

        private static AbilityRecoveryPacket Revive(WorldTestContext world, Client performer, string module, ActionId actionId, ulong targetId, int min, int max)
        {
            var info = new ActionLevelInfo { ActionId = actionId, Level = 5, MaxRange = 40 };

            info.Properties[AbilityProperty.HealAmountMin] = min;
            info.Properties[AbilityProperty.HealAmountMax] = max;
            info.Properties[AbilityProperty.DamageScaleType] = 2;

            var method = typeof(AbilityManager).GetMethod("MorphRevive", BindingFlags.Instance | BindingFlags.NonPublic)!;

            return (AbilityRecoveryPacket)method.Invoke(Abilities(),
                new object[] { world.Map, performer.Player, module, info, new ActionData(performer.Player, actionId, 5, targetId, 0) });
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static void Kill(WorldTestContext world, Client client)
        {
            client.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsTrue(PlayerDeath.AtZero(world.Map, client.Player, null));
        }

        private static List<Rasa.Packets.PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>().Select(message => message.Packet).ToList();

        private Creature Corpse(WorldTestContext world, float x, TargetCategory category, params CreatureFlag[] flags) =>
            Corpse(world.Map, new Vector3(x, 0, 0), category, flags);

        /// <summary>A dead creature of the world's own - with a spawn pool - on the players' side or the other.</summary>
        private Creature Corpse(MapChannel map, Vector3 position, TargetCategory category, params CreatureFlag[] flags)
        {
            var creature = new Creature
            {
                Name = "Corpse",
                TargetCategory = category,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Dead,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                SpawnPool = new SpawnPool { SpawnSlot = new List<SpawnPoolSlot>() },
                ExtraFlags = flags.ToList()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            _creatures.Add(creature);

            return creature;
        }

        #endregion
    }
}
