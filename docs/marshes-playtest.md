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

Set 5, the sentry tower of Far West Watch (migration `20271104000400_Marshes_sentry_tower`).

- [ ] 1874 Mortar Madness (Valverde Marshes): Sergeant McDonald (new, -124, 224.1, -307.6, the higher tree platform by the AFS sentry tower between the two control points) offers it, no requirement, level 37. Kill 3 Bane Mortars (592006; any on the Marshes counts): eight markers on the mortars of the eastern wall line (177, -554 to 462, 79). Turn in at McDonald: 130000 XP, 7200 credits, one of Motor Assist Helmet 121221, Stealth Legs 121222, Hazmat Helmet 121223, Reflective Helmet 121224. Do mortar kills count in the real client? Several mortars stand on walls: can they be hit from the ground?
- [ ] 1875 Under the Radar (Valverde Marshes): Warrior Dikah (new Forean spearman, -521, 222.1, -179, Far West Watch) offers it, level 37. A personal black box (22105 UsableItemDispBlackBox, argument 1) at the downed dropship (-51, 217.1, -366); use it: the AFS Dropship Black Box (no room: the box waits). A marker, the client name "Downed Dropship", at the box (radius 15). Turn in at Lt. Kiang (new, inside the sentry tower, -115, 230.9, -319.5, over the tower teleporter): 65000 XP, 5400 credits, one of Pulse Chaingun 121237, Shotgun 121238, Virulent Injector Gun 121239, Sonic Cannon 121240. Does the black box draw, open with a click and change state? Can the player reach Kiang in the tower? If not: move Kiang to the tower foot by teleporter 573 (-117.6, 217.3, -323.9). Abandon: the box and the giver go.
- [ ] 1876 Under the Radar - Part II, control point version (Valverde Marshes): Kiang offers it after 1875 (with 1877), level 37. A personal Bane computer (20631, argument 1) right outside the Bane Assertion Camp control point (101, 226.1, -289, Ten Ton Hammer); use it: Bane Troop Data (no room: the computer waits). No capture needed. A marker, the client name "Bane Terminal", at the computer. Turn in at Kiang: 140000 XP, 7400 credits, one of Bio 121241, Graviton 121242, Stealth 121243, Mech Boots 121244; he keeps the data. Sets flag 1876001 ("Part II done") for 1870 and 1879. Abandon: the data and the computer go.
- [ ] 1877 Under the Radar - Part II, Overseer Nelg version (Valverde Marshes): Kiang offers it after 1875 (with 1876), level 37. At the accept your own Overseer Nelg (new, level 38, 12000 HP) stands in his post east of Paludos (170, 224.7, 55, Ten Ton Hammer) and attacks; kill him and loot his corpse: Bane Troop Data (always, while the mission is open). A marker at his post. Turn in at Kiang: 140000 XP, 7400 credits, one of Bio 121245, Graviton 121246, Stealth 121247, Mech Boots 121248. Sets flag 1876001 too. Can the player reach the post (its floor is a navmesh part of its own)? If not: Nelg and his marker on the deck east of it (176.8, 224.68, 41.2). Is Nelg too strong alone? A corpse left to rot (120 s) leaves no data and no new Nelg comes, not after a relog or a server restart either: abandon and take it again.

Set 6, the sentry tower's second part (migration `20271104000500_Marshes_sentry_tower_two`).

