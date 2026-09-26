using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.World;

    // Lightning's arc runs through AbilityManager.Lightning.cs (ResolveLightningExtras ->
    // NearestHostiles). These are the target-selection rules from the PR #96 lightning tests,
    // run against that path.
    [TestClass]
    [DoNotParallelize]
    public class LightningConsolidationTests
    {
        [TestMethod]
        public void LightningArcSelectionIsBoundedDistinctAndDeterministic()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var farther = AddTarget(world, new Vector3(16, 0, 0));
            var tiedA = AddTarget(world, new Vector3(13, 4, 0));
            var tiedB = AddTarget(world, new Vector3(13, -4, 0));
            var expected = tiedA.EntityId < tiedB.EntityId ? tiedA : tiedB;

            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
            {
                cell.CreatureList.Reverse();
                if (cell.CreatureList.Contains(expected))
                    cell.CreatureList.Add(expected);
            }

            var selected = AbilityManager.NearestHostiles(world.Map, client.Player, primary, 5, 1);

            Assert.AreEqual(1, selected.Count);
            Assert.AreSame(expected, selected.Single());
            Assert.IsFalse(selected.Contains(primary));
            Assert.IsFalse(selected.Contains(farther));
            Cleanup(world, primary, farther, tiedA, tiedB);
        }

        [TestMethod]
        public void LightningArcSelectionRejectsInvalidTargetsAndNonfiniteRange()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var friendly = AddTarget(world, new Vector3(11, 0, 0));
            friendly.TargetCategory = TargetCategory.Friendly;
            var dead = AddTarget(world, new Vector3(12, 0, 0));
            dead.State = CharacterState.Dead;
            var missingHealth = AddTarget(world, new Vector3(14, 0, 0));
            missingHealth.Attributes.Remove(Attributes.Health);

            Assert.AreEqual(0, AbilityManager.NearestHostiles(world.Map, client.Player, primary, float.NaN, 1).Count);
            Assert.AreEqual(0, AbilityManager.NearestHostiles(world.Map, client.Player, primary, 12, 4).Count);
            Cleanup(world, primary, friendly, dead, missingHealth);
        }

        private static Creature AddTarget(WorldTestContext world, Vector3 position)
        {
            var target = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = position,
                State = CharacterState.Normal,
                TargetCategory = TargetCategory.Hostile,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(world.Map, target);
            return target;
        }

        private static void Cleanup(WorldTestContext world, params Creature[] targets)
        {
            foreach (var target in targets)
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, target);
        }
    }
}
