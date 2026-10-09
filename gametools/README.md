# Game tools

Two editors for a running Rasa.NET game server. Each is a single web page: open it in a browser
by double-clicking it. Nothing is installed and no web server is needed.

| File | What it edits | Where that lives on the server |
|---|---|---|
| `monster-flag-editor.html` (the page is titled Bane Flag Bench) | The flags of each creature class: species, substance, resistances, immunities, vulnerabilities and the rest of the client's 148 | `creature_class_flag` |
| `loot-table-editor.html` | Loot pools: named lists of items with a drop chance and a quantity, and which monsters have which | `loot_group`, `loot_group_item`, `creature_loot_group` |

Both still work on files as they always have (the flag editor's `creature_flags.json`, the loot
editor's project file and SQL export). With a settings file beside them they also read from and
write to a game server through its REST API.

| File | |
|---|---|
| `gametools.config.example.js` | The settings, to copy and fill in. |
| `gametools.config.js` | Your copy. It holds API keys, so it is not kept in git. |
| `gametools.js` | What the two pages share: reading the settings and calling the server. |

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

The loot pool tables are new. A SQLite world gets them when the server starts. A MySQL world
needs them made by hand before the server is started on this version.

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

- **Load from server** puts the server's pools in place of the ones in the page. Undo is offered,
  as it is after loading a project file.
- **Send to server** puts the page's pools in place of **every** pool and assignment the server
  has, so it asks first. They are written to the database in one transaction and the next kill
  rolls them.
- The server refuses the whole project if it names an item or a creature it has not got, and says
  which. It accepts, with a remark, a group with no items and a quantity above a stack.

What a pool does in game: when a creature that has pools is killed, every item of every one of
its pools is rolled on its own at its chance. One that comes up gives between its minimum and
maximum quantity, never more than one stack. The corpse also has the few credits every corpse
has. A creature with no pool drops what it did before pools existed. Mission drops are added as
before, and a creature a mission stages with loot of its own keeps that loot.

## Things to know

- **The lists inside the pages are snapshots.** The flag editor carries the creature classes as
  of 18 September 2026 and the loot editor the items and creature rows as of 6 October 2026. A
  creature or item added to the server since is not offered in the loot editor's pickers. If the
  server's pools use one, the page keeps it, shows it as "Not in this page's list" by its number,
  and sends it back unchanged.
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
| `GET /lootpools` | `{"format":"rasa-loot-tables","version":1,"source":"server","groups":[{"id":1,"name":"Thrax junk","note":"","items":[{"itemTemplateId":28,"chance":5,"minQuantity":1,"maxQuantity":3}]}],"assignments":[{"creatureId":118,"groupId":1}]}` |
| `POST /updatelootpools` | The same shape; replaces every pool and assignment. Answers `{"result":"updated","groups":1,"items":1,"assignments":1}`, with `"warnings":[…]` when there is something to remark on. |

Each speaks its editor's own file: what `GET` returns is a file the editor loads, and a file the
editor saves is a body `POST` takes, with whatever else is in it passed over.

A request that will not do is answered `400` with `{"error":"…","problems":["…"]}` and changes
nothing. `chance` is a percent from 0 to 100, kept to four decimal places; quantities are whole
numbers from 1 to 100000.

```
curl -H "X-API-Key: <key>" http://127.0.0.1:8104/lootpools
curl -X POST -H "X-API-Key: <key>" -H "Content-Type: application/json" --data-binary @loot-tables.json http://127.0.0.1:8104/updatelootpools
curl -X POST -H "X-API-Key: <key>" -H "Content-Type: application/json" --data-binary @creature_flags.json http://127.0.0.1:8104/updatemonsterflags
```