- [ ] 1878 The Core of the Matter (Valverde Marshes): McDonald offers it after 1875, level 37. Three personal Bane item crates (11116, argument 3) before the Bane Supply Depot door: (420, 224.2, -590), (450, 223.7, -580), (465.6, 223.9, -587.2); open two: a Bane Power Core each (no room: the crate waits). One marker round the crates (radius 30). Turn in at McDonald: 150000 XP, 7600 credits, one of Bio Gloves 121229, Graviton Helmet 121230, Mech Gloves 121231, Stealth Gloves 121232; he keeps the cores. Is the base by the door the "Bane base near the supply depot"? Abandon: the cores and the crates go.
- [ ] 1879 This Calls For Destruction (Valverde Marshes): Kiang offers it once Part II (1876 or 1877) is done, level 37. At the accept: three Bane Computer Viruses (needs a free slot). Three personal Bane console switches (7468, argument 1) beside the map's Bane terminals south of the depot: (415, 222.2, -715), (410, 222.1, -735.2), (435, 222.1, -735); each takes one virus. Then 2 opens: a personal Bane generator (3857) at (400, 222.1, -730) with an Explosive Charge (7870, argument 1) 5 m east of it; use the charge: the generator blows. Markers round the consoles (radius 20) and at the generator. Turn in at Kiang: 65000 XP, 5400 credits, one of Stealth Vest 121233, Bio Vest 121234, Mech Legs 121235, Graviton Vest 121236. Do the consoles change state and the generator show as destroyed? Abandon: the viruses left and the objects go.
- [ ] 1870 Data Disaster (Valverde Marshes): McDonald offers it once Part II is done, level 37. A personal Bane CommLink terminal (10519, talks with package 2374) on the deck east of Nelg's post (178, 224.7, 46); talk to it and pick "AFS Troop placement info.": 1 done. A marker at the terminal. Turn in at McDonald: 70000 XP, 5550 credits, one of Reflective Legs 121205, Hazmat Legs 121206, Motor Assist Legs 121207, Mech Vest 121208. Ten Ton Hammer puts the terminal upstairs at (166, 232, 57): the roof of the post, a navmesh island. Can the player get up there? If so: move the terminal there. Abandon: the terminal goes.
- [ ] 1873 Need a Little Help Here (Valverde Marshes): Archer Thoung (new Forean archer, -126.9, 221.1, -343.3, by the sentry tower) offers it, no requirement, level 37. Four personal Bane item crates (11116, argument 3) by Nelg's post: three on its east deck (176.8, 224.7, 41.2), (181.8, 224.7, 45.8), (183.2, 224.7, 42.8), one below it (170, 216.1, 40); a Medical Booster Pack each. The four open 2-5: talk to Pvt. Brady (-110, 217.6, -320), Archer Zui (-110, 216.9, -335), Warrior Dahn (-125, 221.1, -335) and Elder Nez (-132.1, 221.2, -330.6) (new, packages 2375-2378, the client's thank-you lines); each takes one pack. Markers at the crates and at each soldier. Turn in at Thoung: 70000 XP, 5550 credits, one of Weapon Module: Crit Hit Bonus [4] 123161, Armor Module: Armor Absorption [4] 122951, Tool Module: Threat Reduction [4] 123141, Weapon Module: Armor Piercing [4] 123146. Do the soldiers show the thank-you line? Abandon: the packs and the crates go.

Set 7, Stalker Woods (migration `20271104000600_Marshes_stalker_woods`).

