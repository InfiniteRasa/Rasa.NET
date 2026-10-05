using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rasa.Structures
{
    using Data;
    using global::Rasa.Missions.Definitions;

    public sealed class Mission
    {
        public uint MissionId { get; }
        public string Name { get; }
        public string ContentRevision { get; }
        public global::Rasa.Missions.Runtime.MissionRepeatPolicy RepeatPolicy { get; }
        public MissionChannel AcceptanceChannel { get; }
        public MissionChannel CompletionChannel { get; }
        public IReadOnlyList<MissionOfferSourceDefinition> RadioSources { get; }
        public global::Rasa.Missions.Runtime.MissionRequirement Requirement { get; }
        public global::Rasa.Missions.Runtime.MissionRequirement TurnInRequirement { get; }
        public IReadOnlyDictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement> ObjectiveRequirements { get; }
        public uint? ClientNameTextId { get; }
        public uint? MissionGiver { get; }
        public uint? MissionReciver { get; }
        public uint? Level { get; }
        public byte? GroupType { get; }
        public uint? CategoryId { get; }

        /// <summary>
        /// The mission's category as the client knows it (missioncategorylanguage), from the
        /// scene binding, for content whose definition row does not carry it - the battlefields
        /// are 10000016 and up. Null: <see cref="CategoryId"/> is the category.
        /// </summary>
        public uint? ClientCategoryId { get; }
        public bool? Shareable { get; }
        public bool? RadioCompletable => IsOperational && CompletionChannel.HasFlag(MissionChannel.Radio);
        public IReadOnlyDictionary<uint, MissionObjectiveDefinition> Objectives { get; }
        public IReadOnlyList<MissionDialogueTopicDefinition> Dialogue { get; }
        public IReadOnlyDictionary<string, MissionItemBinding> Items { get; }
        public IReadOnlyList<global::Rasa.Missions.Scenes.CharacterIntent> AcceptanceItems { get; }
        public bool IsOperational { get; }
        public string OperationalDiagnostic { get; }

        public Mission(
            uint missionId,
            string name,
            uint? clientNameTextId,
            uint? missionGiver,
            uint? missionReciver,
            uint? level,
            byte? groupType,
            uint? categoryId,
            bool? shareable,
            bool? radioCompletable,
            IEnumerable<MissionObjectiveDefinition> objectives,
            bool enableOperational = false,
            string operationalDiagnostic = null,
            string contentRevision = "unversioned",
            global::Rasa.Missions.Runtime.MissionRequirement requirement = null,
            global::Rasa.Missions.Runtime.MissionRequirement turnInRequirement = null,
            IReadOnlyDictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement> objectiveRequirements = null,
            IEnumerable<MissionDialogueTopicDefinition> dialogue = null,
            IEnumerable<MissionItemBinding> items = null,
            IEnumerable<global::Rasa.Missions.Scenes.CharacterIntent> acceptanceItems = null,
            global::Rasa.Missions.Runtime.MissionRepeatPolicy repeatPolicy = null,
            MissionChannel acceptanceChannel = MissionChannel.Npc,
            MissionChannel completionChannel = MissionChannel.Npc,
            IEnumerable<MissionOfferSourceDefinition> radioSources = null,
            uint? clientCategoryId = null)
        {
            MissionId = missionId;
            ClientCategoryId = clientCategoryId;
            ContentRevision = contentRevision;
            RepeatPolicy = repeatPolicy ?? global::Rasa.Missions.Runtime.MissionRepeatPolicy.Once;
            AcceptanceChannel = acceptanceChannel;
            CompletionChannel = completionChannel;
            RadioSources = Array.AsReadOnly((radioSources ?? Array.Empty<MissionOfferSourceDefinition>()).ToArray());
            Dialogue = Array.AsReadOnly((dialogue ?? Array.Empty<MissionDialogueTopicDefinition>()).ToArray());
            Items = new ReadOnlyDictionary<string, MissionItemBinding>(
                (items ?? Array.Empty<MissionItemBinding>()).ToDictionary(item => item.ItemKey, StringComparer.Ordinal));
            AcceptanceItems = Array.AsReadOnly((acceptanceItems ??
                Array.Empty<global::Rasa.Missions.Scenes.CharacterIntent>()).ToArray());
            Requirement = requirement;
            TurnInRequirement = turnInRequirement;
            ObjectiveRequirements = new ReadOnlyDictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement>(
                new Dictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement>(
                    objectiveRequirements ?? new Dictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement>()));
            Name = name;
            ClientNameTextId = clientNameTextId;
            MissionGiver = missionGiver;
            MissionReciver = missionReciver;
            Level = level;
            GroupType = groupType;
            CategoryId = categoryId;
            Shareable = shareable;
            var objectiveDictionary = (objectives ?? Array.Empty<MissionObjectiveDefinition>())
                .ToDictionary(objective => objective.ObjectiveId);
            Objectives = new ReadOnlyDictionary<uint, MissionObjectiveDefinition>(
                objectiveDictionary);
            var diagnostics = new List<string>();
            if (RepeatPolicy.ValidationError is { } repeatError)
                diagnostics.Add(repeatError);
            diagnostics.AddRange(MissionChannelValidation.Errors(this));
            if (!enableOperational)
                diagnostics.Add("operational eligibility was not enabled");
            if (AcceptanceChannel.HasFlag(MissionChannel.Npc) && !MissionGiver.HasValue ||
                CompletionChannel.HasFlag(MissionChannel.Npc) && !MissionReciver.HasValue ||
                !Level.HasValue || !GroupType.HasValue || !CategoryId.HasValue || !Shareable.HasValue)
                diagnostics.Add("mission database metadata is incomplete");
            if (objectiveDictionary.Values.Any(objective => !objective.HasCompleteServerContract))
                diagnostics.Add("one or more objective server contracts are incomplete");
            if (objectiveDictionary.Values
                .Where(objective =>
                    objective.RevealedObjectiveIds != null &&
                    objective.ActivatedObjectiveIds != null)
                .SelectMany(objective =>
                    objective.RevealedObjectiveIds.Concat(objective.ActivatedObjectiveIds))
                .Any(objectiveId => !objectiveDictionary.ContainsKey(objectiveId)))
                diagnostics.Add("an objective successor is missing");
            diagnostics.AddRange(MissionDialogueValidation.Errors(this));
            diagnostics.AddRange(MissionObjectiveAggregation.Errors(Objectives));
            if (Objectives.Values.Any(objective => objective.HistoryAggregation?.Groups
                .Any(group => group.Contains(MissionId)) == true))
                diagnostics.Add("a history aggregate cannot require its own mission");
            if (!string.IsNullOrWhiteSpace(operationalDiagnostic))
                diagnostics.Add(operationalDiagnostic);

            IsOperational = diagnostics.Count == 0;
            OperationalDiagnostic = IsOperational
                ? null
                : string.Join("; ", diagnostics);
        }

        internal IReadOnlyDictionary<uint, MissionObjectiveLog> CreateInitialObjectiveLogs() =>
            Objectives.Values.ToDictionary(
                objective => objective.ObjectiveId,
                objective => new MissionObjectiveLog(
                    objective.ObjectiveId,
                    objective.InitialState.Value,
                    objective.Counters.ToDictionary(
                        counter => counter.Key,
                        counter => counter.Value.InitialValue),
                    objective.ItemCounters.ToDictionary(
                        counter => counter.Key,
                        counter => counter.Value.InitialValue)));

        internal Mission WithWorldMetadata(Mission worldDefinition) =>
            new(
                MissionId,
                Name,
                ClientNameTextId,
                worldDefinition?.MissionGiver,
                worldDefinition?.MissionReciver,
                worldDefinition?.Level,
                worldDefinition?.GroupType,
                worldDefinition?.CategoryId,
                worldDefinition?.Shareable,
                worldDefinition?.RadioCompletable,
                Objectives.Values,
                enableOperational: true,
                contentRevision: ContentRevision,
                requirement: Requirement, turnInRequirement: TurnInRequirement,
                objectiveRequirements: ObjectiveRequirements, dialogue: Dialogue,
                items: Items.Values, acceptanceItems: AcceptanceItems, repeatPolicy: RepeatPolicy,
                acceptanceChannel: AcceptanceChannel, completionChannel: CompletionChannel, radioSources: RadioSources,
                clientCategoryId: ClientCategoryId);

        internal Mission DisableOperational(string diagnostic) =>
            new(
                MissionId,
                Name,
                ClientNameTextId,
                MissionGiver,
                MissionReciver,
                Level,
                GroupType,
                CategoryId,
                Shareable,
                RadioCompletable,
                Objectives.Values,
                enableOperational: true,
                operationalDiagnostic: diagnostic,
                contentRevision: ContentRevision,
                requirement: Requirement, turnInRequirement: TurnInRequirement,
                objectiveRequirements: ObjectiveRequirements, dialogue: Dialogue,
                items: Items.Values, acceptanceItems: AcceptanceItems, repeatPolicy: RepeatPolicy,
                acceptanceChannel: AcceptanceChannel, completionChannel: CompletionChannel, radioSources: RadioSources,
                clientCategoryId: ClientCategoryId);

        internal Mission WithPolicies(
            IReadOnlyDictionary<uint, global::Rasa.Missions.Runtime.MissionCreditPolicy> credit,
            global::Rasa.Missions.Runtime.MissionRequirement requirement,
            global::Rasa.Missions.Runtime.MissionRequirement turnIn = null,
            IReadOnlyDictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement> objectives = null) =>
            new(MissionId, Name, ClientNameTextId, MissionGiver, MissionReciver, Level, GroupType,
                CategoryId, Shareable, RadioCompletable,
                Objectives.Values.Select(objective => credit.TryGetValue(objective.ObjectiveId, out var policy)
                    ? objective.WithCreditPolicy(policy) : objective), IsOperational, OperationalDiagnostic, ContentRevision,
                requirement, turnIn, objectives, Dialogue, Items.Values, AcceptanceItems, RepeatPolicy,
                AcceptanceChannel, CompletionChannel, RadioSources, ClientCategoryId);

        /// <summary>The titles its objectives give (objective id to title id), from the scene binding.</summary>
        internal Mission WithTitles(IReadOnlyDictionary<uint, uint> titles) =>
            new(MissionId, Name, ClientNameTextId, MissionGiver, MissionReciver, Level, GroupType,
                CategoryId, Shareable, RadioCompletable,
                Objectives.Values.Select(objective => titles.TryGetValue(objective.ObjectiveId, out var titleId)
                    ? objective.WithTitle(titleId) : objective), IsOperational, OperationalDiagnostic, ContentRevision,
                Requirement, TurnInRequirement, ObjectiveRequirements, Dialogue, Items.Values, AcceptanceItems, RepeatPolicy,
                AcceptanceChannel, CompletionChannel, RadioSources, ClientCategoryId);

        /// <summary>The category the client files it under, from the scene binding.</summary>
        internal Mission WithClientCategory(uint? clientCategoryId) =>
            new(MissionId, Name, ClientNameTextId, MissionGiver, MissionReciver, Level, GroupType,
                CategoryId, Shareable, RadioCompletable, Objectives.Values, IsOperational,
                OperationalDiagnostic, ContentRevision, Requirement, TurnInRequirement, ObjectiveRequirements, Dialogue,
                Items.Values, AcceptanceItems, RepeatPolicy, AcceptanceChannel, CompletionChannel, RadioSources, clientCategoryId);

        internal Mission WithDialogue(IEnumerable<MissionDialogueTopicDefinition> dialogue) =>
            new(MissionId, Name, ClientNameTextId, MissionGiver, MissionReciver, Level, GroupType,
                CategoryId, Shareable, RadioCompletable, Objectives.Values, IsOperational,
                OperationalDiagnostic, ContentRevision, Requirement, TurnInRequirement, ObjectiveRequirements, dialogue,
                Items.Values, AcceptanceItems, RepeatPolicy, AcceptanceChannel, CompletionChannel, RadioSources, ClientCategoryId);

        internal Mission WithItems(IEnumerable<MissionItemBinding> items,
            IEnumerable<global::Rasa.Missions.Scenes.CharacterIntent> acceptanceItems) =>
            new(MissionId, Name, ClientNameTextId, MissionGiver, MissionReciver, Level, GroupType,
                CategoryId, Shareable, RadioCompletable, Objectives.Values, IsOperational,
                OperationalDiagnostic, ContentRevision, Requirement, TurnInRequirement, ObjectiveRequirements, Dialogue,
                items, acceptanceItems, RepeatPolicy, AcceptanceChannel, CompletionChannel, RadioSources, ClientCategoryId);
    }
}
