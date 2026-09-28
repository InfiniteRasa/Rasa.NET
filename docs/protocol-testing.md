# Protocol regression checks

Use these checks when changing connection handling, packet decoding or the legacy
client handshake. They exercise server code without installing the game client
or connecting to a developer database.

## Run the checks

Follow the SDK and dependency setup in [the setup guide](setup.md), then run:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Networking|FullyQualifiedName~Rasa.Test.Protocol|FullyQualifiedName~Rasa.Test.Cryptography"
```

The socket tests bind only to loopback using ephemeral ports. The crypto fixtures
use synthetic credentials and keys. The command does not start the game client
or connect to MySQL.

## Integrated mission acceptance

After focused corrections, run one final solution selection rather than
repeating overlapping phase filters:

```powershell
dotnet build Rasa.NET.sln --configuration Release --no-restore
dotnet test Rasa.NET.sln --configuration Release --no-build --no-restore --list-tests
dotnet test Rasa.NET.sln --configuration Release --no-build --no-restore --logger "trx"
```

Retain discovery output, TRX case/class totals and the actual failures. A
mission-only pass does not turn a failing full solution into a pass; report
unrelated failures separately.

The combined seams include:

| Case | Regression coverage |
| --- | --- |
| Native NPC/object choice commits a flag and assignment-bound item, or neither | `NativeChoiceCommitsFlagAndAssignmentItemAtomicallyAndRestoresBothOnReconnect` |
| Item-only scene rewards and independent required objectives converge the final assignment before child publication and immediate turn-in, in either order | `ItemRewardAndIndependentObjectiveConvergeTheFinalSceneAssignment`, including multiple independent item counters |
| Late inventory readers invalidate NPC/object completion and choices without committing objectives, flags, issued/consumed items or receipts | `ItemDialogueRevalidatesSourceAfterFinalInventoryReads`, including reopened retries and still-valid controls |
| NPC acceptance, turn-in and legacy rewards recheck live authority after inventory, requirement and source readers | `NpcAcceptanceAndRewardRevalidateAfterLateReaders` |
| Public kill pays authored loot/normal XP while personal, nearby-party and exact encounter assignments receive independent credit | `RewardingPublicKillKeepsPersonalPartyAndEncounterCreditAssignmentScoped` |
| Repeat replacement rejects old conversations, items, deliveries and forwarded actor operations | `MissionRepeatabilityTests`, `SceneAssignmentLifecycleTests`, `SceneInputDurabilityTests` |
| Daily reset during the final inventory read rejects the old window and retries once | `DailyResetDuringLateEnlistedInventoryValidationRollsBackAndRetriesInTheNewWindow` |
| Sharing at inclusive 20 units joins one run with fresh state and future-only credit, without world control | `MissionSharingTests`, including optional-to-required owner-only dependency rejection |
| One inventory-add frame fails at the existing socket serialization/encryption boundary; the loopback peer does not receive it, and a fresh client restores the exact durable items/receipt without paying again | `FailedItemPublicationRehydratesCommittedRewardsWithoutASecondGrant` |
| Actual-baseline Char/World forward upgrades preserve existing facts | The two `MissionIntegrationUpgrade...` methods |
| Cold Bootcamp success and timeout/retry both depart one way | `BootcampEndToEndTests`, `BootcampDepartureTests` |

The item-publication fixture injects a one-shot `IOException` through
`LengthedSocket.OnEncrypt` and verifies all other queued frames reach the peer.
A caught inventory runtime-convergence callback error alone is not evidence of
lost publication. This loopback test does not certify native UI behavior.

Check all four Char/World provider models and inspect both forward and
idempotent MySQL scripts. Live MySQL still requires an authorized disposable
database for migration, locking, uniqueness, rollback and competing-retry
acceptance. Offline scripts and SQLite tests cannot substitute for that gate.

## Bootcamp mission and departure packet order

For authoring these behaviors, see [mission authoring](missions.md) and the
[trigger/script reference](mission-reference.md). Packet projection lives in
the Game protocol adapters; mission scripts return intents/signals and must
not write Python tuples or call clients directly.

Bootcamp mission order is covered by `BootcampProtocolTests` and the broader
mission suites. They lock down the client-visible sequence used by the
Deployment 11 starting experience:

- reconnect/login snapshot: `MissionStatusInfoPacket`
- unaccepted Bootcamp arrival: `MissionStatusInfoPacket -> DispenseRadioMissionPacket`
- mission accept: `MissionGainedPacket`
- counter progress: `UpdateObjectiveCounterPacket` before completion packets
- objective progression: `ObjectiveCompletedPacket -> ObjectiveRevealedPacket -> ObjectiveActivatedPacket`
- deadline expiry failure: `ObjectiveFailedPacket -> MissionFailedPacket`
- mission turn-in: one `CompleteNPCMission` request commits completion and rewards,
  then publishes `MissionCompleteablePacket(false) -> MissionCompletedPacket`,
  reward deltas, and `MissionRewardedPacket`
- tutorials: `DisplayPlayerTutorialNotificationPacket -> PlayTutorialAudioPacket`
- first Eloh announcement: `ForceConversePacket` greeting `1634`, which starts the client's native Lightning highlight
- NPC interaction: `ConversePacket` on `RequestNPCConverse`
- map transfer: `PreWonkavatePacket -> WonkavatePacket`

The finale sends `Use` on wreck class `24586` for the native `31 -> 91`
transition, then removes the wreck after its presentation interval.
Manual evacuation uses the normal dropship states, boarding fade, map-load
handshake and arrival flight rather than an immediate map change.
Once the assault and Van check-in are complete, entering the beam starts that
flight directly. Extraction emits neither `EnteredWaypoint` nor
`WaypointGained` for Bootcamp trigger `60`; normal public dropship menus are
unchanged. A saved discovery or stale selection cannot authorize a return trip.

Mission reward previews retain the native fixed-currency/item and selectable-item
tuple. The currency slots are credits and prestige; XP is granted separately
and has no native preview field. The shared projection fills authored rewards
for offers, gains and snapshots without changing reward amounts or granting them.

The third field of the six-field mission offer is `offerVOAudioSetId`, now
populated when the mission authors narration. Absent audio remains `None`.
Accepted/completed and ambient-announcement voices use the existing
`PlayTutorialAudio` client method after successful commits; its wire name does
not impose a tutorial-map restriction. Do not add an extra tuple field or
encode XP as a currency/audio value.

Run the focused suite with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampProtocolTests|FullyQualifiedName~MissionProtocolTests|FullyQualifiedName~MissionProgressTests|FullyQualifiedName~MissionRewardTests|FullyQualifiedName~BootcampDepartureTests"
```

