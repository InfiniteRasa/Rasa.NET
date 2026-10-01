using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessRanjaGorgeV1
    {
        private static MissionSceneDefinition QualifiedScene() => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed")
        };

        public static SceneRoute CandidateMatthewRoute() => new("ranja-caverns",
            new SceneWaypoint[]
            {
                new(new ScenePosition(-326.8672f, 173.5628f, -541.16797f)),
                new(new ScenePosition(-350.743f, 173.71783f, -492f)),
                new(new ScenePosition(-364.543f, 173.71783f, -480f)),
                new(new ScenePosition(-367.743f, 173.11783f, -478f)),
                new(new ScenePosition(-418.94302f, 182.51785f, -492.6f)),
                new(new ScenePosition(-426.143f, 187.11783f, -492.6f)),
                new(new ScenePosition(-480.54303f, 202.31784f, -482.2f)),
                new(new ScenePosition(-484.14304f, 203.91785f, -480.6f)),
                new(new ScenePosition(-488.54303f, 205.91785f, -476.4f)),
                new(new ScenePosition(-494.54303f, 211.11783f, -422.19998f)),
                new(new ScenePosition(-494.74304f, 212.31784f, -412.19998f)),
                new(new ScenePosition(-493.14304f, 213.11783f, -410.19998f)),
                new(new ScenePosition(-488.54303f, 213.91785f, -407.99997f)),
                new(new ScenePosition(-485.54303f, 213.71783f, -406.99997f)),
                new(new ScenePosition(-464.54303f, 216.51785f, -401.59998f)),
                new(new ScenePosition(-461.74304f, 215.71783f, -400.39996f)),
                new(new ScenePosition(-461.34302f, 215.31784f, -398.99997f)),
                new(new ScenePosition(-460.34302f, 214.31784f, -391.39996f)),
                new(new ScenePosition(-460.74304f, 212.91785f, -389.19998f)),
                new(new ScenePosition(-461.74304f, 209.71783f, -385.59998f)),
                new(new ScenePosition(-467.54303f, 206.31784f, -379.39996f)),
                new(new ScenePosition(-469.54303f, 206.51785f, -379.99997f)),
                new(new ScenePosition(-470.54303f, 205.91785f, -381.99997f)),
                new(new ScenePosition(-479.94302f, 202.71783f, -405.59998f)),
                new(new ScenePosition(-481.74304f, 202.71783f, -407.99997f)),
                new(new ScenePosition(-488.14304f, 201.51785f, -409.8f)),
                new(new ScenePosition(-511.14304f, 196.91785f, -413f)),
                new(new ScenePosition(-521.343f, 192.71783f, -411.8f)),
                new(new ScenePosition(-534.143f, 194.71783f, -416f)),
                new(new ScenePosition(-535.943f, 194.71783f, -418f)),
                new(new ScenePosition(-540.54297f, 193.11783f, -435f)),
                new(new ScenePosition(-545.943f, 194.71783f, -440f)),
                new(new ScenePosition(-549.743f, 193.71783f, -441.8f)),
                new(new ScenePosition(-551.343f, 193.51785f, -442.19998f)),
                new(new ScenePosition(-553.943f, 191.91785f, -442.19998f)),
                new(new ScenePosition(-559.143f, 192.71783f, -441.8f)),
                new(new ScenePosition(-583.94305f, 192.51785f, -434.19998f)),
                new(new ScenePosition(-614.14307f, 194.71783f, -448f)),
                new(new ScenePosition(-615.94305f, 194.71783f, -450f)),
                new(new ScenePosition(-625.3431f, 186.51785f, -484.2f)),
                new(new ScenePosition(-627.5431f, 186.71783f, -488.2f)),
                new(new ScenePosition(-634.14307f, 186.71783f, -490.6f)),
                new(new ScenePosition(-638.143f, 186.71783f, -490.8f)),
                new(new ScenePosition(-640.543f, 186.51785f, -490.2f)),
                new(new ScenePosition(-652.143f, 186.71783f, -486.6f)),
                new(new ScenePosition(-655.543f, 186.51785f, -484.6f)),
                new(new ScenePosition(-663.143f, 186.31784f, -478.2f)),
                new(new ScenePosition(-663.543f, 186.11783f, -476.8f)),
                new(new ScenePosition(-673.543f, 182.51785f, -435.59998f)),
                new(new ScenePosition(-679.543f, 178.71783f, -423.8f)),
                new(new ScenePosition(-696.94305f, 172.91785f, -396.59998f)),
                new(new ScenePosition(-701.74304f, 171.11783f, -385.8f)),
                new(new ScenePosition(-705.143f, 169.91785f, -366.8f)),
                new(new ScenePosition(-703.343f, 170.51785f, -363.99997f)),
                new(new ScenePosition(-688.143f, 170.51785f, -354.99997f)),
                new(new ScenePosition(-685.343f, 170.51785f, -352.99997f)),
                new(new ScenePosition(-683.343f, 170.11783f, -350.99997f)),
                new(new ScenePosition(-682.343f, 170.11783f, -344.39996f)),
                new(new ScenePosition(-683.74304f, 170.11783f, -342.79996f)),
                new(new ScenePosition(-691.9922f, 170.31784f, -339.1875f))
            }, Speed: 5);

        public static MissionSceneDefinition WalkingWounded(SceneRoute route)
        {
            ValidateWoundedRoute(route);
            var scene = QualifiedScene();
            scene.Script = "wilderness.walking-wounded";
            scene.Actors["matthew"] = new SceneActorDefinition("matthew",
                SceneActorKind.PublicSpawn, MatthewSpawnId);
            scene.Routes["ranja-caverns"] = route with { Key = "ranja-caverns", ResumeAtDestination = false };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[1] = new SceneSequenceDefinition();
            scene.PublicEncounter = new PublicEncounterBinding(697, MatthewSpawnId,
                "matthew", scene.Script, OwnerLossPolicy: "Fail");
            return scene;
        }

        public static IReadOnlyList<SceneActorDefinition> ProvisionalEggClusters() =>
            Array.AsReadOnly(new[]
            {
                new SceneActorDefinition("egg-cluster-1", SceneActorKind.Object, EggClusterClassId,
                    new ScenePosition(-560f, 192.66052f, -444f)),
                new SceneActorDefinition("egg-cluster-2", SceneActorKind.Object, EggClusterClassId,
                    new ScenePosition(-577.4763f, 191.11783f, -450.53333f)),
                new SceneActorDefinition("egg-cluster-3", SceneActorKind.Object, EggClusterClassId,
                    new ScenePosition(-583.47644f, 191.05116f, -437.86667f)),
                new SceneActorDefinition("egg-cluster-4", SceneActorKind.Object, EggClusterClassId,
                    new ScenePosition(-608f, 192.28136f, -444f))
            });

        public static MissionSceneDefinition EggClusters(IReadOnlyList<SceneActorDefinition> clusters)
        {
            ValidateEggClusters(clusters);
            var scene = QualifiedScene();
            scene.Script = "wilderness.ranja-egg-clusters";
            var start = new SceneSequenceDefinition();
            scene.Sequences[0] = start;
            for (var index = 0; index < clusters.Count; index++)
            {
                var sequence = (uint)index + 1;
                var role = $"egg-cluster-{sequence}";
                scene.Actors[role] = clusters[index] with
                {
                    Role = role,
                    MissionId = 860,
                    UseAction = null,
                    Destruction = new SceneObjectDestruction(860, 1, sequence)
                };
                start.World.Add(new EnsureActorIntent($"introduce-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = new() { new RemoveActorIntent($"remove-destroyed-{role}", role) },
                    Signals = new() { new SceneMissionSignal(860, 1, sequence) }
                };
            }
            return scene;
        }

        private static void ValidateWoundedRoute(SceneRoute route)
        {
            if (route?.Points == null || route.Points.Count < 2 ||
                route.Points.Any(point => point?.Position == null || !Finite(point.Position)) ||
                route.Points.Select(point => point.Position).Distinct().Count() < 2 ||
                !float.IsFinite(route.Speed) || route.Speed <= 0)
                throw new ArgumentException(
                    "The Walking Wounded requires a coordinator-supplied connected, grounded route through Ranja Caverns. No fallback route is available.",
                    nameof(route));
        }

        private static void ValidateEggClusters(IReadOnlyList<SceneActorDefinition> clusters)
        {
            if (clusters == null || clusters.Count != 4 ||
                clusters.Any(cluster => cluster == null || cluster.Kind != SceneActorKind.Object ||
                    cluster.TemplateId != EggClusterClassId || cluster.Position == null ||
                    !Finite(cluster.Position)) ||
                clusters.Select(cluster => cluster.Position).Distinct().Count() != 4)
                throw new ArgumentException(
                    "Sacs And Violence requires four distinct grounded native Egg Cluster object bindings from the coordinator.",
                    nameof(clusters));
        }

        private static bool Finite(ScenePosition position) =>
            float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
    }
}
