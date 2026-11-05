# Valverde Marshes missions: playtest list, assumptions and deviations

Missions that are implemented (data, migrations and tests pass) but not yet played through in the
game client. A checked box (`- [x]`) means the playtest is done and the mission works.

Format: `- [ ] <mission id> <name> (<zone>): <what to look at>`.

## Valverde Marshes

Set 1, Paludos (migration `20271104000000_Marshes_paludos`).

- [ ] 541 Roll out the Welcome Wagon (Valverde Marshes): Elder Toida (Paludos) offers it, no requirement, level 36. Kill the public Overseer Cast'ka (520032, pool at 125, 213.9, -364, respawn 500 s): "Boss Eliminated" 0/1. A marker on his pool. Turn in at Toida: 75000 XP, 5700 credits, one of Laser Chaingun 120105, Incendiary Net Gun 120106, Exothermic Polarity Gun 120107, Sonic Shotgun 120108 (the client's reward block).
- [ ] 498 Cannister Collection (Valverde Marshes): Sergeant Starling (Paludos) offers it, no requirement, level 35. Any Thrax Soldier or Grenadier on the Marshes drops the Gas Collection Canister at 25% while it is open; pick it up from the corpse. Markers on the three western Thrax pools. Turn in at Starling: 65000 XP, 5400 credits, one of Cryogenic Injector Gun 120041, Pulse Chaingun 120042, Electric Rifle 120043, Rocket Launcher 120044 (the client's reward block); he keeps the canister.
- [ ] 539 What's that Gas? (Valverde Marshes): Shaman Geli (Paludos) offers it, no requirement, level 34. Six new pools of 3 east of Paludos (two each of Swamp Grubber, Maw, Mox). A Grubber gives a hide always and a frond at 50%; a Maw a gland at 50%; a Mox a heart at 50%. 5 fronds, 10 hides, 5 glands, 10 hearts. The fern marker carries the client name "Calla Ferns". Turn in at Geli: 65000 XP, 5400 credits, one of Cryogenic Grenade Launcher 120133, Incendiary Leech Gun 120134, Electric Net Gun 120135, Pistol 120136 (the client's reward block); she keeps the parts.
- [ ] 917 Divided We Fall (Valverde Marshes): Elder Toida offers it, no requirement, level 35. Talk to Shaman Siva (new, in Paludos, -291, 218.5, 25), Warrior Veska (new, -38, 219.2, 49) and Ranger Alia (new, walks the road Plateau Point - Falcon Hold between -460, 217.5, 460 and -314, 217, 524), any order. Ranger Helka: a personal Forean corpse with a pack on it at (-564, 217.7, -53); use the pack (10 s) to complete objective 4. Turn in at Toida: 65000 XP, 5400 credits, one of Hazmat Boots 120049, Motor Assist Boots 120050, Reflective Boots 120051, Stealth Legs 120052 (the client's reward block). Abandon: the body goes; taken again, a new pack.

Set 2, Falcon Hold (migration `20271104000100_Marshes_falcon_hold`).

- [ ] 1673 It Lies in Ruins (Valverde Marshes): Retread Vincent (Retread City) offers it after Retreads to Retreads (1751), level 35. Report to Lieutenant Morrison at Falcon Hold (his topic, package 299). A marker at Morrison. Turn in at Morrison: 75000 XP, 3800 credits, one of Hazmat Vest 120101, Mech Vest 120102, Motor Assist Vest 120103, Reflective Vest 120104 (the client's reward block). Check the mission log shows it under "Battlefield (Marshes)".
- [ ] 959 Tastes Like Grubber (Valverde Marshes): Research Chemist Horsh (new, Falcon Hold, -168, 219.5, 593) offers it, no requirement, level 33. Any Swamp Grubber on the Marshes drops a Grubber Steak at 60% while it is open; two new pools of 3 Grubbers west of Falcon Hold (-250, 217, 600) and (-290, 214, 620), a marker on each. 10 steaks. Turn in at Horsh: 65000 XP, 5400 credits, one of Armor Module Resist Electric/Fire/Ice/Virulent [4]; he keeps the steaks.
- [ ] 961 Fixing the Food Chain (Valverde Marshes): Horsh offers it after 959, level 37. 10 bags of Treated Grubber Meat at the accept (needs one free slot). Twelve personal "Mox Feeders" on the map's Treeback Feeders round Falcon Hold, a marker on each; use any 10 (each once, one bag each): "Feeders filled" 0/10. Turn in at Horsh: 32500 XP, 3600 credits, one of 2 EMP Bomb VII, 2 Adrenaline Booster VII, 4 Fragmentation Grenade VII, 4 Cryogenic Grenade VII. Abandon: bags and feeders go; taken again, 10 new bags. Look at how the acceptor (class 21824) sits on the feeder model.

Set 3, the Harvesters (migration `20271104000200_Marshes_harvesters`).

- [ ] 881 Reaping the Harvest (Valverde Marshes): Researcher Vetter (Falcon Hold) offers it, no requirement, level 33. Seven personal gas harvesters in the swamp east of Falcon Hold, each a harvester body with a sample tap (a Bane item crate) at its base; use any five taps, one sample each (no room: the tap waits). One marker, the client name "Bane Harvester", round the field. Turn in at Vetter: 65000 XP, 5400 credits, one of Bio Vest 19246, Stealth Boots 38783, Hazmat Legs 20762, Reflective Gloves 35700. Look at the harvester body and the crate in the water, and the crate's name.
- [ ] 890 Hassling the Harvesters (Valverde Marshes): Retread Kline (Paludos) offers it after 881, level 37. Five personal destructible harvesters (500 HP) at five of 881's places: "Gas Harvesters Destroyed" 0/5. Turn in at Kline: 65000 XP, 5400 credits, one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Does a destroyed harvester show its wreck?
- [ ] 913 Beware: Buried Gas Lines (Valverde Marshes): Lieutenant Brazos (new, by the Paludos med tent, -310, 218.5, 125) offers it after 881, level 37. Eight detonators at the accept (two free slots). Four pipeline joints in the far east (open world), two personal bomb receptacles each, one detonator each, once. A "Pipeline Supply Point" marker on each joint. Turn in at Brazos: 140000 XP, no credits, Motor Assist Boots 28614 with every choice and one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Abandon: detonators and receptacles go.
- [ ] 968 A Butcher, a Baker, and a Caretaker (Valverde Marshes): Ranger Taavik (Paludos) offers it after 881, level 36. Kill the public bosses Caretaker Fik, Commander Sto and Commander Barzak, any order; a marker on each pool. Turn in at Taavik: 160000 XP, 7800 credits, one of Electric Blade 120033, Area Healing Disc 120034, Shotgun 120036, Fire Staff 120035 (the client's reward block).

Set 4, the Regulator (migration `20271104000300_Marshes_regulator`).

- [ ] 925 The Regulator: Part I (Valverde Marshes): Retread McCormick (Retread City) offers it, no requirement, level 35. Talk to Retread Wilson (new, -138, 217.7, -472, northeast of Retread City; package 969, the client's line "Scrap? ... Here - take this."): one Retread Scrap Metal (needs a free slot). A marker at Wilson. Turn in at Wilson: 35000 XP, 3700 credits, one of 2 Advanced Med Pack VII, 2 Power Charger VII, 4 Fragmentation Grenade VII, 4 Cryogenic Grenade VII; Wilson keeps the scrap. Abandon: the scrap goes.
- [ ] 1028 The Regulator: Part II (Valverde Marshes): Wilson offers it after 925, level 37. First check: does the real client open talk on a NEUTRAL creature (Kaav; the same open question as the Pools' Nakrinx)? Talk to Conscript Kaav, the Bane Traitor (new, neutral Thrax, 443.5, 223.6, -589.5, on the depot floor 11 m from the Bane Supply Depot door, in the open, to-fix 21 and 24; package 970, "Quickly! Take this and go before they see you!"): the Bane Nanovirus (needs a free slot). A marker at Kaav. Turn in at Kaav: 35500 XP, 3700 credits, one of 2 Advanced Med Pack VII, 2 Armor Charger VII, 4 Concussion Grenade VII, 4 Fragmentation Grenade VII. Does the Thrax NPC look and talk right, and do the Bane nearby leave him alone? Abandon: the nanovirus goes.
- [ ] 1029 The Regulator: Part III (Valverde Marshes): Kaav offers it after 1028, level 37. Kill a Bane Strider: the new pool of one by the Depot (300, 222, -650; level 36, 10406 HP) or the boss Daddy Long-Legs; the Strider Power Core drops always while it is open; pick it up from the corpse. A marker on the pool (radius 20). Turn in at Wilson: 32500 XP, 5550 credits, one of Incendiary Injector Gun 120089, Electric Chaingun 120090, Pulse Pistol 120091, Pulse Cannon 120092 (the client's reward block); he keeps the core. Is the Strider too strong alone (the client: "maybe with friends")?
- [ ] 1030 The Regulator: Part IV (Valverde Marshes): Wilson offers it after 1029, level 35. A personal item giver (11114, a hum canister with the scrapper's model) at TaRapedia's /loc in the Paludos Ruins (-2, 217.2, 29); use it (10 s): the AFS Viral Scrapper (no room: the giver waits). A marker, the client name "Paludos Ruins", at the giver (radius 15). Turn in at McCormick: 70000 XP, 5550 credits, the Nanoviral Regulator with every choice and one of Bio Boots 18750, Graviton Boots 23549, Mech Helmet 8855, Stealth Vest 10637. Does the canister draw and show its name? Abandon: the scrapper and the giver go.

Set 5, the sentry tower of Far West Watch (migration `20271104000400_Marshes_sentry_tower`).

- [ ] 1874 Mortar Madness (Valverde Marshes): Sergeant McDonald (new, -124, 224.1, -307.6, the higher tree platform by the AFS sentry tower between the two control points) offers it, no requirement, level 37. Kill 3 Bane Mortars (592006; any on the Marshes counts): eight markers on the mortars of the eastern wall line (177, -554 to 462, 79). Turn in at McDonald: 130000 XP, 7200 credits, one of Motor Assist Helmet 121221, Stealth Legs 121222, Hazmat Helmet 121223, Reflective Helmet 121224. Do mortar kills count in the real client? Several mortars stand on walls: can they be hit from the ground?
- [ ] 1875 Under the Radar (Valverde Marshes): Warrior Dikah (new Forean spearman, -514, 221.9, -153 (moved clear of the Maw pool 540037, to-fix 4; TaRapedia -521, 222, -179), Far West Watch) offers it, level 37. A personal black box (22105 UsableItemDispBlackBox, argument 1) at the downed dropship (-51, 217.1, -366); use it: the AFS Dropship Black Box (no room: the box waits). A marker, the client name "Downed Dropship", at the box (radius 15). Turn in at Lt. Kiang (new, at the sentry tower's foot, -116.5, 216.9, -311, to-fix 22; retail had him in the tower room, which has no way up): 65000 XP, 5400 credits, one of Pulse Chaingun 121237, Shotgun 121238, Virulent Injector Gun 121239, Sonic Cannon 121240. Does the black box draw, open with a click and change state? Can the player reach Kiang in the tower? If not: move Kiang to the tower foot by teleporter 573 (-117.6, 217.3, -323.9). Abandon: the box and the giver go.
- [ ] 1876 Under the Radar - Part II, control point version (Valverde Marshes): Kiang offers it after 1875 (with 1877), level 37. A personal Bane computer (20631, argument 1) right outside the Bane Assertion Camp control point (101, 226.1, -289, Ten Ton Hammer); use it: Bane Troop Data (no room: the computer waits). No capture needed. A marker, the client name "Bane Terminal", at the computer. Turn in at Kiang: 140000 XP, 7400 credits, one of Bio 121241, Graviton 121242, Stealth 121243, Mech Boots 121244; he keeps the data. Sets flag 1876001 ("Part II done") for 1870 and 1879. Abandon: the data and the computer go.
- [ ] 1877 Under the Radar - Part II, Overseer Nelg version (Valverde Marshes): Kiang offers it after 1875 (with 1876), level 37. At the accept your own Overseer Nelg (new, level 38, 12000 HP) stands on the deck east of his post (181.5, 224.6, 44; to-fix 25: the post is an island the client cannot walk to) and attacks; kill him and loot his corpse: Bane Troop Data (always, while the mission is open). A marker on him. Turn in at Kiang: 140000 XP, 7400 credits, one of Bio 121245, Graviton 121246, Stealth 121247, Mech Boots 121248. Sets flag 1876001 too. Does he come at you on the deck, and can you loot his corpse (within 6 m)? Is Nelg too strong alone? A corpse left to rot (120 s) leaves no data and no new Nelg comes, not after a relog or a server restart either: abandon and take it again.

Set 6, the sentry tower's second part (migration `20271104000500_Marshes_sentry_tower_two`).

- [ ] 1878 The Core of the Matter (Valverde Marshes): McDonald offers it after 1875, level 37. Three personal Bane item crates (11116, argument 3) west of the Bane Supply Depot door, clear of its map link (to-fix 14): (420, 224.2, -590), (428, 224.2, -578), (434, 224.2, -580) (to-fix 24); open two: a Bane Power Core each (no room: the crate waits). One marker round the crates (radius 20). Turn in at McDonald: 150000 XP, 7600 credits, one of Bio Gloves 121229, Graviton Helmet 121230, Mech Gloves 121231, Stealth Gloves 121232; he keeps the cores. Is the base by the door the "Bane base near the supply depot"? Abandon: the cores and the crates go.
- [ ] 1879 This Calls For Destruction (Valverde Marshes): Kiang offers it once Part II (1876 or 1877) is done, level 37. At the accept: three Bane Computer Viruses (needs a free slot). Three personal Bane console switches (7468, argument 1) beside the map's Bane terminals south of the depot: (415, 222.2, -715), (410, 222.1, -735.2), (435, 222.1, -735); each takes one virus. Then 2 opens: a personal Bane generator (3857) at (400, 222.1, -730) with an Explosive Charge (7870, argument 1) 5 m east of it; use the charge: the generator blows. Markers round the consoles (radius 20) and at the generator. Turn in at Kiang: 65000 XP, 5400 credits, one of Stealth Vest 121233, Bio Vest 121234, Mech Legs 121235, Graviton Vest 121236. Do the consoles change state and the generator show as destroyed? Abandon: the viruses left and the objects go.
- [ ] 1870 Data Disaster (Valverde Marshes): McDonald offers it once Part II is done, level 37. A personal Bane CommLink terminal (10519, talks with package 2374) on the deck east of Nelg's post (178, 224.7, 46); talk to it and pick "AFS Troop placement info.": 1 done. A marker at the terminal. Turn in at McDonald: 70000 XP, 5550 credits, one of Reflective Legs 121205, Hazmat Legs 121206, Motor Assist Legs 121207, Mech Vest 121208. Ten Ton Hammer puts the terminal upstairs at (166, 232, 57): the roof of the post, a navmesh island. Can the player get up there? If so: move the terminal there. Abandon: the terminal goes.
- [ ] 1873 Need a Little Help Here (Valverde Marshes): Archer Thoung (new Forean archer, -126.9, 221.1, -343.3, by the sentry tower) offers it, no requirement, level 37. Four personal Bane item crates (11116, argument 3) by Nelg's post: three on its east deck (176.8, 224.7, 41.2), (181.8, 224.7, 45.8), (183.2, 224.7, 42.8), one below it (170, 216.1, 40); a Medical Booster Pack each. The four open 2-5: talk to Pvt. Brady (-110, 217.6, -320), Archer Zui (-110, 216.9, -335), Warrior Dahn (-125, 221.1, -335) and Elder Nez (-132.1, 221.2, -330.6) (new, packages 2375-2378, the client's thank-you lines); each takes one pack. Markers at the crates and at each soldier. Turn in at Thoung: 70000 XP, 5550 credits, one of Weapon Module: Crit Hit Bonus [4] 123161, Armor Module: Armor Absorption [4] 122951, Tool Module: Threat Reduction [4] 123141, Weapon Module: Armor Piercing [4] 123146. Do the soldiers show the thank-you line? Abandon: the packs and the crates go.

Set 7, Stalker Woods (migration `20271104000600_Marshes_stalker_woods`).

- [ ] 1863 Missing in Action (Valverde Marshes): Retread Jeska (new, -777, 241.2, -552, High Point Retreat) offers it, no requirement, level 36. A personal talking corpse (24072, female human, package 2367) lies on the lowland east of the camp (-700, 217.4, -560); talk to it ("her dog tags clearly proclaiming that she is Lt. Koffman"): 1 done. A marker at the body. Turn in at Jeska: 65000 XP, 5400 credits, one of 2 Advanced Med Pack VII, 2 Armor Charger VII, 4 Cryogenic Grenade VII, 4 Fragmentation Grenade VII. Does the corpse draw and open its talk? The triage's spot (-720, 241, -548) is on a navmesh island: if the player can reach it, move the body there. Abandon: the body goes.
- [ ] 1864 Light the Fires (Valverde Marshes): Retread Dudley (new, -784, 243.3, -523) offers it, no requirement, level 36. Five personal Forean fire pits (6137, argument 1, cold 56 -> lit 55, the client's fire state), any order: NE of the camp (-740, 230.9, -500), west of Paludos (-420, 214.4, -40), (-430, 214.7, 20), (-410, 215.6, 70), at the camp (-786.5, 243.4, -522.9). A marker at each. Turn in at Dudley: 32500 XP, 3600 credits, one of 2 EMP Bomb VII, 2 Power Charger VII, 4 Concussion Grenade VII, 4 Fragmentation Grenade VII. Do the pits draw, and does state 56 show a fire? Fire 1's triage spot (-740, 243, -490) is on a navmesh island: if the player can reach it, move fire 1 there. Abandon: the fires go.
- [ ] 1869 Now Can You Hear Me? (Valverde Marshes): Dudley offers it, level 36. Three AFS Communication Spikes at the accept (needs a free slot). Three personal spike spots (7847, argument 3, states 119 -> 121, named "AFS Communication Spike"): north (-760, 237.3, -508), north-east (-730, 227.3, -490), east (-720, 217.9, -550) of the waypoint; each takes one spike. A marker at each. Turn in at Dudley: 70000 XP, 5550 credits, one of Healing Disc 121201, Ice Staff 121202, Sonic Pistol 121203, Ice Blade 121204. Does a placed spot show the spike? The triage's N (-784, 243, -470) and NE (-740, 243, -480) are on a navmesh island: if the player can reach them, move the spots there. Abandon: the spikes left and the spots go.
- [ ] 1871 Stop and Smell the Maw Flowers (Valverde Marshes): Retread Duvall (new, -593, 217, -438, the north road, the client's package 950) offers it, no requirement, level 36. Five personal shed flowers (22518-22522, argument 3) on the slope north-east of the camp around (-748, 232.5, -516), a Maw pool of two among them; take five Maw Flowers (no room: the flower waits). One marker round them (radius 15). Turn in at Duvall: 65000 XP, 5400 credits, one of Area Repair Tool 121225, Sonic Torqueshell Rifle 121226, Rifle 121227, Sonic Disperser 121228; he keeps the flowers. Do the flower givers (client classes marked DELETE) draw? North of the camp (-785, 243, -460) is a navmesh island: if the player can reach it, move the flowers and the Maw pool there. Abandon: the flowers and the givers go.

Set 8, Gun Control (migration `20271104000700_Marshes_gun_control`).

- [ ] 1865 Gun Control (Valverde Marshes): Retread Lou (new, -776, 241.5, -542, High Point Retreat, the client's package 2356) offers it, no requirement, level 36. Any Thrax Technician (531047) on the Marshes drops a Thrax Warrant Officer Armor Fragment at 60%, a Lightbender (new pool of 3, level 36, 10406 HP, 100 m east of the camp at -680, 218.2, -520) a Lightbender Rifle Power Core at 50%, while it is open; pick them up from the corpse. 10 fragments, 5 cores. Markers on the technician pools 531283, 531293, 531288 and on the Lightbenders. Turn in at Lou: 65000 XP, 5400 credits, one of Hazmat Gloves 121209, Motor Assist Gloves 121210, Bio Helmet 121211, Reflective Gloves 121212; he keeps the parts. Do the Lightbenders (class 7120, no name) show a name? Are 10 fragments from the scattered technicians too slow?
- [ ] 1866 Gun Control - Part II (Valverde Marshes): Lou offers it after 1865, level 36. The Mox Electrical Discharge Container at the accept (needs a free slot). Kill 10 Mox (any Marshes Mox 540013): "Number of Mox Killed for Electrical Discharge" 0/10. Markers on the Mox pools 540038 (-495, 218.6, -258) and 540039 (-596, 217.5, -167) north of the camp's hill. Turn in at Lou: 65000 XP, 5400 credits, one of Mech Helmet 121213, Graviton Legs 121214, Stealth Helmet 121215, Bio Legs 121216; he takes the container. Abandon: the container goes.
- [ ] 1867 Gun Control - Part III (Valverde Marshes): Lou offers it after 1866, level 36. The CGH 9000 Ultra Electric Chaingun at the accept (needs a free slot). 1 "Deliver ... to Shaman Masai" and 2 "You've been sidetracked - speak with Retread Duvall" open. Duvall (-593, 217, -438) offers a choice: "Not a chance" (2 done, nothing else) or "I could use the cash" (2 done, the gun goes, 1 fails, 3 "Return to Shaman Masai without the gun" opens). Shaman Masai (new Forean shaman, -735, 218, 171, Plateau Point, the client's package 991) takes the gun on 1 or hears 3. Markers at Masai (1, 3) and Duvall (2). Turn in at Masai: 65000 XP, 5400 credits, no item. Given: Masai offers 1868; sold: Duvall calls by radio with 1904. Does Duvall's choice window show the body and both answers? Does the failed 1 show as failed in the log? Abandon: the gun goes, no follow-up opens.
- [ ] 1868 Gun Control - Part IV (Lou) (Valverde Marshes): Masai offers it after 1867 with the gun given (flag 1867001 = 1), level 36. Talk to Lou ("never play poker with a Forean"): 1 done. A marker at Lou. Turn in at Lou: 32500 XP, 3600 credits, one of EMP Machine Gun 121217, Shotgun 121218, Virulent Injection Gun 121219, Sonic Rocket Launcher 121220 (Rare).
- [ ] 1904 Gun Control - Part IV (Duvall) (Valverde Marshes): Duvall's radio call at 1867's turn-in with the gun sold (flag 1867001 = 2), again on arrival on the Marshes if abandoned, level 36. The call is not a window: it is the status bar's radio indicator (RadioMissionAvailable1904); click it for the offer. Talk to Duvall ("A deals a deal"): 1 done. A marker at Duvall. Turn in at Duvall: 32500 XP, 3600 credits, one of 121217-121220 (1868's). Does the radio offer show Duvall's text?

Set 9, the escorts (migration `20271104000800_Marshes_escorts`).

- [ ] 497 Forean Flu (Valverde Marshes): Shaman Masai offers it, no requirement, level 33. At the accept your own Forean Warrior (new, class 6043, level 35, 4000 HP) stands beside Masai (-733, 218, 173) and follows you. Walk into 15 m of Field Medic DuBois at Falcon Hold (-143.9, 219.3, 614.2; 655 m over the navmesh): 1 done, the warrior goes in. His death fails the mission. A marker at DuBois. Turn in at DuBois: 60000 XP, 5250 credits, one of Bio Boots 18750, Mech Boots 32677, Graviton Helmet 23808, Reflective Legs 36019. Does the warrior keep up and fight back when hit? Is the walk past Stalker Woods too hard alone?
- [ ] 888 Tread Lightly (Valverde Marshes): Retread Duvall offers it, no requirement, level 35. Your own Retread Otto (new, Vincent's look, level 35, 4000 HP) beside Duvall (-591, 217, -436) follows you; walk into 15 m of Retread Vincent at the Retread Camp (-276.8, 217.5, -761; 587 m): 1 done, Otto goes in. His death fails it. A marker at Vincent. Turn in at Vincent: 65000 XP, 5400 credits, one of 6 Fragmentation Grenade VII, 2 EMP Bomb VII, 6 Cryogenic Grenade VII, 8 Standard Med Pack VII. Does Otto show his name?
- [ ] 889 AWOL Andy (Valverde Marshes): MP Price offers it, no requirement, level 37. Price's package 386 moved to your own Lieutenant Bruncke (new, Morrison's look, level 37, 4000 HP) (user decision U1): Price no longer has that talk. Four search places (optional, 20 m, "Possible location of Private Bruncke"): west (-520, 217.5, 40), north-west (-450, 217.1, 120), south (-330, 215.1, -160), east (-200, 215.7, -100); Bruncke stands at the east one from the accept. Talk to him ("lead the way"): 1 done, 2 opens, he follows. Walk into 25 m of Price (-317.6, 218.5, -2.8): 2 done, Bruncke goes. His death fails it. Markers: the four places (1 and 4-7), Price (2). Turn in at Price: 130000 XP, 7200 credits, one of Bio Legs 120013, Graviton Legs 120014, Mech Helmet 120015, Stealth Helmet 120016. Are the four places where players searched (no source)? Does Price still say his other lines?
- [ ] 1023 Rest and Recreation (Valverde Marshes): Sergeant Elway offers it, no requirement, level 33. Four groups of three of your own soldiers (new, the AFS Rifleman's class, level 33, 4000 HP each), at least one must arrive, the first group within 10 minutes (only it is timed): 1 from the Falcon Hold waypoint to Lieutenant Belfast (new, 110, 216.9, 405); talk to him (5): group 2 from him to Lieutenant Delaio (new, 100, 216.9, 530.4); talk (6): group 3 to Lieutenant Urqhart (new, 45.5, 216.9, 704); talk (7): group 4 back to Elway (20 m). All three dead (or group 1 past 10 minutes) fail the mission. Markers at each leg's officer and each report. Turn in at Elway: 70000 XP, 5550 credits, one of Weapon Module: Crit Hit Bonus [4] 123161, Armor Module: Body 122966, Mind 122976, Spirit Bonus [4] 122991. Are Delaio and Urqhart at the right watchtowers? Do three followers walk without blocking each other?

Set 10, Retread City (migration `20271104000900_Marshes_retread_city`).

- [ ] 914 Highway Robbery (Valverde Marshes): Retread Vincent offers it, no requirement, level 36. Two ways, one to take. Steal: your own AFS Supply Crate (class 11078, "AFS Supply Crate") by the Paludos med tent (-310, 218.5, 126) and one by the Falcon Hold med tent (-150, 219.3, 612); use either: AFS Medical Supplies (44929), 1 done, 2 fails, the crates go. Tell: talk to Commander Figgins (new, Falcon Hold -184, 219.6, 627, the client's package 1011, "Damn Retreads!"): 2 done, 1 fails, the crates go. Then report to Vincent (4): after the theft his "Bypass the red tape" (variant 131, he takes the supplies), after the telling his "Your AFS buddies were already here" (variant 130). Markers at both crates (1), Figgins (2), Vincent (4). Turn in at Vincent: 35000 XP, 3700 credits, one of 2 Advanced Med Pack VII, 2 Adrenaline Booster VII, 4 Incendiary Grenade VII, 4 EMP Grenade VII. Does the client draw the failed optional way? Does Vincent show the right answer text?
- [ ] 916 Production Destruction (Vincent) (Valverde Marshes): Vincent offers it after 914 with the supplies stolen (flag 914001 = 1), level 35. Five of your own Production Kiosks (class 10159, 500 HP) round the Bane Repair Station (300..360, -500..-555); shoot them down (counter 5757). Kill Overseer Vesh 520035 (350, 217.5, -525) and Overseer Koorva 520033 (320, 218.1, -525), the public ones. Markers round the kiosks (40 m), at Vesh and Koorva (15 m). Turn in at Vincent: 150000 XP, 7600 credits, one of 119318-119321 (the client's block). Are the kiosks where they belong in the station?
- [ ] 1033 Production Destruction (Holloway) (Valverde Marshes): Lieutenant Holloway (new, Retread City -285, 217.1, -753) offers it after 914 with the AFS told (flag 914001 = 2), level 35. At the accept your own AFS Mech Defender (new, the AFS Mech's class, 692103's stats, level 36) comes up beside Holloway and follows you; his death fails nothing. Kiosks and Overseers as 916's. He goes at the turn-in. Turn in at Holloway: 150000 XP, 7600 credits, one of 120009-120012 (the client's block). Does the mech fight with you? Does he keep up the 1700 m?
- [ ] 1052 Trapped! (Valverde Marshes): Vincent offers it, no requirement, level 38. When you come within 40 m of the station (not at the accept: to-fix 10) your own Retread Samuel (new stand-in, package 1120, 12000 HP: to-fix 29) and three Retreads stand at the Bane Refueling Station (196.4, 219.7, -669.1); the public Samuel is hidden from you. Talk to Samuel ("We got turned around"): 1 done, 2 opens, the defence starts: Thrax waves of 3, 4 and 3 (531045 / 531046; wave 3 was 5: review list 2026-11-04, migration `20271104003500_Marshes_trapped_smaller_wave`) 5 s, 75 s and 150 s later from the west, south-east and south, sent at the three Retreads; Samuel only when no Retread lives (to-fix 29). Hold 4 minutes (TaRapedia): 2 done, the crew and the Thrax go, the public Samuel is back. Samuel's death fails it (the Retreads' deaths do not). Markers at Samuel (1) and round him (2, 30 m). Turn in at Vincent: 75000 XP, 5800 credits, one of 120097-120100 (the client's block). Are the waves too hard or too easy (no balance check now)? Do the Thrax go for the crew?

Set 11, the Retread Arena (migration `20271104001000_Marshes_retread_arena`).

- [ ] 1085 Retread Arena: Beat Street (Valverde Marshes): Retread Karl (new, Retread City -219.9, 216.9, -807.1) offers it, no requirement, level 35. Talk to Retread Foster (new, the dome's south door -219.9, 216.9, -799.1, the client's package 1161) within 3 minutes ("let's get you in the arena then"): 2 done, you are taken onto the Thunderdome's floor (-219.9, 213.9, -746.1; it has no walk in or out) with your own Retread Withers and Ingram (new, level 38, 6000 HP, hostile). Beat both within 5 minutes (counter "Retread Withers Defeated:"): 1 done, you are set down by Foster (-219.9, 216.9, -803). The 5 minutes out fail it. Markers at Foster (2), round the arena (1). Turn in at Karl: 35000 XP, 3700 credits, one of 2 EMP Bomb VII, 2 Power Charger, 4 EMP Grenade VII, 4 Concussion Grenade VII. Does the client show the transfer into the dome right? Do the fighters attack at once?
- [ ] 1087 Retread Arena: Smackdown (Valverde Marshes): Karl offers it after 1085, level 35. As 1085 with Retread King and Baker (level 39, 8000 HP). Turn in at Karl: 35000 XP, 3700 credits, one of 2 EMP Bomb, 2 Adrenaline Booster, 4 Concussion, 4 Frag Grenade VII.
- [ ] 1088 Retread Arena: Legend or Fall (Valverde Marshes): Karl offers it after 1087, level 35. Foster's choice: "I'll take a fall" (flag 1088001 = 1, 1 and 3 open) or "A winner never quits" (flag 2, 1 only). Either way you fight Retread Toro (level 40, 15000 HP). Fight: beat him in 5 minutes or 1 fails. Fall: hold out 5 minutes with Toro standing: 1 and 3 done (beating him also completes 1). Turn in at Karl: 35000 XP, 3700 credits, one of 2 EMP Bomb, 2 Armor Charger, 2 Cryo, 2 Incendiary Grenade VII. Is "survive 5 minutes" a fair reading of a fall?
- [ ] 1084 Calling all Parts (Valverde Marshes): Retread Vincent offers it after 1088, level 33. A Motherboard from any Stalker (540002, the pool -664.6, 217.6, -583), an AI Controller from any Caretaker (531048), a Brain Implant from a Forean Machina (new pool of 3, -650, 218.4, -620, level 36), each 100% while its objective is open. Note: a Machina's first death is a self revive; the part drops at its real death. Markers at the Stalker pool, the three Caretaker pools nearest Retread City and the Machina pool. Turn in at Vincent (he takes the parts): 70000 XP, 5550 credits, one of Virulent Injection Gun 15654, Sonic Rocket Launcher 49164, Electric Rifle 14734, Laser Machine Gun 48256.

Set 12, east and north (migration `20271104001100_Marshes_east_and_north`).

- [ ] 542 Bring Down the Kael (Valverde Marshes): Elder Toida (Paludos) offers it, no requirement, level 33. A prison caravan walks the Long Walk from (480, 216.9, 320) south to (420, 219.6, -300) and back: a Forean Prisoner Leader (new, neutral, no attack) with four Thrax Prison Guards (new, the Marshes Thrax Soldier's row) on his tether. Kill four guards (counter 1529 "Thrax Guards Eliminated"); the caravan comes again 300 s after it is gone. Markers along the route (3, 60 m). Turn in at Toida: 150000 XP, 7600 credits, one of Graviton Vest 24046, Reflective Vest 36148, Bio Boots 18730, Motor Assist Vest 29227. Do the guards fight the player and not the prisoner? Is the route on the Long Walk?
- [ ] 864 Bringing the Spirit Home (Valverde Marshes): Elder Bargas (new, the Temple of Paludos' upper floor -205, 230.3, 40, a server-only package) offers it, no requirement, level 36. Kill four Forean Machina (new pools of three north (-250, 213.9, 200) and east (-100, 217.1, 150) of Paludos, and the High Point Retreat pool; each first death is a self revive): a Forean Spirit Icon (42408) each, 100%; four complete 2 and open 1. Use either of your own Forean braziers (class 7636) on the temple floor by the two squares of shrines either side of Bargas (-185.5, 231.7, 28) and (-230.5, 231.7, 28), out of the fountains (review list 2026-11-04, migration `20271104003100_Marshes_braziers_on_dry_floor`): the four icons go in, "Spirit Shards Returned" 4, both close. Markers on the two new pools (2) and the client's "Forean Spirit Altar" over the braziers (1). Turn in at Bargas: 75000 XP, 5700 credits, one of 120045-120048 (the client's block). Does the brazier read as "an urn on the altar"? Can the player reach the temple's upper floor?
- [ ] 920 Head East, Young Man (Valverde Marshes): Commander Figgins (Falcon Hold) offers it, no requirement, level 33. When you come within 40 m of the guard station (not at the accept: to-fix 10) your own Lieutenant Shaw (new stand-in, package 1012) and two AFS soldiers stand at the eastern guard station (90.4, 216.9, 535); the public Shaw is hidden from you. Talk to Shaw ("I hear their ships approaching now!"): 1 done, the defence starts: 2 Thrax soldiers and a grenadier at 10 s from the north-east, 2 and 2 with a Stalker at 90 s from the south-east, on foot; they go for the two soldiers first and for Shaw only when neither stands. Shaw's stand-in has 12000 hit points, the soldiers 8000 and stand still (review list 2026-11-04, migration `20271104003400_Marshes_guard_station_holds`). Hold 3 minutes: 2 done, the station's men and the Thrax go, the public Shaw is back and your own Ranger Meeko (new, package 1013) stands by Shaw. Talk to Meeko (3); turn in at Meeko: 70000 XP, 5550 credits, one of 120069-120072 (the client's block). Shaw's death fails it. Markers: the client's "Guard Station" at Shaw (1), round the station (2, 40 m), at Meeko (3). Do the waves go for the station? Does Meeko show the turn-in?
- [ ] 2009 Soyuz Salvage: Marshes (Valverde Marshes): Sergeant Starling (Paludos) offers it, no requirement, level 37. Five of your own Soyuz debris pieces (classes 29783/29784) at (-560, 217.5, 350), (-550, 217.1, -150), (250, 214.1, 600), (200, 214.3, 150), (300, 214.1, -100); use one (argument 1): the Soyuz Orbital Module piece (122851), all five close. Markers at each piece (40 m). Turn in at Starling (he keeps the piece): 65000 XP, 5400 credits and the Soyuz ISS Model Rocket 122834. Are the pieces on open ground?

Set 13, the Logos Research Facility and the Eloh chamber (migration `20271104001200_Marshes_logos_research`). The facility is
the squad instance 1700 (door on the Marshes at -318.4, 285.7, -497.8); its missions are bound to it.

- [ ] 546 A Taste of Their Own Medicine (Valverde Marshes): Sergeant Fitz (Temple of Paludos) offers it, no requirement, level 33. In the facility talk to Specialist Hax (new, by the hospital 814, 321.8, -110, the client's package 960): six Thrax (531045/531046) come into the Security Checkpoint (760, 320, -136); kill them: 1 done. Your own Hax and a Field Medic (new) come to the room's west side (748, 319.8, -130); the hack runs 120 s, waves of 4 Thrax at 10, 50 and 90 s from the north and south tunnels attack Hax and the medic. Hack done: 2 done, the waves and the medic go. Hax's death in the hack fails 2 and the mission. Markers at Hax (in the facility and at its door), round the checkpoint (1, 25 m), at Hax in the room (2). Turn in at Hax: 70000 XP, 5550 credits, one of 120017-120020. Do the waves go for Hax? Is Hax's hack readable without a text?
- [ ] 548 Freedom's Just Another Word (Valverde Marshes): Hax offers it after 546, level 35. Your own Thrax Commander (new, level 38, 12375 HP) and two Thrax in Specimen Control (728.5, 297.8, -176); his death gives the Keycard Level 3 (42430): 1 done, 2/4/5 open and three Bane terminals (20631, argument 1) stand at the cell blocks A (728, 289.9, -222), B (728, 289.9, -246) and C (744, 289.8, -262). A terminal frees its block's two prisoners (new, invulnerable) and completes its block. Your own Captain Sakai (new) is held in block C (716, 290, -264). Markers at the Commander (1) and the client's "Cellblock A/B/C" (2, 4, 5). Turn in at Sakai (he takes the keycard): 75000 XP, 5700 credits, one of 96268-96271. Do the prisoners read as freed (they vanish)?
- [ ] 550 Leggo My Logos (Valverde Marshes): Captain Sakai (new, by the hospital 814, 321.8, -100) offers it after 548, level 35. A bomb crate (21568, argument 3) by the south bio lab (580, 257.8, -190): 2 Detonators (2511), 3 done. An Explosive Charge (7870) at the primary tank (an extraction drone 11255, 608, 257.8, -212): one detonator, the tank blown, a Logos Receptive DNA sample (117042), 1 done ("Primary Tank Destroyed" 1/1); the same at the secondary tank (608, 257.8, -44), 5 done. The forcefield console (20631) at the analysis lab's door (604, 257.8, -296): 9 done. The analyzer's keyboard (608, 248.2, -340): the Logos element taken (analyzer 7241 to state 56), 2 done; 4 and 6 open, 5 minutes. Reach the entrance hall (868, 353.8, -120, 12 m) in time: 4 and 6 done, Special Ops Officer Brock (new, package 2203) comes; talk: he takes the two samples, 10 done. Late: 4 and 6 fail, Lieutenant Russell (new, package 284) comes and you are set down by him in the entrance hall; talk to him: the samples go, 7 done and 10 with it. Markers at each object and NPC, round the hall (4, 6). Turn in at Fitz (Paludos): 80000 XP (ESTIMATE), 7600 credits, one of Sonic Torqueshell Rifle 49652, Cryogenic Blade 47648, Pulse Cannon 15437, Virulent Leech Gun 15295. Does the client show the drone blown (state 2)? Is 5 minutes enough from the analysis lab to the hall?
- [ ] 718 Slay on Target (Valverde Marshes): Hax offers it after 546, level 35. Your own Coroner Larvax (776, 289, -104), Dissector Xynnr (688, 289, -24) and Overseer Mesgel (648, 289, -200) (new, level 38, 10000/12500/15000 HP), two pieces of lab equipment (20595, 100 HP) in each room, a DNA dispenser (11116, argument 3) at the third room's shelves (632, 288.2, -200): a DNA Sample Jar (2506), 5 done. Mesgel's first hit: the hidden alarm, 6 opens, a Bane teleporter (10603, 600 HP) in the Test Chamber (608, 248.3, -104) sends 4 Thrax at once and 4 more every 30 s (6 waves at most) until it is shot down (6 done). Kill the three (1-3) and the six pieces (4). Markers at each boss (1-3), round each room (4), at the dispenser (5) and the teleporter (6). Radio turn-in: 75000 XP, 5700 credits, one of 2 EMP Bomb VII, 4 Cryogenic Grenade VII, 2 Standard Med Pack VII. Does the dispenser open with the client's argument 3? Is the teleporter targetable?
- [ ] 1674 To a Subterranean Chamber (Valverde Marshes): Retread Vincent (Retread City) offers it after 550, level 38. Walk to the rock under the lily glows in the lake (-160, 213.7, -868): a local teleporter (new, 907) takes you to the Eloh chamber's plates (-160, 112.4, -808; 908 takes you back). Talk to Jumna (new, the hologram Eloh's look, level 45, -160, 112.1, -902, the client's package 2036): 1 done. Markers: the client's "Eloh Chamber" at Jumna, one at the lake's pad. Turn in at Jumna: 40000 XP, 3900 credits, one of 2 Advanced Med Pack VII, 2 Armor Charger VII, 4 EMP Grenade VII, 4 Concussion Grenade VII. Is the pad on the rock reachable by swimming? Does Jumna's model show?

Set 14, P'reo Das (migration `20271104001300_Marshes_preo_das`). The Village Ruins are the squad instance 1743 (door on the
Marshes at 755.1, 215.6, 727.7, arrival -138.5, 63, -360); its missions are bound to it.

- [ ] 1076 The Lost Ones (Valverde Marshes): Lieutenant Morrison (Falcon Hold) offers it, no requirement, level 33; you get one AFS Signal Spike (45451). In the Village Ruins walk into the travel point (-107, 60.2, -355, 15 m, 32 m from the arrival): 2 done, a spike spot (7847, the client's name "AFS Communication Spike") stands east of P'reo Das (181, 56.3, -118). Use it (argument 3): the spike is planted, 1 done. Markers at the Marshes door and the travel point (2), at the spot (1). Radio turn-in: 30000 XP, 3500 credits, one of 3 Advanced Med Pack VII, 3 Armor Charger VII, 6 Cryogenic Grenade VII, 6 Incendiary Grenade VII; Headquarters then calls with 893 and 1078. Does the spot read as a spike spot?
- [ ] 893 Needle in a Bane Camp (Valverde Marshes): Headquarters offers it by radio after 1076 (at its turn-in, and on arrival in the Village Ruins), not once 1078 is done, level 35. Talk to the Forean Elder (new, the client's package 958) at He'kal Das (127, 66.5, -27, clear of the Maw pool, to-fix 4): 1 done; a held 1078 fails (Myrquar and his guards go). Markers at the Marshes door and the client's "Undefended Village" at the elder. Turn in at the elder: 30000 XP (ESTIMATE), 3600 credits, one of 3 EMP Bomb VII, 3 Power Charger VII, 6 Fragmentation Grenade VII. Does the radio offer come in the instance?
- [ ] 1078 The Choices We Make (Valverde Marshes): Headquarters offers it by radio after 1076 (as 893), not once 893 is done, level 33. Your own Operation Master Myrquar (new, level 38, 12375 HP) and four Thrax Soldiers south of the landing pads (25, 59.7, 270); kill him within 25 minutes of the accept: 1 done, the Level 1 Security Clearance Pass (45453; it opens nothing); a held 893 fails (the choice is made at his death). After 25 minutes he and his guards go, 1 and the mission fail. Take the Disperse Logos in the control room (-33, 65.5, 390; one you have counts): 2 done; three pieces of Bane lab equipment (20595, 100 HP) and a Bane crate (11116, argument 3) stand round it: destroy the three and take the Concentrated Toxin Sample (117028) from the crate, any order: 3 done. Talk to Elder Toida (Paludos): she takes the sample, 4 done. Markers at Myrquar, the Logos, the client's "Bane Equipment" round the control room (18 m), Toida. Turn in at Toida: 65000 XP (ESTIMATE), 7000 credits, one of Electric Torqueshell Rifle 49572, Photonic Blade 47668, Pulse Cannon 17681, Cryogenic Leech Gun 47764. Is Myrquar in reach of the landing pads' approach? Is the Logos usable on its console?
- [ ] 1077 Dirty Secrets (Valverde Marshes): Forean Elder Qunle (new, at P'reo Das 74, 56.1, -196, on the ground clear of the Grubber pool, to-fix 4, 15 and 24) offers it, no requirement, level 35, and gives you the Level 1 Security Clearance Pass. The Disperse Logos (1; one you have counts at once) and your own lab in the control room (2: three pieces of equipment at -22/386, -40/386, -34/412 and the crate at -28, 64, 412, apart from 1078's), any order; 2 opens 3: Toida takes the sample. Markers at the Logos, "Bane Equipment", Toida. Turn in at Toida: 35000 XP (ESTIMATE), 1750 credits, no item. With 1078 held too, do both labs read apart?

Set 15, P'reo Das (part 2) (migration `20271104001400_Marshes_preo_das_village`), in the same squad instance 1743.

- [ ] 891 Never Surrender (Valverde Marshes): Forean Warrior Tola (new, by the arrival -115.2, 61.1, -352) offers it, no requirement, level 35. Walk into P'reo Das (30 m round 100.8, 57.8, -236.8): the Bane attack, your own Thrax from the village's north edge, 4 at once, 5 after 40 s, 6 after 80 s, each attacking you. Kill all 15: 1 done. No fail. Markers round P'reo Das and at Kirii. Turn in at Forean Elder Kirii (new, P'reo Das 166, 55.2, -220, clear of the Grubber pool and 22 m from the waves' spawns and ways, to-fix 4, 15, 17): 130000 XP, 7000 credits, one of 3 Class VII Standard Med Pack, 2 Advanced Med Pack, 3 Incendiary Grenade, 3 EMP Grenade. Do the waves find you from the north edge? Do they come down into the village?
- [ ] 892 Picking Up the Pieces (Valverde Marshes): Forean Shaman Rakla (new, P'reo Das 62, 59.2, -247, clear of the Grubber pool, to-fix 4, 15 and 24) offers it, no requirement, level 35. Six of your own Forean Warrior Corpses (21478), each with a Knapsack (11096, argument 3): four in P'reo Das (84/-223, 120/-224, 80/-244, 105/-215), two in Ra'a Das (231/-8, 247/9). Each knapsack gives one Forean Relic (111319) once; objective 1 shows the relics held (n/6); six: 1 done. Turn in at Rakla: she takes the relics; 60000 XP, 5250 credits, one of the client's block 96333-96336 (Propellant Gun Fire, Staff Ice, Net Gun Ice, Leech Gun Virulent, Rare). Does the knapsack show on the corpse? Does the n/6 counter show?
- [ ] 894 Cleaning House (Valverde Marshes): Kirii offers it after 891, level 35, and gives you 4 Forean Explosive Devices (45450). A freed prisoner sets off 3 s after his talk (ESTIMATE), and 1's marker (r50) holds all four pumps. Four of your own Toxic Waste Pumps (21007) on the ground before Central Operations (10/330, 37.2/330.8, -19.5/300.3, 69.8/299.8), each with a Detonation Device (7334, argument 1) beside it: a use takes one explosive and blows its pump (8-11; 1 counts "Pumps Destroyed" 4). At Ra'a Das (245, 56.3, 0), E'bae Das (60, 55.8, -120) and He'kal Das (140, 66.1, 12) a Forean Prisoner (new, the client's packages 1158, 1159, 1160) and two of your own Thrax guards; a little apart one more prisoner of the same package (the optional survivor, his own row: 256.8/19.2, 90/-125, 151.2/15.6); each offers only his own talk. A prisoner's talk ("I will come with you"): his camp done (2, 3, 13), he follows you; 6 counts "Forean Camps Freed" 3. A survivor's talk: 14-16 (optional), he follows too. The three camps open 12: walk into the arrival (15 m round Tola): 12 done. Turn in at Tola: the followers go; 130000 XP, 7000 credits, one of Sonic Torqueshell Rifle 49652, Laser Pistol 14487, Electric Chaingun 15105, Sonic Rocket Launcher 49168. Can the acceptor be targeted beside the pump (TaRapedia's 2007 bug)? Does the pump show as blown? Do the prisoners keep up?

Set 16, the Bane Supply Depot (migration `20271104001500_Marshes_bane_supply_depot`). The depot is the squad instance 1451
(door on the Marshes at 451.6, 223.6, -581.9, arrival -370.5, 149.8, 3.1); its missions are bound to it.

- [ ] 664 Survivors (Valverde Marshes): Headquarters offers it by radio on arrival in the depot, no requirement, level 35. Walk into the Survivor's Camp (20 m round the hospital 129.9, 125.7, 312.9) within 10 minutes of the accept, else 1 and the mission fail: 1 done, the Bane attack, your own Thrax, 4 at once, 4 after 30 s, 4 after 60 s, from the camp's south and west, each attacking you. The camp reached opens 5 "Eliminate the surrounding Bane attacking the Survivors Camp" with the client's counter "Bane Remaining" (n/12, counting up). Kill all 12: 5 done, 3 opens, a C6801 Supply Crate and a C6801 Medical Crate (11078, argument 3) stand by the wreck (128/340, 140/340). Each gives its item once (2310, 2311): 3 done. Markers at the Marshes door and the camp (1), the camp (5), the crates and C-6801 Copilot Ortega (new, 125, 127, 318) (3). Turn in at Ortega: he takes the crates; 80000 XP, 6000 credits, one of 12 Basic Med Pack, 6 Fragmentation Grenade, 2 EMP Bomb, 9 Cryogenic Grenade (Class VIII). Do the waves reach the camp? Does the radio offer come in the instance?
- [ ] 919 Beinvenidos del Voro (Valverde Marshes): Headquarters offers it by radio on arrival in the depot, no requirement, level 35. Three of your own Bane computers (20631, argument 1) beside the depot's remote terminals (-80.4/198.9, 1.2/92.8, 1.6/171.9): each gives one Bane Datablade (45458) once; 1 shows the blades held (n/3); three: 1 done. Markers: the client's "Bane Terminal" at the first computer, the other two, the Marshes door, Fitz. Turn in at Sergeant Fitz (Paludos): he takes the blades; 75000 XP, 5750 credits, one of Bio Armor Helmet 19008, Stealth Armor Gloves 38932, Graviton Armor Legs 23937, Motor Assist Armor Legs 29073 (ESTIMATE templates). Is each computer reachable from the camp? Does the n/3 counter show?
- [ ] 663 Insertion Point (Valverde Marshes): Elder Toida (Paludos) offers it from level 37. In the depot four of your own Thrax guard the front entrance (-284, 149.6, 36). Your first hit on one sounds the alarm: 4 (by force) done, 3 (by stealth) failed (both optional). Walking into the lower entrance (12 m round 68, 97.6, 120) before the alarm: 3 done. Either completes the hidden 5 (a way in is needed to finish). 6 counts from the accept: kill the depot's 13 Bane Mortars (592006; the Marshes' own do not count) or use your own Bane computer (20631, argument 1) at the mortar terminal (-88.2, 80, -160.4): 6 done. Markers: the Marshes door and the front (4), the door and the lower entrance (3), the client's "Bane Terminal" at the terminal and one at each mortar (6). Radio turn-in: 170000 XP, 6000 credits, one of 6 Advanced Med Pack, 9 EMP Grenade, 3 Res Trauma Kit, 9 Concussion Grenade (Class VIII). Is the lower entrance reachable from the arrival? Is the terminal reachable?

## Assumptions and choices (not from a source)

Creature rows copied from upstream rows since the rebase onto origin/development 61c4e9e6 (2026-11-03). The PRs whose
rows these were (#202 dynamic dropships, #209 Valverde Targets of Opportunity) are not in the base. All ESTIMATE:
- 1029's Strider (760315): copied from the Foreas Base past's Thrax Cavalryman 750100 (PlateauForeasBasePastNpcsV1), not
  #209's Pools Strider 650256. 650256 was itself a copy of 750100, so nothing changes.
- 1023's soldiers (760338), the arena fighters (760346-760350) and 920's station soldier (760359): copied from the Alia
  Das AFS squad Rifleman 670003 (AliaDasAfsSquad: class 29423, name 8717, rifle 27220), not #202's control point
  Rifleman 692101. Look, name and speeds are the same. The attack is 670003's rifle shot 72003, weaker than 692101's own
  rifle (213-317 damage); levels, HP and armour stay ours.
- 1033's mech (760341): copied from the Landing Zone AFS Mech Defender 680001 (class 10235, name 10736, its minigun and
  missiles), not #202's 692103. 692103's numbers are written in as constants: level 36, 38700 HP, 30 body, mind and
  spirit, 1063 armour. The attacks are 680001's.
- 548's block B prisoners stand on the cells' floor at y 291.6 (client collision 291.57 with upstream's parent-bone
  fix f1669e5f; rebuilt navmesh 291.67 / 291.53). The old y was 289.37.

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
- 961: the acceptor class 21824 UsableItemAccForeanTreebackFood (client name override 168 "Mox Feeders") at the origin
  and turn of each of the 12 map Treeback Feeders (the client map's ArchForeanTreebackfeederV01 statics; its food mesh is
  the trough's basin fill, to-fix 19: on the navmesh top it floated about 1.1 m over the trough); any 10. Markers unnamed (the client name 64 "Treeback Feeder" can key only one marker per
  objective). Rewards the lowest template of each client class in TaRapedia's stacks.
- 881: seven harvesters 40-60 m apart in the swamp east of Falcon Hold (Ten Ton Hammer "the swamp to the east", "pick any
  five"), each a body (7908) with a Bane item crate (11116) as the tap 3 m east; a hidden objective counts five samples.
  Since to-fix 20 (migration `20271104002600_Marshes_harvesters_dry_ground`) the seven places stand on dry ground at
  the Northern Mud Pit (terrain above the pit's water at y 214.68, 1-11 m from the first places; ESTIMATE, the client
  map has no harvester): in the water a player swam and could not fire.
  Vetter stays at his upstream spot (Ten Ton Hammer's is 15 m off). Rewards TaRapedia's armour kinds and slots in the band
  that holds 33 (Ten Ton Hammer's older weapon list not used).
- 890: five destructible harvesters (7906) at 881's first five places, 500 hit points each. A kill goes to the client
  as the class's Use chain 110, 185, 186, 2 (its explosion, then its wreck mesh 23998), as upstream's DestroyableStates. Rewards (no names on any
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
  Depot door (client "outside the Bane Supply Depot"; Ten Ton Hammer's "-20, -638" refueling station not used). Since
  to-fix 21 (migration `20271104002700_Marshes_kaav_in_the_open`) at (462, 223.68, -592) on the depot's open floor, 14.5 m
  from the door (ESTIMATE): his first place lay under the chunnel's floor, half in a pipe, and no talk reached him.
- 1029: the Strider the Foreas Base past's Thrax Cavalryman row 750100 (class 9804, its attacks; ESTIMATE, see below) at level 36 with the Marshes level-36 hit points
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
  Morrison's row and look with the client name 10326 and a server-only package, at the tower's foot (-116.5, 216.88, -311;
  migration `20271104002700_Marshes_npcs_in_the_open`, to-fix 22, ESTIMATE): the tower room at TaRapedia's /loc has no door
  or stair, and the upstream pad 573 at the foot goes to Research Point, so no player reached him there.
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
- 1864: any order. The pits start cold (state 56) and a use lights them (55): the client shows class 6137's fire in state
  55 (usabledata specialFX and animation (6137, 55, 97)); to-fix 23, migration `20271104002800_Marshes_fire_pits_lit`.
- 1869: the spike spots are Mapping the Pools' beacon acceptor (7847) named by the client's 273; any order.
- 1871: receiver Duvall (the triage's Q19; TaRapedia's Vincent); the flowers are the client's Mawflower item givers
  (Medicinal Measures' way); a hidden objective counts five; one Maw pool of two (Marshes Maw 531052, 120 s) among them.

- Set 8 NPCs: Lou a copy of Retread Vincent's row and look, Masai of Council Elder Moawi's (a Forean shaman with his
  staff), with their client names and packages (2356, 991), at TaRapedia's /loc. The Lightbender a copy of the Caves of
  Donn Lightbender 566016 (class 7120, no name) at level 36 with the Hunter Lieutenant's hit points and armour (531054).
  Levels 36 (TaRapedia for 1866-1868; the camp's for 1865 and 1904).
- 1865: the Warrant Officers are the Marshes Thrax Technicians (531047, the triage); fragments 60%, cores 50% (no source
  gives a rate); the objective's 5 cores, not the reminder's 10. The Lightbender pool on the lowland 100 m east of the camp
  (the log's "to the east"); the triage's north-east place is a navmesh island.
- 1866: no new Mox pools: north of the camp is a hill the navmesh does not reach; the World DB pools 540038 and 540039 north
  of it carry the markers. Any Mox counts; the container is not checked at the kill.
- 1867: 1 and 2 open at the accept (the client gives no trigger for "sidetracked"); a sale fails 1 (optional) and opens 3;
  hidden 4 completes with 1 or 3. The ending flag 1867001 (1 given, 2 sold) is set only when Masai hears 1 or 3, so an
  abandon decides nothing. No 50,000 credits for the sale (the triage).
- 1868 / 1904: exclusive by 1867001, read from 1867's hidden "add the flag" objective (the client has no prerequisite
  table). 1904 by radio from Duvall (its offer text is his comm call; the triage had Masai); its reward 1868's (TaRapedia's
  one page; no other client block fits; 121237-121240 are 1875's).

- Set 9 escorted: personal scene creatures that follow the player (FollowActorIntent, as Bootcamp's squad), 4000 HP and 300
  armour (the server's escort stats), run 7 / walk 2 (These Old Bones). The warrior a copy of the Holy Vengeance escort
  warrior 750110 (class 6043), Otto of Vincent's row and look, Bruncke of Morrison's, the soldiers of the Alia Das AFS
  squad Rifleman 670003 (ESTIMATE, see below); Belfast, Delaio and Urqhart of Morrison's (client names 8787-8789, packages 1101, 1102, 1100).
  Arrival radius 15 m (Elway 20 m, Price 25 m). No route: the player picks the way.
- 497: the rewards the lowest Uncommon templates of TaRapedia's kinds and bands (no client block fits).
- 888: objective 2 (the client's "no desc", no text) left out; the stacks the lowest Class VII templates.
- 889: Price's package 386 is Bruncke's (U1); Price takes a server-only package (his id). The four search places
  (ESTIMATE, walked to from the road); Bruncke at the east one. Objective 3 (the older "back to Paludos") left out. Rewards
  the lowest of the four client blocks that match TaRapedia (120013-120016).
- 1023: three soldiers a group (the triage), a leg fails only with all three dead; the next group stands at the officer who
  sends it. Delaio and Urqhart at the Corman watchtowers (the triage). The modules read as the [4] ones.
- Set 11 NPCs (ids 760344-760351): Karl and Foster copies of Vincent's row and look (names 8636, 8918; Foster the client's
  package 1161, Karl a server-only one) at the dome's south door; the five fighters copies of the Alia Das AFS Rifleman 670003 (ESTIMATE, see below)
  (the triage's "AFS soldier look"; the Retread rows have no attack), hostile, levels 38/38/39/39/40 (Ten Ton Hammer), HP
  6000/8000/15000 (the triage), Toro with the rifle (no propellant-gun creature action found); the Forean Machina a copy of
  566012 at level 36 with the Treeback's stats, no name.
- Arena: the Thunderdome's floor is a navmesh island, so Foster's talk transfers the player in and the round's end out
  (by Foster). 3 minutes from the accept to Foster (the mission deadline), 5 minutes for the fight (the scene's timer).
  The fighters are the player's own and come with the transfer.
- 1088's fall (the triage's Q20): hold out 5 minutes with Toro standing; the player is not checked to be alive or to stop
  fighting. 6201 (the client's "Baker" counter on Toro's objective) left out.
- 1084: after 1088 (its opening "you took down Toro"), either 1088 ending; the rewards the lowest Uncommon templates of the
  four kinds in the bands holding 33 (no client block matches); the part drops 100%.
- Set 10 NPCs (ids 760339-760343): Figgins a copy of Morrison's row and look (name 8623, the client's package 1011, the
  triage's Q4) at Ten Ton Hammer's place; Holloway a copy of the Pools Holloway 510199 (name 8594, a server-only package,
  Q6) at TaRapedia's; the mech a copy of the Landing Zone AFS Mech Defender 680001 at 692103's level and stats (ESTIMATE, see below); Samuel's stand-in and the
  Retread copies of Retread Samuel 510167 with the escort stats (levels 38 and 36; the Retread has no name).
- 914: one personal crate by each med tent (the client map's tents), the client's name 3 "AFS Supply Crate"; choosing one
  way fails the other and closes the crates; flag 914001 (1 stolen, 2 told) picks Vincent's answer and the follow-up (916
  or 1033, the triage's Q16). The stacks the lowest Class VII templates.
- 916 / 1033: five kiosks (client 5) round the server's Vesh and Koorva, 15-35 m apart; the Overseers are the public pools
  (as 968), so anyone's kill of them counts. 1033's mech: an escort leg with no objective (his death fails nothing), gone at
  the turn-in.
- 1052: the stand-in at the DB Samuel's spot, not TaRapedia's "near (15, -535)" (the triage); 3 Retreads; waves 3/4/3 at
  5/75/150 s, hold 240 s (TaRapedia "about 4 minutes"; the triage said 5 minutes); the wave places within 2 m of the probed
  spots. Wave 3 was five Thrax (two places); with one player's weapons it killed the crew and Samuel 40 s after its spawn
  (to-fix 29 recheck, 2026-11-04): now three (ESTIMATE), the two west soldiers and the south grenadier.
- Quest NPCs (to-fix 15): the Marshes givers, receivers and posted NPCs stand still (run and walk speed 0, as upstream
  givers) and at least 20 m (an 18 m aggro range and 2 m) outside every hostile pool's circle. Moved: Dikah (-514, -153),
  Qunle (82, -204), the Forean Elder (127, -35), Kirii (138, -224.6), Rakla (67, -254), and 1052's crew 8 m west and 4 m
  south (Samuel's stand-in at 188.4, -673.1) (ESTIMATE places, navmesh probe).
- Quest NPCs' hit points (to-fix 17, migration `20271104002400_Marshes_quest_npcs_hold`): 891's own Grenadier killed Kirii
  (100 hit points) with an area attack. Each friendly or neutral Marshes pool NPC of ours without a creature_stat row (33)
  now has Dikah's (4500 hit points, armour 300; ESTIMATE, the retail values are not known). Kirii moved to (165, -228),
  at least 22 m from every 891 wave spawn and from each spawn's way to the village's centre (navmesh probe). Still in a
  fight's path, with the hit points only: Qunle (891), Delaio and Shaw (920), Karl and Foster (the arena), Ortega (664).
- Kirii in sight (review list 2026-11-04, migration `20271104003200_Marshes_kirii_in_sight`): at (165, -228) a rock pile
  (20812) stood between him and the west side, the village side: the client said "cannot see your target" from (162,
  -226). He and his 891 marker moved 3.2 m north-east to (166, 55.0, -225) (ESTIMATE, client floor, sight and navmesh
  probe): seen from that spot and from 15 of 16 places 3 m round him, 30.6 m from 891's waves.
- Kirii in the open (real client 2026-11-05, migration `20271104003600_Marshes_kirii_in_open`): at (166, -225) the
  client still said "cannot see your target" from the south-west, where its walk to him stopped (163.8, -227.8). The
  client decides a talk's sight itself (converse.pyo CheckAction, world.InLineOfSight): five lines from the player's
  capsule middle and top to the NPC's capsule middle, top and quarter height (the capsules of collision.pyo), seen when
  one is clear; the rock pile meets all five there. He and his 891 marker moved 5 m north to (166, 55.2, -220)
  (ESTIMATE, client geometry probe with that rule): seen from every place 2-5 m round him with a client floor, on the
  navmesh's way in from the village, 29 m from 891's waves.
- 1052 and 920 (to-fix 10): the crew stands only when the player comes within 40 m (a server-only hidden objective and
  its area), so nothing at the spot kills it while no player is near (the engine cannot change an actor's policy to
  invulnerable until the talk); one 1052 Retread moved to (198, -678), 19 m outside Thrax pool 531291's circle.
- Set 12 NPCs (ids 760352-760361): Bargas a copy of Toida's row and look (name 8536, a server-only package) at TaRapedia's
  place; the Forean Prisoner Leader a copy of the Torcastra one 660023 (level 33, neutral, no attack); the prison guards a
  copy of the Marshes Thrax Soldier 531045; Shaw and his stand-in copies of Morrison (name 8625, the client's package 1012),
  the stand-in with the escort stats; the station's soldier a friendly copy of the Alia Das AFS Rifleman 670003 (ESTIMATE, see below) with the escort
  stats; the Stalker of the attack a copy of the Marshes Stalker 540002 (its own row: no 1084 motherboard); Meeko a copy of
  Ranger Taavik 510164 (name 8627, the client's package 1013). Next free id 760362.
- 542: one public squad on the Long Walk (the triage's Q10, TaR-hist's line east of the long defensive line), the navmesh
  corners of (480, 320) to (420, -300) by (500, 100), there and back, 10 s at each end, 300 s respawn (ESTIMATE, as the
  Pools' Nakrinx: one caravan is one player's four kills; the triage said 20 minutes); 4 guards; the
  player's own kills count. The rewards the lowest Uncommon templates of the four kinds in the bands holding 33 (no client
  block matches). Level TR MMO DB's.
- 864: two pools of three Machina (L36, 760351's row) north and east of Paludos (the client's "east and north"); the icon
  drops 100% while 2 is open, from any Machina; the altars two personal braziers 7636 (the client's only usable Forean
  burner, no urn class) by the two shrine squares either side of Bargas; one use takes all four icons and counts four
  shards. Their places (ESTIMATE, client floor and navmesh probe 2026-11-04): first the squares' middles, which are the
  fountains' basins (water at 231.14 over a floor at 230.86, the tester saw the brazier in the water); now 5.5 m from
  each middle towards Bargas on the temple floor (231.66), seen from every side; the altar marker stays between them.
- 920: Shaw at Ten Ton Hammer's place; 2 soldiers; waves of 3 and 5 (the second with the Stalker) at 10 and 90 s, hold 180 s
  (the triage's Q40 3 minutes); Meeko comes 6 m from Shaw at the hold's end (Q23) and is the receiver. The station's
  strength (ESTIMATE, review list 2026-11-04: with weapon kills wave 1's four Thrax killed Shaw in 38-44 s, a soldier died
  even with GM kills, the soldiers walked off their places): the soldiers stand still (speed 0), Shaw's stand-in has 12000
  hit points (three times the escort's, as 1052's Samuel), the soldiers 8000, the waves spare Shaw while a soldier stands
  (the defence's spare, as 1052), and wave 1 lost its second grenadier.
- 2009: Starling the giver and receiver (Q21); five pieces, one per map region and one in the middle east (the triage); XP
  and credits Starling's 498's; the model rocket fixed.
- Set 13 NPCs (ids 760362-760372): Hax a copy of Fitz's row and look, Sakai of Bosley's, both public in the facility by the
  hospital and their missions' scene actors, the escort stats; Sakai's package a server-only one (his id). The newbie
  medic a copy of DuBois'. The Thrax Commander the Marshes one at level 38 with the Plains level 38 one's hit points;
  Larvax, Xynnr and Mesgel copies of the Marshes Caretaker at level 38 with 10000, 12500 and 15000 HP. The prisoner a
  friendly copy of the Maligo Base Prisoner, no attack. Russell a copy of Holloway, Brock of Perdu, scene actors only.
  Jumna a copy of Veryin (the hologram Eloh: the client has no other Eloh NPC with a body) at level 45. Teleporters 907
  (the lake) and 908 (the chamber), linked both ways, public. Next free id 760373, next free teleporter id 909.
- Set 14 NPCs (ids 760373-760375): Qunle and the encampment's elder copies of Toida's row and look (levels 35 and 34),
  public in the Village Ruins; Qunle's package a server-only one (his id), the elder's the client's 958 with the plain
  name "Forean Elder" (9626: package 958 names no one). Myrquar a copy of the Marshes Thrax Commander at level 38 with
  the Plains level 38 one's hit points, a scene actor only. No teleporter. Next free id 760376, next free teleporter id 909.
- 1076: Ten Ton Hammer's travel point and spike place read as Village Ruins /locs (the triage's conflict 39); the spot
  1869's class with the client's name 273 "AFS Communication Spike" (no signal-spike name in the client); the stacks
  TaRapedia's.
- 893: the receiver the encampment's elder at He'kal Das (the client's package, no name); offered at 1076's turn-in and on
  arrival in the Village Ruins; XP 30000 (TaRapedia none); the marker named "Undefended Village" (10000003, by name only).
- 1078: Myrquar by the landing pads (the offer: "shipping out"), on the walked-to ground south of them (the pads are a
  navmesh island); four guards; 1500 s from the accept (the offer's 25 minutes); the key at his death (TaRapedia's bug note);
  the element the Disperse Logos (the triage's conflict 19); three pieces of equipment and one crate (no source gives
  counts); XP 65000 (TaRapedia none); weapons the band rule for level 33; the marker named "Bane Equipment" (72, by name).
- 1077: Qunle's key at the accept (the client's offer text; TaRapedia's bug note says he does not give it: the triage's
  conflict 7); the receiver Toida in Paludos (the client's objective; TaRapedia says P'reo Das: conflict 6); 1078's lab at
  other places; XP 35000, no item (TaRapedia gives credits only).
- Set 15 NPCs (ids 760376-760381): Tola a copy of Veska's row and look (the spear), Kirii of Toida's, Rakla of Siva's
  (the staff), level 35, friendly, server-only packages (their ids), public in the Village Ruins: Tola on the walked-to
  ground by the arrival, Kirii and Rakla at P'reo Das beside Qunle. The three Forean Prisoners copies of the Torcastra
  prisoner's row (no weapon, no attack: the patch notes' unarmed prisoners) at level 35, neutral (as 542's prisoner
  leader), one row a camp with the client's package; scene actors only. No teleporter. Next free id 760382, next free
  teleporter id 909.
- 891: the attack starts on walking into P'reo Das (30 m); three waves of 4, 5 and 6 (the triage's sizes) at 0, 40 and
  80 s from the village's north edge (the walkthrough's "north"); the Marshes Thrax Soldier and Grenadier; no fail.
- 892: six corpses (TaRapedia's "at least 4" and "at least 2"), one relic each, six to finish (the triage's Q12/14); the
  knapsack on the corpse as 917's Helka; the item block 96333-96336 (the client's block of TaRapedia's four, in order).
- 894: the "something that should aid you" the instance's own bomb item, Forean Explosive Device, four; the pumps the
  client's Toxic Waste Pump with 913's Detonation Device beside each (TaRapedia's "detonator acceptors"); their places
  before Central Operations' entrance (the triage's "round Central Operations": the building's other sides are off the
  navmesh); the camps' places by their huts; two guards a camp; the E'bae Das camp at the hut and platforms round
  (60, -120) (no map_region row names it); the weapons the band rule for level 35 (no client block holds the four).
- Safe ground (migration `20271104001700_Marshes_safe_ground`, to-fix rows 1 and 4): upstream has no flag that keeps a
  public quest giver alive, so Dikah (1875), Qunle, the Forean Elder, Kirii and Rakla moved to the nearest walked-to spot
  at least 16 m outside every hostile pool's circle (navmesh probe 2026-11-03; ESTIMATE places, markers moved with them).
  Dikah has upstream Ranger Anjuhi's stats (4500 hit points, armour 300): without a creature_stat row the server gives an
  NPC 100 hit points (the creature row's max_hp is not read). Since to-fix 17 all our Marshes pool NPCs have a stat row;
  the upstream givers keep 100 hit points.
- Sets 1-4 rewards (migration `20271104001600_Marshes_reward_blocks`): 541, 498, 539, 917, 1673, 968 and 1029 take the
  client's per-mission template block whose four kinds and levels are the mission's (as sets 5+); 968's block has the staff
  before the shotgun and the Area healing disc (TaRapedia's "Conical"). No block: 881 (no block holds its four kinds), 890,
  913 and 1030 (no item names on any source), 959 (modules), 961, 925 and 1028 (stacks): unchanged.
- Set 16 NPC (id 760382): C-6801 Copilot Ortega a copy of Lieutenant Morrison's row and six appearance slots (the AFS
  officers' look), the client's name 6483 and package 542, level 35, friendly, public in the depot, 7 m from the camp's
  hospital on the walked-to ground. Resnik and Raven are not added (no objective names them). No teleporter. Next free id
  760383, next free teleporter id 909.
- 664 and 919: offered by radio on arrival in the depot (TaRapedia's "Radio, at Bane Supply Depot"; the triage's Q10/Q11).
- 664: the camp 20 m round the hospital; 10 minutes (the 2008-01-17 patch notes' timer) to reach it; three waves of four (the
  triage's 12) at 0, 30 and 60 s from the camp's south and west; 5 the fight, opened by 1, with the client's counter 6254 "Bane
  Remaining" (to-fix 5); the crates by the wreck, one item each (the patch notes' "once per player"); Ortega's talk is the
  turn-in (the client's objective talks 664,5 and 664,3 at package 542 are not used).
- 919: three consoles, one blade each (no source gives a count): the player's own Bane computer beside each of the first
  three remote terminals of the client map; the command-console cluster (205-211, 96, -148) is not reachable from the
  camp's navmesh.
- 663: the level-37 requirement (TaRapedia); the alarm a first hit on four front guards (Slay on Target's
  DamagedSequences), not a camera; the lower entrance the client map's second industrial entrance; 6 by the 13 depot
  mortars (World DB pools 592144-592156) or one terminal use; both ways optional, hidden 5 (required) on either; 6 open
  from the accept.
- 919: the four armor pieces are TaRapedia's cached names; the templates are the Uncommon base templates (lowest version)
  of those kinds and slots in the level-35 bands (19008, 38932, 23937, 29073), as 894's weapons.
- 546: six room Thrax, waves of 4 at 10/50/90 s, a 120 s hack (no source gives counts or times); the item block 120017-120020
  (the client block nearest TaRapedia's four, ESTIMATE). Hax has 12000 hit points (ESTIMATE: three times the escort's, as
  1052's Samuel; review list 2026-11-04, migration `20271104003300_Marshes_hax_holds`): with the escort's 4000 he died
  22-24 s after wave 1 in three real-client runs. The waves' targets stay (half Hax, half the medic): Trapped!'s spare is
  the defence script's, not data.sequence's.
- 548: the keypass is given at the Commander's death (TR MMO DB "from a boss in the control room"), not a loot drop; two
  prisoners a block; Sakai held in block C; the item block 96268-96271 (ESTIMATE).
- 550: the bomb crate by the south bio lab, the tanks extraction drones in the two bio labs, the console at the analysis
  lab's door (no forcefield object), the element taken at the analyzer's keyboard (not the facility's Logos shrine:
  the triage's conflict 20); 5 minutes (TaRapedia's "escape timer" was raised after 2007-07, no value); XP 80000 (no source).
- 718: the bosses one a room in the three labs at y 289, 2 pieces of equipment a room, the dispenser 881's crate class; the
  alarm at Mesgel's first hit, the teleporter in the Test Chamber, 4 Thrax every 30 s, 6 waves at most; stacks 2/4/2.
- 718: Mesgel dead before the alarm (no player-side hit, for example a GM kill): no teleporter, so a server-only hidden 8
  completes 6 at once; after the alarm the teleporter must still be destroyed (to-fix 9).
- 1674: the receiver Jumna (the client's texts and objective; TaRapedia's infobox says Vincent); the pads have no mission
  gate (the client has none).


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
- 1865: the drops have no area on the server, so Thrax Technicians anywhere on the Marshes drop the fragments.
- 1867: delivered straight to Masai, objective 2 stays open (optional) and Duvall no longer asks (his choice needs the
  gun).
- 889: Price's path from the road ends 15 m short of him (Paludos's navmesh is cut off), so 2 completes within 25 m.
- 1023: only group 1 is timed (TaRapedia: 10 minutes per group): the server keeps one deadline per character and
  mission (MissionDeadlineService, upstream). Abandoned, objective 1 shows Failed whatever the leg (upstream
  TryPlanAuthoredAbandonment, cosmetic).
- 497, 888, 889, 1023: a leg completes when the player walks in; where the followers are is not checked (as These Old
  Bones), ESTIMATE.
- 1023: abandoned, the mission fails (the server fails a timed mission at an abandon, as Daghda's Urn); the group waits
  until the failed mission is cleared from the log.
- 497, 888, 889, 1023: a follower is never put back after a death; a failed escort must be cleared and taken again.
- 1052: TaRapedia's "the mission fails if you leave the vicinity" is left out (no such fail in the scene); only Samuel's
  death fails it. The defence is timed from the talk, not from the accept.
- 916 / 1033: the Overseers are public creatures: a kill by another player counts for whoever has the mission (as 968).
- 1085 / 1087 / 1088: no teleporter object by Karl (Ten Ton Hammer): Foster's talk is the way in. A player who dies in the
  dome respawns at a hospital; the fight timer still runs, and at its end the player is set down by Foster after the respawn
  (from the hospital to the arena exit, a harmless surprise; not tested). The 1085/1087 count shows on counter 0 only (the
  client also names counter 1).
- 1085 / 1087 / 1088: the dome's floor is a navmesh island. A player left there with no scene (abandon, a server restart or
  error) is not stranded: a one-way local teleporter on the dome's west floor (teleporter 905) takes them to a pad at the exit
  (906). It is not retail. /suicide also gets them out. A player who uses the pad in a fight leaves it; the fight then runs out.
- 1088: the fall is "survive 5 minutes with Toro standing"; the player is not checked to be alive (lenient: it completes after a
  death too).
- 1084: a Forean Machina's first death is a self revive (upstream CreatureSupport): kill it twice, its implant drops at the second.
- 542: one prisoner, not the debriefing's three: only a squad's leader can be a bystander (NpcSquads). He is NEUTRAL
  (faction 3, as the Pools' Nakrinx): friendly, his own Thrax guards fight him and the caravan stands; a player may shoot
  him. The prisoners are not freed in the world: left alone, the prisoner walks on until the caravan is sent again.
- 864: the "urn on a Forean altar" is a Forean brazier (no urn class in the client); one use places all four icons (the
  counter goes 0 to 4 at once). A use with fewer than four icons fails silently: the brazier stays ready and nothing counts
  (abandon and accept again to recover; review finding, not tested). The temple's upper floor (Bargas, the braziers) is a
  navmesh island: no walk from the Paludos or Falcon Hold waypoint (players stood there, TaRapedia); a real-client playtest
  item: can a player climb up there?
- 864: a Forean Machina's first death is a self revive (upstream): kill each twice for its icon.
- 920: no Bane dropship: the waves come on foot (the Marshes dropships are on their own branch, and no scene spawns by
  dropship). Meeko does not walk up to the player: he appears by Shaw. Meeko is invulnerable: a killed scene actor stays dead for its run, so
  the receiver could not come back without a relog.
- 2009: "only one of the five Soyuz Salvage missions" is not enforced (only 2007 and 2009 are on the server).
- 916 / 1033: the kiosks stand in the open round the Overseers; the client's objective is "Destroy the Production Matrix
  in the Strider Bay" (underground, the triage's Q13).
- 914: abandoned after the theft, the supplies go. The flag is set when a way is chosen; an abandon does not clear it.
- 546: Hax and Sakai are scene actors only their owner sees (OwnerOnly): squad mates see the public Hax by the hospital, not
  the one in the hack or Sakai in his cell.
- 548: the keypass is given at the Commander's death, not dropped as loot; the prisoners vanish when freed (no walk out).
- 550: no forcefield object (the client has none to place): the console stands at the lab's door. The element is taken
  at the analyzer's keyboard, not at the facility's Logos shrine. A death after the element is no escape: the player gets up at the door inside the entrance hall, but only a walk into the hall counts (a revive or a step inside it does not, tested); a player who then walks out of the hall and back in (about 12 m) escapes in time (lenient: the server has no mission event for the owner's death, so a death cannot fail the escape; no engine change). The self destruct is a set-down by Russell,
  not a knock-out.
- 718: the bosses drop no keycards (TaRapedia names keycards 1-3: no objective takes them). The teleporter stands in the
  Test Chamber, about 100 m from Mesgel's room; at most 6 waves.
- 1674: the lake's teleporter is public, not gated by the mission (the client has no gate on it).
- Set 14: the Village Ruins arrival (-138.5, 63, -360) is a small navmesh island about 4 m short of the main mesh (probe
  2026-11-03; NPCs cannot path to the door; players walk it). The landing pads are an island too: Myrquar stands south of
  them. Open upstream PR #214 regenerates this navmesh: places to check again after it.
- 1077, 1078: the Security Clearance Pass opens nothing: Central Operations has no locked door on the server (no
  key-gated door mechanic). 893: the "Bane attack" on the village is not built (the client has only the contact).
- 1077: the Logos and the lab are open together from the accept, in any order (the client lists the lab after the Logos):
  an owned Logos completes 1 inside the accept, where the engine cannot queue a scene sequence yet (StartScenario from an
  existing-fact completion at accept fails the accept), so the lab cannot wait for 1.
- 893 and 1078: one fork, a cross-mission scene signal. The elder's talk (893's 1) and Myrquar's death (1078's 1) each
  start a sequence that signals the other mission's "closed by the other" scenario; that mission's objective then fails
  (a required objective: the mission fails). A signal to a mission the player does not hold does nothing, so each talk is
  the client's one flag-1 topic in every case (no server-only flag, no FailRelatedMission, which throws when the other is
  not held). 1078's choice is made at Myrquar's death, not at Toida's talk (893's offer: "not enough time to both save
  this village and intercept the next Bane convoy"); a signal from 1078's bound scene queued on the Marshes (at Toida)
  does not reach 1743.
  The choice is also kept in the mission flag 893001 (893 or 1078, set by the elder's talk or Myrquar's death): each
  mission's offers and scene need the flag not to hold the other, so a path taken only after the other's choice is
  closed. The flag stays at an abandon or a fail: a player who abandons 1078 after Myrquar's death cannot take 893
  (ESTIMATE, by design). 893's scene is bound to the squad's Village Ruins, as the other three.
- 1077 and 1078: each has its own lab (a player with both sees two sets in the control room); TaRapedia's P'reo Das bug
  (a failed 1078 takes Myrquar's key from 1077) is not built: 1077's key is Qunle's.
- 1078: Myrquar's leaving is a scene timer of the deadline's length beside the objective's own deadline (the deadline's
  failure does not take the scene's actors away; 893's signal starts 1078's ShipsOut sequence, which does).
- 894: each camp's prisoner and survivor share the client's package (objectiveconversation 894,2 and 14 are both 1158, and
  so on); the survivors have their own creature rows (760383-760385, copies of the prisoners), and each talk is bound to
  its own NPC, so the prisoner offers only the rescue and the survivor only his own talk; each talk sets only its own NPC
  following (to-fix 16). 12 checks the player at the
  arrival, not the prisoners (as Pools 1136's return); a prisoner's death fails nothing. The prisoners follow until the
  turn-in, which takes them away. The client's older "0/4" rescue count (TaRapedia history) is not built: the client
  1.16.5 counter is "Forean Camps Freed" over three camps.
- 891: the waves come on timers, not when the previous one is dead; the Thrax attack the player, not Tola's village or
  Kirii (no village NPC fights).
- 663: no camera or alarm detection (NOTES-missing-mechanics.md "Detection (stealth)"): the alarm is a first hit on one of
  four scene guards at the front entrance. A player who never hits them and walks to the lower entrance completes 3; a
  fight elsewhere in the depot does not sound the alarm. A hit after 3 is done completes 4 too (both optional).
- 664: the waves come on timers, not when the previous one is dead; the Thrax attack the player, not Ortega.
- 664: "Bane Remaining" counts up (n/12): the server's counters count up only (upstream Pravus' "Bane Remaining" ambush
  does the same; the tester saw 1 to 12, review list 2026-11-04). The client could show a count down: a counter with
  target 0 shows its value alone, no "/target" (client/gameuiutil.pyo _SetupMissionCounter, ui/bigtextwindow.pyo
  OBJECTIVE_COUNTER_UPDATED_NO_TARGET), so 12, 11 ... 0 would need the server to send the remaining count with target 0,
  in UpdateObjectiveCounter and in the mission status. The mission runtime (MissionRuntime) counts from the initial
  value up to the target, and no counter data says "remaining": an upstream code change, not changed here.
- 664: after a fail it can be taken again once the player clears it (retail: one take until the instance reset).
- 548: a cell terminal's use takes 10 s, and a player who walks off before the end gets nothing and no message (review list
  2026-11-04). Both are upstream's for every scene object without its own windup (DynamicObjectManager.
  DefaultScenarioUseWindupMs, FootlockerRecovery's range check); the client shows the use's progress bar. Not changed:
  no source gives the terminals another time.
- 919: the reward templates are ESTIMATE (the names only are sourced).
- 663: the mortars (and the terminal) count before a way in (retail after its 2008-10 fix); the mission still needs a way in.
- 663: the front guards are hostile, so they fight a player who comes near: the stealth way means not shooting back.
- Set 16 navmesh (probe 2026-11-03): the depot's arrival and front entrance (x <= -284) are a navmesh part of their own,
  apart from the main part with the camp, the consoles, the terminal and the lower entrance. Creatures do not path from one
  to the other; the front guards stand on the arrival's part.
- Client use not seen in game (ESTIMATE): the bomb crate 21568 with argument 3, the dispenser 11116 in the facility, the
  extraction drone 11255's states 110 and 2.
  916 and 1033 also need 914 completed, and either choice overwrites it, so a left-over flag offers nothing.

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
