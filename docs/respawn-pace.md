# Respawn pace

How long a cleared spawn pool on a battlefield stays down (`Managers.RespawnPace`,
`appsettings.json`'s `RespawnPace`). Server only: the retail client sees creatures come back later, nothing else.

## Why

A pool's row says how long it stays down (`spawnpool.respown_time`, in tenths of a second). Of the 2595 rows in
the world database (at `20270123000000`):

| Row | Pools | What they are |
|---|---|---|
| 2 s | 719 | ~690 friendly NPCs (hospitals, vendors, trainers, AFS at posts); 23 Wilderness Bane rows `630xxx` (the Pinhole Egg-Layers, Light Bender snipers, Lightbenders, mortars, Machina, Predators, Overseer Skeev) |
| 50 s | 74 | 62 of them the battlefields' named bosses (`520xxx`: Kennilaxx, Caretaker Mordra, the Overseers) |
| 90 / 120 s | 1154 | the rank and file |
| 180 s | 46 | camps with a Thrax Commander |
| 300 / 500 s | 568 | turrets; the Wilderness Overseers and bosses (`580xxx`) |

The original servers kept a named boss down five to ten minutes (the wiki: Kennilaxx "5-10 Minutes", Caretaker
Mordra "approximately 10 minutes"), the rank and file a minute or two.

## What it does

With `Enabled`, on the maps it is kept on:

- A pool that has been cleared stays down at least its kind's time, drawn between the kind's two numbers each time.
  A row that says longer keeps its own time.
- A pool short of some of its creatures (`SpawnPoolManager.TopUp`) has them back no sooner than the pace of what was
  killed: a camp's grunt in a minute or so, its commander no sooner than an elite's time.
- Anything that puts a pool back at once by making it due (a GM's `.respawn <pool> now`, a public encounter's reset)
  still does: the pace is a timer started below zero.

| Kind | Default | What |
|---|---|---|
| `Mission` | 60-120 s | holds a creature a mission's objective asks to be killed (`CreatureKilled` by row, `CreatureClassKilled` by class): "Mama Miasma!"'s Egg-Layers, the Light Bender snipers, Horntail. Before every other kind, so the next player on the mission does not wait a boss's time |
| `Boss` | 300-600 s | a boss by its flags (`POWERLEVEL_6_BOSS`, `_7_SUPERBOSS`, the `UI_INDICATOR_` boss borders) or class name (`_Boss`, `Miniboss`, `_Queen`), or one a boss title names on its map (`BossTitles`: the title bosses, a Filcher or a Juggernaut prototype among them), whose row stands in one pool of its map |
| `Elite` | 180-300 s | such a row in several pools of its map (the camps' Thrax Commanders), or `POWERLEVEL_4_LIEUTENANT` |
| `Trash` | 60-90 s | everything else, an armed friend at a post too |
| `Service` | 30 s | a friend with an NPC package or no attack: vendors, trainers, medics |

A pool is of its strongest creature's kind.

Left out, keeping their row: a control point's garrison (it comes back together, `ControlPoints.HoldsBack`), a
scripted pool, a mission's or a scene's pools, escorts and followers, a public encounter's staged actor, a player's
private instance, a squad's instance (it has its own clock, `SquadInstances.RespawnMinutes`), and every map not listed.
`Maps` empty means the fifteen battlefields (`RespawnPace.Battlefields`); Bootcamp is not one of them, so its bridge
battle and Proving Grounds front still come back at once.

## Settings

```json
"RespawnPace": {
  "Enabled": true,
  "Maps": [],
  "TrashSecondsMin": 60, "TrashSecondsMax": 90,
  "EliteSecondsMin": 180, "EliteSecondsMax": 300,
  "BossSecondsMin": 300, "BossSecondsMax": 600,
  "MissionSecondsMin": 60, "MissionSecondsMax": 120,
  "ServiceSecondsMin": 30, "ServiceSecondsMax": 30
}
```

Without the section it is off and every pool comes back on its row, as before. A config reload applies from the next
pool that goes down. `.respawn` (see [GM commands](gm-commands.md)) shows a pool's kind, pace and when it is back.
