# Game tools

Two editors for a running Rasa.NET game server. Each is a single web page: open it in a browser
by double-clicking it. Nothing is installed and no web server is needed.

| File | What it edits | Where that lives on the server |
|---|---|---|
| `monster-flag-editor.html` (the page is titled Bane Flag Bench) | The flags of each creature class: species, substance, resistances, immunities, vulnerabilities and the rest of the client's 148 | `creature_class_flag` |
| `loot-table-editor.html` | Canonical weighted profiles, groups, candidates, map/creature bindings and policies | `loot_profile`, `ordinary_loot_group`, `loot_item`, `creature_loot_binding`, `loot_map_policy` |

The loot editor uses `rasa-weighted-loot-profiles`, version 2. It rejects old
`rasa-loot-tables` percentage files, rather than interpreting percentages as
weights. Nonempty inactive archives block readiness and editor access. See
[the archive and explicit authoring boundary](../docs/loot.md#reference-legacy-flat-configuration-boundary).

Both still work on files as they always have (the flag editor's `creature_flags.json`, the loot
editor's project file and SQL export). With a settings file beside them they also read from and
write to a game server through its REST API.

| File | |
|---|---|
| `gametools.config.example.js` | The settings, to copy and fill in. |
| `gametools.config.js` | Your copy. It holds API keys, so it is not kept in git. |
| `gametools.js` | What the two pages share: reading the settings and calling the server. |
| `weighted-loot-project.js`, `weighted-loot-editor.js` | The weighted file/SQL protocol and plain JavaScript editor. Keep these beside the loot page. |

## Setting it up

### 1. On the game server

The tools use four endpoints of the game server's REST API. All four are off until you turn them
on. In the server's `appsettings.json` (or `appsettings.env.json`):

```json
{
  "ApiConfig": {
    "Rest": {
      "Enabled": true,
      "Port": 8104,
      "AllowedOrigins": [ "null" ],
      "Endpoints": {
        "monsterflags":       { "Enabled": true, "ApiKey": "<a key for reading>" },
        "updatemonsterflags": { "Enabled": true, "ApiKey": "<a key for changing>" },
        "lootpools":          { "Enabled": true, "ApiKey": "<a key for reading>" },
        "updatelootpools":    { "Enabled": true, "ApiKey": "<a key for changing>" }
      }
    }
  }
}
```

- **`AllowedOrigins`** says which web pages a browser may let talk to the API. `"null"` is what a
  browser calls a page opened from a file on disk, which is how these tools are opened. If you
  serve the folder from a web server instead, list that address (`"http://tools.example"`).
  Left empty, as it ships, no page can use the API at all.
- **Each endpoint needs a key.** The server never opens an endpoint to web pages if it answers
  without a key, so do not set `"Public": true` on these. Use one key for all four, or a
  read key and a write key as above, or the global `ApiConfig.Rest.ApiKey`.
- **`AllowedIps`** still applies: if it is set, the computer the browser runs on must be on it.
- The settings are picked up when the file is reloaded; no restart is needed.

Apply the reviewed archive/weighted migrations before using the loot editor.
SQLite startup applies pending migrations; MySQL uses the documented explicit
[migration workflow](../docs/setup.md#applying-migrations). Do not create a
second set of numeric pool tables.

### 2. Beside the tools

Copy `gametools.config.example.js` to `gametools.config.js` and fill it in:

```js
window.GAMETOOLS_CONFIG = {
  instance: { name: "Live server", url: "http://192.168.1.50:8104" },
  apiKey: "",
  endpoints: {
    monsterFlagsRead:  { path: "/monsterflags",       key: "<the key for reading>" },
    monsterFlagsWrite: { path: "/updatemonsterflags", key: "<the key for changing>" },
    lootPoolsRead:     { path: "/lootpools",          key: "<the key for reading>" },
    lootPoolsWrite:    { path: "/updatelootpools",    key: "<the key for changing>" }
  },
  timeoutMs: 15000
};
```

| Setting | |
|---|---|
| `instance.url` | The game instance: the address of its REST API, `http://` or `https://`, the server's address and `ApiConfig.Rest.Port`. Nothing after the port. |
| `instance.name` | What the pages call it, so a test server and the live one are not mistaken for each other. |
| `apiKey` | A key used for any endpoint that has none of its own below. |
| `endpoints.<name>.path` | The endpoint's path on the server. Only changes if yours differ. |
| `endpoints.<name>.key` | That endpoint's key. Empty uses `apiKey`. |
| `timeoutMs` | How long a page waits for the server. |

Reload the page after changing the file. To point the tools at another server, keep a second copy
of the folder with its own `gametools.config.js`.

## Using them

### Monster flag editor

The **Game server** panel is at the top of the left column.

- **Load flags from server** reads every creature class's flags. From then on "changed" means
  changed from what the server has. If you have changes that were not sent, the button asks you
  to click again before it replaces them.
- **Send N changed classes to server** sends only the classes you changed, each with its whole
  set of flags. They are written to the database and are in force at once: the server uses the
  new flags for every creature of the class from the next thing it decides. A game client is told
  a creature's flags when the creature is shown to it, so a creature already on a player's screen
  shows its new flags the next time it comes into view.
- The server refuses a request whole if it names a class that is not a creature's or a flag that
  does not exist, and says which. Nothing is changed in that case.

### Loot table editor

The **Game server** card is on the **Save and export** tab, and the bar at the top says whether
what is in the page is the same as what that server last had.

- **Load from server** puts canonical content in place of the browser draft. Undo is offered,
  as it is after loading a project file.
- **Send to server** asks for confirmation, replaces groups/candidates of named
  profiles and upserts bindings/policies. Unnamed server content is retained.
  One World transaction commits before the new validated immutable catalog is
  published. A failed save keeps the draft and does not mark it committed.
- **Profiles and groups** edits native template IDs, keys, provenance/evidence,
  group kind, admission basis points, roll bounds, positive integer weights and
  inclusive quantity bounds. Empty profiles deliberately give no ordinary items;
  groups must have candidates. Junk groups select at most one type, with only
  one Junk group per profile.
- **Map and creature bindings** uses both native IDs, not a global creature-only
  assignment. Bind an empty profile to suppress items without deleting a
  required binding. Existing required-map policies cannot be disabled.
- The server rejects invalid native templates/classes, mission quality, unusable
  inventory, quantities over native stacks, invalid keys/bounds/duplicates, and
  incomplete required-map coverage. It never clamps or normalizes invalid edits.
- Files and browser drafts preserve all five collections and metadata. Old flat
  browser storage is left intact and is not activated. Clear browser draft does
  not delete server rows. SQL exports touch only canonical tables in FK-safe
  order, preserve unnamed bindings, and never create schema. Review and validate
  SQL offline; applying SQL does not publish a running catalog. On any SQL error,
  roll back rather than continuing to commit.
- **SQL dialect** selects SQLite or MySQL for the preview, copy and download.
  The SQL header and `weighted-loot-profiles-<dialect>.sql` filename identify the
  provider. SQLite uses text literals with doubled apostrophes and unchanged
  backslashes. MySQL uses `CONVERT(X'<UTF-8 hex>' USING utf8mb4)` for every text
  value, preserving quotes, backslashes and Unicode with normal backslash
  escapes or `NO_BACKSLASH_ESCAPES`. It does not change SQL mode. MySQL target
  text columns must support `utf8mb4` for full Unicode. Programmatic export is
  `WeightedLootProject.sql(project, 'sqlite')` or `.sql(project, 'mysql')`;
  a missing or unsupported dialect is rejected.

Each group independently tests admission, chooses its inclusive roll count
uniformly, then samples relative weights with replacement. The preview shows
admission, conditional per-roll selection, expected selections and the chance
of no selection (including zero rolls). Quantity bounds do not alter selection
odds. Credits, authored overrides and mission drops remain separate. Frozen
Wilderness v1 balance is unchanged by opening the editor.

## Things to know

- **Native identities:** the flag editor carries its historical class snapshot.
  The weighted loot editor edits native IDs directly, with no stale picker list
  treated as validation. Unknown IDs in a file are retained for server
  validation, never discarded or converted to a substitute.
- **Keys cross the network as they are unless the API uses HTTPS** (`ApiConfig.Rest.Tls`). Keep
  the port on a network you trust or turn TLS on. With a self-signed certificate the browser has
  to be told to trust it first: open `https://<server>:8104/healthcheck` once and accept it.
- **`gametools.config.js` is a password file.** Anyone who can read it can change the server's
  loot and flags. Do not publish the folder with it inside.
- **Without `gametools.config.js`** the pages say so and work on files only.
- The monster flag editor loads two fonts from Google Fonts when it can and uses the system's
  fonts when it cannot.

## If a page cannot reach the server

The page shows what the server said. The ones that need explaining:

| The page says | Look at |
|---|---|
| No answer … that this page may read | The server is not running or not reachable at `instance.url`; or the endpoint is not `Enabled`; or it is set `Public`; or `AllowedOrigins` does not list `"null"`. A browser reports all of these the same way. The server's log names the reason for the last two. |
| The server refused the key | `apiKey` or the endpoint's `key` does not match the server's. |
| The server does not answer this computer | `ApiConfig.Rest.AllowedIps` does not include this computer's address. |
| The game server is not ready | It is still starting. |

## The endpoints

For anything else that wants to use them. A key goes in the `X-API-Key` header, or as
`Authorization: Bearer <key>`. Bodies are JSON sent as `application/json`, up to 8 MB.

| Endpoint | |
|---|---|
| `GET /monsterflags` | `{"schema":"rasa.creature_flags/1","source":"server","creatures":[{"class_id":6032,"class_name":"Bane_Amoeboid_v1","flags":[5,71]}]}`: every creature class, with the flags it has. |
| `POST /updatemonsterflags` | `{"creatures":[{"class_id":6032,"flags":[5,71,142]}]}`: each class named gets exactly these flags; a class not named is not touched. Answers `{"result":"updated","classes":1,"flags":3}`. |
| `GET /lootpools` | The canonical weighted contract below, with `source: "server"`. |
| `POST /updatelootpools` | The same contract. Answers `{"result":"updated","profiles":1,"groups":1,"items":1,"bindings":1,"mapPolicies":1}` (submitted counts). |

Each speaks its editor's own file: what `GET` returns is a file the editor loads, and a file the
editor saves is a body `POST` takes, with whatever else is in it passed over.

A request that will not do is answered `400` with `{"error":"…","problems":["…"]}`
and changes nothing. Persistence failure is `500`, with a log diagnostic and no
publication. Missing store or a nonempty inactive archive is `503`. Existing
key, IP, origin, endpoint-enable and 8 MB body controls are unchanged.

### Weighted loot contract

All five arrays, format and version are required. Every row must be an object
with every property shown below explicitly present and correctly typed: keys
and metadata are strings, IDs and numeric settings are JSON integers, and
`requireProfiles` is a JSON boolean. Omitted, null or wrong-type properties
return `400` with a collection/row/field diagnostic before the store is called.
They never become zero/false defaults. Explicit zero admission, zero rolls,
zero sort order, empty metadata and optional (`false`) map policies remain
valid where the canonical rules permit them.
Property names remain case-insensitive, but duplicate names in the body or a
row are rejected as ambiguous rather than letting deserialization overwrite
the presence/type-checked value.

Keys are lowercase ASCII
letters, digits, "_" or "-". Profile keys are 1..64 characters; group keys are
1..32. Profile metadata strings must be explicitly included (empty is permitted);
content revision is at most 32 characters. Kind IDs are Junk `1`, Ammunition `2`,
Consumable `3`, Equipment `4`.

```json
{
  "format": "rasa-weighted-loot-profiles",
  "version": 2,
  "profiles": [
    {"profileKey":"example","description":"Illustrative, not recommended balance","contentRevision":"example_v1","provenance":"Authored example","evidenceNotes":"Replace with reviewed native content"}
  ],
  "groups": [
    {"profileKey":"example","groupKey":"ammo","kind":2,"chanceBasisPoints":2500,"minimumRolls":0,"maximumRolls":2,"sortOrder":0}
  ],
  "items": [
    {"profileKey":"example","groupKey":"ammo","itemTemplateId":28,"weight":7,"minimumQuantity":1,"maximumQuantity":3,"sortOrder":0}
  ],
  "bindings": [
    {"mapContextId":1220,"creatureId":3,"profileKey":"example"}
  ],
  "mapPolicies": [{"mapContextId":1220,"requireProfiles":true}]
}
```

Weights and quantities are positive integers. Each group's total weight must fit
the signed integer RNG range; aggregate maximum rolls per profile are at most
32. Admission is `0..10000` basis points, not a percent field. Roll bounds may
include zero and must be ordered. Native stack/usability/quality and eligible
map coverage are authoritative server checks, not client guesses.

This is a scoped replacement: groups/items are complete for every profile
named in `profiles`; omitted groups/items of those profiles are removed.
Unlisted profiles and their content are retained. Bindings/policies are upserts:
omission does not delete them. To change a binding, include its exact
map/creature pair and new profile key. Use an explicitly empty profile for no
items. Archive configuration must be exported, reviewed and resolved by an
approved authoring decision before this interface can be used.

Focused offline checks (existing tools, no package install):

```powershell
node --test gametools\tests\weighted-loot-project.test.cjs
python gametools\tests\weighted-sql.test.py
```

The SQL tests execute SQLite directly and check MySQL UTF-8 hex encoding and
the portable statements offline (adapting only MySQL's CONVERT expression and
transaction opener for owned SQLite execution). They do not execute MySQL.
`tests\weighted-editor-review.browser.js` checks both key inputs' native
validity and pattern console errors; `tests\weighted-editor-sql.browser.js`
checks provider selection, exact copy/download content and retained metadata.

The browser fixtures in `tests` serve only an owned loopback port and mock
configuration/API. They never read a user's `gametools.config.js` or connect to a
game database. Stop the fixture server after verification.

```
curl -H "X-API-Key: <key>" http://127.0.0.1:8104/lootpools
curl -X POST -H "X-API-Key: <key>" -H "Content-Type: application/json" --data-binary @loot-tables.json http://127.0.0.1:8104/updatelootpools
curl -X POST -H "X-API-Key: <key>" -H "Content-Type: application/json" --data-binary @creature_flags.json http://127.0.0.1:8104/updatemonsterflags
```
