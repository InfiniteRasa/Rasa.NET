using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.Char;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public sealed record WildernessDaghdasUrnBindings(
        uint SkeevSpawnId,
        uint OutcomeFlagId,
        uint HerbObjectClassId,
        uint HerbActionArgId,
        uint HerbReadyState,
        IReadOnlyList<ScenePosition> HerbPositions,
        IReadOnlyList<ScenePosition> XanxPositions,
        IReadOnlyList<double> XanxHeadings = null)
    {
        public void Validate()
        {
            if (SkeevSpawnId == 0 || !CharacterFlagIds.IsMissionFlag(OutcomeFlagId) ||
                HerbObjectClassId == 0 || HerbActionArgId == 0 || HerbActionArgId > int.MaxValue ||
                HerbReadyState > int.MaxValue)
                throw new ArgumentException("W7 requires centrally allocated Skeev/outcome identities and an approved native herb use binding.");
            ValidatePositions(HerbPositions, 5);
            ValidatePositions(XanxPositions, 3);
            ValidateHeadings(XanxHeadings, 3);
        }

        internal static void ValidatePositions(IReadOnlyList<ScenePosition> positions, int count)
        {
            if (positions == null || positions.Count != count || positions.Distinct().Count() != count ||
                positions.Any(position => position == null || !float.IsFinite(position.X) ||
                    !float.IsFinite(position.Y) || !float.IsFinite(position.Z)))
                throw new ArgumentException($"W7 requires {count} distinct finite, approved World positions.");
        }

        internal static void ValidateHeadings(IReadOnlyList<double> headings, int count)
        {
            if (headings != null && (headings.Count != count || headings.Any(heading => !double.IsFinite(heading))))
                throw new ArgumentException($"W7 requires {count} finite headings when headings are supplied.");
        }
    }

    public static partial class WildernessDaghdasUrnV1
    {
        public const uint SkeevCreatureId = 530130;
        public const uint SkeevSpawnId = 530130;
        public const uint OutcomeFlagId = 530003;
        public const uint TinctuObjectClassId = 10512;
        public const uint TinctuUseArgument = 3;
        public const uint TinctuActiveState = 81;
        public const uint TinctuInactiveState = 82;

        public static WildernessDaghdasUrnBindings AdoptedBindings() => new(
            SkeevSpawnId, OutcomeFlagId, TinctuObjectClassId, TinctuUseArgument, TinctuActiveState,
            new ScenePosition[]
            {
                new(600, 285.1309f, 728),
                new(658, 283.299817f, 689),
                new(637, 283.421889f, 726),
                new(590, 282.9336f, 690),
                new(608, 282.811528f, 657)
            },
            new ScenePosition[]
            {
                new(-461, 193.39366f, 513),
                new(-488, 192.172938f, 510),
                new(-475, 193.69884f, 496)
            },
            new double[] { 2.035918279, -2.213683176, -3.131953953 });

        private static MissionRequirement FirstBatchCarried(bool originalOffer = false) =>
            new AllRequirements(new MissionRequirement[]
            {
                new MissionStateRequirement(700, MissionState.Active, Accepted: true),
                new AssignmentItemRequirement(700, VaccineItemKey, SourceOfferMissionId: originalOffer ? 701U : null)
            });
        public static MissionSceneDefinition RangerEncounter(IReadOnlyList<ScenePosition> xanxPositions,
            IReadOnlyList<double> xanxHeadings = null)
        {
            WildernessDaghdasUrnBindings.ValidatePositions(xanxPositions, 3);
            WildernessDaghdasUrnBindings.ValidateHeadings(xanxHeadings, 3);
            var scene = new MissionSceneDefinition
            {
                Script = "wilderness.daghdas-rangers",
                Actors = new()
                {
                    ["anjuhi"] = new SceneActorDefinition("anjuhi", SceneActorKind.PublicSpawn, 176)
                },
                // Reconnect resumes this attack and its distinct-death checkpoint.
                PublicEncounter = new PublicEncounterBinding(682, 176, "anjuhi",
                    "wilderness.daghdas-rangers", OwnerLossPolicy: "Wait"),
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new EnsureActorIntent("reserve-anjuhi", "anjuhi"),
                            new SetInteractionIntent("anjuhi-waiting-interaction", "anjuhi", true)
                        }
                    },
                    [1] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new EnsureActorIntent("defend-anjuhi", "anjuhi"),
                            new SetInteractionIntent("anjuhi-under-attack", "anjuhi", false)
                        }
                    }
                },
                DefeatSequences = new()
            };
            for (var index = 0; index < xanxPositions.Count; index++)
            {
                var role = $"xanx-{index + 1}";
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, 87, xanxPositions[index],
                    Orientation: xanxHeadings?[index] ?? 0,
                    GameplayPolicy: new ActorGameplayPolicy { RewardScenarioKills = true, TrackParticipation = true });
                scene.Sequences[1].World.Add(new EnsureActorIntent($"introduce-{role}", role));
                scene.Sequences[1].World.Add(new AttackActorIntent($"attack-anjuhi-{role}", role, "anjuhi"));
                scene.DefeatSequences[role] = (uint)index + 11;
                scene.Sequences[(uint)index + 11] = new SceneSequenceDefinition();
            }
            return scene;
        }

        public static MissionSceneDefinition Herbs(uint herbObjectClassId, uint herbActionArgId,
            uint herbReadyState, IReadOnlyList<ScenePosition> herbPositions)
        {
            if (herbObjectClassId == 0 || herbActionArgId == 0 || herbActionArgId > int.MaxValue ||
                herbReadyState > int.MaxValue)
                throw new ArgumentException("The herb scene requires an approved native usable class, argument and state.");
            WildernessDaghdasUrnBindings.ValidatePositions(herbPositions, 5);
            var scene = new MissionSceneDefinition
            {
                Script = "wilderness.daghdas-herbs",
                HiddenObjectiveIds = new() { 6, 7, 8 },
                ObjectiveAggregations = new()
                {
                    [5] = new MissionObjectiveAggregation(new uint[] { 6, 7, 8 }, 3, 0)
                },
                Items = new() { IssuedItem("tinctu-herbs", HerbTemplate, 5, 5) },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [1] = new SceneSequenceDefinition()
                }
            };
            for (var index = 0; index < herbPositions.Count; index++)
            {
                var number = (uint)index + 1;
                var role = $"tinctu-{number}";
                var sequenceId = number + 10;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object,
                    herbObjectClassId, herbPositions[index],
                    InitialObjectState: herbReadyState,
                    UseAction: new SceneObjectAction(695, 1, sequenceId, herbActionArgId));
                scene.Sequences[1].World.Add(new EnsureActorIntent($"introduce-{role}", role));
                scene.Sequences[sequenceId] = new SceneSequenceDefinition
                {
                    World = new()
                    {
                        new TransitionObjectStateIntent($"harvest-{role}", role, TinctuInactiveState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Character = new()
                    {
                        new IssueMissionItemIntent($"herb-from-{role}", 695, "tinctu-herbs", HerbTemplate, 1)
                    }
                };
            }
            return scene;
        }

        public static MissionSceneDefinition Vaccines(uint missionId)
        {
            if (missionId is not (700 or 820))
                throw new ArgumentOutOfRangeException(nameof(missionId));
            var requirement = new List<MissionRequirement> { NoMedicinePayout() };
            if (missionId == 700)
            {
                requirement.Add(new NotRequirement(new MissionStateRequirement(700, MissionState.Failed)));
                requirement.Add(new NotRequirement(new MissionStateRequirement(820, Accepted: true)));
            }
            else
            {
                requirement.Add(new MissionStateRequirement(700, MissionState.Failed));
                requirement.Add(new NotRequirement(new MissionStateRequirement(700, MissionState.Active)));
                requirement.Add(new NotRequirement(new MissionStateRequirement(700, MissionState.Success)));
            }
            return new MissionSceneDefinition
            {
                Script = missionId == 700 ? "wilderness.corman-first-batch" : "wilderness.corman-replacement",
                HiddenObjectiveIds = new() { 6 },
                Requirement = new AllRequirements(requirement),
                TurnInRequirement = NoMedicinePayout(),
                Items = new() { IssuedItem(VaccineItemKey, VaccineTemplate, 3) },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-vaccine-batch", missionId, VaccineItemKey, VaccineTemplate, 3)
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition
                    {
                        Character = missionId == 700
                            ? new List<CharacterIntent> { new OfferRadioMissionIntent("offer-high-command", 701, IfEligible: true) }
                            : new List<CharacterIntent>()
                    },
                    [1] = new SceneSequenceDefinition(),
                    [2] = new SceneSequenceDefinition(),
                    [3] = new SceneSequenceDefinition(),
                    [5] = new SceneSequenceDefinition()
                }
            };
        }

        public static MissionSceneDefinition Finale(uint skeevSpawnId, uint outcomeFlagId)
        {
            if (skeevSpawnId == 0 || !CharacterFlagIds.IsMissionFlag(outcomeFlagId))
                throw new ArgumentException("The finale requires centrally allocated Skeev and outcome-flag identities.");
            var flag = outcomeFlagId;
            var scene = new MissionSceneDefinition
            {
                Script = "wilderness.corman-finale",
                HiddenObjectiveIds = new() { 5 },
                Requirement = new AllRequirements(new MissionRequirement[]
                {
                    FirstBatchCarried(),
                    new NotRequirement(new MissionStateRequirement(701))
                }),
                ObjectiveRequirements = new()
                {
                    [1] = FirstBatchCarried(true),
                    [2] = FirstBatchCarried(true)
                },
                Actors = new()
                {
                    ["skeev"] = new SceneActorDefinition("skeev", SceneActorKind.PublicSpawn, skeevSpawnId,
                        GameplayPolicy: new ActorGameplayPolicy { TrackParticipation = true, RewardScenarioKills = true })
                },
                // Owner loss must not reset the absolute ultimatum deadline.
                PublicEncounter = new PublicEncounterBinding(701, skeevSpawnId, "skeev",
                    "wilderness.corman-finale", OwnerLossPolicy: "Wait", ManualCombat: true),
                DefeatSequences = new() { ["skeev"] = 5 },
                Dialogue = new()
                {
                    new MissionDialogueTopicDefinition(2, 595, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 }),
                    new MissionDialogueTopicDefinition(8, 609, 1, transitionId: 1,
                        requirement: new FlagRequirement(flag, 1)),
                    new MissionDialogueTopicDefinition(8, 609, 26, transitionId: 2,
                        requirement: new FlagRequirement(flag, 26)),
                    new MissionDialogueTopicDefinition(8, 609, 27, transitionId: 3,
                        requirement: new FlagRequirement(flag, 27))
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new EnsureActorIntent("reserve-skeev", "skeev"),
                            new SetInteractionIntent("skeev-awaits-burke", "skeev", false)
                        },
                        Character = new() { new SetCharacterFlagIntent("clear-corman-outcome", flag, null) }
                    },
                    [1] = new SceneSequenceDefinition
                    {
                        World = new() { new SetInteractionIntent("skeev-ultimatum", "skeev", true) }
                    },
                    [2] = new SceneSequenceDefinition
                    {
                        Character = new()
                        {
                            new SetCharacterFlagIntent("corman-surrendered", flag, 26),
                            new ObjectiveIntent("surrender-reveals-burke", 701, 8, MissionObjectiveState.NotAssigned),
                            new ObjectiveIntent("surrender-activates-burke", 701, 8, MissionObjectiveState.Incomplete)
                        }
                    },
                    [3] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new SetInteractionIntent("skeev-refusal-closes-dialogue", "skeev", false),
                            new AttackActorIntent("skeev-attacks-after-refusal", "skeev")
                        },
                        Character = new() { new SetCharacterFlagIntent("corman-refused", flag, 1) }
                    },
                    [4] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new SetInteractionIntent("skeev-timeout-closes-dialogue", "skeev", false),
                            new AttackActorIntent("skeev-attacks-after-timeout", "skeev")
                        },
                        Character = new() { new SetCharacterFlagIntent("corman-timed-out", flag, 27) }
                    },
                    [5] = new SceneSequenceDefinition()
                }
            };
            return scene;
        }
    }
}
