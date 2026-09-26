# Mission data and script reference

Use [Mission authoring and operations](missions.md) for the migration workflow.
Mission content is authored in C# and deployed by EF migrations. No pack
envelope, release manifest or publish command is required.

## Source map

| Contract | Source |
| --- | --- |
| Shared scene and experience definitions | [MissionSceneDefinition.cs](../src/Rasa.Missions/Content/MissionSceneDefinition.cs) |
| Internal scene serialization | [MissionContentCodec.cs](../src/Rasa.Missions/Content/MissionContentCodec.cs) |
| Migration helpers | [MissionDataMigration.cs](../src/Rasa.DBL/Services/Preloader/Missions/MissionDataMigration.cs) |
| Consolidated World seed | [WorldContentDataV1.cs](../src/Rasa.DBL/Services/Preloader/WorldContentDataV1.cs) |
| Fixed Bootcamp migration data | [BootcampMissionDataV1.cs](../src/Rasa.DBL/Services/Preloader/Missions/BootcampMissionDataV1.cs) |
| Normalized row fields | `src\Rasa.DBL\Structures\World\Mission*Entry.cs` |
| Objective trigger shapes | [MissionObjectiveRuntimeAnalyzer.cs](../src/Rasa.Game/Managers/MissionObjectiveRuntimeAnalyzer.cs), [MissionProgressRuleAuthoring.cs](../src/Rasa.Game/Managers/MissionProgressRuleAuthoring.cs) |
| Scene observations, intents and bindings | [SceneContracts.cs](../src/Rasa.Missions/Scenes/SceneContracts.cs) |
| Script discovery and sequences | [SceneScriptRegistry.cs](../src/Rasa.Missions/Scenes/SceneScriptRegistry.cs) |
| Predicates and handlers | [MissionRequirements.cs](../src/Rasa.Missions/Runtime/MissionRequirements.cs) |
| Authoritative fact providers | [MissionRequirementFactsAdapter.cs](../src/Rasa.Game/Missions/Persistence/MissionRequirementFactsAdapter.cs) |
| Typed native dialogue | `src\Rasa.Missions\Definitions\MissionDialogueTopicDefinition.cs`, `src\Rasa.Game\Missions\Protocol\MissionConversationProjection.cs` |
| Assignment-owned quest items | `src\Rasa.Missions\Definitions\MissionItemBinding.cs`, `src\Rasa.Game\Missions\Persistence\MissionItemPlanner.cs` |
| Repeat policies and attempt identity | `src\Rasa.Missions\Runtime\MissionRepeatPolicy.cs`, `src\Rasa.Missions\Runtime\MissionLog.cs` |
| Radio channels and source authoring | `src\Rasa.Missions\Definitions\MissionChannels.cs`, `src\Rasa.DBL\Structures\World\MissionChannelPolicyEntry.cs` |
| Durable offer authority | `src\Rasa.Game\Missions\MissionOfferAuthority.cs`, `src\Rasa.DBL\Repositories\Char\MissionOffer\MissionOfferRepository.cs` |
| Explicit party sharing | `src\Rasa.Game\Missions\MissionSharing.cs`, `src\Rasa.Game\Managers\PartyManager.cs` |

## Migrated definitions

Normalized rows repeat `MissionId` and `ContentRevision`. Revisions are nonempty
strings of at most 32 characters. Objective, transition, reward and scenario
cross-links stay within that mission/revision unless a field explicitly names
another mission.

`MissionContentDefinitionEntry.Enabled` controls runtime selection. Keep
unreconstructed/example data disabled and enable at most one revision per
mission. A migration can update an enabled definition; there is no separate
release-immutability rule.

Legacy `npc_mission` fallback rows remain inactive until a complete enabled
definition is installed by migrations. That inactivity diagnostic takes
precedence over reward-shape diagnostics for those rows; an unsupported legacy
reward must not bypass the operational-content gate.

`Requirement` on normalized rows is `MissionContentRequirement.Required` or
`Optional`, describing validation criticality. It is not a player-eligibility
predicate. `AbandonmentPolicy` is `Allowed` or `Prohibited`.

`GiverId` and `ReceiverId` identify creature definitions. Conversation triggers
use NPC **package** IDs; public actor reservations use static **spawn** IDs.
Keep those namespaces distinct.

`Shareable` opts into explicit party sharing, subject to the supported scene
capability and live source checks below. The native projection uses that
validated capability, not the row boolean alone. `RadioCompleteable` on old
normalized rows is retained for migration compatibility, but is not admission
authority or a completion switch. `Mission.RadioCompletable` is derived from
the validated explicit completion channel. Supply real native mission,
objective and text IDs and source evidence. A nonzero confidence value does
not verify the source.

## Radio offer authority

`Mission.AcceptanceChannel` and `CompletionChannel` independently use `Npc=1`,
`Radio=2` or `Mixed=3`. `mission_channel_policy(mission_id, content_revision)`
stores both and the JSON array of `MissionOfferSourceDefinition` values.
No row means NPC-only. A side that permits NPC interaction requires its NPC ID;
radio-only sides support actual SQL `NULL`. `MissionRuntime` indexes only
present, channel-authorized giver/receiver IDs. All definition copies preserve
channels, sources, repeat policy, dialogue, items and requirements.

| Source field | Meaning |
| --- | --- |
| `Kind` | `ServerEvent` or `Scene`; live party authority is not an authored radio source |
| `Key` | Authored event key or scene script key, 1-96 characters |
| `MapContextId` | Optional nonzero map restriction |
| `OwnedPrivateMap` | Require a private map owned by the recipient |
| `Requirement` | Additional shared requirement evaluated against durable facts |

`MissionOfferSourceIdentity` adds `InstanceId`, source `Generation`,
`AssignmentId` and `AssignmentGeneration`. Server events use a stable event ID
(the key by default), generation zero and no assignment. Scene intents capture
the originating run ID, scene generation and exact participant assignment;
these are not the shared actor's execution-root identity. A mission source's
release must match its current operational definition. The active
starting-experience fact used by Initiation is durable-only, not inferred from
the absence of a completion flag. Its `ReadState` scalar query bypasses EF's
tracked pre-write value at the final source check. Authored map restrictions
must reference an installed World map.

`MissionApplication.Offers.TryOffer` is the trusted server-event entry point.
`PlanOffer` is its enlisted scene adapter seam. Both return publication only
after committing Char state. `OfferRadioMissionIntent` is a `CharacterIntent`,
so its offer, source scene advancement and receipt share that transaction.
It does not change forwarded world-effect provenance or operation retirement.

Char stores one row per `(character_id, mission_id)` with an independent offer
ID, target revision, recipient account/entity identity, connection session,
player-location epoch, map epoch, source identity, creation/expiry times,
predecessor assignment/history identity, state and concurrency version.
Consumption records the new exact assignment ID/generation. A pending offer
is valid for **five UTC minutes**, with expiry excluded. Creation timestamps
are canonical microseconds for MySQL `datetime(6)` compatibility; final time
checks use the current UTC clock.

