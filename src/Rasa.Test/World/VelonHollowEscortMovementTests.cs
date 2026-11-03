using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Structures;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class VelonHollowEscortMovementTests
    {
        private static string NavFile
        {
            get
            {
                var root = new DirectoryInfo(AppContext.BaseDirectory);
                while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln"))) root = root.Parent;
                return Path.Combine(root?.FullName ?? throw new AssertFailedException("Repository not found."),
                    "navmesh", "adv_foreas_howlingmaw_deathburrow.nav");
            }
        }
        private static readonly Lazy<CoverMesh> Collision = new(() => CoverMesh.Read(Path.ChangeExtension(NavFile, ".cover")));
        private static readonly Vector3 Hospital = new(165, 77, -520);
        private static readonly (Vector3 Start, Vector3 End)[] Routes = {
            (Hospital, new(171.9f, 72.2f, -470.6f)),
            (new(100.8f, 67.3f, -440), new(.2f, 65.2f, -482.3f)),
            (Hospital, new(-115.2f, 60.3f, -488.8f)),
            (Hospital, new(-119.3f, 81.8f, -279.5f)),
            (Hospital, new(631.6f, 49.8f, -343.2f))
        };

        [TestMethod]
        [DataRow(0, false)] [DataRow(0, true)]
        [DataRow(1, false)] [DataRow(1, true)]
        [DataRow(2, false)] [DataRow(2, true)]
        [DataRow(3, false)] [DataRow(3, true)]
        [DataRow(4, false)] [DataRow(4, true)]
        public void ActualScriptedEscortCompletesTheSectionsAndTempleRoutes(int routeIndex, bool reverse)
        {
            using var world = new WorldTestContext();
            world.Map.MapInfo = new MapInfo(2136, "adv_foreas_howlingmaw_deathburrow", 1, 0);
            var query = TestNavMeshes.Query(NavFile);
            world.Map.NavMesh = query;
            var route = Routes[routeIndex];
            var start = query.Nearest(reverse ? route.End : route.Start).Value;
            var end = query.Nearest(reverse ? route.Start : route.End).Value;
            var actor = new Creature {
                Name = "Velon escort fixture", EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle, MapContextId = 2136, RuntimeMapChannel = world.Map,
                Position = start, RunSpeed = 2.175f, WalkSpeed = 2.175f, AggroRange = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            actor.HomePos.Position = start;
            actor.HomePos.MapContextid = 2136;
            actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(actor, false);
            EntityManager.Instance.RegisterActor(actor.EntityId, actor);
            CellManager.Instance.AddToWorld(world.Map, actor);
            try
            {
                Assert.IsTrue(BehaviorManager.Instance.SetActionScriptedMove(world.Map, actor, end, 0));
                for (var tick = 0; tick < 12000 && !actor.Controller.ScriptedMove.Arrived; tick++)
                {
                    var previous = actor.Position;
                    BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                    var current = actor.Position;
                    // Observe the production worker's position. Do not re-ground it against
                    // arbitrary collision surfaces: there is a lower surface under the west cave floor.
                    if (InInvestigatedSection(previous) && InInvestigatedSection(current))
                    {
                        Assert.IsLessThan(.9f, Math.Abs(current.Y - previous.Y), $"Unexpected vertical jump at {current}.");
                        // The ramp repair has the tighter height requirement. The west section
                        // retains the shipped mesh's sub-meter approximation envelope.
                        AssertSupported(Collision.Value, current, InHospitalSection(current) ? .6f : 1f);
                        Assert.IsFalse(Collision.Value.GeometryBlocks(current + Vector3.UnitY * .1f, current + Vector3.UnitY * 2f));
                        foreach (var height in new[] { .8f, 1.6f })
                            Assert.IsFalse(Collision.Value.GeometryBlocks(previous + Vector3.UnitY * height, current + Vector3.UnitY * height),
                                $"Escort body crosses geometry at {current}.");
                    }
                }
                Assert.IsTrue(actor.Controller.ScriptedMove.Arrived, $"Escort failed to arrive; stopped at {actor.Position}.");
                Assert.IsLessThan(.01f, Vector3.Distance(end, actor.Position));
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, actor);
                EntityManager.Instance.UnregisterEntity(actor.EntityId);
                EntityManager.Instance.UnregisterCreature(actor.EntityId);
                EntityManager.Instance.UnregisterActor(actor.EntityId);
                EntityManager.Instance.FreeEntity(actor.EntityId);
            }
        }

        [TestMethod]
        [DataRow(165.65f, 76.1f, -514.44f)]
        [DataRow(166f, 75f, -511.8f)]
        [DataRow(166.43f, 72.2f, -505.1f)]
        public void HospitalRampHeightStaysWithinSixtyCentimetersOfCollisionFloor(float x, float y, float z)
        {
            var query = TestNavMeshes.Query(NavFile);
            var at = new Vector3(x, query.GroundHeight(new Vector3(x, y, z)).Value, z);
            AssertSupported(Collision.Value, at);
        }

        private static void AssertSupported(CoverMesh collision, Vector3 position, float tolerance = .6f) =>
            Assert.IsTrue(collision.GeometryBlocks(position + Vector3.UnitY * tolerance, position - Vector3.UnitY * tolerance),
                $"No physical support within {tolerance} m of the navmesh height at {position}.");

        private static bool InHospitalSection(Vector3 p) =>
            p.X >= 155 && p.X <= 180 && p.Z >= -530 && p.Z <= -475;

        private static bool InInvestigatedSection(Vector3 p) =>
            InHospitalSection(p) ||
            (p.X >= 55 && p.X <= 90 && p.Z >= -450 && p.Z <= -425);
    }
}
