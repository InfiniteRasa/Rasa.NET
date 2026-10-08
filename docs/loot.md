# Ordinary loot: maintainer reference and tuning

Ordinary creature loot is World database content. Game loads and validates it
before accepting clients, then rolls an immutable in-memory catalog. Tune the
rows with a new paired World **data migration**, apply migrations normally and
restart Game. There is no live reload, admin UI or separate publication step.

This page covers ordinary drops, their rollout boundary and migration-based
tuning. [Corpse requests and pickup transactions](world-testing.md#owner-only-corpse-loot)
remain the existing interaction path. Credits amounts/timing, auto-loot filtering,
harvesting, equipment stats, treasure chests and endgame Epic rules are outside
this change.

## Reference: data contract

The five tables and their CLR rows live in
[`Rasa.DBL`](../src/Rasa.DBL/Structures/World). Row types use namespace
`Rasa.Structures.World`; the engine/catalog namespace is `Rasa.Loot`.

| Table / row type | Primary key | Other columns |
| --- | --- | --- |
| `loot_profile` / `LootProfileEntry` | `profile_key` | `description`, `content_revision`, `provenance`, `evidence_notes` |
| `ordinary_loot_group` / `OrdinaryLootGroupEntry` | `profile_key`, `group_key` | `kind`, `chance_basis_points`, `minimum_rolls`, `maximum_rolls`, `sort_order` |
| `loot_item` / `LootItemEntry` | `profile_key`, `group_key`, `item_template_id` | `weight`, `minimum_quantity`, `maximum_quantity`, `sort_order` |
| `creature_loot_binding` / `CreatureLootBindingEntry` | `map_context_id`, `creature_id` | `profile_key` |
| `loot_map_policy` / `LootMapPolicyEntry` | `map_context_id` | `require_profiles` |

Profile and group keys are stable tokens containing only
lowercase ASCII letters, digits, `_` and `-`, with lengths 1..64 and 1..32
respectively. They are not normalized. `content_revision` is a `varchar(32)`
metadata label, not part of the key or an activation selector. Description,
provenance and evidence notes are required text columns.

Groups reference their profile; candidates reference their composite group and
`itemtemplate.id`. Bindings reference `loot_profile`, `map_info.map_context_id`
and `creature.id`; map policies reference `map_info.map_context_id`. All these
foreign keys restrict deletion. Insert parents before children and delete owned children
before parents. `item_template_id` is not an item-class ID, and `creature_id` is
the native creature row (`Creature.DbId`), not a class or spawn-pool ID.
`map_context_id` identifies the World map context, not the client's map-template
ID or a runtime map instance.

The shared DBL enum
[`LootGroupKind`](../src/Rasa.DBL/Structures/World/LootGroupKind.cs)
uses byte values `Junk = 1`, `Ammunition = 2`, `Consumable = 3`,
`Equipment = 4`. Do not introduce another enum in Game or migration helpers.

### Roll units and order

[`OrdinaryLootProfile.Roll(Func<int, int, int> next)`](../src/Rasa.Game/Loot/OrdinaryLootProfile.cs)
returns an eager, read-only sequence of `(uint TemplateId, uint Quantity)`.
The delegate has minimum-inclusive, maximum-exclusive `Random.Next` semantics.

1. Catalog groups are ordered by `sort_order`, then ordinal `group_key`.
   Candidates are ordered by `sort_order`, then `item_template_id`. Signed
   sort orders and ties are allowed; these tie-breakers make them stable.
2. Admit each group independently when `next(0, 10000) < chance_basis_points`.
   Every group chance is in **basis points, 0..10000**: 100 is 1%, 7000 is
   70%, zero never admits and 10000 always admits.
3. Draw an inclusive selection count in `minimum_rolls..maximum_rolls`.
   Zero is allowed. For each selection, draw in `0..sum(weight)-1` and choose
   the first candidate whose cumulative weight exceeds the draw.
4. Draw that candidate's inclusive quantity in
   `minimum_quantity..maximum_quantity`. Selections use replacement; repeated
   templates remain separate entries in the roll result. Materialization uses
   this snapshot once, not a second roll when the corpse is opened or claimed.

Weights are positive relative integers, normalized **within their group**.
They are not percentages or basis points and do not change group admission.
Each weight total must fit `int`. Quantities must be positive, fit the native
stack and have an RNG-safe inclusive endpoint (at most `int.MaxValue - 1`).
Counts must be nonnegative and ordered; the sum of every group's configured
maximum is at most **32 selections per ordinary profile**, including disabled
groups. This is an implementation safety bound, not a recovered retail rule.

An ordinary profile may have no groups, but a group must have candidates even
when disabled. At most one group may be `Junk`, with `maximum_rolls <= 1`:
**one junk type**, not one total item or one unit. A selected junk candidate
may have quantity above one. Junk groups require native Junk-quality items;
other kinds cannot contain Junk, and no ordinary candidate may be Mission
quality. These ordinary caps do not rewrite authored Bootcamp loot or additive
mission drops.

### Lookup, coverage and readiness

[`IWorldUnitOfWork.OrdinaryLoot.Get()`](../src/Rasa.DBL/Repositories/World/IOrdinaryLootRepository.cs)
reads all five tables without tracking. Startup calls
`OrdinaryLootCatalog.Load(IWorldUnitOfWork)`, validates native item/class/entity
links, stacks, qualities, keys, bounds and coverage, copies the rows into
immutable models, and publishes only a valid catalog. This happens after native
entity/template loading and mission validation, before listener creation or
client acceptance. No database reads occur per kill.

`require_profiles = true` requires bindings for current ordinary,
reward-eligible hostile sources: populated spawn-pool slots, arrival and
control-point populations, and eligible enabled scene/experience actors without
explicit authored loot. Coverage follows native map/source links and existing
combat/reward rules, not comments or names. Friendly/nonrewarding actors and
the existing summoned-creature kill exclusion do not become new loot sources.

Invalid content or missing required coverage logs identifiable profile/group/
template or map/creature diagnostics and blocks readiness. At runtime,
`Resolve(uint mapContextId, uint creatureId)` also explicitly rejects a missing
binding on a required map with `GameplayRejectionException`. Neither failure
substitutes template 28, an empty profile or another profile.

Lookup precedence is explicit `ActorGameplayPolicy.Loot`, then the ordinary
map/creature binding, then legacy generation only for an unbound, non-required
map. An explicit binding opts content in even without a required map policy.
An empty bound profile or rejected group draws do not invoke legacy fallback.
Unconfigured other maps keep their existing item and credits distributions.

Explicit scene/Bootcamp authored overrides remain personal and bypass ordinary
lookup. Ordinary profiles retain the existing squad Free For All, Rotation and
Dice Roll allocation and credit sharers. Do not implement ordinary bindings by
setting `ActorGameplayPolicy.Loot`; that would bypass squad allocation.
Personal mission drops are appended **after ordinary squad allocation/rolls**,
remain reserved for the qualified killer and are revalidated at pickup.
Opening a menu alone does not advance collection progress.

Corpse quality is the highest actual materialized item quality by
`LootQuality.Rank()`, including the legacy fallback, not an independent draw or
the largest numeric quality ID. An item-empty corpse starts at Junk; subsequent
mission additions can raise its quality.

## Reference: frozen Wilderness v1

The paired schema migration is `20261202000100_Add_ordinary_loot_tables`.
The data pair `20261202000200_Wilderness_ordinary_loot_v1` follows it and calls
the frozen
[`WildernessOrdinaryLootV1`](../src/Rasa.DBL/Services/Preloader/Loot/WildernessOrdinaryLootV1.cs)
helper. It installs 27 profiles, 83 groups, 179 candidates, 36 bindings and one
required-map policy, for **Concordia Wilderness 1220 only**.
Its revision label is `wilderness_ordinary_loot_v1`.

| Group key / sort order | Regular chance | Explicit boss chance | Inclusive quantity |
| --- | ---: | ---: | --- |
| `junk` / 10, where native candidates were verified | 7000 (70%) | 9000 (90%) | 1 |
| `ammo` / 20, only on explicitly assigned profiles | 3000 (30%) | 4500 (45%) | Cartridges 12..24; batteries 8..16 |
| `consumable` / 30 | 1000 (10%) | 2500 (25%) | Medpack 1 |
| `equipment` / 40 | 800 (8%) | 6500 (65%) | 1 |

All initial groups have `minimum_rolls = maximum_rolls = 1`. Junk candidates
have equal weights. Regular equipment has two Normal candidates at weight 35,
one Uncommon at 25 and one Rare at 5: aggregate **70/25/5**, total 100.
Boss equipment has two Uncommon candidates at 65 and one Rare at 70:
aggregate **65/35**, total 200. These are conditional equipment-selection
ratios, not corpse-wide rarity chances.

For example, map 1220 creature `3` (native level-11 Thrax Soldier) binds to
`wilderness_v1_thrax_10_14`. Its equipment candidates, in order, are
`3061:35`, `11567:35`, `17133:25`, `17132:5`.
Grenadier `57` instead uses `wilderness_v1_thrax_5_9`; Glognar `82` uses
`wilderness_v1_thrax_5_9_boss`. Family, level band and boss choice are fixed
bindings, not a runtime name classifier or a claim that every zone actor is
low-level.

Verified native examples are Thrax junk templates `41665/41666`, Miasma
`41651/41652/42341/42342`, cartridges `28`, batteries `56`, and medpack `44917`
(class `21969`, native item action `419`). Thrax/technician profiles receive
cartridges; Lightbenders receive batteries. Amoeboid, Mox, Linker, Shield Drone,
mortar and Predator profiles deliberately omit junk because a species-specific
native identity was not established. Quest collectibles and generic substitutes
were not inserted. The frozen helper and
[independent native-content expectations](../src/Rasa.Test/Database/WildernessOrdinaryLootMigrationTests.cs)
hold the complete curated inventory; no runtime pool scans every native item.

## Reference: legacy flat configuration boundary

The incoming numeric `LootGroupEntry` is not `OrdinaryLootGroupEntry`. Its
historical `20261128000000_Add_loot_groups` migration still creates `loot_group`,
`loot_group_item` and `creature_loot_group`, with their original numeric keys
and independent per-item percentage chances.

After the incoming `20261201000000_Add_bootcamp_trainees` tail, both providers
run `20261202000000_Archive_flat_loot_pools`. It renames those three tables to
`legacy_flat_loot_group`, `legacy_flat_loot_group_item` and
`legacy_flat_creature_loot_group`, preserving every row and value. Rollback
restores the original names. The weighted schema and Wilderness content then
follow at `20261202000100` and `20261202000200`.

These are inactive compatibility archives, not a second loot backend.
The flat generator, publisher and write repository have been removed.
The existing unconfigured-map fallback remains inside the
ordinary generation path; archived pool assignments do not change it.

Any nonempty archive, including orphan item or assignment rows, blocks weighted
catalog readiness with table/count diagnostics. Export and review the original
rows, explicitly author weighted profiles, group admissions, candidates and
map/creature bindings, and validate them before deciding to clear the archives.
Clearing requires an approved authoring decision and a retained export; the
server never deletes or converts these rows automatically. A per-item percent
cannot be imported as a relative candidate weight.

The [gametools editor and REST routes](../gametools/README.md#weighted-loot-contract)
now read and save only `rasa-weighted-loot-profiles`, version 2. They reject the
old `rasa-loot-tables` percentage payload, never write the archives, and keep the
same readiness guard for archive contents. The historical row types remain
mapped solely to preserve archive diagnostics and migration/model metadata.

The live store makes private copies, validates the whole candidate with the same
native-item and coverage validator used at startup, persists changes in one
World transaction, then publishes the immutable catalog after commit. An edit
replaces the groups/items of explicitly named profiles and upserts bindings and
policies. Unnamed content remains; existing required-map policies cannot be
disabled. Database or validation failure changes neither durable content nor
the published catalog. Existing captured profiles and corpses remain intact.

## How-to: tune and deploy ordinary loot

1. Choose the exact installed profile/group/candidate keys and record the
   intended new values, previous values and evidence. Back up the database and
   test migration/rollback on a disposable copy, never a player's World or Char
   database. Preserve applied migrations, their helpers and
   `__EFMigrationsHistory`; do not consolidate history or change gameplay
   constants to adjust odds.
2. Prepare a new **data-only** migration pair for `SqliteWorldContext` and
   `MySqlWorldContext` with the repository's EF 9.0.20 tool, following
   [setup](setup.md#creating-migrations). Assign the same unique ID ordered after
   the latest installed/repository World history. This repository already has
   forward-dated IDs through `20261202000200`; scaffolding on October 7, 2026
   would generate an earlier timestamp. Do not use that raw timestamp.

   With the current tail, an example next ID is
   `20261202000300_Tune_wilderness_loot_v2`. Recheck both histories if later
   migrations have since been added. **Before building, scripting or applying
   the pair**, rename the scaffolded files in both `Migrations\SqliteWorld` and
   `Migrations\MySqlWorld` to
   `20261202000300_Tune_wilderness_loot_v2.cs` and
   `20261202000300_Tune_wilderness_loot_v2.Designer.cs`, and set each designer to
   `[Migration("20261202000300_Tune_wilderness_loot_v2")]`.
   Keep the scaffolded class/partial names and provider `DbContext` attributes
   consistent. A data-only migration has no model diff; do not add schema
   operations or hand-edit snapshots to disguise one. Never deploy the raw
   earlier scaffold timestamp ahead of the loot schema/content.
3. Put fixed `UpdateData`/`InsertData`/`DeleteData` operations in the new pair,
   using a new shared frozen helper for equivalent provider operations.
   This illustrative helper uses real v1 keys and reversible values:

   ```csharp
   using Microsoft.EntityFrameworkCore.Migrations;

   namespace Rasa.Services.Preloader.Loot
   {
       internal static class TuneWildernessLootV2
       {
           private const string Profile = "wilderness_v1_thrax_10_14";

           internal static void Up(MigrationBuilder migration)
           {
               migration.UpdateData("ordinary_loot_group",
                   new[] { "profile_key", "group_key" },
                   new object[] { Profile, "consumable" },
                   "chance_basis_points", 0);
               migration.UpdateData("loot_item",
                   new[] { "profile_key", "group_key", "item_template_id" },
                   new object[] { Profile, "equipment", 3061U },
                   "weight", 1);
           }

           internal static void Down(MigrationBuilder migration)
           {
               migration.UpdateData("ordinary_loot_group",
                   new[] { "profile_key", "group_key" },
                   new object[] { Profile, "consumable" },
                   "chance_basis_points", 1000);
               migration.UpdateData("loot_item",
                   new[] { "profile_key", "group_key", "item_template_id" },
                   new object[] { Profile, "equipment", 3061U },
                   "weight", 35);
           }
       }
   }
   ```

   Each provider migration delegates `Up`/`Down` to the helper. This disables
   that Soldier profile's 10% medpack group and changes one equipment candidate
   weight from 35 to 1. Equipment admission stays 800; its new total weight is
   66, so its Normal/Uncommon/Rare weights are 36/25/5 rather than 70/25/5.
   Changing only the chance needs only the first operation, not gameplay edits.
   These exact data changes, reloaded rolls, immutable old snapshots and Down
   restoration are covered by
   `FollowUpDataOnlyChanceAndWeightMigrationChangesReloadedRollsAndDownRestoresThem`.
   The example is not another installed migration or a recommended balance.

   For candidate additions/replacements, verify the native template/class link,
   usability, quality, stack and intended level/skill requirements. Author exact
   keys and stable sort orders; recalculate aggregate rarity weights when the
   candidate count changes. Delete only owned exact keys on rollback. Update
   profile revision/provenance/evidence metadata when it changes, with reversible
   operations too. Keep `WildernessOrdinaryLootV1` unchanged.
4. Check both models/snapshots, provider operations, SQL dependency order,
   fresh/upgrade/rollback behavior and the
   [focused runtime checks](world-testing.md#ordinary-loot-regression).
   To add a map, author its bindings and coverage first; only require profiles
   once every eligible source is covered. An explicit zero-group profile is a
   valid deliberate no-item decision, not a workaround for invalid content.
5. Deploy the matching build and migrations with players kept out during the
   update. SQLite's normal initialization applies pending migrations before
   Game loads the catalog. MySQL does not auto-migrate; use the existing
   [explicit migration workflow](setup.md#applying-migrations), including:

   ```powershell
   dotnet ef database update --project .\src\Rasa.DBL --startup-project .\src\Rasa.Game --context MySqlWorldContext
   ```

   Finish any other required database updates for that release, then restart
   Game and check readiness diagnostics. Already loaded catalogs and existing
   corpse contents do not change when rows are edited. A deployed rollback also
   needs a matching migration target/build and restart. Do not remove the loot
   schema/content beneath a Game build that requires it.

## Explanation: provenance and limits

Native migrated World rows establish item identities, template/class links,
qualities, stacks, requirements and source placements. They do not recover
creature-to-item probabilities. Every v1 profile's provenance says
`reconstructed starter balance; native IDs verified against migrated World`;
its evidence notes distinguish those facts from authored rates and pool choices.

The previously inspected official
[Deployment 13 notes](https://web.archive.org/web/20081017160137/http://www.rgtr.com/news/patch_notes/deployment_134_10152008.html)
support differing creature drop chances, and
[Deployment 14 notes](https://web.archive.org/web/20081115092604/http://www.rgtr.com/news/patch_notes/deployment_14_11112008.html)
support the one-junk-type limit. Neither supplies Wilderness distributions.
[Patch 1.4 notes](https://web.archive.org/web/20080203152047/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_01292008.html)
identify Wilderness bosses but do not give their loot odds. Initial admission
rates, quantities, equipment pools, ammunition assignments and boss tuning
remain reconstruction, not recovered retail balance.

[Deployment 15.7 notes](https://web.archive.org/web/20081220113726/http://www.rgtr.com/news/patch_notes/deployment_157_12132008.html)
describe endgame Epic/Epic Boss multipliers and equipment guarantees for those
specific tiers. They neither establish a low-level Wilderness baseline nor apply
to every named boss. No Legendary/Epic equipment or endgame guarantee is seeded
here. Higher native qualities remain possible in future validated content.

Automated evidence uses deterministic draws, native migrated creatures and
real server requests, with packet and durable-state assertions. It is not live
native-client rendering/delivery, live MySQL execution or retail-fidelity proof.
See [the acceptance boundary](world-testing.md#ordinary-loot-regression).