There are at most **30 stored transient slots per recipient** through this API.
Issuance prunes expired, invalid-session and terminal rows other than the slot
being reused. Consumed/cancelled rows are not permanent mission history.
Identical creation preserves the offer ID/expiry and sends no second dialog.
A different live source cannot overwrite an existing valid slot.
Session-invalid rows fail closed immediately; their physical cancellation or
collection can wait until another authority operation. An old connection
cannot cancel a newer connection's pending offer.

Radio acceptance revalidates the pending offer, predecessor, source, ownership,
requirements, repeat entitlement, duplicate assignment and journal capacity.
It consumes the offer and creates objectives and acceptance item plans in the
same common transaction used by NPC acceptance. Opening NPC/object dialogue
does not replace pending authority. NPC acceptance cancels any outstanding
radio or party offer for that mission.

Radio turn-in requires an explicit radio completion channel, not an NPC
conversation or an unconsumed acceptance offer. Admission captures one current
`MissionLog` identity and rechecks it in the shared reward transaction. Normal
objective, requirement, selection, inventory, repeat-window and per-assignment
receipt checks still apply. Legacy pending `Success` can be settled, not
reaccepted. Every non-null rating is rejected.

The common acceptance and reward planners treat requirements as
preconditions, not predicates that must remain true after their own writes.
Before writing, they capture durable typed inputs, including legacy admission
prerequisites and the selected radio source's requirement. Final validation
compares those inputs with the expected new `Active` assignment, or the
`Completed` journal/history and the reward planner's exact resulting level.
The captures belong to the existing unit-of-work transaction. Common reward,
progress/action and scene planners project their explicit writes in execution
order, including cross-mission failures and history, flag writes/removal,
entitlement and qualification changes. An optional objective failure does not
project a mission failure or new history. Earlier lifetime success/reward
memberships are retained when a later attempt fails.

Level-derived custom facts use the shared reward planner's planned level.
Qualification-derived completion uses the captured starting-experience state
and the planned flag value, not a reread of mutated persistence. A stored zero
flag remains distinct from an absent flag, and only the last ordered write is
expected. Thus a turn-in requiring flag 92 equal to 1 can intentionally set it
to 2 through another mission's completion action; a later write to 3 still
rejects. An unaccepted-only requirement can admit its own assignment, and an
eligible level-9 turn-in can award the XP that reaches level 10.

Only planners executing in the captured transaction contribute expectations.
Queued scenario work executed after commit belongs to its own transaction;
queueing it does not authorize its future writes in the current transaction.
Fresh final facts must match the captured inputs plus those explicit outputs.
Unexpected changes still reject and roll back the whole transaction.

Receipt/time writes remain in `FinalizePersistence`, before inventory guards.
Requirement postconditions, offer expiry/session/source identity and radio
reward/window checks run in the final read-only `ValidateCommitBoundary` phase.
Nothing persists after those guards. Source identity checks remain independent
of the captured source precondition.
Native completion has no assignment token: byte-identical old input after a
fresh eligible/current attempt cannot be distinguished from current input.
No native nonce or NPC placeholder is added.

## Explicit party sharing

`MissionApplication.Sharing.TryShare(sender, missionId)` captures the sender's
current active assignment, then releases the sender's mutation lock before
calling the existing `Offers.TryOffer` for each eligible recipient. Each
recipient's transaction commits independently before notification. This is
another source for the same durable authority, not another offer or reward
engine. Sharing does not require a radio acceptance channel.

Both players must be living, registered world actors in the same live party,
on the same map instance and within **20 units**, including the exact boundary,
at issuance and acceptance. Distance uses finite three-dimensional world
positions only after actor, cell and instance resolution. Equal coordinates
on different instances do not establish proximity. Durable character/account
ownership is checked separately from party membership.

`Party.LifetimeId` distinguishes recycled numeric party IDs.
`PartyMember.MembershipId` invalidates departures, rejoining, map departure and
character/entity replacement. `TryGetLiveMembership` checks the current
account-to-character/entity mapping. The nullable `party_source` JSON column
on `character_mission_offer` stores the immutable `MissionPartyOfferSource`:
party lifetime, both membership identities, sender character/account/entity,
and sender connection, player-location and map epochs. Recipient identity
continues to use `MissionSessionIdentity`. Actor IDs remain unsigned 64-bit
values; the native dialog identifies the sender's actor, never its account.

`SourceKind.Party` uses the sender's exact assignment ID/generation and the
target revision. Without a scene, its instance identity is the party lifetime
and its generation is zero. For a public encounter it instead pins the
existing run ID/generation. A participant may offer its own assignment to
another member, but the run's initiator identity stays separate.

The same five-minute expiry, one slot per recipient/mission, 30-slot cap,
predecessor checks and duplicate-notification suppression apply. Repeating an
identical valid offer neither renews expiry nor sends another dialog.
Departure, character/session/map changes, abandonment/replacement, expiry or
invalid current range make acceptance fail. Physical cancellation/pruning may
wait until another authority operation. Declining can simply dismiss the UI;
no decline RPC or automatic acceptance is introduced.

`AssignSharedMission(sourcePlayerEntityId, missionId)` must match a current
pending party offer. Radio callbacks cannot consume party authority, nor can
shared callbacks consume radio authority. The common acceptance planner
rechecks prerequisites, repeat policy, journal capacity and unsettled rewards,
creates fresh objectives and independent acceptance items, and consumes the
offer in one transaction. It retains the transaction-scoped ordered requirement
outputs and enlisted shadow inventory. Journal capacity is checked again at
the read-only commit boundary without counting the new slot twice.
The native callback has no offer token: byte-identical delayed input after a
fresh currently valid offer cannot be distinguished from current input.
Captured stale authority is still rejected; no extra native field is invented.

Public joining requires `PublicEncounterBinding.AllowPartyJoin = true`.
`TryGetJoinRun` checks the real reserved actor, lease, map, running scene,
initiator assignment and source participant. `AttachSharedAssignment` adds
the recipient's exact assignment membership in the acceptance transaction.
It creates no reservation, scene or actor and dispatches no second scene-start
event. Its final guard checks the persisted membership and run generation.

Only the initiator/script controls world work. Joined assignments cannot
start a scene or queue its sequences. Their personal dialogue can still update
their own objectives and flags. Retiring a participant deactivates only that
assignment's membership and expires its deliveries; initiator abandonment
retains the existing authored run cancellation. Execution-root/source-run
provenance, per-operation cancellations and reconstruction retries are unchanged.

Shared scenes with assignment-owned deadlines, or objective transitions using
`StartScenario`/`ActivateSpawnGroup`, must be nonshareable: those paths require
an independent scene controller. Content loading rejects incompatible
shareable declarations, and native status projection hides unsupported sharing.
The same capability check rejects required `Personal` scenario-event
transitions because their signals only progress the initiator's assignment.
Another objective's group policy does not make that personal path shareable.
It also traces required-progress dependencies backward through the executable
reveal/activate graph shared with content cycle validation, plus
`ObjectiveStateReached` prerequisites. This includes optional predecessors and
transitive chains in normalized, explicit-transition and legacy definitions.
Unlock edges only introduce dependencies for initially `Inactive` or
`NotAssigned` targets; already active objectives can progress independently.
All authored dependency branches count: an alternate progress or dialogue
branch does not prove an owner-only branch safe. This conservative check does
not simulate execution or prove branch equivalence.

