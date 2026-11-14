# Ligo Ashen Desert missions: playtest list, assumptions and deviations

Missions that are implemented (data, migrations and tests pass) but not yet played through in the
game client. A checked box (`- [x]`) means the playtest is done and the mission works.

Format: `- [ ] <mission id> <name> (<zone>): <what to look at>`.

## Ligo Ashen Desert

Set 1, the lead-ins (migration `20271110000000_Ashen_lead_ins`). Map 1734 (`adv_arieki_ligo_ashendesert`), log
category 10000016 "Battlefield (Ashen Desert)". New NPCs 790000-790003, next free id 790004.

- [ ] 1250 The Dustiest Place on Earth (Ligo Ashen Desert): Sergeant Bernard Ames (new, 790000, Ashen Lookout camp by the
  communication tent, 345, 311.14, -818) offers it, no requirement, level 40. Talk to Colonel Mason Linier (moved to the
  Shadow's Edge HQ entrance, -132, 284.05, -875). A marker at Linier. Turn in at Linier: 42500 XP, 4000 credits, one of
  2x 45065, 3x 111029, 1x 47600, 3x 111019. Check the mission log shows it under "Battlefield (Ashen Desert)".
- [ ] 1273 Report to Stonewall Ridge (Ligo Ashen Desert): Captain Flynn Harris (moved to 405, 281.41, -459.5, 8 m from the
  Ashoka waypoint) offers it, no requirement, level 40. Talk to Captain Marcus Emmons (moved to the Stonewall Command
  Tower, -62, 350.48, -414). A marker at Emmons. Turn in at Emmons: 45000 XP, 4100 credits, one of 3x 111039, 2x 45065,
  3x 111029, 1x 47600.
- [ ] 1274 Fithik Infestation (Ligo Ashen Desert): Emmons offers it, no requirement, level 40. Kill 10 Fithik ("Kill
  Fithik" n/10; any creature with the Fithik species flag counts, anywhere). One marker (radius 30) on the Stonewall
  Ridge Fithik pool 540067, since fix job A a point pool on the open east slope of the Ridge (261, 308.6, -642.2): a full
  spawn puts its 2-4 Fithik on the point and the rings round it (within 6.6 m), a single one topped up comes on the point;
  on open ground, also after a respawn (tested). Turn in at
  Emmons: 90000 XP, 6150 credits, one of 123046, 123141, 123166, 123026. No marker on the other Ridge pool 540066: its
  Fithik stand in a navmesh pocket no player reaches (see Deviations).
- [ ] 1283 Good Day to Die (Ligo Ashen Desert): Lt. Colonel Trent Ivan (new, 790001, Shadow's Edge east barracks, -97,
  297.05, -792) offers it, no requirement, level 40. Talk to Captain Akio Yamamoto (new, 790003, Boneyard Outpost eastern
  tent, 686, 289.93, -495). A marker at Yamamoto. Turn in at Yamamoto: 45000 XP, 4100 credits, one of 3x 111019, 6x
  45063, 3x 45447, 1x 111233.
- [ ] 1284 You Can't Drink Ash (Ligo Ashen Desert): Lt. Colonel Dania Caldwell (new, 790002, Shadow's Edge west barracks,
  -158, 297.05, -792) offers it, no requirement, level 40. Talk to Captain Daris Linier (moved 2.0 m from his upstream
  spot, 4.17 m from the White Oasis control point's point, -507.4, 282.73, 162.4). A marker at Daris. Turn in at Daris: 47500 XP, 4200 credits, one of 3x
  111029, 1x 47600, 3x 111019, 6x 45063. Look: Daris is a control point 17 service NPC; he must stand while the point is
  AFS-held.

Set 2, the Mauna Barrens (migration `20271110000100_Ashen_mauna_barrens`). New NPCs 790040-790042, Atta 790043-790045, Atta
pools 790046-790051 (three Atta each; one each since fix G, 20271110001320), next free id 790052. The Mauna Barrens have
no waypoint: walk from Shadow's Edge Post (or White Oasis Post).

- [ ] 1242 Angry Angry Atta (Ligo Ashen Desert): Lieutenant Simon Kuang (new, 790040, Shadow's Edge west gate under the
  "Mauna Barrens" holosign, -205, 297.54, -736) offers it, no requirement, level 40. Kill 10 Ashen Atta Soldiers (counter
  "Atta Soldiers") and 10 Ashen Atta Workers ("Atta Workers") in the Mauna Barrens' south basin: Soldier pools 790046
  (-493, 295.61, -240) and 790047 (-429, 300.18, -240), Worker pools 790048 (-461, 298.32, -216) and 790049 (-461, 297.3,
  -264); a marker r20 on each. Turn in at Kuang: 90000 XP, 6150 credits, one of 49616, 48712, 48300, 15025 (weapons).
  Look: the Atta names and levels (Ashen Atta Soldier L40, Ashen Atta Worker L40), and that they count for Targets of
  Opportunity 1580's "50 Atta".
- [ ] 1243 Harmful Mutations (Ligo Ashen Desert): Kuang offers it, no requirement, level 40. Recover 4 Mutated Atta Blood
  Samples (item 97458; a 50% personal drop) from the Mutated Atta Soldiers (L41) of pools 790050 (-533, 302.79, -256) and
  790051 (-405, 300.97, -272); a marker r20 on each. Turn in at Doctor Jung Kwon (new, 790041, by the Shadow's Edge
  hospital, -89, 296.78, -822): 90000 XP, 6150 credits, one of 34116, 40502, 36787, 31088; she takes the samples.
- [ ] 1244 Chemical Oversight (Ligo Ashen Desert): Kuang offers it after 1242, level 41. Kill Thrax Overseer Exing (L44;
  pool 520007 moved off its navmesh island: set 2 to -289, 332.98, -264 on the mesa slope, fix B to -260, 308.9, -310 on
  the flat at the mesa's south foot, west of the Sheitan Highway). A marker r15 on him. Walk from the Shadow's Edge Post
  waypoint, or on from the Mauna Barrens basin or the highway camp. Turn in at Kuang: 90000 XP, 6150 credits, one of 28634,
  38952, 18770, 35740. "Bring some friends" (a group boss).
- [ ] 1245 Eyes on the Ground (Ligo Ashen Desert): Captain Sera Karter (new, 790042, by the Shadow's Edge dropship pad,
  -112, 296.19, -748) offers it after 1243, level 41, and gives 5 Atta Sensor Beacons (97460). Plant one at each of five
  personal spots (class 7847, named "Atta Sensor Beacon") in the Barrens, any order: 1 (-431, 304.07, -203), 2 (-554,
  304.23, -230), 3 (-596, 303.04, -83), 4 (-476, 328.91, -17), 5 (-338, 293.53, -80); a marker r8 on each, named "Atta
  Beacon Point 1" .. "5" on the map. Turn in at Karter: 90000 XP, 6150 credits, one of 33987, 23698, 21100, 46013. Look:
  the spot's name and the planted beacon model; the map labels of the markers.

Set 3, the Sheitan Highway (migration `20271110000200_Ashen_sheitan_highway`). New Desert Predator 790080, mortar copies
790081-790084, Thrax pools 790085-790088, Predator pools 790089-790090, next free id 790091. The highway has no waypoint:
walk north from Shadow's Edge Post along the road; the Bane camp is at (-160, 303, -300).

- [ ] 1247 Highway to Hell (Ligo Ashen Desert): Captain Sera Karter (790042, Shadow's Edge dropship pad) offers it, no
  requirement, level 41. Kill 10 Thrax Ravagers (counter "Thrax Ravagers"; the Ashen Thrax Grenadier 531009, L39, now
  named "Thrax Ravager") and 10 Thrax Soldiers (counter "Thrax Soldiers"; Thrax Soldier Mark VI 531008, L39). Ravager
  pools of two 790085 (-213, 307.87, -302) and 790086 (-125, 314.27, -359) on the camp's west and south-east edge (fix E;
  set 3 had three each at (-180, -305) and (-140, -305), inside the camp), Soldier pools 790087 (-160, 304.19, -260)
  and 790088 (-120, 304.81, -225) at and north of its north end; a marker r20 on each. The camp's own pool 531034
  (Soldiers and others) counts too. Turn in at Karter: 180000 XP, 8200 credits, one of 49576, 45777, 47948, 45762
  (weapons). Look: the "Thrax Ravager" name over the grenadiers (also in the Bane Conscription Facility and Indra Caverns).
- [ ] 1248 Air Superiority (Ligo Ashen Desert): Lt. Colonel Trent Ivan (790001, Shadow's Edge east barracks) offers it, no
  requirement, level 42. Destroy 1 Desert Predator (new, 790080, L42, 4700 HP; counter "Desert Predators"): two walk beats
  on the road south of the camp, pool 790089 from (-170.6, 295.8, -515.6) to (-160, 299.52, -440) (home, the pool's point,
  at -173.3, 297.11, -478.7) and pool 790090 from (-160, 297.59, -425) to (-160, 301.35, -365) (home at -162, 299.39,
  -400), each a loop from home to both ends; a marker on each beat (r50 at -174.4, -482.2; r40 at -161.6, -392.2). Turn in at Ivan: 180000 XP, 8200 credits, one of 12065, 11953, 47772, 15111 (weapons). Look: the Predators walk
  their beats on the road (not through the ground), fight and go back to the beat; "get some help" (a group mission).
- [ ] 1249 Artillery Annihilation (Ligo Ashen Desert): Lt. Colonel Dania Caldwell (790002, Shadow's Edge west barracks)
  offers it, no requirement, level 41. Destroy Mortar 1-4 (any order): the Bane mortars on the pads by the camp, pool
  592191 (-210.9, 315.29, -409.7), 592192 (-195.6, 317.34, -337.7), 592193 (-180.6, 310.17, -282.7), 592194 (-140.3,
  310.43, -285.3) (L42, 17500 HP); a marker r10 on each, named "Bane Heavy Mortar 1" .. "4" on the map. Turn in at
  Caldwell: 90000 XP, 6150 credits, one of 24086, 20504, 36188, 19286 (armour). Look: each mortar ticks off its own
  objective; the map labels of the markers.

Set 4, the Boneyard (migration `20271110000400_Ashen_boneyard`). New Lieutenant Gabriel Infanger 790120, Desert Stalker
790121, Xanx pools 790122-790124, Stalker pools 790125-790126, next free id 790127. Boneyard Outpost and the Boneyard have
no waypoint: walk north-east from the Ashoka Settlement waypoint (up out of Ashoka) to the outpost (682, 290, -516) and the
Boneyard (579, 283, -291).

- [ ] 1262 Xanxless Job (Ligo Ashen Desert): Lieutenant Jan Blood (upstream 510013, Boneyard Outpost, 682.4, 289.56,
  -516.4, where upstream has him) offers it, no requirement, level 40. Kill 6 Xanx (counter "Xanx Killed"; the World DB's
  Xanx - Ashen Desert 540020, "Xanx Burrower", L39): pools 790122 (560, 283.41, -250), 790123 (600, 282.49, -230), 790124
  (640, 282.85, -250) of three in the Boneyard; a marker r20 on each. Turn in at Blood: 95000 XP, 6300 credits, one of
  46029, 34374, 23569, 31241 (armour). Look: the Xanx come down and back on the Boneyard floor (not under it).
- [ ] 1263 Fallen Soldiers (Ligo Ashen Desert): Captain Akio Yamamoto (790003, Boneyard Outpost eastern tent) offers it, no
  requirement, level 40. Six personal "Fallen Soldier" bodies in the Boneyard, one Ashen AFS Dog Tag (97464) each, any
  order: (483, 286.56, -379), (520, 283.38, -308), (507, 282.89, -321), (544, 285.58, -352), (519, 283.51, -306), (500,
  286.56, -345); one marker r55 over them (512.5, -335.5); the objective shows n/6. Turn in at Yamamoto (he takes the six):
  95000 XP, 6300 credits, one of 49616, 49536, 15301, 14861 (weapons). Look: the bodies' model and name, the tag counter.
- [ ] 1272 Stalking the Stalkers (Ligo Ashen Desert): Lieutenant Gabriel Infanger (new, 790120, Boneyard Outpost, 676,
  289.65, -505) offers it, no requirement, level 41. Destroy 1 Desert Stalker (new, 790121, "Class XI Desert Stalker", L40,
  4500 HP; counter "Desert Stalkers"): pools of one 790125 (580, 282.85, -345) and 790126 (585, 283.58, -370) by
  TaRapedia's spot (595, -354); a marker r15 on each. Turn in at Infanger: 190000 XP, 8400 credits, one of 15335, 48108,
  12125, 14534 (weapons). Look: the Stalker on the Boneyard floor, not on Ashoka's lower level under it; "Class XI Desert
  Stalker" over it.

Set 5, the Bane Conscription Base (migration `20271110000500_Ashen_conscription_base`). New trench mortar copies
790160-790163, Linker pools 790164-790168, next free id 790169; Overseer Naxveik's pool 520005 moved. The three need 1262
and 1263 done. The base and the trench have no waypoint a player holds (the Conscription Garrison is Bane's control point
16): walk north from the Ashoka Settlement waypoint through the Boneyard.

- [ ] 1352 Death from Above (Ligo Ashen Desert): Captain Akio Yamamoto (790003, Boneyard Outpost) offers it, level 41, and
  gives 6 High Grade Plastic Explosives (97491) at the accept (a bag with no room refuses the accept). Destroy Mortar 1-4: the trench pools 592199 (440.3, 288.13,
  -201.0), 592202 (490.1, 281.22, -194.2), 592203 (524.7, 283.71, -170.6), 592204 (550.6, 287.55, -130.5), west to east,
  each its own mortar copy, a marker r10 on each; objective 11 "Mortars Destroyed" n/4 (the count of Mortar 1-4 done:
  a mortar killed again does not count again), a marker r75 over the trench.
  Destroy Launcher 1-6: six personal Missile Launchers (21873) in the walled yard north of the Garrison, two rows of three
  at x 376, 400, 428 and z 128 / 144; use the Detonation Device 11 m off each launcher's point, south of the south row
  and north of the north row (devices at (376, 117), (412, 117), (428, 117), (376, 155), (416, 155), (424, 155); each
  takes one explosive); a marker r10 on each device. Turn in at Yamamoto: 200000 XP, 8600 credits, one of 12099, 15442, 14980, 48108 (weapons).
  Look: the launcher model, its facing (the model is 22 m long; facing 0 is not verified, the ground is flat for it
  either way along its row and each device stands outside it either way) and its blown state; the mortar counter.
- [ ] 1354 Turning Out the Lights (Ligo Ashen Desert): Lieutenant Gabriel Infanger (790120, Boneyard Outpost) offers it,
  level 41, and gives 3 Electromagnetic Charged Explosives (111184) at the accept. Destroy Power Generator 1-3: three
  personal small Bane generators (20594, named "Power Generator") on the plateau west of the Garrison, (318, 296.2, -14),
  (326, 296.7, -34), (314, 296.3, -52); use the Detonation Device 6 m west of each; a marker r8 on each generator. Turn in
  at Infanger: 100000 XP, 6450 credits, one of 19802, 39081, 20951, 31394 (armour). Look: the generators' look and name,
  the blown state; Naxveik (L44) stands among them.
- [ ] 1355 The Strongest Link (Ligo Ashen Desert): Lieutenant Jan Blood (510013, Boneyard Outpost) offers it, level 42,
  shareable (TaRapedia updates 2007-08-21 list "Mission is not shareable" as a known issue).
  Kill 15 Linkers (the World DB's Linker - Ashen Desert 531014, "Linker Master", L40; counter "Linkers Killed"): pools of
  three 790164 (385, 304.31, 82), 790165 (420, 299.37, 95), 790166 (466, 304.06, 132), 790167 (430, 304.13, 160), 790168
  (358, 304.16, 160) round the walled base, a marker r15 on each. Kill Overseer Naxveik (520005, L44) at (296, 296.49,
  -30), a marker r15. Turn in at Blood: 200000 XP, 8600 credits, one of 33342, 24086, 19544, 36638 (armour). Look: the
  Linkers can be hit and killed (to-fix row 2 was an Atta hit problem; Linker class 9751 not yet tried in the client).

Set 6, Formal Complaints and the AFS side (migration `20271110000600_Ashen_formal_complaints`). New: Scavenger Vakiel
Latan 790200 (576, 248.65, -321) and Shinichiro Ito 790201 (560, 248.65, -320) on Ashoka's lower level (the hospital's
floor), Megumi Yamamoto 790202 (-166, 296.98, -895, Shadow's Edge Post, south enclosure); next free id 790203. The Boneyard
Thrax pools 531038, 531039, 531041 made point pools on open ground. Walk to Latan and Ito from the Ashoka Settlement
hospital, the Ashoka Upper teleporter (613) or down the market's ramp (the server navmesh does not join the ramp top to
the waypoint's terrace: N18; the client floor does).

- [ ] 1254 Formal Complaints (Ligo Ashen Desert): Latan offers it, level 40. Objective 1: talk to Latan and choose:
  choice 1 (side with Latan) sets player flag 250 to 1 and 251 to 0 and opens 2 "talk to Latan" (her variant 250);
  choice 2 (hear Yamamoto first) sets 251 to 1 and 250 to 0 and opens 3 (variant 251). Hidden objective 4 is done by 2 or 3.
  Turn in at Latan: 45000 XP, 4100 credits, one of 111029 x3, 47600, 111019 x3, 45063 x6. Flag 250 leads to 1264-1266
  (set 7), 251 to 1258. Look: the choice window and both answer texts.
- [ ] 1258 The Human Perspective (Ligo Ashen Desert): Ito offers it after 1254 with flag 251 at 1, level 41. Talk to
  Captain Akio Yamamoto (790003, Boneyard Outpost; variant 251). Turn in at Latan: 45000 XP, 4100 credits, the 1254 stacks.
- [ ] 1259 A Bribe for Your Thoughts (Ligo Ashen Desert): Latan offers it after 1258, level 41; Latan's Package (97462) at
  the accept (a full bag refuses the accept). Give it to Yamamoto (variant 251); turn in at Yamamoto: 45000 XP, 4100
  credits, the 1254 stacks.
- [ ] 1261 Court Martial Arts (Ligo Ashen Desert): Yamamoto offers it after 1259, level 41; the package again at the
  accept. Give it to Colonel Mason Linier (510010, Shadow's Edge HQ entrance; variant 251); turn in at Linier: 45000 XP,
  4100 credits, one of 45447 x3, 111233, 111049 x3, 45064 x3.
- [ ] 1285 Dearly Departed (Ligo Ashen Desert): Megumi offers it after 1261, level 42. Yamamoto's Wedding Ring (97481): a
  personal 15 % drop off a Thrax Soldier (531008) or Thrax Ravager (531009) anywhere on Ashen Desert while 1 is open, one
  at most; markers r25 on the Boneyard Thrax pools 531038 (566, 285.1, -136), 531039 (487, 286.45, -261.3), 531041 (487,
  271.9, -219). Turn in at Yamamoto (he takes the ring): 190000 XP, 8400 credits, one of 38823, 23828, 20802, 29093
  (armour). Look: the ring drops and is picked up; the Thrax stand on the ground at the moved points.

Set 7, Latan's side of Formal Complaints (migration `20271110000700_Ashen_scavenger`). New: the Nitroglazer 790240 and
the Hominis Machina Mk XI 790241 (both level 40) in the Boneyard, pools of three 790243 (700, 282.2, -250) and 790244
(690, 281.05, -310) (Nitroglazers, east), 790245 (440, 287.89, -320) and 790246 (440, 285.32, -360) (Hominis Machina,
west); next free id 790247. Only for a character who chose Latan's side (choice 1) and finished Formal Complaints.

- [ ] 1264 Robotic Salvage Recovery (Ligo Ashen Desert): Latan offers it after 1254 done with flag 250 at 1, level 40.
  Recover 15 Intact Nitroglazer Salvage (97465) and 15 Intact Machina Salvage (97466): personal 75 % drops, one a kill,
  off the Nitroglazers and the Hominis Machina; markers r20 on the four pools. Turn in at Latan (she takes the 30): 95000
  XP, 6300 credits, one of 23698, 29113 (fix D; set 7 had 29093), 20802, 36039 (armour). Look: the robots' look, name and level (Nitroglazer 434,
  "Hominis Machina Mk XI" 7716), their attacks (the Nitroglazer's mortar), the Machina's revive (upstream action 62059,
  not tried); a player of choice 2, or one who took choice 1 and abandoned 1254, is not offered it.
- [ ] 1266 Weapon Salvage Recovery (Ligo Ashen Desert): Latan offers it after 1264, level 41. Recover 15 Thrax Rifle
  Salvage (97469) off Thrax Soldiers (531008) and 15 Thrax Grenade Launcher Salvage (97470) off Thrax Ravagers (531009):
  personal 75 % drops anywhere on Ashen Desert; markers r25 on the Boneyard Thrax pools (rifles) and on the Sheitan
  Highway Ravager pools 790085, 790086 (launchers; fix E: on the camp's edge, two each). Turn in at Latan: 95000 XP, 6300 credits, the Incendiary Injector
  Gun 45777.
- [ ] 1265 Heavy Salvage Recovery (Ligo Ashen Desert): Latan offers it after 1266, level 42. Recover 1 Intact Stalker
  Salvage (97467) off a Desert Stalker (790121, Boneyard pools 790125, 790126) and 1 Intact Predator Salvage (97468) off
  a Desert Predator (790080, the Sheitan Highway beats): every kill drops it while it is needed; markers as 1272's and
  1248's. Turn in at Latan: 200000 XP, 6300 credits, one of the 1254 stacks.

Set 8, the AFS side of Formal Complaints (migration `20271110000800_Ashen_logistician`). No new world rows: the three
use set 7's robots and pools, the Thrax rows 531008 / 531009, set 3's Desert Predator and set 4's Desert Stalker. Only for
a character who chose Yamamoto (choice 2), finished Formal Complaints and the chain up to Dearly Departed (1285). Ito
stands on Ashoka's lower level: walk to him from the Ashoka Settlement waypoint through the Brann corridor under the
dome (fix C: mouth at (404, -472), down to the market, the east and cross corridors), or from the Ashoka hospital or the
Ashoka Upper teleporter (613). The straight line to the market ramp is walled off.

- [ ] 1267 Robotic Salvage Acquisitions (Ligo Ashen Desert): Ito offers it after 1285 and 1254 done with flag 251 at 1,
  level 42. Recover 15 Intact Nitroglazer Salvage (97465) and 15 Intact Machina Salvage (97466): personal 75 % drops off
  set 7's Nitroglazers and Hominis Machina; markers r20 on the four pools. Turn in at Ito (he takes the 30): 95000 XP,
  6300 credits, one of the Armor Modules Resist Electric / Fire / Ice / Virulent [4] (123016, 123026, 123031, 123071).
  Look: a player of choice 1, or one who took choice 2 and abandoned 1254, is not offered it; Latan offers nothing.
- [ ] 1269 Weapon Salvage Acquisitions (Ligo Ashen Desert): Ito offers it after 1267, level 42. 15 Thrax Rifle Salvage
  (97469) off Thrax Soldiers and 15 Thrax Grenade Launcher Salvage (97470) off Thrax Ravagers, personal 75 % drops
  anywhere on Ashen Desert; markers r25 on the Boneyard Thrax pools 531038, 531039, 531041 (rifles) and the highway
  Ravager pools 790085, 790086 (launchers; fix E: on the camp's edge, two each). Turn in at Ito: 95000 XP, 6300 credits, one of Cryogenic Blade 12101, Area
  Healing Disc 119882, Sonic Pistol 14452, Pulse Staff 15220.
- [ ] 1268 Heavy Salvage Acquisitions (Ligo Ashen Desert): Ito offers it after 1269, level 42. 1 Intact Stalker Salvage
  (97467) off a Desert Stalker (Boneyard pools 790125, 790126) and 1 Intact Predator Salvage (97468) off a Desert Predator
  (the Sheitan Highway beats), every kill while needed; markers as 1265's. Turn in at Ito: 200000 XP, 6300 credits, one
  of the 1254 stacks. The chain ends (TaRapedia: title "Desert Logistician", the Targets of Opportunity's, not built).

Set 9, Stonewall Ridge (migration `20271110000900_Ashen_stonewall_ridge`). No new world rows: the salvage crate, the
consoles, the ambushers, the turret copies and the waves are scene actors on existing rows (Bane crate 7904, human monitor
9478, Thrax Soldier 531008, Thrax Ravager 531009, Desert Stalker 790121, AFS Turret 550001). Emmons stands by the
Stonewall Command tower (-62, 350.5, -414), 13 m from its ground teleporter 539. Walks east of the waypoint go round the
knee-high rise at (21.6, -437.0) (e.g. by (18.5, -429.6) and (32.8, -438.1)).

- [ ] 1275 Cycled Frequencies (Ligo Ashen Desert): Emmons offers it (no requirement), level 40. Search the Bane Dropship
  Salvage crate (537, 283.9, -267) by the Boneyard wreck at (537, 282, -279) (walk from the Ashoka Settlement waypoint):
  the Bane Radar Frequency Codes (97475). Turn in at Emmons: 95000 XP, 6300 credits, one of Sonic Rifle 14658,
  Endothermic Polarity Gun 48712, Sonic RPG Launcher 49172, Pulse Shotgun 15025. Look: the crate's look and name
  ("Bane Dropship Salvage"), the open animation, the use with a full bag (codes stay in the crate).
- [ ] 1277 On The Same Frequency (Ligo Ashen Desert): Emmons offers it after 1275, level 41. On each tower's second floor
  (ground teleporter -> upper: 539 -> 823 Command, 538 -> 824 Operations, 537 -> 825 Artillery Control 1, 540 -> 826
  Artillery Control 2) use the Radar Operations Console 5-6 m off the upper pad, on the side the lift opens to (fix E:
  tower 1 east, 2 east, 3 north-west, 4 west; walk off the pad on that side and straight to the console, not back over
  the pad); any order. The console is used only from its own floor (fix E: not from the floor under it). Towers 2-4:
  three Thrax Soldiers come on the top floor by the lift's exit and attack (fix E; set 9 had them at the tower's foot).
  Markers at the ground teleporters. Turn in at Emmons: 190000 XP, 8400 credits, one of Cryogenic Blade 12101, Electric
  Rifle 11953, Chaingun 48300, Pistol 14409. Look: each console's look (human monitor), name, on-state; the teleporters
  both ways; the console's use right after the ride up (to-fix row 12).
- [ ] 1278 King of the Hill (Ligo Ashen Desert): Emmons offers it after 1277, level 42. Four AFS Turrets of the player's
  own stand at the four batteries (the public ones hidden for the player). 1: reach the batteries (area r60 round (100.5,
  -528.4)) within 3:00 (countdown), else it fails. Then four waves: 0 s three Thrax Soldiers between batteries 2 and 3, 45 s two
  Desert Stalkers and a Ravager by battery 1, 90 s two Soldiers and two Ravagers below battery 3, 135 s two Stalkers
  and two Soldiers below battery 4; they attack the turrets. At 180 s 2 completes; the fourth turret lost (before or after the top: each loss counts) fails the mission. Turn in
  at Emmons: 190000 XP, 8400 credits, one of Bio Helmet 19673, Hazmat Gloves 20504, Reflective Gloves 37985, Motor Assist
  Boots 45843. Look: the copies over the public turrets (no doubled turret for the player; others see the public ones);
  do the waves reach and hurt the turrets on their platforms (navmesh islands); do the turrets shoot the waves; the client
  crash at a turret kill (upstream bug C23); a lost turret's public one comes back for the player at once.

Set 10, Ida Hayner's orders (migration `20271110001000_Ashen_designer_hides`). New world rows 790360-790377: four bosses
(Sally, Molly, Trudy, Wendy, level 44, 4900 HP, one each, 50 s respawn) and three hide creatures (Lasher 790362, Beam
Manta 790367, Granitour 790373, 3 per pool, 120 s respawn) in 11 pools. Ida Hayner (pool 510011) moved 6 m off the weapon
vendor 500018, at whose x/z she stood (-161, 296.57, -710). Hides and carapaces are personal drops while objective 1 is
open (75 %, 10 at most); the pristine piece is a 100 % drop off the boss; Ida takes all at the turn-in, an abandon keeps
them. All places walked both ways on the client floor; corners in AshenDesignerHidesTests.Places.

- [ ] 1287 Designer Jewelry Gone Wrong (Ligo Ashen Desert): Ida offers it (no requirement), level 41. 10 Shiny Atta
  Carapace (97483) off set 2's Ashen Atta Soldiers / Workers (Mauna Barrens pools 790046-790049) and the Pristine Atta
  Carapace (97487) off Sally (-439, 298.47, -298) (fix E, to-fix row 18: 40 m south of TaRapedia's (-439, -258), where
  she stood 20.6 m from Atta pool 790047 and joined the Atta fights; now 40 m and more from every other hostile pool). Turn
  in at Ida: 180000 XP, 8200 credits, one of Motor Assist legs 45986, Mech legs 35019, Bio legs 46069, Graviton legs
  13043. Retest: pull the Atta of 790049 / 790047; Sally should not come. Fix F (to-fix row 22): the camp's Workers call two
  more hostiles within 80 m (the Atta harvester warcry), so a pull at the camp is a group fight; farm the carapaces at
  1287's own pool of Ashen Atta Soldiers 790378 (three; one since fix G) (-355, 328.97, -210) on the slope east of the camp (marker 1/6), 80 m
  from the camp's pools. Sally (fix F, row 18) comes back 300 s after her death (was 50 s). Immunities (row 20): Sally takes
  nothing from Fire and Electrical (class 22627), nor do the Ashen Atta Soldiers (class 7081; the Workers take all): use
  another type (a Sonic rifle killed her, 2026-11-10).
- [ ] 1293 All the Latest Rage! (Ligo Ashen Desert): Ida offers it after 1287, level 42. 10 Shiny Granitour Carapace
  (97484) off the Granitours of Coheri Pools (-185, 520), (-215, 525) and the Pristine Granitour Carapace (97488) off Wendy
  (-180, 286.25, 570) (TaRapedia -190, 580). Walk from the White Oasis Post waypoint. Turn in: 200000 XP, 8600 credits, one
  of Sonic Torqueshell Rifle 45773, Virulent Disperser 12077, Electric Polarity Gun 15550, Laser Pistol 14492.
  Immunities (row 20): Wendy (class 25620) and the Granitours (10164) take nothing from Fire. Wendy comes back after 300 s
  (fix F).
- [ ] 1289 I Wouldn't Wear It, But... (Ligo Ashen Desert): Ida offers it (no requirement), level 41. 10 Lasher Leather
  Hides (97486) off the Lashers by the road west of Ashoka (330, -585), (280, -578) and the Pristine Lasher Hide (97489) off
  Molly (315, 283.02, -622) (fix E, to-fix row 17: set 10 had (299, 295.08, -658), reached only past the Fithik pool
  540067; now 47 m north-east of TaRapedia's notch (275, -659), the walk from the Ashoka Settlement waypoint 30 m and
  more from other hostile pools). Turn in: 180000 XP, 8200 credits, one of Cryogenic
  Injector Gun 48108, Laser Cannon 49108, Pulse Chaingun 15154, Pistol 14409. Immunities (row 20): Molly (class 10331)
  and the Lashers (7477) take nothing from Virulent and Laser. Molly comes back after 300 s (fix F).
- [ ] 1291 A New Fad (Ligo Ashen Desert): Ida offers it after 1289, level 42. 10 Beam Manta Leather Hides (97485) off the
  Beam Manta in the eastern Boneyard (770, -400; fix E: set 10's (764, -322) was walked to past the Nitroglazer pool
  790244), the Indra Pass (216, 486) and northeast Maji Field (-189, 635), and the Pristine Beam Manta Leather (97490) off
  Trudy (-250, 302.74, 632) (fix E: set 10's (-198, 299.48, 648) was 16 m from the Manta pool 790370; TaRapedia's hilltop
  -242, 771 is not joined to the navmesh). Walk to Coheri Pools and Maji Field from the White Oasis Post waypoint by the
  west ridge (-300, 620), not past the Fithik pools 540075 / 540077. Turn in: 190000 XP, 8400 credits, one of Electric Polarity Gun
  15550, Electric Net Gun 12131, Sonic Cannon 49172, Electric Chaingun 15111. Immunities (row 20): Trudy (class 10317) and
  the Beam Manta (6441) take nothing from Electrical: of the four reward weapons only the Sonic Cannon hurts them. Trudy
  comes back after 300 s (fix F).

Set 11, White Oasis Post (migration `20271110001100_Ashen_white_oasis`). New world rows 790400 Doctor Harold Meeks (-498,
282.39, 164) and 790401 Agent Hale (-516, 282.78, 176), both in the post. Lieutenant Dania Fontecilla (pool 510012) moved
7.9 m west to (-519, 283.71, 164), off the White Oasis control point's console (she stood on its spot). All four White Oasis
givers (Daris Linier, Fontecilla, Meeks, Hale) stand within 130 m of control point 17: they are gone while the Bane hold the
post, and come back when the AFS take it again. Every place is walked both ways on the client floor from the White Oasis
Post waypoint, 30 m or more outside every hostile pool's circle that is not the mission's own; corners in
AshenWhiteOasisTests.Places.

- [ ] 1295 Analyzing the Harvesters (Ligo Ashen Desert): Daris Linier offers it (no requirement), level 41. The Harvester
  Diagnostic Tool (97477) at the accept. Use the four Harvester Control Panels (your own monitors) at Maji Field's
  harvesters, any order: 1 (-305.7, 284.0, 198.1), 2 (-292, 290.71, 305), 3 (-375.5, 297.29, 394.1), 4 (-367, 298.49,
  514). Walk to 2 via (-305.7, 198.1); to 3 and 4 west round the field via (-439.1, 204.8), (-430, 300) (4 also (-425,
  470)). Turn in at Meeks: 100000 XP, 6450 credits, one of Flame Thrower 48752, Incendiary Net Gun 12125, Cryogenic Leech
  Gun 47772, Pulse Rifle 11957; Meeks takes the tool. Do not walk straight from panel 3 (or 2) to panel 4: the line passes
  through the Xanx pool 540076 (-363.9, 447.9; to-fix row 21, fix F: they notice a player within 33 m of its point and
  follow a pull up to 60 m from home, webbing from 40 m); go by (-425, 470).
- [ ] 1298 Murky Waters (Ligo Ashen Desert): Meeks offers it after 1295, level 41. Talk to Hale: choice 1 ("I can't do
  that", the repair) sets flag 258 and opens 3; choice 2 ("I agree", the poison) sets flag 257 and opens 2. Hale answers in
  that side's text and pays: 50000 XP, 4300 credits, one of Basic Med Pack x6 45063, EMP Bomb 111233, Frag Grenade x3
  45447, Cryogenic Grenade x3 111049. The choice is for good (set 12's 1299 needs 258, 1300 needs 257). Check: the client
  shows the choice dialogue and the right answer text for each flag.
- [ ] 1303 Prying Eyes (Ligo Ashen Desert): Daris Linier offers it (no requirement), level 41. Kill Overseer Ulovik
  (upstream boss 520006, level 44, 4900 HP) at (-235, 291.2, 335) between the first two harvesters; straight walk from the
  waypoint. Turn in at Daris: 95000 XP, 6300 credits, one of EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3, Basic
  Med Pack x6. Risk: a level 44 boss against a level 41 player (as 1244's Exing); the nearest other pool (Fithik 540075) is
  46 m off.
- [ ] 1356 Gas Shortages (Ligo Ashen Desert): Fontecilla offers it (no requirement), level 41. Three Shaped Charges
  (111185) at the accept. On the Sheitan Embankment, three Raw Fuel Pumps (your own, class 21007, fix F; set 11 had 10190) at (-76, 282.67, 125),
  (-76, 282.75, 150), (-95, 282.8, 145), each with a Detonation Device 8.1 m off it: (-76, 133.1), (-67.9, 150), (-95,
  136.9). Use a device: it takes one charge and blows its pump. Walk east via (-260, 200), (-160, 200), (-100, 180). Turn in
  at Fontecilla: 95000 XP, 6300 credits, the same four stacks as 1303. Risk: the pumps stand inside the Embankment's Thrax
  garrison (pool 531032, radius 30, 2-3 Soldiers and Ravagers): a fight there is the mission; its dropship lands on the
  pads 6.1 m and more from the pump models and 5.6 m from the devices (the pads' models as the client places them).
  Retest (to-fix row 23, fix F): after the use the pump should show the explosion and then its wreck (set 11's class 10190
  kept its intact model).
- [ ] White Oasis Post control point 17 (real client): Daris Linier (set 1's place, -507.4, 162.4) stands 4.2 m from the
  point, about 2.3 m inside the console model's box (not moved). Click the console from the east side and look for
  Daris clipping into the model or taking the click.

Set 12, Murky Waters' two sides (migration `20271110001200_Ashen_murky_waters`). New world rows: two pools of set 10's
Lasher (790362, three each) on the ridge west of White Oasis Post, 790440 (-648, 336.06, 314) and 790441 (-655, 345.48,
360) (the client's 1300 log: "including the ridge west of White Oasis"). No NPC is new or moved. The side is for good: 1299
needs Murky Waters done with choice 1 (flag 258), 1300 and 1302 with choice 2 (flag 257); a player gets one side only.
Every place is walked both ways on the client floor, 30 m or more outside every hostile pool's circle that is not the
mission's own; corners in AshenMurkyWatersTests.Places.

- [ ] 1299 Water Purification (Ligo Ashen Desert): Meeks offers it after 1298 done by choice 1 (the repair), level 42. 12
  Granitour Water Glands (97479) off set 10's Coheri Pools Granitour (pools 790374 (-185, 520), 790375 (-215, 525)); Wendy
  drops none. Walk from the White Oasis Post waypoint round the west of Maji Field: (-439.1, 204.8), (-430, 300), (-425,
  470), the ridge (-300, 620), and to the first pool by (-250, 540) (the straight way passes Wendy and the Xanx / Tree Mite
  camps). Turn in at Meeks: 95000 XP, 6300 credits, one of EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3, Basic Med
  Pack x6.
- [ ] 1300 Neurotoxins (Ligo Ashen Desert): Hale offers it after 1298 done by choice 2 (the poison), level 42. 12 Lasher
  Poison Glands (97480) off any of set 10's Lashers: the ridge pools west of the post (straight walk from the waypoint,
  about 350 m: the navmesh goes round to the north) or set 10's two pools west of Ashoka; Molly drops none. Turn in at Hale:
  100000 XP, 6450 credits, one of Mech V04 Legs 34374, Graviton V02 Vest 24086, Bio V04 Vest 46037, Stealth V04 Vest 40631.
  Look: the ridge Lashers stand on a slope (the player comes from below: check that the abilities reach them).
  Immunities (row 20, fix F): the Lashers (class 7477) take nothing from Virulent and Laser.
- [ ] 1302 Poisoning the Wellwater (Ligo Ashen Desert): Hale offers it after 1300 (flag 257), level 42. Four Water
  Filtration Systems (97495) at the accept. Use the four Harvester Control Panels (your own, on 1295's four places; 1295's
  own panels are gone after its turn-in): each takes one system. Same walks as 1295. Turn in at Hale: 45000 XP, 4000
  credits, one of the Resist Fire / Ice / Virulent / Electric [4] armour modules. Do not GM-reset 1295 (`.resetmission`)
  while 1302 is open: both missions' own panels would then stand at each place (two own panels per place).
- [ ] 1300 ridge pool 790440 (-648, 336.06, 314): watch point. A line-of-sight sample from the walk up (20, 25 and 30 m
  off, chest height) finds it hidden (the slope's surfaces 3-9 m in front of the player); 790441 is seen. Check in the
  real client that the Lashers there can be targeted from below, or from where a player stands.
- Set 10's walks (tests only, no place moved): with the whole walk now kept 30 m off other camps, the north-east Maji
  Field Beam Manta, Trudy, both Coheri Pools Granitour and Wendy go round the west of Maji Field first ((-439.1, 204.8),
  (-430, 300), (-425, 470), then the ridge (-300, 620)); the first Granitour pool by (-250, 540) both ways (off Trudy);
  the Indra Pass Beam Manta east by the south of Maji Field ((-260, 200), (-160, 200)) before set 10's Indra Pass corners
  (the straight way passed 5.3 m from Overseer Ulovik).

Set 13, Indra Pass (migration `20271110001300_Ashen_indra_pass`; places moved by fix G, `20271110001320_Ashen_fix_g`).
New NPCs: Surveyor Malya Odesha 790480 (115, 304.69, 450) and Researcher Endala Berish 790481 (112, 304.54, 458), 40 m
west of the valley's south entry, where set 10's way from the White Oasis Post waypoint comes in (copies of Raji Lasat's
Brann row, level 43). No hostile creature is new or moved by set 13. Next free id 790482. God of Storms and The Bonds of Blood are
exclusive: a player gets one side. Fix G (to-fix row 24): the real client could not climb set 13's way from the valley to
the Indra Pass waypoint (52-59 degree terrain; the client climbs 55 at most), and no way in to the waypoint's floor was
found outside hostile reach (N23), so the NPCs and the four resistors that stood there are in the valley now. Every walk
below is the client's floor, checked both ways with the client's slope limit, 30 m or more outside every hostile pool's
circle (AshenIndraPassTests.Walks): the NPCs from the valley's south entry, Veyon and the resistors from Odesha (up east
to the valley's south rim by (139, 450), (145, 456), (170, 479), then down).

- [ ] 1280 Missing Scientists (Ligo Ashen Desert): Odesha offers it, level 41. Search Researcher Veyon's body ("Corpse of
  Researcher Veyon", your own) in the valley at (178, 315.73, 536): it gives the Indra Lightning Resistor Codes (97476).
  A full bag leaves the codes on the body (free a slot and search again). Turn in at Odesha: 95000 XP, 6300 credits, one
  of Bio Vest 19286, Stealth Gloves 38952, Graviton Gloves 23698, Hazmat Helmet 20653; she takes the codes.
- [ ] 1281 God of Storms (Ligo Ashen Desert): Odesha offers it after 1280, level 42 (not while 1282 is held or done). Use
  the ten Indra Lightning Resistors (your own, any order): 1 (158, 541), 2 (60, 551), 3 (201, 525), 4 (174, 462), 5 (130,
  500), 6 (176, 496), 7 (66, 599), 8 (96, 462), 9 (124, 532), 10 (96, 554) (1, 2, 3, 7 moved by fix G); each turns on and
  completes its objective.
  Turn in at Odesha: 190000 XP, 8400 credits, one of four weapons. Look: the resistor model (class 10196) on the ground
  and its on state; the tenth resistor fails an open 1282.
- [ ] 1282 The Bonds of Blood (Ligo Ashen Desert): Endala offers it after 1280, level 42 (not while 1281 is held or done).
  Find Maiek's body (a Brann corpse, your own) in the Bane Conscription Base by the power generators at (344, 288.75, -72),
  walked from the Ashoka Settlement waypoint: off the pad to the west, north up the road, east of the hump at (394, -186)
  (fix G: the real client stopped there) by (402, -200), (402, -187), (396, -177), then (352, -137), (352, -84). The search opens "Talk to Endala Berish"; talk to her: 200000 XP, 8600
  credits, one of four weapons. Finding Maiek fails an open 1281.
- [ ] Fix H lift pads (real client, to-fix row 33): walk from the valley's south entry (152.8, 312.8, 459.3) straight to the
  pad 913 (121, 305.97, 443) by Odesha; stand on it: after 3 s you are on the island pad 914 (258, 315.49, 325), not sent
  straight back; step off and on again: back down to 913. From 914 walk to the Indra Pass waypoint (262.2, 315.1, 344.4)
  (19 m north): the waypoint is gained (then it is a way back too). Look: is the pad drawn (ArchHumTeleporterV02)?
- [ ] Fix G walks (real client): from the valley's south entry west down to Odesha and Endala by (126, 451); from Odesha
  to the resistors up east to the rim by (139, 450), (145, 456), (170, 479), then down by (171, 494); Maiek's new way east
  of the hump at (394, -186). The Indra Pass waypoint itself is not reached on foot (N23: no way in found).
- [ ] The Atta pools of the Mauna Barrens (790046-790051) and 1287's 790378 put down one creature each (fix G): 1242's ten
  Soldiers and ten Workers and 1243's samples come on respawns (120 s).


All ESTIMATE (set 13):
- Odesha and Endala 40 m west of the valley's south entry (fix G; 85 m or more from set 10's Indra Pass Beam Manta pool), not at TaRapedia's /loc (312, 324, 293) in the cave by the
  Indra Caverns, 190 m south-east: the Indra Pass waypoint's floor and the cave mouth have no way in a player can climb
  (N23, N26). Resistors 1, 2, 3, 7 in the valley (fix G), 32 m or more from the others. Both copy Raji Lasat's row and look (no female
  Brann NPC look on the World DB); level 43 (TaRapedia's Endala); Odesha has a server-only package (790480).
- Levels 41 (1280), 42 (1281, 1282). Rewards: TaRapedia's XP and credits; the items are the lowest Uncommon template of
  each named kind in the mission's band.
- Veyon's body (the client's Brann corpse 7928 with the client's name 67) and the ten resistors (the client's class 10196
  named 68) in the valley: places of no source; Maiek's body (7928, the client's own name) 36 m from the third generator
  and 64 m from Overseer Naxveik (TaRapedia "very near Naxveik"; kept 34 m and more outside every hostile pool's circle).
- The fork: each side's end (the tenth resistor, Maiek found) fails the other if it is held (the client logs); the offers
  are exclusive (TaRapedia, God of Storms). Markers r8.

Set 14, Ashoka (migration `20271110001400_Ashen_ashoka`). New NPCs: Lieutenant Daniel Chase 790520 (-120, 296.32, -805) in
Shadow's Edge Post, 15 m from the waypoint (a copy of Captain Flynn Harris' row, level 41); Researcher Yorish 790521 and
Researcher Jackson 790522 (1736's escort, scene actors, no pool; copies of Raji Lasat's and Professor Tiernan's rows, level 42,
4000 health, run 7); Overseer Jirex 790523 (hostile, the Overseers' class 10504, level 44, 4900 hit points, armour 420) alone in
pool 790524 at (426.8, 288.85, -261), 30 m west of the destroyed Bane pillbox (454, -273) northeast of the Ashoka waypoint,
respawn 300 s. Next free id 790525. Echoes in the Sand is kept on the Crucible's revision (crucible_1_16_5, the 1218 chain it
ends; the server keeps a required chain on one revision); its category and map are Ashen Desert's. Every walk below is the
client's floor, checked both ways with the client's slope limit, 30 m or more outside every hostile pool's circle
(AshenAshokaTests.Walks).

- [ ] 1736 Echoes in the Sand (Ligo Ashen Desert): Captain Botha (Ashoka) offers it after It Came From Erdas (1218, the
  Crucible), level 42. At the accept Researchers Yorish and Jackson come by Botha and follow you. Take them to the Eloh
  obelisk at Daedalus' Landing (188, 298.03, -200), off the pad west: (360, -426), (358, -424), (328, -391), (298, -353),
  (258, -299), (220, -240), then the obelisk (334 m). Within 20 m of it the first ambush comes: two Ashen Thrax
  Soldiers (531008) from (230, -255) and (232, -258). Both dead, the obelisk comes (the client's NPCElohObeliskV01; not
  before: never four Thrax at once). Talk to the obelisk (its vision,
  package 2066): "Escort Researchers back to Ashoka" opens with a marker at Botha, and the second ambush comes, two more from
  (235, -250) and (237, -253). Walk back the same way and talk to Botha: the researchers go in. Turn in at Botha: 190000 XP,
  8400 credits, one of Laser Pistol 14492, Laser Rifle 11947, Photonic Shotgun 11983, Laser Chaingun 45762. Either
  researcher's death fails the mission (the other, the obelisk and the Bane go); an abandon takes them away. Check: both keep
  up with a running player on the whole way, both ways; the obelisk only after both of the first pair are dead. A logout or
  a map change: the researchers wait (the run's own; they follow again on return); after a server restart they are placed
  again by Botha and run to the player.
- [ ] 1358 Supply Logistics (Ligo Ashen Desert): Lieutenant Chase (Shadow's Edge Post) offers it, level 41; no requirement.
  The accept gives five Tactical Military Supplies (97522-97526; a bag without five free mission-item slots refuses it).
  Deliver each, any order, by a talk: Captain Botha (378, -458.5) and Captain Harris (405, -459.5) at Ashoka, Captain Emmons
  (-62, -414) on Stonewall Ridge, Captain Yamamoto at Boneyard Outpost, Daris Linier (-507, 162) at White Oasis Post (gone
  while the Bane hold control point 17: then 5 waits). Turn in at Chase: 95000 XP, 6300 credits, one of Mech Vest 34503,
  Graviton Legs 13043, Hazmat Boots 20355, Reflective Boots 37836.
- [ ] 1286 Precious Tools (Ligo Ashen Desert): Mechanic Raji Lasat (Ashoka) offers it, level 42; no requirement. Kill
  Overseer Jirex at (426.8, -261), walked from the Ashoka waypoint: off the pad to the west, north up the road ((388, -431),
  (402, -401), (405, -352), (395, -290)), then east. He drops Raji's Lucky Wrench (97482, 100%, your own, one). Turn in at
  Raji: 190000 XP, 8400 credits, one of Bio Gloves 18899, Graviton Helmet 23828, Mech Gloves 32826, Stealth Gloves 38952; he
  takes the wrench. Check: Jirex is a boss name in the kill log (BossTitles: 9793 "Overseer Jirex").

Fights (rule 3; to-fix rows 20 and 26): Jirex's class 10504 and the ambushers' class 20757 (531008) carry none of the client's
IMMUNE_* damage flags (creature_class_flag 125-135), nor the researchers' and Chase's classes. Jirex is a single level 44
boss (as Overseer Ulovik, beaten solo in 1303); the ambushes are two level 39 Thrax Soldiers each.

All ESTIMATE (set 14):
- Chase's place (TaRapedia: Shadow's Edge Post, no /loc) and look (Harris' AFS row); his package is server-only (790520).
- The researchers' looks: Yorish a Brann (Raji's row: Kapa Grupa Lohen's man, "Syndicate Researcher"), Jackson an AFS
  researcher (Tiernan's row); their stats and speeds the Marshes escorts'.
- The obelisk at (188, -200), 50 m from the escape pod (134, -164): the Tree Mite pool 540072 stands on the pod (upstream,
  not moved); the client has no obelisk at the pod (map statics). The ambush spots and sizes (two and two): no source.
- Jirex at (426.8, -261), not at TaRapedia's "near a pillbox" itself: the pillbox's ground is within 35 m of the Boneyard
  Thrax pool 531039 and the Hominis Machina pool 790245; 30 m west of it he stands 60 m or more from every other hostile pool
  (531039 60.2, 790245 60.5), 32 m from Maiek's road, 76 m from Ellatha's boss-map spot (474, -202). His level, stats and respawn are Overseer Mitecht's and fix F's.
- Levels 42 (1736, 1286), 41 (1358) (triage D5, from TaRapedia's XP). Rewards: TaRapedia's XP and credits; the items the
  lowest template of the named kind and version in the mission's band.
- 1736's helpers: 3 and 4 (the researchers) are required and hidden, done with 2; 6 (server-only, hidden, not required) counts
  the first pair's deaths and brings the obelisk; 5 (Ambushes) hidden and not required, done
  at the landing. Markers: 1736 on the obelisk (r20) and Botha, 1358 at each captain (r8), 1286 on Jirex (r15).

Set 15, Indra Caverns and the Brann facility (migration `20271110001500_Ashen_indra_caverns`). Indra Caverns (map 2055) is a
squad instance: the door is map link 58 at (321.9, 319.0, 319.6) on Ashen Desert, east of Surveyor Odesha; it puts you at
(-40.7, 8.9, -421.6). New NPCs, all in the caverns: Major Darkua Diren 790560 (-53, 8.6, -395), 29 m in from the door, by
the medic; Sergeant Jonas Drof 790561 (-5, 0.58, -221) in the facility's Control Room; Scout Hansa Naimi 790562 (9.83,
24.85, -244.76) inside the Enslaver cell by the Security Station (copies of Captain Flynn Harris' row, levels 44, 43, 42);
Thrax Overseer Kuutk 790563 (hostile, class 10504, level 44, 4900 hit points, armour 420) alone in pool 790564 in the
Science Lab (59, -7.42, -403), respawn 300 s; the Indra Watch Post: a Thrax Soldier 531008 in pool 790565 (-36, 6.34, -355)
in the low trench on the way in and a Ravager 531009 in pool 790566 on the Watch Post's north-west terrace (-50, 14.56,
-296) (fix J, to-fix row 36; set 15 had it at (44, 16.35, -335) by the facility's entrance tunnel), one each
(pool respawn 120 s, never reached: in the squad's copy a cleared pool comes back 45 minutes after its last death,
SquadInstanceConfig.RespawnMinutes; Kuutk, a public-spawn actor of 1395's scene, comes back on his own 300 s). Next free id 790567. Every walk
below is the client's floor, checked both ways with the client's slope limit (AshenIndraFacilityTests); the server's
navmesh of the caverns is islands (an upstream navmesh defect), so the walk tests use the client floor only.

- [ ] 1559 Indra Caverns (Ligo Ashen Desert): Surveyor Odesha (Indra Pass) offers it after Missing Scientists (1280), level
  42. The door's island has no walk-in (N23, N26; to-fix row 33): fix H's lift pad (local teleporter 913, marker 1559.1 r4)
  stands 9 m south-east of Odesha at (121, 305.97, 443); stand on it 3 s and it sets you down on the island pad 914 (258,
  315.49, 325). Marker before the door (305, 319.9, 326). Walk from the island pad east: (278, 337), (286, 331), (293, 324),
  (306, 322), into the door's circle at (320, 320.5). Arriving in the caverns completes 1; walk within 10 m of Diren (marker on him): 2, the
  reward. Turn in at Diren: 60000 XP, 6750 credits, one of Class IX Concussion Grenade 111020 x3, Basic Med Pack 45066 x2,
  Fragmentation Grenade 45448 x3, EMP Bomb 111234 x1.
- [ ] 1360 Threshold (Ligo Ashen Desert): Diren offers it after 1559, level 43. The accept gives the Supply Package
  (97516). Walk north past the Watch Post: kill its two Bane (the Thrax Soldier in the low trench, the Ravager on the
  north-west terrace (-50, 14.56, -296), fix J: from the Soldier's trench (-36, -355) north by (-31, -317), (-31, -305),
  (-32, -303.6), (-35, -301.6), (-37, -301.6) and west; marker r45 over both at (-43, -325.5)) for 3. Search Guard Rolim Bendal's body in the pillbox at (9.5, 18.52, -336) (fix
  I): from the Watch Post (-5, -351) east by (15, -350), (23, -345), (24, -345) along the pillbox's south side, then north
  to its open east side (28, -337) and west in. Use the facility door, the Security Check Point entrance's door (fix I:
  class 23160 at (64, 16, -328), its leaf across the 3 m tunnel at x 60-62): from (24, -345) by (32, -341), (37, -341),
  (50, -330), (52, -329), then straight east along the tunnel's middle (z -328; its mouth at x 52, south of the dark panel
  of the entrance's face at x 56): the card opens it and is taken. Walk these by hand: a walk on the server's navmesh stops
  at the island ends (N29). Fix U: from Diren the walk to the Watch Post goes by (-41, -362), (-40, -356) (west of the rock
  lip at (-39.4, -358)), (-36, -355), (-31, -353), (-25, -354), (-25, -353) to (-5, -351) (not by (-26, -353): a 1.1 m step
  there); the hall's stair down to the Control Room is walked at z -203 ((21, -203), (8, -203), (3, -203)), not at z -204,
  where the floor has gaps. Both legs pass with their start moved 0.13-0.4 m. Go through to the Control Room and talk to Drof (marker on him; open once 1 and 3 are
  done): he takes the package. Turn in at Drof: 120000 XP, 9000 credits, one of Graviton Legs 23957, Hazmat Helmet 20653,
  Reflective Gloves 35740, MotorAssist Helmet 28960. Check: the door stays shut without the card (the server refuses the
  use); in a squad each member on 1360 gets his own card at the search and his own door opens (his card taken) at the
  user's use; a mate away then (or whose card holder abandoned) opens his own door later with his own card.
- [ ] 1395 Vindication (Ligo Ashen Desert): Drof offers it after 1360, level 43. The accept gives the Key Pass (97548): it
  opens three room doors (kept). Search Scout Aksrah's body in dorm 152 (153, 8.5, -296; door at (150, -296)) for the Radio
  (97551) and 2. Kill Thrax Overseer Kuutk in the Science Lab (59, -403; door at (36, -362)) for 4: he drops the Thrax Hand
  (110824, 100 %, your own). Use Naimi's cell (10.4, 24.6, -244.2; marker before it) with the hand (taken) for 6: the cell
  opens. Talk to Naimi (3): 5 opens. Search the Supply Crate in dorm 32 (24, 8.5, -284; door at (34, -280)) for the Power
  Supply (97519) and 5. Turn in at Drof: 110000 XP, 8800 credits, one of Vextronics Cryogenic Blade 12101, Virulent
  Disperser 12077, Endothermic Polarity Gun 12137, Cryogenic Leech Gun 47772. Check: each room door refuses a player without
  the pass; the cell refuses one without the hand; the radio, pass and power supply go at the turn-in. In a squad each
  member on 1395 loots his own hand; the user's use of his cell takes each present member's hand and opens each one's
  cell; a mate away then opens his own cell later with his own hand.

Fights (rule 3; to-fix rows 20 and 26): Kuutk's class 10504 and the Watch Post's classes 20757 (Thrax Soldier) and 9354
(Ravager) carry none of the client's IMMUNE_* damage flags (creature_class_flag 125-135), nor the AFS classes of Diren,
Drof and Naimi. Kuutk is a single level 44 boss (as Jirex and Ulovik, beaten solo); the Watch Post is two level 39 Bane, one
per pool, 82 m apart (one at a time).

Tester notes (set 15):
- Take a squad for the squad checks: a mate on the mission, a mate without it. The doors and the cell are personal: each
  member on the mission has his own, and his own card (1360, given at the search) or hand (1395, looted from Kuutk); a
  use completes the objective for each member in the copy then, each one's key taken and each one's door or cell open; a
  mate away at the use opens his own later with his own key. A mate without the mission is not shown the doors at all.
- Kill the Watch Post before or after the accept: kills ahead count (see Deviations).
- Reload (log out and in) with the card or hand held: the key is kept and still opens the door.

All ESTIMATE (set 15):
- The NPCs' looks (Harris' AFS row), levels 44/43/42 and places: Diren 29 m in from the door (TaRapedia: "right inside"),
  Drof in the Control Room's middle, Naimi in cell B (cell A's floor is a navmesh island). Kuutk's row Mitecht's (as Jirex),
  in the Science Lab south of its machine bank. The Watch Post: two pools of one (to-fix row 26), Thrax Soldier and Ravager,
  pool respawn 120 s (the upstream Indra Caverns pools'; in the squad's copy 45 minutes, SquadInstanceConfig).
- 1559 after 1280 (Odesha's chain; TaRapedia names no requirement); 1 the whole map, 2 within 10 m of Diren (no talk in the
  client). The items the lowest templates of TaRapedia's four kinds, stack sizes as 1273's.
- 1360: Bendal's body in the pillbox (fix I, to-fix row 34; set 15 had it 9 m south-west of it, behind its wall); the
  facility door's class and place (fix I: the entrance's own door UsableDoorBrannCorridorEntranceDoorV01 23160 at the
  entrance's "door Proxy", set 15 had the 4 m corridor door 10749 in the jamb at (82, -328)) and its yaw; the door states
  31/91; that the client lets a player through the terrain face in the entrance tunnel (x 58-59, as in the Ashoka corridor).
- 1395: the three key-pass doors (dorm 152, the Science Lab, dorm 32) in the jambs of the rooms the mission goes into; the
  cell door's states 31/91 (10749's kind, no source for 24910); the crate's place in dorm 32.
- Levels 42 (1559), 43 (1360, 1395) (triage D5). Rewards: TaRapedia's XP, credits and item kinds; the items the lowest
  template of the kind in the band. Markers: 1559 before the door (r6) and on Diren (r8); 1360 on Bendal, Drof (r8) and the
  Watch Post (r45); 1395 on Aksrah, Naimi (r8), Kuutk (r10), before the cell (r5) and the crate (r8).

Set 16, Indra Caverns, the rear caves and the Gotha Processing Plant (migration `20271110001600_Ashen_gotha_plant`). All in
Indra Caverns (map 2055, the squad instance of set 15). Getting there: the caverns' door (map link 58) is not reached on foot
on the client floor (to-fix row 33); fix job H's lift pads (migration `20271110001510`) take the player to it: the valley pad 913 (121, 305.97, 443) by Odesha sets you down on the island pad 914 (258, 315.49, 325), then walk
to the door as in 1559. New NPCs: Sergeant Jonas Drof's second row 790600 (-309, 129.55, -53) and Scout Hansa Naimi's second row 790601
(-306, 129.55, -57), just inside the plant's entrance hall at the top of the mining area's ridge (copies of set 15's rows;
they stand from the start, and set 15's stay in the facility); Overlord Kretas 790602 (hostile, class 10504, level 44, 4900
hit points, armour 420; the Overseers' row) alone in pool 790603 in Bane Central Administration's north hall (-364, 168.71,
-406), respawn 300 s. Next free id 790604. Every walk below is the client's floor, checked both ways with the client's slope
limit (AshenGothaPlantTests); the caverns' navmesh is islands (upstream), so the walk tests use the client floor only, and no
Ashen-side walk is part of this set.

- [ ] 1447 Digging for the Truth (Ligo Ashen Desert): set 15's Drof (Control Room) offers it after 1395, level 43. The
  accept gives a Power Supply (97519; 1395's own was taken at its turn-in). Use the Power Supply Box (name 144, marker 218)
  just behind Drof at (-9, 0.5, -221) with it (taken): 12, the power on: the rear exit door (140, 16.5, -226) opens and the
  two teleporters in the Control Room, (-12, 0.5, -215) and (-1, 0.5, -227), come on. Use one: it takes you just outside
  the rear exit (140, 16.02, -205) (a held passage of 3 s); walk on toward the caves (into the circle at (131, -191), r6)
  for 13; 12 and 13 complete 5 and open 2. Or walk through the open door: the Control Room's east door, the east hall, the
  ramp south-east and north up to the rear exit. Walk north-west through the rear caves to the Bane's point of entry (4,
  46.18, -94; r10, marker 219): 2, 3 opens. Follow the Bane tunnel west and north (it bends at (-252, -44) and turns west at
  (-302, 23)) to its exit (-325, 64.12, 24; r10, marker 220): 3. On through the north cavern (-350, 58) up into the mining
  area: six Articulated Drills (500 hit points each, marker 221 on each, 134 over the area): (-327, 90.31, 239), (-326,
  90.43, 259), (-338, 90.43, 270), (-337, 90.43, 264), (-339, 90.43, 276), (-341, 90.56, 282) (drills 4-6, fix J, to-fix
  row 38: set 16 had them in the lava river at (-311, 245), (-312, 227), (-322, 211); now 11 m+ from the lava, in a row
  with drills 2 and 3, each drill's body (17 x 5 m) 0.5 m+ from the others; marker 134 at (-334, 90.43, 255)): shoot each to pieces (6-11;
  1 counts them). Turned in by radio: 110000 XP, 8800 credits, one of Teleract Torqueshell Rifle 12111, Vitalius Sonic
  Disperser 12071, Vextronics Electric Rifle 11953, Virulent Injector Gun 12155. Check: the box refuses a player without
  the supply; the door stays shut and the teleporters off until the box is used; drills shot before 2 and 3 still count.
- [ ] 1532 Meet Sergeant Drof (Ligo Ashen Desert): Headquarters offers it by radio at 1447's turn-in, and again on arriving
  in Indra Caverns, after 1447, level 43. Walk from the mining area west and north up the ridge to the plant's entrance
  (-309, -53): talk to Drof there (marker 222 on him). Turn in at Drof: 60000 XP, 6750 credits, one of Class IX Incendiary
  Grenade 111040 x3, Advanced Med Pack 45068 x2, EMP Grenade 111030 x3, Res Sickness Kit 47601 x1.
- [ ] 1451 Subversion (Ligo Ashen Desert): Drof (plant entrance) offers it after 1532, level 44. Use the two Gas Flow
  Controls (name 152, marker 216): Alpha (-143, 137.59, -167) east and Beta (-415, 137.59, -237) west (3, 4; 1 counts
  them, marker 134 r30 at (-279, -264)); and the four Life Support Systems (name 153, marker 217) by the sleep pods: the
  south hall (-172, 137.11, -416) upper deck and (-177, 131.21, -416) lower deck, the east hall (-81, 137.11, -363) upper and
  (-82, 130.23, -369) lower (5-8; 2 counts them, marker r30 at (-156, -344)). Each switch sets on use and is used no more.
  The south hall's upper deck (life support 1, 5.5 m over life support 2's) is reached only by the ramp along z -416: from
  its foot at about (-154.7, 129.8, -416.4), at the hall's east end, west up to the deck at x -166 (fix J, to-fix row 42:
  from beside the ramp, e.g. (-157, -414.5), the client stays on the lower deck and refuses the use, pm 274).
  Turn in at Drof (the client's summary 12890 "Speak to Sergeant Drof for your reward", completion text 12892 Drof's,
  reminder 12893; TaRapedia's "Radio HQ" turn-in not followed): 110000 XP, 9000 credits, one of Vextronics Pulse Rifle 11957, AccuMax Incendiary Staff 49496, Phoenix
  Direct Repair Tool 18274, Titan Tissue Extractor 12053.
- [ ] 1452 Desistance (Ligo Ashen Desert): Naimi (plant entrance) offers it after 1451, level 44. The accept gives the Bomb
  (111011). 1: the Security Door Control Console (name 151, marker 213) at the bay's north-east door (-300, 128.01, -224); 2
  opens: either Juggernaut console in Juggernaut Operations (marker 212 at (-396, -250), r12): Programming (-395, 128.01,
  -246, name 149) or Systems (-398, 128.01, -253, name 150) (one used closes the other); 4 opens: walk up to the landing
  pad's deck (the circle at (-395, 176.2, -295), r14, marker 210; fix K, row 44: on the deck go north round the landing
  pad's dish, not south: the deck's south half is pool 531092's ground, see the walk points below); 3 opens: use the Explosive Charge (-392.3, 175.81, -301; fix J,
  row 43: set 16's stood inside the generator's body) on the Outpost Generator's east face (-401, -301; marker 214 at
  (-395, -301)), from the floor east of the generator (the pad walk ends at (-390.8, 175.81, -297)) (it has no lock; 3 takes the bomb, then the generator blows; without your bomb your own 3 stays open and your charge
  stays usable: alone nothing happens, in a squad the use still completes the 3 of each mate who has his bomb); 5 and 6 open
  (fix J): walk into Central Administration's north hall ((-364, 168.71, -398), r16, marker 211; Kretas stands 8 m inside
  it); 6: kill Overlord Kretas (marker 209; the kill counts also for a member who shoots from outside the hall); he drops his hand
  (110986, 100 %, your own); 7: use the Teleportation Console (-392, 169.01, -432; name 154, marker 215) with the hand
  (taken): your teleporter beside it (-389, 168.53, -431) comes on; use it: it takes you to Indra Watch Post (-51, 7.6,
  -407) beside Diren; 8: talk to Diren (marker 260). Turn in at Diren: 120000 XP, 9000 credits, one of Bio-Engineered
  Boots, Mechanized Boots, Stealth Gloves, Graviton Gloves (the V02 armour of levels 40-44).

Walks (set 16; client floor both ways): 1447: Drof -> box and each teleporter; the Control Room -> east hall -> rear ramp ->
outside the rear exit (through the door's leaf); -> rear caves -> cave entrance; -> the tunnel (split at its bend (-252,
-44) and last turn (-302, 23)) -> its exit; -> the north cavern (-350, 58) -> drill 1 -> 2 -> 3 and drill 1 -> 4, 5, 6;
the north cavern -> up the ridge -> Drof at the plant -> Naimi (passes 531089, 35 m below). 1451 / 1452: 41 legs from Drof
at the plant through the plant to each valve, life support system, console, the pad, the charge, Central Administration,
Kretas, the console and the teleporter (AshenGothaPlantTests.Legs). Kretas' teleporter's arrival -> Diren. Since fix J
(row 42) each leg must start and end on its corners' own deck (0.5 m), so a leg onto another deck climbs from the ramp's foot.
The pad route (fix J, row 44, the tester's way) climbs to the landing at (-232, -319) at the ramp's east end, not over
its side (the check's rampFoot rule refuses a step of more than 0.6 m off the terrain onto a mesh). Fix K (row 44, deaths 7
and 8): fix J's loop south round the trench at (-340, -304) ran 2-7 m outside upstream pool 531092's circle, through its
spawn ground (a spawn draw lands up to 36.7 m from its middle, Detour's whole polygons); the way now goes north round the
landing pad's dish, 25 m and more outside 531092's circle (51.2 m from its middle; 27.67 m from its nearest seeded spawn draw at
(-329, -287), over its 18 m aggro, 3D, Creature.AggroRange; fix N). Walk points (x, y, z) from the ramp's landing: (-231.9, 173.6, -319.2),
(-241.9, 177.5, -307.2), (-247.1, 177.61, -301.2), (-250.2, 177.61, -298.9), (-257.3, 177.61, -291.9), (-260, 177.61, -291),
(-264.8, 177.61, -289.9), (-289.1, 177.61, -286.7), (-329, 175.43, -287), (-341, 175.93, -265), (-351, 176.37, -260),
(-367, 176.52, -264), (-380.5, 176.37, -273), (-381.5, 176.61, -274.5), (-384, 176.37, -274), (-387.5, 176.06, -274),
(-391, 175.81, -276), (-391, 175.81, -296), the pad (-390.8, 175.81, -297), beside the charge (-390.8, 175.81, -301)
(AshenGothaPlantTests legs 23, 24, 44; AshenFixKTests: client floor both ways with shifted starts at either end, out of the
generator's body, 1.28 m from the charge). Fix N (row 50): fix K's way west of (-351, -260) ran on the landing pad pit's
trench rim 0.25-0.5 m from the pit (5 m down); the real client fell in and could not get out (the pit's walls are the
trench's; its north-east slope, 44-51 degrees on the heightmap, the client does not climb; its south side is 531092's
ground). The way now keeps 2.25 m and more from the pit's edge (the check's edge margin, 2 m, AshenFixNTests) round its
north-west corner and walks south on the terrain west of the rim. If you fall into the pit: /stuck. Hostile pools
each 1451/1452 walk passes (the plant's own; the 30 m walk rule is not kept in it): valve 1 (Alpha): 531086, 531088, 531089;
valve 2 (Beta): 531086, 531089, 531091, 531093; life support 1: 531086, 531087, 531089, 531090, 531093; life support 2:
531086, 531087, 531089, 531090, 531093; life support 3: 531086, 531087, 531089, 531090, 531093; life support 4: 531086,
531087, 531089, 531090, 531093; the door console: 531086, 531089, 531093; the programming console: 531085, 531086, 531089,
531091, 531093; the systems console: 531085, 531086, 531089, 531091, 531093; the landing pad: 531085, 531086, 531089,
531090, 531091, 531092, 531093; the explosive charge: 531085, 531086, 531089, 531090, 531091, 531092, 531093; Central
Administration: 531085, 531086, 531089, 531090, 531092, 531093, 790603; Kretas: 531085, 531086, 531089, 531090, 531092,
531093, 790603; the teleportation console: 531085, 531086, 531089, 531090, 531092, 531093, 531095, 790603; Kretas'
teleporter: 531085, 531086, 531089, 531090, 531092, 531093, 531095, 790603.

Fights (rule 3): Kretas' class 10504 has none of the client's IMMUNE_* damage flags (creature_class_flag 125-135); he is a
single level 44 boss as Kuutk (beaten solo). The drills, switches and charge are objects. No new hostile pools besides Kretas';
the walks through the plant meet its own Thrax pools (see the rule breaks).

Rule breaks inside the plant and the mining cave (rule 2; the plant's rooms are small and full of upstream pools):
- 1447: the Bane tunnel runs through pool 531094 and over 531088 and 531089 (its only way to the mining area).
- 1451: the valves stand 20 m (Alpha) and 15 m (Beta) outside the nearest hostile pool's circle; the life support systems
  about 25 m (the barracks halls' own pools); the walks to them pass the plant's pools (listed in the test's walks).
- 1452 (fix K, fix N): the way north round the dish passes pool 531093's spawn ground on the deck's north half (4-5 m from
  its nearest draw at (-351, -260); fix N keeps it there: the deck north of the pit's rim is that ground, a way 8 m from it
  squeezes past the pit's edge at (-347, -268.5)); about 16 % of its draws stand within its 18 m aggro of the way: with its 2-4 Thrax Soldiers
  and Grenadier, one or two may come, ESTIMATE). The deck's only two ways west pass 531092's draws (south) or 531093's
  (north); north is the smaller pool (531092: 5-7 with Linkers, a Technician and a Caretaker; it killed the tester on fix
  J's way). Set 16's legs 01 and 13 pass the bay pool 531086's draws at about 18 m ((-324, 129.6, -110)).
- 1452: the Juggernaut consoles about 22 m; the generator 13.5 m and the charge 18.5 m (the pad deck's pools); the
  console and the teleporter about 23 m; Kretas 38.7 m (the 35 m boss rule kept; not the 60 m between hostile pools).
- Drof and Naimi at the plant: 34 m and more in 3D (pool 531089 35 m below in the mining cave; 26 m on the flat).

Tester notes (set 16):
- Walk on Indra Caverns (2055) in straight client walks between the listed corners (by hand), not by navmesh walks
  (cwalk.py / visual.walk): the 2055 navmesh is islands (upstream N29) and a navmesh walk stops at an island's edge.
- Known risk (audit S4, the 1401/1395 design): in a squad, a mate on 1452 without his hand at the player's console use
  (his take fails) keeps his 7 open and his own console usable, and the shared event waits for him; check that the player's
  own console still works.
- The legs are checked at a 0.5 m body half width (audit S5; 0.33 m before): 38 of the 68 legs were rerouted (their
  stops moved or split; AshenGothaPlantTests.Legs); report any stop on a prop.
- Get into Indra Caverns by fix job H's lift pads (`ashen_fix_h`), not on foot (to-fix row 33).
- Take a squad: the box, door, teleporters, drills, switches, consoles, charge and Kretas' teleporter are personal: each
  member on the mission has his own; a use or a drill destroyed completes the objective for each member in the copy (his
  power supply, bomb or hand taken then). A mate away at the use uses his own later with his own key item; a mate whose
  holder abandons is not blocked (his own items stay).
- With a full bag the accept of 1447 and 1452 is refused (the power supply and the bomb come at the accept).
- The teleporters are held passages: stand on them 3 s.
- 1452: abandoning after Kretas keeps the hand (CharacterOwned): taken again, the hand opens the console, but 6 waits for
  Kretas again.

All ESTIMATE (set 16):
- The places of the box (on the console behind Drof), the two teleporters, the rear exit door, the rear area just outside,
  the cave entrance and the tunnel exit circles, the six drills in the mining area, the valves (one per gas processing
  wing), the four life support systems (beside each sleep pod group), the door console, the two Juggernaut consoles, the
  pad circle, the generator and charge, the Central Administration circle, Kretas, the console and the teleporter: picked
  on the client map's statics and the client floor, no source places them.
- Drof's and Naimi's second rows just inside the plant's entrance hall; they stand from the start (no dropship arrival).
- Levels 43 (1447, 1532) and 44 (1451, 1452). 1451's experience 110000 (TaRapedia gives none). Items: the templates of the
  named kinds chosen by the level band (1447's four, three of 1451's four, 1452's V02 band); 1532's stack sizes TaRapedia's,
  the kit 1; TaRapedia's "Res Trauma Kit" mapped to 47601 "Res Sickness Kit" (no client template of that name).
- Kretas' class 10504, level 44, 4900 hit points, armour 420 (the Overseers' row); the generator's 100 hit points; every
  area and marker radius.
- 1532's radio offer at 1447's turn-in and on map arrival; requires 1447. 1452 requires 1451 (the story's order).
- 1452's order 1, 2, 4, 3, 5, 6, 7, 8; the Juggernauts are statics, no creature.

Set 17, Avernus Outpost, the outpost part (migration `20271110001700_Ashen_avernus_outpost`). All in Avernus Outpost (map 2110,
a squad instance; its door is map link 59 from Ashen Desert, arrival (83.99, 528.5, 107.67)). New NPCs: Captain Sera Karter's
second row 790640 (80.8, 528.5, 99.5) at the entrance; Commander Drazen 790641 (57.5, 477.97, -313) in the cavern south of the
outpost; Captain Petrovich 790642 (fix M: (873, 284.11, -219); set 17's (909, 273.86, -246)) in the AFS Graveyard and his second row 790643 (645.5, 352.67, -246.5) at
the tunnel's top (Food Storage shelf); Petrovich's squad, an Avernus Outpost Officer 790644 and Avernus Outpost Soldier 790645
(1535's scene only, no pool). The Avernus Invaders 790646 (hostile, an Atta Soldier 531015 copy, name 9675, level 41) in three
pools of two or three (respawn: the row's 120 s; in a squad copy every pool comes back 45 min after its last death,
SquadInstances.RespawnMinutes): 790647 (158, 528.25, 17) and 790648 (205, 515.85, 74) in the north ravine, 790649 (-26,
528.87, -176) in the south-west yard. The Atta Teleporter: local teleporter pads 915 (fix M: (48, 477.12, -318); set 17's (64, 480.73, -312)) by Drazen and 916 (fix M: (888,
281.86, -212); set 17's (890, 278.11, -232)) in the graveyard, linked both ways (class 25302); the graveyard has no walk from the outpost, the pad is the way.
Next free id 790650. Every walk below is the client's floor, checked both ways with a 0.5 m half width and the 55 degree climb
rule (AshenAvernusOutpostTests).

- [ ] 1473 Overly Sensitive (Ligo Ashen Desert): Karter (outpost entrance) offers it, level 41. 1: search the courier bag in
  the armour shop (102.9, 514.25, -56.7): the Armor Sample (111191). 2: destroy the prototype ammo in the weapon shop (141.4,
  514.57, -49.1). 3: search the corpse in the tavern (194.7, 519.01, -31): the Commander's Diary (111190); it opens 5. 4: use the
  four servers in the HQ (176.4, 532.66, -126.8 .. -132.8). 6: destroy the data backup black box beside them (175.8, 532.66,
  -135.1). 5: search the corpse by the landing strip (8.9, 531.16, -124.9) for the security card (111189, hidden 7), use the
  Universal Interior Teleporter west of the strip (28, 528.12, -106; locked to the card) while 5 is open: it sets you down in the
  prison (-8, 520.01, -46); destroy the stasis tube (-6.5, 520.01, -50.2). The prison is a closed room in the client: leave it
  by stepping onto its Universal Interior Teleporter (local teleporter 923, (-14, 520.01, -44), fix L): it sets you down by
  the inbound teleporter (31, 528.12, -109). Each marker is unnamed on its target (5: the tube,
  the teleporter and the card's corpse). Turn in at Karter: 90000 XP, 6150 credits, one of Class VIII Cryogenic Grenade 111049
  x3, Standard Med Pack 45064 x3, Incendiary Grenade 111039 x3, Advanced Med Pack 45065 x2.
- [ ] 1533 Petrovich's Last Stand (Ligo Ashen Desert): Drazen (south cavern) offers it, level 41. No client objective: the
  mission can be turned in at once; step on Drazen's pad (915), it sets you down in the graveyard; walk to Petrovich (15 m area
  round him, hidden; fix M: down pad 916's ramp to the south, then 15 m west). Turn in at Petrovich: 90000 XP, 6150 credits, one of Class VIII EMP Grenade 111029 x3, Res Trauma Kit
  47600 x1, Class VIII Concussion Grenade 111019 x3, Class VIII Basic Med Pack 45063 x6.
- [ ] 1535 Petrovich's Run (Ligo Ashen Desert): Petrovich (graveyard) offers it, level 42. His own copy, the officer and two
  soldiers stand by him from the accept (unhurt by anything). 1: the graveyard's four Atta pools (fix O: 790700, 531050, 790701, 531063)
  dead at once (markers on each; the client shows no counter). 2: about 2 s after the accept Petrovich leads up the tunnel (his route, speed 4), the squad follows
  you; walk into the turn (775, 317.37, -112.5; r10). 3: talk to Petrovich there: two Atta Soldiers come down the tunnel at
  you; both dead, two more; 4 completes when they are dead too. 5: Petrovich leads on to the top; walk to his second row at
  the top: the squad goes. Turn in at Petrovich at the top: 90000 XP, 6150 credits, one of Class VIII Frag Grenade 45447 x3,
  EMP Bomb 111233 x1, Cryogenic Grenade 111049 x3, Standard Med Pack 45064 x3.

Walks (set 17; client floor both ways): the entrance -> Karter, the bag, the ammo, the tavern corpse, the four servers and
the black box in the HQ, the strip corpse, the prison teleporter; in the prison (closed in the client, fix L) the arrival -> the
stasis tube and -> its way out 923; the way out's arrival 924 -> the strip road and the plaza;
Drazen (the south road east of the open frame that closes it at z -129.5, the slope beside the fallen slab, the
cave tunnel under the terrain's lip and west of its rubble); Drazen -> his pad; the graveyard pad -> Petrovich; Petrovich -> the
turn; the turn -> the top (AshenAvernusOutpostTests.Legs and Walks). Steps, ramps and the HQ's door ramp are walked along their
length, not climbed from the side (audit S-B): the entrance east of the 1.2 m block at (95-99, 73-74), the weapon shop through
its door's middle, the HQ up its door ramp eastward, the strip corpse's platform by its south ramp. Every leg also passes with
its start moved 0.13, 0.27 and 0.4 m (the samples fall elsewhere; the walk test does this for every leg). The steep diagonal
stretches left in the walks are on terrain or cave ramp meshes, not on a static's stair, and under the client's 55-degree
limit (probe 2026-11-12, the floor's source and slope every 0.5 m): (77, 3) -> (76.65, 3.35) on the trunk, terrain, 14 degrees; (189, -68) -> (192, -65)
to the tavern, terrain, 29 degrees; (34, -290) -> (43, -308) into Drazen's cavern, the cave ramp
terra_arieki_cavern_endcap_02_24m_v02 (class 6988) with terrain at its top, 29-48 degrees, 30-37 degrees off its fall line;
(754, -115) -> (746, -128) at the turn, the cave ramp terra_arieki_cavern_turn_ramp_v01 (class 9557), 28-44 degrees, up to 70
degrees off its fall line (the ramp curves). Petrovich's two routes are checked on the navmesh.

Fights (rule 3): the Invaders are Atta Soldier copies at level 41 in twos and threes; 1535's ambush is two Atta Soldiers at a
time (solo never four at once; in a squad that talks at the turn together see the tester notes); the squad is unhurt and
fights with you.

Rule breaks (rule 2):
- Every upstream pool in Avernus Outpost (the hive and the graveyard) is a warcry pool: the 100 m warcry rule can not hold
  for Drazen, Petrovich, the pads and the 1535 places (they keep 34 m and more outside every hostile pool's circle).
- The walk to Drazen passes 531047 in the tunnel from the outpost to his cavern and 531061 (21.8 m outside its circle) on the
  ramp down into the cavern (the only way); the turn -> top walk passes
  531042 over the hive tunnel's top half (Petrovich's route). 531042's creatures (8 Atta, AshenSpawnReach's 1500 seeded draws)
  stand on the tunnel floor itself, x 722.6-739.5, z -211.0..-169.9: the walk comes 0.1 m from their ground at (733, -197) and
  stays within 20 m of it from (739, -152) to (735, -231), about 80 m (fix U, to-fix row 56: the tester died twice on this
  way, deaths 33 and 35). A flood on the client's floor (1 m cells, 1.1 m steps) from the turn to the top finds no way that keeps even
  1 m off that ground: the tunnel is the only way, so the leg stays; kill or run past 531042 (1535's 5). The walks from the
  top (1536, 1537, 1581) go west and keep 84.7 m and more from it; Petrovich -> the turn passes the graveyard's own pools (1 has
  them dead first).
- Drazen moved 50.9 m north-east of TaRapedia's place (it lies 1.4 m outside the Atta pool 531055's circle) and Petrovich to
  the graveyard's north-west shelf (fix M; TaRapedia's place is inside 531050's circle, set 17's middle on its spawn ground).
- Drazen stands 31.8 m on the flat outside pool 531061's circle (35.7 m in 3D: the pool is 18.6 m above him at the ramp's
  top); the 34 m place rule holds only in 3D. His cavern has no place farther from it beside the pad.

Tester notes (set 17):
- Take a squad: the bag, corpses, servers, teleporter and the destructibles are personal; each member on 1473 gets his own
  sample, diary and card. With a full bag the bag waits (search again with room). A mate whose mission-item bag is full
  at the player's search gets no sample and his 1 stays open; with room, his own courier bag gives his.
- 1535 in a squad: each member's 6 opens with his own rest talk, his 7 when his 6 completes. A wave kill counts for every
  member whose counter is open (squad credit in the copy): a mate who talks after the player's ambush is over gets his own two
  waves and his own 4; a mate who talks while the player's first wave stands has his 6 and 7 completed by the player's kills,
  his second wave comes while his own first still stands (four of his at once), and his own waves stay alive uncounted. 4 and
  5 still complete for both.
- 1535: Petrovich's route to the top crosses a 38-degree bump at (704-716, -250) (TopWay); the player's walk goes south of it
  (the walk test's leg). Watch whether Petrovich stops there in the real client.
- 1473: a mate without the card (away at the player's search) is refused by his own teleporter.
- 1535: the cave must be cleared after the accept or before it (a cleared cave completes 1 at the accept); Petrovich leads 2 s
  later. Abandoned, 1535 starts over at the graveyard.
- The Atta Teleporter pads are public: any player may ride them.

All ESTIMATE (set 17):
- Levels 41 (1473, 1533) and 42 (1535); Karter 48, Drazen and Petrovich 44, the squad and the Invader rows' levels (42, 41).
- The places of the objects (TaRapedia's /loc, moved to a floor seen from all round), the pads, Drazen, both Petrovich rows,
  the squad, the turn, the ambush and the top; the Invader pools (the only client floor off every walk and place).
- 1535's ready timer (2 s), the ambush's two waves of two, Petrovich's speed.
- Every marker and area radius (1473: 4 m on objects, 6 m on the servers; 1533: the 15 m area; 1535: the 10 m turn and top
  areas, 8 m on Petrovich, 15 m over the ambush, pool circles + 4 m): no source gives them.
- No prerequisites (none sourced). The Atta Invader Chief (9677) is not built: no source gives its level, stats or place.

Set 18, Avernus Outpost, the hive (migration `20271110001800_Ashen_avernus_hive`). All in Avernus Outpost (map 2110). All three
are given at Petrovich's second row 790643 (645.5, 352.67, -246.5) on the Food Storage shelf (1535's receiver). New NPCs:
Captain Tananda 790650 (449, 391.46, -45) at the Cave In; Sgt. Frogga 790651 (652, 353.32, -238) by Petrovich; Colonel
Vandeweghe 790652 (fix M: (294, 216.61, -330) in the hollow down the Queen's Chamber tunnel; set 18's (349, 233.11, -260) on
the ramp over the chamber; 1537's copy only, no public pool); the Eloh
Zuriel 790653 (628.6, 276, -677.8) in the Eloh Sanctuary. The Bane of the Queen's Chamber: a Caretaker 790654, a Fithik 790655, a Mutated Miasma 790656 (name 9037: the
client has no "Altered Miasma") and a Thrax Tunneler 790657 (name 9892), level 43, one each in pool 790658 (fix R: (225, 158.75, -502);
r5; its row's respawn is 120 s, but in the squad's copy, the only place it stands, a cleared pool comes back 45 min after
the last of it died) on the chamber's floor (set 18's (147, 194.11, -590) on the west ledge, an island; fix R). Two Atta Teleporter pairs (local teleporters, class 25302, linked both ways):
917 (509, 368.37, -506) near the southern tunnel's end <-> 918 (fix M: (306, 218.11, -326) in the hollow; set 18's (340,
238.11, -252)) over the Queen's Chamber; 919 (470, 385.83,
-324) on the Food Storage's west side <-> 920 (628, 275.98, -639) in the Eloh Sanctuary's entrance. Next free id 790659. Every
walk below is the client's floor, checked both ways with a 0.5 m half width and the 55 degree climb rule (AshenAvernusHiveTests).
The pads' body (prop_atta_teleporter_v01.geo): a platform 1.1-1.22 m high whose only way up is a ramp on its south (-z) side;
the other sides drop. All four stand at rotation 0: walk up the ramp on the south (-z) side, along its length, to the
platform's centre (the trigger is 2 m in 3D from the pad's origin: on the platform only within about 1.6 m of its centre; by
the platform's sides on the floor, outside it). Every walk to or from a pad goes up or down its ramp
(AshenAvernusHiveTests.ThePadsAreWalkedUpTheirRamps). 917 is 4.3 m north-east of its first place (505, 369.87, -512): the
floor ends 1 m south of that (the tunnel's endcap), so its ramp met the wall (ESTIMATE); at its new place the platform's north
edge touches the rock wall (arch_arieki_atta_queens_obj01_v08), seen only. Arrivals: the server sets a rider down at the far
pad's origin + 1 m, 0.1-0.2 m below the platform's top: check in the real client that he stands on the platform and does not
fall through or stick.

- [ ] 1536 Assessing The Atta (Ligo Ashen Desert): Petrovich (the top) offers it after 1535, level 43. Two Avernus Outpost
  Soldiers (Frogga's men, your own, unhurt by anything) stand by Frogga; about 2 s after the accept they go down the northern
  tunnel (speed 4) and stop at the Cave In. 1: walk to Tananda (r10). 2: talk to Tananda. 3: talk to Petrovich. Turn in at
  Petrovich: 90000 XP, 8200 credits, one of Phoenix Mech Armor Legs 46149 (Lv 40), Pulsar Hazmat Armor Legs 23062 (Lv 44),
  Teleract Reflective Armor Vest 37705 (Lv 44), Pathogex Motor Assist Armor Vest 29247 (Lv 38).
- [ ] 1537 God Save the Queen (Ligo Ashen Desert): Petrovich (the top) offers it after 1536, level 43. Vandeweghe's own copy
  (unhurt by anything; there is no public one) stands at his place from the accept. 1: walk onto the southern tunnel's pad 917
  (r8). 2: it sets you down over the Queen's Chamber; walk to Vandeweghe (r6). 3: talk to him: he leads down the tunnel to the
  chamber's mouth (speed 4). 4: walk to the bottom (273, 144.53, -462; r15). 5: talk to him. 6: clear
  the Bane's pool on the west ledge (marker on it). 7: talk to him: he goes. The markers of 5 and 7 stand at the bottom (the
  lead's end): a marker does not follow him, so if his lead stopped short, look for him on the way down. Turn in at Petrovich: 90000 XP, 6150 credits, one
  of Class VIII Incendiary Grenade 111039 x3, Advanced Med Pack 45065 x2, EMP Grenade 111029 x3, Res Trauma Kit 47600 x1.
- [ ] 1581 The Big Secret (Ligo Ashen Desert): Petrovich (the top) offers it after 1537, level 44. Step onto the west pad 919:
  it sets you down in the Eloh Sanctuary's entrance. 1: walk into the round room (628, 276, -662; r10). 2: talk to Zuriel.
  Turn in at Captain Yamamoto in the Boneyard Outpost (Ashen Desert): 40000 XP, 5850 credits, one of Class VII Fragmentation
  Grenade 45446 x3, EMP Bomb 111232 x1, Cryogenic Grenade 111048 x3, Standard Med Pack 45061 x3.

Walks (set 18; client floor both ways): Petrovich -> Frogga, Tananda (the northern tunnel), the southern pad, the west pad;
the queen pad -> Vandeweghe; Vandeweghe -> the bottom; the bottom -> the Bane's pool; the sanctuary pad -> Zuriel
(AshenAvernusHiveTests.Legs and Walks). The men's and Vandeweghe's routes are checked on the navmesh.

Fights (rule 3): the Bane are one of each at level 43 (copies of the Ashen Caretaker Doctor 531011, Fithik Guardian 540018,
Polonium Miasma 531035 and Thrax Soldier 531008); none has a warcry. Vandeweghe and the men are unhurt by anything (whether
they fight beside you is not set or tested). Tananda, a public NPC 20 m from the warcry pool 531051, can be killed: 1536's
scene names her pool, so in the squad's copy she comes back on her pool's own time (2 s), not after the copy's 45 min.

Rule breaks (rule 2):
- Every upstream pool in Avernus Outpost is a warcry pool: the 100 m warcry rule can not hold for Tananda, Frogga,
  Vandeweghe, the pads and the walks in the hive. The Bane's pool keeps it (its circle 106 m and more outside every Atta
  pool's circle); Zuriel (75.7 m) and the sanctuary pad (36.9 m) keep 34 m.
- Tananda stands 20.0 m outside 531051's circle (the Cave In: TaRapedia's place is inside it; the farthest floor she is seen
  from all round at); the men stop there too. Fix M: Vandeweghe stands 18.8 m and the queen pad 22.4 m outside 531062's
  circle (the hollow down the tunnel; 26.5 and 26.4 m from every pool's spawn draws). Set 18 had them 13.6 and 21.9 m outside
  531058's circle, but on its spawn ground (0.5 and 4.0 m).
- The walks pass the upstream pools on the only way (2D, every leg also checked with its start moved 0.13, 0.27 and 0.4 m):
  Petrovich -> Tananda goes through 531045 and 531051 (the northern tunnel); Petrovich -> the southern pad through 531046
  and 531057 and 7.6 m outside 531059; Petrovich -> the west pad through 531057, 13.0 m outside 531048 and 18.1 m outside
  531059 (the floor round 531057 is the only way that passes the shifted check: the west side's 56-degree spot at (469,
  -282.5) and hump at (468, -317.6) fail it); Vandeweghe -> the bottom through 531044 and 531062's spawn ground (his lead's
  way down; fix M: it no longer passes 531058); the queen pad -> Vandeweghe 18.8 m outside 531062's circle (fix M; 26.5 m
  from its draws).
- Set 18 spawns no scene object (its actors are creatures); no walk crosses one of the pads 917-920 but at its end.
- The meshes under the walks (the probe, 2026-11-12): the hive's floor is the terrain heightmap nearly everywhere. The zig-zag
  climb to the Bane's ledge (the bottom -> the Bane, 2-3) and the queen pad -> Vandeweghe are terrain (no stair or ramp mesh).
  Vandeweghe -> the bottom goes round the cavern light bunch terra_arieki_atta_cavern_light_bunch_v01 (class 25792 at (199,
  220, -328), 2.6 m over the floor) instead of over it, and crosses the low light bunch _v02 (class 25793 at (300, 217, -337),
  up to 1.3 m, faces 29-46 degrees) along the walk: every way round it is blocked at (285, -338).

Tester notes (set 18):
- 1537 in a squad: each member's 2-7 wait until he walks and talks them himself; a mate's sweep counts for a member only if
  the Bane's pool is still down when his own 6 opens (otherwise clear it again after it comes back: 45 min after it was
  cleared, the squad's copy's respawn).
- 1537: the pad's arrival is 12 m short of Vandeweghe: walk to him for 2. Nothing waits for his lead: you can run ahead to
  the bottom.
- 1581: the west pad's arrival is in the sanctuary's entrance corridor; the round room is 23 m on. The turn-in is in Ashen
  Desert, not at Petrovich.
- The pads are public: any player may ride them.

All ESTIMATE (set 18):
- Levels 43 (1536, 1537) and 44 (1581); Tananda and Frogga 44, Vandeweghe 46, Zuriel 50, the Bane 43.
- The places of Tananda, Frogga, Vandeweghe, the pads, the bottom, the round room and the Bane's pool; Zuriel at TaRapedia's
  place. The men's and Vandeweghe's routes (the client-floor walk's corners on the navmesh), their speed, the 2 s timer.
- The Bane's rows (the nearest upstream rows and names), one of each, the pool's circle and respawn.
- The armour pieces of 1536 (the version and level band TaRapedia names; "Experimental" is no client quality).

Fix job M (migration `20271110001930_Ashen_fix_m`, to-fix row 48), a delta on sets 17 and 18 (and on fix L for 1498's scene). A creature notices a player
within 18 m (Creature.AggroRange, 3D, no line of sight) of where it stands, and a pool with a circle stands its creatures
anywhere on the navmesh polygons that touch the circle (NavMeshManager.RandomPointAround): 531050 (r20) draws up to 67 m
from its middle. The circle rules on paper (places 34 m, walks 30 m) protect nobody; the places and walks of sets 15-19 on the
squad maps are now measured against 1500 seeded spawn draws of every hostile pool (AshenSpawnReach,
AshenFixMTests.ThePlacesAndWalksKeepOutOfThePoolsReach: OK 20 m and more, RISK 18-20 m, FAIL under 18 m).
- Petrovich 790642 (1533's receiver, 1535's giver) stood 0.6 m from 531050's draws; the real client died on the walk from
  pad 916 (Atta Soldier Trooper 2554 about 13 m from him) and later found him dead (his pool came back only after the squad
  copy's 45 min). He moves to the graveyard's north-west shelf (873, 284.11, -219): 40.8 m from every pool's draws, 36 m
  outside 531049's circle. 1533's 15 m area, 1535's copy of him and his squad (officer (871, 284.61, -216), soldiers (872,
  284.36, -221) and (875, 283.86, -217)) move with him. Pad 916 (19.7 m from 531050's draws) moves to (888, 281.86, -212):
  30.9 m.
- Pad 915 (Drazen's): the real client does not let a player step off its west side (row 48, 2026-11-12), and its south
  (ramp) side at (64, 480.73, -312) is a 42-45 degree hillside. It moves to the flat floor south-west of Drazen (48, 477.12,
  -318), its ramp foot (48, 477.49, -324.5) on the same floor. Rule break: 26.8 m (3D) outside 531061's circle on paper (34 m
  rule); the walk to it from Drazen passes 25.3 m outside 531055's circle (30 m walk rule). Both far from every pool's
  spawn draws (20 m and more).
- Every walk onto or off a pad (915-920) goes by its ramp on the south (-z) side, both ways
  (AshenFixMTests.ThePadsAreWalkedOnAndOffByTheirRampsOnly; ESTIMATE of the ramp side, the pads' geometry and row 48).
- Every set 17 and 18 leg is now walked with fix J's rampFoot rule (0.6 m terrain-to-mesh step). Rerouted: the plaza climb
  (the real client stopped at (99.15, 522.38, -110.72), under a rock face, row 45; the check passed it; since the follow-up
  the real client's own walked way up the ramp (93.6, -105.2) -> (102, -110.4), see the walk points); the shops' corner
  at (82, -54) (a 0.71 m step onto a mesh); the hive's legs from Petrovich past (585.4, -296.5) and (570.5, -305.3) (0.61-0.65 m
  steps).
- Petrovich, Drazen 790641 and Petrovich at the top 790643 are named public-spawn actors (1533's scene: Petrovich, Drazen;
  1535's: Petrovich at the top): killed in a squad's copy, each comes back on his pool's own time (2 s), as Tananda. A plain
  pool NPC can not be made unhurt by data (only a scene role is). Karter (106 m from every pool's draws) is left as he is.
- Vandeweghe (1537) stood 0.5 m and the queen pad 918 4.0 m from 531058's draws (its whole ramp). They move to the hollow down
  the Queen's Chamber tunnel: Vandeweghe (294, 216.61, -330), 26.5 m from every pool's draws; pad 918 (306, 218.11, -326),
  26.4 m; its pair 917 stays. 1537's area 2 and markers 2 and 3 move with him; his lead down starts at his place (set 18's
  first route point (316, 224.28, -332), back up toward 531058, dropped). The pad's arrival is 12.6 m from him (his area 6 m).
- Two placements off the navmesh (MissionGroundTests, found by fix L's run): 1535's first ambush man (both waves) from (745,
  329.96, -135), 1.81 m sideways off it, to (745.2, 330.48, -136.8); 1497's front field (set 19) from (6.5, 129.5, -405), 6.87 m
  from the navmesh (an object's 6 m), up to (6.5, 131, -405): its foot still 0.5 m under the tunnel's lowest floor (131.5), and
  it still closes the tunnel wall to wall (AshenConscriptionEntryTests).
- 1498's lab charge (a delta on fix L's scene, set 19) from the Atta Laboratory's door (-216, 96.01, -160), 13.6 m from the
  Atta Imperial Guard 520002's draws (a boss pool without a circle), 9 m south along the hall to (-213, 96.01, -169): 21.7 m
  from 520002's draws, 23.6-24.8 m from the lab's own 531064 and 531066, 20.6 and 20.4 m outside their circles (set 19's 20 m
  rule). Objective 5's marker moves with it. The walk from the service pad ends beside it at (-211.41, 96.01, -169.84), 23.4 m from
  520002's draws (set 19's end was 15.3 m).
- All new places ESTIMATE (client-floor and navmesh probe, 2026-11-12): on the client floor, seen from all round, walked both
  ways with fix J's rules (rampFoot, 0.5 m half width, start shifts 0.13/0.27/0.4 m), each corner by a complete navmesh path.

New walk points (fix M), for the tester:
- The pads 915-920 (class 25302) have a wall 7.2 m high round their west, north and east sides (a horseshoe out to 4.6 m
  either side and 9.8 m north of the pad's middle; the client mesh, 2026-11-13), open only on the ramp's side (south): walk
  round it, never along it.
- 1533: Drazen (57.5, 478.62, -315) -> south to (56, 479.62, -325) -> west to pad 915's ramp foot (48, 477.49, -324.5) ->
  north up the ramp onto the pad (48, 477.12, -318). Step off it the same way (south).
- 1533 into the cavern: ... (34, 492.11, -290) -> (43, 479.55, -308) -> south down the west side of pad 915's wall (42,
  476.6, -316) -> (42, 476.9, -324) -> east past the ramp (54, 479.1, -326) -> north to Drazen (57.5, 478.62, -315). Not
  through (47, -311), inside the wall (the real client stopped at (44.7, -310.2)).
- Set 17 plaza (1473's servers, black box, strip corpse, prison teleporter): ... (75, 514.37, -72) -> (88, 514.12, -85) ->
  (92.8, 516.07, -104.4) -> the ramp's foot (93.6, 516.97, -105.2) -> up the ramp to (102, 525.37, -110.4) -> (103.6, 527.12,
  -112.4) -> the plaza (103, 528, -119): the way the real client walked (row 45, 2026-11-13; the client checker passes it).
  Not the straight (94, -101.5) -> (103, -114), which stops at (93.61, -101.54) (the checker refuses it too).
- Set 17 shops (1473's bag, ammo, tavern): (75, 514.37, -47) -> (90, 514.12, -50) -> (90, 514.12, -68) -> on as before.
- Set 18 from Petrovich at the top to the southern pad 917 and the west pad 919: ... (595, 358.61, -292) -> (567, 368.36,
  -313) -> (557, 370.62, -312) -> on as before (917: south; 919: west along z -309). The last steps: to 917 ... (521, 367.61,
  -501) -> (516, 367.4, -504) -> (514, 367.9, -508.5) -> (512.5, 368.4, -511.9) -> west to the ramp foot (509, 368.9, -512)
  -> north up onto the pad; to 919 ... (477, 382.87, -321) -> (474, 385.1, -327.8) -> west to the ramp foot (470, 386.08,
  -330.5) -> north up onto the pad.
- 1533: pad 915 by Drazen -> arrival on pad 916 (888, 282.86, -212) -> down its ramp south to (888, 281.36, -218.5) -> west to
  Petrovich (873, 284.11, -219). The area (15 m) is entered on the walk west.
- Back to pad 916 (row 48, try 3: set 17's pad at (890, -232) stopped a player coming from the east-south-east, 4.4 m from
  its middle): Petrovich (873, 284.11, -219) -> east 15 m to the ramp foot (888, 281.36, -218.5) -> north up the ramp onto the
  pad (888, 281.86, -212), 21.5 m in all. Never onto a pad from its east, west or north side.
- 1498: the service pad -> ... (-194, 96.63, -175) -> (-196, 96, -178) -> north-west to beside the charge (-211.41, 96.01,
  -169.84); the charge (-213, 96.01, -169) stands in the hall 9 m south of the lab's door, not at the door.
- 1535: Petrovich (873, -219) -> (862, 285.36, -236) -> (850, 286, -245.4) -> set 17's line to the tunnel (804, 288.71, -245).
- 1537: pad 917 -> arrival on pad 918 (306, 219.11, -326) -> down its ramp south to (306, 218.36, -332.5) -> west to Vandeweghe
  (294, 216.61, -330); down: (290, 216.9, -336) -> (290, 218.5, -340.33) -> set 18's way at (282, 219.86, -342).

Rule breaks measured by fix M (not moved; AshenFixMTests.Breaks):
- 1395's walk into the Watch Post passes 1360's own pool 790565 (1395 requires 1360).
- Set 16: valve 1 stands in 531088's room (its walk passes it); Kretas' teleporter 18.0 m and the Teleportation Console 19.8 m
  from 531095's draws (their walk passes 12.4 m); 1452's Central Administration area round Kretas (its own target).
- Tananda, her area and the men's stop 16.6-17.1 m from 531051's draws: the Cave In's floor has no spot 20 m off them (the best,
  19.8 m, a corner), and the walk there passes through them.
- Set 19: Peacock -> the field's charge
  passes 9.7 m from 531068's draws, beside 531067 and 531081 it already passes (the yard is the only way; 1497's own field).
- Kept as before (the pools a walk names as passed): every leg through set 16's plant rooms, the hive tunnels, 1535's cave and
  1498's biolab. Their draws reach 0-5 m of the walks: expect fights there.

Fix job O (migration `20271110002110_Ashen_fix_o`, real client 2026-11-13), a delta on set 17 and fix M. 1535's objective 1
(the graveyard's four pools dead at once; no counter in the client) could not complete: two of its upstream pools stand their
Atta on navmesh islands inside the client's rock (as upstream N29). 531049 (821.3, 289, -240.5) r 20: a 22 m2 island west of
the graveyard; the tester saw seven Atta on one point (831.2, 288.8, -238.8) inside the rock wall, no model, "You cannot see
your target from here" from 8.9-15 m (a chase stops at the island's edge nearest the player, ESTIMATE). 531060 (974.5, 274.1,
-230.4) r 13.7: an 8.8 m2 island, not seen from the floor round it (ESTIMATE: the same in the client). The upstream rows stay.
- Two pools of ours with the same creatures and counts count instead: 790700 (814, 289.12, -248) r 4 for 531049 (Atta
  Soldiers 2-3, Harvesters 3-4, Grubs 0-2), on the floor 1535's walk to the tunnel crosses; 790701 (972, 274.08, -212) r 6
  for 531060 (Harvesters 2-4), on the floor north of the island, reached from Petrovich by the navmesh's way east. Objective
  1's markers 1 and 3 move to them (radius the circle + 4 m; 790701's draws reach 14.0 m from its point, outside its
  10 m marker: fix P widens it to 18 m). Killing the island pools does nothing for the objective.
- Their draws are all on the main floor, on the client floor (790701: one draw 0.89 m over it, the navmesh's step height), seen
  from the walk to them and walked to (AshenFixOTests).
- Rule break (60 m between hostile pools): 790700 stands beside 531049's island and 790701 beside 531060's island and 531050's
  draws (the objective's own). The island creatures cannot step off their islands (no navmesh path), so they never join a
  fight on the floor (ESTIMATE from the probe); they may still stand inside the rock, unseen. 1535's walk to the tunnel passes
  2.4 m from 531049's island draws (the walk names both islands as passed): a chase in the rock is possible there, not a
  fight on the walk.

New walk points (fix O), for the tester: 790700's Atta stand on the graveyard floor round (814, -248), just south of the walk
to the tunnel (850, -245.4) -> (804, -245); 790701's round (972, -212), east of Petrovich: from him east along the north
shelf. Expect the island Atta inside the rock west of the graveyard (around (822-831, -243 to -236)) and in the east rock
(around (968-972, -228 to -224)): ignore them.

Fix job P (migration `20271110002120_Ashen_fix_p`, real client 2026-11-13), a delta on fix O. Deviation: our replacement
pools sized for one player; user review. 1535's objective 1 (all four pools dead at once, a cleared pool refilled only after
the squad copy's 45 min) was too much for one player: 790700's nine Atta stand beside 531049's nine island Atta and the
Harvesters' warcries chain. Fix O's counts were upstream's (ESTIMATE); fix P sets them: 790700 one Atta Soldier 531015 (was
2-3), one or two Atta Harvesters 531016 (was 3-4), no Atta Grub 531017 (was 0-2); 790701 one or two Harvesters (was 2-4).
Places, radii and respawn kept. 1535's marker 3 round 790701 widens from 10 to 18 m (its draws reach 14.0 m from its
point, and 4 m). 790701 stays 12 m from 531060's island spit (row 54): no place 20 m and more from the island
passed the floor, sight and reach checks in fix O's probes (the floor east of it is off the client floor, the floor south is
531050's), so it is not moved.

Fix job R (migration `20271110002140_Ashen_fix_r`, real client 2026-11-13), a delta on fix P. 1537's objective 6 could not
be reached: set 18's Bane pool 790658 stood on the Queen's Chamber's west ledge (147, 194.11, -590), and its way up from the
chamber's floor at (243..238, -667..-668) is a terrain slope of 48-50 degrees over 2 m (single 56-58 degree triangles; the
55-degree rule passed it with the leg's start unshifted only, as one steep sample). The real client stopped at (240.1, 169.6,
-664.1) and (237.8, 169.9, -663.3), as on fix N's refused slope (row 50). The server navmesh does not join it either: the ledge
is an island of 10457 m2, the bottom and the chamber's floor one region of 31662 m2. The pool and 1537's marker 6 (radius 9
kept) move to the chamber's floor on the way from the bottom, (225, 158.75, -502): navmesh ground, floor of 0-38 degrees for
7 m round, 66 m from the bottom (Vandeweghe). Its walk from the bottom climbs at most 37 degrees over 2 m (AshenFixRTests,
under 45). Rule break: the place is 60 m+ outside every hostile pool's circle, but 70.9 m (not 100 m) outside upstream
warcry pool 531043's (the chamber's Atta, (275.3, 166.9, -583.7) r20) and 89.8 m outside 531044's (an island 29 m above, set
18's walk down already passes it); no place on the chamber floor keeps 100 m from 531043 but the bottom itself. The walk
keeps 30 m+ from 531043's circle; the warcry only calls if 531043 is fought. No source places the Bane.

Set 19, the Bane Conscription Facility, the entry and the biolab (migration `20271110001900_Ashen_conscription_entry`). The
facility is a squad instance (map 1988), entered by its door in Ashen Desert (map_link 57, (427.74, 305.61, 206.80)); you arrive
at (7.25, 153.5, -670.25). New NPCs: Capt. Peacock 790659 (6, 153.36, -646), 24 m on from the arrival, a public NPC 150 m and
more from every Bane pool; Slocombe 790660 (1498's copy only, no public pool). The front force field
(UsableForceFieldBaneOutpostWallMajorV01, class 9572, intact) across the HQ tunnel at (6.5, 129.5, -405) is each member's own
1497 scene object (no world field): it stands for a member only while his 1497 objective 1 is open, and his charge takes it
away. A Bane teleporter pair (local teleporters 921/922, class
8044, linked both ways, public): 921 (-7, 103.7, -325) in Central Operations' west (fix S2; set 19's (17, 135.94, -370) on the HQ's round hall's east side) <-> 922 (-161.5, 89, -245) in the
biolab's service room. The pads' body (class 8044): a disc 0.68 m up; its rim is a 42 degree slope only on its south (-z) and
north (+z) side within 0.5 m of its centre line (x), steeper elsewhere: step on from the north or south, in line with the pad's
centre (AshenConscriptionEntryTests.ThePadsAreWalkedOverTheirShallowRim). Next free id 790661. Every walk below is the
client's floor, checked both ways with a 0.5 m half width, the 55 degree climb rule and the collision of the set's objects (the
charges, the terminal, the teleporter, the pads), each leg also with its start moved 0.13, 0.27 and 0.4 m (the way back from
its last corner), with fix J's rules (start and end on the corners' own deck, no step over 0.6 m off the terrain onto a mesh).
Each leg keeps 1.28 m (the charge's body 0.18 m + 0.6 m + the half width) from both charges: the walks end beside them, and
the way from the field's charge to the terminal goes round it on its east side (9.5, -410). The hall pad's walk ends on its
north rim 1.68 m from its centre, inside its 2 m trigger (ESTIMATE: the real client's position against that trigger).

- [ ] 1500 Lockdown the Conscription Facility (Ligo Ashen Desert): Captain Yamamoto (Boneyard Outpost) offers it after 1581,
  level 42. 1: go through the facility's door (marker on the plaza 22 m south of the door, Ashen Desert) and talk to Capt.
  Peacock just inside (marker r8 on him, in the facility). Turn in at Peacock: 47500 XP, 6300 credits, one of Class VIII
  Fragmentation Grenade 45447 x3, Class VIII EMP Bomb 111233 x1, Class VIII Cryogenic Grenade 111049 x3, Class VIII Standard
  Med Pack 45064 x3.
- [ ] 1497 Raid the Conscription Facility (Ligo Ashen Desert): Peacock offers it after 1500, level 42; the accept gives a
  Detonator (111195). 1: walk down the yard into the HQ tunnel to your own Explosive Charge (class 7870) at (6.5, 137.61, -410),
  5 m before the field; use it: it takes the Detonator, the field goes down, the charge is set. 2: through the tunnel into the
  tunnel's end deck: talk to your own Bane CommLink Terminal (class 10519, package 1929, named "Conscription Database Interface", 167) at (12, 137.6, -379.5) (fix S; set 19's (-4, 135.94, -372)). Turned in by radio:
  95000 XP, 8400 credits, one of Hellstrom Bio Armor Boots 46005, Pulsar Stealth Armor Helmet 39727, Voltrox Graviton Armor
  Legs 25248, Audiodyne Motor Assist Armor Gloves 31088. The radio then offers 1498.
- [ ] 1498 Blast the Biolab (Ligo Ashen Desert): offered by radio after 1497 (also on arriving in the facility with 1497
  done), level 43. 1: five Bane Bio Material (111198), 2: five Bane Machine Parts (111197), each a 50 % drop of your own from
  the Bane in the facility (bio: Caretaker 531011, Linker 531014; parts: Thrax Soldier 531008, Grenadier 531009, Technician
  531010), only while you need more;
  the markers on the pools 531064 and 531066 by the Atta Laboratory. When you hold five and five, 3 opens: your own Bane
  teleporter (class 8044) in Central Operations' east (19.8, 103.62, -325.6) (fix S2; set 19's (7, 135.94, -358)) appears, usable (none before 3); its use turns it on and Slocombe comes
  through beside it (17, 103.62, -326.2) (fix S2; set 19's (1.5, 135.94, -359)); both are used from (18.3, 103.65, -328.2). 4: talk to Slocombe: he takes the five and five and gives you a bomb (111195). 5: take
  the hall's pad 921 to the service room and walk up the overlook ramp to your own Explosive Charge at the Atta Laboratory's door
  (-216, 96.01, -160); use it: it takes the bomb. Turned in by radio: 95000 XP, 8400 credits, one of Vextronics Incendiary Staff
  49496, Cryogenic Net Gun 12129, ChiTech Leech Gun 45763, Teleract Laser Pistol 14492.

Walks (set 19; client floor both ways): the arrival -> Peacock; Peacock -> the field's charge (the yard, the HQ tunnel); the
charge -> the terminal (through the field's place, the tunnel's mouth into the round hall); the terminal -> Slocombe; Slocombe
-> the hall pad; the service pad -> the lab's charge (AshenConscriptionEntryTests.Legs and Walks). With the field up, the walk
through the tunnel is stopped (the client floor line and ForceFields.StopsPlayer).

Fights (rule 3): no new hostile creature; the drops come from the facility's upstream Bane pools. Slocombe is unhurt by
anything; Peacock is a public NPC far from every pool (no protection needed).

Rule breaks (rule 2):
- The field's charge stands 20.8 m outside 531081's circle and 26.6 m outside 531067's (the guards at the HQ tunnel's mouth:
  the field is in the tunnel, the client map's only way into the facility from the yard).
- The lab's charge (fix M's place, 9 m south of the Atta Laboratory's door) stands 20.6 and 20.4 m outside 531064's and 531066's circles and 31.4 m
  from the Atta Imperial Guard 520002 (a boss, rule 35 m): the lab is the objective, its pools stand in and around it.
- The walks pass the upstream pools on the only way (2D): Peacock -> the field's charge passes 531083 (18.4 m outside its
  circle, the yard), 531081 and 531067 (the tunnel's mouth); the charge -> the terminal passes 531081 and 531067; the service
  pad -> the lab's charge passes 531064, 531066 and 520002 (the lab's own pools).

Deviations (set 19):
- 1497 and 1498 are turned in by radio: the client names the Bane CommLink Terminal as the receiver and the giver of 1498, and
  an object can not take a mission in or offer one; its talk completes 2, and the radio offers 1498 to each member on his own
  1497 reward (and on arrival). The client's texts name nobody to speak to.
- The terminal stands in the HQ's round hall, not in Central Operations (6, 102, -318): on the client's floor Central
  Operations is joined to the hall only by a 1.15 m step up and a spiral whose top lies 1.07-1.3 m under the hall's floor (the
  probe's floods, 2026-11-12).
- Fix S (20271110002150, real client 2026-11-13): the round hall's floor at 135.94 is the terrain heightmap alone, which the
  server navmesh and our collision reader (no terrain holes) take as floor; the real client has none there: on set 19's corners
  it walked down the spiral tunnel ramp under it (to (-2.0, 121.9, -371.6), 14 m under the terminal). 1497's terminal and its
  marker 251 moved onto the HQ tunnel's end deck (12, 137.6, -379.5); the walk ends 2.55 m west of it (9.5, 137.61, -380).
  Slocombe, the teleporter and the hall pads 921, 925, 927 stood on that terrain too: fix S2 moved them.
- Fix S2 (20271110002160): the way on from the deck is down the spiral tunnel ramp (class 4153) to Central Operations (the
  round room at 103.6 at its foot): (9.5, 137.61, -380) > (12, -378) > (14, 134.9, -369) > (14, 132.92, -366) > (12, 130.42, -361)
  > (1.5, 126.69, -361.5) > (-2, 125.6, -362) > (-2, 121.92, -371.5) > (-1, 120.3, -374.5) > (3.65, 119.36, -374.85) >
  (12, 114.49, -374.5) > (13.5, 112.02, -369.5) > (13.5, 111.14, -368.5) > (7.5, 105.27, -351) > (7, 103.97, -343) > (7, 103.81,
  -340); its first loop the real client's own, all of it 2 m from every drop (the well has knee-high walls), both ways. The
  teleporter (19.8, 103.62, -325.6) and Slocombe (17, 103.62, -326.2) (markers 287 and 3 with them), pads 921 (-7, 103.7, -325),
  925 (-7, 103.62, -318), 927 (21.5, 103.62, -319); class 8044's frame stands on its east side, so the pad walks step on over
  its shallow north rim (921) or south rim (925, 927). Break: the server navmesh has the terrain plate, not the ramp, so Central
  Operations is a navmesh region of its own (no NPC route of 1498, 1551, 1499 or 1501 crosses it; none moves an NPC).
- Walk breaks (2 m edge margin, fix S2 sweep): the lab's deck to its ramp (1498, -162..-176) stays on its line at z -156, under 1 m
  from a 10 m drop; the walkway's middle line (z -155.2 to -155.8) has knee-high obstacles, so the break is noted, not fixed; set 20's ramp up from the ledge (x 186.4,
  z -123..-139) runs mid-ramp, about 1.5 m from drops of 2-9 m on both sides, the ramp 3 m wide. The ledge (240, 96.02, -98.5)
  keeps 2 m+ from its 4.9 m drop south; Peacock's way to the charge keeps x -2 at z -495, off the yard's 25 m pit; at z -540 it
  stays at x -3 (a 2-2.4 m terrain dip 1-2 m west, a break: x -2 climbs a 55 degree step there).
- The Atta Laboratory's wing has no walk from the hall on the client's floor (the floods from the hall and Central Operations
  never reach it); the pads 921/922 join them (ESTIMATE: retail may have had a way the client map lacks).
- No lab object is destroyed: the bomb's charge at the lab's door is set (the client map has no destructible lab).
- The field is each member's own and stands only while his 1497 objective 1 is open. A member who never took 1497 sees no
  field and can walk on (ESTIMATE: retail most likely barred him until 1497; a mission scene shows its objects only to its
  owner, and a public field would bar the players who completed 1497 in every later copy, so set 20's 1551, 1499 and 1501).
  The field is not solid on the server for anyone. Real-client check: whether the client stops its owner at it (a scene
  object gets no BlockInfo; the world fields do).

Tester notes (set 19):
- 1497 in a squad: each member has his own Detonator, charge and terminal; one member's charge use credits each member whose
  1 is open and who still holds his Detonator (his Detonator taken, his charge set); a member without his Detonator is refused.
  Each member sees only his own field: a mate who completed 1497 (or whose charge was set) walks through while yours stands.
  An abandon takes your field away; taken again, it stands again.
- 1498 in a squad: each member's drops, teleporter, Slocombe and bomb are his own; a member's teleporter use credits only the
  members whose 3 is open (five and five held). An abandon keeps the gathered materials: taken again, they count at once.
- 1498 (fix L, to-fix row 47): in the squad's copy the drop pools 531064 (bio) and 531066 (parts) come back 120 s after
  they are cleared (their own respawn time; 1498's scene names them), not after the copy's 45 minutes.
- The pads are public: any player may ride them.

All ESTIMATE (set 19):
- Levels 42 (1500, 1497) and 43 (1498); Peacock 45 and Slocombe 44, Harris' looks and stats; 1500 after 1581.
- The places of Peacock, the field, both charges, the terminal, the teleporter, Slocombe and the pads.
- The five and five (no source gives the counts), the 50 % drop chance, the drop creatures.
- Every marker radius (10 m on the field's charge, 6 m on the objects, 8 m on the NPCs, pool circles + 5 m, 6 m before the door).
- The Detonator as 1498's bomb (the client has no other bomb item for it).

Set 20, the Bane Conscription Facility, the Atta queen (migration `20271110002000_Ashen_conscription_queen`). New hostile rows:
the Enraged Atta 790662 (the Ashen Atta Soldier row 531015, named "Atta Soldier", level 43), four in pool 790663 at (350, 97.61,
-104) in the east wing's arena ring (respawn 90 s); the Atta Hive Queen 790664 (class 24087, level 49, 9800 health, the Atta
Imperial Guard's attacks, standing still) alone in pool 790665 at (270, 97.61, -335), the ring's south end (respawn 300 s);
Enslaver Xahn 790666 (class 11134 Boss_Enslaver, Overseer Mitecht's attacks and stats, level 45) alone in pool 790667 at (6.5,
78.11, 166) in Information Extraction (respawn 900 s, ESTIMATE; 4900 hit points and armour 420 as Kretas', ESTIMATE). The
three pools are named by 1551's and 1501's scenes as public-spawn actors: in a squad's copy each comes back on its own respawn
time (90 s, 300 s, 900 s), not 45 min after its last death. New friendly rows: Lt. Humphries 790668, Lukaz 790669 and the Brann
Prisoner 790670 (1499's copies only, no public pool); Commander Jawan 790671 on Thunderhead (map 1911) at (672, 339, -527), 8.7 m
from the base's waypoint (Thunderhead Base Services): Thunderhead's missions are not built, he is only there for 1501. Two Bane
teleporter pairs (local teleporters, class 8044, public, linked both ways): 925 (-7, 103.62, -318) in Central Operations' west (fix S2; set 20's (-2, 135.94, -379) on the HQ round hall's
south-west <-> 926 (288, 96.02, -156) in the east wing's arena ring; 927 (21.5, 103.62, -319) in Central Operations' east (fix S2; set 20's (16.5, 135.94, -359) on the hall's north) <-> 928 (6.5,
77.61, 110) on the deck before Information Extraction (north of the stair from Enslaver pool 531075's room, outside 1501's area). Step on the pads from the north or south, as set 19's. Next free id 790672.
Every walk below is the client's floor, checked both ways with a 0.5 m half width, the 55 degree climb rule and the collision of
the set's objects, each leg also with its start moved 0.13, 0.27 and 0.4 m.

- [ ] 1551 What Makes Them Tick (Ligo Ashen Desert): offered by radio after 1498 (also on arriving in the facility with 1498
  done), level 43. 1: kill ten Enraged Atta (marker r20 on their pool; take the hall's pad 925 to the east wing, then east along
  the ring). 2: kill the Atta Hive Queen at the ring's south end (marker r10): your own DNA sample (an invisible talk object,
  2.5 m off her place at (271.5, 97.61, -333)) appears; talk to it. Turn in at Captain Yamamoto (Boneyard Outpost, Ashen Desert):
  100000 XP, 6450 credits, one of Bio V03 Boots 19415, Stealth V03 Legs 39856, Hazmat V02 Legs 20802, Reflective V05 Legs 38283.
- [ ] 1499 Into the Lion's Den (Ligo Ashen Desert): offered by radio after 1498 (also on arrival), level 43. 1: reach the
  arena (the Atta Observatory: the area is 30 m round the ledge's middle (240, -100), the ledge, the corridor to the cells and
  the pit floor under it, y 72-104; marker r30 on the ledge; entered on the walk in from pad 926) within 10 minutes of the accept,
  else the mission fails (clear it; the next arrival offers it again). At the arena both fights come: the pit's three Atta
  Imperial Guards and four Atta Soldiers round Lt. Humphries (238, 80.97, -150), the ledge's two Nitroglazers and four Atta
  Soldiers before Lukaz (240, 97.61, -54) and six Brann prisoners. Either side: 3: kill the pit's seven (walk west along the
  ledge, up the ramp and down to the pit), or 4: kill the ledge's six, then use your own Cell Door Control (class 7468, named
  "Cell Door Control", 140) at (243, 97.61, -60). The first side done fails the other; the other side's NPCs (and the switch)
  go, and every fighter still standing, either side, falls (no XP or loot; no body is removed: the loot of your own kills stays).
  In a squad the side's credit completes each mate's matching objective, and his own fighters still standing fall too.
  5: talk to Humphries, or 6: talk to Lukaz. Turned in by radio: 100000 XP, 8600 credits, one of Vextronics Sonic Disperser
  12071, Virulent Injector Gun 12155, Vitalius Exothermic Polarity Gun 12135, Vextronics Cryogenic Net Gun 12129. The radio then
  offers 1501.
- [ ] 1501 Save the Queen (Ligo Ashen Desert): offered by radio after 1499 (also on arrival), level 44. 1: take the hall's pad
  927 to the deck before Information Extraction (pad 928) and walk north into it (marker r35; walk round the platform's hole at (5-8,
  149-153)). 2: kill Enslaver Xahn (marker r10). 3: talk to the Atta queen (your own invisible talk object at (6.5, 78.11,
  159.5)). 4: use your own Life Support System console (class 7468, named "Life Support System", 153) at (8.5, 78.11, 160.5): the
  hanging device (class 26423) goes off, the queen is at rest. 5: talk at the egg (11.5, 78.11, 155): the egg goes with you. 6: talk to Captain Yamamoto
  (Boneyard Outpost, Ashen Desert). 7: talk to Commander Jawan at Thunderhead Base (map 1911; marker on him). Turn in at Jawan:
  200000 XP, 12000 credits, one of Weapon Module: Debuff Ice Resist [4] 123181, Weapon Module: Steal Power [4] 123276, Armor
  Module: Resist Virulent [4] 123071, Armor Module: Power Regen Bonus [4] 123006.

Walks (set 20; client floor both ways): the hall -> the east-wing pad 925; the hall -> the fortress pad 927; pad 926 -> the
arena's ledge; the ledge -> Lukaz and the Cell Door Control; the ledge -> the ramp -> down into the pit to Humphries; pad 926 ->
the Enraged Atta; pad 926 -> the queen; pad 928 -> Information Extraction -> round the platform's hole -> the queen's life
support -> the egg; on Thunderhead the base's waypoint -> Jawan (AshenConscriptionQueenTests.Legs and Walks). With fix J's
rules (start and end within 0.5 m of the corners' deck, no step over 0.6 m off the terrain onto a mesh): the walks to the hall
pads 925 and 927 end up the pad's shallow rim, 1.6 m from its centre, inside its 2 m trigger (as set 19's hall pad); the walks
from 926 and 928 start on the pad's disc. The walk from 928 goes north along x 6.5 to z 127, then by x 9 round the raised step
at x 6.5, z 129-136 (no client floor line along x 6.5 there). Every walk keeps 30 m outside every hostile pool's circle, set
20's own too, but the pools it names (its own target pool; the rule breaks below, those kept 15 m off but 531065).

Fights (rule 3): the Enraged Atta, the queen and Xahn are public pools (a squad shares them; each kill counts for every member
on the mission); 1499's fighters are each member's own (the scene's, hostile; as Petrovich's Run's ambush). Humphries, Lukaz
and the prisoners are unhurt by anything.

Rule breaks (rule 2):
- Xahn's pool stands 20.2 m outside the circles of Information Extraction's Enslaver cell pools 531074/531077 (rule 60 m), and
  the queen's talk, the Life Support System, the egg and the room's marker 19-22 m outside them (rule 34 m): TaRapedia puts the
  Enslaver and the queen among the four cells.
- 1499, Humphries' side: the walk down to Humphries goes through the middle of upstream pool 531065 (the ramp's own Atta pool,
  r30; the only way down into the pit): that side needs 531065 cleared on the way (TaRapedia makes it the hard side; user review).
- 1501: Information Extraction's places stand 17-23 m from the Enslaver cell pools 531073-531077 (AggroRange 18 m): nothing in
  the room can meet the 30 m rule; TaRapedia puts Xahn, the queen and the egg among the four cells (user review, real-client
  check). The walk from pad 928 keeps 15 m or more outside each of their circles; the 34 m place rule is broken by pad 928
  itself (24 m outside 531075's circle; nearer the room there is no floor outside 1501's area, r35).
- Spawn reach (fix M's AshenSpawnReach: 1500 seeded spawn draws per hostile pool, a pool without a circle its point and a
  10 m stroll; least 3D distance; OK 20 m+, RISK 18-20 m, FAIL under AggroRange 18 m; a mission's own target pools exempt;
  AshenConscriptionQueenTests.ThePlacesAndWalksKeepOutOfThePoolsReach). FAIL: the walk down to Humphries through 531065's draws (0.1 m, the rule break
  above); the walk into Information Extraction 17.1 m from 531073's draws and 14.9 m from 531076's at z 126-128 (round the
  raised step at x 6.5, z 129-136; the cells, above). RISK: the Life Support System 19.3 m from 531077's and
  19.8 m from 531074's. Every other place and walk 20 m or more.

Deviations (set 20):
- 1551: the client's "Atta Queen Eliminated" counter (14085) is not written; the queen's kill opens a hidden objective, and the
  DNA sample's talk completes 2. The Enraged Atta have the Atta Soldier's name and look (no client name; a red tint later, set 21).
- 1499: the key room Humphries names (14308) and Lukaz' three men (14313) are not built; the prisoners and Humphries can not
  die (the client has no fail text for them; the other side "dies" by being shut out). 1499 and 1501 are offered by radio
  (Headquarters) and 1499 turned in by radio (TaRapedia: Headquarters).
- 1501: the egg can not be attacked or lost (M3: scene creatures can not attack an object, and the server has no "protect an
  object" mechanic); the queen's body is the client map's (her talk object is the client's invisible one), the Life Support
  System turns off, nothing dies. No egg item (the client has none): the egg's talk is an objective only.
- Thunderhead is not built: Jawan stands there for 1501 only.

Tester notes (set 20):
- 1499's deadline runs from the accept (also an accept by radio outside the east wing): take the hall's pad 925 at once.
- 1499, Humphries' side: clear the ramp's Atta pool 531065 on the way down (the walk goes through it); the Cell Door Control
  side does not pass it. Accepted on the ledge itself, 1 completes only after a step off it and back (an area is entered by a
  move into it).
- In a squad's copy the Enraged Atta come back 90 s after the last of the four died, the queen 300 s, Xahn 900 s.
  The queen (by the DNA sample) and Xahn (in Information Extraction, by the queen's talk, the device and the egg) can come
  back next to an open objective after their own respawn: accepted as a low risk (re-audit).
- 1499 in a squad: each member's arena, fighters, NPCs and switch are his own; a member's kills of his own fighters count for
  every member whose matching objective is open (GroupCreditService).
- 1551 and 1501 both talk with package 1937 (the DNA sample, the queen, the egg): each object talks only of its own mission.
- Real-client checks: Xahn's class (11134, never used before) with Mitecht's attacks; the queen's class 24087 standing still;
  the invisible queen talk objects can be clicked; the Life Support System's states (10000003 on, 10000001 off).

All ESTIMATE (set 20):
- Levels 43 (1551, 1499) and 44 (1501); the Enraged Atta 43, Xahn 45, Humphries and Lukaz 43, the prisoners 40, Jawan 45 (TaR).
- Ten Enraged Atta (D16), four in one pool; the queen's health, attacks and stillness; Xahn's rows (4900 hit points, armour
  420, as Kretas') and his 900 s respawn.
- 1499's 10 minutes (M7), the fighters (TaRapedia's counts: three bosses and four Atta, two Nitroglazers and four Atta) and
  their places, the Cell Door Control, the arena's area (30 m round the ledge).
- The places of every pool, NPC, object and pad; Jawan's height (TaRapedia's y 383 is the roof).
- Every marker radius (r30 arena, r35 Information Extraction, r20 the Atta pool, r15 the fights, r10 the bosses, r8 NPCs, r6
  objects).
- The items: the version and level band TaRapedia names (1551 armour, 1499 weapons), grade 4 modules (1501).

## Deviations (client/server limits)

- 1274: one marker, not two. The Ridge's other automatic Fithik pool 540066 puts its Fithik in an isolated navmesh pocket
  (lowest of three layers at 72.99, -581.11: y 325.3, 330.6, 336.5; about 15-25 m) with no path to any waypoint. The pool
  row is not changed (upstream navmesh defect).
- 1250: Colonel Mason Linier stands at the entrance of the Shadow's Edge HQ, not in the rooms under the post: the rooms
  are within 20 m (2D) of the Fithik pools 540083 and 540084 on the ridge above.

- 1245: the air strikes in the offer ("Despite your efforts ...") and any Atta reaction to a beacon are flavour; a planted
  beacon only completes its objective (no mission air strike, as Crucible 1214).
- 1244: Exing stays near the World DB spot that Ellatha, TaRapedia's boss page and 1580 give; the client log's "Sheitan
  Outpost, at the north end of the Sheitan Highway" is not followed (triage D10). He does not walk "among the Atta" (the
  1244 offer): a boss pool of his own, 14 m from the upstream spot.
- 1242: the Atta "call for assistance" (TaRapedia 2007-12-11) is not built (no such server behaviour).

- 1247: "the Thrax are landing in waves" (the reminder) is flavour: no dropship waves, the pools respawn.
- 1247: the kills count by creature row (531009, 531008), so Thrax Ravagers and Thrax Soldier Mark VIs killed in the Bane
  Conscription Facility and Indra Caverns (the same rows) count too, not only those of the Sheitan Highway.
- 1248: the air strikes on the camp after the turn-in (the turn-in text) are flavour.
- 1249: the mortars shell what comes within their reach (BaneMortars), not Shadow's Edge Post far to the south (the log's
  "firing at Shadow's Edge Post" is flavour).

- 1262: the Xanx "digging tunnels and ambushing" (the offer) do not burrow (MawLurk is keyed by class; triage M10).
- 1262: the kills count by creature row 540020, so Xanx killed in Maji Field and Indra Pass (the same row) count too.
- 1272: "Cowards started dropping Stalkers on us" is flavour: no dropship, the pools respawn.

- 1354: the explosion of a generator does no damage to a player near it (the offer's warning; not built, triage set 5).
- 1352, 1354: the launchers and generators cannot be shot: only the Detonation Device blows them. The client lets a player
  aim at them (their classes' target flag is 1), but they are not interactable and the server drops a hit on a disabled
  object (PracticeTargetManager.TryGetTarget).
- 1355: "Hundreds of them": fifteen Linkers in five pools round the base, not a perimeter of hundreds. Linkers killed in
  the Bane Conscription Facility, Indra Caverns and on the Sheitan Highway count too (same row 531014).

- 1254: a player can change sides by abandoning 1254 before the turn-in and choosing again (the flags are set at the
  choice, the follow-ups open only at the turn-in).
- 1254: the client reminder texts 10066 / 10067 are not sent (no server reminder).
- 1285: the ring drops off any Ashen Thrax Soldier or Ravager (a drop is bound to creature rows and the map), not only
  those of the Boneyard: also those of the Sheitan Highway and the Conscription Facility.
- 1285: Megumi does not stand in the "eastern tent" (TaRapedia): see the assumptions.

- 1265: the Predator salvage comes off the Desert Predators of the Sheitan Highway (the client's log and TaRapedia), not
  "near Maji Field" (the client's offer text): no Predator stands there.
- 1264, 1266: the client gives the objectives no counter text: the log shows no n/15 count (the bag shows the stack).
- 1266: rifles and launchers drop off any Ashen Thrax Soldier or Ravager on Ashen Desert (the drop is bound to the rows and
  the map), as 1285's ring.
- 1255-1257 (the client's further flag-250 missions) are not built (triage D2): Latan's side ends with 1265.
- 1268: the title "Desert Logistician" is not given (it is the Targets of Opportunity's objective 17, not built upstream).
- 1267, 1269: no n/15 counter in the log (no counter text in the client). 1269's salvage drops off any Ashen Thrax
  Soldier / Ravager on Ashen Desert.

- 1278: no fail when the player leaves Stonewall Ridge (the client's body of 2): the server has no area-leave event
  (triage M2, D17).
- 1278: the client's "Towers Lost" counter (10295) is not shown: a counter rule can only complete its objective
  (MissionRuntime.Evaluate), so the losses are counted on a hidden server-only objective 7 whose completion (the fourth)
  fails 2.
- 1278: the client's wave helpers 3-6 ("Wave 1", "Spawn 2".."4") and 1277's 5 ("Ambushes") are not written.
- 1278: a lost turret's public copy is shown again to the player at once (the hide is held by the turret copy).

- 1287, 1289, 1291, 1293: the drops are bound to the creature rows and the map, so a hide drops off any of the set's
  hide creatures on Ashen Desert (1287: off any set 2 Atta Soldier / Worker), not only in the named area.
- 1287, 1289, 1291, 1293: no n/10 counter in the log (no counter text in the client).
- 1291: Trudy not on TaRapedia's hilltop (-242, 771): it is not reachable on the navmesh.
- 1295: the panels do not check that the player holds the diagnostic tool.
- 1295: panel 2 is 33 m and panel 4 4 m off TaRapedia's 1302 spots (a hostile camp and the view).
- 1298: the client's empty choice 3 (10486) is not offered. The repair branch (1299) and the poison chain (1300, 1302) come
  in set 12; 1297 and 1301 are not built (triage D3).
- 1298: "Murky Waters Lead In" (TaRapedia, no client id) is not built: Meeks gives 1298 himself.
- White Oasis Post: Daris Linier, Fontecilla, Meeks and Hale are gone while the Bane hold control point 17 (the server's
  existing rule for service NPCs within 130 m of a point).

- 1299, 1300: no n/12 counter in the log (no client counter text); the glands drop anywhere on Ashen Desert, not only at
  Coheri Pools / on the ridge.
- 1302: the panels do not check the systems by kind (each use takes one; a mission item cannot be destroyed).
- 1297, 1301 (the repair side's follow-ups, client only) are not built (D3): the repair side ends at 1299.
- 1280-1282: Odesha and Endala stand by the Indra Pass valley's south entry, not in the cave by the Indra Caverns
  (TaRapedia): the Indra Pass waypoint's floor has no way in (N23, fix G), the cave pocket none we can check (N26).
- 1282: Maiek 64 m from Overseer Naxveik, not "very near" him (the 34 m keep-out of the base's hostile pools).
- 1281 / 1282: the exclusive offers and the fork are offer/accept rules and a scene signal; a GM-given pair still fails
  the other side at either end.
- 1281 / 1282: a failed side keeps its own objects (resistors or Maiek's body) for good: a failed mission cannot be
  abandoned (only an Active one); a use of them counts nothing. Only a GM's gift holds both sides.
- 1736: kept on the Crucible's revision (crucible_1_16_5) with 1218, not Ashen's: the server keeps a required chain on one
  revision (MissionContentValidator, "cross-revision-reference"), as Marshes 1673 on Plateau's.
- 1736: the obelisk is a scene object that comes at the landing, 50 m from the escape pod (the pod's ground is a Tree Mite
  pool's), only once the first ambush is dead (a server-only hidden objective 6 counts the two); the researchers are the run's
  own, not world NPCs; the helpers 3-6 do not show in the log.
- 1736: both researchers need not arrive: objectives 3 and 4 complete with objective 2 (Botha's talk), wherever the
  researchers are then; only a death fails them. A logout or map change leaves them waiting; after a server restart they are
  placed again by Botha and run to the player (ESTIMATE of the retail behaviour: no source).
  A first-pair Thrax that is still alive (one that gave up the chase, or one that comes back at its spawn after a server
  restart) keeps the obelisk away until it is killed: kill both of the first pair before you look for the obelisk.
- 1286: Jirex 30 m from the pillbox, not by it (60 m from the Boneyard's hostile pools).
- 1358: the White Oasis delivery waits while the Bane hold control point 17 (Daris Linier gone, the service rule).
- 1360: kills at the Watch Post before the accept count (3 completes at the accept if both pools are clear): TaRapedia's "Do
  NOT kill ahead" can not be kept, a pool is the squad copy's, not the mission's.
- 1360: no Thrax attack on Drof in the Control Room (TaRapedia "wipe out the thrax attacking Drof"): no scene fight in a
  closed room (the cautious option).
- 1360: the Watch Post's west pool stands on the only way into the facility: every walk in meets it (1360's own fight;
  the 30 m walk clearance is not kept there). The east pool (fix J, to-fix row 36) stands off the way: 107 m from the
  entrance tunnel (past the 60 m leash), 65 m and more from every 1360/1395 walk, 60.6 m from the west pool; 72 m from the
  Watch Post's point (-5, -351), not near it: no place on the Watch Post's floor keeps the 60 m pool rule, the walks (30 m
  plus the 10 m roam) and the tunnel (leash) at once (ESTIMATE place).
- 1452 (fix J, row 43): the Explosive Charge stands on the generator's east face, 5.8 m outside pad pool 531085's circle
  (the place rule asks 13 m): the generator's body leaves no place by it further off.
- Known gaps (fix J, follow-up): the walk check sees the map's geometry only; scene objects' bodies (the generator, the
  teleporters, the drills) are kept out by the walks' end places (AshenFixJTests.TheWalksEndOutsideTheSceneBodies), but
  the 1447 drill walks still end inside the drills' bodies (the drills are shot from their sides). The 1452 pad loop round
  the trench (the client's way, row 44) is not walked by the check: along the trench rims it finds knee-high geometry the
  client passes.
- 1360 / 1395: a squad mate's 1 (1360) completes with the user's search and gives the mate his own card (as Crucible
  1401's cards); a mate's 2 and 5 (1395) complete with the user's search without the radio or power supply (only the user
  gets those; nothing later needs them).
- 1395: the Thrax Hand is kept at an abandon (CharacterOwned, as Crucible 1401's cards; the server's cleanup can not take a
  character-owned item); each member's hand is taken when his own 6 completes. Taken again, the kept hand opens the new
  cell, but 4 still waits for Kuutk.
- 1360 / 1395: the doors and the cell are personal objects: a player without the mission is not shown them at all, and
  creatures walk through them shut (the server has no door collision).
- 1559: the Ashen navmesh does not join the door's pocket (N26): the marker before the door is checked on the client floor.
- 1559 (fix H): a lift pad pair (local teleporters 913 in the valley by Odesha, 914 on the door's island) that retail did not
  have as far as we know: the island has no walk-in on the client floor (N23, N26). Both places ESTIMATE; 1559.1 has a second,
  unnamed marker on the valley pad. Not gated: any player may ride it.
  The caverns' navmesh is islands (upstream): creatures may not path between rooms; Kuutk and the Watch Post stand still.
- 1395: the Power Supply is taken at the turn-in; Doomed Expedition (1447) asks to keep it: set 16 issues one at its accept.
- 1447: a teleporter's arrival just outside the rear exit does not complete 13 by itself: the server counts an area only
  when walked into, so the first steps toward the caves do it.
- 1447: the teleporters and the door are the player's own objects; a player without the mission is not shown them, and
  creatures walk through the shut door (no door collision).
- 1447: the Bane tunnel to the mining area runs through hostile pool 531094 and over 531088 and 531089: the 30 m walk
  clearance can not be kept on the only way.
- 1532: Drof and Naimi stand at the plant's entrance from the start (and set 15's rows stay in the facility); the client's
  AFS dropship that flies them in is not built.
- 1451: TaRapedia's six Thrax at each switch use are not made (rule 3: a solo player must win each fight; the plant's own
  pools stand in the rooms).
- 1452: no Juggernaut creatures (the client map's three are statics) and no Kretas Drone; one console stands them down.
- 1452: the generator's bomb sets off no siren and no Bane dropship wave at the pad (the deck's own pools stand by it).
- 1451 / 1452: the walks through the plant pass its own upstream Thrax pools, and the switches, consoles, generator and
  Kretas stand closer to them than the place rules ask (listed under the set 16 rule breaks): the plant's rooms are small.
- 1452: Kretas' hand is kept at an abandon (CharacterOwned, as 1395's hand); each member's hand is taken when his own 7
  completes.
- 1452 (fix J): Kretas (6) opens with Locate Bane Central Administration (5) when the generator (3) completes, so a member
  who never walked into Central Administration's 16 m can still do 6, 7 and 8; Diren then refuses the turn-in until he
  walks back into the area (5 completes there).
- 1473 (fix L, to-fix row 46): the prison is a closed room in the real client (walls in 12 directions; the walk out by its
  east stair stops at the stair's foot), though the client collision the server reads (arch_hum_fort_prison_v02.geo) has a
  stair, corridor and ramp out to the east and the client map places no force field: the walk checks take the room as the
  client has it (AshenAvernusOutpostTests.ClientClosedRooms). The teleporter (locked to the card) is the only way in; the
  way out is a one-way local teleporter, 923 (class 7895, the inbound teleporter's look) in the room to 924 (class 0, not
  drawn) by the inbound teleporter, public: anyone in the room leaves by it at any time (ESTIMATE: retail's way out is
  not known).
- 1473: the stasis tube only takes damage while 5 is open (the 2008 patch note), not before the diary.
- 1533: the client gives 1533 no objective; the server's one hidden objective (an area round Petrovich) holds the reward.
- 1535: Petrovich and his squad are unhurt by anything (Invulnerable): the client has no fail text for a death.
- 1535: the lead starts from a 2 s scene timer after the accept (a scene start inside the accept is refused).
- The Atta Teleporter (915/916): local teleporter pads that are not gated; any player may ride them.
- The Avernus Invaders in the north ravine stand on a navmesh island of their own: they do not chase into the base.
- 1536: Frogga's men are unhurt by anything and are the player's own; they lead on their own (nothing waits for them, and 1
  is the player's own walk to Tananda).
- 1537: Vandeweghe is unhurt by anything (the client has no fail text). His lead does not wait for the player.
- 1537: Vandeweghe has no public NPC, only each player's own copy while 1537 runs (user review). The ramp's tip is the only
  floor round TaRapedia's place, and no spot on it 15 m (the start-up safe-ground check) outside 531058's circle is seen from
  all round; a public NPC there fails that check. A player without 1537 does not see him. Fix M moved him and the queen pad
  off the ramp (on 531058's spawn ground) to the hollow down the tunnel.
- 1537: "Sweep the Room" is one pool of the four Bane kinds, one each (no source gives their number); the sweep counts the
  pool cleared, also one cleared before 6 opens while it is still down.
- 1581: no Targets of Opportunity operation (1580, upstream content, out of scope).
- The hive's Atta Teleporters (917-920): local teleporter pads that are not gated; any player may ride them. The client marker
  123 "Queen Chamber" is not used (no source ties it to 1537).

## Server data that differs from the sources (not changed)

- Lieutenant Jan Blood 510013 is level 10 with 1000 HP (upstream), in a level 40-42 zone. Not changed: he is friendly and
  stands in the outpost, no hostile pool reaches him.

- The Stonewall Ridge Fithik pool 540066 stands 14 m under the ground. Cosmetic, upstream design: the spawn puts its
  Fithik on the ground (SpawnPoolManager.Anchor, RandomPointAround). Its ground is an unreachable navmesh pocket (upstream
  navmesh defect).

## Server data changed by set 1 (upstream rows)

- Moved (upstream U1, D7): Mason Linier 510010 (from the Shadow's Edge weapon vendor, -163.4, 296.39, -709, to the HQ
  entrance), Harris 510008 (from 4 m off the Ashoka waypoint, under 2 m from the navmesh edge, to 405, 281.41, -459.5),
  Emmons 510009 (from the Stonewall Ridge region centre, 179.3, 316.37, -557.2, to the Command Tower), Daris Linier
  510007 (2.0 m from his upstream spot, 3.68 m from the White Oasis control point's point; now 4.17 m from it). Down puts them back.

## Server data changed by set 2 (upstream rows)

- Moved: Thrax Overseer Exing's pool 520007 from (-281, 340.4, -252), a navmesh island on the mesa top that no walk from
  any waypoint reaches (probe of 2026-11-09), 14 m to (-289, 332.98, -264) on the mesa's slope, walked to from the
  Stonewall Ridge and Shadow's Edge Post waypoints. Down puts it back.

## Server data changed by set 3 (upstream rows)

- 531009 "Thrax Grenadier - Ashen Desert" (no name id upstream) gets the client name 9223 "Thrax Ravager" (triage D9). Down
  takes it off while it is still 9223.
- The Sheitan Highway mortar pools 592191-592194 (Bane_mortars) hold the copies 790081-790084 for 592011 (BaneMortarTests
  allows it). Down gives them 592011 back while they still hold the copy.

## Server data changed by fix job A (20271110000300)

- Fithik pool 540067 (to-fix row 1): from (233.2, 316.4, -651.2) radius 15 to a point (radius 0) at (261, 308.3, -642.2),
  29 m east on the Ridge's open slope; 1274's marker from (234.8, 320.05, -651.2) onto it (261, 308.6, -642.2). Upstream's
  15 m (Detour's random points reach up to 10 m past it) took in ground under a rock shelf (about x 232-240, z -660 to
  -636): 15 % of the Fithik came down there, a client surface 3.5-7 m over them, and could not be shot from the shelf; not
  killed, they filled the pool. A full spawn comes on the point and its rings, a single top-up on the point. The point has
  open ground joined to the waypoints 17 m round it (the rings and a 10 m stroll), one navmesh floor in each spot. ESTIMATE: the place. Down puts upstream's pool and set 1's marker back while they are still the fix's.

## Server data changed by fix job B (20271110000610)

- Thrax Overseer Exing's pool 520007 (to-fix row 5): from set 2's spot (-289, 332.98, -264) on the mesa's slope to (-260,
  308.9, -310) on the flat at the mesa's south foot, 54 m on; 1244's marker with him (-260, 309.26, -310). No real-client
  walk reached set 2's spot: every navmesh route ends on a mesa flank the client floor does not climb (more than 0.9 m a
  metre; upstream N19). The new spot is walked on the client floor (0.9 m steps at most, nothing in the way at chest
  height) from the Shadow's Edge Post waypoint, the Mauna Barrens basin and the Sheitan Highway camp; still west of the
  highway and south of the Barrens (D10). ESTIMATE: the place. Down puts set 2's pool and marker back while they are
  still the fix's.

## Server data changed by set 4 (upstream rows)

- None. Set 4 uses Blood 510013 and the Xanx row 540020 as upstream has them.

## Server data changed by set 5 (upstream rows)

- The trench mortar pools 592199, 592202-592204 (Bane_mortars) hold the copies 790160-790163 for 592011 (BaneMortarTests
  allows it). Down gives them 592011 back while they still hold the copy.
- Moved: Overseer Naxveik's pool 520005 from (305.41, 282.61, 84.41) to (296, 296.49, -30) in the generators' compound.
  Down puts it back (the exact stored values) while it still stands at the new place.

## Server data changed by set 6 (upstream rows)

- The Boneyard Thrax pools 531038 (544.2, 283.2, -120, radius 14.4), 531039 (499, 285.5, -269.3, radius 10.7) and 531041
  (502, 277.5, -198.2, radius 12.6) become point pools (radius 0) at (566, 285.1, -136), (487, 286.45, -261.3) and (487,
  271.9, -219), 14-27 m away (probe of 2026-11-09). Upstream's radius draws (Detour's random point round the circle) put
  Thrax up to 47 m from the point, onto rock tops (531038 has two navmesh floors 2.7-6 m apart) and 2-5 m off the rows'
  heights (the rows stand 1.7-2.75 m under the ground). Round each new point every navmesh floor within 17 m is one floor,
  joined to the Ashoka Settlement waypoint, on a client floor under open sky. Count, creatures and respawn as upstream.
  ESTIMATE: the places. Down puts upstream's rows back (the exact stored values) while they are still set 6's.

## Server data changed by set 7 (upstream rows)

- None. Set 7 adds its own rows only; it uses the Thrax rows 531008 / 531009, set 3's Desert Predator and set 4's Desert
  Stalker as they are, and upstream's machina_revive action 62059 and the Torcastra Machina's look 660012 by reference.

## Server data changed by fix job D (20271110000710)

- 1264's reward item 2 (Motor Assist Armor Legs): set 7's 29093 (level 38-42) to 29113 (V04 Uncommon 43-47; TaRapedia
  "Dynamo Motor Assist Armor Legs 2, green, 43"), guarded on set 7's exact row; an evidence row 4 for 1264. Down puts
  29093 back. A delta on set 7 (live): set 7's data class stays.
- Teleporter 208 (Hospital: Shadow's Edge Post, upstream row): y 269.3 to 296.5, x, z and rotation unchanged. Upstream's
  height is 27 m under the hospital's floor (client floors 296.20 / 296.50 / 300.95 / 302.27 / 307.30 at its x/z, navmesh
  296.68): a hospital return fell under the map and the fall guard put the player back at the death spot. Guarded on the
  row's exact stored values; Down puts 269.3 back. AshenHospitalTests checks every Ashen hospital and waypoint (195-198,
  207-209, 317-319, 322) on the client floor and the navmesh, and the walk 208 to and from the Shadow's Edge waypoint.
  Retest: die in Ashen Desert, take the hospital return (or Respawn at Shadow's Edge Post Hospital): the player stands in
  the hospital.

## Server data changed by fix job E (20271110001010)

- 1277's scene (set 9, live): the four consoles 5-6 m off their upper pads on the lift's exit side, so the walk from the
  exit keeps 2.5 m off the pad's 2 m trigger (to-fix row 12); each console's use only within 3 m of its floor
  (SceneObjectAction.MaxUseHeight, an opt-in field no other scene sets; row 13); the ambushes on the top floors (row 14).
  The scene's row is swapped only where it holds set 9's exact JSON; Down puts it back. Evidence row 4 for 1277.
- The Sheitan Highway Ravager pools (set 3's rows, live): 790085 (-180, 306.57, -305) to (-213, 307.87, -302) and 790086
  (-140, 306.34, -305) to (-125, 314.27, -359), three Ravagers each to two (the encounter tuning, user 2026-11-10: a solo
  player died twice; the pools stood 24-27 m from the camp's upstream pool 531034, radius 30, and pulled with its Linker
  and Caretaker; now 53-55 m off it, ESTIMATE). The markers of 1247 (r20), 1266 and 1269 (r25) on them move with them.
  Guarded on the rows' exact stored values; Down puts set 3's places and counts back. Evidence rows 4 for 1247, 1266, 1269.
  Retest: 1266 / 1269 launchers and 1247's Ravagers solo at the new places; the camp's Linker and Caretaker should not join.
- Set 10's pools (live, migration 20271110001000; to-fix rows 17, 18): Sally 790361 (-439, 299.63, -258) to (-439, 298.47,
  -298), Molly 790366 (299, 295.08, -658) to (315, 283.02, -622), Trudy 790372 (-198, 299.48, 648) to (-250, 302.74, 632),
  the eastern Boneyard Beam Manta 790368 (764, 305.49, -322) to (770, 327.21, -400); the markers on them (1287 2, 1289 2,
  1291 2 and 1291 1's first) move with them. Each boss is now 38 m and more from every other hostile pool, and the last
  100 m of each walk to a set 10 pool from its waypoint keep 30 m and more from the hostile pools not of its order
  (AshenFixETests); set 10's places failed it (Sally 20.6 m, Trudy 15.8 m; the walks to Molly 22.3 m, to the Boneyard
  Manta 2.2 m). Wendy and the other pools stay. Guarded on the rows' exact stored values; Down puts set 10's back.
  Evidence rows 4 for 1287, 1289, 1291. ESTIMATE: the places (TaRapedia's spots for Sally and Molly are 40 and 47 m off).

## Server data changed by fix job F (20271110001310)

- Set 10's boss pools (live, migration 20271110001000; to-fix row 18): Sally 790361, Molly 790366, Trudy 790372, Wendy
  790377 respawn in 3000 tenths (300 s), not set 10's 500 (50 s, the value of every upstream boss pool); Sally came back
  about 50 s after her death by the camp a player farms. ESTIMATE: 300 s, the World DB's most common respawn. Guarded on
  the rows' exact stored values (and their boss); Down puts 500 back.
- New pool 790378 (to-fix row 22): three Ashen Atta Soldiers (set 2's 790043), radius 0, 120 s, at (-355, 328.97, -210) on
  the slope east of the Mauna Barrens camp, with a marker of 1287's objective 1 (indicator 6, r20). The camp's Ashen Atta
  Workers (790044, a copy of the Thunderhead harvester) have the Atta harvester warcry (477: the two nearest hostile
  creatures within 80 m that are not fighting join, once a minute a Worker), which reaches every camp pool, Sally and the
  Mutated pools: each pull there became a group fight. The new pool is 79.6 m from the nearest hostile pool, 106 m from the
  Worker pools, walked both ways from the Shadow's Edge Post waypoint (ESTIMATE: the place). Its Soldiers count for 1242
  and drop 1287's carapace. Down takes the pool and the marker out. Evidence rows 5 for 1287, 1289, 1291, row 4 for 1293.
- No change at White Oasis's panel 4 (to-fix row 21): the Xanx of upstream pool 540076 (radius 15) notice a player within
  33 m of its point (its radius and the 18 m aggro range); panel 4 is 66 m off. A Xanx pulled on the way follows the fight
  up to 60 m from its home and webs from 40 m, so it reaches the panel; the straight walk from panel 3 passes 7.8 m from the
  pool's point. Moving the panel out of that reach (115 m) would put it 50 m off its harvester: the walk by (-425, 470)
  keeps out of the Xanx's notice instead (playtest note above).
- 1356's pumps (set 11's scene, live; to-fix row 23): class 10190 (UsableInertDestBaneRawFuelPumpHouseV01) to 21007
  (V02), the same model bounds, augmentation 41, wreck mesh 26121 on state 2 and explosion 24019 (usabledata); 10190 alone
  loops an intact animation (usabledata.animation (10190, 110, NULL) = 53), and a blown 10190 kept its model in the real
  client although the server sends the destroy path 110 -> 185 -> 186 -> 2; 21007's wreck showed in 1053. ESTIMATE: the
  cause (not checked in the client). The scene row is swapped only where it holds set 11's exact JSON; Down puts it back.
  Evidence row 4 for 1356.
- Set 10's and set 12's creatures' damage immunities (to-fix row 20; no change): the classes' IMMUNE_* flags (World DB
  creature_class_flag, CreatureImmunities) make Sally 22627 take nothing from Fire and Electrical, Molly 10331 from
  Virulent and Laser, Trudy 10317 from Electrical, Wendy 25620 from Fire; the hide creatures alike: Lasher 7477 (set 10's
  pools and set 12's ridge pools 790440 / 790441) Virulent and Laser, Beam Manta 6441 Electrical, Granitour 10164 Fire,
  Ashen Atta Soldier 7081 (and set 2's Mutated Atta Soldier, same class) Fire and Electrical, Ashen Atta Worker 7078 none.
  A hit of such a type shows "absorbs all the damage". The classes are reconstructions (triage D8, MS10 10.4), and the rule
  is to switch to an immune-free class of the same kind: the client has none. Every Creature_Atta_Soldier_* class (7081,
  22627, 25612, 26024, 26025) has Fire and Electrical, both Lasher classes (7477, 10331) Virulent and Laser, both Beam Manta
  classes (6441, 10317) Electrical, every Granitour class (10164, 25610, 25620) Fire; the immune-free Atta classes are
  Harvesters and Queens, another kind. So the classes stay; bring a weapon of another type (a Sonic rifle killed Sally;
  of 1291's rewards only the Sonic Cannon hurts the Mantas and Trudy). Pinned in AshenFixFTests.

## Server data changed by fix job G (20271110001320)

- Set 13's places (live, migration 20271110001300; to-fix row 24): Odesha's pool 790480 (258, 315.49, 325) -> (115, 304.69,
  450), Endala's 790481 (266, 316.46, 337) -> (112, 304.54, 458); God of Storms' resistors 1 (200, 352) -> (158, 312.2,
  541), 2 (158, 362) -> (60, 301, 551), 3 (237, 348) -> (201, 317.56, 525), 7 (246, 309) -> (66, 300.72, 599) (1281's
  scene, its exact JSON) and their markers; 1282's Endala marker on her. Cause: the real client could not climb (99, 482) ->
  (86, 469) on set 13's way from the valley to the Indra Pass waypoint: the leg runs slant up terrain of 52-59 degrees; our
  check measured only the rise along it (0.3-0.7 m a metre) and took floors up to 60 degrees. tabula_rasa.exe reads
  Client.Developer.MaxAngleToClimb, default 55 (not set in client.cfg): the check now fails two climbing samples within a
  metre on a floor over 55 degrees (AshenFixBTests.MaxClimbAngle). No way in to the waypoint's floor was found with it (from
  the valley, from Ashoka; only one through the Bane Conscription Facility's pools). ESTIMATE: the new places. Evidence
  rows 4 for 1280, 1281, 1282.
- The Atta pools (coordinator, real client 2026-11-10 22:46: a level 50 player with the Sonic rifle cannot win three Ashen
  Atta, 8409 health each, about 13000 damage in 20 s from the three, ESTIMATE from the log): set 2's 790046-790051 (Ashen
  Atta Soldiers, Workers, Mutated Atta Soldiers) and fix F's 790378 put down 1, not 3 (ESTIMATE); respawn 120 s, places and
  creatures stay. Evidence rows 4 for 1242, 1243, row 6 for 1287.
- Every UPDATE is guarded on the row's exact stored values (markers within 0.001); Down puts them back.

## Server data changed by fix job H (20271110001510)

- No upstream row changes. New rows: local teleporters 913 "Local Teleporter: Indra Pass valley (fix H)" (121, 305.97, 443)
  and 914 "Local Teleporter: Indra Caverns door (fix H)" (258, 315.49, 325) on Ashen Desert (type 1, class 29648
  ArchHumTeleporterV02), local_teleporter_link 913 -> 914 and 914 -> 913; 1559's objective 1 marker 1000000001 (r4) on 913;
  1559's evidence row 4. Cause: to-fix row 33 (the Indra Caverns door's island has no walk-in: upstream N23, N26; set 15
  could not be started). Inserts guarded; Down deletes exactly these rows.

## Server data changed by fix job L (20271110001920)

- New rows: local teleporters 923 "Local Teleporter: Avernus Outpost prison, the way out (fix L)" (-14, 520.01, -44; class
  7895 UsableTwoStateHumTeleporter) and 924 "Local Teleporter: Avernus Outpost, out of the prison (fix L)" (31, 528.12, -109;
  class 0, an arrival only) on Avernus Outpost (type 1); local_teleporter_link 923 -> 924 (one way); 1473's evidence row 4.
  Cause: to-fix row 46 (the prison is closed in the real client; 1473 could not be turned in without /suicide).
- 1498's scene (set 19's row, our own) gets two public-spawn actors, the upstream pools 531064 and 531066 (not changed), so
  they return on their own respawn time in a squad's copy; 1498's evidence row 4. Cause: to-fix row 47. The scene UPDATE
  only where the row is set 19's exactly; inserts guarded; Down restores set 19's scene and deletes exactly these rows.
- Upstream pool 531066 (the facility's lab chunnel): its centre's height from 103.9 to 105.27, the navmesh's floor straight
  above (it lay 1.37 m under it; as a scene's public spawn it is now a mission placement, MissionGroundTests). x and z kept.
  Guarded on its exact place; Down restores the stored 103.9000015258789.

## Server data changed by set 8 (upstream rows)

- None. Set 8 adds mission content only, on the rows of sets 3, 4, 6 and 7 and the Thrax rows 531008 / 531009.

## Server data changed by set 9 (upstream rows)

- None. Set 9 adds mission content only: its objects and creatures are scene actors on existing rows (the AFS Turret
  550001, the Thrax 531008 / 531009, set 4's Desert Stalker 790121, the client classes 7904 and 9478). The public turret
  pools 550001-550004 are not changed (hidden per player while the run's copies stand).

## Server data changed by set 10 (upstream rows)

- Ida Hayner's pool 510011 moved from (-167.1, 296.27, -709.0) to (-161, 296.57, -710): she stood at the x/z of the
  weapon vendor 500018 (upstream data). Down restores the exact stored value.

## Server data changed by set 11 (upstream rows)

- Lieutenant Dania Fontecilla's pool 510012 moved from (-511.1, 281.76, 164.4) to (-519, 283.71, 164): she stood on the
  White Oasis control point's own spot (control point 17, -511.08, 283.43, 164.35), inside the console a player uses
  (upstream data). Down restores the exact stored value.
