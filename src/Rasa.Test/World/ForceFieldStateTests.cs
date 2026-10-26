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

    /// <summary>
    /// A faction force field's health states shown a band at a time (ForceFields.Steps): the client
    /// drops a Use between two states FORCEFIELD has no transition for.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ForceFieldStateTests
    {
        /// <summary>usabledata.usableaugmentationstatetransition[62]: (from, to) pairs.</summary>
        private static readonly HashSet<(int, int)> Transitions = new()
        {
            (191, 192), (191, 196), (192, 191), (192, 193), (192, 196), (193, 192), (193, 194), (193, 196),
            (194, 191), (194, 193), (194, 196), (196, 191), (196, 197), (197, 191), (197, 196), (197, 198),
            (198, 191), (198, 197), (198, 199), (199, 191), (199, 196), (199, 198)
        };

        private static readonly UseObjectState[][] Chains =
        {
            new[] { UseObjectState.FfStateFactionAIntact, UseObjectState.FfStateFactionA75pHealth, UseObjectState.FfStateFactionA25pHealth, UseObjectState.FfStateFactionADestroyed },
            new[] { UseObjectState.FfStateFactionBIntact, UseObjectState.FfStateFactionB75pHealth, UseObjectState.FfStateFactionB25pHealth, UseObjectState.FfStateFactionBDestroyed }
        };

        [TestMethod]
        public void EveryWayBetweenTwoHealthStatesIsOneTheClientHas()
        {
            foreach (var chain in Chains)
                foreach (var from in chain)
                    foreach (var to in chain)
                    {
                        var steps = ForceFields.Steps(ForceFields.Kind.Faction, from, to);

                        if (from == to)
                        {
                            Assert.IsEmpty(steps.ToList());
                            continue;
                        }

                        Assert.AreEqual(to, steps.Last(), $"{from} to {to} ends there");

                        var current = from;
                        foreach (var step in steps)
                        {
                            Assert.Contains(((int)current, (int)step), Transitions, $"{from} to {to}: {current} to {step}");
                            current = step;
                        }
                    }
        }

        [TestMethod]
        public void DestroyedStraightToIntactIsOneStepAndTheOtherKindsAreOne()
        {
            CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionBIntact },
                ForceFields.Steps(ForceFields.Kind.Faction, UseObjectState.FfStateFactionBDestroyed, UseObjectState.FfStateFactionBIntact).ToList());
            CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionA75pHealth, UseObjectState.FfStateFactionAIntact },
                ForceFields.Steps(ForceFields.Kind.Faction, UseObjectState.FfStateFactionA25pHealth, UseObjectState.FfStateFactionAIntact).ToList());
            CollectionAssert.AreEqual(new[] { UseObjectState.StateDestroyed },
                ForceFields.Steps(ForceFields.Kind.Clan, UseObjectState.CffStateBaneOwns, UseObjectState.StateDestroyed).ToList());
        }

        [TestMethod]
        public void AHitAcrossBandsAKillAndARepairAreShownAStateAtATime()
        {
            using var world = new WorldTestContext();
            var watcher = world.CreateClient(x: 10, z: 5);
            CellManager.Instance.AddToWorld(watcher);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("banemajor"), ForceFields.Side.B, new Vector3(10, 0, 0), 0f, 1000);

            try
            {
                WorldTestContext.Drain(watcher);

                // Intact to 25% in one blow.
                ForceFields.Damage(field, 800, 0);
                CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionB75pHealth, UseObjectState.FfStateFactionB25pHealth }, Uses(watcher, field));

                // Repaired from 25%: back up through 75%.
                ForceFields.Repair(field, 0);
                CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionB75pHealth, UseObjectState.FfStateFactionBIntact }, Uses(watcher, field));

                // Killed from intact (a GM's .destructible kill takes all its hit points at once).
                ForceFields.Damage(field, field.Health, 0);
                CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionB75pHealth, UseObjectState.FfStateFactionB25pHealth, UseObjectState.FfStateFactionBDestroyed },
                    Uses(watcher, field));
                Assert.AreEqual(UseObjectState.FfStateFactionBDestroyed, field.Object.StateId);

                // And up again in the one step the class has.
                ForceFields.Repair(field, 0);
                CollectionAssert.AreEqual(new[] { UseObjectState.FfStateFactionBIntact }, Uses(watcher, field));
                Assert.AreEqual(UseObjectState.FfStateFactionBIntact, field.Object.StateId);

                // A hit within a band changes no state.
                ForceFields.Damage(field, 100, 0);
                Assert.IsEmpty(Uses(watcher, field));
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        /// <summary>usabledata for class 10000083 (OWNABLEFORCEFIELD 84): states 2 and 170, transitions 2 to 170 and 170 to 2.</summary>
        private static readonly HashSet<(int, int)> OwnableTransitions = new() { (2, 170), (170, 2) };

        [TestMethod]
        public void AnOwnableFieldStandsActiveAndGoesDownAndUpByTheTransitionsItsClassHas()
        {
            Assert.AreEqual(UseObjectState.StateActive, ForceFields.StateOf(ForceFields.Kind.Ownable, ForceFields.Side.A, 100, 100));
            Assert.AreEqual(UseObjectState.StateDestroyed, ForceFields.StateOf(ForceFields.Kind.Ownable, ForceFields.Side.B, 0, 100));

            using var world = new WorldTestContext();
            var watcher = world.CreateClient(x: 10, z: 5);
            CellManager.Instance.AddToWorld(watcher);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("ownable"), ForceFields.Side.B, new Vector3(10, 0, 0), 0f, 1000);

            try
            {
                Assert.AreEqual(UseObjectState.StateActive, field.Object.StateId, "created in the state its hum and loop effect are keyed on");
                WorldTestContext.Drain(watcher);

                ForceFields.Damage(field, 400, 0);
                Assert.IsEmpty(Uses(watcher, field), "no bands: standing is one state");

                ForceFields.Damage(field, field.Health, 0);
                var down = Uses(watcher, field);
                CollectionAssert.AreEqual(new[] { UseObjectState.StateDestroyed }, down);
                Assert.Contains(((int)UseObjectState.StateActive, (int)down.Single()), OwnableTransitions);

                ForceFields.Repair(field, 0);
                var up = Uses(watcher, field);
                CollectionAssert.AreEqual(new[] { UseObjectState.StateActive }, up);
                Assert.Contains(((int)UseObjectState.StateDestroyed, (int)up.Single()), OwnableTransitions);
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        private static List<UseObjectState> Uses(Client client, ForceFields.Field field) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == field.Object.EntityId)
                .Select(message => message.Packet).OfType<UsePacket>().Select(use => use.CurState).ToList();
    }
}
