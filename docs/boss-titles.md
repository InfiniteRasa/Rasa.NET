# Boss titles: the bosses they ask for

`Rasa.Game/Managers/BossTitles.cs` gives the fourteen battlefield titles ("Abyss Assassin",
"Hunters Hunter", ...) to whoever has killed every boss on the title's list. A boss counts by the
client's name for it (`creature.name_id`) on one of the battlefield's maps.

Before migration `20270303000001_Title_bosses`, 32 bosses of those lists had no creature
anywhere in the world, so seven titles could not be earned:

| Title | Bosses on the list | Had a creature |
|---|---|---|
| Abyss Assassin | 9 | 1 |
| Thunderhead Hero | 9 | 2 |
| Crucible Assassin | 6 | 1 |
| Hunters Hunter (Pools) | 10 | 7 |
| Plateau Assassin | 5 | 4 |
| Desert Assassin | 6 | 5 |
| Descent Assassin | 8 (7 needed) | 1 |

The migration adds each missing boss as a creature on a point spawn pool of its own on its title's
map (`TitleBossesSeed`). It only inserts rows, ids 9300001..9300032 (creature, creature_stat, spawnpool;
creature_appearance for the Linkers and Vaasti) and 9310001..9310046 (attack rows of their own).
Down deletes exactly those rows. `BossTitles` itself is unchanged.

- **Stats** follow the boss rule of upstream's boss pools: the zone's boss level, health
  100 x level + 500, armor 200 + 5 x level.
- **Respawn** is `respown_time` 500 (50 s), the value upstream's boss pools use. Respawn pacing
  for bosses is a separate change.
- **Classes** are all retail entity classes. Each boss uses its family's `_Boss` class where one
  exists.
- **Positions** are chosen by us. The client data names these bosses and their missions but has
  no spawn point for any of them. Every position is on the server navmesh of its map (within 2.5 m),
  and every pool passes the server's startup check against safe ground (`ValidatePools`). For the
  first 18 bosses we also checked the retail client's floor.

| Pools | Bosses | Map |
|---|---|---|
| 9300001-9300006 | Nyogthu, Granitour Colony King, Overfiend Grimor, Maxxum Lagrotz, Overseer Hoktul, Preceptor Magoz | 2028 Abyss |
| 9300007-9300008 | Overlinker Hubor, Overlinker Rubor | 2190 Dybbukar |
| 9300009-9300012, 9300023, 9300024, 9300032 | Aramix, Arthox, Porthox, Irrogar, Juroaz, Esoobac, Warden Abomination | 1911 Thunderhead |
| 9300013-9300015 | Cainynix, Grumble, Oakril | 1304 Pools |
| 9300016-9300022 | Snuffy, Y'mas, Billy Bob, Preceptors Gissa, Kai Zul, Maalev, Vaasti | 2047 Descent |
| 9300025 | Overseer Jirex | 1734 Ashen Desert |
| 9300026-9300030 | Preceptor Molitor, Archfiend Krubb, Coercer Mungrel, Defiler Sedge, Maxxum Ugoretz | 1993 Crucible |
| 9300031 | Advanced Juggernaut Prototype | 1497 Plateau |

`TitleBossesSeed.cs` lists each boss's exact position. To see one in game, use `.tele` to go to its
map and position. Its pool spawns it when someone is on the map.

Tests: `World/TitleBossesTests` (the migration only inserts and Down removes exactly those rows,
the bosses load as hostile, the pools are on the navmesh and off safe ground) and
`Gameplay/BossTitlesTests` (every list now has all its bosses).
