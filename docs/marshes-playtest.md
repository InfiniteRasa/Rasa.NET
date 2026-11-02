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

Set 2, Falcon Hold (migration `20271104000100_Marshes_falcon_hold`).

- [ ] 1673 It Lies in Ruins (Valverde Marshes): Retread Vincent (Retread City) offers it after Retreads to Retreads (1751), level 35. Report to Lieutenant Morrison at Falcon Hold (his topic, package 299). A marker at Morrison. Turn in at Morrison: 75000 XP, 3800 credits, one of Hazmat Vest 20951, Mech Vest 33193, Motor Assist Vest 29247, Reflective Vest 36188. Check the mission log shows it under "Battlefield (Marshes)".
- [ ] 959 Tastes Like Grubber (Valverde Marshes): Research Chemist Horsh (new, Falcon Hold, -168, 219.5, 593) offers it, no requirement, level 33. Any Swamp Grubber on the Marshes drops a Grubber Steak at 60% while it is open; two new pools of 3 Grubbers west of Falcon Hold (-250, 217, 600) and (-290, 214, 620), a marker on each. 10 steaks. Turn in at Horsh: 65000 XP, 5400 credits, one of Armor Module Resist Electric/Fire/Ice/Virulent [4]; he keeps the steaks.
- [ ] 961 Fixing the Food Chain (Valverde Marshes): Horsh offers it after 959, level 37. 10 bags of Treated Grubber Meat at the accept (needs one free slot). Twelve personal "Mox Feeders" on the map's Treeback Feeders round Falcon Hold, a marker on each; use any 10 (each once, one bag each): "Feeders filled" 0/10. Turn in at Horsh: 32500 XP, 3600 credits, one of 2 EMP Bomb VII, 2 Adrenaline Booster VII, 4 Fragmentation Grenade VII, 4 Cryogenic Grenade VII. Abandon: bags and feeders go; taken again, 10 new bags. Look at how the acceptor (class 21824) sits on the feeder model.

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
- 1673: on the Plateau revision with 1751 (the server keeps a required chain on one revision), in the Marshes category.
  Rewards by the band rule; TaRapedia's numbers after "Vest" (3, 4, 6, 3) are not used.
- 959: Horsh a copy of Researcher Vetter's row and look (class 3846, level 10) with the client name 3142 and a server-only
  package; steak drop 60% (no source gives a rate); two mission pools west of Falcon Hold (client log "western marshes").
  Level TR MMO DB's 33 (TaRapedia has none). "Modification" = the grade [4] Armor Modules (Ten Ton Hammer "Armor
  Modification").
- 961: the acceptor class 21824 UsableItemAccForeanTreebackFood (client name override 168 "Mox Feeders") on top of each of
  the 12 map Treeback Feeders; any 10. Markers unnamed (the client name 64 "Treeback Feeder" can key only one marker per
  objective). Rewards the lowest template of each client class in TaRapedia's stacks.

## Deviations (client/server limits)

- 498: the drop has no area on the server, so Thrax anywhere on the Marshes drop the canister.
- 539: the open world has too few Grubbers, Maws and Mox, so the mission adds its own pools; the fronds are a Grubber
  drop, not a plant. Maws lie under the ground until a player comes near (upstream `MawLurk`).
- 917: no Forean corpse class with a use has a real model, so Helka is a corpse model with a pack on it; Alia's original
  route is not known.
- 959: the drop has no area on the server, so Grubbers anywhere on the Marshes (also the eastern upstream pools and
  539's pools) drop steaks.
- 961: the Mox that swarm the troughs (client offer text, Ten Ton Hammer) are left out: no objective needs them.

## Server data that differs from the sources (not changed)

- Elder Toida, Sergeant Starling, Shaman Geli: upstream spots at the Paludos region marker (-321, 218, -7 and the upper
  level -321/-318, 226.5, -11). TaRapedia puts Toida at the Temple of Paludos (-205, 230, 28). Not moved.
- Overseer Cast'ka: upstream pool (125, 213.9, -364) is Ellatha's map-view value; converted it would be 160 m further
  south. Not moved.
- The navmesh inside Paludos is cut off from the marsh outside (no complete path from the Paludos waypoint out).
- Lieutenant Morrison: upstream spot by the Falcon Hold hospital (-142.7, 219.1, 618); TaRapedia (-205, 219, 600). Not moved.
