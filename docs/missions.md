# Mission authoring and operations

Mission content is deployed by **EF Core database migrations**, just like the
other World data. There is no mission pack, publication command, release
activation step, or separate database path to provide to another tool.

The consolidated mission baseline targets **fresh databases**. Databases
containing its removed intermediate migration IDs are not supported upgrade
sources, and experimental mission saves are not converted. The subsequent
Wilderness rollout supports fresh merged databases and existing PR105 databases.
Its 17 provider pairs run after PR105's `20261103000000` World boundary, using
the common `20261104000000..20261104001600` sequence. The earlier September
Wilderness IDs were unshipped and their disposable databases are not an upgrade
source. Database files are never deleted automatically.

After the preserved `development` history, each provider's baseline has one
Char schema migration and two World migrations: `ConsolidatedCharacterSchema`,
`ConsolidatedWorldSchema`, and `SeedWorldContent`. The schemas create assignment
items, per-attempt history, forwarding provenance, radio authority and nullable
party-source identity directly. There are no intermediate save backfills.
See [the migration layout](setup.md#mission-data-migrations) and
[quest-item persistence](mission-reference.md#assignment-owned-quest-items).

`SeedWorldContent` enables exactly the five protected Bootcamp missions.
Initiation's arrival offer uses bounded radio authority while retaining its NPC
path and NPC-only completion. All Bootcamp missions remain Once, private and
unshareable. Later paired World migrations add the reconciled **64 outdoor
Wilderness missions**, including mutually exclusive branches and the partial
Targets of Opportunity assignment, for **69 enabled definitions** in total.
Native UI and live MySQL behavior need separate acceptance checks.

## Start the servers

With SQLite selected in database configuration, run Auth and Game normally,
including from Visual Studio. The existing initialization flow creates each
database and applies its pending migrations. World migrations install complete
Bootcamp mission definitions, scene bindings and the private experience before
Game validates mission readiness. A second startup applies nothing already
recorded in `__EFMigrationsHistory`.

With MySQL, apply the normal migrations explicitly before starting the matching
server build:

```powershell
dotnet ef database update --project .\src\Rasa.DBL --startup-project .\src\Rasa.Game --context MySqlAuthContext
dotnet ef database update --project .\src\Rasa.DBL --startup-project .\src\Rasa.Game --context MySqlCharContext
dotnet ef database update --project .\src\Rasa.DBL --startup-project .\src\Rasa.Game --context MySqlWorldContext
```

Game does not migrate MySQL automatically. Mission data and a required C# script
can ship together as one code/database release. World and Char are separate
databases and their migrations are not one distributed transaction; finish the
required updates before admitting players.

See [setup](setup.md#database-configuration) for provider configuration and
[Docker setup](docker_setup.md) for container paths.

## Where the code belongs

| Module | Responsibility |
| --- | --- |
| `Rasa.Missions` | Independent objective rules, requirements, typed scripts, scene decisions and shared content definitions |
| `Rasa.DBL` | Schema, normalized content rows, fixed migration data and migration helpers |
| `Rasa.Game\Missions` | Loading migrated content, transactions, protocol projection, scene execution, world adapters and group credit |

Do not add a branch for each new mission to `MissionApplication`. Most missions
need data only. Unusual behavior belongs in a small registered C# script using
the existing typed scene interface.

Current Bootcamp scene data lives in
`src\Rasa.DBL\Services\Preloader\Missions\BootcampMissionDataV1.*.cs`.
The older C# World preloaders supply the normalized objective, reward and world
rows; `SeedWorldContent` calls `WorldContentDataV1` to install those definitions,
the current scene bindings and their supporting World data.
`MissionDataMigration` provides typed helpers for inserting/updating scene and
experience bindings and enabling a completed mission definition.

Wilderness helpers live under the corresponding `Missions\Wilderness`
directory. Their providers apply the opening, Alia branches, Eloh/Pinhole,
Landing Zone, Twin Pillars, Ranja Gorge and Daghda's Urn in dependency order.
Shared World corrections precede their consumers. `WildernessSupportedRewards`
is a forward, exact-key correction to eight existing reward rows; it does not
rewrite already-applied helpers or character inventory. Twin Pillars and Ranja
Gorge author the same supported medpack policy before their activation.
Daghda's Urn installs Skeev's World data and its manual-combat scene binding
together. See the [coverage and reconstruction ledger](wilderness-missions.md).

Mission-authored World creature, spawn-pool and attack identities use
`630001..630199`, separate from PR105's Divide rows. Native mission, class and
item IDs are unchanged, as are Char outcome flags `530002` and `530003`.
PR105's class-sourced armor rows are authoritative; the retained
`WildernessRewardEquipment` marker has no Up or Down data operations.

Evidence notes use `TEXT`, not the baseline's `varchar(256)`. The unshipped W2
provider wrappers widen this column before calling the helper that inserts the
first long notes. The later `WildernessEvidenceCapacity` pair retains the
additive capacity boundary.
This additive compatibility change retains capacity on rollback rather than
truncating surviving notes.

The [data and script reference](mission-reference.md) explains the fields and
ID namespaces. Ordinary and escort examples are C# fixtures in
`src\Rasa.Test\Missions\Content\MissionAuthoringExampleData.cs`.

## Add a mission

1. Identify the native mission, objective, text, NPC-package and asset IDs.
   Do not invent client text IDs or treat an opcode declaration as proof that
   the client supports the desired interaction.
2. Add a **data-only** migration for SQLite and MySQL. Keep schema changes in
   separate migrations. Both provider migrations should call the same fixed
   C# data helper where their operations are equivalent.
3. Insert the normalized definition, objectives, transitions, triggers, actions
   and any reward, area, indicator or spawn rows. Keep their `MissionId` and
   `ContentRevision` consistent. Normal EF `InsertData`, `UpdateData` and
   `DeleteData` operations are appropriate.
4. If scripting is needed, construct a `MissionSceneDefinition` in C# and insert
   it with `MissionDataMigration.InsertScene`. Declare actors, routes and named
   sequences explicitly; merely declaring an actor does not spawn it.
5. Enable the completed definition with
   `MissionDataMigration.EnableMission(migration, missionId, revision)`.
   Unreconstructed legacy definitions stay disabled. At most one revision of a
   mission may be enabled.
6. Exercise acceptance, progress, reward, failure, abandonment and reconnect
   through the real Game entry points. Verify the native client separately.

An enabled mission is not exempt from validation. Game checks normalized
contracts, script/state versions, requirement handlers, actor/route bindings,
sequence targets and operation-key constraints before use.

## Change mission data

Add a new migration rather than changing a migration that has already been
applied. Migration-owned C# data must also remain fixed: do not make an old
migration call a mutable "latest mission" factory or read loose files from the
working directory.

For example, a data migration can adjust an existing indicator's radius:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions
{
    internal static class AdjustBombIndicatorV2
    {
        internal static void Up(MigrationBuilder migration) =>
            migration.UpdateData(
                "mission_indicator",
                new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
                new object[] { 1995U, "deployment_11", 3U, 436U },
                "radius", 10.0);

        internal static void Down(MigrationBuilder migration) =>
            migration.UpdateData(
                "mission_indicator",
                new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
                new object[] { 1995U, "deployment_11", 3U, 436U },
                "radius", 8.0);
    }
}
```

This is an illustrative change, not another required Bootcamp migration. The
provider migration classes delegate their `Up`/`Down` methods to the helper.

Scene bindings use `InsertScene` or `UpdateScene`; private experiences use
`InsertExperience` or `UpdateExperience`. Those helpers serialize the typed C#
definitions into their database columns. That internal serialization is not a
JSON authoring or deployment workflow.

When moving an actor, also update relevant indicators and areas. A private
experience-owned role must agree between the mission and experience definitions.
Validate its complete footprint and route against the intended surface, especially
at bridges and other stacked geometry; finding a nearby navmesh polygon is not
proof of visible, reachable placement.

## Opt in to repeatable missions

Missing repeat metadata means `Once`. Leave all five Bootcamp missions at that
default, private and unshareable. Author repeats in an ordinary paired forward
World data migration by inserting a `mission_repeat_policy` child row for the
exact mission ID and content revision. No scene script is required.

For example, a Daily mission with a 06:00 UTC reset uses:

```csharp
migration.InsertData(
    "mission_repeat_policy",
    new[] { "mission_id", "content_revision", "repeat_kind", "cooldown_seconds", "reset_second_utc" },
    new object[] { missionId, revision, (int)MissionRepeatKind.Daily, null, 21600U });
```

`Immediate` has no timing fields. `Cooldown` requires a positive number of
seconds and no reset field. `Daily` requires a reset second between 0 and 86399
and no cooldown field. Mixed or unsupported policies fail validation; the
database also enforces these parameter combinations.

An eligible acceptance replaces a terminal journal entry in the same character
transaction. It creates a new assignment ID, fresh objectives and counters,
and new item/scene ownership. It does not reset persistent flags or copy prior
progress or choices. Existing exclusive public actors must finish returning
and release their old lease before another run can reserve them.
The client receives the old journal's clear followed by the new mission gain,
only after commit. `Once` retains its existing failure-dismissal behavior;
Bootcamp's failed retry does not become an automatic new offer.

Cooldown starts at the committed reward timestamp. A Daily reward uses the
window containing its commit, even when the attempt began before reset.
Neither failure nor abandonment spends an entitlement. An active assignment
or unrewarded `Success` blocks another attempt; settle the latter at its
receiver rather than clearing it. See the
[repeatability reference](mission-reference.md#repeat-policies-and-attempt-history)
for persistence, lifetime prerequisites and retry semantics.

## Author a radio mission

Add a `mission_channel_policy` row for the exact mission/revision in a paired
World data migration. Acceptance and completion independently use
`MissionChannel.Npc`, `Radio` or `Mixed`. Missing policy means NPC-only on both
sides. Set `GiverId` or `ReceiverId` to `null` when that side is radio-only;
there is no need for a dummy NPC. The CLR zero defaults remain solely for
historical migration seed compatibility.

For example, after defining the mission and its rewards:

```csharp
var sources = new[]
{
    new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "area.arrival", mapContextId)
};
migration.InsertData(
    "mission_channel_policy",
    new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
    new object[] { missionId, revision, (int)MissionChannel.Radio, (int)MissionChannel.Radio,
        System.Text.Json.JsonSerializer.Serialize(sources) });
```

The trusted server event calls
`missions.Offers.TryOffer(client, missionId, MissionOfferSourceIdentity.ServerEvent("area.arrival"))`.
Use a stable event identity for retries. A submitted native mission ID is not
authority. The service verifies the authored source, ownership, requirements
and repeat eligibility, persists the offer, then sends the native dialog.
Acceptance consumes that offer in the existing assignment/item transaction.

A scene can instead declare a `Scene` source keyed by its script and emit
`new OfferRadioMissionIntent(operationKey, missionId)`. The adapter captures
the real originating scene and assignment generations; scripts do not supply
them or fabricate an NPC. The source must remain Running/Waiting with its
current assignment and active participant. Replaying a sequence inbox entry
still does nothing; a new trusted scene signal may reissue an eligible offer
after reconnect. Other character-operation receipts remain unchanged.

For an optional follow-up, use
`new OfferRadioMissionIntent(operationKey, missionId, IfEligible: true)`.
Already assigned/completed targets and ordinary eligibility or capacity limits
then produce a logged no-op instead of failing the source scene's recovery.
Invalid source identity still fails. A consumed offer remains stored while its
exact target assignment is active or awaiting reward, preserving the original
source-assignment proof for concurrent delivery/choice missions.

Offers last five minutes, with one pending slot per character/mission and at
most 30 per character. Identical retries do not notify again or renew expiry.
Session/map/character changes invalidate old offers. A still-eligible source
must issue a fresh offer; reconnect does not turn old native input into authority.

Radio completion uses the shared turn-in/reward planner and the current exact
assignment. Author the completion channel explicitly; the old
`RadioCompleteable` database boolean no longer enables it. Non-null ratings
are unsupported. Test the native UI separately from packet/server tests.
See [radio authority contracts](mission-reference.md#radio-offer-authority)
for identity, retention and commit-boundary rules.

## Author a party-shareable mission

Set the normalized definition's `Shareable` flag only when the mission supports
independent recipient assignments. NPC/radio acceptance and completion channels
stay separate. Each recipient must explicitly accept a durable offer from a
living party member within the native 20-unit range, on the same live instance.
Do not use account IDs as native source entities.

An ordinary mission without a scene needs no additional sharing source metadata.
A scripted public encounter must opt in through its existing binding:

```csharp
scene.PublicEncounter = new PublicEncounterBinding(
    missionId, spawnId, "guide", scene.Script,
    OwnerLossPolicy: "Reset", AllowPartyJoin: true);
```

This attaches new assignments to the exact existing actor/run. It does not
reserve another actor or replay scene startup. Keep assignment-owned deadlines
and `StartScenario`/`ActivateSpawnGroup` objective actions nonshareable; those
paths need a separate scene controller. Content validation rejects an
incompatible shareable scene rather than exposing a broken native button.
Required `Personal` scenario-event objectives also make a scripted mission
unshareable: joined assignments cannot receive initiator-only scene signals.
An optional personal signal is also unsafe when required progress depends on
its reveal/activate actions, including chains through other optional objectives,
or observes its objective state. Keep those dependencies nonshareable even if
another authored branch could unlock the same objective; validation does not
prove alternate branches equivalent. Independent optional signals and personal
dialogue/actions remain supported, including dialogue that independently
activates required objectives. A redundant unlock of an initially active
objective does not create a dependency. Author group credit explicitly only
when future scene events should be available to eligible participants.

Use `NearbyParty` or `EncounterParticipants` objective credit only for eligible
future kills/scenario signals. Personal dialogue, flags, choices, items and
rewards remain per character. A participant cannot control or cancel the
initiator's world operations, and late joiners receive no prior progress.
`IncludeEligibleParty` remains the separate option for already-assigned members
present when an encounter is reserved; it never accepts a mission.
Encounter membership belongs to one assignment and generation. It cannot
authorize another mission's encounter-only objectives or prevent an independent
`Personal`/`NearbyParty` kill mission from receiving its own eligible credit.

Run the [party sharing checks](protocol-testing.md#explicit-party-mission-sharing)
before enabling authored content. Bootcamp stays private, Once and unshareable;
the sharing implementation does not activate Wilderness missions.

## Write a scripted mission

Implement `ISceneScript` in `Rasa.Missions` and register it with
`[MissionScript("your.script.key", 1)]`. Its interface receives a `SceneContext`
and typed `SceneObservation`, and returns a `SceneDecision`.

Scripts have no Game clients, EF contexts or direct world mutation. Return
world intents, character intents, signals and timers; Game owns their
transaction and publication behavior. Use shared actor roles and named routes
rather than parsing runtime entity IDs or legacy scenario-key strings.

`MissionSceneDefinition` contains:

- `Script` and `StateVersion`, identifying the registered implementation.
- `Actors`, `Routes`, `Sequences` and `Names`, defining its authored scene.
- Optional `DefeatSequences`, mapping actor roles to durable sequence inputs
  after confirmed deaths, independent of player kill-reward eligibility.
- `Requirement`, `ObjectiveRequirements` and `TurnInRequirement`, binding
  admission, progress and turn-in eligibility.
- `Credit` and optional `PublicEncounter`, controlling eligible group credit
  and public actor reservation.
- Optional `Audio`, binding briefing narration, accepted/completed voice cues
  and audio paired with mission announcements. This is shared by all missions,
  not restricted to Bootcamp.
- Optional `Items` and `AcceptanceItems`, binding temporary quest items and
  explicit acceptance-time item actions. Ordinary transition item actions use
  `MissionActionEntry.ItemIntentJson`; scene-only item operations belong in the
  sequence's `Character` list.

Use `data.sequence` for ordinary authored sequences. Bootcamp scripts and
`example.escort` show how to add unusual behavior without enlarging the
mission manager.
`BootcampExtractionScene` is an example of counting a finite assault in the
script checkpoint, waiting for both ship arrival and enemy defeats, then
unlocking an NPC objective. Enemy movement and combat use shared world intents.

Recovery is defined by the script checkpoint, route resume settings and
public encounter policy. There is no generic `Recovery` string that dispatches
an automatic recovery strategy.

### Bind native object actions

For a usable object, set
`UseAction: new SceneObjectAction(missionId, objectiveId, sequenceId, actionArgId)`.
Use the native argument, such as `3` for Surveyor Unit class `7827`, rather than
the usual `1`. For a damageable object, set
`Destruction: new SceneObjectDestruction(missionId, objectiveId, sequenceId, hitPoints, destroyedState)`.
Real weapon/ability damage must reach zero HP before its sequence runs. Preserve
the native destroyed state: ordinary inert objects use `2`; the Bane forcefield
uses `196 -> 199` and has no native use callback.

Both inputs require the exact live actor role, owner, assignment, generation
and eligible objective. Their sequence can combine inventory costs, grants and
progress in one transaction. A full inventory leaves the action retryable.
Do not replace real destruction with class-wide hit counts, or make a usable
prop into a creature just to receive a kill event.

For a finite set of objects sharing one native counter, author one scoped
`ScenarioEvent` trigger per distinct event ID, a common scenario ID in
`CounterId`, `InitialValue = 0`, and `TargetValue` equal to the set size.
Each source sequence emits its own `SceneMissionSignal`. Assignment-generation
receipts preserve distinctness and publish counter `0` through reconnect.

### Give an actor role a gameplay policy

Set optional `SceneActorDefinition.GameplayPolicy` on a `Creature` or
`PublicSpawn` role. Public scenes do not need a private experience to configure
combat or kill rewards:

```csharp
scene.Actors["hostile"] = new SceneActorDefinition(
    "hostile", SceneActorKind.Creature, 510210,
    new ScenePosition(0, 0, 0),
    GameplayPolicy: new ActorGameplayPolicy
    {
        RewardScenarioKills = true,
        TrackParticipation = true,
        Tags = new[] { "encounter-hostile" },
        Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 12, 12) })
    });
