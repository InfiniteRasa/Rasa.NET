using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content
{
    public sealed class MissionSceneDefinition
    {
        public string Script { get; set; }
        public int StateVersion { get; set; } = 1;
        /// <summary>World effects are refused outside this map; zero leaves the scene unrestricted.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint MapContextId { get; set; }
        public MissionAudioDefinition Audio { get; set; }
        public Dictionary<string, SceneActorDefinition> Actors { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, SceneRoute> Routes { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<uint, SceneSequenceDefinition> Sequences { get; set; } = new();
        public Dictionary<string, uint> Names { get; set; } = new();
        public Dictionary<string, uint> DefeatSequences { get; set; }
        /// <summary>
        /// Actor role to sequence: the sequence runs once, on the first hit a player's side lands on that actor
        /// (damage that took something off it; an immune hit does not count), as DefeatSequences runs on its
        /// death. Once per run generation: a reload or a respawned actor does not run it again.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, uint> DamagedSequences { get; set; }
        public Dictionary<uint, MissionCreditPolicy> Credit { get; set; } = new();
        public MissionRequirement Requirement { get; set; }
        public MissionRequirement TurnInRequirement { get; set; }
        public Dictionary<uint, MissionRequirement> ObjectiveRequirements { get; set; } = new();
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<uint, MissionObjectiveAggregation> ObjectiveAggregations { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<uint, MissionHistoryAggregation> ObjectiveHistoryAggregations { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<uint> HiddenObjectiveIds { get; set; }
        /// <summary>
        /// Objective id to its scene set's events split over the client's counters: entry N is the
        /// events counter N counts, as Sabotage the Tower's 6 generators and 10 lockers
        /// (PoolsSabotageTheTowerV1). The objective completes when all are full.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<uint, List<List<uint>>> CounterGroups { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<uint> ExistingFactObjectiveIds { get; set; }
        public PublicEncounterBinding PublicEncounter { get; set; }
        /// <summary>
        /// The client's mission category (missioncategorylanguage) when it is one of the ids past
        /// a byte, which the definition's category_id cannot hold: "Battlefield (Wilderness)" is
        /// 10000044. The mission log files the mission under it.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? Category { get; set; }
        /// <summary>Objective id to title id: the title the character earns when the objective completes.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<uint, uint> Titles { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<MissionDialogueTopicDefinition> Dialogue { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<MissionItemBinding> Items { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CharacterIntent> AcceptanceItems { get; set; }
        /// <summary>
        /// The item classes the character has to carry, in the personal inventory, to take the
        /// mission: the offer's itemsRequired. The client lists them under "Item Requirements"
        /// on the offer and greys Accept until it holds one of each
        /// (_UpdateItemRequirements); the acceptance is refused without them. None are taken.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<uint> ItemsRequired { get; set; }
        /// <summary>
        /// The map the scene's actors stand on (a map_info id; for a squad instance, the copy the owner is in).
        /// Unset, the scene runs on the map where the mission was accepted. Set, a mission accepted elsewhere
        /// waits: its scene starts when the owner first comes onto that map, a step asked for before then waits
        /// with it, and the run goes with the owner to a later copy of the map (a new squad, a weekly reset).
        /// The acceptance items come at acceptance either way. (MapContextId above only refuses world effects
        /// off its map; it does not move or hold a run.)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? BoundMapContextId { get; set; }

        public SceneBindings Bindings(string revision) => new(revision, Actors, Routes,
            Sequences.ToDictionary(entry => entry.Key, entry => new SceneSequence(
                entry.Value.World, entry.Value.Character, entry.Value.Signals, entry.Value.Timers)), Names, DefeatSequences,
            MapContextId, DamagedSequences);
    }

    public sealed class MissionExperienceDefinition
    {
        public string Key { get; set; } = "";
        public string Revision { get; set; } = "";
        public uint MapContextId { get; set; }
        public bool PrivatePerCharacter { get; set; }
        public MissionSceneDefinition Scene { get; set; } = new();
        public List<ExperienceMissionTrigger> MissionTriggers { get; set; } = new();
        public Dictionary<uint, ActorGameplayPolicy> ActorPolicies { get; set; } = new();
    }

    public sealed record ExperienceMissionTrigger(uint MissionId, string Event, uint SequenceId);

    public sealed class SceneSequenceDefinition
    {
        public List<WorldIntent> World { get; set; } = new();
        public List<CharacterIntent> Character { get; set; } = new();
        public List<SceneMissionSignal> Signals { get; set; } = new();
        public List<SequenceTimer> Timers { get; set; } = new();
    }
}