`CompleteMissionRequestClaimsRewardsWithoutAnotherAcceptStep` routes acceptance,
objective completion, NPC conversation, and the decoded completion request.
It covers fixed and selectable rewards, recovery of older unrewarded `Success`
rows, and retries after reconnect. `Accept Mission` only starts a mission;
`Complete Mission` claims its rewards without a second acceptance step.

Mission gains, snapshots, reveals and offers omit unrevealed (`Inactive`)
objectives. The native mission log creates a row for every received objective
and does not hide that state itself. Revealed-but-not-activated (`NotAssigned`)
objectives and completed history remain visible; hidden objectives still exist
in durable server progress. This prevents Initiation from displaying both
"Approach the Eloh Hologram" steps before the second is revealed.

Calling for Reinforcements (`1995`) uses the shipped client objective IDs
`2, 3, 1, 4`. Proximity completes objective `2`; the corpse's Continue action
advances objective `3`. The native conversation lookup only has
`(1995, 2, 2584, 1, COMPLETION)` for this text, so the authored object binding
separates displayed objective `2` from progression objective `3`.
The corpse is class `21081`, with NPC augmentation `52`, and receives
`NPCInfo(2584)` plus `NPCConversationStatus`, not `UsableInfo`.
`RequestNPCConverse` opens `Converse` with its objective-completion tuple;
`CompleteNPCObjective(corpseId, 1995, 2, 1)` is validated against that open
object/assignment before the scoped objective-3 transition commits.

