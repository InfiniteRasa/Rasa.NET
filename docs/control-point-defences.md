# Control point defences

The open world's control points (`ControlPoints`) defend themselves: shields in the gates of
their outposts, AFS soldiers at the gates, sandbags and counters, defenders that notice by
their role and call a fight to the rest of the base, and turrets that fire in bursts. Server
only: the retail client draws every part of it from what it already has.

Settings: `ControlPointDefence` in `appsettings.json` (`Config\ControlPointDefenceConfig.cs`),
picked up by a config reload. Every value has a default, so a file without the section has
everything on. GM: `.cpdef` (see [GM commands](gm-commands.md)).

## Gate shields

Data: `world_destructible` rows 9708001-9708082 (migration `Control_point_defences`), each
linked to its point by `control_point_link` kind 7 (`ControlPointLinkEntry.KindField`). The
Landing Zone fort's gate field upstream already placed (640016) is linked to point 4. 82 + 1
shields at 40 of the 41 points (the Iapyx has no gate within 140 m of it). Where each goes was
worked out from the gate's model and its field model's plug; see `ControlPointDefences`.

They are placed fields (`PlacedDestructibles`, `ForceFields`). On the point's own map channel,
`ControlPointFields` makes them the point's:

| Rule | Setting | Default |
|---|---|---|
| A shield is its holder's: AFS (A) lets players and friendly creatures through and stops hostile ones; Bane (B) stops players and friendly creatures. When the point changes hands every shield stands again whole on the new side. | `FieldsFollowOwner` | `true` |
| A destroyed shield stands again whole this long after (other placed fields: 45-60 s). | `FieldRebuildSeconds` | `120` |
| A damaged shield left alone this long powers up again ... | `FieldRechargeAfterSeconds` | `60` |
| ... from nothing to full in this long. | `FieldRechargeFullSeconds` | `300` |
| A fighting creature a shield stops strikes at it with its own best ready attack, through the ordinary missile path (`MissileManager` at the field's entity, `ForceFields.TakeHit`). | `CreaturesStrikeFields` | `true` |
| A hit on a shield by someone it stops calls that side's creatures within this range to the attacker. | `FieldAlertRange` | `80` m |

A private or shared copy of the map has the rows as plain placed fields on their rows' side.

For the raids (and anything else): `ControlPointFields.Of(point)`, `AnyUp`, `AllUp`, `PointOf(field)`,
`RebuildsInMs(field)`, `Opening(field)` (the gate's middle and the way through), the events
`Broken(field, point, byEntityId)` and `Rebuilt(field, point)`, `Strike(map, creature, field)` and
the `StrikesFields` predicate (default: a creature of no player's that is fighting).

## Garrisons

Data, same migration:

- 120 AFS soldier roles (creature 9700001-9701408, their attacks as creature_action with the
  same ids, creature_stat, the weapon in slot 13): for each of the fifteen open zones a Rifleman and
  an Infantryman (a man and a woman of each), a Shotgunner, a Machine Gunner, a Grenadier and a
  Field Gunner on the client's soldier bodies (classes 29423, 29424, 29432, 29433), at the zone's
  level. None has the Rushing Blow (a Logos ability); soldiers 39 and 97 lose theirs.
- 259 posts (spawnpool 9705001-9705259) behind the gates, behind the sandbags, at the counters and
  round the points, and Imperial Valley's (9707001), each in its point's AFS garrison (kind 2): there
  while the AFS hold the point, gone while the Bane do.
- The Wilderness AFS posts 49-51 and the Landing Zone's 117 are given the Wilderness roles.

## Defenders

`Defenders`: a FRIENDLY creature of a spawn pool that carries an attack (not a minion, a mission's
actor, escort or defender, an ambient creature, or one in a private instance).

| Rule | Setting | Default |
|---|---|---|
| On/off for everything below. | `Defenders` | `true` |
| A defender that walks notices out to its longest attack plus a margin, within a band. | `DefenderNoticeMargin`, `DefenderNoticeRange`, `DefenderNoticeMax` | 4, 30, 40 m |
| An AFS turret notices out to its gun's reach, never under 18 m. | `TurretNoticeMax` | `30` m |
| A fight a defender starts is called to its side's free defenders and turrets within this range (a turret only if it can reach the target); an answer does not call on. | `CallRange` | `45` m |
| A target further from the defender's post than its reach plus this is let go; when its fight is over, a defender off its post walks back and faces the way the post does. | `HoldPostMargin` | `5` m |

Every other creature notices out to 18 m, as before. With the defaults, 5 hostile camps lie within
30 m of an AFS turret and 9 within 30 m of a garrison post (of 1381 camps); those fight the base now
and then.

## Turret bursts

`Emplacements`: the AFS's turrets (4064, 11302, 23902) fire `BurstSizes` (3 or 5) rounds `BurstGapMs`
(250 ms, one creature think) apart, then pause for the rest of the time those rounds took one at a
time, so their rounds per minute are unchanged. Between shots a turret is sent its facing again when
its target has moved (more than 0.05 rad), so it stays turned to it. `TurretBursts: false` fires one
round a cooldown, as before.