```

Replace the example template, position and loot IDs with verified content.
This declaration does not enable a mission or activate a map. For a
`PublicSpawn`, `TemplateId` is the existing spawn-pool ID, and the public
encounter must reserve that same role.

A role policy replaces the entire applicable private-experience policy; its
unset fields use `ActorGameplayPolicy` defaults rather than merging with the
experience. `RewardScenarioKills = false` explicitly suppresses kill rewards,
including for a leased static actor. Set it to `true` when adding tags or
defense to a role that should still reward eligible kills. Without a role policy,
ordinary static spawns retain normal rewards; the legacy experience reward flag
continues to apply to scene-created actors only. Scene-created actors with
neither role nor experience opt-in remain nonrewarding.
Keep policies identical wherever a private `SharedKey` names the same actor.

Game snapshots the policy at binding, applies it to the exact actor and
scene/lease generation, and clears it on release or replacement. Do not mutate
a creature template or a caller-owned tag list to change a running encounter.
Use a forward content migration for authored changes; omitted policy metadata
does not add a field to historical seed serialization.

## Private experiences and public encounters

Bootcamp remains a separate private map per character. Its
`MissionExperienceDefinition` owns actors that must survive individual mission
completion, such as Youngblood. `SharedKey` means sharing across that character's
experience, not creating personal copies of public main-world NPCs.

Main-world actors remain public. An escort reserves the exact static spawn
before the assignment is accepted. Competing starts wait for the authored
return/reset/respawn. Departure policy may be `Reset`, `Wait`, `Continue` or `Fail`;
deliberately abandoning the initiating assignment cancels that run.
`Fail` commits a required-objective failure and item cleanup before releasing
the actor. A persistence failure pauses world work and retries that failure;
it does not release a still-active assignment's actor to another player.
Public actor death is observed even before its first route starts.

Use `PublicEncounterBinding.ManualCombat = true` for a public negotiation that
must not begin as ordinary faction combat. The unleased and reserved actor
remains protected from automatic targeting, direct damage and retaliation.
Only an `AttackActorIntent` from the current lease authorizes combat; cancellation,
reset or release revokes it. Ordinary faction IDs and public actor identity stay
unchanged. Do not simulate this with a fake minion owner or an empty attack list.
An emitted attack retains its exact lease/operation authorization. Opening a
new fight on the same actor cannot make an old, revoked missile valid again.

For new World creatures, supply the `creature_stat` row used by normal spawning.
Setting only `creature.max_hp` does not populate `Attributes.Health`; the legacy
normal-spawn fallback is 100. The fixed Wilderness statistics migration makes
the allocated actors' runtime health match their explicitly reconstructed
World health values without changing unrelated fallback behavior.

Group credit requires each recipient's matching active objective and the
authored eligibility policy. It does not accept missions automatically or copy
another character's progress, dialogue or reward choice.

Valid private-map static actors can be deferred until their normal spawn worker
runs. Such operations remain `Pending`, without a failure diagnostic, and
complete through the normal actor-available notification. Missing or invalid
spawns and unowned public leases remain explicit failures.

## Revisions, persistence and safety

EF migration history controls deployment. `ContentRevision` and script
`StateVersion` still identify saved-state contracts; they are not separately
published release names. Compatible position/text/data corrections can be
ordinary data updates. A later change to objective identities or persisted
checkpoint shape must explicitly handle the corresponding character state.

Keep journal assignments, completion history, reward receipts, deadlines and
scene receipts intact during ordinary gameplay. Clearing a completed journal
entry never erases its reward claim. `Once` remains blocked; an explicitly
repeatable mission can reward only a new, eligible assignment. Inventory and
progress changes commit before success packets.

Assignment item ledgers are not children of the active journal row. Cleanup
runs inside the terminal transaction, before removing that row, and targets
only that assignment's durable item IDs. Normal reward and loot stacks remain
unbound. If an assignment is quarantined, inspect the read-only diagnostic before
preparing a corrective migration:

```sql
SELECT character_id, mission_id, assignment_id, reason
FROM character_mission_item_quarantine;
```

Do not clear a quarantine row, adopt by template alone, or remove all matching
templates as a repair. Reconcile the exact assignment, issue receipt,
scene participant and concrete inventory row first. Deploy both providers'
equivalent migration changes with the server build; a generated MySQL script
does not replace testing against a live MySQL server.

General character flags are stored in `character_flag`; `Manifestation.PlayerFlags`
is their login-restored cache. Mission flag actions must not write only to the
dictionary. Use the owning character transaction so a failed objective/reward
operation also rolls back its flag changes. The generic repository is available
to future reward and door checks; it is not tied to a mission assignment.
See [flag storage and IDs](mission-reference.md#persistent-character-flags).

`ConsolidatedCharacterSchema` creates `character_flag` directly, without an
intermediate `character_qualification` table or a legacy backfill. The
fresh-database restriction applies to the entire consolidated branch history,
not only the original flag/content transition. The operator chooses when to
remove disposable files. There is no automatic reset, database deletion or
mission-pack publishing. MySQL remains manually migrated.

The forward Wilderness migrations preserve an existing PR105 database's Char
assignments, inventory, flags and history. Fresh initialization runs the retained
baseline, all PR105 migrations, then Wilderness in the same order. Validate this
upgrade on a disposable copy; September Wilderness and pre-consolidation
experimental histories are not supported sources. Never use a World `Down`
migration as a live character-save rollback.

Use `AssignmentItemRequirement(missionId, itemKey)` when eligibility requires
actual held stock from an active assignment. `SourceOfferMissionId` additionally
binds it to the consumed radio offer's original assignment/generation. Template
counts, flags and an active mission alone are not ownership proof.
`MissionDialogueTopicDefinition.Requirement` gates an individual native topic;
`SourceCreatureId` disambiguates unrelated NPCs that reuse a native package.

A betrayal transition uses `MissionActionKind.FailRelatedMission` with a typed
`FailRelatedMissionIntent` payload. It fails the other assignment's required
objective and performs that assignment's own cleanup in the originating
transaction. Do not implement this as a later scene callback or relax
cross-assignment item guards. The serialized action payload remains in the
existing `item_intent` column.

## Focused verification

```powershell
dotnet test .\src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionMigrationTests|FullyQualifiedName~MissionAuthoringLocalityTests"
```

The migration checks exercise fresh SQLite initialization, repeat startup,
enabled content, script validation and provider-equivalent seed operations.
`MigrationConsolidationTests` checks schema-only operations, preserved defaults,
and full-row round trips through the retained `development` migration boundary.
`ConsolidatedBaselineRetainsExpectedDatabaseMigrations` pins the preserved
baseline independently of later forward migrations. `WildernessCoverageTests`
checks the exact latest 64-outdoor-plus-five-Bootcamp set, and
`WildernessProgressionAcceptanceTests` upgrades an active W1 assignment through
the retimed providers without changing its identity or earned counters.
`WildernessMigrationTests` additionally covers fresh and PR105-existing World
databases, preserving Divide, rebuilt Wilderness pools, moved bosses and armor
while checking all 69 definitions and the mission-specific `630xxx` bindings. The
content suites retain final objective, reward, scene, item and radio assertions;
they no longer require removed intermediate migration IDs.
Use the affected gameplay suites for the mission being changed, then the
[native-client checklist](world-testing.md#native-client-bootcamp-acceptance-checklist).
Offline MySQL model/SQL checks are not a live MySQL acceptance result.

The published Game executable's `--check-mission-assets` command verifies
navigation files and compiled Bootcamp script bindings without opening databases
or listeners. It no longer searches for mission JSON packs.
