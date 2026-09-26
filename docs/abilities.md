# Abilities, learned state and tray reliability

This reference describes the bounded action-table implementation for maintainers.
It uses the approved community historical tables, not a claim of exact client
1.16.5.0 balance or native-client acceptance.

| Ability | Executable ranks | Current boundary |
| --- | --- | --- |
| Lightning, action 194 / skill 49 | 1-2 | Rank 2 includes one eligible secondary arc. Ranks 3-5 fail explicitly without spending Power or reserving cooldown; Sonic/stun/storm contracts remain unknown. |
| Sprint, action 401 / skill 165 | 1-5 | Toggle with elapsed-time adrenaline upkeep. |
| Medpack, item-provided action 419 | Authored item levels | Consumes the source medpack once, then applies its authored healing-over-time effect. |
| Other catalogue abilities | Not executable | Existing training and drawer mappings remain available; execution fails explicitly. The wider class/tier catalogue remains separate work. |

Remaining higher-rank Lightning packet layouts are tracked in
[InfiniteRasa/Rasa.NET#92](https://github.com/InfiniteRasa/Rasa.NET/issues/92).
Rank 2 adopts [Blumster's September 17, 2026 comment](https://github.com/InfiniteRasa/Rasa.NET/issues/92#issuecomment-5716373212)
at John's request. This is a contributor-supplied layout, not verified
native-client evidence. Keep ranks 3-5 blocked until their remaining fields
are supported by evidence.

## Historical baseline

[Lightning revision 34908](https://tabularasa.fandom.com/wiki/Lightning?oldid=34908),
dated October 9, 2008, specifies a 60-metre primary range, 0.5-second activation
and 1.2-second cooldown for every rank:

| Rank | Power | Primary electric base damage | Arc radius | Arc electric damage |
| --- | --- | --- | --- | --- |
| 1 | 25 | 180-240 | None | None |
| 2 | 50 | 240-300 | 12 m | 210 |
| 3 | 75 | 240-300 | 18 m | 90 |
| 4 | 100 | 240-300 | 24 m | 210 |
| 5 | 150 | 240-300 | 30 m | 210 |

Ranks 2-5 arc to one additional enemy. Ranks 3-5 add 50% Sonic damage without
specifying its calculation basis. Ranks 4-5 add a 50% chance of a three-second
stun without clearly identifying which targets receive it. Rank 5 adds a
30-metre enemy area effect, dealing 60-90 electric damage every two seconds over
six seconds; its center, first-tick timing and target cap are unspecified.
`HistoricalAbilities` retains all these values, including rank 3's arc damage
of 90, even where execution is blocked.

[Sprint revision 32299](https://tabularasa.fandom.com/wiki/Sprint?oldid=32299),
dated August 27, 2008, specifies:

| Rank | Speed increase | Maximum adrenaline consumed per second |
| --- | --- | --- |
| 1 | 20% | 1.5% |
| 2 | 30% | 1.35% |
| 3 | 40% | 1.25% |
| 4 | 50% | 1.0% |
| 5 | 60% | 0.9% |

Upkeep continues while standing still. The
[original manual, PDF page 23](https://archive.org/download/richard-garriotts-tabula-rasa-2007-destination-games-box-art/Richard_Garriott%27s_Tabula_Rasa_2007_Manual_text.pdf)
corroborates upkeep and advanced Lightning behavior.
[Deployment 10](https://web.archive.org/web/20090203215337/http://www.playtr.com/news/patch_notes/deployment_10_7232008.html)
corroborates a Mind-based Lightning bonus but supplies no coefficient or level
scaling. These sources do not establish the final damage at every level.

## Execution policies

The following are engine choices, not additional historical facts:

- Lightning reserves its cooldown at admission, including casts that are later
  interrupted. One pending activation prevents duplicates. Power is checked at
  admission and charged only after successful completion-time validation. No
  weapon ammunition is consumed.
- Rank 1 rolls inclusively from 180 through 240. Rank 2 rolls inclusively from
  240 through 300 and uses a fixed 210 base damage for its arc. Both electric
  components apply the source code's
  existing Mind-bonus calculation: 0.375% per Mind point above
  `2 * (level - 1) + 10`, never below zero. The old stats routine calculated this
  percentage without applying it to Lightning. The integrated engine adopts it as a
  compatibility policy. Decimal calculation floors the final integer damage;
  no additional level-scaling coefficient is invented.
- Eligible targets are living registered Bane creatures, not AFS creatures,
  self or other players. PvP and GM always-friendly attacks are not enabled.
  Range is three-dimensional Euclidean distance in existing world coordinates,
  treating one world unit as one metre. No collision/line-of-sight behavior is
  fabricated without map assets.
- Rank 2 selects at most one secondary at validated activation completion from
  the current map's existing creature cells, excluding the primary. It uses
  the same living, registered Bane eligibility and finite 3D distance, within
  12 metres of the primary, not the caster. Nearest distance, then lowest
  entity ID, is an explicit deterministic server policy, not a historical
  targeting claim. No eligible secondary is a valid primary-only rank 2 cast
  at the same 50 Power cost.
- Pending Lightning retains the original client, actor, map, target, action,
  rank, monotonic activation deadline and actor/target lifetime revisions.
  Recovery rechecks learned state, resources, hostility, range and active-world
  membership. Wrong-map dispatch, interruption, death/revival, departure,
  replacement targets and replay cannot grant another hit.
- The selected arc retains its target identity and action lifetime. After the
  primary resolves, source eligibility/lifetime and secondary identity,
  lifetime, hostility and range are checked again. A now-ineligible arc is
  omitted, not redirected to another enemy. Primary death does not by itself
  cancel an eligible secondary. Both use the existing armor, health, death,
  XP and loot paths. One trigger owns both hits and one recovery packet;
  repeated dispatch cannot apply either hit again.
- Successful waypoint, local-teleporter and dropship admission cancels pending
  Lightning and weapon recovery, settles and stops Sprint, and removes
  auto-fire before relocation. Selection and acknowledgement in one processing
  batch cannot resume that work. Rejected travel leaves it unchanged.
- `Actor.ActionLifetime` identifies combat work, including missiles and
  auto-fire. Death and world departure advance it along with the existing
  `AbilityLifetime` used for corpse eligibility. Local travel advances only
  the combat revision, preserving nearby owned loot in the same world lifetime.
- Lightning and weapon activation/reload are mutually exclusive pending work.
  Arming or replacing the active weapon cancels pending Lightning. An already
  active Sprint can coexist with weapons and Lightning.
- Sprint activates immediately, with no invented upfront cost or cooldown. A
  positive adrenaline balance is required. A valid second Sprint request stops
  it regardless of which learned rank was requested; changing rank requires
  stopping and starting again. Sprint accepts no target or the owner's ID.
- Sprint uses monotonic time since its own activation/update, not a nominal
  tick interval. Each settlement uses the current maximum adrenaline.
  Fractional debt survives ticks and toggles within the character lifetime,
  preventing rapid toggles from avoiding upkeep. Whole units are published to
  the existing integer resource field. Depletion clamps to zero and detaches.
- Sprint uses base movement modifier `1 + rank bonus`; no stacking with
  unimplemented movement effects or numeric combat-drain multiplier is added.
  It stops on death, loss of eligibility/learned rank, departure, character
  replacement or explicit detachment. Repeated detachment is harmless.
- Current resources, Sprint and cooldown deadlines remain runtime state.
  Login starts adrenaline empty while filling Health and Power; map transfers
  retain their existing resource-preservation behavior. These values are not
  represented as durably saved by the tray migration.

`GameEffectManager.DoWork` now receives every map tick's elapsed milliseconds,
including irregular ticks. Existing timed effects accumulate that elapsed time,
and all effects due in a tick expire. Toggle upkeep uses its own clock seam so
time preceding activation is not charged.

## Learned state and drawer persistence

### Item-provided abilities

`RequestPerformAbility` retains the source item's 64-bit entity ID through
recovery. The source must still be owned in personal inventory when the ability
lands. Source-item consumption and reagent requirements share one transaction;
when the source also satisfies a reagent row, it is counted once. A failed save,
interruption or item moved out of the inventory does not consume it or apply the
effect. Reloaded items retain their template ID so item-granted actions work
after reconnect.

Thrax can drop medpack template `44917` (action `419`, level `1`). Its World
properties specify a five-second effect, one-second interval and 60 health per
tick. The first tick is scheduled on landing, with subsequent ticks handled by
the existing effect worker. Other medpack levels use their own authored values.
Health remains capped at the recipient's maximum, and the normal action cooldown
applies. This does not add support for unrelated consumable ability modules.

### Initial weapon tray

After selecting the controlled actor, Game publishes a complete weapon-drawer
snapshot before the selected slot. This refreshes the initial tray without
requiring the player to press E; it does not regrant weapons or refill ammunition.
The starting pistol, template `17131`, receives its weapon profile through the
Bootcamp data migration. Its fixed values match the authored profile for
template `11557`, which uses the same class `27120`; no runtime fallback is needed.

### Learned abilities

New characters persist rank 1 in Lightning, Sprint, Firearms, Hand to Hand and
Motor Assist Armor as part of creation, consuming the five recruit skill points.
Lightning and Sprint start in zero-based drawer slots 0 and 1. Later logins load
the saved ranks and slots without reapplying these defaults. Mission training
reminders also preserve an already learned rank and the player's chosen slots.

[Logos ability revision 33138](https://tabularasa.fandom.com/wiki/Logos_ability?oldid=33138)
allows lower learned ranks. Validation therefore requires
`1 <= requested rank <= learned rank` and the matching canonical skill/ability
mapping, not equality with the highest learned rank.

Training preserves the existing 73-entry catalogue, class availability behavior,
level-point formula and cumulative rank costs `0, 1, 3, 6, 10, 15`. Requests must
contain a well-formed, nonempty batch with no unknown IDs, duplicate skills,
negative/zero/over-cap requested ranks, rank reductions or overspending.
Persisted ownership, level and learned state are checked inside the transaction.
The complete batch commits before runtime mutation and before Skills,
Abilities and available-points acknowledgements.

The [historical emulator's `manifestation.h`](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.h)
declares `abilityDrawer[5*5]`, and
[`manifestation_SendAbilityDrawerFull`](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.cpp)
serializes indices 0 through 24. These are the implemented wire bounds; they
are not inferred solely from the manual's 6-0 keys.

Set, clear, swap and selection use the same serializable character transaction
API as ammunition. Both sides of a move/swap commit atomically, including moves
from either empty side. Lower learned ranks are valid. A cursor stays at its
numeric slot when contents move or clear, and selecting an empty in-range slot
is permitted. No multiple-of-five restriction is inferred for selection.

Clearing accepts either `(None, None)` or numeric `(0, 0)` for ability/rank.
Those forms remain distinct in decoding; mixed missing/numeric fields and the
different Python `Zero` struct are not interpreted as a clear. The unused item
field must retain its existing `None` contract.

`character.current_ability_slot` is a new server-owned byte field with migration
default zero, not a fabricated client option. The SQLite and MySQL
`ConsolidatedCharacterSchema` migrations include it after the preserved
`development` history.
Drawer contents and cursor are sent together on assignment, including an empty
drawer. Invalid restored mappings, ranks, budget, slots or cursor fail closed
before world admission, with an explicit log and per-client disconnect rather
than a silently repaired grant.

Stale durable state, including a deleted character, produces a logged rejection
without runtime changes or success packets for selection, set/swap and training.
Expected transaction failures are limited to explicit `GameplayRejectionException`
guards, repository missing records, database/update errors, checked overflow and
unsupported capabilities. Unrelated application exceptions retain their identity
and stack instead of becoming ordinary loadout failures. Database-commit
acknowledgement loss remains an external ambiguity, not an exactly-once
persistence guarantee.

## Packets and unresolved wire contracts

Skills, drawer and preload packets snapshot their values. Power and Chi packets
snapshot current/max/refresh values; later resource mutations cannot rewrite a
queued notification. The source enum/comments identify Chi (attribute 5) as
adrenaline and Power as attribute 6. Native UI mapping remains an acceptance
gate.

Sprint uses the existing effect type 247 and attach/detach packets. Its attach
omits a fixed-duration tooltip instead of inventing an infinite-duration
sentinel. Rank, source and movement modifier come from authoritative state.
New observers receive the active effect and current movement modifier.

Lightning rank 1 retains the existing electric `LightningRecovery` shape,
including empty arc encoding. Rank 2 uses the same primary hit layout with
the associated secondary below. Recovery packets snapshot action fields,
primary identities/raw fields and each arc collection/raw value at
construction, before outgoing serialization.
`ActionFailed(actionId, actionArgId)` and
`ActionReuseTimerRestarted(actionId, actionArgId)` use the contracts documented
by `ActorManager`; their UI behavior still needs native observation.

The previous `LightningRecovery` and
[historical `missile_ActionHandler_Lighting`](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/missile.cpp)
both left `OnHitData.arcData` empty. The adopted
[contributor comment](https://github.com/InfiniteRasa/Rasa.NET/issues/92#issuecomment-5716373212),
introduced with "It looks like something this", supplies this structure:

```text
primary hit = (existing primary raw HitData, OnHitData)
OnHitData = ([arc entry, ...],)
arc entry = (target entity ID: WriteULong, raw HitData)
raw HitData = tuple(12), in order:
  DamageType: WriteUInt
  Reflected: WriteUInt
  Filtered: WriteUInt
  Absorbed: WriteUInt
  Resisted: WriteUInt
  FinalAmt: WriteLong
  IsCritical: WriteInt
  DeathBlow: WriteInt
  CoverModifier: WriteUInt
  WasImune: WriteInt
  empty target-effect list
  empty source-effect list
```

Each arc uses its own damage type, amount and raw fields, never the primary
missile's amount. Gameplay supplies the actual Mind-adjusted electric damage;
other raw values retain existing damage-path defaults. The serializer supports
multiple arc entries associated with each primary, although rank 2 gameplay
selects at most one. Arcs are not separate primary hits or separate recovery
packets. Both effect lists remain empty even if model objects contain effect
IDs; no effect IDs or undocumented fields are added.

The comment does not supply combined Sonic damage, stun or periodic storm
messages. Consequently ranks 3-5 remain rejected in full, not downgraded or
partially executed. Their gameplay ambiguities also remain as listed above.
Native captures or a verified client decoder are still needed to establish
native acceptance of the adopted arc layout and the missing contracts.

## Verification and acceptance

Run the focused cases after building:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~Rasa.Test.Gameplay.LightningConsolidationTests|FullyQualifiedName~Rasa.Test.Gameplay.AbilityTrayConsolidationTests|FullyQualifiedName~Rasa.Test.Gameplay.ProgressionPersistenceTests"
```

The fixtures exercise the final `AbilityManager` action-table entry points,
outgoing serialization, deterministic selection, migrated SQLite transactions,
rollback, reopening, and persisted tray selection. The database compatibility
suite separately compares both provider models with their snapshots and
generates MySQL migration SQL. No live MySQL acceptance is implied.

Arc cases include literal bytes and decoded layouts with wide entity IDs,
non-default raw fields, empty/multiple lists and immutable snapshots. Gameplay
cases cover rank 2 bounds and Mind flooring, primary-only completion, finite
12-metre range around the primary (including beyond the caster's 60-metre
range), deterministic selection and stale identity/lifetime exclusion.
Cancellation, travel, once-only resource/damage/reward paths and source
revalidation after primary death are exercised at manager seams.

Native Power/adrenaline HUD mapping, Sprint presentation,
Lightning primary/arc animation and reuse timing, and exact drawer clearing/selection behavior
remain external acceptance work. These changes do not complete the full
class/tier catalogue or native 1.16.5.0 acceptance.
