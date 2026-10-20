using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// A creature's rushing blow is a charge (KaelRushingBlow): the Kael's falls on whoever stands
    /// in its EFFECT_RADIUS where its target stood, and the AFS soldiers' CR_HUMAN_RUSHING_BLOW,
    /// with no radius, on its target alone. The Fithik's CR_FITHIK_LUNGE is a leap at the end of
    /// its windup, its blow on its target if the target is still in reach where it lands.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureChargeTests
    {
        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly Dictionary<ActionId, ActionInfo> _previous = new Dictionary<ActionId, ActionInfo>();

        private long _clock = 1_000_000;

        [TestCleanup]
        public void Cleanup()
        {
            KaelRushingBlow.Now = () => System.Environment.TickCount64;

            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();

            foreach (var (id, previous) in _previous)
            {
                if (previous != null)
                    Actions()[id] = previous;
                else
                    Actions().Remove(id);
            }

            _previous.Clear();
        }

        [TestMethod]
        public void BothRushingBlowsAreChargesAndOnlyTheKaelsHasAnAreaWithoutOneInItsData()
        {
            Assert.IsTrue(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrKaelRushingBlow }));
            Assert.IsTrue(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrHumanRushingBlow }));
            Assert.IsFalse(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrKaelSmash }));
            Assert.IsFalse(KaelRushingBlow.Is(null));

            var kael = new ActionLevelInfo { ActionId = ActionId.CrKaelRushingBlow, Level = 1 };
            kael.Properties[AbilityProperty.EffectRadius] = 15;

            Assert.AreEqual(15f, KaelRushingBlow.RadiusOf(ActionId.CrKaelRushingBlow, kael));
            Assert.AreEqual(KaelRushingBlow.DefaultRadius, KaelRushingBlow.RadiusOf(ActionId.CrKaelRushingBlow, Human()));
            Assert.AreEqual(0f, KaelRushingBlow.RadiusOf(ActionId.CrHumanRushingBlow, Human()), "the AFS soldier's: its target alone");
        }

        [TestMethod]
        public void AnAfsSoldierChargesItsTargetAndStrikesItAloneWhereverItHasGot()
        {
            Seed(ActionId.CrHumanRushingBlow, Human());

            using var world = new WorldTestContext();
            var soldier = Place(world, TargetCategory.Friendly, new Vector3(0, 0, 0));
            var bane = Place(world, TargetCategory.Hostile, new Vector3(0, 0, -15));
            var beside = Place(world, TargetCategory.Hostile, new Vector3(1, 0, -15));
            var row = new CreatureAction { ActionId = ActionId.CrHumanRushingBlow, ActionArgId = 1, RangeMin = 1, RangeMax = 20, Cooldown = 5000, WindupTime = 1598, MinDamage = 34, MaxDamage = 46 };

            KaelRushingBlow.Start(world.Map, soldier, row, bane, 40);

            Assert.IsTrue(KaelRushingBlow.IsCharging(soldier));
            Assert.IsTrue(soldier.IsRushing, "sent running");
            Assert.AreEqual(13f, Vector3.Distance(soldier.Position, soldier.RushTo.Value), 0.05f, "to 2 m short of where the Bane stood");

            // 15 m at 70 m/s is 214 ms, not the row's 1598.
            Assert.AreEqual(214, KaelRushingBlow.WindupMs(15, KaelRushingBlow.DefaultVelocity));

            // The Bane steps aside while the soldier runs; the blow is still its own.
            bane.Position = new Vector3(3, 0, -15);
            Thread.Sleep(300);

            KaelRushingBlow.Worker(world.Map);
            MissileManager.Instance.DoWork(world.Map, 250);

            Assert.IsFalse(KaelRushingBlow.IsCharging(soldier));
            Assert.IsFalse(soldier.IsRushing);
            Assert.AreEqual(-13f, soldier.Position.Z, 0.05f, "where the run ends");
            Assert.IsLessThan(10000, bane.Attributes[Attributes.Health].Current, "the blow on its target");
            Assert.AreEqual(10000, beside.Attributes[Attributes.Health].Current, "and nobody beside where it stood");

            world.Map.QueuedMissiles.Clear();
        }

        [TestMethod]
        public void AFithikLungeIsALeapTimedToLandAsItsWindupEnds()
        {
            Assert.IsTrue(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrFithikLunge }));
            Assert.IsFalse(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrFithikAcidSpray }));

            // 28 m at 30 m/s is 933 ms: it leaps 267 ms into its 1200 ms windup.
            Assert.AreEqual(267, KaelRushingBlow.FithikLeapAt(1200, 28, 30));

            // Nothing to cross: it strikes where it stands as the windup ends.
            Assert.AreEqual(1200, KaelRushingBlow.FithikLeapAt(1200, 0, 30));

            // A run the windup is too short for starts with it.
            Assert.AreEqual(0, KaelRushingBlow.FithikLeapAt(1200, 40, 30));
            Assert.AreEqual(0, KaelRushingBlow.FithikLeapAt(0, 10, 30));
        }

        [TestMethod]
        public void AFithikWindsUpWhereItStandsLeapsForWhereItsTargetIsAndBitesItOnLanding()
        {
            Seed(ActionId.CrFithikLunge, Lunge(), "CR_FITHIK_LUNGE", "abilities.ai.fithiklungeability");

            KaelRushingBlow.Now = () => _clock;

            using var world = new WorldTestContext();
            var fithik = Place(world, TargetCategory.Hostile, new Vector3(0, 0, 0));
            var prey = Place(world, TargetCategory.Friendly, new Vector3(0, 0, -20));
            var row = LungeRow();

            KaelRushingBlow.Start(world.Map, fithik, row, prey, 90);
            KaelRushingBlow.Worker(world.Map);

            Assert.IsTrue(KaelRushingBlow.IsCharging(fithik));
            Assert.IsFalse(fithik.IsRushing, "winding up where it stands: 18 m at 30 m/s is the windup's last 600 ms");

            // It moves while the Fithik winds up; the leap goes where it is now.
            prey.Position = new Vector3(0, 0, -25);
            _clock += 700;
            KaelRushingBlow.Worker(world.Map);

            Assert.IsTrue(fithik.IsRushing, "leaping");
            Assert.AreEqual(23f, Vector3.Distance(fithik.Position, fithik.RushTo.Value), 0.05f, "2 m short of where its target is now");
            Assert.AreEqual(10000, prey.Attributes[Attributes.Health].Current, "nothing has landed yet");

            _clock += 600;
            KaelRushingBlow.Worker(world.Map);
            MissileManager.Instance.DoWork(world.Map, 250);

            Assert.IsFalse(KaelRushingBlow.IsCharging(fithik));
            Assert.IsFalse(fithik.IsRushing);
            Assert.AreEqual(-23f, fithik.Position.Z, 0.05f, "where the leap ends");
            Assert.IsLessThan(10000, prey.Attributes[Attributes.Health].Current, "bitten where it stood");

            world.Map.QueuedMissiles.Clear();
        }

        [TestMethod]
        public void AFithikMissesATargetThatHasGotOutOfReachWhenItLands()
        {
            Seed(ActionId.CrFithikLunge, Lunge(), "CR_FITHIK_LUNGE", "abilities.ai.fithiklungeability");

            KaelRushingBlow.Now = () => _clock;

            using var world = new WorldTestContext();
            var fithik = Place(world, TargetCategory.Hostile, new Vector3(0, 0, 0));
            var prey = Place(world, TargetCategory.Friendly, new Vector3(0, 0, -20));

            KaelRushingBlow.Start(world.Map, fithik, LungeRow(), prey, 90);
            _clock += 700;
            KaelRushingBlow.Worker(world.Map);
            Assert.IsTrue(fithik.IsRushing);

            // Out of the way while it is in the air: six metres from where it comes down.
            prey.Position = new Vector3(6, 0, -18);
            _clock += 600;
            KaelRushingBlow.Worker(world.Map);
            MissileManager.Instance.DoWork(world.Map, 250);

            Assert.IsFalse(KaelRushingBlow.IsCharging(fithik));
            Assert.AreEqual(-18f, fithik.Position.Z, 0.05f);
            Assert.AreEqual(10000, prey.Attributes[Attributes.Health].Current, "missed: not bitten from six metres");

            world.Map.QueuedMissiles.Clear();
        }

        /// <summary>CR_FITHIK_LUNGE argument 1 as the world database has it.</summary>
        private static ActionLevelInfo Lunge()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrFithikLunge, Level = 1, WindupMs = 1200, RecoveryMs = 2350, MaxRange = 30, ReuseMs = 15000 };

            info.Properties[AbilityProperty.DamageAmountMin] = 50;
            info.Properties[AbilityProperty.DamageAmountMax] = 63;
            info.Properties[AbilityProperty.VfxVelocity] = 30;

            return info;
        }

        /// <summary>The Palisades Fithik's lunge row, 55007.</summary>
        private static CreatureAction LungeRow() => new CreatureAction
        {
            ActionId = ActionId.CrFithikLunge, ActionArgId = 1, RangeMin = 1, RangeMax = 30, Cooldown = 15000, WindupTime = 1200, MinDamage = 84, MaxDamage = 106, DamageType = 1
        };

        /// <summary>CR_HUMAN_RUSHING_BLOW argument 1 as the world database has it.</summary>
        private static ActionLevelInfo Human()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrHumanRushingBlow, Level = 1, WindupMs = 1598, MaxRange = 20 };

            info.Properties[AbilityProperty.DamageAmountMin] = 75;
            info.Properties[AbilityProperty.DamageAmountMax] = 100;
            info.Properties[AbilityProperty.Duration] = 1;
            info.Properties[AbilityProperty.DamageScaleType] = 2;
            info.Properties[AbilityProperty.KnockbackDistance] = 3;

            return info;
        }

        private void Seed(ActionId id, ActionLevelInfo info, string name = "CR_HUMAN_RUSHING_BLOW", string module = AbilityManager.RushingBlowModule)
        {
            var actions = Actions();

            if (!_previous.ContainsKey(id))
            {
                actions.TryGetValue(id, out var previous);
                _previous[id] = previous;
            }

            var action = new ActionInfo { ActionId = id, Name = name, Module = module };
            action.Levels[info.Level] = info;
            actions[id] = action;
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;

        private Creature Place(WorldTestContext world, TargetCategory category, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }
    }
}
