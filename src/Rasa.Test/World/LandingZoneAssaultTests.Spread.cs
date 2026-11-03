using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;

namespace Rasa.Test.World
{
    public partial class LandingZoneAssaultTests
    {
        [TestMethod]
        public void AWaveSpreadsOverTheDefendersRatherThanFilingAfterOne()
        {
            _world.SpawnWorld(Point.AfsPools.ToArray());
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            LandingZoneAssaults.Worker(_world.Map, Point);
            var fighting = Troops.Where(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting).ToArray();
            var targets = fighting.GroupBy(c => c.Controller.ActionFighting.TargetEntityId).Select(g => g.Count()).OrderByDescending(n => n).ToArray();
            Assert.IsTrue(fighting.Length >= 20, $"{fighting.Length} fighting");
            Assert.IsTrue(targets.Length >= 2, $"targets: {string.Join(", ", targets)}");
            Assert.IsTrue(targets[0] <= fighting.Length * 0.6, $"shared out, not all drawn by the nearest: {string.Join(", ", targets)}");
        }

        [TestMethod]
        public void TroopsKeepTakingOnTheGarrisonUntilItIsAllDead()
        {
            _world.SpawnWorld(Point.AfsPools.ToArray());
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            LandingZoneAssaults.Worker(_world.Map, Point);
            var garrison = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && Point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) == null && CreatureManager.IsLivingOnMap(_world.Map, c)).ToList();
            Assert.IsTrue(garrison.Count >= 3);
            Assert.IsFalse(Troops.Any(c => c.OnClaimDuty));

            // Whoever a troop was on falls: the troop takes on another of them, on the next pass.
            for (var round = 0; round < 3 && garrison.Count > 1; round++)
            {
                var troop = Troops.First(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting);
                var target = garrison.Single(g => g.EntityId == troop.Controller.ActionFighting.TargetEntityId);
                target.Attributes[Attributes.Health].Current = 0;
                CreatureManager.Instance.HandleCreatureKill(_world.Map, target, null);
                garrison.Remove(target);
                BehaviorManager.Instance.GiveUp(troop);
                LandingZoneAssaults.Worker(_world.Map, Point);
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, troop.Controller.CurrentAction, $"round {round}");
                Assert.IsTrue(garrison.Any(g => g.EntityId == troop.Controller.ActionFighting.TargetEntityId), "on to another of the garrison");
            }

            // Nobody left idle while the garrison lives.
            foreach (var troop in Troops) BehaviorManager.Instance.GiveUp(troop);
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(Troops.All(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting),
                $"{Troops.Count(c => c.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)} of {Troops.Length} idle");
        }

        [TestMethod]
        public void TroopsWithNobodyToFightSpreadOverTheCourtyard()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            LandingZoneAssaults.Worker(_world.Map, Point);
            var walking = Troops.Where(c => !c.OnClaimDuty && c.Controller.ActionWander.Arriving).ToArray();
            var centre = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
            var spots = walking.Select(c => c.Controller.ActionWander.WanderDestination).ToArray();
            Assert.IsTrue(walking.Length >= 20);
            Assert.IsTrue(spots.All(p => Vector3.Distance(p, centre) <= 14), "about the point");
            Assert.IsTrue(spots.Select(p => (Math.Round(p.X), Math.Round(p.Z))).Distinct().Count() >= walking.Length * 2 / 3,
                "each to a place of its own, not all to the one spot");
        }

        [TestMethod]
        public void TheGateFieldShotDownStaysDownForTheAssaultAndTenMinutesAtLeast()
        {
            var clock = PlacedDestructibles.Now;
            PlacedDestructibles.Now = () => _now;
            try
            {
                PlacedDestructibles.Init(_world);
                PlacedDestructibles.Worker(_world.Map);
                var field = ForceFields.OnMap(_world.Map).Single(f => Vector3.Distance(f.Position, Point.Position) < 40);
                Assert.AreEqual(ForceFields.Side.A, field.Side);

                // On an ordinary day: ten minutes.
                ForceFields.Damage(field, field.Health, 0);
                PlacedDestructibles.Worker(_world.Map);
                _now += PlacedDestructibles.FieldRespawnMs - 1;
                PlacedDestructibles.Worker(_world.Map);
                Assert.IsTrue(field.IsDestroyed, "not back in a minute");
                _now += 1;
                PlacedDestructibles.Worker(_world.Map);
                Assert.IsFalse(field.IsDestroyed);

                // Broken in an assault: not back while it goes on.
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
                ForceFields.Damage(field, field.Health, 0);
                PlacedDestructibles.Worker(_world.Map);
                _now += PlacedDestructibles.FieldRespawnMs;
                LandingZoneAssaults.Worker(_world.Map, Point);
                PlacedDestructibles.Worker(_world.Map);
                Assert.IsTrue(field.IsDestroyed, "the assault is still on");
                _now += 600000;
                LandingZoneAssaults.Worker(_world.Map, Point);
                Assert.AreEqual(0, Pools.Length, "the assault is over");
                PlacedDestructibles.Worker(_world.Map);
                Assert.IsFalse(field.IsDestroyed, "and the field is back");
            }
            finally
            {
                PlacedDestructibles.Forget(_world.Map);
                PlacedDestructibles.Load(null);
                PlacedDestructibles.Reset();
                PlacedDestructibles.Now = clock;
            }
        }
    }
}
