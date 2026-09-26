# Mission authoring and operations

Mission content is deployed by **EF Core database migrations**, just like the
other World data. There is no mission pack, publication command, release
activation step, or separate database path to provide to another tool.

This branch targets **fresh databases**. All branch-added migrations have been
consolidated; databases containing the removed intermediate migration IDs are
not supported upgrade sources. Previously published experimental mission
databases and character saves are not converted. Database files are never
deleted automatically. Use a fresh development database path, or remove your
own disposable databases when you intend to start over.

After the preserved `development` history, each provider has one Char schema
migration and two World migrations: `ConsolidatedCharacterSchema`,
`ConsolidatedWorldSchema`, and `SeedWorldContent`. The schemas create assignment
items, per-attempt history, forwarding provenance, radio authority and nullable
party-source identity directly. There are no intermediate save backfills.
See [the migration layout](setup.md#mission-data-migrations) and
[quest-item persistence](mission-reference.md#assignment-owned-quest-items).

The World seed installs explicit radio channels and bounded offer authority.
Only Initiation gains a radio source: its existing arrival offer now uses the
generic authority. Its NPC path is retained, completion stays NPC-only, and
all Bootcamp missions remain Once, private and unshareable. Wilderness content
remains disabled. The data migration follows the final schema migration for
both providers. Native UI and live MySQL behavior still need separate
acceptance checks.

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
return/reset/respawn. Departure policy may be `Reset`, `Wait` or `Continue`;
deliberately abandoning the initiating assignment cancels that run.

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

## Focused verification

```powershell
dotnet test .\src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionMigrationTests|FullyQualifiedName~MissionAuthoringLocalityTests"
```

The migration checks exercise fresh SQLite initialization, repeat startup,
enabled content, script validation and provider-equivalent seed operations.
`MigrationConsolidationTests` checks schema-only operations, preserved defaults,
and full-row round trips through the retained `development` migration boundary.
`BranchMigrationsAreConsolidatedByDatabase` checks the six-step layout. The
content suites retain final objective, reward, scene, item and radio assertions;
they no longer require removed intermediate migration IDs.
Use the affected gameplay suites for the mission being changed, then the
[native-client checklist](world-testing.md#native-client-bootcamp-acceptance-checklist).
Offline MySQL model/SQL checks are not a live MySQL acceptance result.

The published Game executable's `--check-mission-assets` command verifies
navigation files and compiled Bootcamp script bindings without opening databases
or listeners. It no longer searches for mission JSON packs.
