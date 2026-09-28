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
        public MissionAudioDefinition Audio { get; set; }
        public Dictionary<string, SceneActorDefinition> Actors { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, SceneRoute> Routes { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<uint, SceneSequenceDefinition> Sequences { get; set; } = new();
        public Dictionary<string, uint> Names { get; set; } = new();
        public Dictionary<string, uint> DefeatSequences { get; set; }
        public Dictionary<uint, MissionCreditPolicy> Credit { get; set; } = new();
        public MissionRequirement Requirement { get; set; }
        public MissionRequirement TurnInRequirement { get; set; }
        public Dictionary<uint, MissionRequirement> ObjectiveRequirements { get; set; } = new();
        public PublicEncounterBinding PublicEncounter { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<MissionDialogueTopicDefinition> Dialogue { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<MissionItemBinding> Items { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CharacterIntent> AcceptanceItems { get; set; }

        public SceneBindings Bindings(string revision) => new(revision, Actors, Routes,
            Sequences.ToDictionary(entry => entry.Key, entry => new SceneSequence(
                entry.Value.World, entry.Value.Character, entry.Value.Signals, entry.Value.Timers)), Names, DefeatSequences);
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
