# World movement, travel and spawn checks

The first world-reliability target is **Concordia Wilderness**, map context
`1220` (`adv_foreas_concordia_wilderness`). New Deployment 11 characters enter
private Bootcamp `1985` first; legacy and skipped characters use their saved map.
The checked-in seed contains 218 spawn pools there: 183 have a nonzero configured
population and 35 are empty. Empty pools are not populated with invented defaults.

For mission creation, use [mission authoring and operations](missions.md) and
the [data/script reference](mission-reference.md). This page describes behavior
and acceptance checks. Mission data is installed by the normal provider
migrations: automatically at SQLite startup and manually for MySQL.
This branch's migration-owned design targets fresh databases.

## Run the automated checks

After the SDK/dependency setup in [the setup guide](setup.md), run:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.World"
```

The fixtures use isolated in-memory worlds, actual outgoing packet queues,
deterministic clocks/random sources and disposable SQLite files. They do not
connect to a developer database or require the game client.

## Bootcamp starting-experience regression suites

Deployment 11 Bootcamp coverage is split between focused mission tests and
starting-experience gameplay tests. The maintained suites now cover:

- the authored mission chain `1990 -> 1992 -> 1994 -> 1995`
- reconnect at mission and objective boundaries
- combat/timed failure, timeout, retry, and respawn paths
- account-wide skip entitlement and second-character skip parity
- two simultaneous Bootcamp characters in distinct private `1985` instances
- startup validation logging for required-content defects

Run the Bootcamp-focused regression suites with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Bootcamp"
```

Run the full mission-suite regression pack with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Missions"
```

`BootcampEndToEndTests` covers the normal chain, the `1995 -> 2005` retry
path, entitlement unlock, and simultaneous-character isolation. The mission
files `BootcampInitiationTests`, `BootcampGearingUpTests`,
`BootcampCaptureTheFlagTests`, `BootcampCallingForReinforcementsTests`, and
`BootcampBombRetryTests` keep the reconnect, boundary, and failure cases
focused. `BootcampCharacterEntryTests`, `BootcampDepartureTests`, and
`BootcampSkipTests` cover first entry, exit-pad departure, and skip parity.
These automated suites validate server-side mission and gameplay behavior only.
They do not drive the retail client.

### New-character defaults and the arrival offer

Character creation persists a pistol (template `17131`) in weapon drawer slot
`0`, 1,000 rounds (template `28`) in the consumables inventory, and rank 1 in
Lightning, Sprint, Firearms, Hand to Hand, and Motor Assist Armor. Lightning and
Sprint occupy drawer slots `0` and `1` (visible positions 1 and 2). These records
share the character-creation transaction. They are not granted again on login,
and existing characters' equipment, allocations and drawer choices are not reset.

Deleting and recreating a character in the same selection pod must start with
only the new character's loadout. Moving the old pistol to visible weapon slot 2
before deletion must not leave a second pistol on the replacement character,
either in the same session or after reconnecting. Selection pod numbers are not
inventory owner IDs.

Level-1 attributes remain 10 Body / 10 Mind / 10 Spirit, with zero unspent
attribute or skill points. Later level-ups retain their normal point budgets.
Login fills Health and Power but starts adrenaline empty. The first Bootcamp
entry reverses the old spawn heading by 180 degrees; reconnect uses the saved
heading instead of rotating again.

Initiation (`1990`) is offered on arrival, not preaccepted. The client receives
`DispenseRadioMission` and its Accept Mission button sends `AssignRadioMission`.
Only an active character in its own private Bootcamp instance, with durable
starting-experience state `Bootcamp`, can accept this arrival offer. The mission
and initial objectives are committed before `MissionGained` is sent. Reconnecting
before acceptance offers it again; reconnecting after acceptance restores progress.

The two icons under "You will receive" show **credits** and **prestige**, not XP.
Initiation grants 100 XP and, after `BootcampMissionAudioAndCredits`, 100 credits.
Gearing Up, Capture the Flag, Calling for Reinforcements and its retry each
grant 200 credits at their successful mission turn-in. Prestige remains zero.
The retry is the alternative failed-attempt path, not a second grant for the
original mission. The native client has no XP field in this reward tuple. NPC/radio offers,
mission gain and mission-log snapshots use the same authored currency/item
preview as turn-in; creating a preview does not grant rewards.

Mission voices use generic authored audio metadata, not Bootcamp-specific
branches in the manager. The offer's existing audio slot plays narration when
the dialog opens: Bootcamp uses sets `2773`, `2774`, `2775`, and `2776` for
missions `1990`, `1992`, `1994`, and `1995`. Eloh announcements bind sets `2788`
and `2789`; successful Initiation completion binds McAllister's set `2777`.
These IDs refer to installed native audio sets, not copied sound files.
Accepted/completed/announcement cues run after committed progress and are not
replayed by a rejected duplicate request or ordinary reconnect. The native
offer narrator owns its dialog playback; it is not played again by a second
acceptance cue unless one is explicitly authored.

The `SeedWorldContent` migration installs greeting `1634`, the client's
Logos/Lightning cue (SQLite applies pending migrations on startup; MySQL
requires an explicit update).
`client/ui/conversationwindow.py` selects `tutlightning_left` or
`tutlightning_right` for that greeting. The client owns the glow animation and
hides it when the animation ends; the server does not change the selected slot
or create a competing timer. The second greeting is unchanged. Native acceptance
should confirm the expected roughly five-second highlight in both UI layouts.

The focused creation, acceptance, rollback, reconnect and cue checks are:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~CharacterStartingExperienceCreationTests|FullyQualifiedName~BootcampCharacterEntryTests|FullyQualifiedName~BootcampInitiationTests|FullyQualifiedName~BootcampLightningCue|FullyQualifiedName~MissionProtocolTests"
```