Optional personal signals outside those dependencies and independent personal
dialogue/actions remain allowed, including personal actions that activate
required objectives. Authored `NearbyParty`/`EncounterParticipants` signals
retain future-only delivery. Validation never rewrites a credit policy or
grants participants world control.

Joining never copies checkpoint progress, flags, choices or rewards.
`GroupCreditService` freezes only future eligible kill/scenario deliveries.
Encounter kill credit uses the same exact participant lookup as scene signals.
Selection matches participant assignment ID and generation to each candidate
mission before evaluating its objective policies. Unmatched membership cannot
authorize encounter-only credit; unrelated `Personal`/`NearbyParty` assignments
still use their own eligibility rules. Freezing rechecks the captured assignment.
Previously frozen deliveries retain their original assignment ID/generation;
they cannot move to a repeated or late-joining assignment.

## Repeat policies and attempt history

`Mission.RepeatPolicy` is an immutable `MissionRepeatPolicy`. Missing metadata
uses `Once`; all mission copy paths preserve it alongside dialogue, items and
requirements. World stores the optional policy in
`mission_repeat_policy(mission_id, content_revision)`, not as new reflected
columns on historical normalized content entries.

| Kind/value | Timing fields | Admission after a successful attempt |
| --- | --- | --- |
| `Once` / 0 | Both null | Never, including after journal clear |
| `Immediate` / 1 | Both null | After the prior assignment is terminal and rewarded |
| `Cooldown` / 2 | Positive `CooldownSeconds`; null reset | At or after the last committed reward time plus the duration |
| `Daily` / 3 | `ResetSecondUtc` in 0..86399; null cooldown | When the last reward predates the current UTC window |

The UTC clock is injected through `MissionApplication`. A Daily window starts
at today's UTC midnight plus `ResetSecondUtc`; subtract one day if that instant
is still in the future. Midnight, host-local time and DST are not implicit
reset policies. Rewards are stamped after deferred inventory preparation and
before all final validation. A read-only commit-boundary check runs after every
participant has validated, including inventory plans enlisted by progress in
another mission. If reset crosses during those writes or database reads, the
transaction rolls back and the same valid conversation can retry.
No success packet is sent for that rollback.

There is one current journal row per character/mission, including pending
`Success`. Acceptance rechecks durable eligibility and capacity inside its
transaction, archives/removes an eligible terminal row, retires its old work,
and creates fresh objectives and a new assignment ID. New attempt generations
advance from prior stored generations. `MissionLog` carries the exact ID,
generation and stored content revision during creation and hydration.
`legacy`/`unversioned` remain compatible stored revisions, not permission to
substitute another attempt.
The committed native projection sends `MissionCleared` before `MissionGained`
when replacing a terminal journal. `Once` keeps its prior failure-dismissal
behavior, including Bootcamp's authored failure/reset flow; automatic terminal
replacement is an opt-in repeat behavior.

Transaction participants run `Prepare`, the initial flush,
`FinalizePersistence`, another flush if needed, all `Validate` guards, and
finally all read-only `ValidateCommitBoundary` checks before commit.
`TransactionValidation.BeforeValidation` stamps rewards during finalization;
`TransactionValidation.AtCommitBoundary` verifies the Daily window after all
final database validation. Neither validation phase may write receipts or
timestamps after the shared inventory plan has validated.

Forwarded shared-actor effects retain their experience-root execution identity
and separately store `SourceRunId`, `SourceGeneration`, `SourceAssignmentId`
and `SourceAssignmentGeneration`. Replacement cancels only that attempt's
forwarded effects. Post-commit route retirement matches the execution run,
generation and operation key, without resetting the root or removing its actors.
Removing a retired route's movement anchors the actor only while its controller
still runs that scripted movement. A retained movement reference alone does not
authorize replacing a newer attack or follow command.
Follow and attack controls also retain their current operation key, so retiring
an old attempt does not override a newer root command on the same actor.
Replay, reconstruction, acknowledgement and route callbacks check the durable
source identity; final persistence checks also guard callback/acknowledgement
writes. A cleared `Once` success can retain its authored experience state through
its exact terminal history row. Missing or changed source identities do not
authorize a new attempt's work.
Private instance keys can change when an instance is recreated. The current
private-map owner and durable source identity authorize replay, not equality
between historical instance keys; public map-key checks remain in force.

If reconstruction applies a durable `Running` or `Applied` effect but its
acknowledgement fails, the failure is logged and its owned route/control is
stopped. An ordinary retry tick can replay the operation. The retry retains the
exact execution and source identities, status, version, payload and concrete reconstructed intent, including pose
recovery instead of route movement. It rechecks current source ownership and
discards changed or superseded work, including newer route/follow/attack commands
on the same actor. It does not reconstruct the whole experience or replay
unrelated applied effects. Detachment or retirement clears the run's retry state;
fresh attachment still uses guarded reconstruction.

`character_mission_history` has one row per assignment ID. It records
`AssignmentGeneration`, terminal `Outcome`, `CompletedAtUtc`, `Rewarded`,
`RewardedAtUtc` and optional `RewardWindowStartUtc`. Outcomes use mission-state
values: `Success = 1` is pending reward, `Failed = 2`, `NotAssigned = 3` records
abandonment, and `Completed = 4` is rewarded. Settling a pending reward updates
that assignment's row; a later failed attempt does not overwrite earlier
success.

Use `MissionRuntimeRepository.EverSucceeded` for lifetime completion,
`LatestTerminal` for the most recent outcome, `LastRewardedAtUtc` for timing,
and `WasRewarded(assignmentId)` for duplicate reward reconciliation.
`HasHistory` retains its success-only compatibility meaning; it does not mean
"any failure exists" and is not repeat admission policy. `History` now returns
multiple rows per mission: never call `ToDictionary(missionId)` on it.
For explicit non-success state requirements, a present journal takes precedence
over terminal history. Only lifetime success/reward prerequisites can override a
nonmatching current journal. Committed abandonment immediately projects
`NotAssigned` into runtime history without clearing lifetime success or the last
reward timestamp.

Reward receipts use `(assignment_id, assignment_generation, "mission-reward")`.
Legacy generation-zero receipts remain valid evidence for that exact
assignment. A unique database index on
`(character_id, mission_id, reward_window_start_utc)` prevents competing Daily
claims. Other outcomes use null windows and do not collide on that guard.
Failure and abandonment consume neither cooldown nor Daily eligibility.
Unsettled `Success`, including preserved legacy history, blocks a new attempt.

`ConsolidatedCharacterSchema` creates per-assignment history, reward timestamps
and forwarding provenance directly. It does not import intermediate branch
journals, reconstruct missing attempts or infer prior reward claims. The
consolidated branch history requires fresh databases.

