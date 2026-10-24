# Control point raids and reinforcements

The open world's control points change hands by more than a player's capture: the Bane raid the
points the AFS hold, the AFS raid the points the Bane hold, and the troops of both sides come back
to their camps and garrisons by dropship or beamed in. Server only: the retail client already has
the dropships, the beam-in, the messages and the point's claim effects. Code:
`Managers/BaneRaids.cs`, `Managers/Reinforcements.cs`, `Managers/OutpostWalls.cs`,
`Managers/GmRaidCommands.cs`; settings: the `Raids` and `Reinforcements` sections of
`appsettings.json` (`Config/RaidConfig.cs`, `Config/ReinforcementConfig.cs`), read again when the
file changes.

## Bane raids

- **When.** Each point the AFS hold is raided every 30 to 55 minutes (`RaidMinutesMin/Max`), the
  first time 6 to 12 minutes after the server starts and 8 minutes after the AFS take a point
  (`RaidAfterCaptureMinutes`). A raid begins only while a player who is playing - not under
  `.immune`, not AFK - is within 300 m of the point (`RaidRange`, `RaidCallersMustBePlaying`).
- **Who.** The troops of the point's own Bane garrison, or else the Bane troops of the pools nearest
  it: the rank and file of the ground's own kind and level, no bosses, no beasts.
- **How.** "They usually come in 3-4 waves": each wave is one Bane dropship (class 9268,
  `UsableCrSpawnerBaneDropshipV01`) of 4 troops (`RaidSquadSize`), two ships from the third wave on,
  one more ship for every three players near, and one more troop a ship for every 6 hours the AFS
  have held the point (`RaidRampHours`, up to 3). The next wave is called when the last is worn down
  to a third, or after 90 s; "THRAX REINFORCEMENTS ARE ARRIVING!!!" is shown across the screen. The
  first is announced in the Defense chat: "Thrax are gathering to attack a friendly outpost."
- **Where the ships come down.** Before a gate of the point's outpost, straight out from it at 26 m
  (then 32, 20, 38, 44 m, and a little to the side), each ship at the next gate; a second ship of a
  wave comes down beside the first (22, 36, 50 m to the side), not behind it. A point with no gate
  that has room is raided from round it: 40 to 75 m out, then out to 120 m at most, outside the
  walls of a point within them (a wall between the spot and the point, and the spot itself in no
  yard), with open sky over the ship's whole hull and a way on foot to the point. A ship with nowhere
  to come down waits for room, tried every 3 s for 45 s; a wave none of whose ships could land calls
  the raid off.
- **The claim.** A raider within 8 m of the point's object, not fighting, not walking and unhurt for
  3 s claims it: the object is locked to it with its contested effect, and the map is told "Bane
  claiming Control Point" (once in 30 s however often the claim is begun again under fire). Hurt,
  drawn into a fight, gone from the spot or dead, the claim is interrupted; 10 s at it
  (`RaidClaimSeconds`) and the point is the Bane's through the control point's own change of hands:
  the AFS garrison goes, "Bane took Control Point", the hospital and waypoint shut.
- **After.** The point's own Bane garrison is flown in; the raiders still standing hold the point
  beside it until they are killed, and while they do the point cannot be captured. A point with no
  Bane garrison of its own keeps the raid's troops as its garrison, which comes back like any other.
  A raid's kills are worth the point's minion prestige.
- **Defence.** While a raid is on, whoever of the holding side stands within 40 m of the point
  (`RaidDefendRange`) goes for the nearest raider there, the two nearest for a raider claiming it
  (`RaidDefendersOnClaimer`), and the point's garrison is not restocked until the raid is over.
- **The end.** Every wave down - the AFS held - or the point taken, or 20 minutes gone, when what is
  left of the raid is taken back.

## AFS raids

The AFS raid the points the Bane hold the same way, with less hope (`AfsRaids`): every 30 to 60
minutes, the first 10 to 20 minutes after a start and 30 minutes after the Bane take a point. Their
coming is counted down to the players near for two minutes (`AfsRaidWarningSeconds`) by the client's
own messages - "AFS Reinforcements En Route - 2 minutes remaining", 1 minute, 30 seconds, 10 seconds
(PlayerMessage 1369, 1371-1376) - then three waves of one AFS dropship (9269,
`UsableCrSpawnerHumDropshipV01`) of 8 soldiers, one every 40 s whatever is left of the last. The
soldiers are the point's own AFS garrison's, or the nearest AFS soldiers of the map, brought to three
levels over the middle level of the point's Bane (`AfsRaidLevelsBelow` -3). Near the point they go for
the Bane's healers first - Caretakers, Technicians, shield drones, the Machina - and a claim takes
15 s (`AfsRaidClaimSeconds`). Beaten off, "AFS Reinforcements Called Back" (1370). While players are
at a point the Bane hold and no raid is on, an AFS ship brings three of the point's own soldiers every
three minutes to fight beside them (`AfsSupport`).

## Reinforcements

A spawn pool restocks through its own arrival rows (`spawnpool_arrival`: a Bane pad or bay, a Bane
teleporter) as before. A pool with none of its own is given an arrival point of no table's for each
restock - never for the world's first stocking after a start, so the world starts stocked:

- a pool of Bane troops on one of the fifteen open-world maps (`ShipMaps`) comes by Bane dropship,
  down on open sky on its own ground; a pool whose ground is within an outpost's walls, or a control
  point's Bane garrison within walls, has its ship come down before the outpost's gates or outside
  its walls instead, and its troops walk in;
- the soldiers of an AFS control point garrison come by AFS dropship;
- troops that get no ship (no open sky, indoors, nowhere outside the walls just now), and the AFS's
  allies, beam in on their ground: their class's CREATURE_BIRTH (action 155), for the classes that have
  one;
- a control point's garrison that has just taken its point over arrives this way even on its first
  spawn - through its rows too - "a number of Bane will be dropshipped in";
- a pool whose `anim_type` asks for a ship, and a mission's own spawns, are as they were.

`spawnpool_arrival.kind` takes two more values for data that wants them: 3, an AFS dropship lands at
the point; 4, the creatures beam in at it.

An outpost's gates are the force fields that stand in its openings (`OutpostWalls.GatesOf`): the
point's own gate shields of the control point defences (`ControlPointFields.Of`, control_point_link
kind 7; [control point defences](control-point-defences.md)), joined at start-up by
`Managers/RaidDefences.cs` (without it, every faction field within 120 m of the point). With no field
set in an outpost's gates, raids land round it by the cover mesh's walls alone.

A raider held back at a gate by a shield that stops it - not yet in a fight, within 10 m of the
shield's sheet - strikes at it each second with its best ready attack (`ControlPointFields.Strike`:
an ordinary missile at the field, its cooldowns respected), and a raider whose step a point's shield
stops strikes at it whether or not it is fighting. A shield brought down stands again whole
`FieldRebuildSeconds` (120) later whatever the raid is doing, and is struck at again.

Settings: `BaneDropships`, `AfsDropships`, `BeamIn` (all on), `SkyClearance` 40 m, `ShipMaps` (empty:
the fifteen battlefields).

## Watching it

`.cp raids`, `.cp <id> raid [stop | in <minutes> | who]`, `.dropship <creatureId> [count] [bane|afs]
[here]` and `.dropship pool <poolId>` ([GM commands](gm-commands.md)). The server log (Debug) has a
line for each raid begun, each ship and where it came down and how far out, each claim and why it was
interrupted, and each raid's end.
