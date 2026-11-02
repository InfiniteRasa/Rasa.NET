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

Set 3, the Harvesters (migration `20271104000200_Marshes_harvesters`).

- [ ] 881 Reaping the Harvest (Valverde Marshes): Researcher Vetter (Falcon Hold) offers it, no requirement, level 33. Seven personal gas harvesters in the swamp east of Falcon Hold, each a harvester body with a sample tap (a Bane item crate) at its base; use any five taps, one sample each (no room: the tap waits). One marker, the client name "Bane Harvester", round the field. Turn in at Vetter: 65000 XP, 5400 credits, one of Bio Vest 19246, Stealth Boots 38783, Hazmat Legs 20762, Reflective Gloves 35700. Look at the harvester body and the crate in the water, and the crate's name.
- [ ] 890 Hassling the Harvesters (Valverde Marshes): Retread Kline (Paludos) offers it after 881, level 37. Five personal destructible harvesters (500 HP) at five of 881's places: "Gas Harvesters Destroyed" 0/5. Turn in at Kline: 65000 XP, 5400 credits, one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Does a destroyed harvester show its wreck?
- [ ] 913 Beware: Buried Gas Lines (Valverde Marshes): Lieutenant Brazos (new, by the Paludos med tent, -310, 218.5, 125) offers it after 881, level 37. Eight detonators at the accept (two free slots). Four pipeline joints in the far east (open world), two personal bomb receptacles each, one detonator each, once. A "Pipeline Supply Point" marker on each joint. Turn in at Brazos: 140000 XP, no credits, Motor Assist Boots 28614 with every choice and one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Abandon: detonators and receptacles go.
- [ ] 968 A Butcher, a Baker, and a Caretaker (Valverde Marshes): Ranger Taavik (Paludos) offers it after 881, level 36. Kill the public bosses Caretaker Fik, Commander Sto and Commander Barzak, any order; a marker on each pool. Turn in at Taavik: 160000 XP, 7800 credits, one of Electric Blade 15475, Healing Disc 18367, Shotgun 14820, Fire Staff 49496.

Set 4, the Regulator (migration `20271104000300_Marshes_regulator`).