Bootcamp remains `Once`, private and unshareable. Persistent flags and
starting-experience state are separate from attempt history. Party acceptance
reuses these repeat and history rules; no Wilderness content is enabled.

### Reward arithmetic and publication

The shared mission reward planner checks currency, experience and crossed
clone-credit milestones before provider writes. Milestones at levels 5, 15 and
30 each add one clone credit when crossed. Arithmetic overflow is a gameplay
rejection and rolls back the whole reward. An `OverflowException` originating
from a provider operation is not gameplay arithmetic and retains its identity;
do not catch it around the complete persistence call.

A publication failure after commit does not undo history, items or rewards.
Reconnection restores committed state through the normal journal and inventory
readers, and the exact assignment's receipt prevents another grant.

## Objective triggers

Use one executable trigger shape per transition. Mixing conversation, progress,
area, objective-state and timer trigger shapes in one transition is rejected.

| `MissionTriggerKind` | Required binding |
| --- | --- |
| `Conversation` | `NpcPackageId`, `PlayerFlagId` |
| `ProgressEvent` | `EventKind` and event-specific parameters below |
| `AreaEntered` | `AreaId` |
| `ObjectiveState` | `RelatedObjectiveId`, `RelatedState` |
| `TimerElapsed` | Positive `DurationSeconds` |

Progress event values come from `MissionProgressEventKind`, not client opcodes:

| Value | Event | Parameter meaning |
| --- | --- | --- |
| 0 | `WaypointAcquired` | `SubjectId` is a waypoint ID |
| 1 | `LogosAcquired` | `SubjectId` is a Logos ID |
| 2 | `CreatureKilled` | `SubjectId` is a creature ID |
| 3 | `MissionCompleted` | `SubjectId` is the mission ID |
| 4, 5 | `ItemAcquired`, `ItemConsumed` | `SubjectId` is an item class, not template ID |
| 6 | `InteractionUsed` | `SubjectId` is the interacted entity class |
| 7 | `AreaEntered` | Prefer the typed area trigger |
| 8 | `ItemEquipped` | `SubjectId` is class, or template with `SourceSpawnResolved = true` |
| 9 | `AbilityHit` | `SubjectId` is action ID; `CounterId` is target creature ID |
| 10 | `ScenarioEvent` | `SubjectId` is signal event ID; `CounterId` is scenario/sequence ID |
| 11 | `DeadlineElapsed` | Prefer `TimerElapsed`; runtime supplies mission/objective scope |
| 12 | `ObjectiveStateReached` | Prefer the typed objective-state trigger |
| 13 | `ObjectHit` | `SubjectId` is entity class; `CounterId` is action ID |

For item counters, set `InitialValue` and `TargetValue`, omit `CounterId`, and
provide the native counter-text binding. Generic counters require all three.
Targets must exceed initial values. Do not combine counter ranges with
`SourceSpawnResolved`.

Multiple progress triggers form an all-distinct-subject set only for waypoints
or Logos; they are not a general AND expression. Counter text uses
`ClientCounter0TextId`, `ClientCounter1TextId`, `ClientCounter2TextId`.

Objective states are `NotAssigned = 0` (revealed), `Incomplete = 1` (active),
`Completed = 2`, `Failed = 3`, `Inactive = 4` (hidden). Only `Inactive` is omitted
from client projections. Hidden objectives remain in definitions and storage.

| `MissionActionKind` | Binding |
| --- | --- |
| `RevealObjective` | `TargetObjectiveId` |
| `ActivateObjective` | `TargetObjectiveId`, `ObjectiveState` |
| `CompleteObjective` | Current transition's `TargetObjectiveId`, `ObjectiveState` |
| `GrantReward` | `RewardId`, the turn-in package rather than an immediate grant |
| `StartScenario` | `ScenarioId`, also declared in the typed scene |
| `ActivateSpawnGroup` | `SpawnGroupId`; bind `Names["spawn-group-<id>"]` to its sequence |
| `ShowIndicator` | `IndicatorId` |
| `SetPlayerFlag` | `PlayerFlagId`, `PlayerFlagValue` |
| `ShowAmbientConversation` | `NpcPackageId` is the greeting text ID for this historical shape |
| `IssueMissionItem` (10) | `ItemIntentJson` containing an `IssueMissionItemIntent` |
| `ConsumeMissionItem` (11) | `ItemIntentJson` containing a `ConsumeMissionItemIntent` |
| `RemoveMissionItems` (12) | `ItemIntentJson` containing a `RemoveMissionItemsIntent` |

Reward rows distinguish fixed and selectable items. At most one turn-in reward
ID is selected per mission; selectable rewards require a native selection.
Scene reward grants cannot contain selectable alternatives.

## Assignment-owned quest items

Use quest-item operations for temporary items issued by an assignment. Permanent
items still belong in normal reward packages. A matching template alone never
establishes assignment ownership.

`MissionSceneDefinition.Items` supplies immutable `MissionItemBinding` values:

| Field | Meaning |
| --- | --- |
| `ItemKey` | Mission-local key, at most 64 characters |
| `ItemTemplateId` | Existing World item template |
| `Scope` | `AssignmentIssued` or an explicit `CharacterOwned` cost |
| `MaximumQuantity` | Positive upper bound on an operation and on the currently held assignment quantity |
| `Completion`, `Failure`, `Abandonment` | Explicit `Remove` or `Retain` dispositions |

All binding fields, including terminal dispositions, are required when reading
the internal serialized document. Character-owned bindings must use `Retain`
for every terminal disposition: cleanup is not permission to spend ordinary
inventory. Prefer `Remove` for temporary assignment items. `Retain` keeps their
provenance and restrictions; it does not convert them to ordinary rewards.
After an assignment row is cleared, retained ownership still exists and requires
an explicit migration or character deletion to retire.
Automatic replacement of a terminal journal for a new attempt instead retires
all of that old assignment's issued stacks, even `Retain` bindings. Normal
rewards and character-owned costs are unaffected; old operation receipts cannot
issue or spend the replacement attempt's items.

The character intents are:

```csharp
new IssueMissionItemIntent("recover-scanner", missionId, "scanner", templateId, 1);
new ConsumeMissionItemIntent("use-scanner", missionId, "scanner", 1,
    MissionItemScope.AssignmentIssued);
new RemoveMissionItemsIntent("retire-scanner", missionId, "scanner");
```

The issuing template, consuming scope and quantity must match the binding.
`CharacterOwned` consumption selects only unbound personal inventory; it never
spends another assignment's stack. An issue cannot use that scope. Removal
operates only on the exact assignment's concrete items for the named key.

For an ordinary objective transition, author the corresponding action kind and
serialize its intent into `MissionActionEntry.ItemIntentJson` with
`MissionContentCodec.Options`. Leave the other action parameter columns null.
Game executes the item plan in the same transaction as the selected objective
transition. Do not replace this with a queued scene grant after progression:
inventory capacity or a stale stack must reject the objective transition too.
NPC completion, typed native choices and object/corpse Continue share this path.
Flag actions and item actions on that transition are atomic together. An item
flush failure leaves neither the branch flag nor the issue receipt behind, and
the same still-valid conversation can retry. Successful native flag snapshots
and inventory publication occur only after commit.