The former loot-only class `24990` has TreasureDispenser augmentation `64`,
not the NPC receiver required for the Continue dialog. No standing or hidden
proxy human is created, and no client files are changed. Server-only objective
`10` is not sent. The consolidated history installs the final dialogue on fresh
databases; it does not repair intermediate branch saves.
See the [Bootcamp checks](world-testing.md) for migration and interaction coverage.

## Authorized radio missions

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionRadioLifecycleTests|FullyQualifiedName~MissionRewardTests|FullyQualifiedName~MissionRepeatabilityTests|FullyQualifiedName~BootcampCharacterEntryTests|FullyQualifiedName~MissionProtocolTests" --logger "console;verbosity=minimal" --verbosity quiet
```

| Native method | Exact tuple |
| --- | --- |
| `AssignRadioMission` / 408 | `(missionId,)` |
| `DispenseRadioMission` / 444 | `(missionId, six-field MissionInfo offer, forceDialog)` |
| `CompleteRadioMission` / 432 | `(missionId, selectionIdx, rating)` |

Nullable selection and rating fields accept only Python int or `None`, not
bool, zero-struct or Python long. Non-null ratings decode correctly but are
explicitly rejected by the shared reward planner. The offer remains the
six-field conversation shape, not the five-field mission-status shape.

`MissionRadioLifecycleTests` exercises non-Bootcamp fixtures through authority,
the native handlers and shared transaction planners. Coverage includes
five-minute expiry, duplicate notification/renewal suppression, the 30-slot
bound, reconnect and same-client round trips, wrong identities/revisions,
real scene provenance, requirements/capacity, item and receipt rollback,
selected rewards, repeat attempts and final-reader expiry/reset/session races.
The timestamp regression simulates MySQL `datetime(6)` precision in SQLite;
it is not a live MySQL acceptance result.

Run `BootcampInitiationTests` for the preserved arrival/reconnect flow and
`RadioChannelMigrationPreservesWorldRowsAndSupportsActuallyNullableNpcIds`
for the World upgrade and source metadata. Char upgrade coverage verifies
existing assignment, objective, history and receipt preservation.
`RadioSchemaChangesKeepDataInASubsequentMigration` checks both providers'
schema/data separation; SQLite must finish rebuilding the definition table
before `BootcampRadioOfferAuthority` seeds the source.
All fixtures are disposable; no Wilderness content is enabled.

Radio authority is separate from P2 NPC/object conversation sessions. Completion
resolves the current assignment once, because the native tuple has no token.
Do not claim detection of byte-identical delayed input after a fresh eligible
attempt. Native UI and live MySQL remain unverified by these server tests.

## Explicit party mission sharing

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionSharingTests|FullyQualifiedName~MissionRadioLifecycleTests|FullyQualifiedName~GroupMissionCreditTests|FullyQualifiedName~PublicEscortSceneTests|FullyQualifiedName~PublicSceneLifecycleTests|FullyQualifiedName~MissionRepeatabilityTests" --logger "console;verbosity=minimal" --verbosity quiet
```

| Native method | Exact tuple |
| --- | --- |
| `ShareMission` / 547 | `(missionId,)` |
| `DispenseSharedMission` / 445 | `(sourceActorId, missionId, six-field MissionInfo offer)` |
| `AssignSharedMission` / 409 | `(sourcePlayerEntityId, missionId)` |

Entity fields are Python long/unsigned 64-bit values, not account IDs; mission
fields are Python int. Incorrect tuple sizes and scalar kinds are rejected.
Dismissal need not send a decline RPC. An unaccepted offer grants nothing and
expires through the existing authority.

`MissionSharingTests` covers the real native handlers and common planners:
explicit acceptance, the inclusive 20-unit boundary and invalid coordinates,
live party/account/character/entity membership, recycled party IDs, reconnect
and session/map epochs, forged/stale/expired callbacks, duplicate notification
and the shared 30-slot cap. It also checks prerequisite self-writes, repeat
history, full journals, independent recipient failures and final-reader races.

