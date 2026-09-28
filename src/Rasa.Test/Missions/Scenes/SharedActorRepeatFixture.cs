using System;
using System.Collections.Generic;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Encounters;

namespace Rasa.Test.Missions.Scenes
{
    internal sealed class SharedActorRepeatFixture : IDisposable
    {
        private Creature _previousGuideTemplate;
        private bool _guideTemplateChanged;
        internal MissionTestContext Context { get; }
        internal DateTime Now { get; set; } = DateTime.UnixEpoch;
        internal Creature Guide { get; private set; }
        internal Creature Independent { get; }
        internal Creature Giver { get; }
        internal string RootId { get; }
        internal SceneBindings RootBindings { get; }
        internal SceneBindings MissionBindings { get; }

        internal SharedActorRepeatFixture(MissionRepeatKind repeatKind = MissionRepeatKind.Immediate, bool resumeAtDestination = false)
        {
            var mission = new Mission(321, "Shared escort repeat", 321, 77, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, repeatPolicy: new(repeatKind));
            Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = mission },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) },
                utcNow: () => Now);
            Context.Map.IsPrivateInstance = true;
            Context.Map.OwnerCharacterId = 1;
            var start = SceneNavigationFixture.Attach(Context.Map);
            var destination = Context.Map.NavMesh.Nearest(start + new System.Numerics.Vector3(4, 0, 0)).Value;
            Guide = Actor(501);
            Independent = Actor(502);
            Giver = Context.AddNpc(77);
            var guide = new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 501, SharedKey: "repeat-guide",
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });
            var routes = new Dictionary<string, SceneRoute>
            {
                ["outbound"] = new("outbound",
                    new[] { new SceneWaypoint(new ScenePosition(destination.X, destination.Y, destination.Z)) },
                    Speed: 3, ResumeAtDestination: resumeAtDestination)
            };
            RootBindings = new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["guide"] = guide,
                    ["independent"] = new("independent", SceneActorKind.PublicSpawn, 502)
                }, routes, new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: new WorldIntent[]
                        {
                            new EnsureActorIntent("root-guide", "guide"),
                            new EnsureActorIntent("root-independent", "independent"),
                            new RunRouteIntent("root-route", "independent", "outbound")
                        }, timers: new[] { new SequenceTimer("root-wait", 600000, 9) }),
                    [3] = new(worldIntents: new WorldIntent[] { new FollowActorIntent("root-follow", "guide", 1) }),
                    [4] = new(worldIntents: new WorldIntent[] { new AttackActorIntent("root-attack", "guide", "independent") }),
                    [5] = new(worldIntents: new WorldIntent[] { new RunRouteIntent("root-guide-route", "guide", "outbound") }),
                    [6] = new(worldIntents: new WorldIntent[] { new SetInteractionIntent("root-independent-disable", "independent", false) }),
                    [9] = new()
                });
            MissionBindings = new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["escort"] = guide with { Role = "escort" },
                    ["independent"] = RootBindings.Actors["independent"]
                },
                routes, new Dictionary<uint, SceneSequence>
                {
                    [0] = new(),
                    [1] = new(worldIntents: new WorldIntent[] { new RunRouteIntent("escort-route", "escort", "outbound") }),
                    [2] = new(worldIntents: new WorldIntent[] { new SetInteractionIntent("escort-disable", "escort", false) }),
                    [3] = new(worldIntents: new WorldIntent[] { new FollowActorIntent("escort-follow", "escort", 1) }),
                    [4] = new(worldIntents: new WorldIntent[] { new AttackActorIntent("escort-attack", "escort", "independent") })
                }, new Dictionary<string, uint> { ["route"] = 1, ["disable"] = 2, ["follow"] = 3, ["attack"] = 4 });
            Context.Manager.Scenes.Bind(321, "data.sequence", MissionBindings);
            RootId = Context.Manager.Scenes.Start(Context.Client, "data.sequence", RootBindings);

            Creature Actor(uint spawnId)
            {
                var actor = Context.AddNpc(spawnId, position: start);
                actor.RunSpeed = 6.5f;
                actor.WalkSpeed = 3;
                actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                actor.SpawnPool = new SpawnPool
                {
                    DbId = spawnId, MapContextId = Context.Map.MapInfo.MapContextId,
                    RuntimeMapChannel = Context.Map, Position = start
                };
                Context.Map.SpawnPools.Add(actor.SpawnPool);
                return actor;
            }
        }

        internal void DeferGuideSpawn()
        {
            CreatureManager.Instance.LoadedCreatures.TryGetValue(501, out _previousGuideTemplate);
            CreatureManager.Instance.LoadedCreatures[501] = Guide;
            _guideTemplateChanged = true;
            Guide.SpawnPool.SpawnSlot = new List<SpawnPoolSlot> { new(501, 1, 1) };
            Guide.SpawnPool.AliveCreatures = 0;
            Context.RemoveNpcFromWorld(Guide);
        }

        internal void RestoreGuideSpawn()
        {
            var pool = Guide.SpawnPool;
            Guide = Context.AddNpc(501, position: pool.Position);
            Guide.RunSpeed = 6.5f;
            Guide.WalkSpeed = 3;
            Guide.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            Guide.SpawnPool = pool;
            pool.AliveCreatures = 1;
        }

        public void Dispose()
        {
            Context.Manager.Scenes.LeaseReset(RootId);
            using (var unit = Context.CreateChar())
                foreach (var scene in unit.CharacterMissions.Runtime.Scenes(1, 321))
                    Context.Manager.Scenes.LeaseReset(scene.RunId);
            if (_guideTemplateChanged)
            {
                if (_previousGuideTemplate == null)
                    CreatureManager.Instance.LoadedCreatures.Remove(501);
                else
                    CreatureManager.Instance.LoadedCreatures[501] = _previousGuideTemplate;
            }
            Context.Dispose();
        }
    }
}