For a scripted sequence, place these intents in `SceneSequenceDefinition.Character`.
Only mission-owned scenes with a matching assignment participant may use them,
not private experience scenes. The adapter checks the assignment ID and
assignment generation separately from the scene generation.

Acceptance issuance is explicit:

```csharp
scene.Items = new()
{
    new MissionItemBinding("scanner", templateId, MissionItemScope.AssignmentIssued, 1,
        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
        MissionItemCleanupDisposition.Remove)
};
scene.AcceptanceItems = new()
{
    new IssueMissionItemIntent("accept-scanner", missionId, "scanner", templateId, 1)
};
```

`Items` and `AcceptanceItems` are optional and omitted from serialized content
when absent. All `Mission` copy paths preserve them and existing dialogue and
requirement metadata. A running deadline does not imply issuance. Reconnect
restores durable provenance and never reissues a consumed item.

### Persistence and restrictions

`character_mission_item` identifies the character, mission, assignment, item
key, concrete durable item and owned quantity, and records its generation.
Each protected stack belongs wholly to one assignment/key. The ledger and
`character_mission_item_receipt` survive removal of the active
`character_mission` row; they cascade only with character deletion. Receipts are
keyed by character, assignment and operation key and retain the generation and
typed payload. An exact replay is a no-op; reusing a key with different content
or generation is rejected. Use stable, distinct operation keys for distinct
assignment operations.

Quest items, normal rewards, loot and explicit consumption share one
transaction-local shadow inventory. Original ownership/slot/count checks run
before planning and flushing; final inventory and interaction checks run after
writes and before commit. Runtime state and packets receive the final inventory
once. An issue followed by full consumption in one transaction creates no
visible intermediate item. Rollback releases staged entities and leaves
inventory and receipts unchanged. A still-valid dialogue can retry a failed
write.

Each validation phase batch-reads persisted items and provenance, then checks
indexed results. The final phase reads again after writes, including removed
item IDs and ownership outside the current character. It also checks for
orphaned assignment rows; earlier reads are not reused as commit validation.

Assignment items cannot be sold, traded, auctioned, banked, deposited in clan
storage, equipped outside personal inventory, destroyed or merged through
ordinary mutation paths. All personal-category moves and swaps are
transactional and update both items' runtime slot metadata after commit.
Normal rewards and loot cannot merge into protected stacks. Server handlers and durable
inventory/item repositories enforce these rules; they do not depend on UI
flags or the presence of a runtime marker alone.

Reloads and ordinary ability costs skip assignment-owned stacks even when a
matching unbound stack comes later in the inventory. A missing runtime marker
can be restored from validated durable ownership after commit; a conflicting
marker or ownership change during the transaction still rejects. Clone-credit
redemption rejects assignment ownership and commits the ordinary item cost
and credit increment together before publishing either change.

### Bootcamp seed data

`ConsolidatedWorldSchema` creates the final action payload schema.
`SeedWorldContent` installs the fixed `BootcampMissionItemsV7` data through
`WorldContentDataV1`. Schema and data are separate paired migrations, so all
constraints exist before seeding. `ItemIntentJson` is fluent-mapped: adding a
reflected `ColumnAttribute` would change fixed preloader row widths.

For mission `1995`, objective-3 Continue issues one template `11519`; its
existing objective activation starts the 600-second deadline in that same
transaction. The planting transition consumes that assignment's item before
queuing the existing fuse. Timeout and abandonment remove only that attempt's
item. Mission `2005` explicitly issues one new item on acceptance.

`ConsolidatedCharacterSchema` installs the item ledger, receipts and quarantine
table. No migration adopts legacy bombs or reconstructs their receipts.
Runtime item operations and cleanup reject quarantined assignments; unrelated
items must not be adopted or deleted to resolve ambiguity.

Apply the paired World and Char migrations to fresh databases before admitting
players. The five existing Bootcamp missions remain the only enabled
production definitions.

## Player mission conversations

`MissionInteractionPolicy` in Game is the admission boundary for NPC and authored
object conversations. `NpcManager.RequestNpcConverse` opens a
`MissionConversationSession` on the client; acceptance, objective completion,
choice, turn-in and legacy reward callbacks require a matching topic from that session.
Calling a mutation method does not open a conversation.

The session captures the character and target references, entity IDs, runtime
map and epoch, NPC package, source owner/scene generation or public lease, and
the topics actually sent. Each topic pins the loaded content revision and, for
an existing mission, its durable assignment ID, generation and stored revision.
Legacy/unversioned assignments retain their existing compatibility behavior;
their exact stored revision is still captured and checked. Opening does not
migrate or replace an assignment.
Repeat offers capture the terminal assignment being replaced and the latest
history identity. An offer opened with no assignment cannot be replayed after
another attempt was accepted, settled and cleared. A stale runtime log cannot
authorize a callback against a newer durable assignment. A genuine NPC/object
reopen hydrates changed attempt identities from a consistent durable snapshot
before classifying topics; the mutation callback itself never performs this
rebinding. Other guards, including coherent inventory and currency state,
remain required.

Mission-scoped source scenes must independently match the current operational
definition's revision. The `legacy`/`unversioned` compatibility applies to the
stored assignment, not to the scene release. Changing either compatible stored
revision to the other while dialogue is open still invalidates that dialogue.

Admission requires a living registered character with no pending logout,
removal or transfer. Ordinary NPCs must be living and interactable. An enabled
authored object needs NPC augmentation; a corpse's dead-body appearance does
not make that object ineligible. Targets must be registered in the same runtime
map, with eligible ownership and current scene/lease state. Shared public NPCs
remain shared.

The server uses finite, inclusive **5-metre origin distance in three dimensions**,
not the generic usable-object 20-metre limit. Native
`shared/gameconstants.pyo` defines `MAX_CONVERSATION_RANGE = 5`, and
`Actor.IsInConversationRange` passes it to `body.InRadiusOf`. Server entities do
not expose the native body bounds, so body-distance acceptance can differ at
model edges. Native body-boundary and line-of-sight behavior remain separate
client acceptance checks. There is no authoritative server LOS implementation;
navmesh reachability is not used as a substitute.

Callbacks revalidate the session, source and assignment inside their existing
write transaction before applying mission changes. Their terminal
`ValidateCommitBoundary` check runs after every participant's validation reads
and any required fact reads, including deferred inventory validation. NPC
acceptance and rewards use the same ordering. Ownership and scene/lease reads
are followed by another live session/target check; a reader cannot invalidate a
target that was only checked earlier in the transaction. Becoming eligible after
opening does not add a topic. Abandoning and accepting again does not authorize
an old objective or reward callback. Native requests carry no nonce: identical
input after a legitimately reopened, current conversation cannot be identified
as an old packet.

Transitions without a `DeadlineElapsed` rule do not read deadline state merely
to detect a timer notification. This does not remove the independent dialogue,
planner or final persisted assignment/source guards. The simple non-deadline
lifecycle regression measures eight SELECTs: three character ownership reads,
four assignment reads and one objective/counter aggregate, with one save and
unchanged ordered post-commit packets. It is not a universal budget for item,
requirement or scene-bearing transitions.