For guidance on authoring new mission content itself - conversation delivery,
text/position sourcing, real-client verification - see the
[mission authoring guide](missions.md#add-a-mission).

## Mission protocol boundary

The mission request boundary matches the local 1.16.5.0 client scripts:

- `AbandonMission` (392): `(missionId,)`
- `AssignNPCMission` (407): `(npcId, missionId)`
- `AssignRadioMission` (408): `(missionId,)`, requiring a current server-authored radio offer
- `AssignSharedMission` (409): `(sourcePlayerEntityId, missionId)`, requiring a current party offer
- `CompleteNPCMission` (430): `(npcId, missionId, selectionIdx, rating)`
- `CompleteNPCObjective` (431): `(npcId, missionId, objectiveId, playerFlagId)`
- `CompleteRadioMission` (432): `(missionId, selectionIdx, rating)`
- `PerformNPCChoice` (497): `(npcId, missionId, objectiveId, playerFlagId, choiceIdx)`
- `RewardNPCMission` (540): `(npcId, missionId, selectionIdx, rating)`
- `ShareMission` (547): `(missionId,)`

`selectionIdx` and `rating` accept only Python `int` or `None`; boolean structs,
longs, and incorrect tuple sizes are rejected. Registering these handlers does
not activate source-only mission definitions. Unknown and inactive requests do
not mutate runtime or durable state and do not publish success packets.

Objective updates use the client receiver tuple layouts for
`ObjectiveRevealed`, `ObjectiveActivated`, `ObjectiveCompleted`,
`ObjectiveFailed`, `UpdateObjectiveCounter`, and
`UpdateObjectiveItemCounter`. Each serialized objective has eight fields,
including separate generic and item counter dictionaries, nullable remaining
time, and complete X/Y/Z indicator coordinates. Objective state and current
counter values are persisted separately from immutable initial/target metadata.
Radio and shared offers use the six-field conversation information tuple, not
the five-field mission-status tuple. Radio channels and party sharing use the
same acceptance/reward planners with separate durable source authority; a
submitted mission ID alone grants nothing. Bootcamp remains private, Once and
unshareable, and only its existing Initiation arrival source is enabled in
production. See the [radio and sharing checks](protocol-testing.md#authorized-radio-missions).

Run the focused boundary checks with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Missions.MissionProtocolTests|FullyQualifiedName~Rasa.Test.Missions.MissionTrackerTests|FullyQualifiedName~Rasa.Test.Missions.MissionRewardTests"
```

## Movement and visibility

Movement is accepted only for an active, assigned player in a world cell, with
finite position/motion values and no pending transfer or logout. Rejected input
does not change the authoritative position.

The existing grid uses 25.6-unit cells, a 32768 coordinate bias and 16-bit cell-key
components. Cell identities must not wrap onto another cell. Packed movement
coordinates retain their signed 24-bit representation. Channel sequences reject
duplicates/older messages and continue across uint32 wrap.

Cell transitions notify both sides when players leave visibility. Introductions
are queued before movement reaches a newly visible observer. Recipients are
deduplicated and disconnected clients are excluded. Losing visibility does not
unregister the unseen player from the world.

No anti-cheat speed tolerance or terrain/collision boundary is inferred from these
checks. Those require client timing or map data that is not present in the repo.

## Waypoints and dropships

Both local and dropship destinations must be discovered by that character,
available and uncontested. Selection also requires a nearby source station of
the matching type: the existing proximity ranges are two units for local
waypoints and five for dropship triggers. Discovery is persisted before its
runtime grant and notification.

One-way starting-experience extraction is not part of this network. Entering
its ready beam starts the authored departure directly, without discovery or a
travel menu. Old Bootcamp discovery rows are ignored during character loading
and destination enumeration, and requests to fly back there are rejected.

Destination map context comes from the waypoint definition. It is not an alias
for the instance identifier. The current shared-world implementation advertises
instance `1`; private operations and population instances are separate work.
Menus contain one instance descriptor per map and use authored station positions.

A local transfer uses one authoritative landing position for server state,
teleport notification and movement. The existing local one-unit height offset
is retained. Position persistence occurs on acknowledgement. Dropships track
boarding, departure and map-load acknowledgement, advance only on their owning
map, and preserve runtime inventory identities across the map transition.

Configure the deadline in Game's environment-specific settings:

```json
{
  "GameConfig": {
    "TransferTimeoutSeconds": 60
  }
}
```

The value must be positive. The default is 60 seconds. If the client does not
acknowledge in time, the server restores the transfer origin and disconnects;
the client can reconnect at that origin. This prevents a late acknowledgement
from completing a later transfer. A failed position write does not report a
successful transfer. Duplicate or unsolicited acknowledgements are ignored.

Provider/query failures are logged and restore the in-memory transfer origin.
The disconnect save is best-effort and also logs database failures; durable
recovery cannot be promised while the database is unavailable. Departure
cancels owned auto-fire timers and completes clan cleanup before discarding
runtime inventory.

## Weapon ammunition and HUD packets

Run the focused gameplay checks with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Gameplay.WeaponAmmoConsolidationTests"
```

Weapon commands require an active, living owner and a valid weapon in the
selected drawer slot. Shots use the loaded template's ammunition consumption
and refire interval. Manual requests and auto-fire share the same deadline.
Reloads accumulate matching reserve stacks, retain ammunition already loaded,
and recheck inventory when their template-defined delay ends. Switching weapons,
interruption and departure invalidate pending work. Reload/draw/stow capture
their owner's combat revision at admission; missiles capture source and target
revisions at launch. Recovery and impact reject changed revisions even if the
same objects have returned to the map or revived. This includes shots left
queued while a map has no clients. A valid shot pays for ammunition at launch;
invalidating its later impact neither spends another round nor refunds that shot.
Auto-fire also captures the owner's combat revision and cannot resume after
death/revival.

Shots and reloads persist before changing runtime ammunition or sending success
packets. `ICharUnitOfWork.ExecuteTransaction(Action)` wraps one character context
in an EF relational transaction. Participating repository calls may save inside
the callback, but must use that same unit of work and propagate errors. The
callback must not publish gameplay state or packets. The operation commits after
the callback and any remaining changes; failure rolls back and clears tracking.
It does not nest transactions or retry an operation automatically.

Transaction completion explicitly detects a lost connection before commit.
Rollback skips an already-ended connection; a rollback error is logged without
replacing the original exception. The transaction boundary still rethrows.
Gameplay catches only explicit planning/stale-state rejection, repository
missing-record, database/update, checked-overflow and capability errors.
Unexpected null dereferences and unrelated `InvalidOperationException`s remain
observable with their identity and stack. Expected failures publish no grant
and allow a later request to retry from durable state.

The SQLite fixtures reopen item and inventory rows after successful operations,
injected save failures and retries. They also check drawer selection on relog,
equipment/ammunition packet pairing, and launch/impact rejection of stale or
cross-map targets. These checks do not establish native crosshair or HUD
acceptance. MySQL uses the same EF transaction API but requires separate live
provider verification.

The cross-feature regression suites exercise real gameplay entry points and
SQLite query/save boundaries:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Gameplay"
```

Successful local/dropship admission uses the same combat-cancellation hook as
world departure, before the transfer becomes pending or the position changes.
It cancels Lightning, reload/draw/stow, Sprint and auto-fire, and advances the
combat revision for queued missiles. Same-map selection and acknowledgement
need no intervening worker tick. Rejected travel does not cancel combat.
The separate corpse-eligibility revision does not change for local travel;
loot remains subject to its original ownership, lifetime and current distance.

## Owner-only corpse loot

Run the focused checks with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Gameplay.LootConsolidationTests"
```

The default maximum corpse-looting distance is **6 metres**, measured in finite
3D world coordinates from the player to the corpse's origin, including height.
It applies to both opening the menu and claiming its contents. Configure it in
Game's settings:

```json
{
  "GameConfig": {
    "CorpseLootDistance": 6
  }
}
```

A configured limit must be finite and greater than zero. Invalid limits reject
both opening and claiming; they do not silently fall back to a different range.
Explicit configuration overrides still take precedence over the default.
If an existing local configuration sets `CorpseLootDistance` to `2`, update it
to `6` to receive the new behavior.

The native 1.16.5.0 client permits manual looting within the manifestation's
six-metre use range, testing the corpse's `DAMAGE1` connection point or its origin.
The previous two-metre server default rejected ordinary clicks at three to five
metres even though the corpse advertised lootability. The server still measures
to the origin, not animated client connection points. Its out-of-range
rejections now include the actual/configured distances in the debug log;
lootability effects alone do not mean a corpse is currently in reach.

Both requests recheck the living, active owner, account/character identity,
registered player and corpse, current map/cell membership, corpse attachment and
original lifetimes. Logging out, departure/re-entry, expired/removed corpses,
foreign ownership and non-finite or out-of-range positions cannot grant loot.
No obstacle, collision or group-distribution data is inferred.

Supported requests use the existing layouts:

- `RequestCorpseLooting` (opcode 650): `(dispenserEntityId,)`. It refreshes
  item entity data, `LootInfo` and `CanLootItems`, then sends
  `LootCorpse(actorId, lootItems)` on the dispenser entity to open the menu.
  It does not grant items or credits.
- `RequestLootAllFromCorpse` (opcode 651):
  `(dispenserEntityId, autoLootOnly)`. Both manual and automatic requests attempt
  the selected batch and can succeed without a preceding open request. Manual
  take-all selects every remaining item. Automatic requests select items at or
  below the player's validated `SetAutoLootThreshold` value, which defaults to
  Junk until the client sends its option. Both paths retain the same ownership,
  range, capacity, transaction and duplicate-grant checks.
- `RequestLootItemFromCorpse` claims one named remaining item into the requested
  destination slot through the same transaction path.
- `CancelCorpseLooting` detaches the client from the current dispenser without
  granting its contents.

Malformed tuple arities are rejected. Gameplay rejections return failure and
log the reason without grants or success packets; no new wire error layout is
assumed.

The planner uses loaded template categories, the existing five 50-slot personal
inventory categories and class stack maxima. It merges and splits the complete
batch before writing, and validates current durable character ownership, credits,
item identities/counts and inventory slots. A batch that does not fit grants
nothing, including credits.

Stack updates, new item rows, inventory rows and credits share one
`ICharUnitOfWork.ExecuteTransaction(Action)` operation. Immediate-save repository
methods propagate failures. No participating operation opens another unit of
work. Runtime inventory, credits and success packets are published only after
commit; rollback frees staged item identities and leaves the corpse available
for a new attempt. The fixtures use migrated SQLite repositories and reopen
state after failures before/after each write, connection loss before commit,
successful retries and inventory relog.

Claims serialize with owner departure and corpse removal. After commit, item
and credit updates precede `ActorGotLoot`, `TakenInfo`, disabled `CanLootItems`,
`GotLoot` and dispenser destruction. Queued loot packets snapshot their values
before runtime loot is cleared. Consumption is terminal even for empty or
credit-only loot. The dispenser is removed, its item identities reclaimed, and
the corpse attachment cleared. Dispenser request IDs are not recycled, so a
delayed claim cannot address a later corpse. Expiry uses the existing 20-second
corpse timer and disables/destroys the dispenser before destroying the corpse.
Loot tables and random template, quantity, credit and quality selection are
unchanged. Mission rewards use their own atomic turn-in path; party distribution
remains separate work.

These automated checks do not drive the native 1.16.5.0 client. Verify opening,
item display, manual take-all, inventory updates and closure in the native
client before declaring the UI complete. Auto-loot threshold behavior is
covered by automated manager tests, but its native option and window
presentation need separate acceptance. The offline suite covers the shared
transaction and migration paths without a live MySQL server.
Loss of a database commit
acknowledgement is still ambiguous; there is no durable distributed exactly-once
claim ledger. Subsequent stale inventory/credit snapshots fail closed.

Inventory loading registers and publishes only the selected character's items
and the account's shared home inventory (`character_id = 0`). It never adopts an
item from a deleted, unknown or zero character owner into a character inventory,
even when the destination slot is empty. Invalid-owner rows are logged and left
unchanged for explicit repair; another character's valid inventory is not sent
to the client.

Character deletion removes its inventory ownership rows and item records in the
same transaction as the character and auction listings. Other characters'
inventories and shared account/clan storage remain intact. A failed transaction
retains the character and its items. This requires no schema migration or
database reset. Items already reassigned by an older build are not removed
automatically because their former ownership is no longer recorded.
Already-duplicated personal slots are also not repaired automatically, and loot
claims still reject them before any inventory or credit writes.

## Bootcamp equipment crate

Mission `1992` keeps the existing crate class `29877`. Right-click uses its
normal `UseObject` request and short recovery to open the same loot menu as
creature looting. The Use acknowledgement precedes the menu; it never transfers
items. Do not disable Usable or substitute a different entity class to change
click targeting.

The crate is present at `(398, 122, 173)` when the private Bootcamp map is
created, before mission acceptance. Its interaction is disabled until Delessio's
gear briefing activates the loot objective. Activation reuses the same physical
entity; it does not replace or duplicate the crate. Dormant crates have no loot
dispenser. A crate already visible when the objective activates receives its
loot attachment without requiring the player to leave the area or reconnect.

The dispenser is introduced after the crate enters the client's visible cells.
Opening introduces the real item entities before the menu packet. Selecting a
row claims only that row; Loot All claims the remaining contents. Proximity
auto-loot cannot claim this crate. Opening and claiming use the existing object
use distance, not the shorter configurable corpse-looting distance.

Claims use the same inventory transaction as creature loot. Per-template
receipts in `character_mission_scenario_step` commit with the inventory changes,
so reconnect restores only unclaimed rows. Collecting the final row completes
the crate objective and grants the existing equipment proficiencies. The empty
crate stays visible in its opened state, including after reconnect and mission
completion. A character whose crate objective was already completed by the old
bulk-grant implementation does not receive a second loadout.

`SeedWorldContent` installs the final crate behavior without an intermediate
bulk-grant or despawn migration. Apply the current World/Char migrations; see
[deployment setup](setup.md#mission-data-migrations).
The crate's world lifetime now belongs to the experience run. Its per-template
loot-claim records still use `character_mission_scenario_step`; do not confuse
that loot ledger with the general scene receipt tables. Normal reconnect does
not reset either; the migration-owned branch itself requires fresh databases.

Run the focused server regressions with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampCrateLoot|FullyQualifiedName~LootConsolidationTests"
```

Native-client acceptance for this repair remains separate: right-click and
confirm all six rows are visible without inventory changes; close and reopen;
take one row and reconnect; collect the remainder with Loot All; confirm the
crate stays in place and cannot grant duplicates. Repeat creature looting to
check that its existing interaction is unchanged.

## Bootcamp NPC staging and the DeSimone handoff

After the committed acceptance of Gearing Up for Battle (`1992`), Major
McAllister runs along the map's navigation mesh to `(400, 120, 150)`, stops at
orientation `2.175`, and remains there. Rejected acceptance does not move him.
The experience-owned role and committed mission/history facts drive recovery:
reconnecting during or after the run restores him at the destination, not at his
original post. His movement and spawn position affect only the owning
character's private map.

On reconnect, scene recovery can run before the normal static-NPC spawn worker.
A valid automatic spawn pool waiting to produce McAllister leaves his scene
operation pending without an error. His saved final pose is applied to the pool
before spawning, and the existing actor-available notification completes the
pending operation. This avoids the repeated `ensure-mcallister` / `Public actor
spawn 510203 is unavailable` errors during normal map initialization. Missing,
disabled or invalid spawn definitions still report failures; no NPC is spawned
early to bypass its normal lifecycle.

Corporal DeSimone is a static NPC, creature `510206`, conversation package
`2562`. His corrected spawn is `(391.5, 120.059, 164.8)`. The existing horizontal
position is retained; the height is measured from the checked-in Bootcamp
navigation mesh. The previous height, `114`, placed him about six units beneath
the walkable floor, outside the normal three-unit spawn-snapping tolerance.
This is a terrain-validated reconstruction, not a verified retail coordinate.

Finishing Hartmann's training makes Gearing Up ready to turn in at DeSimone.
The mission briefing remains until that turn-in; training completion is not an
automatic reward claim. DeSimone then offers Capture the Flag (`1994`) through
the existing NPC conversation.

`SeedWorldContent` includes DeSimone's corrected height and McAllister's nonzero
run speed, while retaining his zero wander speed. It does not change the
mission chain.

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampMapSetupTests|FullyQualifiedName~BootcampWorldContentGrounds"
```

In the native client, verify the locked crate before accepting Gearing Up,
watch McAllister run and stop at the supplied pose, and reconnect to confirm
that he stays there. Finish rifle and Lightning training, find DeSimone above
ground, turn in Gearing Up, and accept Capture the Flag without relogging.

## Gearing Up conversation and practice targets

Committed mission changes refresh the conversation status of visible NPCs.
Completing the crate/equipment step must make Delessio available immediately;
his handoff must make Hartmann available without reconnecting. NPCs outside the
player's visible cells or in another instance are not part of that refresh.

The firing range uses three permanent Practice Dummy objects, entity class
`29365` (`arch_hum_practice_target_v01.geo`), at these exact positions:

| X | Y | Z |
| --- | --- | --- |
| 386 | 120 | 184.7 |
| 380 | 120 | 186 |
| 375 | 120 | 186 |

They exist when the map is created, before accepting mission `1992`. Rifle and
Recruit Lightning rank 1 training use the same objects. Hits leave them upright;
they do not die, grant loot, or get replaced when an objective is accepted.
The client receives usable-object damage information and target category
`OBJECT`, not creature/NPC metadata. The former `TestTargetDummy` class `26548`
is not used for these targets.

The `ObjectHit` progress event (`event_kind = 13`) distinguishes object hits
from creature kills and ability hits on creatures. Its `subject_id` is the
object's entity class and `counter_id` is the action ID: weapon attack `1` for
objective `3`, Recruit Lightning `194` for objective `8`. A rifle hit cannot
complete the Lightning objective. Weapon ammunition and Lightning eligibility,
range, costs, interruption and cooldown checks remain on their normal paths.
Other abilities and higher-rank object arc behavior are not added by this change.

Apply the pending **World** migrations when deploying. `SeedWorldContent`
installs the shared practice-target bindings and disables `show_3d_effect` for
the Bootcamp chain, preserving navigation coordinates and radii.
The client's `missionlog.py` checks this flag before creating the floating
`OVERHEAD_MISSION_INDICATOR` effects; NPC conversation status is not suppressed.

The client contract was checked against `trpython`'s English entity-class
names, `augmentations/inertdestroyable.py`, `augmentations/usable.py`,
`actions/abilities/damagebase.py`, `actions/targetedaction.py`, and `missionlog.py`.
The repository's `src\Rasa.NavMesh\data\entity_meshes.csv` confirms the model.
No client files are modified.

Run the focused regression cases with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampGearingUpInteractionTests|FullyQualifiedName~BootcampPracticeTargets|FullyQualifiedName~BootcampObjectiveIndicators"
```

Native-client acceptance still requires a new connection to the updated server:
check all three target models and positions before accepting the mission, finish
the rifle/Lightning sequence without relogging, and confirm that objective stars
are absent while tracker and NPC interactions continue to work.

## Native-client Bootcamp acceptance checklist

### Bootcamp startup navigation

`BootcampReportedNavigationTests` runs the normal navigation loader from the
Game project directory before creating a private instance. It verifies Forean
entity introduction and following to `(347.66016, 121.69922, 64.625)`, both bridge
factions fighting across respawn cycles on walkable ground, and refusal of
unchecked scripted or failed-query movement. Loading the mesh directly inside
a movement fixture does not cover asset discovery at server startup.

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampReportedNavigationTests|FullyQualifiedName~BootcampInitiationTests|FullyQualifiedName~AcceptingGearingUpRunsAlisterToHisFinalPosition"
```

### Capture the Flag encounter and escort

Apply the paired `SeedWorldContent` World migration before exercising
mission `1994`. It disables the old Collector/Dissector bridge pools, adds a
recurring battle between AFS soldiers and Thrax Infantry Initiates, and authors
the three Forean companions. The cave-exit area and indicator `439` are unchanged.

The Thrax use class `29769`, model `creature_thrax_soldier_grunt_v01.geo`, and
client name `7674`. Two AFS soldiers and three Thrax have individual,
terrain-checked bridge spawn points. Both factions fight through normal AI and
missile damage and respawn 20 seconds after death. These soldiers stay in the
bridge encounter; they are not the player's escort. Positions and combat
balance are server reconstructions, not claimed retail measurements.

The companions exist around the shooting range before the mission:

| Companion | Creature | Client name | Entity class | Starting position |
| --- | --- | --- | --- | --- |
| Forean Guardsman Initiate | 510213 | 7874 | 7034 | (368, 120.21479, 158) |
| Forean Shaman Initiate | 510214 | 7890 | 7035 | (372, 119.956856, 158) |
| Forean Archer Initiate | 510215 | 7986 | 7036 | (374, 119.74777, 164) |

Accepting Capture the Flag makes these same actors follow the character.
`UpdateEscortStatus` (`684`) drives the client's `OVERHEAD_ESCORT` effect
(`vfx_overhead_escort`) and escort minimap markers. This does not re-enable the
gold 3D objective indicators. Marker state is included when an escort becomes
visible again. Reconnect restores surviving companions near the character's
saved position; deaths remain durable, and losing a companion does not fail the
mission. Only the owning escort's kill can advance the player's objective.

Owned escorts enter catch-up running beyond 10 metres, return to walking within
6 metres, and stop within 4 metres. A bounded catch-up sprint closes a larger gap
without teleporting. They assist actual attacks against hostile creatures,
rather than attacking anything the player selects, and return to following when
the fight ends or the owner moves away. Their kills grant the owning player
normal XP, loot and mission progress.

The existing cave-exit trigger starts Tizzik's encounter. Tizzik G is the
mission's boss (the client creature-name table spells his name "Tizzik Gi").
He now uses normal combat actions. His defeat retains the existing seven-second
Youngblood arrival delay. Scenario objective activation refreshes NPC
conversation availability after the actors and objective state converge, so
Youngblood is speakable without reconnecting. After the turn-in he remains at
the reclaimed base across reconnects and offers Calling for Reinforcements.
Complete Mission grants rewards and finishes the mission in one request.
Youngblood is an unkillable base defender, not an escort. He fights nearby
Thrax while remaining available for conversations. His kills grant player XP
and loot only when the player or their owned escort damaged that enemy during
its current life; unattended base fighting grants no player rewards.

The paired `SeedWorldContent` World migration adds twelve packs (42 Thrax total)
along the cave, base, missing-team and crash-site approaches. Each pack contains
three or four level-8-13 infantry; the new pools respawn after 120 seconds.
Tizzik is level 13. The pre-existing bridge battle keeps its 20-second respawn.
Positions are measured against the repaired navmesh, including the underground
cave route, rather than terrain height alone.

Bootcamp Thrax (bridge infantry, route infantry and Tizzik) use a dedicated drop
profile. Each eligible corpse contains one Thrax Skull (template `41666`, class
`20307`) and rolls these additional drops independently:

| Item | Template | Chance | Quantity |
| --- | --- | --- | --- |
| Standard cartridges | 28 | 55% | 12-24 |
| Standard batteries | 56 | 30% | 8-16 |
| Basic medpack | 44917 | 15% | 1 |
| Thrax Medal | 41665 | 25% | 1 |

Credits retain the existing 1-9 range. Items are rolled and created once, in a
single character transaction, before presenting the corpse. The existing
owner-only inventory claim, partial claims and retry protection still apply.
Other creatures retain their existing drop behavior.
Loot-bearing scenario corpses, including Tizzik, use the ordinary corpse
lifetimes (120 seconds unclaimed, 300 seconds while open), not the one-second
cleanup used for non-lootable scenario actors.

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BootcampCaptureTheFlag|FullyQualifiedName~CaptureTheFlagContent|FullyQualifiedName~BootcampEncounterLootTests|FullyQualifiedName~BootcampCompanionCombatTests"
```

#### Navigation repair from the cave exit to the reclaimed base

The checked-in `navmesh\adv_bootcamp.nav` used to stop short of a complete route
from the cave exit `(279.05, 120.5, 66.07)` to Youngblood at
`(93.2, 109.64925, 137.5)`, with escort AI following the partial route and
stopping near `(182.84, 108.35, 83.18)`. Following from the shooting range to
the cave exit already worked; only the cave-to-base leg was broken.

Rebuilding `adv_bootcamp` from the real client data (`data\maps\adv_bootcamp`
and `data\mesh*.glm`; map/terrain files alone omit the bridge and cave
collision meshes) reproduced the same stop point, ruling out a stale checked-in
file. Probing the built mesh isolated the break to a single seam around
`(206-207, 105, 96)`, between the walkable shelf/cavern-entrance geometry
next to the AFS bridge (`arch_hum_bridge_48m_v02`, reachable from the cave
side) and the bridge deck/terrace geometry reachable from the reclaimed base.
The two sides sit about 0.9-1.1 m apart in height there - a small ledge in the
broken-bridge/cavern-entrance collision (`arch_forean_bridge_ceremonial_broken_v01`,
`arch_forean_eloh_cavern_entrance_v01`) - just over the builder's default
0.9 m `AgentMaxClimb`, so Recast never linked the two regions. The much larger,
genuinely vertical cliff face further north (`x` about 182-190, dropping from
the cave-exit ledge to the river) stays correctly excluded at any reasonable
climb value; the fix does not touch it, and the repaired route avoids it
entirely, crossing the AFS bridge instead.

Rebuilding just `adv_bootcamp` with `--climb 1.0` (up from the 0.9 m default,
justified by the measured ledge height, and scoped to this one map's build
rather than the shared default in `BuildSettings`) closes that seam and
produces a complete path. `ForeanEscortsCanFollowFromTheCaveExitToTheReclaimedBase`
covers this route and is no longer ignored.

The subsequent grounding correction retains that climb setting but uses
0.05-metre vertical cells, one-metre terrain sampling and denser surface-height
details (`--detail-distance 1`). The previous mesh put Youngblood's base
0.32-0.55 metres above the source terrain despite passing a nearest-navmesh
check. Five measured base points now differ from the source surface by less
than 0.15 metres; `BootcampGroundingTests` also requires complete cave/bridge,
missing-team and crash-site routes. Youngblood's authored Y is `109.2201` at
`(93.2, 137.5)` in X/Z. These settings apply only to the regenerated Bootcamp
asset, not the builder defaults or other maps. Native animation and feet-to-ground
rendering still require client acceptance.

To reproduce or re-diagnose on a machine with the full game installation:

```powershell
dotnet run --project src\Rasa.NavMesh\Rasa.NavMesh.csproj --configuration Release -- --path navmesh\adv_bootcamp.nav 279.05 120.5 66.07 93.2 109.64925 137.5
```

This now returns a complete path and exit code `0`. To rebuild `adv_bootcamp`
into a temporary output directory instead of trusting the checked-in file:

```powershell
dotnet run --project src\Rasa.NavMesh\Rasa.NavMesh.csproj --configuration Release -- --client "<Tabula Rasa install>" --out <temp dir> --map adv_bootcamp --climb 1.0 --cell-height 0.05 --terrain-step 1 --detail-distance 1
```

Verify any future rebuild with the same `--path` query before replacing the
checked-in mesh; do not move area/indicator `439`, move the base NPCs to
bypass a gap, or raise climb/slope limits without checking the resulting
paths against the actual collision geometry, as was done here.

The repository does not contain a native-client automation harness. Use this
manual script when validating Bootcamp in the retail `1.16.5.0` client.

Calling for Reinforcements (`1995`) exposes only client objectives `2, 3, 1, 4`.
Entering area `435` completes objective `2` ("Locate the missing AFS soldiers")
and reveals Conrad's corpse interaction. No standing survivor NPC is spawned.
Clicking the corpse opens the native mission Continue dialog; only Continue
completes objective `3`, places the bomb in Mission inventory and starts the
600-second deadline. Closing the dialog does not grant or advance anything;
finishing the 1400 ms planting windup satisfies it before the fuse and
reinforcement arrival. Planting also starts one six-Thrax assault at the foot
of the hill. The attackers follow a grounded uphill route and engage the player;
combat can interrupt their advance without counting as a death.
After the five-second fuse, the wreck receives its native closed-to-open
destruction transition. Two seconds later the wreck clears and the evacuation
ship appears with Van Valkenberg and two AFS soldiers. The soldiers defend the
pad through normal AI and damage. They do not replace the player's earlier
Forean companions.

Van's check-in stays locked until all six assault enemies are defeated.
Soldier final blows count even without player reward credit. Defeats and their
scene inputs are saved together, so reconnect restores only surviving attackers
and a delayed scene write can recover the final unlock. There are no recurring
assault respawns. Abandonment removes the attempt's attackers, soldiers and ship;
the `2005` retry gets its own encounter.

After checking in, walk into the ship's beam to depart directly. There is no
Bootcamp waypoint notification or travel menu, and no normal travel back to
Bootcamp afterward. Checking in alone does not board the player. If already
inside the beam while boarding was locked, step out and back in after check-in.
The hovering ship is replaced by the normal departure flight, followed by the wilderness
load and arrival flight at Alia Das. Departure commits the
existing Bootcamp progression/entitlement changes once, and the old private map
is released after destination persistence succeeds. The mission remains ready
for its existing Rogers turn-in in the wilderness.

Internal extraction trigger `60` uses the wreck's pad at
`(-225, 101.12099, -71)`, rather than the old hillside location. It is not a
discoverable waypoint. Bootcamp does not create an always-on hovering ship at
map entry. Other public-world dropship pads keep their existing visuals and menus.
The ready-state sequence clears a retained wreck before staging the ready ship
on reconnect.

`SeedWorldContent` installs the finale, ready-state cleanup, assault templates
and reinforcement bindings together. SQLite applies it on startup; MySQL
remains manually migrated. The consolidated branch history requires fresh
databases and has no separate mission publication step.

The new encounter positions and balance are authored server behavior, not
claimed retail measurements. Native acceptance must check the uphill advance,
ship/beam appearance, allied combat, locked/unlocked check-in, and direct boarding.

The bomb target uses tutorial wreck class `24586`, not dropship-crate class
`24911`. Its authored shared role and experience-owned effects drive reconnect
recovery; do not locate it by parsing a legacy scenario-key string. The
crash-site destination is the user-confirmed `(-225, 101, -71)` near the damaged
landing pad; authored actors and interactions are grounded against the current
Bootcamp navmesh. Conrad and the wreck use their shipped usable-state contract,
including disabled planting after the charge is placed.

The explosion uses native `Use` for state `31 -> 91`, whose client data binds
the transition animation and effect package `37160`. `ForceState` and
`UsableInfo` set a state directly and are not substitutes for this transition.
The server-side packet/lifecycle sequence is covered automatically; native
animation rendering and camera behavior still require an in-game check.

The bomb target is tutorial wreck class `24586` (mesh `20000024`), not dropship
crate class `24911` or extraction landing-pad class `29771`. Planting triggers
and interaction enable/disable steps use `24586` for both `1995` and retry
`2005`. The client uses argument `1` for Conrad and the wreck; their initial
usable states are TreasureDispenser closed (`200`) and Door closed (`31`).
Scenario reconstruction replaces old crate visuals and reapplies interaction
state after spawning, so a planted bomb does not become usable again on login.

Enabled scene objects are published as mouse-targetable even when their base
class's target flag is false. The wreck's class `24586` is one such class;
publishing only its enabled usable state left normal mouse interaction blocked.
Disabling the planting interaction also updates targetability.

Recovering Conrad's bomb now places one native Explosives Detonator
(template `11519`, class `20000064`) in Mission inventory. Planting consumes it;
timeout or abandonment removes it, and accepting retry `2005` supplies one new
bomb. Item changes share the mission/scene transaction and are published only
after commit. Full Mission inventory rejects pickup without completing the
objective. The issuance receipt prevents repeated reconnects from granting
extra bombs. `SeedWorldContent` uses native conversation-capable class `21081`
(`UsableNPCHumMCorpseV01`, mesh `29456`).

Apply both World and Char migrations to fresh databases; SQLite does so during
normal startup. No migration repairs older survivor layouts or adopts
intermediate branch bomb attempts. Ordinary reconnect preserves progress and
deadlines written by the current schema.

The old Conrad placement `(-102.4, 86.20677, 66.8)` was inside the client's
static trench wall (class `9707`), despite having a nearby navmesh polygon.
The C# Bootcamp data migration now places the corpse at `(-99, 86.33577, 74)`
and indicator `436` at ground `(-99, 86.32086, 74)`. Mission and experience actor
bindings use the same authored position. The new height accounts for class
`21081`'s native render minimum Y of `-0.01490639`; its X/Z placement is unchanged.

On a fresh migrated database, confirm the corpse is visible beside the missing team, the
marker agrees with its location, and clicking it opens the mission dialog with
a Continue button. Confirm that the search completes on proximity, opening or
closing the dialog grants nothing, and Continue advances objective `3` and
starts the bomb deadline. Check reconnect before opening, before Continue,
after pickup and after planting. Source-geometry checks use the new corpse's
measured footprint and connected approach; native-client rendering still needs
the manual check.

After rebuilding/restarting Game, check that the rifle image appears on login
without pressing E, that the bomb appears in the Mission tab, and that the wreck
can be right-clicked to plant it. Use a Thrax-dropped medpack as well: it must not
disconnect the client, must consume one item and must heal. Mission changes
use the normal database migration flow, not a publication script.

The migrated definition uses the client's supported objectives `2, 3, 1, 4`,
not the old experimental objective `10`. Runtime conversion of old objective
layouts has been removed for the fresh-database design.

### SQLite pass

The consolidated branch history is fresh-database-only. Its Char schema creates
flags directly without backfilling old qualifications. On the new database, verify that a
mission-set numeric flag survives reconnect, a failed transition grants no flag,
and deleting/recreating a character does not carry flags into the new character.
Bootcamp completion and skip store `CharacterFlagIds.BootcampComplete = 1`;
the starting-experience state and account skip entitlement remain separate.

1. Start from disposable SQLite character/world databases and apply migrations.
2. Launch `Rasa.Auth` and `Rasa.Game`; normal initialization installs mission data.
3. Confirm the Game log reaches `Server ready!`.
4. Create a fresh account and a fresh character.
5. In a cold client session, validate these ten scenarios and capture the
   matching server log window for each:
   - complete `1990 -> 1992 -> 1994 -> 1995 -> Alia Das`
   - reconnect at every mission boundary
   - reconnect at every objective boundary
   - die and respawn during each combat or timed mission
   - let `1995` time out, confirm the reset, then finish `2005`
   - disconnect during planting and confirm the original deadline survives
   - finish planting with five seconds remaining
   - complete Bootcamp once, then create a second character and skip
   - run two Bootcamp characters at once and confirm they never share actors,
     rewards, or mission state
   - confirm no unsupported packet, stale objective, duplicate actor,
     duplicate reward, or stranded character appears

### MySQL pass

Repeat the same ten scenarios against disposable MySQL character/world databases
after explicitly applying the MySQL migrations. See [migration operations](missions.md#start-the-servers).
If your local setup includes opt-in live MySQL verification, use it. Otherwise,
the automated suites still cover offline MySQL migrations and model parity.

### Startup-gate negative check

Before signing off startup validation, intentionally break one required
Bootcamp reference in a disposable database copy. Two safe examples are:

- remove NPC package `2564` (Van Valkenberg), or
- set `mission_objective_definition.client_body_text_id = 0` for mission `1995`
  objective `2`

Restart `Rasa.Game` and confirm:

- the log prints the exact diagnostic naming the broken mission/objective or
  missing package
- the log also prints `Mission content validation failed for required content;
  the Game server will not report ready.`
- `Server ready!` never appears

Restore the clean database before any other client run.

## Abilities, learned state and tray

The [ability reference](abilities.md) records the approved historical tables,
explicit engine policies, transactions, lifecycle guards and native acceptance
limits. Sprint ranks 1-5 and Lightning ranks 1-2 are executable. Rank 2 uses
[Blumster's supplied arc layout](https://github.com/InfiniteRasa/Rasa.NET/issues/92#issuecomment-5716373212).
Lightning ranks 3-5 still fail explicitly because the additional Sonic, stun
and storm contracts are not established; they do not run as incomplete ranks.

Run `LightningConsolidationTests`, `AbilityTrayConsolidationTests`, and
`ProgressionPersistenceTests` together for action-table bounds, arc selection,
learned rank/cost validation, migrated tray persistence, rollback, resource
snapshots, and replay protection. Selection adds one server-owned character
column through both SQLite and MySQL migrations. Effects receive actual elapsed
time on every map tick.

## Mission persistence and lifecycle

Run the focused mission checks with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Missions"
```

Mission progress belongs to persistent character IDs, not account IDs or party
rosters. The current journal row is keyed by `(character_id, mission_id)` and
has a separate assignment ID, pinned content revision, generation and version.
Reacceptance is a new assignment, not permission to consume the previous run's
timers, signals or grants. Objective rows/counters retain their normalized keys
and optimistic-concurrency checks.

`character_mission_history` retains terminal outcomes and reward claims when a
journal entry is cleared. Rewarded completions do not consume the 30-slot
journal limit; non-completed journal rows, including pending reward/failed rows,
still count. Clearing a rewarded mission cannot permit another nonrepeatable
reward, and a failed history entry is not a completion/reward claim.

`MissionJournalAdapter` restores current values against the selected definitions.
An unavailable or changed pinned revision is quarantined and preserved for an
explicit migration decision, not silently rebound to another content revision.
Inconsistent runtime state still follows explicit invalid-row handling; this
fresh-database design does not convert old experimental objective layouts.

Production definitions come from enabled, migrated World data. Optional
invalid and source-only content remains non-operational; required-content
defects block readiness. A new mission is authored as a C# data migration, optionally with a
registered typed script, not inserted into `MissionApplication` as special code.

The independent `Rasa.Missions` core evaluates typed progress rules and indexes
only relevant event/NPC bindings. Rules include exact events, waypoint/Logos
distinct sets and explicitly authored generic/item counters. Counters are not
test-only and are not inferred from prose. The Game application rechecks durable
state and stage-specific requirements before persisting changes. Protocol
adapters publish only after commit, preserving counter, objective and
mission-completable ordering and hiding only unrevealed `Inactive` objectives.

World scene state is independent of the active journal: `mission_scene`, inbox
messages, named timers, receipts, pending world effects, actor outcomes and
per-character credit deliveries preserve their own run/generation identities.
An authoritative committed objective can recover its queued scene input after
a crash. This is replay of recorded work, not invented progress for quests
accepted later.

Public-map logout/transfer detaches the client according to the encounter's
`Wait`, `Continue` or `Reset` policy. Deliberate default abandonment invalidates
the exact assignment-owned work in the same transaction as deletion; authored
failure transitions remain distinct. Generation checks prevent stale callbacks
or loot/grant intents from affecting a replacement run. Cleanup removes only
the terminated run's created actors, while a leased static actor completes its
own return/reset/respawn lifecycle.

Experience-owned actors survive the appropriate mission's journal cleanup.
Youngblood must still appear after rewarding and clearing `1994`, cold
reconnecting, then requesting his `1995` conversation. Persisted scene rows
alone are not sufficient evidence; test the actual actor and client flow.

Group credit is opt-in for supported kill/scenario events. Capture authoritative
objective requirements, active objective, map/range and participation eligibility
when the event occurs. Deliver only to those frozen assignments; a later party
join, acceptance or predicate change neither invents eligibility nor removes
earned credit. Delivery still checks exact assignment/generation and current
objective state. Personal actions and reward choices are not shared.

Mission turn-in keeps reward items, currencies, XP, terminal state and dependent
progress in one character transaction. A failure rolls back that turn-in.
Public actors remain shared and an exclusive escort may be unavailable to the
next group until it returns or respawns; there is no personal NPC phasing or
automatic party acceptance.

The implemented state mapping is:

- Active attempt: `MissionState.Active` with `Completeable = false`.
- Completable attempt: `MissionState.Active` with `Completeable = true`.
- Legacy pending reward attempt: `MissionState.Success` with `Completeable = false`.
- Completed attempt: `MissionState.Completed` with `Completeable = false`.
- Failed attempt: `MissionState.Failed`.
- Abandoned attempt: the active durable row is removed and `MissionDiscarded`
  is sent.

Login sends one `MissionStatusInfo` snapshot from durable state. Acceptance
validates the active client, registered NPC, persistent giver identity, current
map instance, stage requirements, duplicate/history state and the durable
30-slot journal capacity, then creates
the mission and all definition-authored objective/counter rows in one
transaction. Hydration combines persisted current values with immutable
definition metadata. Abandonment reloads the durable attempt and cannot remove
completed history from a stale client.

NPC objective completion requires an active operational definition, an
incomplete durable objective, a current-map NPC, and an exact NPC-package/player
flag completion binding. Completion and explicitly authored reveal/activation
transitions load as one tracked objective graph and flush once. Runtime state
and packets include only successor transitions that were durably applied.
Packets are emitted after commit in
`ObjectiveCompleted`, `ObjectiveRevealed`, `ObjectiveActivated`, then
`MissionCompleteable(true)` order.

Turn-in infrastructure reloads the character and mission inside one serializable
character transaction. Inventory, XP, supported currencies and completion state
commit together on `CompleteNPCMission`. `AssignNPCMission` only starts a new
mission; it never claims rewards. Normal turn-in moves directly from completable
`Active` to `Completed`, without persisting an intermediate `Success` state.
Runtime state and packets are published only afterward.
Sequential, reconnect and competing-client retries grant at most once. Staged
item entity IDs are released if planning or publication fails. SQLite fixtures
exercise objective persistence and competing reward transactions; offline
database checks cover MySQL model and migration SQL consistency. `selectionIdx`
must be `None` for rewards without selectable items and an in-range zero-based
integer when choices exist; non-null ratings are rejected. An older durable
`Success` row is advertised as `MissionComplete` and can resume through
`CompleteNPCMission`. `RewardNPCMission` remains supported for legacy reward
requests against `Success` rows.

NPC conversations derive dispense, objective-complete, and mission-complete
entries from the character's current lifecycle state. A rewarded mission is not
offered again by its giver or receiver.
Vending, auction, and clan behavior remains the fallback when no mission state
applies. The recovered opening Wilderness metadata identifies missions
`1449` (Wilderness Targets of Opportunity), `1407` (Too Close For Comfort) and
`1069` (Receptive Reception), including source-backed objective text identities
and four completion-conversation bindings. Missing ordinals, initial/required
states, transitions, indicators, counters, prerequisites, repeatability, and
rewards remain null/absent, so these definitions stay inactive and never appear
in conversation packets.

The source-only catalog also preserves these bounded progress slices:

- Mission `1069`, objective `1`: exact Logos `10` (entity class `7364`, map
  `1220`). Objectives `2` and `3` retain only completion conversations
  `(168,1,Completion)` for Solis creature `42`/spawn `184` and
  `(112,1,Completion)` for Apirka creature `43`/spawn `219`.
- Mission `1407`, objective `1`: completion conversation
  `(113,1,Completion)`, corroborated by Moawi creature `38`. Objective `10`
  retains presentation and `(168,1,Completion)` conversation metadata only.
  No escort entity, route, success/failure transition, or Ranger binding is
  inferred.
- Mission `1449`, objective `1`: distinct waypoints
  `{49,50,51,57,61,73,156}`. Objective `8`: distinct Logos
  `{1,2,6,9,10,23,24,28,38,49,53,56}`. Objectives `20` through `25` bind
  exact creature IDs `{82,83,84,79,80,75}` respectively. Objective `23`
  explicitly records that Horntail's map-1220 spawn is unresolved. Spawn
  resolution for objectives `20-22` and `24-25` remains unknown rather than
  inferred from corroborating rows. Every objective outside the exact listed
  rules has no progress rule.

All three definitions have `IsOperational == false`, carry no production
rewards, and produce no character mission/objective/counter writes or mission
packets for any progress event. Source preservation is not activation.

Run the progress and adapter boundary checks with:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Rasa.Test.Missions.MissionProgressTests|FullyQualifiedName~Rasa.Test.Missions.MissionDefinitionCatalogTests|FullyQualifiedName~Rasa.Test.World|FullyQualifiedName~Rasa.Test.Gameplay.LootConsolidationTests|FullyQualifiedName~Rasa.Test.Gameplay.ProgressionPersistenceTests"
```

## MySQL persistence acceptance

The repository's automated database checks compare models, snapshots, migration
ordering, and generated MySQL SQL without opening a live connection. The current
test project has no opt-in live MySQL category. Validate clean creation,
upgrades, rollback, concurrent mission rewards, and shared gameplay transactions
against an isolated disposable MySQL 8.0/8.4 instance before claiming live
provider acceptance. Do not point exploratory checks at a developer or
production database.

## Spawn timing and lifecycle

Seeded `RespawnTime` values are interpreted as **seconds**, converted once to
runtime milliseconds. The worker receives actual elapsed milliseconds for each
active-map tick. For example, seed value `20` means a 20-second cooldown.
An inactive map does not acquire an invented offline catch-up policy.

The existing limit of 64 creatures applies across the whole pool. Configured
minimum/maximum counts remain inclusive. Malformed ranges and missing templates
are reported without replacing them with default creatures. Manual modes are
not silently made automatic.

An animated wave retains its selected creatures until delivery rather than
rerolling them. Alive creatures, reserved deliveries and in-flight dropships
block another generation. The last such reference leaving starts cooldown;
remaining corpses do not delay the next generation. Each death and terminal
corpse removal changes counters once, independently of observer count.

## Acceptance still requiring external evidence

The checkout contains 77 generated `.nav` files under `navmesh`. `Rasa.Game`
loads matching files through `GameDataConfig.NavMeshPath`, uses them for ground
queries and creature paths, and falls back to straight-line movement when a map
has no usable file. The automated world suite covers map-link transfer and
lifecycle behavior, but it does not certify every generated route or real
terrain/collision interaction.

Native 1.16.5.0 client checks remain separate: two clients crossing visibility
boundaries, waypoint/dropship UI behavior, interruption/reconnect, and movement
against real terrain. These automated checks do not establish that
[InfiniteRasa/Rasa.NET#45](https://github.com/InfiniteRasa/Rasa.NET/issues/45) or the
remaining gameplay issues are complete. See the [protocol regression guide](protocol-testing.md)
for the separate first-map-load acceptance boundary.