- [ ] 1863 Missing in Action (Valverde Marshes): Retread Jeska (new, -777, 241.2, -552, High Point Retreat) offers it, no requirement, level 36. A personal talking corpse (24072, female human, package 2367) lies on the lowland east of the camp (-700, 217.4, -560); talk to it ("her dog tags clearly proclaiming that she is Lt. Koffman"): 1 done. A marker at the body. Turn in at Jeska: 65000 XP, 5400 credits, one of 2 Advanced Med Pack VII, 2 Armor Charger VII, 4 Cryogenic Grenade VII, 4 Fragmentation Grenade VII. Does the corpse draw and open its talk? The triage's spot (-720, 241, -548) is on a navmesh island: if the player can reach it, move the body there. Abandon: the body goes.
- [ ] 1864 Light the Fires (Valverde Marshes): Retread Dudley (new, -784, 243.3, -523) offers it, no requirement, level 36. Five personal Forean fire pits (6137, argument 1, the client's states 55 -> 56), any order: NE of the camp (-740, 230.9, -500), west of Paludos (-420, 214.4, -40), (-430, 214.7, 20), (-410, 215.6, 70), at the camp (-786.5, 243.4, -522.9). A marker at each. Turn in at Dudley: 32500 XP, 3600 credits, one of 2 EMP Bomb VII, 2 Power Charger VII, 4 Concussion Grenade VII, 4 Fragmentation Grenade VII. Do the pits draw, and does state 56 show a fire? Fire 1's triage spot (-740, 243, -490) is on a navmesh island: if the player can reach it, move fire 1 there. Abandon: the fires go.
- [ ] 1869 Now Can You Hear Me? (Valverde Marshes): Dudley offers it, level 36. Three AFS Communication Spikes at the accept (needs a free slot). Three personal spike spots (7847, argument 3, states 119 -> 121, named "AFS Communication Spike"): north (-760, 237.3, -508), north-east (-730, 227.3, -490), east (-720, 217.9, -550) of the waypoint; each takes one spike. A marker at each. Turn in at Dudley: 70000 XP, 5550 credits, one of Healing Disc 121201, Ice Staff 121202, Sonic Pistol 121203, Ice Blade 121204. Does a placed spot show the spike? The triage's N (-784, 243, -470) and NE (-740, 243, -480) are on a navmesh island: if the player can reach them, move the spots there. Abandon: the spikes left and the spots go.
- [ ] 1871 Stop and Smell the Maw Flowers (Valverde Marshes): Retread Duvall (new, -593, 217, -438, the north road, the client's package 950) offers it, no requirement, level 36. Five personal shed flowers (22518-22522, argument 3) on the slope north-east of the camp around (-748, 232.5, -516), a Maw pool of two among them; take five Maw Flowers (no room: the flower waits). One marker round them (radius 15). Turn in at Duvall: 65000 XP, 5400 credits, one of Area Repair Tool 121225, Sonic Torqueshell Rifle 121226, Rifle 121227, Sonic Disperser 121228; he keeps the flowers. Do the flower givers (client classes marked DELETE) draw? North of the camp (-785, 243, -460) is a navmesh island: if the player can reach it, move the flowers and the Maw pool there. Abandon: the flowers and the givers go.

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
- Set 5 rewards: the client's item template list has a block of four Uncommon templates per mission whose kinds and levels
  match TaRapedia's lists in order (121221-121224 for 1874, 121237-121240 for 1875, 121241-121244 and 121245-121248 for the
  two versions of Part II, lower block for 1876). Set 5 uses these blocks, not the lowest-id rule of sets 1-4.
- 1874: McDonald a copy of Sergeant Elway's row and look (class 3846, level 10) with the client name 10327 and a
  server-only package; level 37 (no source). Any Bane Mortar on the Marshes counts; the eight markers are unnamed (the client
  name 414 "Bane Mortar Position" can key only one marker per objective).
- 1875: Dikah a Forean spearman with the spear (as Veska), level 37, a server-only package. Kiang a copy of Lieutenant
  Morrison's row and look with the client name 10326 and a server-only package, on the tower floor at TaRapedia's /loc.
  The black box giver at Ten Ton Hammer's point by the wreck, states 81 -> 82 (ESTIMATE: the other item givers'; the
  client has no state entry for 22105).
- 1876 and 1877: both offered after 1875, both can be done, either sets flag 1876001. 1876 needs no capture (the server's
  control points drive no mission objective). The data of the computer is 1876's own and does not count for 1877.
- 1877: Nelg a copy of Overseer Nivvik's row (class 10504, his attacks) at level 38 with 12000 HP (no source), armour 390,
  set down by the mission at the accept (no dropship shown), in his post, not on the roof.
