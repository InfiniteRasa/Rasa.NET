# Critical Death: the killing-blow stagger

The retail Critical Death works like this. A red skull appears over a creature, a player presses
melee next to it, a finishing animation plays, and the kill pays double experience
(`Managers/CritDeathManager.cs`). The window opens when a creature is stunned and at 8 % of its
health or less. Plain pistol and rifle fire stuns nothing, so in most fights the window never opens.

## The stagger

A player's killing blow leaves the creature staggering on one health instead of dead, with
probability `CritDeath.StaggerChancePercent` (default 25). A killing blow is a hit that would take
its health to zero; a tick of damage over time does not count. The creature's window opens at once,
with no stun needed. From then on it is the ordinary window:

- If the player or a squad mate finishes it, it is a crit kill: the experience is paid twice and the body is destroyed.
- If nobody finishes it within 6.9 s, it dies to whoever opened the window, as the blow would have killed it.

The armour goes as it would have. The hit reports what it dealt (armour plus health minus one).
Both damage paths cut the blow the same way: `MissileManager.DoDamageToCreature` (weapon fire,
swings, ability missiles) and `ActorManager.Damage` (abilities, bombs, constant fire).

These never stagger:

- creatures whose class carries `NO_CRIT_KILL`
- creatures an ability runs (`IsScripted`)
- guns on mounts (`Emplacements`)
- pets and summons of players
- creatures that are immune to the hit

Scene creatures and bosses can stagger.

## Morphed players: no Critical Deaths from mechs

A player in a Polymorph or a Personal Armored Unit (Angel, Vulcan, AFS Mech, Grendel, Orson;
`AbilityManager.IsMorphed`) has no Critical Deaths while `CritDeath.FromMorphedPlayers` is false
(the default):

- Their killing blows never stagger.
- The stun rule opens no window for them.
- Their finisher press is refused.

Their squad mates can still finish a window that someone else opened.

## Configuration

```json
"CritDeath": {
  "StaggerChancePercent": 25,
  "FromMorphedPlayers": false
}
```

Changes are picked up on a config reload. A file without the section uses these defaults.
`StaggerChancePercent: 0` and `FromMorphedPlayers: true` together give the behaviour from before
this change. Use `.critdeath` to see or change the rule in game until the next reload.

Tests: `World/CritDeathStaggerTests`.
