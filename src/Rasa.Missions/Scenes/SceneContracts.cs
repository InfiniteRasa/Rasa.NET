using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Scenes
{
    public enum SceneStatus { Running, Waiting, Ended, Faulted, Resetting }
    public enum SceneEventKind { Started, Signal, TimerElapsed, WaypointReached, RouteCompleted, ActorDied, OwnerLost, Recovered, Cancelled, MissionRewarded, ObjectiveDeadlineElapsed }
    public enum SceneClockPolicy { WallClock, ActiveScene }
    public enum SceneActorKind { PublicSpawn, Creature, Object, PracticeTarget }
    public enum PresentationKind { Tutorial, Audio, Greeting }

    public sealed record ScenePosition(float X, float Y, float Z);
    public sealed record SceneActorDefinition(
        string Role, SceneActorKind Kind, uint TemplateId,
        ScenePosition Position = null, double Orientation = 0,
        bool InitiallyInteractable = true, uint InitialObjectState = 0,
        uint? LootMissionId = null, uint? LootRewardId = null, uint? LootObjectiveId = null,
        string SharedKey = null, uint? WindupMilliseconds = null, uint MissionId = 0,
        uint? GroupId = null, uint SpawnId = 0, ScenePosition FollowOffset = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SceneObjectConversation Conversation = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Definitions.ActorGameplayPolicy GameplayPolicy = null);
    public sealed record SceneObjectConversation(uint MissionId, uint ObjectiveId, uint NpcPackageId,
        uint DialogObjectiveId, uint PlayerFlagId = 1,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Definitions.MissionDialogueKind Kind =
            Definitions.MissionDialogueKind.Completion);
    public sealed record SceneWaypoint(ScenePosition Position, double Orientation = 0, uint PauseMilliseconds = 0);
    public sealed record SceneSpawnPose(ActorHandle Handle, uint OwnerCharacterId, ScenePosition Position, double Orientation);
    public sealed record SceneRoute(string Key, IReadOnlyList<SceneWaypoint> Points, float Speed = 6.5f, bool ResumeAtDestination = false);
    public sealed record SceneRun(
        string Id, string Release, string ScriptKey, int StateVersion,
        uint Generation, long Version, string Checkpoint, SceneStatus Status, uint OwnerCharacterId, uint MissionId = 0);
    public sealed record SceneObservation(
        SceneEventKind Kind, uint Generation, string Name = null, string Role = null,
        string OperationKey = null, uint SequenceId = 0, int Waypoint = 0, string DeliveryKey = null,
        ScenePosition Position = null);
    public sealed record SceneTimerChange(
        string Name, SceneClockPolicy ClockPolicy, DateTime? DueAtUtc,
        uint SequenceId = 0, bool Cancel = false);
    public sealed record SceneMissionSignal(uint MissionId, uint SequenceId, uint EventId);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(EnsureActorIntent), "ensure")]
    [JsonDerivedType(typeof(RemoveActorIntent), "remove")]
    [JsonDerivedType(typeof(SetInteractionIntent), "interaction")]
    [JsonDerivedType(typeof(RunRouteIntent), "route")]
    [JsonDerivedType(typeof(FollowActorIntent), "follow")]
    [JsonDerivedType(typeof(PresentationIntent), "presentation")]
    [JsonDerivedType(typeof(TransferIntent), "transfer")]
    [JsonDerivedType(typeof(RestoreActorPoseIntent), "restore-pose")]
    [JsonDerivedType(typeof(TransitionObjectStateIntent), "object-state")]
    [JsonDerivedType(typeof(AttackActorIntent), "attack")]
    public abstract record WorldIntent(string OperationKey, string Role);
    public sealed record EnsureActorIntent(string OperationKey, string Role, ScenePosition RestorePosition = null) : WorldIntent(OperationKey, Role);
    public sealed record RemoveActorIntent(string OperationKey, string Role) : WorldIntent(OperationKey, Role);
    public sealed record SetInteractionIntent(string OperationKey, string Role, bool Enabled, uint? ObjectState = null,
        bool IfPresent = false) : WorldIntent(OperationKey, Role);
    public sealed record RunRouteIntent(string OperationKey, string Role, string Route, int StartWaypoint = 0,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool ResumeAfterCombat = false)
        : WorldIntent(OperationKey, Role);
    public sealed record AttackActorIntent(string OperationKey, string Role, string TargetRole = null)
        : WorldIntent(OperationKey, Role);
    public sealed record FollowActorIntent(string OperationKey, string Role, uint CharacterId, bool Enabled = true) : WorldIntent(OperationKey, Role);
    public sealed record PresentationIntent(string OperationKey, PresentationKind Kind, uint Value) : WorldIntent(OperationKey, null);
    public sealed record TransferIntent(string OperationKey, uint MapContextId, ScenePosition Position, double Orientation) : WorldIntent(OperationKey, null);
    public sealed record RestoreActorPoseIntent(string OperationKey, string Role, ScenePosition Position, double Orientation)
        : WorldIntent(OperationKey, Role);
    public sealed record TransitionObjectStateIntent(string OperationKey, string Role, uint State,
        uint WindupMilliseconds = 0) : WorldIntent(OperationKey, Role);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(GrantRewardIntent), "reward")]
    [JsonDerivedType(typeof(GrantAbilityIntent), "ability")]
    [JsonDerivedType(typeof(SetQualificationIntent), "qualification")]
    [JsonDerivedType(typeof(SetCharacterFlagIntent), "character-flag")]
    [JsonDerivedType(typeof(SetEntitlementIntent), "entitlement")]
    [JsonDerivedType(typeof(ObjectiveIntent), "objective")]
    [JsonDerivedType(typeof(MissionDeadlineIntent), "deadline")]
    [JsonDerivedType(typeof(IssueMissionItemIntent), "issue-mission-item")]
    [JsonDerivedType(typeof(ConsumeMissionItemIntent), "consume-mission-item")]
    [JsonDerivedType(typeof(RemoveMissionItemsIntent), "remove-mission-items")]
    [JsonDerivedType(typeof(OfferRadioMissionIntent), "offer-radio-mission")]
    public abstract record CharacterIntent(string OperationKey);
    public sealed record GrantRewardIntent(string OperationKey, uint MissionId, uint RewardId) : CharacterIntent(OperationKey);
    public sealed record GrantAbilityIntent(string OperationKey, uint SkillId, uint AbilityId, byte Level, byte? Slot) : CharacterIntent(OperationKey);
    public sealed record SetQualificationIntent(string OperationKey, byte Qualification, bool Present) : CharacterIntent(OperationKey);
    public sealed record SetCharacterFlagIntent(string OperationKey, uint FlagId, uint? Value) : CharacterIntent(OperationKey);
    public sealed record SetEntitlementIntent(string OperationKey, bool Enabled) : CharacterIntent(OperationKey);
    public sealed record ObjectiveIntent(string OperationKey, uint MissionId, uint ObjectiveId, Data.MissionObjectiveState State) : CharacterIntent(OperationKey);
    public enum DeadlineIntentKind { Start, Satisfy, Cancel }
    public sealed record MissionDeadlineIntent(string OperationKey, uint MissionId, DeadlineIntentKind Kind, uint Milliseconds = 0)
        : CharacterIntent(OperationKey);
    public enum MissionItemScope { AssignmentIssued, CharacterOwned }
    public sealed record IssueMissionItemIntent(string OperationKey, uint MissionId,
        string ItemKey, uint ItemTemplateId, uint Quantity) : CharacterIntent(OperationKey);
    public sealed record ConsumeMissionItemIntent(string OperationKey, uint MissionId,
        string ItemKey, uint Quantity, MissionItemScope Scope) : CharacterIntent(OperationKey);
    public sealed record RemoveMissionItemsIntent(string OperationKey, uint MissionId,
        string ItemKey) : CharacterIntent(OperationKey);
    public sealed record OfferRadioMissionIntent(string OperationKey, uint MissionId, bool ForceDialog = true)
        : CharacterIntent(OperationKey);

    public sealed class SceneSequence
    {
        public IReadOnlyList<WorldIntent> WorldIntents { get; }
        public IReadOnlyList<CharacterIntent> CharacterIntents { get; }
        public IReadOnlyList<SceneMissionSignal> Signals { get; }
        public IReadOnlyList<SequenceTimer> Timers { get; }

        public SceneSequence(IEnumerable<WorldIntent> worldIntents = null,
            IEnumerable<CharacterIntent> characterIntents = null,
            IEnumerable<SceneMissionSignal> signals = null, IEnumerable<SequenceTimer> timers = null)
        {
            WorldIntents = Array.AsReadOnly((worldIntents ?? Array.Empty<WorldIntent>()).ToArray());
            CharacterIntents = Array.AsReadOnly((characterIntents ?? Array.Empty<CharacterIntent>()).ToArray());
            Signals = Array.AsReadOnly((signals ?? Array.Empty<SceneMissionSignal>()).ToArray());
            Timers = Array.AsReadOnly((timers ?? Array.Empty<SequenceTimer>()).ToArray());
        }
    }

    public sealed record SequenceTimer(string Name, uint Milliseconds, uint SequenceId,
        SceneClockPolicy ClockPolicy = SceneClockPolicy.WallClock, bool Cancel = false);

    public sealed class SceneBindings
    {
        public string Release { get; }
        public IReadOnlyDictionary<string, SceneActorDefinition> Actors { get; }
        public IReadOnlyDictionary<string, SceneRoute> Routes { get; }
        public IReadOnlyDictionary<uint, SceneSequence> Sequences { get; }
        public IReadOnlyDictionary<string, uint> Names { get; }
        public IReadOnlyDictionary<string, uint> DefeatSequences { get; }
        public SceneBindings(string release, IDictionary<string, SceneActorDefinition> actors,
            IDictionary<string, SceneRoute> routes, IDictionary<uint, SceneSequence> sequences,
            IDictionary<string, uint> names = null, IDictionary<string, uint> defeatSequences = null)
        {
            Release = release;
            Actors = new ReadOnlyDictionary<string, SceneActorDefinition>(actors.ToDictionary(
                entry => entry.Key, entry => entry.Value with
                { GameplayPolicy = entry.Value.GameplayPolicy?.Snapshot() }, StringComparer.Ordinal));
            Routes = new ReadOnlyDictionary<string, SceneRoute>(new Dictionary<string, SceneRoute>(routes));
            Sequences = new ReadOnlyDictionary<uint, SceneSequence>(new Dictionary<uint, SceneSequence>(sequences));
            Names = new ReadOnlyDictionary<string, uint>(new Dictionary<string, uint>(
                names ?? new Dictionary<string, uint>()));
            DefeatSequences = new ReadOnlyDictionary<string, uint>(new Dictionary<string, uint>(
                defeatSequences ?? new Dictionary<string, uint>()));
        }
    }

    public sealed record SceneContext(SceneRun Run, SceneBindings Bindings, DateTime UtcNow);

    public sealed class SceneDecision
    {
        public string Checkpoint { get; }
        public IReadOnlyList<WorldIntent> WorldIntents { get; }
        public IReadOnlyList<CharacterIntent> CharacterIntents { get; }
        public IReadOnlyList<SceneMissionSignal> Signals { get; }
        public IReadOnlyList<SceneTimerChange> Timers { get; }
        public SceneStatus Status { get; }
        public string Fault { get; }
        public bool ResetGeneration { get; }

        public SceneDecision(string checkpoint, IEnumerable<WorldIntent> worldIntents = null,
            IEnumerable<CharacterIntent> characterIntents = null,
            IEnumerable<SceneMissionSignal> signals = null, IEnumerable<SceneTimerChange> timers = null,
            SceneStatus status = SceneStatus.Running, string fault = null, bool resetGeneration = false)
        {
            Checkpoint = checkpoint;
            WorldIntents = Array.AsReadOnly((worldIntents ?? Array.Empty<WorldIntent>()).ToArray());
            CharacterIntents = Array.AsReadOnly((characterIntents ?? Array.Empty<CharacterIntent>()).ToArray());
            Signals = Array.AsReadOnly((signals ?? Array.Empty<SceneMissionSignal>()).ToArray());
            Timers = Array.AsReadOnly((timers ?? Array.Empty<SceneTimerChange>()).ToArray());
            Status = status;
            Fault = fault;
            ResetGeneration = resetGeneration;
        }
    }
}