Public scene cases verify one actor/run, atomic assignment/items/participant
membership, exact source-run generations, participant dialogue and abandonment
isolation, owner cancellation and late/repeated assignments receiving no old
credit. P4 final inventory checks remain active. Upgrade tests preserve P7
offers, objectives, assignments, history and receipts while adding nullable
party identity, including unsigned 64-bit serialization.

Capability cases cover required owner-only scene signals at content loading,
native projection and admission, including optional-to-required reveal/activate
paths, transitive optional chains and objective-state prerequisites. They cover
normalized, explicit-transition and legacy definitions, conservative rejection
of mixed owner-only/alternate branches, and independent initially active
progress. Optional independent signals, personal dialogue unlocks and future
group-credit dependency paths remain supported without copied progress or
participant world control. `GroupMissionCreditTests` drives
`CreatureManager`/`SceneApplication` deaths with two simultaneous missions,
checks independent personal/party credit, and rejects unmatched encounter
membership, stale assignment generations and old-run deaths.

These checks use disposable fixtures and do not activate production missions.
Native UI and live MySQL remain separate gates. The non-deadline lifecycle
query budget is purpose-counted, and migration fixtures use the preserved
`development` boundary and the consolidated schema/data steps.

## Assignment-owned quest items

Run the lifecycle and adjacent Bootcamp/reward checks:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionItemLifecycleTests|FullyQualifiedName~ConradCorpseDialogueTests|FullyQualifiedName~BootcampCallingForReinforcementsTests|FullyQualifiedName~BootcampBombRetryTests|FullyQualifiedName~MissionRewardTests" --logger "console;verbosity=minimal" --verbosity quiet
```

`MissionItemLifecycleTests` uses non-Bootcamp assignments without enabling
production content. It exercises issuance, explicit character-owned costs,
consumption and cleanup through real planners and handlers. It checks
per-assignment receipts, same-template isolation, composition with normal
rewards/loot, stale inventories, rollback, reload and character deletion.
Transfer, vendor, auction and trade checks exercise mutation paths, including
missing runtime markers backed by durable provenance.

Conrad's full Mission inventory must reject Continue without completing
objective `3`, starting the deadline or losing the valid open conversation.
Planting spends the bound bomb in its objective transaction before the fuse.
Timeout preserves unrelated copies, and retry `2005` issues a new bound item
only on acceptance. These fixtures create the final Char schema directly;
there is no intermediate legacy-inventory upgrade.

These checks run against disposable SQLite fixtures. Native UI behavior and
live MySQL migration/transaction acceptance remain separate checks; generated
scripts and server packet assertions do not certify either.

## Repeatability and stale attempts

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionRepeatabilityTests|FullyQualifiedName~MissionCompletionHistoryRegressionTests|FullyQualifiedName~MissionLifecycleTests|FullyQualifiedName~MissionRewardTests|FullyQualifiedName~GroupMissionCreditTests" --logger "console;verbosity=minimal" --verbosity quiet
```

`MissionRepeatabilityTests` covers opt-in authoring, per-assignment history,
independent rewards, competing retries, cooldown/reset boundaries, UTC
midnight and non-midnight windows, relog, pending rewards, terminal
replacement/capacity, durable history, and stale conversations,
progress publications, scene inputs, item operations and group candidates.
Automatic terminal replacement checks `MissionCleared -> MissionGained` and
rejects any publication before the replacement transaction commits.
P4 item lifecycle and P5 public actor suites remain required adjacent checks.
The reset regression uses an item-free Daily turn-in that advances a second
mission with item bindings, then moves the clock during that late inventory
participant's final reads. A save-time clock change alone does not cover this
ordering.

