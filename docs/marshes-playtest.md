# Valverde Marshes missions: playtest list, assumptions and deviations

Missions that are implemented (data, migrations and tests pass) but not yet played through in the
game client. A checked box (`- [x]`) means the playtest is done and the mission works.

Format: `- [ ] <mission id> <name> (<zone>): <what to look at>`.

## Valverde Marshes

Set 1, Paludos (migration `20271104000000_Marshes_paludos`).

- [ ] 541 Roll out the Welcome Wagon (Valverde Marshes): Elder Toida (Paludos) offers it, no requirement, level 36. Kill the public Overseer Cast'ka (520032, pool at 125, 213.9, -364, respawn 500 s): "Boss Eliminated" 0/1. A marker on his pool. Turn in at Toida: 75000 XP, 5700 credits, one of Laser Chaingun 45762, Incendiary Net Gun 45812, Exothermic Polarity Gun 48680, Sonic Shotgun 14937.
- [ ] 498 Cannister Collection (Valverde Marshes): Sergeant Starling (Paludos) offers it, no requirement, level 35. Any Thrax Soldier or Grenadier on the Marshes drops the Gas Collection Canister at 25% while it is open; pick it up from the corpse. Markers on the three western Thrax pools. Turn in at Starling: 65000 XP, 5400 credits, one of Cryogenic Injector Gun 48108, Pulse Chaingun 15150, Electric Rifle 14738, Rocket Launcher 49136; he keeps the canister.
- [ ] 539 What's that Gas? (Valverde Marshes): Shaman Geli (Paludos) offers it, no requirement, level 34. Six new pools of 3 east of Paludos (two each of Swamp Grubber, Maw, Mox). A Grubber gives a hide always and a frond at 50%; a Maw a gland at 50%; a Mox a heart at 50%. 5 fronds, 10 hides, 5 glands, 10 hearts. The fern marker carries the client name "Calla Ferns". Turn in at Geli: 65000 XP, 5400 credits, one of Cryogenic Grenade Launcher 47944, Incendiary Leech Gun 47728, Electric Net Gun 15513, Pistol 14404; she keeps the parts.
- [ ] 917 Divided We Fall (Valverde Marshes): Elder Toida offers it, no requirement, level 35. Talk to Shaman Siva (new, in Paludos, -291, 218.5, 25), Warrior Veska (new, -38, 219.2, 49) and Ranger Alia (new, walks the road Plateau Point - Falcon Hold between -460, 217.5, 460 and -314, 217, 524), any order. Ranger Helka: a personal Forean corpse with a pack on it at (-564, 217.7, -53); use the pack (10 s) to complete objective 4. Turn in at Toida: 65000 XP, 5400 credits, one of Hazmat Boots 20335, Motor Assist Boots 28614, Reflective Boots 35571, Stealth Legs 39191. Abandon: the body goes; taken again, a new pack.

## Assumptions and choices (not from a source)

Rules carried over from the Pools batch (`docs/pools-playtest.md`):
- Client data first (titles, objectives, texts, packages, item classes), then TaRapedia, TR MMO DB, Ten Ton Hammer,
  Ellatha, archived pages. Each value without a source is ESTIMATE in the mission's evidence rows (confidence: client
  1.0, wiki 0.5, our reconstruction 0.3).
- Levels, XP, credits: TaRapedia, else TR MMO DB.
- Named weapon and armor rewards: the lowest-id Uncommon template of that kind and band (the manufacturer names are not
  in the client tables), one to choose.
- A public named creature (Cast'ka) counts for anyone who kills it.

Per mission:
- 541: none beyond the rules. Cast'ka stays at the upstream pool.
- 498: the canister as a personal drop at 25% (no source gives a rate) from the Marshes' Thrax Soldiers and Grenadiers;
  markers on the three Thrax pools in the west (the Marshes have none in the north). "Cryogenic" = Ice, "Pulse" = EMP,
  the Rocket Launcher the physical one.
- 539: ten Mox hearts (client title and TaRapedia) over the client log's five. Drop rates 50% (frond, gland, heart) and
  100% (hide); six mission pools east of Paludos (the client log names "east of Paludos"); both Marshes Maw rows drop
  glands. "Cryogenic RPG Launcher" = the Ice grenade launcher (no rocket launcher is Ice), "Incendiary" = Fire, the
  Hellstrom Pistol the physical one.
- 917: TaRapedia history's "-381, 671" for Alia read as a map-view value (/loc -381, 511, on the road). Alia an NPC squad
  of one on four road corners, there and back. Looks: Alia Forean archer, Siva Forean shaman, Veska Forean spearman,
  level 35. Helka a Forean warrior corpse model with a usable pack on it; the Forean Insignia item (21564) is not used.
- Markers carry their map (1454), unlike the Pools rows.

## Deviations (client/server limits)

- 498: the drop has no area on the server, so Thrax anywhere on the Marshes drop the canister.
- 539: the open world has too few Grubbers, Maws and Mox, so the mission adds its own pools; the fronds are a Grubber
  drop, not a plant. Maws lie under the ground until a player comes near (upstream `MawLurk`).
- 917: no Forean corpse class with a use has a real model, so Helka is a corpse model with a pack on it; Alia's original
  route is not known.

## Server data that differs from the sources (not changed)

- Elder Toida, Sergeant Starling, Shaman Geli: upstream spots at the Paludos region marker (-321, 218, -7 and the upper
  level -321/-318, 226.5, -11). TaRapedia puts Toida at the Temple of Paludos (-205, 230, 28). Not moved.
- Overseer Cast'ka: upstream pool (125, 213.9, -364) is Ellatha's map-view value; converted it would be 160 m further
  south. Not moved.
- The navmesh inside Paludos is cut off from the marsh outside (no complete path from the Paludos waypoint out).