A successful mutation consumes the session. A failed durable write can retry
the same still-valid session. Opening another conversation, character
replacement, transfer/logout/disconnect, map detachment, or target removal
invalidates it. Accepted movement outside range also invalidates it; returning
does not restore it.

`MissionConversationTopicKind` distinguishes acceptance, objective completion,
mission completion, legacy reward, objective choice, mission reminder and
ambient objective topics. Reminders and ambient topics cannot authorize mutation.
Older `Success` saves may use the legacy
reward callback against the offered completion presentation for that same
assignment. Trusted typed scene transitions keep their scene authorization and
do not require player dialogue.

### Native dialogue authoring

`MissionSceneDefinition.Dialogue` is optional typed metadata. Each
`MissionDialogueTopicDefinition` supplies a progression `ObjectiveId`,
`NpcPackageId`, `PlayerFlagId` and `Kind`. `DialogObjectiveId`, when present,
overrides only the native text/callback objective ID. The flag ID selects native
text; it does not instruct the server to write a character flag.

| `MissionDialogueKind` | Native `Converse` payload | Transition binding |
| --- | --- | --- |
| `Completion` | `ObjectiveComplete`: list of mission/objective/flag triples | One `TransitionId` |
| `Reminder` | `MissionReminder`: list of mission IDs | None; read-only |
| `Ambient` | `ObjectiveAmbient`: list of mission/objective/flag triples | None; read-only |
| `Choice` | `ObjectiveChoice`: list of mission/objective/flag triples | `Choices`, mapping native indices to transition IDs |

Typed topics replace the default completion presentation for the same
progression objective/package/flag binding. Other conversation bindings remain
unchanged. Without metadata, normalized `Conversation` triggers retain their
completion behavior; the existing objective `Reminder` discriminator projects
as ambient objective dialogue.

Completion and choice bindings must reference existing executable conversation
transitions on their own objective and NPC package/flag. Each selected transition
must leave the objective completed or failed; a null target state means completed.
Event/counter triggers cannot be repurposed as choice transitions. Missing or
ambiguous transitions, duplicate native topics, unknown kinds, missing NPC
packages and unbound scene sequences fail authoring validation.

The native window has callbacks **1, 2 and 3**, not zero-based indices. Author
all three mappings before exposing a choice topic. Sparse maps are rejected:
the packet contains no option labels or list of available indices, so the
server cannot hide a missing mapping from the native window.

For isolated tests, mission `339`, objective `8`, package `586`, flag `1` has
reviewed native body text `4318` and choice texts `4319/4320/4321`. Given authored
transitions `101/102/103`, its metadata is:

```csharp
scene.Dialogue = new()
{
    new(8, 586, 1, MissionDialogueKind.Choice,
        choices: new Dictionary<int, uint>
        {
            [1] = 101, [2] = 102, [3] = 103
        })
};
```

Those transition IDs are server-authored mappings, not text IDs. Do not enable
mission `339` in production or invent translations from this example. The five
Bootcamp missions remain the only enabled production content.

`PerformNPCChoice` (`497`) accepts exactly
`(ulong npcEntityId, uint missionId, uint objectiveId, uint playerFlagId, int choiceIdx)`.
It uses the same session and transactional transition planner as Continue.
Objective changes, flag writes and `StartScenario`/`ActivateSpawnGroup` inputs persist together before
success publication. Scene inputs use the existing durable inbox and grant
receipts. `GrantReward` actions still declare a turn-in package; immediate scene
grants use `GrantRewardIntent`.

An opened choice captures its immutable mapping as well as the P2 source and
assignment identity. A successful choice consumes the session and sends an
`EndConversation` marker. Repeated or competing callbacks cannot select another
branch; a rolled-back write may retry the same still-valid session.

`ConversePacket` validates every entry and nested payload before writing any
bytes. `ImportantGreering` (the existing enum spelling) is a scalar greeting ID,
`EndConversation` carries a boolean presence marker, and `ForcedByScript` is a
native boolean controlling the client's range-based auto-close behavior.

## Requirements

Bind `MissionRequirement` expressions to `MissionSceneDefinition.Requirement`,
`ObjectiveRequirements[objectiveId]`, or `TurnInRequirement`.

| C# type | Arguments |
| --- | --- |
| `AllRequirements`, `AnyRequirement` | List of expressions |
| `NotRequirement` | One expression |
| `LevelRequirement` | Minimum level |
| `MissionStateRequirement` | Mission ID, optional state, optional accepted flag |
| `FlagRequirement` | Server-owned flag ID and value |
| `CustomRequirement` | Registered pure handler key |

```csharp
var requirement = new AllRequirements(new MissionRequirement[]
{
    new LevelRequirement(4),
    new NotRequirement(new CustomRequirement("character.starting-experience-completed"))
});
```

`MissionStateRequirement.Accepted = true` requires journal presence; otherwise
history can satisfy it. With no explicit state, completion accepts `Success` or
`Completed`. Successful history is lifetime evidence: an active or failed repeat
does not mask it. Explicit `Completed` also checks rewarded history, while
failure outcomes use the current/latest state. `Accepted = true` never uses
archived history. These are mission states, not objective states.

Current custom keys are `example.even-level`,
`account.starting-experience-entitlement`, and
`character.starting-experience-completed`. Handlers declare their required
boolean facts; Game must provide them for both runtime queries and durable
transactions. Unknown handlers/facts fail explicitly. Nesting is bounded to 32.
`character.starting-experience-active` is also available for durable radio-source
requirements; it is not a runtime-cache predicate. Both active and completed
starting-experience facts use the fresh `ReadState` scalar query during durable
validation rather than EF's previously tracked state.

## Persistent character flags

The Char database owns `character_flag(character_id, flag_id, value)`, keyed by
`(character_id, flag_id)` with cascading deletion when the character is deleted.
IDs and values are unsigned 32-bit integers. A missing row means unset;
an explicit zero is a stored value and does not match an unset flag.
Flags belong to the character, not its character-selection slot or account.

Any subsystem can use `ICharUnitOfWork.CharacterFlags`:

| Operation | Meaning |
| --- | --- |
| `Get(characterId)` | Read all flags as an ID/value dictionary |
| `GetValue(characterId, flagId)` | Read a nullable value; null means unset |
| `HasValue(characterId, flagId, value = 1)` | Match a stored value exactly |
| `Set(characterId, flagId, value)` | Insert or update one flag |
| `Remove(characterId, flagId)` | Unset one flag |
| `Add(entry)` | Insert a new row, rejecting a duplicate key |

Mission `SetPlayerFlag` actions write in the same transaction as their objective
transition. NPC dialogue, ordinary progress, failure and abandonment follow the
same rule. `SetCharacterFlagIntent(operationKey, flagId, value)` provides typed
scene writes; a null value removes the flag. Composed scene plans publish the
last flag snapshot in transaction order, not an earlier child plan's snapshot.
Runtime flags change only after commit and are loaded during character selection.
The `Manifestation.PlayerFlags` dictionary is a cache, not a persistence API.
Transactional prerequisites and `FlagRequirement` checks use the database.