`MissionRequirementProductionTests` checks current-journal failure precedence
through both runtime offers and durable admission, alongside lifetime success
and rewarded-completion prerequisites. Abandonment coverage verifies immediate
post-commit history, rollback and unchanged lifetime reward timing.
`SceneInputDurabilityTests`, `SceneAssignmentLifecycleTests` and
`SceneApplicationTests` cover forwarded running routes, deferred/reconnected
effects, already-resetting sources, follow/attack controls, exact source
generations, late acknowledgement/callback changes and duplicate receipts.
They retain unrelated root actors/routes and one-time
experience state, and run alongside the public lifecycle/cleanup and private
Bootcamp actor cases.
The same SQLite database enforces the Daily window uniqueness rule; generated
MySQL SQL is not a live-provider concurrency result.

Native callbacks remain tokenless. The server checks the conversation/offer it
actually opened and resolves each operation to an exact assignment. A
byte-identical packet sent after a legitimately reopened new conversation is
indistinguishable from a current request. No native packet field or tuple
shape was added for repeatability, and native UI acceptance remains a separate
manual check.

## Mission interaction admission

NPC and corpse callbacks require a topic from an actual `RequestNPCConverse`
opening. The server captures the target/map/epoch and assignment/revision/
generation in one session, then rechecks them in the mutation transaction.
New recipient state cannot add a topic to old dialogue. No nonce or extra native
packet field has been added.

Run the admission and end-to-end checks:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionInteractionAdmissionTests|FullyQualifiedName~MissionLifecycleTests|FullyQualifiedName~ConradCorpseDialogueTests|FullyQualifiedName~BootcampEndToEndTests" --logger "console;verbosity=minimal" --verbosity quiet
```

The checks cover unopened input, non-finite/out-of-range positions, the inclusive
5-metre 3D origin boundary, dead/dying or disabled actors, foreign owners/maps,
target replacement/removal, stale assignments and generations, failed writes,
and movement/transfer/logout/character lifecycle invalidation. Corpse tests
decode the four-field Continue payload and dispatch the real handler, including
the displayed-objective-2/progression-objective-3 distinction.

Existing gameplay fixtures use `MissionConversationTestDriver` to position the
character and call the real opening handler before a callback. It removes only
the newly opened `Converse` response from the fixture's output queue, preserving
earlier gameplay packets. Admission tests use raw callbacks and explicit
openings; concurrent/failure tests open before the tested race or write.
Production mutation methods have no auto-open or test-only admission path.

Native `body.InRadiusOf(5)` uses body geometry unavailable on the server. The
server's origin-distance boundary and native body-distance acceptance still need
in-client comparison. Authoritative LOS is also unimplemented; these tests do
not certify visibility through obstacles or substitute navigation reachability.

## Native dialogue and choices

`MissionDialogueTests` checks literal wire bytes for all 16 `ConversationType`
values. `Converse` is a one-element tuple containing a dictionary. Completion,
ambient and choice entries contain lists of `(missionId, objectiveId, playerFlagId)`
triples. Important greetings are scalar IDs. EndConversation has a valid boolean
presence marker; ForcedByScript is a boolean, including native false. Invalid
types, unknown kinds and malformed nested entries must fail before any output.

The strict `PerformNPCChoice` decoder and registered handler use opcode `497`,
a five-field tuple, a 64-bit entity ID and choice indices `1/2/3`. The native
window supplies its own labels. Tests use the reviewed
`339/8/586/1` triple only in isolated content; no production mission is enabled.

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~MissionDialogueTests|FullyQualifiedName~MissionProtocolTests|FullyQualifiedName~MissionContentValidatorTests|FullyQualifiedName~ConradCorpseDialogueTests" --logger "console;verbosity=minimal" --verbosity quiet
```

Coverage includes typed reminder/ambient projection without progress, three-way
branch selection, missing or stale sessions, source identity and scene release,
duplicate and competing callbacks, rollback/retry, and vendor/trainer/clan/
auction topic preservation. Loaded NPC and object fixtures verify that objective
and flag writes plus the selected scene input share a transaction before
publication, and that the existing scene grant receipts prevent repeat rewards.
Conrad's actual corpse and displayed-objective-2/progression-objective-3 Continue
alias remain covered by the unchanged corpse suite.