- [ ] 925 The Regulator: Part I (Valverde Marshes): Retread McCormick (Retread City) offers it, no requirement, level 35. Talk to Retread Wilson (new, -138, 217.7, -472, northeast of Retread City; package 969, the client's line "Scrap? ... Here - take this."): one Retread Scrap Metal (needs a free slot). A marker at Wilson. Turn in at Wilson: 35000 XP, 3700 credits, one of 2 Advanced Med Pack VII, 2 Power Charger VII, 4 Fragmentation Grenade VII, 4 Cryogenic Grenade VII; Wilson keeps the scrap. Abandon: the scrap goes.
- [ ] 1028 The Regulator: Part II (Valverde Marshes): Wilson offers it after 925, level 37. First check: does the real client open talk on a NEUTRAL creature (Kaav; the same open question as the Pools' Nakrinx)? Talk to Conscript Kaav, the Bane Traitor (new, neutral Thrax, 440, 223.7, -590, 13 m from the Bane Supply Depot door; package 970, "Quickly! Take this and go before they see you!"): the Bane Nanovirus (needs a free slot). A marker at Kaav. Turn in at Kaav: 35500 XP, 3700 credits, one of 2 Advanced Med Pack VII, 2 Armor Charger VII, 4 Concussion Grenade VII, 4 Fragmentation Grenade VII. Does the Thrax NPC look and talk right, and do the Bane nearby leave him alone? Abandon: the nanovirus goes.
- [ ] 1029 The Regulator: Part III (Valverde Marshes): Kaav offers it after 1028, level 37. Kill a Bane Strider: the new pool of one by the Depot (300, 222, -650; level 36, 10406 HP) or the boss Daddy Long-Legs; the Strider Power Core drops always while it is open; pick it up from the corpse. A marker on the pool (radius 20). Turn in at Wilson: 32500 XP, 5550 credits, one of Incendiary Injector Gun 45777, Electric Chaingun 15111, Pulse Pistol 14569, Pulse Cannon 15437; he keeps the core. Is the Strider too strong alone (the client: "maybe with friends")?
- [ ] 1030 The Regulator: Part IV (Valverde Marshes): Wilson offers it after 1029, level 35. A personal item giver (11114, a hum canister with the scrapper's model) at TaRapedia's /loc in the Paludos Ruins (-2, 217.2, 29); use it (10 s): the AFS Viral Scrapper (no room: the giver waits). A marker, the client name "Paludos Ruins", at the giver (radius 15). Turn in at McCormick: 70000 XP, 5550 credits, the Nanoviral Regulator with every choice and one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Does the canister draw and show its name? Abandon: the scrapper and the giver go.

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
- 881: seven harvesters 40-60 m apart in the swamp east of Falcon Hold (Ten Ton Hammer "the swamp to the east", "pick any
  five"), each a body (7908) with a Bane item crate (11116) as the tap 3 m east; a hidden objective counts five samples.
  Vetter stays at his upstream spot (Ten Ton Hammer's is 15 m off). Rewards TaRapedia's armour kinds and slots in the band
  that holds 33 (Ten Ton Hammer's older weapon list not used).
- 890: five destructible harvesters (7906) at 881's first five places, 500 hit points each. Rewards (no names on any
  source): the lowest Uncommon template of the 35 band for Bio, Graviton, Mech and Stealth.
- 913: open world (patch note under "Marshes"). Four joints of two receptacles (log 8, client 4 objectives), at the
  pipeline ends of the client map, one at TaRapedia's (734, 213, -149); any order. Eight detonators, the lower template
  of the class. Brazos a copy of Lieutenant Morrison's row and look with the client name 8595 and a server-only package,
  by the Paludos med tent. Fixed reward Motor Assist Boots (an old TaRapedia name, 33 band), the four to choose as 890's.
- 968: the public bosses count for anyone who kills them, any order; their upstream pools stay. "Conical" healing disc =
  the Direct disc (the Pools read "Radial" as Area), "Incendiary" = Fire, the shotgun with no element the lowest of the
  band.
- 925: Wilson a copy of Retread McCormick's row and look with the client name 8579 and package 969, 4 m from Ten Ton
  Hammer's "-138, -468" (the nearest navmesh point walked to from Retread City); no hostile Thrax by him (no objective
  needs them). One piece of scrap (the client gives no count). Credits and items TaRapedia's (Ten Ton Hammer's 1850 and
  an EMP Bomb are older).
- 925: 925's finishing text "So Wilson's got you running errands now, eh?" reads in McCormick's voice although TaRapedia makes Wilson the receiver; the client has no receiver data (kept as TaRapedia, ESTIMATE).
- 1028: Kaav a neutral Thrax NPC (the Pools' Nakrinx: class 11320, his hit points and armour), level 37, outside the
  Depot door (client "outside the Bane Supply Depot"; Ten Ton Hammer's "-20, -638" refueling station not used).
- 1029: the Strider the Pools' Strider row (class 9804, its attacks) at level 36 with the Marshes level-36 hit points
  (10406) and armour; one pool of one, 120 s respawn, between Kaav and Retread City. Core 100% (no source gives a rate),
  also from Daddy Long-Legs. No crates (Ten Ton Hammer's crates lose to the client's "take down a Bane Strider").
  "Incendiary" = Fire, "Pulse" = EMP, the Pulse Cannon the EMP rocket launcher; the band that starts at the named level.
  Credits TaRapedia's 5550 (TR MMO DB 1800).
- 1030: the giver class 11114 (same model as the scrapper item); rewards (no names on any source) as 890's; the
  Regulator a fixed item. Credits TaRapedia's 5550 (TR MMO DB 1800).

## Deviations (client/server limits)

- 498: the drop has no area on the server, so Thrax anywhere on the Marshes drop the canister.
- 539: the open world has too few Grubbers, Maws and Mox, so the mission adds its own pools; the fronds are a Grubber
  drop, not a plant. Maws lie under the ground until a player comes near (upstream `MawLurk`).
- 917: no Forean corpse class with a use has a real model, so Helka is a corpse model with a pack on it; Alia's original
  route is not known.
- 959: the drop has no area on the server, so Grubbers anywhere on the Marshes (also the eastern upstream pools and
  539's pools) drop steaks.
- 961: the Mox that swarm the troughs (client offer text, Ten Ton Hammer) are left out: no objective needs them.
- 881: the client's harvester item giver (10450) has the client's "Default" mesh, so each harvester is a body model with a
  Bane item crate as the sample tap; the client's objective has no counter, so a hidden objective counts the samples.
- 890, 913: the harvesters and the bomb receptacles are personal objects placed by the mission (the client map has none).
- 913: no explosion or pipeline wreck object; the nanovirus armour debuff in the marsh fog is left out (no area debuff on
  the server).
- 925, 1028, 1029, 1030: the texts say to carry all the parts to McCormick; each part is taken at its own mission's
  turn-in (Wilson, Kaav, Wilson, McCormick). The server binds a mission's issued items (AssignmentIssued: scrap, nanovirus) to that mission, so a later mission cannot take them. The core (a corpse drop, untagged) could be carried to McCormick (DevilsDenContent.cs:830-842 precedent), but a lost core would then block Part IV (1030 has no drop of its own), so each part goes in at its own turn-in.
- 1030: the Nanoviral Regulator is a plain item: it does not block 913's armour debuff, which the server does not have.

## Server data that differs from the sources (not changed)

- Elder Toida, Sergeant Starling, Shaman Geli: upstream spots at the Paludos region marker (-321, 218, -7 and the upper
  level -321/-318, 226.5, -11). TaRapedia puts Toida at the Temple of Paludos (-205, 230, 28). Not moved.
- Overseer Cast'ka: upstream pool (125, 213.9, -364) is Ellatha's map-view value; converted it would be 160 m further
  south. Not moved.
- The navmesh inside Paludos is cut off from the marsh outside (no complete path from the Paludos waypoint out).
- Lieutenant Morrison: upstream spot by the Falcon Hold hospital (-142.7, 219.1, 618); TaRapedia (-205, 219, 600). Not moved.
- Researcher Vetter: upstream spot (-147, 219, 614); Ten Ton Hammer (-161, 219, 609). Not moved.
- Retread Kline, Ranger Taavik: upstream spots at the Paludos region marker. Not moved.
- Caretaker Fik, Commander Sto, Commander Barzak: upstream pools at Ellatha's map-view values (as Cast'ka). Not moved.