Composed mission state follows the same final-state rule. Transaction
finalization freezes each touched assignment's full objective states, both
counter sets, mission state and completable state from the tracked aggregates
after all character intents have been planned. The scene converges that state
before publishing flags or child packets. Child progress/failure plans do not
reapply intermediate state, while their packet order is retained. Full snapshots
also preserve committed successor counters when a competing client was stale;
neither a post-commit database reread nor reconnect is needed for immediate
turn-in. Publication still checks the exact assignment, generation and revision.

Normal mission flag IDs use `1..2147483647`. IDs `2147483648..4294967295` are
reserved for named server state declared in `CharacterFlagIds`; mission
`SetPlayerFlag` actions cannot write that range. `BootcampComplete` is
`0x80000001` with value `1`. Bootcamp completion/skip and requirement facts now
use that flag instead of `character_qualification`.

The old qualification enum and `SetQualificationIntent` remain only to decode
frozen content and historical scene intents; their adapter writes the new flag.
There is no current qualification entity, repository or table. Mission history,
reward receipts and `character_starting_experience` remain separate records.

### Native flag projection

`PlayerFlags` (opcode `710`) takes one collection of flag IDs, not a bitmask.
The native `Recv_PlayerFlags(playerFlagIds)` replaces its collection;
`HasPlayerFlag(id)` tests membership. `PlayerFlagsPacket` writes
`tuple(1) -> list(count) -> uint IDs` and requires an explicit collection.
An empty list clears native membership.

Game's `CharacterFlagProjection` selects IDs accepted by
`CharacterFlagIds.IsMissionFlag(id)` whose stored values are nonzero, in ascending
ID order. It sends neither numeric values nor reserved server IDs such as
`BootcampComplete`. Stored zero and unset both project to absence without
changing their distinct database meanings. Any nonzero value grants native
membership, while server-side requirements still compare the numeric value.

Owner introduction and map reintroduction use the committed
`Manifestation.PlayerFlags` cache. Introductions to another player contain an
empty list, never the introduced character's private flags.

NPC objectives, ordinary progress, failure/abandonment, scene intents and
starting-experience updates converge the cache after their transaction commits.
Changed native membership produces a full owner-only replacement snapshot;
nonzero-to-nonzero changes do not resend an identical set. A composite plan
applies its final flag snapshot once, and child plans cannot restore earlier
writes. NPC flags converge before follow-up scenarios can commit newer changes.

If publication fails, the owning connection retains a dirty snapshot. The map
tick retries the latest committed cache; a successful owner introduction also
satisfies the pending publication. Recovery does not reread database flags or
replay rewards. Pending state is bound to the connection, manifestation and
character ID, and is not sent while disconnected or transferring. A fresh
login reloads committed flags through normal character selection.

## Scene definitions

`MissionSceneDefinition` contains `Script`, `StateVersion`, role-keyed `Actors`,
named `Routes`, numeric `Sequences`, named sequence entry points, eligibility
requirements, `Credit`, optional `Audio`, and an optional `PublicEncounter`.
Optional `Dialogue` supplies the native topic and branch bindings described above.

Scripts receive only `SceneContext` and `SceneObservation`, not clients or EF
contexts. Decisions contain typed intents, signals, timers and JSON checkpoints.
Internal database/checkpoint JSON does not introduce a file-based publish step.

### Mission audio

`MissionAudioDefinition` is available to any mission, including main-world
quests and data-only missions. Author it on the mission's scene definition
(a metadata-only scene may omit `Script`):

- `OfferAudioSetId`: native briefing narration in the existing offer tuple.
- `Events`: optional `MissionAudioEvent.Accepted` and `Completed` voice cues.
- `Announcements`: greeting-text ID to audio-set ID, paired with authored
  ambient mission announcements.

Use verified client audio-set IDs, not sound-file IDs. Omit audio metadata for
a silent mission; zero or invalid bindings are rejected. Do not bind the same
briefing to both offer and acceptance unless a repeated readout is intended.
The Game adapter uses the native offer slot or existing voice-over RPC
(`PlayTutorialAudio`, a historical wire name that is not restricted to tutorial
zones). There are no Bootcamp-ID checks in that playback module.
Scene scripts can also use the existing `PresentationIntent` with
`PresentationKind.Audio` for explicitly timed cues.

| `SceneActorKind` | `TemplateId` means |
| --- | --- |
| `PublicSpawn` | Exact existing static spawn-pool ID |
| `Creature` | Creature definition ID |
| `Object`, `PracticeTarget` | Entity class ID |

Role dictionary keys must equal `SceneActorDefinition.Role`. Created actors need
`ScenePosition(X, Y, Z)`, with Y as height. Other fields include `Orientation`,
`InitiallyInteractable`, `InitialObjectState`, `WindupMilliseconds`, loot
bindings and mission/group/spawn attribution.

An object actor can have an optional `SceneObjectConversation` binding:
`MissionId` and `ObjectiveId` identify the progression target;
`NpcPackageId`, `DialogObjectiveId` and `PlayerFlagId` select an existing native
objective dialogue. Its entity class must already support the client's NPC
augmentation. The adapter publishes NPC metadata and opens `Converse` on that
same object, without adding a creature or sending unsupported Usable packets.
`Kind` defaults to `Completion`; other kinds require an exactly matching typed
mission dialogue topic. In particular, `Kind = Choice` uses that topic's authored
index-to-transition map rather than a separate object choice system.
The client must open the dialog before Continue, remain near the same object
in the same instance, and retain the same mission assignment. Completion
uses the existing transactional progress planner, restricted to the bound
objective. Opening a dialog never grants inventory or advances progress.

`SharedKey` is for private experience-owned actors. It does not make personal
copies of main-world NPCs. Keep the role consistent between mission and
experience definitions.

`SceneRoute` contains ordered `SceneWaypoint` records, speed in metres/second
and optional `ResumeAtDestination`. Waypoint orientation is radians; pauses are
milliseconds. Scripted movement needs a complete loaded-navmesh route.
`RunRouteIntent.ResumeAfterCombat` lets a combat-capable actor pause its route
while fighting and resume it afterward. Removing that actor cancels its route.

Optional `MissionSceneDefinition.DefeatSequences` maps actor roles to sequence
IDs. A confirmed defeat persists the actor outcome and queues that sequence in
one transaction, even when no player receives kill rewards. The normal durable
scene inbox delivers it once and retries failed scene writes. Scripts can track
the defeated roles in their checkpoint to gate a finite encounter; losing a
route or despawning an actor is not a confirmed defeat.

| World intent | Purpose |
| --- | --- |
| `EnsureActorIntent` | Ensure the authored actor is present |
| `RemoveActorIntent` | Remove/release its role |
| `SetInteractionIntent` | Set enabled/object-state properties |
| `RunRouteIntent` | Run an authored route |
| `AttackActorIntent` | Engage a living hostile actor in the same map; omitted `TargetRole` means the run's owner |
| `FollowActorIntent` | Follow a character; ID zero means owner |
| `PresentationIntent` | Tutorial, audio or greeting using an existing ID |
| `TransferIntent` | Transfer to a map and authored position |
| `RestoreActorPoseIntent` | Recover a static actor in its owning private map |
| `TransitionObjectStateIntent` | Send the native usable transition to an object state, with an optional windup in milliseconds |

