# Mission hazards: radiation and toxic gas

Some missions send players into radiation or toxic gas. The client has the effects:

- `RADIATION_POISON` (200)
- `SCRIPTED_PERCENT_DOT` (423), whose level 2 tooltip is "Exposure to an experimental toxic gas is
  causing damage over time."

The server applies them (`Managers/MissionHazards.cs`). This sits beside the lava
(`LavaDamage`, `LavaFlowFields`) and works the same way: the world's own damage, from nobody.
Lava is unchanged.

## The volumes

| Id | Mission | Map | Where | Effect | Damage every 2 s |
|---|---|---|---|---|---|
| 1 | 965 Mislaid Plans | 1497 Plateau | 40 m circle round the courier bag behind the wrecked Pinnacle Watchtower ("RADIATION WARNING!") | 200 level 1 | 70-105 Environmental |
| 2 | 1344 Chambers Of Horror | 2028 Abyss | Box over the rooms of Penumbra Section Five's twenty Machina chambers | 423 level 2 | 4 % of the victim's max health, Virulent |

Retail's volumes and numbers are lost, so both are our own. Each volume applies only to a
player who holds its mission (taken and not yet turned in, failed or abandoned). A test checks
that each one sits on its map's server navmesh.

**Note:** this server does not have missions 965 and 1344 yet. Their volumes stay dormant
until it does. Volumes a GM places (`.hazard place`) apply to everyone now.

## What it does

The check runs once a second per map, next to the lava:

- A player standing in a volume gets its effect. If the effect is already on, its end is pushed
  to 4 s from now, so it lingers a little after the player steps out.
- One effect of a kind at a time: two overlapping volumes of the same kind hurt once.
- The effect is the world's (`GameEffect.Environmental`), so Cure neither blocks it nor removes it.
- On each tick, the volume's damage is scaled by `Hazards.DamageScale` and reduced by the
  player's resistance to its damage type (Hazmat Armor resists Virulent) or stopped by an
  immunity. It is then dealt as a periodic hit with no attacker: a shield takes its share and
  armour goes first. A hit that takes the last of a player's health kills them, and nobody gets
  the kill.

Volumes never touch:

- the dead and the dying
- `.immune all`
- a GM who has not used `.allowdeath`
- creatures

When the volume is switched off, the mission is gone or the section is off, the effect comes off
at its next tick.

## Configuration

```json
"Hazards": {
  "Enabled": true,
  "DamageScale": 100
}
```

Changes are picked up on a config reload. A file without the section uses these defaults.

GM: `.hazard`, `.hazard place radiation|toxic [radius] [missionId]`, `.hazard <id> on|off`,
`.hazard remove <id>` ([gm-commands.md](gm-commands.md)). Tests: `World/MissionHazardsTests`.