Authoring tests reject sparse choice maps, unknown or mismatched transitions,
unbound scene inputs and object choices without matching metadata. Optional
dialogue fields do not change historical scene serialization or reflection-based
preloader columns. These are server/serialization tests, not native UI or live
MySQL acceptance.

## Native character flag snapshots

`PlayerFlags` (`710`) is a full replacement ID collection:
`tuple(1) -> list(count) -> unsigned IDs`. It is not a bitmask.
The native receiver stores `playerFlagIds`, and `HasPlayerFlag(id)` uses membership.
Game projects only mission-range IDs (`1..2147483647`) with nonzero stored values,
sorted by ID. Zero/unset and reserved server flags are absent on the wire;
numeric values and unset/zero distinctions remain in server storage.

Owners receive their committed cache during introduction/reintroduction and
after membership-changing commits. Other players' introduction payloads contain
an empty flag list. A failed publication leaves the latest owner snapshot pending
for a map-tick retry or owner reintroduction, without database reloads or repeated
grants. Enqueue success is not a native-client delivery acknowledgement;
disconnect/relog reconstructs the snapshot from persistence.

Run the focused packet and integration checks:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~CharacterFlagProjectionTests|FullyQualifiedName~CharacterFlagPersistenceTests|FullyQualifiedName~MissionProtocolTests" --logger "console;verbosity=minimal" --verbosity quiet
```

`CharacterFlagProjectionTests` checks literal empty/one/multiple-ID bytes, unsigned
encoding, zero/reserved filtering, both nonowner introduction paths, real
character-selection relog, NPC/world/failure/scene commits, rollback, final
composite ordering and duplicate suppression. Publication-failure tests verify
latest-snapshot recovery without extra database reads, writes or reward grants.
They also cover character replacement, transfer deferral and Bootcamp departure
reintroduction. These tests use disposable SQLite fixtures, not user databases;
native-client visual acceptance remains a separate check.

## Coverage inventory

| Boundary | Automated coverage |
| --- | --- |
| Length-prefixed transport | Invalid and oversized lengths, partial headers, buffer compaction after an earlier frame, and rejected-decryption callbacks |
| Protocol frame | The four-byte header and unsigned 16-bit size, exact frame consumption, compressed frames followed by another frame, and nonzero stream offsets |
| Reassembly | Splits at every boundary of a representative message, concurrent receive/decode, internal timeout frames, and older channel sequences followed by valid data |
| Compression | Large payloads, malformed DEFLATE/back-references, final-block completion, trailing compressed bytes, early EOF, declared-size mismatch, caller stream ownership, and pooled frame-buffer return |
| Expansion allocation | An unverified expanded-size field does not cause an allocation of that claimed size; memory grows as actual decompressed bytes arrive |
| Values and server-method payloads | Truncated counts/strings/arrays, negative collection/string lengths, unsupported flags/types, unknown methods, and trailing payload data |
| Handshake | The existing game-key length bound, short DES login payloads, queue key truncation, game cipher block/padding checks, and auth checksums at different buffer offsets |
| Login and queue lifecycle | Close/completion races, preloaded synchronous handshakes, disconnect-before-enqueue, redirect capacity, and removal of disconnected queued clients |
| Peer isolation | Invalid framing or RPC data rejects its decoder while another peer still dispatches; unrelated application exceptions remain visible |
| Compatibility | Existing auth/game cipher bytes, DES login fields, password format and uncompressed channel fields |

The incoming protocol message types remain `LoginMessage`, `MoveMessage`,
`CallServerMethodMessage` and `PingMessage`. Channel `0xFF` carries the internal
four-byte timeout message. Unknown message types and unsupported named methods
are rejected rather than routed as successfully decoded packets.

Producing the expected output length does not prove that DEFLATE completed.
The decoder retains .NET's data/history validation and uses the existing
Bouncy Castle 2.7.0 library to verify final-block completion and exact compressed
input consumption. This requires a second inflate pass, but does not retain a
second expanded payload.

Feature requests use the existing server-method router:

| Feature area | Examples of registered request methods |
| --- | --- |
| Character selection | `RequestFamilyName`, `RequestCreateCharacterInSlot`, `RequestCloneCharacterToSlot` |
| Abilities and tray | `RequestArmAbility`, `RequestPerformAbility`, `RequestSetAbilitySlot`, `RequestSwapAbilitySlots` |
| Missions and titles | `AssignNPCMission`, `AssignRadioMission`, `CompleteNPCMission`, `ChangeTitle` |
| Maps and travel | `MapLoaded`, `SelectWaypoint` |
| Loot | `RequestCorpseLooting`, `RequestLootAllFromCorpse` |
| Chat and contacts | `ChannelChat`, `ClanChat`, `PartyChat`, `RadialChat`, `Whisper`, `AddFriendByName`, `RemoveFriend` |

`RequestPerformAbility` accepts exactly four arguments
`(actionId, actionArgId, target, sourceItemId)`, or five with the client's yaw
last. Source item IDs may be Python longs (`0x2F` plus eight bytes), integer-form
IDs or the existing absent-value markers. They remain 64-bit entity IDs through
ability recovery; a legitimate consumable request must not be decoded as a null
marker or truncated to 32 bits. Unsupported tuple sizes and source types remain
invalid. The target retains its entity, absent or location forms.

Registration does not establish gameplay completeness. This suite uses
`RequestFamilyName` as a representative RPC payload; it does not certify cloning,
missions, abilities, travel, or loot. Those systems have their own focused tests,
and production mission definitions remain inactive unless their complete server
contract is available. Voice and dynamic-map-marker opcode declarations likewise
do not establish working feature support.

The explicit sharing workflow has its own packet, authority and lifecycle
checks above. An opcode declaration alone, including `DeclineSharedMission`,
does not establish a supported interaction. Public encounters and group credit
never automatically accept missions; acceptance and turn-in remain per character.

## Bounds established by the code

| Value | Meaning |
| --- | --- |
| 8,192 bytes | Default game transport buffer, from `SocketAsyncConfig.BufferSize` |
| 2,048 bytes | Default auth transport buffer |
| 65,535 bytes | Maximum encoded protocol-frame size representable by its unsigned 16-bit length |
| 262,140 bytes | Maximum declared expanded protocol payload (`4 * ushort.MaxValue`) |
| 64 bytes | Existing maximum game key length |
| Eight-byte blocks | Legacy cipher block size; game front-padding count is validated separately |

Transport sizes include the configured length-prefix convention. A partial
frame is compacted before receiving more data when earlier frames have consumed
part of the buffer.

The outbound writer's 32 KiB scratch allocation is not the inbound expansion
limit. The signed expanded-size field is validated before inflation and cannot
request more than 262,140 bytes. No guessed per-client queue quota is added by
these checks; an operational queue quota still needs a separate decision or
client/protocol evidence.

## Native-client acceptance remains separate

Use only an explicitly authorized isolated client session and disposable
characters. In addition to the Bootcamp checklist, verify owner-only native
flag snapshots (including clearing the last flag), all three NPC/object choices,
quest-item display and restrictions, radio offer/turn-in, repeat log replacement
and Daily reset, and two-client sharing at and beyond 20 units. Confirm that
late joiners see only future credit and cannot control the shared actor. Server
packet tests do not establish native rendering, dialogue text, body-boundary
range or line-of-sight behavior.

These synthetic regressions do not reproduce the complete first-load sequence
reported in [InfiniteRasa/Rasa.NET#45](https://github.com/InfiniteRasa/Rasa.NET/issues/45).
Do not close that issue or remove its workaround solely because this suite passes.

To verify the reported behavior, use a cold 1.16.5.0 client to log in, select a
character and enter a map repeatedly without restarting the game server. Capture
the server error and relevant frame boundaries if it fails. Redact account
identifiers, one-time login keys, credentials and private chat from any shared
trace. Record native-client results separately from server startup, synthetic
socket checks and unit-test results.

For Bootcamp-specific client validation, pair this guide with the manual
acceptance checklist in [world-testing.md](world-testing.md).