- Set 6 rewards: the client's per-mission blocks as set 5 (121229-121232 for 1878, 121233-121236 for 1879, 121205-121208
  for 1870, each in Ten Ton Hammer's or TaRapedia's kinds). 1873 has no block: the four grade [4] modules of Bane
  Investigation (932, the user's reading of "Modification").
- 1878: level 37 (no source); three crates before the depot door, any two (the client "Gather 2"); a hidden objective
  counts them (the client's objective has no counter).
- 1879: the hub is the client map's terminal group south of the depot (no source places it); the consoles are Silent But
  Deadly's console class beside the map terminals; the generator is Silent But Deadly's outpost generator with its charge
  (3857 is not targetable in the client); the Generator Bomb item is not given. 2 opens only after 1 (the client log
  "then destroy").
- 1870 and 1879: offered on flag 1876001 (either version of Part II).
- 1870: no item (the objective has none); the terminal talks, as 932's CommLink terminal.
- 1873: Thoung and Zui Forean archers, Dahn a spearman, Nez a shaman (the server's Forean elders), level 37; Brady a copy of
  Private Remick's row and look (class 3846, level 10). The four soldiers at the tower's foot (no source places them). No
  requirement (TaRapedia none). A hidden objective counts the four packs.
- Set 7 rewards: 1869 and 1871 the client's per-mission blocks (121201-121204, 121225-121228, TaRapedia's kinds and levels
  in order); 1863 and 1864 TaRapedia's Class VII stacks, the lowest template of each class (925's, 1028's, 961's).
- Set 7 NPCs: Jeska, Dudley and Duvall copies of Retread Vincent's row and look with their client names (Jeska and
  Dudley server-only packages, Duvall the client's 950 of 1867/1904), at TaRapedia's /loc (Jeska's height 270 is no ground: on the camp floor at 241.2). Duvall stands where 888 and
  1867 will use him. Levels: 1863 and 1864 36 (TaRapedia), 1869 and 1871 36 (the camp's; no source).
- 1863: Koffman a talking female human corpse (24072, the Bootcamp corpse's way); no name shows on it (objects take their
  class name; the client's "Dead Retread Koffman" is a creature name).
- 1864: any order (the fire pit's states 55 -> 56 are the client's, usableaugmentationstate[9]).
- 1869: the spike spots are Mapping the Pools' beacon acceptor (7847) named by the client's 273; any order.
- 1871: receiver Duvall (the triage's Q19; TaRapedia's Vincent); the flowers are the client's Mawflower item givers
  (Medicinal Measures' way); a hidden objective counts five; one Maw pool of two (Marshes Maw 531052, 120 s) among them.

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
- 1876: "Engage control point to the east": no capture is needed; the computer stands outside the point (Ten Ton Hammer),
  and the server's control points cannot complete an objective.
- 1877: Nelg is not flown in by a dropship (the server's dropship arrivals are public); he stands in his post at the
  accept. His data is in his corpse only: a corpse that rots before the pickup needs an abandon and a new accept (a
  defeated scene actor is not set down again).
- 1876 and 1877: both offered and both can be done (ESTIMATE). TaRapedia's history has the Nelg objective until 2008-04
  and the control point one after, so retail likely had one version at a time.
- 1870: the terminal stands on the deck east of Nelg's post, not upstairs at Ten Ton Hammer's (166, 232, 57): the post's
  floor and roof are navmesh islands (no walk from the Bane Assertion Camp waypoint).
- 1873: the supplies are in crates by Nelg's post (Ten Ton Hammer), not dropped by a "northbound patrol" (client text);
  three crates on the walked-to deck and one below, as the post's floor is a navmesh island.
- 1863, 1864, 1869, 1871: the ground north of High Point Retreat (z above -500) and 60 m east of it is a navmesh island
  (no walk from the waypoint, nor from Duvall, Plateau Point, Paludos or Research Point); the triage's places there move to
  the nearest walked-to ground: Koffman to the lowland (-700, -560), fire 1 to (-740, -500), the north and north-east spikes
  to (-760, -508) and (-730, -490), the flowers to the slope north-east of the camp. If players reach the northern ground in
  the client, move them back.
- 1864: fires 2-4 are walked to from High Point Retreat, not from the Paludos waypoint (Paludos's navmesh is cut off).

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