Character intents are `GrantRewardIntent`, `GrantAbilityIntent`,
`SetCharacterFlagIntent`, `SetEntitlementIntent`, `ObjectiveIntent`, and
`MissionDeadlineIntent`. `SetQualificationIntent` is retained for historical
content compatibility. Game applies them through its transaction adapters.

Operation keys are nonempty, at most 96 characters and unique across a scene's
authored sequences. Timers have names of at most 64 characters, a target sequence
and `WallClock` or `ActiveScene` policy. One decision permits at most 64 world
intents, character intents and signals combined. A checkpoint is a JSON object
limited to 16,384 UTF-8 bytes.

### Actor gameplay policies

`SceneActorDefinition.GameplayPolicy` is an optional `ActorGameplayPolicy` for
`Creature` and `PublicSpawn` roles, in public or private scenes. It is invalid
on objects and practice targets. Absence is serialized as no field, including
with the default serializer used by older data helpers.

Resolution uses the current role policy on the exact actor first, then the
existing private-experience policy where applicable, then ordinary behavior.
Role policies are complete overrides, not field-by-field merges.

| Field | Default and contract |
| --- | --- |
| `Invulnerable` | `false`; blocks normal direct and missile damage and kill handling |
| `DefenseRadius` | `0`; finite and nonnegative, in metres; positive values enable defense |
| `DefenseTargetTag` | Absent; required when defense is enabled |
| `Tags` | Empty list; nonempty, unique, case-sensitive tokens without whitespace |
| `RewardScenarioKills` | `false`; explicit kill-reward eligibility for this role, including leased static actors |
| `TrackParticipation` | `false`; tracks eligible damage participation for normal defender final-blow credit |
| `Loot` | Absent; optional `AuthoredLootProfile`, used by the existing corpse-loot path when the kill is eligible |

| Actor/policy source | Reward behavior |
| --- | --- |
| Ordinary static actor, no role policy | Normal rewards, including the existing private-map behavior |
| Scene-created actor, no role or applicable experience policy | No kill rewards |
| Explicit role policy | Its `RewardScenarioKills`, even when `false` or when the actor is a leased static spawn |
| Private scene-created actor with no role policy and an experience policy | The experience policy's reward setting |

Enabling rewards does not override faction or killer eligibility. Eligible
kills use existing XP, adrenaline, harvest and corpse-loot logic. Group mission
credit still follows `MissionCreditPolicy`; XP and corpse ownership are not
redistributed to the party.

`AuthoredLootProfile` snapshots its `LootDrop` list. A drop has a nonzero
`TemplateId`, `ChancePercent` in 0-100, and inclusive positive `Minimum` and
`Maximum` quantities (`Maximum < int.MaxValue`). Catalog loading rejects
missing item-template/class references and quantities exceeding the class's
stack size. Scene bindings, experience catalogs and public lease bindings
copy tag lists into immutable policy snapshots.

Definitions using the same private `SharedKey` must agree on kind, template and
policy, including whether the policy is absent. Tag order is not significant.
A shared key may identify only one role in each scene.

Game installs the snapshot after the owning transaction commits, before actor
publication. The binding checks the actor object, template, spawn ownership, run/role,
generation and map epoch. Death captures the policy needed for that kill's
loot, then clears the live override. Removal, reset/return and lease release
discard old overrides; a replacement or later lease cannot inherit them.
`Wait`/`Continue` reconnects retain the same public actors and policy snapshots.
Private reconstruction reapplies its role binding; releasing one reference to
a private shared actor preserves the policy of a remaining scene reference.
Public scene termination removes live actors and their spawn pools, but keeps
already-earned corpse loot on the normal corpse timer. This does not retain
the dead actor's role policy or make it available to a later lease. Explicit
actor-removal intents and private-map cleanup retain their existing behavior.

## Public encounters and private experiences

`PublicEncounterBinding` identifies mission, spawn, role, script and owner-loss
policy (`Reset`, `Wait`, `Continue`). Main-world NPCs remain public; admission
reserves the existing spawn. Deliberate initiator abandonment cancels its run.
`AllowPartyJoin` explicitly permits later shared assignments to join that run;
it defaults to false and is independent of `IncludeEligibleParty`, which
captures already-assigned eligible members at reservation time. Neither option
automatically accepts a mission for anyone.

`MissionCreditPolicy` supports `Personal`, `NearbyParty` and
`EncounterParticipants`. Shared modes require a positive finite radius and
matching active objectives; participant mode also requires participation.
Only eligible creature-kill/scenario-event objectives share credit, not personal
dialogue, inventory actions, acceptance or reward choices.

`MissionExperienceDefinition` has a key, revision, map context, private-per-character
flag, scene, mission triggers and actor policies. Triggers respond to `Accepted`,
`Rewarded`, `Completeable` or `Departing` for a mission and name an experience
sequence. `Completeable` is also derived from current active assignments during
reconnect; `Departing` is emitted by the authorized starting-experience boarding
flow. Each trigger must reference a loaded mission and a declared sequence.

Experience-level `ActorPolicies` remain a private-map fallback; public roles
use the actor metadata above rather than installing a map/template override.
The fixed C# Bootcamp example is
[BootcampMissionDataV1.Experience.cs](../src/Rasa.DBL/Services/Preloader/Missions/BootcampMissionDataV1.Experience.cs).
The composable helper
[BootcampFinaleDataV2.cs](../src/Rasa.DBL/Services/Preloader/Missions/BootcampFinaleDataV2.cs)
supplies finale presentation and experience bindings.
[BootcampAudioAndCreditsV3.cs](../src/Rasa.DBL/Services/Preloader/Missions/BootcampAudioAndCreditsV3.cs)
supplies native audio bindings and the configured credit rewards.
[BootcampExtractionDataV5.cs](../src/Rasa.DBL/Services/Preloader/Missions/BootcampExtractionDataV5.cs)
authors a finite assault, allied defenders and defeat-sequence bindings.
`SeedWorldContent` applies these fixed helpers in dependency order.
The mission-local extraction script keeps its additive checkpoint compatible
with earlier sequence-only saves; the generic mission manager has no
Bootcamp-specific enemy counters or spawn coordinates.

## Migration helper interface

All helpers take a `MigrationBuilder`. Provider migration classes can share
these C# operations:

| Method | Operation |
| --- | --- |
| `EnableMission` | Set the selected definition's enabled flag |
| `InsertScene`, `UpdateScene` | Write a typed scene for a mission/revision |
| `InsertExperience`, `UpdateExperience` | Write a typed private experience and enabled flag |

Use ordinary EF data operations for normalized rows. Keep migration-owned data
fixed, add new migrations for changes, and scaffold provider schema changes
with EF rather than hand-editing generated snapshots.
