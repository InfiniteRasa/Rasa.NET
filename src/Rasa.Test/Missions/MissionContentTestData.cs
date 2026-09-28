using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore.Storage;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures.World;

    public sealed class MissionContentFixture
    {
        internal List<MissionContentDefinitionEntry> Definitions { get; } = new();
        internal List<MissionPrerequisiteEntry> Prerequisites { get; } = new();
        internal List<MissionObjectiveDefinitionEntry> Objectives { get; } = new();
        internal List<MissionObjectiveTransitionEntry> Transitions { get; } = new();
        internal List<MissionTriggerEntry> Triggers { get; } = new();
        internal List<MissionActionEntry> Actions { get; } = new();
        internal List<MissionRewardDefinitionEntry> Rewards { get; } = new();
        internal List<MissionRewardItemEntry> RewardItems { get; } = new();
        internal List<MissionIndicatorEntry> Indicators { get; } = new();
        internal List<MissionAreaEntry> Areas { get; } = new();
        internal List<MissionSpawnGroupEntry> SpawnGroups { get; } = new();
        internal List<MissionSpawnEntry> Spawns { get; } = new();
        internal List<MissionScenarioEntry> Scenarios { get; } = new();
        internal List<MissionScenarioStepEntry> ScenarioSteps { get; } = new();
        internal List<MissionEvidenceEntry> Evidence { get; } = new();

        internal HashSet<uint> NpcPackageIds { get; } = new() { 77 };
        internal Dictionary<uint, uint> ItemTemplateClasses { get; } = new()
        {
            [28] = 3147,
            [29] = 3147
        };
        internal HashSet<uint> EntityClassIds { get; } = new() { 3147, 4001 };
        internal Dictionary<uint, uint> CreatureClasses { get; } = new()
        {
            [501] = 4001
        };
        internal HashSet<uint> MapContextIds { get; } = new() { 1220 };

        internal static MissionContentFixture CreateValid()
        {
            var fixture = new MissionContentFixture();

            fixture.Definitions.AddRange(
                new MissionContentDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    Requirement = MissionContentRequirement.Required,
                    AbandonmentPolicy = MissionAbandonmentPolicy.Allowed,
                    ClientNameTextId = 2001,
                    GiverId = 101,
                    ReceiverId = 102,
                    Level = 9,
                    GroupType = 2,
                    CategoryId = 3,
                    Shareable = false,
                    RadioCompleteable = false,
                    Comment = "Bootcamp deployment mission"
                },
                new MissionContentDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "legacy",
                    Requirement = MissionContentRequirement.Optional,
                    AbandonmentPolicy = MissionAbandonmentPolicy.Allowed,
                    ClientNameTextId = 0,
                    GiverId = 101,
                    ReceiverId = 102,
                    Level = 9,
                    GroupType = 2,
                    CategoryId = 3,
                    Shareable = false,
                    RadioCompleteable = false,
                    Comment = "Legacy backfill"
                });

            fixture.Objectives.AddRange(
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2101,
                    ClientBodyTextId = 2102,
                    ClientCounter0TextId = null,
                    ClientCounter1TextId = null,
                    ClientCounter2TextId = null,
                    Ordinal = 1,
                    InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = true,
                    Comment = "Talk to the NPC"
                },
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2201,
                    ClientBodyTextId = 2202,
                    ClientCounter0TextId = null,
                    ClientCounter1TextId = null,
                    ClientCounter2TextId = null,
                    Ordinal = 2,
                    InitialState = (byte)MissionObjectiveState.Inactive,
                    IsRequired = true,
                    Comment = "Follow-up objective"
                });

            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Conversation completes the first objective"
            });

            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.Conversation,
                Sequence = 1,
                NpcPackageId = 77,
                PlayerFlagId = 11,
                Comment = "Completion conversation"
            });

            fixture.Actions.AddRange(
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.CompleteObjective,
                    Sequence = 1,
                    TargetObjectiveId = 10,
                    ObjectiveState = (byte)MissionObjectiveState.Completed,
                    Comment = "Completes the current objective"
                },
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.RevealObjective,
                    Sequence = 2,
                    TargetObjectiveId = 11,
                    Comment = "Reveal the second objective"
                },
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ActivateObjective,
                    Sequence = 3,
                    TargetObjectiveId = 11,
                    ObjectiveState = (byte)MissionObjectiveState.Incomplete,
                    Comment = "Activate the second objective"
                },
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.GrantReward,
                    Sequence = 4,
                    RewardId = 40,
                    Comment = "Grant the authored reward"
                });

            fixture.Rewards.Add(new MissionRewardDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = 40,
                Requirement = MissionContentRequirement.Required,
                Experience = 125,
                Credits = 75,
                Prestige = 10,
                SelectionCount = 1,
                Comment = "Reward"
            });
            fixture.RewardItems.AddRange(
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = 40,
                    ItemId = 1,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28,
                    Quantity = 2
                },
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = 40,
                    ItemId = 2,
                    Kind = MissionRewardItemKind.Selectable,
                    ItemTemplateId = 29,
                    Quantity = 1
                });

            fixture.Indicators.Add(new MissionIndicatorEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 11,
                IndicatorId = 70,
                Requirement = MissionContentRequirement.Required,
                PosX = 1,
                PosY = 2,
                PosZ = 3,
                Radius = 4,
                Show3DEffect = true,
                Comment = "Indicator"
            });

            fixture.Areas.Add(new MissionAreaEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                AreaId = 30,
                Requirement = MissionContentRequirement.Required,
                MapContextId = 1220,
                Shape = MissionAreaShape.Sphere,
                PosX = 1,
                PosY = 2,
                PosZ = 3,
                Radius = 6,
                Comment = "Area"
            });

            fixture.SpawnGroups.Add(new MissionSpawnGroupEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                Requirement = MissionContentRequirement.Required,
                AreaId = 30,
                MapContextId = 1220,
                Enabled = false,
                SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                RespawnSeconds = null,
                Comment = "Spawn group"
            });

            fixture.Spawns.Add(new MissionSpawnEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                SpawnId = 1,
                CreatureId = 501,
                PosX = 8,
                PosY = 9,
                PosZ = 10,
                Rotation = 0,
                Quantity = 1
            });

            fixture.Scenarios.Add(new MissionScenarioEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                Requirement = MissionContentRequirement.Required,
                StartPolicy = MissionScenarioStartPolicy.Automatic,
                Name = "Scenario",
                Comment = "Scenario"
            });
            fixture.ScenarioSteps.Add(new MissionScenarioStepEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                StepId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionScenarioStepKind.EmitScenarioEvent,
                Sequence = 1,
                ScenarioEventId = 1,
                Comment = "Intro event"
            });

            fixture.Evidence.Add(new MissionEvidenceEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                EvidenceId = 1,
                OwnerKind = MissionEvidenceOwnerKind.Mission,
                OwnerId = 321,
                SourceKind = MissionEvidenceSourceKind.Documentation,
                SourceUri = "https://example.invalid/mission-321",
                Confidence = 0.9,
                ReconstructionNote = "Authoritative source"
            });

            return fixture;
        }

        internal IMissionContentRepository CreateRepository() =>
            new FakeMissionContentRepository(this);

        internal IWorldUnitOfWork CreateWorldUnitOfWork() =>
            new FakeWorldUnitOfWork(this);

        private sealed class FakeMissionContentRepository : IMissionContentRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeMissionContentRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public List<MissionContentDefinitionEntry> GetDefinitions() =>
                _fixture.Definitions.Select(Clone).ToList();

            public List<MissionPrerequisiteEntry> GetPrerequisites() =>
                _fixture.Prerequisites.Select(Clone).ToList();

            public List<MissionObjectiveDefinitionEntry> GetObjectives() =>
                _fixture.Objectives.Select(Clone).ToList();

            public List<MissionObjectiveTransitionEntry> GetTransitions() =>
                _fixture.Transitions.Select(Clone).ToList();

            public List<MissionTriggerEntry> GetTriggers() =>
                _fixture.Triggers.Select(Clone).ToList();

            public List<MissionActionEntry> GetActions() =>
                _fixture.Actions.Select(Clone).ToList();

            public List<MissionRewardDefinitionEntry> GetRewards() =>
                _fixture.Rewards.Select(Clone).ToList();

            public List<MissionRewardItemEntry> GetRewardItems() =>
                _fixture.RewardItems.Select(Clone).ToList();

            public List<MissionIndicatorEntry> GetIndicators() =>
                _fixture.Indicators.Select(Clone).ToList();

            public List<MissionAreaEntry> GetAreas() =>
                _fixture.Areas.Select(Clone).ToList();

            public List<MissionSpawnGroupEntry> GetSpawnGroups() =>
                _fixture.SpawnGroups.Select(Clone).ToList();

            public List<MissionSpawnEntry> GetSpawns() =>
                _fixture.Spawns.Select(Clone).ToList();

            public List<MissionScenarioEntry> GetScenarios() =>
                _fixture.Scenarios.Select(Clone).ToList();

            public List<MissionScenarioStepEntry> GetScenarioSteps() =>
                _fixture.ScenarioSteps.Select(Clone).ToList();

            public List<MissionEvidenceEntry> GetEvidence() =>
                _fixture.Evidence.Select(Clone).ToList();
        }

        private sealed class FakeWorldUnitOfWork : IWorldUnitOfWork
        {
            internal FakeWorldUnitOfWork(MissionContentFixture fixture)
            {
                Equipment = new FakeEquipmentRepository(fixture);
                Creatures = new FakeCreatureRepository(fixture);
                EntityClasses = new FakeEntityClassRepository(fixture);
                MapInfos = new FakeMapInfoRepository(fixture);
                MissionContent = fixture.CreateRepository();
                NpcPackages = new FakeNpcPackageRepository(fixture);
            }

            public IActionRepository Actions => null;
            public IEquipmentRepository Equipment { get; }
            public ICreatureRepository Creatures { get; }
            public IEntityClassRepository EntityClasses { get; }
            public IFootlockerRepository Footlockers => null;
            public ILogosRepository Logoses => null;
            public IMapInfoRepository MapInfos { get; }
            public IMapLinkRepository MapLinks => null;
            public IKraftwerksRepository Kraftwerks => null;
            public IMapRegionRepository MapRegions => null;
            public IMapMarkerRepository MapMarkers => null;
            public IRecipeRepository Recipes => null;
            public INpcMissionRepository NpcMissions => null;
            public INpcMissionRewardRepository NpcMissionRewards => null;
            public IMissionContentRepository MissionContent { get; }
            public INpcPackageRepository NpcPackages { get; }
            public IPlayerRandomNameRepository RandomNames => null;
            public ISpawnpoolRepository Spawnpools => null;
            public ITeleporterRepository Teleporters => null;
            public void Complete() { }
            public void Reject() { }
            public IDbContextTransaction BeginTransaction() => throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class FakeNpcPackageRepository : INpcPackageRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeNpcPackageRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public List<NpcPackageEntry> Get() =>
                _fixture.NpcPackageIds
                    .OrderBy(id => id)
                    .Select(id => new NpcPackageEntry
                    {
                        Id = id,
                        PackageId = id,
                        Comment = $"Npc package {id}"
                    })
                    .ToList();
        }

        private sealed class FakeMapInfoRepository : IMapInfoRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeMapInfoRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public List<MapInfoEntry> Get() =>
                _fixture.MapContextIds
                    .OrderBy(id => id)
                    .Select(id => new MapInfoEntry
                    {
                        Id = id,
                        MapName = $"Map {id}",
                        MapVersion = 1,
                        BaseRegion = 1
                    })
                    .ToList();
        }

        private sealed class FakeEntityClassRepository : IEntityClassRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeEntityClassRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public List<EntityClassEntry> Get() =>
                _fixture.EntityClassIds
                    .OrderBy(id => id)
                    .Select(id => new EntityClassEntry
                    {
                        Id = id,
                        ClassName = $"Entity class {id}",
                        MeshId = id,
                        ClassCollisionRole = 0,
                        TargetFlag = 0,
                        AugList = string.Empty
                    })
                    .ToList();
        }

        private sealed class FakeCreatureRepository : ICreatureRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeCreatureRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public List<CreatureEntry> Get() =>
                _fixture.CreatureClasses
                    .OrderBy(entry => entry.Key)
                    .Select(entry => new CreatureEntry
                    {
                        Id = entry.Key,
                        Comment = $"Creature {entry.Key}",
                        ClassId = entry.Value,
                        Faction = 1,
                        Level = 1,
                        MaxHitPoints = 1,
                        NameId = 1,
                        RunSpeed = 1,
                        WalkSpeed = 1
                    })
                    .ToList();

            public List<CreatureClassFlagEntry> GetClassFlags() => new();
            public CreatureStatEntry GetCreatureStats(uint creatureId) => null;
            public CreatureActionEntry GetCreatureActionById(uint id) => null;
            public Dictionary<uint, CreatureActionEntry> GetCreatureActions() => new();
            public void CreateOrUpdateAppearance(uint dbId, uint slotId, uint classId, uint hue) =>
                throw new NotSupportedException();
            public List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId) => new();
            public List<VendorItemEntry> GetVendorItems() => new();
            public List<VendorEntry> GetVendors() => new();
        }

        private sealed class FakeEquipmentRepository : IEquipmentRepository
        {
            private readonly MissionContentFixture _fixture;

            internal FakeEquipmentRepository(MissionContentFixture fixture)
            {
                _fixture = fixture;
            }

            public uint GetItemClass(uint itemTemplateId) =>
                _fixture.ItemTemplateClasses[itemTemplateId];

            public List<ArmorClassEntry> GetArmorClasses() => new();
            public List<WeaponClassEntry> GetWeaponClasses() => new();
            public List<ItemClassEntry> GetItemClasses() => new();
            public List<EquipableClassEntry> GetEquipableClasses() => new();

            public List<ItemTemplateItemClassEntry> GetItemTemplateClasses() =>
                _fixture.ItemTemplateClasses
                    .OrderBy(entry => entry.Key)
                    .Select(entry => new ItemTemplateItemClassEntry
                    {
                        ItemTemplateId = entry.Key,
                        ItemClass = entry.Value
                    })
                    .ToList();

            public List<ItemTemplateRequirementRaceEntry> GetRequirementsRace() => new();
            public List<ItemTemplateRequirementSkillEntry> GetRequirementsSkill() => new();
            public List<ItemTemplateRequirementEntry> GetRequirementsGeneric() => new();
            public List<ItemTemplateArmorEntry> GetArmorItems() => new();
            public List<ItemTemplateWeaponEntry> GetWeaponItems() => new();
            public List<ItemTemplateEntry> GetItemTemplates() => new();
            public List<ItemTemplateResistanceEntry> GetItemResistances() => new();
        }

        private static MissionContentDefinitionEntry Clone(MissionContentDefinitionEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                Requirement = entry.Requirement,
                AbandonmentPolicy = entry.AbandonmentPolicy,
                ClientNameTextId = entry.ClientNameTextId,
                GiverId = entry.GiverId,
                ReceiverId = entry.ReceiverId,
                Level = entry.Level,
                GroupType = entry.GroupType,
                CategoryId = entry.CategoryId,
                Shareable = entry.Shareable,
                RadioCompleteable = entry.RadioCompleteable,
                Comment = entry.Comment
            };

        private static MissionPrerequisiteEntry Clone(MissionPrerequisiteEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                PrerequisiteId = entry.PrerequisiteId,
                Requirement = entry.Requirement,
                Kind = entry.Kind,
                RequiredMissionId = entry.RequiredMissionId,
                RequiredMissionState = entry.RequiredMissionState,
                RequiredLevel = entry.RequiredLevel,
                PlayerFlagId = entry.PlayerFlagId,
                PlayerFlagValue = entry.PlayerFlagValue,
                Comment = entry.Comment
            };

        private static MissionObjectiveDefinitionEntry Clone(MissionObjectiveDefinitionEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ObjectiveId = entry.ObjectiveId,
                Requirement = entry.Requirement,
                ClientNameTextId = entry.ClientNameTextId,
                ClientBodyTextId = entry.ClientBodyTextId,
                ClientCounter0TextId = entry.ClientCounter0TextId,
                ClientCounter1TextId = entry.ClientCounter1TextId,
                ClientCounter2TextId = entry.ClientCounter2TextId,
                Ordinal = entry.Ordinal,
                InitialState = entry.InitialState,
                IsRequired = entry.IsRequired,
                Comment = entry.Comment
            };

        private static MissionObjectiveTransitionEntry Clone(MissionObjectiveTransitionEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ObjectiveId = entry.ObjectiveId,
                TransitionId = entry.TransitionId,
                Requirement = entry.Requirement,
                Sequence = entry.Sequence,
                FromState = entry.FromState,
                ToState = entry.ToState,
                Comment = entry.Comment
            };

        private static MissionTriggerEntry Clone(MissionTriggerEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ObjectiveId = entry.ObjectiveId,
                TransitionId = entry.TransitionId,
                TriggerId = entry.TriggerId,
                Requirement = entry.Requirement,
                Kind = entry.Kind,
                Sequence = entry.Sequence,
                RelatedObjectiveId = entry.RelatedObjectiveId,
                RelatedState = entry.RelatedState,
                EventKind = entry.EventKind,
                SubjectId = entry.SubjectId,
                CounterId = entry.CounterId,
                InitialValue = entry.InitialValue,
                TargetValue = entry.TargetValue,
                AreaId = entry.AreaId,
                DurationSeconds = entry.DurationSeconds,
                NpcPackageId = entry.NpcPackageId,
                PlayerFlagId = entry.PlayerFlagId,
                SourceSpawnResolved = entry.SourceSpawnResolved,
                Comment = entry.Comment
            };

        private static MissionActionEntry Clone(MissionActionEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ObjectiveId = entry.ObjectiveId,
                TransitionId = entry.TransitionId,
                ActionId = entry.ActionId,
                Requirement = entry.Requirement,
                Kind = entry.Kind,
                Sequence = entry.Sequence,
                TargetObjectiveId = entry.TargetObjectiveId,
                ObjectiveState = entry.ObjectiveState,
                RewardId = entry.RewardId,
                SpawnGroupId = entry.SpawnGroupId,
                ScenarioId = entry.ScenarioId,
                IndicatorId = entry.IndicatorId,
                PlayerFlagId = entry.PlayerFlagId,
                PlayerFlagValue = entry.PlayerFlagValue,
                Comment = entry.Comment
            };

        private static MissionRewardDefinitionEntry Clone(MissionRewardDefinitionEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                RewardId = entry.RewardId,
                Requirement = entry.Requirement,
                Experience = entry.Experience,
                Credits = entry.Credits,
                Prestige = entry.Prestige,
                SelectionCount = entry.SelectionCount,
                Comment = entry.Comment
            };

        private static MissionRewardItemEntry Clone(MissionRewardItemEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                RewardId = entry.RewardId,
                ItemId = entry.ItemId,
                Kind = entry.Kind,
                ItemTemplateId = entry.ItemTemplateId,
                Quantity = entry.Quantity
            };

        private static MissionIndicatorEntry Clone(MissionIndicatorEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ObjectiveId = entry.ObjectiveId,
                IndicatorId = entry.IndicatorId,
                Requirement = entry.Requirement,
                PosX = entry.PosX,
                PosY = entry.PosY,
                PosZ = entry.PosZ,
                Radius = entry.Radius,
                Show3DEffect = entry.Show3DEffect,
                Comment = entry.Comment
            };

        private static MissionAreaEntry Clone(MissionAreaEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                AreaId = entry.AreaId,
                Requirement = entry.Requirement,
                MapContextId = entry.MapContextId,
                Shape = entry.Shape,
                PosX = entry.PosX,
                PosY = entry.PosY,
                PosZ = entry.PosZ,
                Radius = entry.Radius,
                ExtentX = entry.ExtentX,
                ExtentY = entry.ExtentY,
                ExtentZ = entry.ExtentZ,
                Comment = entry.Comment
            };

        private static MissionSpawnGroupEntry Clone(MissionSpawnGroupEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                SpawnGroupId = entry.SpawnGroupId,
                Requirement = entry.Requirement,
                AreaId = entry.AreaId,
                MapContextId = entry.MapContextId,
                Enabled = entry.Enabled,
                SpawnPolicy = entry.SpawnPolicy,
                RespawnSeconds = entry.RespawnSeconds,
                Comment = entry.Comment
            };

        private static MissionSpawnEntry Clone(MissionSpawnEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                SpawnGroupId = entry.SpawnGroupId,
                SpawnId = entry.SpawnId,
                CreatureId = entry.CreatureId,
                PosX = entry.PosX,
                PosY = entry.PosY,
                PosZ = entry.PosZ,
                Rotation = entry.Rotation,
                Quantity = entry.Quantity
            };

        private static MissionScenarioEntry Clone(MissionScenarioEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ScenarioId = entry.ScenarioId,
                Requirement = entry.Requirement,
                StartPolicy = entry.StartPolicy,
                Name = entry.Name,
                Comment = entry.Comment
            };

        private static MissionScenarioStepEntry Clone(MissionScenarioStepEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                ScenarioId = entry.ScenarioId,
                StepId = entry.StepId,
                Requirement = entry.Requirement,
                Kind = entry.Kind,
                Sequence = entry.Sequence,
                TargetObjectiveId = entry.TargetObjectiveId,
                RewardId = entry.RewardId,
                SpawnGroupId = entry.SpawnGroupId,
                SpawnId = entry.SpawnId,
                DynamicObjectKey = entry.DynamicObjectKey,
                EntityClassId = entry.EntityClassId,
                TargetScenarioId = entry.TargetScenarioId,
                DelayMilliseconds = entry.DelayMilliseconds,
                SkillId = entry.SkillId,
                AbilityId = entry.AbilityId,
                SkillLevel = entry.SkillLevel,
                AbilitySlot = entry.AbilitySlot,
                TutorialId = entry.TutorialId,
                AudioSetId = entry.AudioSetId,
                AttemptKey = entry.AttemptKey,
                ScenarioEventId = entry.ScenarioEventId,
                MapContextId = entry.MapContextId,
                PosX = entry.PosX,
                PosY = entry.PosY,
                PosZ = entry.PosZ,
                Orientation = entry.Orientation,
                InitialInteractionEnabled = entry.InitialInteractionEnabled,
                QualificationKey = entry.QualificationKey,
                QualificationValue = entry.QualificationValue,
                AccountSkipEntitlement = entry.AccountSkipEntitlement,
                Comment = entry.Comment
            };

        private static MissionEvidenceEntry Clone(MissionEvidenceEntry entry) =>
            new()
            {
                MissionId = entry.MissionId,
                ContentRevision = entry.ContentRevision,
                EvidenceId = entry.EvidenceId,
                OwnerKind = entry.OwnerKind,
                OwnerId = entry.OwnerId,
                SourceKind = entry.SourceKind,
                SourceUri = entry.SourceUri,
                LocalClientPath = entry.LocalClientPath,
                Confidence = entry.Confidence,
                ReconstructionNote = entry.ReconstructionNote
            };
    }
}
