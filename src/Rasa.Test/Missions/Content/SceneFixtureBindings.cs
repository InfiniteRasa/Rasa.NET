using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Structures.World;

    internal static class SceneFixtureBindings
    {
        internal static void Bind(MissionApplication manager, MissionContentFixture fixture)
        {
            manager.Scenes.ClearBindings();
            foreach (var definition in fixture.Definitions.Where(definition => definition.ContentRevision != "legacy"))
            {
                var actors = new Dictionary<string, SceneActorDefinition>();
                foreach (var spawn in fixture.Spawns.Where(spawn => spawn.MissionId == definition.MissionId))
                    for (var index = 0; index < spawn.Quantity; index++)
                    {
                        var role = $"group-{spawn.SpawnGroupId}-{spawn.SpawnId}-{index}";
                        actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, spawn.CreatureId,
                            new ScenePosition((float)spawn.PosX, (float)spawn.PosY, (float)spawn.PosZ), spawn.Rotation,
                            MissionId: definition.MissionId, GroupId: spawn.SpawnGroupId, SpawnId: spawn.SpawnId);
                    }
                var steps = fixture.ScenarioSteps.Where(step => step.MissionId == definition.MissionId).ToArray();
                foreach (var step in steps.Where(step => step.Kind == MissionScenarioStepKind.SpawnDynamicObject))
                    actors[step.DynamicObjectKey] = new SceneActorDefinition(step.DynamicObjectKey, SceneActorKind.Object,
                        step.EntityClassId.Value, new ScenePosition((float)step.PosX.Value, (float)step.PosY.Value, (float)step.PosZ.Value),
                        step.Orientation.Value, step.InitialInteractionEnabled ?? true, (uint)UseObjectState.IdStateActive);
                var sequences = new Dictionary<uint, SceneSequence>();
                foreach (var scenario in fixture.Scenarios.Where(scenario => scenario.MissionId == definition.MissionId))
                {
                    var world = new List<WorldIntent>();
                    var character = new List<CharacterIntent>();
                    var signals = new List<SceneMissionSignal>();
                    var timers = new List<SequenceTimer>();
                    foreach (var step in steps.Where(step => step.ScenarioId == scenario.ScenarioId).OrderBy(step => step.Sequence))
                    {
                        var key = $"scenario-{scenario.ScenarioId}-step-{step.StepId}";
                        var group = actors.Values.Where(actor => actor.GroupId == step.SpawnGroupId).ToArray();
                        switch (step.Kind)
                        {
                            case MissionScenarioStepKind.GrantRewardPackage:
                                character.Add(new GrantRewardIntent(key, definition.MissionId, step.RewardId.Value)); break;
                            case MissionScenarioStepKind.GrantSkillAbility:
                                character.Add(new GrantAbilityIntent(key, step.SkillId.Value, step.AbilityId.Value, step.SkillLevel.Value, step.AbilitySlot)); break;
                            case MissionScenarioStepKind.SetQualification:
                                character.Add(new SetQualificationIntent(key, (byte)step.QualificationKey.Value, step.QualificationValue == 1)); break;
                            case MissionScenarioStepKind.SetAccountSkipEntitlement:
                                character.Add(new SetEntitlementIntent(key, step.AccountSkipEntitlement.Value)); break;
                            case MissionScenarioStepKind.SpawnGroup:
                                world.AddRange(group.Select(actor => new EnsureActorIntent(key + actor.Role, actor.Role))); break;
                            case MissionScenarioStepKind.DespawnGroup:
                                world.AddRange(group.Select(actor => new RemoveActorIntent(key + actor.Role, actor.Role))); break;
                            case MissionScenarioStepKind.EscortSpawnGroup:
                                world.AddRange(group.Select(actor => new FollowActorIntent(key + actor.Role, actor.Role, 0))); break;
                            case MissionScenarioStepKind.SpawnDynamicObject:
                                world.Add(new EnsureActorIntent(key, step.DynamicObjectKey)); break;
                            case MissionScenarioStepKind.DespawnDynamicObject:
                                world.Add(new RemoveActorIntent(key, step.DynamicObjectKey)); break;
                            case MissionScenarioStepKind.EnableInteraction:
                            case MissionScenarioStepKind.DisableInteraction:
                                foreach (var actor in actors.Values.Where(actor => step.EntityClassId.HasValue
                                    ? actor.TemplateId == step.EntityClassId : actor.GroupId == step.SpawnGroupId && actor.SpawnId == step.SpawnId))
                                    world.Add(new SetInteractionIntent(key + actor.Role, actor.Role,
                                        step.Kind == MissionScenarioStepKind.EnableInteraction, IfPresent: true));
                                break;
                            case MissionScenarioStepKind.ScheduleScenario:
                                timers.Add(new SequenceTimer(key, step.DelayMilliseconds.Value, step.TargetScenarioId.Value)); break;
                            case MissionScenarioStepKind.EmitScenarioEvent:
                                signals.Add(new SceneMissionSignal(definition.MissionId, scenario.ScenarioId, step.ScenarioEventId.Value)); break;
                            case MissionScenarioStepKind.RevealObjective:
                            case MissionScenarioStepKind.ActivateObjective:
                            case MissionScenarioStepKind.CompleteObjective:
                            case MissionScenarioStepKind.FailObjective:
                                character.Add(new ObjectiveIntent(key, definition.MissionId, step.TargetObjectiveId.Value, step.Kind switch
                                {
                                    MissionScenarioStepKind.RevealObjective => MissionObjectiveState.NotAssigned,
                                    MissionScenarioStepKind.ActivateObjective => MissionObjectiveState.Incomplete,
                                    MissionScenarioStepKind.CompleteObjective => MissionObjectiveState.Completed,
                                    _ => MissionObjectiveState.Failed
                                })); break;
                            case MissionScenarioStepKind.StartDeadline:
                            case MissionScenarioStepKind.CancelDeadline:
                            case MissionScenarioStepKind.SatisfyDeadline:
                                character.Add(new MissionDeadlineIntent(key, definition.MissionId, step.Kind switch
                                {
                                    MissionScenarioStepKind.StartDeadline => DeadlineIntentKind.Start,
                                    MissionScenarioStepKind.CancelDeadline => DeadlineIntentKind.Cancel,
                                    _ => DeadlineIntentKind.Satisfy
                                }, step.DelayMilliseconds.GetValueOrDefault())); break;
                            case MissionScenarioStepKind.TransferPlayer:
                                world.Add(new TransferIntent(key, step.MapContextId.Value,
                                    new ScenePosition((float)step.PosX.Value, (float)step.PosY.Value, (float)step.PosZ.Value), step.Orientation.Value)); break;
                            case MissionScenarioStepKind.PlayTutorial:
                                world.Add(new PresentationIntent(key, PresentationKind.Tutorial, step.TutorialId.Value));
                                world.Add(new PresentationIntent(key + "-audio", PresentationKind.Audio, step.AudioSetId.GetValueOrDefault())); break;
                            case MissionScenarioStepKind.ResetAttempt:
                                throw new InvalidOperationException("Encoded attempt-key reset fixtures must use versioned scene lifecycle tests.");
                            default:
                                throw new InvalidOperationException($"Unsupported fixture step {step.Kind}.");
                        }
                    }
                    sequences.Add(scenario.ScenarioId, new SceneSequence(world, character, signals, timers));
                }
                manager.Scenes.Bind(definition.MissionId, "data.sequence",
                    new SceneBindings(definition.ContentRevision, actors, new Dictionary<string, SceneRoute>(), sequences));
            }
        }
    }
}
