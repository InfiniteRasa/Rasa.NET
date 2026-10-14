using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A creature's health comes back once its fight is over (CreatureHealth), and a creature
    /// that cannot press its fight - its target where it cannot go, or inside the reach of all
    /// its attacks - gives it up and goes home whole (BehaviorManager.Stalled). Before, a
    /// creature kept every point of damage until it happened to leash, and stood "in the fight"
    /// for as long as it was shot at (BR-181).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureRecoveryTests
    {
        private const long Tick = 250;

        [TestMethod]
        public void AWoundedCreatureAtRestRegainsATenthASecondAndNothingInAFight()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 10);
            var creature = Spawn(world, x: 0, maxHealth: 1000);
            var health = creature.Attributes[Attributes.Health];
            health.Current = 100;

            Assert.AreEqual(100, health.RefreshAmount, "the resting rate is on the attribute from the start");
            Assert.AreEqual(1, health.RefreshPeriod);

            CreatureArmor.Regenerate(world.Map);
            Assert.AreEqual(200, health.Current);
            Assert.AreEqual(0, HealthUpdates(enemy, creature).Count, "the clients predict a rate they already have");

            CreatureArmor.Regenerate(world.Map);
            Assert.AreEqual(300, health.Current);

            // The fight starts: the rate goes to 0 and the clients are told at once, and nothing
            // comes back while it lasts.
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));

            Assert.AreEqual(0, health.RefreshAmount);
            var told = HealthUpdates(enemy, creature).Single();
            Assert.AreEqual(0, told.Health.RefreshAmount);
            Assert.AreEqual(300, told.Health.Current);

            CreatureArmor.Regenerate(world.Map);
            CreatureArmor.Regenerate(world.Map);
            Assert.AreEqual(300, health.Current, "nothing in a fight");
            Assert.AreEqual(0, HealthUpdates(enemy, creature).Count, "and nothing more to tell");

            // The fight is given up - its target died, left or went out of sight: the rate is back.
            BehaviorManager.Instance.GiveUp(creature);
            WorldTestContext.Drain(enemy);
            CreatureArmor.Regenerate(world.Map);

            Assert.AreEqual(400, health.Current);
            told = HealthUpdates(enemy, creature).Single();
            Assert.AreEqual(100, told.Health.RefreshAmount);

            for (var second = 0; second < 10; second++)
                CreatureArmor.Regenerate(world.Map);

            Assert.AreEqual(1000, health.Current, "whole again, and no further");
        }

        // Frighten, Confusion and Subversion drop the creature's fight as they land, and out of its
        // fight it regained a tenth of its health a second: a 15-second Frighten left it whole.
        [TestMethod]
        [DataRow(AbilityManager.MindControlFrighten)]
        [DataRow(AbilityManager.MindControlConfusion)]
        [DataRow(AbilityManager.MindControlSubversion)]
        public void ACreatureTurnedOnItsOwnSideRegainsNothingUntilTheControlEnds(int pump)
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 10);
            var creature = Spawn(world, x: 0, maxHealth: 1000);
            var health = creature.Attributes[Attributes.Health];
            health.Current = 300;

            var control = new GameEffect
            {
                TypeId = AbilityManager.MindControlTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                ExpiresTick = System.Environment.TickCount64 + 15_000,
                MindControlPump = pump
            };
            GameEffectManager.Instance.Attach(world.Map, creature, control);
            HealthUpdates(enemy, creature);

            CreatureArmor.Regenerate(world.Map);
            CreatureArmor.Regenerate(world.Map);

            Assert.AreEqual(300, health.Current, "nothing while it is held");
            Assert.AreEqual(0, HealthUpdates(enemy, creature).Single().Health.RefreshAmount, "and the clients are told the rate is 0");

            GameEffectManager.Instance.DettachEffect(world.Map, creature, control);
            CreatureArmor.Regenerate(world.Map);

            Assert.AreEqual(400, health.Current, "its health comes back once the control is over");
        }

        [TestMethod]
        public void AnEnslavedCreatureRestsAsItsCastersOwn()
        {
            var creature = new Creature { TargetCategory = TargetCategory.Hostile };
            creature.ActiveEffects[1] = new GameEffect { TypeId = AbilityManager.MindControlTypeId, EffectId = 1, MindControlPump = AbilityManager.MindControlEnslavement };

            Assert.IsTrue(CreatureHealth.Rests(creature));
        }

        [TestMethod]
        public void TheRateIsAtLeastOneAndNoneForWhatTakesNoPartInFights()
        {
            Assert.AreEqual(1, CreatureHealth.BaseRegen(5));
            Assert.AreEqual(0, CreatureHealth.BaseRegen(0));
            Assert.AreEqual(50, CreatureHealth.BaseRegen(500));

            var decoration = new Creature { TargetCategory = TargetCategory.Decoration };
            Assert.IsFalse(CreatureHealth.Rests(decoration));
            Assert.AreEqual(0, CreatureHealth.RateFor(decoration, new ActorAttributes(Attributes.Health, 500, 500, 100, 0, 1)));

            var hostile = new Creature { TargetCategory = TargetCategory.Hostile };
            Assert.IsTrue(CreatureHealth.Rests(hostile));
            hostile.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            Assert.IsFalse(CreatureHealth.Rests(hostile));
        }

        [TestMethod]
        public void ACreatureThatCanNeitherStrikeNorCloseGivesTheFightUpAndGoesHomeWhole()
        {
            using var world = new WorldTestContext();
            // Inside the reach of its only attack: it can neither use it nor, at 2 m, close in.
            var enemy = Watch(world, x: 2);
            var creature = Spawn(world, x: 0, maxHealth: 1000, rangeMin: 4);
            creature.Attributes[Attributes.Health].Current = 300;

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));

            // Short of the time, still in the fight.
            for (var ms = 0L; ms + Tick < BehaviorManager.StallEvadeMs; ms += Tick)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, creature.Controller.CurrentAction);
            Assert.AreEqual(300, creature.Attributes[Attributes.Health].Current);

            // The time up: it leashes home - here - and is whole again.
            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
            Assert.AreEqual(BehaviorManager.BehaviorActionReturning, creature.Controller.CurrentAction);

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
            Assert.AreEqual(BehaviorManager.BehaviorActionWander, creature.Controller.CurrentAction);
            Assert.AreEqual(1000, creature.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0UL, Threat.ChooseTarget(creature), "forgotten");
        }

        [TestMethod]
        public void ACreaturePutHomeWhenItsRunTimesOutStandsThereAsFarAsTheServerKnows()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, x: 0, maxHealth: 1000);
            creature.Position = new Vector3(20, 0, 0);

            // Its last step was a run.
            creature.Controller.LastMovement = new Rasa.Models.Movement(creature.Position, 5f, 0x08, new Vector2(0f, 0f));
            Assert.IsTrue(ToHit.IsMoving(creature));

            BehaviorManager.Instance.Leash(world.Map, creature);
            creature.Controller.ActionReturning.Elapsed = creature.Controller.ActionReturning.TimeoutMs;
            WorldTestContext.Drain(watcher);

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.AreEqual(0f, creature.Position.X, 0.01f, "put home");
            Assert.AreEqual(0f, creature.Controller.LastMovement.Velocity);
            Assert.AreEqual(creature.Position, creature.Controller.LastMovement.Position);
            Assert.IsFalse(ToHit.IsMoving(creature), "a shot at it takes no moving penalty");
            Assert.IsTrue(WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<MoveObjectMessage>()
                .Any(move => move.EntityId == creature.EntityId && move.Movement.Velocity == 0f), "and the clients are told it stands there");
        }

        [TestMethod]
        public void AStrikeOrAStepPutsTheStallClockBack()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 2);
            var creature = Spawn(world, x: 0, maxHealth: 1000, rangeMin: 4);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));

            for (var i = 0; i < 20; i++)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.AreEqual(20 * Tick, creature.Controller.ActionFighting.StalledMs);

            BehaviorManager.MadeProgress(creature);
            Assert.AreEqual(0, creature.Controller.ActionFighting.StalledMs);

            // Held still, the time does not count against it.
            creature.MovementSpeed = 0;
            for (var i = 0; i < 20; i++)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.AreEqual(0, creature.Controller.ActionFighting.StalledMs);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, creature.Controller.CurrentAction);
        }

        [TestMethod]
        public void MinionsEscortsEmplacementsAndTheUnarmedDoNotEvade()
        {
            var armed = new Creature { TargetCategory = TargetCategory.Hostile };
            armed.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, RangeMax = 30 });
            Assert.IsTrue(BehaviorManager.Evades(armed));

            var unarmed = new Creature { TargetCategory = TargetCategory.Hostile };
            Assert.IsFalse(BehaviorManager.Evades(unarmed));

            var minion = new Creature { TargetCategory = TargetCategory.Friendly, MasterEntityId = 42 };
            minion.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, RangeMax = 30 });
            Assert.IsFalse(BehaviorManager.Evades(minion));
        }

        private static Creature Spawn(WorldTestContext world, float x, int maxHealth, double rangeMin = 0)
        {
            var creature = new Creature
            {
                Name = "Patient",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(maxHealth);
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 1, RangeMin = rangeMin, RangeMax = 30, MinDamage = 1, MaxDamage = 1, Cooldown = 1000 });
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        /// <summary>
        /// The health updates about the creature that reached the client. WorldTestContext.Drain
        /// gives the protocol packets as sent, a call to a method wrapped in a CallMethodMessage,
        /// so the update is the call's packet; looked for among the protocol packets themselves
        /// it was never there, and every check for none passed whatever was sent.
        /// </summary>
        private static List<UpdateHealthPacket> HealthUpdates(Client client, Creature creature) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Where(call => call.EntityId == creature.EntityId)
                .Select(call => call.Packet)
                .OfType<UpdateHealthPacket>()
                .ToList();

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
