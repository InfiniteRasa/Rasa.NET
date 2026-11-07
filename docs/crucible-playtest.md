# Ligo Crucible missions: playtest list, assumptions and deviations

Missions that are implemented (data, migrations and tests pass) but not yet played through in the
game client. A checked box (`- [x]`) means the playtest is done and the mission works.

Format: `- [ ] <mission id> <name> (<zone>): <what to look at>`.

## Ligo Crucible

Set 1, Outpost Intrepid (migration `20271108000000_Crucible_outpost_intrepid`). Map 1993
(`adv_arieki_ligo_burningsteps`), log category 1 "Battlefield (Crucible)". New NPCs 770000-770002, next free id 770003.

- [ ] 1142 The Last Men Standing (Ligo Crucible): Corporal Shandy (new, 770000, Outpost Intrepid, 987.07, 140, 59.65)
  offers it, no requirement, level 40. The Transfer Orders (template 50212) at the accept. Talk to Captain DeLisi (moved
  to 952, 144.5, 106): he takes the orders. A marker at DeLisi. Turn in at DeLisi: 37500 XP, 3800 credits, one of 2x
  111232, 2x 118907, 4x 111038, 4x 111028. Check the mission log shows it under "Battlefield (Crucible)".
- [ ] 1150 A Fort Called Intrepid (Ligo Crucible): DeLisi offers it after 1142, level 40. The Black Ice program (template
  50297) at the accept. Talk to the data terminal in the ruins of Fort Intrepid (809.74, 175.07, 676.74): it takes the
  program. A marker at the terminal. Turn in at DeLisi: 75000 XP, 5700 credits, one of 39985, 25119, 21100, 31547.
  Abandon: the program goes; taken again, a new one.
- [ ] 1145 The Wire (Ligo Crucible): DeLisi offers it after 1150, level 40. The Audio Surveillance Bug (template 50305)
  at the accept. Five talks, any order: Recon Sergeant Mullins (new, 770001, east mouth of Convict Pass, 292.49, 167.43,
  736.36), Vitto (Larai, moved to -300, 180.5, 320), Donal (Viddia, moved to -216, 195.69, 472), Munit (Top Khana, new,
  770002; since 20271108000500 on the lower corridor under the Top Khana Quarter, -218, 180.68, 228) and Rigs (Labbna, moved to -392.77, 249.46, 276.23). A marker at each. Turn in at DeLisi:
  75000 XP, 5700 credits, one of 33729, 20802, 23569, 30167; the bug goes. Look: can a player walk from the pass into
  Staal and into the Labbna Quarter (the navmesh has these as separate parts, to-fix row 3)?
- [ ] 1143 The Departed (Ligo Crucible): Private Drake (moved to 952.46, 144.5, 110.89) offers it, no requirement, level
  40. Twenty personal "Fort Intrepid Casualty" corpses in the ruins east of the control point (one marker, radius 70,
  round 802.3, 570). Search one: it opens and gives one set of Fort Intrepid Dogtags (50213). "Collect 12 Sets of
  Dogtags" n/12. At 12 all twenty corpses go. Turn in at Drake: 75000 XP, 5700 credits, one of 19802, 34116, 36488,
  24474; he keeps the tags. Abandon: the tags and the corpses go.

Set 2, the Grupa chiefs (migration `20271108000100_Crucible_grupa_chiefs`). New NPCs 770003-770005, next free id 770006.
New local teleporters 909 (lower) and 910 (upper).

- [ ] 1161 The Boss Of All Bosses (Ligo Crucible): DeLisi offers it after 1145, level 40. Step onto the lower teleporter of
  the Kappa Grupa's office (-206.7, 183.4, 317.9, next to the Staal waypoint): it takes you to the office (-214.9, 244.5,
  319.8). Talk to Kappa Grupa Lohen (moved to -215, 244.9, 326). Markers at Lohen and at the lower teleporter. Turn in at
  Lohen: 40000 XP, 3900 credits, one of 3x 111018, 6x 45060, 3x 45446, 1x 111232. Look: the Brann teleporter statics
  show where the pads are; the upper pad takes you back.
- [ ] 1146 The Protection Racket (Ligo Crucible): Vitto offers it after 1161, level 35. Four talks in any order: Bunko
  (new, 770003, -197, 180.7, 319), Merchant Cugin (moved to -174.3, 196.7, 280.2), Merchant Vargo (moved by 20271108000500
  out of the crafting station to -190.5, 180.79, 267.2; by 20271108000800 7.4 m clear of it to -186, 180.78, 270), Merchant Yeg (new, 770004, -153.7, 196.9, 346.9). Bunko, Cugin and Yeg each give their Protection Money.
  Vargo refuses: Roscoe (770005, a Flare Gasher, hostile, 3000 HP) comes out 22 m west of him (-214, 180.7, 258) and attacks. Kill Roscoe, then
  talk to Vargo again: he gives his money. Turn in at Vitto: 75000 XP, the Larai Syndicate Token (50314) and one of
  14444, 14815; he takes the four payments. Abandon: the money and Roscoe go. Look: Roscoe attacks only the player who
  called him.
- [ ] 1148 Smoking Guns (Ligo Crucible): Munit offers it after 1161, level 35. Three Arms Shipments (50358, 50357, 50359)
  at the accept (three free mission-item slots needed). Deliver in any order: Corporal Fiorella (moved to 959, 144.7, 111,
  Outpost Intrepid), Labbna Sigal Haggus (moved to -400, 249.7, 270, Labbna Quarter), Sergeant Melching (not moved, Awol
  Camp, -945.9, 309.6, 246.9). Melching's text: "Who the hell are you?" (variant 203) until Calendar Girl (1144, set 6)
  sets flag 1144001. Turn in at Munit: 75000 XP, the Top Khana Syndicate Token (50316) and one of 19524, 25357. Abandon:
  the shipments go.
- [ ] 1149 Junkyard Dogs (Ligo Crucible): Rigs offers it after 1161, level 40. Kill 20 Flare Gashers (any Flare Gasher
  counts; markers at pools 540139-540141 before the Staal Junkyard bridge, clear of the door). Turn in at Rigs: 75000 XP, the Labbna Syndicate
  Token (50317) and one of 20355, 28634, 33193, 35591.

Set 3, wardens and lashers (migration `20271108000200_Crucible_wardens_and_lashers`). New NPC 770006 (Halprin), new
creatures 770007 (Warden Bot Screw) and 770008 (Lasher, attack 770008), new pools 770006 and 770009-770013, next free id
770014.

- [ ] 1147 Game Wardens (Ligo Crucible): Viddia Grupa Donal (-216, 195.7, 472) offers it after 1161, level 35. Three
  Warden Bot pools (770009-770011, three Warden Bot Screws each, level 35, hostile) south, west and east of the Chaukas Robotics
  Facility door (-528.6, 249.4, 581.6): (-575, 491), (-499, 434), (-606, 534), each circle 70 m or more outside the
  door's circle (fix 20271108000800, to-fix row 44); for each objective a marker on each pool (radius 20; fix
  20271108000300, to-fix row 25). Each bot drops a Power Supply
  (50222) at 50 %, a Neural Processor (50220) at 50 %, a Servo-Motor (50221) at 75 %, until 10/10/15. Turn in at Donal:
  75000 XP, the Viddia Syndicate Token (50315) and one of 19395, 39061; he takes the parts. Abandon: the parts stay. Look:
  can a player walk from the Awol Camp (or the Labbna Quarter) to the door (to-fix rows 15, 21)? The bots must not attack
  a player who comes out of the facility door at once (the pools are 23 m or more outside the link).
- [ ] 1157 Whiplash (Ligo Crucible): Medical Technician Halprin (new, 770006, by the Outpost Intrepid hospital, 989,
  144.9, 108) offers it, no requirement, level 40. Two Lasher pools (770012-770013, three Lashers each, level 40,
  hostile) north-east of the Awol Camp, since fix job F off the lava at (-752, 285.6, 368) and (-780, 286.04, 356) (set 3
  put them on the lava lake at (-785, 284.3, 390) and (-790, 283.9, 420)); a marker at each. Each Lasher drops a
  Lasher Venom Gland (50295) at 60 %, until 10. Turn in at Halprin: 75000 XP, 5700 credits, one of 49140, 15513, 15301,
  15025; he takes the glands. Look: the Lasher looks and attacks right (a new row of class 7477, no creature weapon in its
  look, melee 174/28); its name shows "Lasher".
- [ ] 1170 Earning Respect (Ligo Crucible): Kappa Grupa Lohen (in his office, by teleporter 909) offers it after 1161,
  level 35. Eight objectives, any order: talk to Munit, Vitto, Donal and Rigs; and each Grupa's token mission done
  (Smoking Guns 1148, The Protection Racket 1146, Game Wardens 1147, Junkyard Dogs 1149). A token mission done before
  1170 counts at the accept. Markers at each Grupa (two each). Turn in at Lohen: 160000 XP, 7800 credits, one of 49592,
  15542, 49136, 15295; the tokens stay.
- [ ] 1229 Business Proposal (Ligo Crucible): Lohen offers it after 1170, level 35. Kappa Grupa Lohen's Business Proposal
  (73215) at the accept. Talk to Captain DeLisi (952, 144.5, 106): he takes it. A marker at DeLisi. Turn in at DeLisi:
  40000 XP, 3900 credits, one of 3x 111028, 1x 47599, 3x 111018, 6x 45060. Abandon: the proposal goes.

Set 4, the minefield and the salvage (migration `20271108000400_Crucible_minefield_and_salvage`). New creatures 770014
(Smart Mine) and 770015 (Reconstructor Bot Laborer), their actions 770014-770016, new pools 770014-770018, Derge's pool
520013 moved; next free id 770019. Before it: the marker fix `20271108000300_Crucible_junkyard_dogs_markers`.

- [ ] 1232 Minesweeper (Ligo Crucible): Private Drake (952.46, 144.5, 110.89) offers it after 1150, level 40. Destroy 20
  Smart Mines (770014 only; four in each of three pools in the trench on the road west of the Fort Intrepid waypoint,
  (635, 168.45, 557), (585, 168.43, 577), (545, 168.46, 587); since fix job F point pools, the four within 6.6 m of the
  point); a mine that blows up on the player does not count (fix job F), only one shot down. A marker (r20) on each pool. Turn in at Drake: 75000 XP, 5700 credits, one of 33342, 38823, 25248, 30474. Abandon: the count
  starts again.
- [ ] 1241 Funeral Derge (Ligo Crucible): Captain DeLisi offers it after 1150, level 40. Kill Executioner Derge 520013 on
  the Fort Intrepid rampart (859, 188.68, 476), up the stairs from the Fort Intrepid waypoint. A marker (r15) on him. Turn
  in at DeLisi: 75000 XP, 5700 credits, one of 19673, 33600, 24861, 36188.
- [ ] 1203 Salvage Duty (Ligo Crucible): Labbna Sigal Haggus (-400, 249.68, 270) offers it after 1149, level 35. 6
  Electrostatic Sequencers (50431, 40 %) and 8 Pressure Boosters (50429, 50 %) from Warden Bot Screws (770009-770011) and
  Reconstructor Bots (770017 (-617, 249.68, 570), 770018 (-545, 250.53, 486)) by the Chaukas Robotics Facility. A marker
  (r20) per objective on each of the five bot pools. Turn in at Haggus: 80000 XP, 5850 credits, one of 15470, 15247,
  49136, 14610; he takes the parts. Abandon: the parts stay.
- [ ] 1204 More Salvage Duty (Ligo Crucible): Haggus offers it after 1203, level 35. Objectives 3, 4, 5 (the client's
  ids): 6 Process Controllers (50430, 25 %), 10 Solenoid Valves (50428, 30 %), 10 Vacuum Pumps (50427, 30 %), from the
  same bots. Markers as 1203. Turn in at Haggus: 160000 XP, 7800 credits, one of the modules 123041, 122986, 122961,
  123176; he takes the parts. Abandon: the parts stay.

Set 5, the Staal Sigals (migration `20271108000600_Crucible_staal_sigals`). New NPCs 770019 Viddia Sigal Nema, 770020 Top
Khana Sigal Pico, 770021 Trista; new creatures 770022 (Shield Drone) and 770023 (Preceptor Molitor), the drone's attack
770022; new pools 770024-770029; next free id 770030. Before it: the Staal walks fix `20271108000500_Crucible_staal_walks`.

- [ ] 1202 Architects Of Destruction (Ligo Crucible): Viddia Sigal Nema (-219.5, 195.62, 474), 4 m from Donal by the
  Staal Infirmary waypoint, offers it after 1147, level 35. Destroy 20 Reconstructor Bots (class 6960 or 10314; the bots
  of set 4's pools 770017, 770018 and the new 770027 (-490, 249.72, 480), 770028 (-454, 250.1, 495; moved by 20271108000800, to-fix row 44) by the Chaukas
  Robotics Facility). A Warden Bot does not count. A marker (r20) on each of the four pools. Turn in at Nema: 80000 XP,
  5850 credits, one of 47688, 48104, 15437, 14569. Abandon: the count starts again.
- [ ] 1208 Out Of Sight (Ligo Crucible): Top Khana Sigal Pico (-216, 180.68, 231.5), 4 m from Munit on the lower corridor,
  offers it after 1148, level 35. 6 Shield Drone Optical Processors (50434, 50 %) from the new Shield Drones 770022 (three
  in each pool north of Khulago Base: (415, 170.08, 110), (490, 173.48, 65), (535, 172.92, 80)). A marker (r20) on each
  pool. Turn in at Pico: 80000 XP, 5850 credits, one of 32806, 10637, 20484, 29533; he takes the processors. Abandon: the
  processors stay.
- [ ] 1185 Irreconcilable Differences (Ligo Crucible): Trista (-211.5, 244.89, 325.5), next to Lohen in the Kappa Grupa's
  office, offers it after 1170, level 35. The way in: the north top entrance (-144, 252, 352) and its corridor to the
  office (the tester passed it in the real client, 2026-11-07); pad 909 -> 910 works only when the player steps or jumps
  onto the dais centre (to-fix row 31, upstream C10). The way out: pad 910 -> 909 (works). 1 Speak to Lohen (the
  challenge), 2 get Molitor's Bone Necklace (50360, 100 %, only while 2 is open) from Preceptor Molitor 770023 (with three
  Thrax Soldiers and a Thrax Grenadier, pool 770029 at (-856, 286.83, 371), north-east of the Awol Camp), 3 return to
  Lohen (he takes the necklace). Markers on Lohen (1, 3) and the pool (2, r20). Turn in at Trista: 160000 XP, 3900 credits,
  one of 3x 111048, 3x 45061, 3x 111038, 2x 45062. Abandon: the necklace stays and counts after the challenge again.
  Once objective 2 is complete, Molitor does not drop the necklace again: a player who loses it (sold or destroyed) can only
  abandon the mission and do it again from the challenge (as Plateau Cryptic Messages 557).

Set 6, the AWOL Camp (migration `20271108000700_Crucible_awol_camp`). No new rows (next free id stays 770030). Moved
upstream NPCs: Awol Captain Cheung 510015 to the Awol Camp (-953, 309.56, 255), Awol Lieutenant Deirdre 510016 to the Staal
Centrum (-230, 180.86, 324), Lieutenant Sherer 510030 (969, 144.68, 108) and Intelligence Officer D'Ambrose 510023 (975,
144.68, 110) in Outpost Intrepid. Awol Sergeant Breiburg 510017 stays (-942.2, 309.51, 246.9).

Calendar Girl branches and the flag 1144001 that Melching's Smoking Guns talk (1148, 3) reads:

| Deirdre's choice | D'Ambrose's choice | Flag | Then | Melching's text |
|---|---|---|---|---|
| 1 agree | 1 "dead" (helped) | 2 | Deirdre signs (3), Sherer takes it (4) | 178 |
| 1 agree | 2 "alive" (ratted out) | 1 | Sherer: 1 truth ends it, or 2 lie, then comfort him (9) | 177 |
| 2 threaten | none | 3 | Deirdre signs (8), Sherer takes it (4) | 203 |

The fork: Stray Cats' report to D'Ambrose fails an open The Other Side Of The Coin; the 1153 promise to Cheung fails an
open Stray Cats. 1152 done leads to Making Amends (1155), 1153 done leads to Sweet Revenge (1205). The fork is the
designed one (the client's 1153 log), not the patch-note known issue.

- [ ] 1144 Calendar Girl (Ligo Crucible): Sherer offers it, no requirement, level 40. The Pin-up Girl Calendar (50214) at
  the accept (a full mission bag refuses the accept). 1 Find Deirdre (Staal Centrum): a choice, agree or threaten. Agree
  gives her dog tags (50213; a full bag refuses the talk, nothing changes) and opens 5 Speak to D'Ambrose: a choice, "dead" (he takes the dog tags; opens 3, return to Deirdre) or "alive" (opens 6, the unsigned calendar to
  Sherer: truth or lie; the lie opens 9, comfort Sherer). Threaten opens 8, Deirdre signs. Every path ends at Sherer, who
  takes the calendar. Markers at the NPC of each open objective. Turn in at Sherer: 75000 XP, 5700 credits, one of 49596,
  15189, 48712, 14861. Then check Melching's text at the Awol Camp (Smoking Guns objective 3) against the table. Abandon:
  the calendar goes, the flag stays; taken again, a new calendar.
- [ ] 1152 Stray Cats (Ligo Crucible): D'Ambrose offers it after 1144, level 40. 1 Locate the leader: talk to Cheung at
  the Awol Camp. 2 Report back to D'Ambrose. Markers at Cheung and D'Ambrose. Turn in at D'Ambrose: 75000 XP, 5700
  credits, one of 45796, 15550, 15442, 14699. If 1153 is in the log, it fails a moment after the report. Not offered
  once 1153 is done.
- [ ] 1153 The Other Side Of The Coin (Ligo Crucible): Breiburg offers it only while 1152 is in the log, level 40. 1
  Inform Cheung of your decision: a choice. 1 promise: the mission can be turned in, and 1152 fails a moment later. 2
  refuse: the mission fails at once; clear it and Breiburg offers it again. A marker at Cheung. Turn in at Breiburg:
  75000 XP, 5700 credits, one of 15475, 15588, 48680, 49140.
- [ ] 1155 Making Amends (Ligo Crucible): Breiburg offers it after 1152, level 40. The Top Khana weapons crate (50219) at
  the accept. 1 Munit (Staal lower corridor) takes the crate and gives the ledger (50432). 2 DeLisi (Outpost Intrepid)
  keeps the ledger. 3 tell Breiburg. Markers at Munit, DeLisi and Breiburg. Turn in at Breiburg: 80000 XP, 5850 credits,
  one of 18899, 23828, 32826, 38952. Abandon: crate and ledger go.
- [ ] 1205 Sweet Revenge (Ligo Crucible): as 1155, but offered after 1153 (never with 1155). Turn in at Breiburg: 80000
  XP, 5850 credits, one of 19157, 33084, 40373, 36638.

Set 7, the Staal Junkyard (migration `20271108000900_Crucible_staal_junkyard`). New rows 770030-770038 (next free id
770039). The Staal Junkyard is a squad instance (map 2138): its door is on the Crucible at (-492.1, 236.9, -149.1), at the
end of the bridge road; the arrival in the Junkyard is (29.7, 241.2, -336.9). Cirkin (new, 770030, a Brann, conversation
package 1742) stands in the Junkyard at (187, 240.31, -233).

- [ ] 1206 Heads Will Roll (Ligo Crucible): Sergeant Melching (Awol Camp) offers it after 1153 or 1155 (completed), level
  45. Kill Archfiend Krubb 770031, Coercer Mungrel 770032 and Defiler Sedge 770033 (level 45, 12000 HP each), any order.
  They walk together with a Shield Drone (770022) on a patrol of 7 points in the trench by the rock bridge north-west of
  the Awol Camp, from (-839.5, 285.68, 576.3) to (-764, 276.77, 493) and back; respawn 300 s. One marker r60 for each on
  the rock bridge (-796, 289.9, 528). A kill before the accept does not count. Turn in at Melching: 160000 XP, 7800
  credits, one of 12105, 12143, 12027, 11973. Look: the squad walks the patrol and the drone stays with them.
- [ ] 1519 Careful Negotiation (Ligo Crucible, Staal Junkyard): Captain DeLisi offers it after 1229, level 40 (ESTIMATE).
  1 Kill Kalamit (new, 770034, a Lasher boss, class 10331, level 42, pool r10 at (-486, 249.88, 120) on the Crucible,
  respawn 120 s): Kalamit's Reproductive Sack (111149) drops at 100 %, for the killer only. 2 Meet Cirkin: walk into the
  10 m area round Cirkin in the Junkyard (no talk). Markers: Kalamit's pool r20 (1); the bridge road (-492, 237.88, -115)
  r8 on the Crucible and Cirkin in the Junkyard (2; each shows only on its own map). Turn in at Cirkin: 90000 XP, 6150
  credits, one of 14658, 49172, 15301, 15111; he takes the sack. Abandon: the sack stays.
- [ ] 1529 Your Part of the Bargain (Staal Junkyard): Cirkin offers it in the Junkyard after 1519, level 40. 1 Eliminate
  all Thrax Technicians: the two Technician pools of the south-east (770036 at (330, 243.46, -160), 770037 at (306,
  257.43, -106), three Thrax Technicians each, level 40) dead at the same time. 2 Collect Dolomite: six personal Dolomite
  Blocks (TaRapedia's places: (306, 263.51, -65), (325, 265.45, -22), (225, 242.21, -172), (231, 265.99, -48), (28,
  243.67, -219), (-331, 263.43, -36)); a use gives one block, five complete 2 (n/5). The sixth block stays; its use gives
  nothing. 3 Collect Energy Matrix: a 100 % drop from the Ultra Reconstructor (new, 770038, class 10314, at (164, 265.44,
  -76), the open middle of the Junkyard since fix job H; set 7 had it on the north-centre ledge (-14, 335.14, 244), to-fix
  row 64). 4 Return to Cirkin (opens when 1-3 are done): walk into the 10 m area round him. Markers on each
  pool (r15), each block (r3), the Ultra Reconstructor (r15) and Cirkin. Turn in at Cirkin: 180000 XP, 8200 credits, one
  of 25119, 21400, 36339, 46021; he takes the blocks and the matrix. Abandon: the blocks are put back. Look: the
  Ultra Reconstructor comes back 90 s after a kill in the squad's copy; the Technicians come back only with the copy.
- [ ] 1538 Not My Junk (Ligo Crucible, Staal Junkyard): Awol Captain Cheung (Awol Camp) offers it after 1205 or 1155
  (completed), level 40. 1 Speak to Cirkin in the Junkyard (talk, Cirkin's text 13943). 2 Kill Lamontix (new, 770035, a
  Thrax, level 42, at (350, 265.42, -52), east of Cirkin): the Bane Lockbox Keycard (111182) drops at 100 %. 4 Retrieve
  the Power Converter: use the player's own Bane Lockbox at (343, 263.8, -68); it gives the Plasma Power Converter
  (111181) and takes the keycard. 3 Return to Captain Cheung: walk into the 10 m area round him at the Awol Camp. Markers:
  the bridge road and Cirkin (1), Lamontix (2), the lockbox (4), Cheung (3). Turn in at Cheung: 85000 XP, no credits, the
  Eloh Resonator (117177, fixed); he takes the converter. With a full bag the turn-in still works: the Resonator takes
  the converter's slot. Abandon: start again from the talk. Look: Lamontix comes back 90 s after a kill in the squad's
  copy.

Second client (set 7, the squad's Junkyard copy). Expected results (not yet seen in a client); the bot must cover each:
- A squad mate kills Lamontix or the Ultra Reconstructor: each squad member near with the mission gets the keycard or the
  matrix. A mate's kill of Kalamit on the open Crucible gives the sack to the killer only (to-fix row 58).
- Late join: a squad member who comes into the copy after a kill can kill the boss again after its 90 s.
- Leave and enter again: the mission state, the taken blocks and the opened lockbox stay.
- Instance reset (the copy closes, a new copy): the bosses, the Technicians and the blocks not yet taken stand again.

Set 8a, Prometheus Outpost and the Eloh Crash Site (migration `20271108001100_Crucible_kardash_and_prometheus`). New
NPCs 770039 Professor Tiernan (Crucible, the Eloh Crash Site, (589.5, 154.37, 250), package 1483) and 770040 Captain
Amandla Botha (Ashen Desert 1734, the Ashoka Settlement, (378, 280.69, -458.5), package 1410); next free id 770041.
Major Keplinger 510031 and Larai Zupa Madias 510029 moved out of the Prometheus Outpost control point. Walk from the
Prometheus Outpost waypoint (435, 168.05, 407): the walks from the Fort and Outpost Intrepid waypoints cross lava at
(126, 183.88, 328.8) (upstream, to-fix row 59).

- [ ] 1210 Prometheus Rising (Ligo Crucible): Captain DeLisi (Outpost Intrepid) offers it after 1229, level 35
  (ESTIMATE). 1 Report to Keplinger: talk to him at (379, 168.47, 384), by the Prometheus Outpost control point gate;
  marker r8 on him. Turn in at Keplinger: 42500 XP, 4000 credits, one of Class VIII Res Trauma Kit (47600) x1, Class VIII
  Concussion Grenade (111019) x3, Class VIII Basic Med Pack (45063) x6, Class VIII Fragmentation Grenade (45447) x3.
- [ ] 1212 Confirmed Kills (Ligo Crucible): Larai Zupa Madias offers it at (378.9, 168.3, 367.6) after 1229, level 35. 1
  Kill 15 Bane Soldiers: Thrax Soldier Mark IX 531018 or Thrax Grenadier 531019, counter "Bane Foot Soldiers Killed"
  n/15. Markers r35 in the west trench (320, 165.28, -128.5) and the north trench (388, 166.71, -26) of Khulago Base. A
  kill before the accept does not count. Route: the north trench first, then the west one (the navmesh walk from the
  waypoint straight to the west trench crosses lava at (398.4, 164.1, 238)). Turn in at Madias: 170000 XP, 8000 credits, one of 40482, 46004, 36767, 29994.
- [ ] 1217 Point Of Impact (Ligo Crucible): Awol Captain Cheung (Awol Camp) offers it after 1538, level 40. The accept
  sets the player's own Kardash Eloh Obelisk (class 24889, no use) on the escape pod's deck (575.5, 157.27, 232). 1 Speak
  to Professor Tiernan (marker r8 on him, 23 m north-east of the pod). His talk opens a hidden step: walk up onto the
  pod's deck, into the 6 m area round the obelisk. Then turn in by radio: 42500 XP, 4000 credits, one of Class VIII EMP
  Bomb (111233) x1, Class VIII Cryogenic Grenade (111049) x3, Class VIII Standard Med Pack (45064) x3, Class VIII
  Incendiary Grenade (111039) x3. Look: the obelisk stands on the deck from the accept and goes after the turn-in; the
  radio turn-in shows the obelisk's message.
- [ ] 1218 It Came From Erdas (Ligo Crucible, Ashen Desert): offered by radio when 1217 is turned in, and on each arrival
  on the Crucible while it is not taken; level 35. The accept gives the Eloh Memory Capsule (97496). 1 Tell Tiernan of
  your discovery (talk, marker on Tiernan). 2 Bring the artifact to Botha: talk to Captain Amandla Botha at the Ashoka
  Settlement on Ashen Desert (marker on him, Ashen Desert only), 19 m west of the Ashoka Settlement waypoint. Turn in at
  Botha: 85000 XP, 6000 credits, one of 33987, 24086, 20971, 28940; he takes the capsule. Abandon: the capsule goes; the
  offer comes again on the next arrival on the Crucible.

Fix job G (migration `20271108001200_Crucible_fix_g`, to-fix row 61): Shield Drone pool 770026 (1208) is a point
(radius 0): its drones stand within 6.6 m of (535, 172.92, 80), none on the lava 46 m north of it.

Set 8b, Khulago Base for Larai Zupa Madias (migration `20271108001300_Crucible_prometheus_outpost`). New creatures 770041
Juggernaut (class 7722, level 42) and 770042 Maxxum Ugoretz (name 9317, level 43); attacks 770041, 770042; pools
770041-770044; next free id 770045. Walk to Khulago Base by Confirmed Kills' north trench marker (388, 166.71, -26).

- [ ] To-fix row 62 (look): walk from the Prometheus Outpost waypoint (435, 168.05, 407) to the north trench marker
  (388, 166.71, -26) as the navmesh does. It passes (399, 168.84, 267.3), over a lava lake at y 164.09 (lakes at x
  380-384 and 398-412 on z 267.3). Watch for Fire hits (a navmesh walk over lava, as row 59) or a bridge (no hits).

- [ ] 1213 Iron Giants (Ligo Crucible): Madias offers it after 1212, level 35. 1 Destroy 6 Juggernauts: counter
  "Juggernauts Destroyed" n/6. Two Juggernauts in each of three pools in front of Khulago Base, (383, 174.41, -185),
  (440, 175.5, -190), (310, 175.18, -230), 120 s respawn; a marker r15 on each. A kill before the accept does not count.
  Turn in at Madias: 170000 XP, 8000 credits, one of 47648, 14855, 119370, 14774. Look: the client shows the name
  "Juggernaut" over them (class name, row name 0). If it shows no name or another one, the fallback is name 10757
  "Juggernaut Aggressor" (the Devil's Den row's). No navmesh lava within 80 m of the pools.
- [ ] 1215 Who's In Charge Here? (Ligo Crucible): Madias offers it after 1213, level 35. The accept gives the Viddia
  Syndicate Hacking Program (96429). 1 Hack the Bane Computer: talk to the player's own Bane CommLink Terminal (class
  10519) at (390, 174.28, -236), the mouth of the Bane tunnel piece that holds the client's console; marker r10. The talk
  shows "Viddia hacking program uploading...", takes the program and opens 2; the terminal is swapped for the hacked one
  in the same place. 2 Use the Hacked Computer: talk to it again ("Kuhlago Base Commander: Maxxum Ugoretz."); marker r10.
  Turn in at Madias: 85000 XP, 6000 credits, one of 15582, 14691, 3853, 49136. Abandon: the terminal and the program go;
  taken again, a new program and the first terminal. Look: can a player step into the tunnel to the client's console at
  (360.7, 175.6, -236.2)? (Its floor is a navmesh island 1.4 m above the ground.)
- [ ] 1216 The Big Kahuna (Ligo Crucible): Madias offers it after 1215, level 35. 1 Kill Maxxum Ugoretz: he stands alone
  at (340, 165.12, -130), west of Khulago Base (TaRapedia's /loc), 50 s respawn, no navmesh lava within 80 m; marker r15
  on him. A kill before the
  accept does not count. Turn in at Madias: 85000 XP, 2000 credits, one of 47628, 120030, 47808, 48256.

Set 9, Khulago Base from Prometheus Outpost (migration `20271108001400_Crucible_khulago_strikes`). No new row (next free
id stays 770045); Captain Tiersky 510021 moved off the control point to (379, 168.36, 400), 16 m north of Keplinger.
Walk to Khulago Base by Confirmed Kills' north trench marker (388, 166.71, -26).

- [ ] Tiersky (look): he stands by the CP gate, not inside the control point model; talk to him from each side.
- [ ] 1224 Pump Up The Jam (Ligo Crucible): Tiersky offers it (no prerequisite), level 36. 1-6 Destroy Tower #1-#6: six
  personal Bane towers (class 10282 Sonic Tower, name "Biomechanical Jamming Tower", 1500 HP) on the rim of Khulago Base,
  any order: #1 (433, 173.17, 2), #2 (523, 185.34, -68), #3 (523, 177.01, -208), #4 (363, 164.68, -368), #5 (323, 174.5,
  -258), #6 (233, 178.95, -158); a marker r10 on each. A tower shot down shows its wreck and completes its own objective.
  Turn in at Tiersky: 85000 XP, 6000 credits, one of 15181, 49136, 18260, 14444. Then Keplinger offers 1214. Look: does
  the client show the name "Biomechanical Jamming Tower" (usable name override 4) or "Sonic Tower"? Can the player
  target and shoot each tower, and does it explode into a wreck? The walk to #6 (west) goes round (543 m from the north
  trench marker).
- [ ] 1214 Strike Zone (Ligo Crucible): Keplinger offers it after 1224, level 35. The accept gives three GPS Beacons
  (50714). 1-3 Place Beacon #1-#3: three personal GPS Beacon spots (class 7847) inside Khulago Base, any order: #1 (403,
  174.48, -168), #2 (363, 174.48, -208), #3 (383, 174.22, -268); a marker r8 on each. A use takes one beacon, shows the
  planted beacon and completes the objective. No air strike follows (see Deviations). Turn in at Keplinger: 170000 XP,
  8000 credits, one of 40611, 25099, 21380, 28614. Abandon: the beacons left and the spots go; taken again, three beacons.

Fix job H (migration `20271108001500_Crucible_fix_h`, to-fix row 64): the Ultra Reconstructor's point pool 770038 (1529,
Staal Junkyard) moved from the north-centre rock ledge (-14, 335.14, 244) to the open middle of the Junkyard (164, 265.44,
-76); 1529's marker 9 with it.

- [ ] 1529 (look): walk from the Junkyard arrival and from Cirkin to the Ultra Reconstructor with no GM help: no junk
  slope or ledge stops the walk, and the boss is seen before the walk ends. It still comes back 90 s after a kill.

Set 11a, the Incurables Ward (squad instance 2141; migration `20271108001600_Crucible_incurables_ward`). New rows
770045-770057 (next free id 770058). The Ward's door on the Crucible: map link 97 at (-216, 180.5, 419.7), under the
Staal Infirmary; arrival (18.27, 70.5, -131.31). The server navmesh of the Ward is in islands (upstream, N13): the north
part (Dhimand, Paliv, the Throne Room, the north pools) is reached through the north airlock (78, 62.6, 70.5), where the
client floor goes on and the navmesh has a 1 m gap; the bot cannot path across it (walk it by hand).

- [ ] Ward (look): Sheira (moved 12 m north of the medic Siebring, (-18, 62.61, -88)), Shanul (60, 62.61, -70) in the
  dormitory and Dhimand (-40, 94.61, 386) between the Throne Room and the Sanctum hub; talk to each from each side. Walk
  through the north airlock: does the client pass it with no jump?
- [ ] 1445 Sadistic Sabotage (Ligo Crucible -> Incurables Ward): Viddia Sigal Nema (Staal Infirmary) offers it after
  1147 Game Wardens, level 35. 1 Report to Supervisor Sheira: her talk (package 1640) in the squad's Ward completes it.
  Markers: before the Ward's door on the Crucible (r6) and on Sheira (r8). Turn in at Sheira: 42500 XP, 4000 credits,
  one of 45063 x6, 45447 x3, 111233 x1, 111049 x3.
- [ ] 1444 Lost in the Asylum (Incurables Ward): Sheira offers it after 1445, level 40. 1 Find Mechanic Shanul (talk,
  package 1664), 2 Find Technician Paliv (a personal talking Fresh Brann Corpse, class 25474, package 1727, at (94,
  78.5, 421) by the north door display), 3 Find Engineer Dhimand (talk, package 1700); all open at once, any order; a
  marker r8 on each. Turn in at Sheira: 170000 XP, 8000 credits, one of 34116, 40373, 21249, 23569. Abandon: all three
  again. Look: does the client draw the body and open its text on a use?
- [ ] 1402 Rehabilitation at Gunpoint (Ligo Crucible -> Incurables Ward): Larai Sigal Tempa (moved to TaRapedia's place
  by the Larai Arena, (-307, 180.79, 326)) offers it (no prerequisite), level 35. 1 Kill Jaikev (point pool, (-98, 94.61,
  332)), 2 Kill Parvatee (point pool, (-90, 94.73, 340)), both in the Throne Room, back 90 s after a kill in the copy; 3
  Execute 30 Incurables: the Brutes (class 23191) and Slayers (25611) of nine pools of four (two of each, radius 5, 770053 and 770055 points, 36 in
  all; back only with the copy); the bosses do not count. Markers: the bosses (r10), each pool (r15), before the Ward's
  door on the Crucible (r6, each objective). Turn in at Tempa: 180000 XP, 8200 credits, one of 122971, 123046, 123041,
  123276. Abandon: the count starts at 0. Look: the Incurables' look and attack (weapon 174/51); Jaikev's and Parvatee's
  look. Solo: the 36 Incurables are 6 over the 30; a cleared pool comes back only with the copy (45 min,
  SquadInstanceConfig.RespawnMinutes), so a player who leaves the count short after clearing all nine waits up to 45 min.
- [ ] Second client (bot checklist): a squad mate in the same Ward copy kills Jaikev, Parvatee or Incurables: counted for
  each member near with 1402? Each member sees his own Paliv body (1444).

Set 12a, the Chaukas Robotics Facility (squad instance 2146; migration `20271108001700_Crucible_chaukas`). New rows
770058-770059 (next free id 770060). The facility's door on the Crucible: map link 98 at (-528.6, 249.44, 581.64), north
of the Awol Camp; arrival (-31.91, 17.23, -282.11). The east (Robotics Assembly) room's inside is a server navmesh island
apart from the arrival's (upstream, N14): only its north and south entrance strips are joined; the client floor goes on.
Set 12b (1523 Factory Fortification, in the east room; 1525 Information is Key, no objectives) is not built.

- [ ] Chaukas (look): Brian (-38, 17.44, -256), 28 m from the arrival; he keeps his post (pose Standing). Ruben on the
  Crucible (966, 152.73, 156), by Outpost Intrepid's Front Lines exit. Talk to each from each side.
- [ ] 1577 Get to Chaukas (Ligo Crucible -> Chaukas): Information Analyst Ruben offers it (no prerequisite), level 40. 1 Get
  to Chaukas: Brian's talk (package 1800) in the squad's copy completes it. Markers: before the Chaukas door on the
  Crucible (r6) and on Brian (r8). Turn in at Brian: 90000 XP, 8200 credits, one of 33858, 24086, 21699, 31241.
- [ ] 1521 Control the Brain (Chaukas): Brian offers it after 1577, level 35. At the accept his copy (yours, the public
  Brian hidden from you) stands at his place and follows you 3 s later. 3 Escort Brian: walk to the Brain room's mouth (-14,
  16.36, 4) (r8 area; the area checks you, not Brian). 8 Clear the Brain Room: the three Claviger pools 531127-531129 dead
  at once (a room cleared before the accept counts). 3 and 8 in either order; then Brian stops following and walks to the
  Brain (-24, 16.04, 38). 7 Talk to Brian (package 1800). 4 Find the Spare Parts: your own Spare Parts Container (class
  26580, name 189) on the lower level (-16, 1.51, -164); one use. 5 Take the Parts to Brian (talk, "Quick Give me the
  part."): three Warden Bot Clavigers (531023) come at Brian from the room's north side. 6 Keep Brian Safe: the three dead
  (a hidden tally counts them; 6 shows no count).
  Brian cannot die. Turn in at Brian (the copy): 90000 XP, 8200 credits, one of 47688, 48676, 15437, 14815. Abandon: the
  copy goes, the public Brian is back; all again. Look: does Brian keep up on the ramps and in the corridor, and walk into
  the room to the Brain? Does the client draw the container and open it on a use? Solo: the Clavigers are level 43, 10905
  HP (balance out of scope).
- [ ] 1522 Fix the Factory (Chaukas): Brian (public, at the entrance) offers it after 1521, level 35. 1 Restart the
  Machine: your own Factory Reset Switch (class 26578, name 190) at the east room's south entrance (88, 17.36, -176). 2 Turn
  on the Ore Processor: your own Ore Machine Override Switch (name 191) on the north side of the west Ore Processing room's
  ring (-146, 16.68, -55); it takes no use before 1. Markers on each switch (r3). Turn in at Brian: 85000 XP, 8000 credits, one of 122951,
  122986, 123001, 123006. Look: the switches' look and their thrown state.
- [ ] 1521 (look): leave the Chaukas copy for more than 5 min during 1521 (after Brian walked to the Brain), then return.
  The copy may be made again: is Brian back at the Brain, or at the entrance while the markers point at the Brain (a squad
  copy refuses RestorePosition)? Can the mission still be done?
- [ ] Chaukas pools: a cleared Claviger pool comes back only with the copy (45 min, SquadInstanceConfig.RespawnMinutes):
  a player who abandons 1521 after clearing the Brain room keeps the room cleared (counted again at the accept).
- [ ] Second client (bot checklist): a squad mate in the same copy sees his own Brian copy only with his own 1521 (the
  other's copy hidden from him); the room cleared by either counts for both (8); each has his own container and switches.

## Assumptions and choices (not from a source)

All ESTIMATE (set 1):
- Levels: 1150 and 1145 are level 40 (no source; the level of 1142 and 1143). Mullins level 40, Munit level 40.
- All positions: Shandy, Mullins, Munit, the five moved NPCs, the terminal and the twenty corpses. Chosen by a probe
  (2026-11-07): on the navmesh and on a client floor, seen from a client floor 2 m round, a complete path from the
  outpost (or the Staal or Labbna waypoint), at least 20 m outside every hostile pool. The terminal is 13 m outside
  Fithik pool 531113.
- Looks: Shandy is a copy of DeLisi 510019, Mullins of Sherer 510030, Munit of Vitto 510027. The new NPCs have 4500 HP and
  armour 300, and stand still.
- Rewards: the client has no reward blocks for these missions. 1142: the stack sizes 2/2/4/4. The armour of 1143, 1145
  and 1150: the lowest Uncommon template of each kind and icon number in the level 40 band. Where that icon number has
  no Uncommon template, the nearest one: Mech V05 gloves -> V04 34116, Reflective V05 -> V03 36488, Graviton V01 boots ->
  V02 23569.
- 1143: twenty personal corpses (the client's twenty hidden objectives 2-21 are not written; hidden objective 22 counts
  twelve distinct corpses). Corpse classes 24995, 7865, 24989, 24990 (human male corpses, as the Caves of Donn's fallen
  soldier), name 12 "Fort Intrepid Casualty".
- 1150: the terminal class 11143 and its talk (package 1245).
- 1145: the five talks are open at once, in any order (objective 6 is first in the log). Receiver DeLisi (the turn-in
  text; TaRapedia names no reward giver). Nothing fails the mission ("don't get caught wearing a wire" has no client
  objective).

All ESTIMATE (set 2):
- Levels: 1161 level 40 (no source; the level of 1145). 1146 35, 1148 35 and 1149 40 are from the TR MMO DB list. Bunko,
  Yeg and Roscoe level 35.
- All positions: Bunko, Yeg, Roscoe's spawn point, Lohen, Cugin, Fiorella, Haggus, and the two teleporters. Chosen by a
  probe (2026-11-07): on the navmesh and a client floor, at least 2 m from a wall, nothing over the head, seen from 16/16
  places 2 m round, a complete path from the area waypoint (Lohen: from the upper teleporter), 200 m or more from a
  hostile pool, 57 m or more from a map link. The teleporters stand at the origins of the client's Brann statics.
- The office teleporters 909/910 (type 1, class 0, linked both ways): TaRapedia names a local teleporter by the Staal
  waypoint, and the client has two Brann statics and two map markers there. The upstream DB has no rows for them.
- Looks and stats: Bunko and Yeg are copies of Vargo 510033 (class 7776), 4500 HP, armour 300, stand still. Roscoe is a
  copy of the Crucible Flaregasher 540030 (class 7338), 3000 HP, armour 150 (TaRapedia: "an easy fight").
- Rewards: no client reward blocks. The token of each mission is a fixed reward. The items: the lowest Uncommon template
  of each kind in the band of the mission level (1146: 14444, 14815; 1148: 19524, 25357; 1149: 20355, 28634, 33193,
  35591). 1161: TaRapedia's four items as one to choose, in TaRapedia's stacks.
- 1161: only the client's variant 1 talk (variant 159 has the same text and no known flag).
- 1146: receiver Vitto. The four talks open at once, in any order. Vargo's first talk calls Roscoe; Roscoe's death opens
  Vargo's second talk. "Help from the surrounding guards" (TaRapedia) is not built. The four Protection Guaranty items
  (24864-24868) are not used.
- 1148: receiver Munit. The three shipments at the accept, delivered in any order. Melching's variant by flag 1144001:
  1 "ratted out" (177), 2 "helped" (178), other values or unset "neither" (203). Set 6 (1144) must set this flag.
- 1149: any Flare Gasher counts (creature flag 85, as Targets of Opportunity 1589). Markers at the three pools before the
  Staal Junkyard bridge. Fix 20271108000300 (to-fix rows 23, 24): upstream pool 540139 stood on the junkyard door (map
  link 96) and 540140, 540141 off the navmesh; the three pools and markers are now on the plateau north of the bridge
  (-452, 249.56, 58), (-518, 250.21, 64), (-380, 248.05, 46), 190 m or more from the door (ESTIMATE, navmesh probe).
  Upstream pool 540144 moved off the Thunderhead border link 94 to (-841, 285.48, 674).
  Roscoe (1146, class 7338, flag 85) is a Flare Gasher too: with 1149 open, his kill counts 1 of the 20.
- Melching is not moved (his upstream place passes the probe). Vargo was not moved in set 2; the fix 20271108000500
  (to-fix row 34) moves him out of the Staal Centrum crafting station (kraftwerk 22), which hid him from the client.
- Fix 20271108000500 (to-fix rows 32, 35; ESTIMATE, navmesh probe): Munit at (-218, 180.68, 228), on the lower corridor
  23 m from his quarter's region point, because the only way up to the region point crosses a floor hole; Yeg's marker
  r5 (was r8), clear of the second hole. A player who walks to Yeg from the stairs at (-177, 196, 355) passes the hole
  near (-176..-161, 196.7, 357..345): the walk from Cugin's counter keeps clear of it.

All ESTIMATE (set 3):
- Levels: 1147 35, 1157 40 and 1170 35 are from the TR MMO DB; 1229 35 (no source; the level of 1170). Halprin level 38
  (TaRapedia).
- All positions: Halprin and the five pools. Chosen by a probe (2026-11-07): on the navmesh and a client floor, 2 m (pools
  3 m) or more from a wall, nothing over the head, seen from 16/16 places 2 m round (Halprin and four pools; one pool
  15/16), a complete path from the Outpost Intrepid waypoint (Halprin), the Labbna Quarter and Awol Camp waypoints (the
  Warden Bots) and the Awol Camp waypoint (the Lashers); the pools 120 m or more from every friendly NPC. Halprin stands
  6 m from TaRapedia's place (995, 144, 108), 3 m from the hospital spawn point.
- 1147: "the Warden Bot Facility" read as the Chaukas Robotics Facility's door (no source names a facility of that name on
  the Crucible). Receiver Donal.
- 1157: the Lasher nest place: north-east of the Awol Camp, between the Seeds logos and Molitor's road (TaRapedia Defiler
  Sedge: "possibly fighting the lashers by there", 0.3). Receiver Halprin.
- The new creatures: the Warden Bot a copy of the Jailor 531044 (class 6961, its laser and shock attacks and its death
  blast 60002) one rank down, "Warden
  Bot Screw" (8045), level 35, 4970 HP, armour 247. The Lasher a new row of class 7477, no name (the client shows the class
  name), level 40, 8470 HP, armour 285, one melee attack 174/28 (the client's Weapon_Creature_Lasher), reach 0.5-4 m,
  2100 ms (client), 557-779 damage, Virulent (damage_type 0: the client's weapon table). Three per pool, 120 s respawn, radius 6. Halprin a copy of DeLisi 510019
  (class 3846), 4500 HP, armour 300, server-only package 770006.
- Drop chances: 1147 power supply 50 %, neural processor 50 %, servo-motor 75 %; 1157 venom gland 60 %. Personal drops,
  from any of the kind on the Crucible while the objective is open.
- Rewards: no client reward blocks. The token of 1147 is a fixed reward. The items: the lowest Uncommon template of each
  kind and icon number in the band of the mission level (1147: 19395, 39061; 1157: 49140, 15513, 15301, 15025 with "RPG
  Launcher" = the physical rocket launcher and "Pulse" = EMP; 1170: 49592, 15542, 49136, 15295). 1229: TaRapedia's four
  items as one to choose, in TaRapedia's stacks.
- 1170: receiver Lohen. The eight objectives open at once, in any order. Each token objective completes when that Grupa's
  mission is completed (also before 1170). The tokens stay with the player.
- 1229: the proposal at the accept, taken by DeLisi at the talk.

All ESTIMATE (set 4):
- Levels: 1232 40 (TR MMO DB); 1241 40 (no source; the level of 1232 and 1150); 1203 and 1204 35 (no source; the level
  of 1147 and of the bots).
- All positions: the three mine pools, the two Reconstructor pools and Derge's place. Chosen by a probe (2026-11-07): on
  the navmesh and a client floor, a complete path from the Fort Intrepid waypoint (mines, Derge) or the Labbna Quarter
  waypoint (Reconstructors). The mines in the trench on the road to Staal (client: "near the trenches due west of the Fort
  Intrepid ruins", "no way to reach Staal without passing through the area"). Derge at the wall spawn (IGN / patch 1.4,
  859 188 476), on the rampart (TaRapedia, Ellatha: up the stairs). The Reconstructors 83 m or more from the facility door.
- The Smart Mine: a copy of the Necromite 570003 (class 7528, name 433 "Necromite"; TaRapedia: Smart Mine = Necromite),
  level 40, 1500 HP, armour 150, four per pool, 120 s respawn, radius 6. Its weapon 1/109 80-120 and its self-destruct
  489 150-300 (TaRapedia "very little damage"; the Necromite has 320-480 and 733-1448; user review), damage_type 0.
- The Reconstructor Bot: a new row of class 6960, "Reconstructor Bot Laborer" (8033, the Screw's rank), level 35, 4970
  HP, armour 247 (the Screw's), one attack 1/152 (client Weapon_Creature_Arieki_Bot_Reconstructor), 1-40 m, 1500 ms
  (client), 77-115 damage, damage_type 0. Three per pool.
- Drop chances: 1203 40 / 50 %; 1204 25 / 30 / 30 % (client: "the most common", "rarer"; no rate). Personal drops from
  a Warden Bot Screw or a Reconstructor Bot on the Crucible while the objective is open.
- 1204 counts 6 / 10 / 10 from the client objective names (no client counter text; TaRapedia's counters say x/8, 0.3).
- Rewards: no client reward blocks. The items: the lowest Uncommon template of each kind and icon number in the band of the
  mission level (1232: 33342, 38823, 25248, 30474; 1241: 19673, 33600, 24861, 36188; 1203: 15470, 15247, 49136, 14610
  with "Incendiary RPG Launcher" = the physical rocket launcher (no fire one exists) and the rifle of no kind = physical).
  1204: "Modification" = the grade [4] modules: Resist Photonic (Light), Regen Bonus, Armor Recharge (Regen Armor), Debuff
  Fire Resist.

All ESTIMATE (set 5):
- Levels: 1202 and 1185 35 (TR MMO DB); 1208 35 (no source; the level of 1148 and 1202, which pays the same). Nema and Pico
  level 35, Trista 37 (TaRapedia).
- All positions: Nema, Pico, Trista, the three drone pools, the two Reconstructor pools and Molitor's pool. Chosen by a probe
  (2026-11-07): on the navmesh and a client floor; the NPCs 2 m or more from a wall, nothing over the head, seen from 16/16
  places 2 m round, 4 m from their Grupa (Nema by Donal, Pico by Munit, Trista by Lohen: TaRapedia "standing next to Lohen"),
  walked to from the lower office teleporter 909 and their Grupa (Nema, Pico) or from the upper teleporter 910 (Trista)
  with no walk over a Staal floor hole; the pools walked to from the Prometheus Outpost (drones), Labbna Quarter
  (Reconstructors) and Awol Camp (Molitor) waypoints, 150 m or more from every friendly NPC. Molitor at the nearest
  navmesh point 11 m from TaRapedia's x and z (-846, 376.4); TaRapedia's y 266.2 has no floor (the road is at y 286.8).
  The drones on the approach north of Khulago Base (no source places them; the client says "Bane Shield Drones").
- Looks and stats: Nema is a copy of Donal 510037, Pico of Vitto 510027, Trista of Lohen 510024 (class 7776). Trista is
  female on TaRapedia, but the server has no row of the female Brann class 7775 to copy (to-fix row 39). 4500 HP, armour
  300, stand still, server-only packages.
- The Shield Drone: a new row of class 7233 (client name 10766 "Shield Drone Aggressor"), level 35, 5473 HP, armour 248
  (between the Marshes and Plains drones), the drone attack 396/1 at 120-358, damage_type 0, and the drones' heal aura 45.
  Three per pool, 120 s respawn, radius 6.
- Preceptor Molitor: a copy of Executioner Derge 520013 (class 10504, his six attacks), level 43, 10905 HP, armour 308 (the
  Crucible's level 43 Thrax row 531020). His pack: three Thrax Soldiers 531018 and one Thrax Grenadier 531019 in his pool
  (radius 10, 50 s respawn, Derge's). He stands; his patrol on the road is not built.
- 1202: any Reconstructor class counts (6960 and its boss 10314, as Targets of Opportunity 1589 objective 71).
- Drop chances: 1208 50 % (no source); 1185 100 % (TaRapedia: "He drops Molitor's Bone Necklace").
- 1185: 1 opens 2, 2 opens 3; Lohen takes the necklace at 3 (client 9118); Trista pays (client 9065, TaRapedia).
- Rewards: no client reward blocks. 1202 and 1208: the lowest Uncommon template of each kind and icon number in the band of
  level 35 (1202: "Filament Blade" of no kind = the physical blade 47688, Cryogenic Injector Gun = ice injection gun 48104,
  Pulse Cannon = EMP rocket launcher 15437, Pulse Pistol = EMP pistol 14569; 1208: 32806, 10637, 20484, 29533). 1185:
  TaRapedia's four stacks by their client names.

All ESTIMATE (set 6):
- Positions: Cheung, Deirdre, Sherer and D'Ambrose, chosen by a probe (2026-11-07): on the navmesh and a client floor, 2 m
  or more from a wall, seen from 16/16 places 2 m round, walked to from their area's waypoint (Deirdre also from the lower
  office teleporter 909, with no walk over a Staal floor hole), 141 m or more from hostile pools, 44 m or more from map
  links. Cheung is 10.8 m from Melching.
- 1144: the flag is set at the deed (D'Ambrose's or Deirdre's choice), not at the turn-in; an abandon keeps it. Objective
  7 is a hidden required outcome; the other objectives are optional. The truth to Sherer ends it at once; the lie needs
  the comfort talk (9).
- 1144: the dog tags (class 24589) come at Deirdre's "agree"; D'Ambrose takes them at "dead"; at "alive" the player keeps
  them until the mission ends (then removed).
- 1153: Breiburg is the receiver (TaRapedia's "debriefing"; the client names none).
- 1155 and 1205: the crate is item class 24600, Munit gives the ledger at the same talk, DeLisi keeps it.
- Rewards: no client reward blocks. The lowest Uncommon template of each named kind in the band of level 40 ("Endothermic"
  = Ice, "Exothermic" and "Incendiary" = Fire, "Pulse Cannon" = EMP rocket launcher, "Incendiary RPG Launcher" = the
  physical rocket launcher, Stealth V06 read as V04).

All ESTIMATE (set 7):
- Stats: no source gives the levels of the new NPCs, their HP or armor. Officers level 45 (the mission level), Kalamit,
  Lamontix and the Ultra Reconstructor level 42, Technicians level 40 (copy of 531020, 8409 HP, armor 285).
- Positions: Cirkin 11 m from the TR MMO DB spot (to-fix row 53); Lamontix and the lockbox at TaRapedia's (339.1, -70.5)
  on the navmesh; the Ultra Reconstructor "near the upper centre" (TaRapedia); Kalamit "near the Junkyard" (client) on
  the Crucible; the officers' patrol on TaRapedia's three points.
- 1206: offered after 1153 or 1155 completed (TaRapedia names 1205, 1153, 1155 as before it).
- 1519: level 40 (no source; as 1529). The sack drops only while 1 is open.
- 1529: objective 1 needs both Technician pools dead at the same time; the block places are on the navmesh's height.
- 1538: the lockbox is the item crate 11116 15 m west of Lamontix; the keycard is the gate (the patch of 2008-01-31).
- Rewards: no client reward blocks. 1206 and 1519: TaRapedia's four kinds (to-fix row 54); each the lowest Uncommon
  template whose band holds the mission level (1206 level 45: 12105, 12143, 12027, 11973; 1519 level 40: 14658, 49172,
  15301, 15111). 1529: the lowest Uncommon template of each kind and icon number in the band of level 40 (Graviton V04,
  Hazmat V03, Reflective V03, Bio V04: no Uncommon V05 or V04 of these in the band).

All ESTIMATE (set 8a):
- Stats and looks: Tiernan a copy of Keplinger's row and look, Botha a copy of Captain Flynn Harris's (no source gives
  Botha's sex), both level 40, 4500 HP, armor 300. 1210 level 35 (no source; 1229's).
- Positions: Tiernan on the crater floor 23 m from the escape pod; Botha 19 m west of the Ashoka Settlement waypoint;
  Keplinger by the CP gate (no source), Madias at TaRapedia's /loc on the navmesh height; the obelisk on the pod's deck.
- 1212: 170000 XP (TaRapedia's 17000 read as a typing error); the two markers on the west and north trench pools.
- 1217: the walk up to the obelisk is a hidden step after Tiernan's talk, then a radio turn-in (see Deviations).
- 1218: the capsule at the accept ("Take the capsule"); offered by radio at 1217's turn-in and on arrival on the Crucible.
- Rewards: 1210 and 1217 TaRapedia's items by their client names (EMP Bomb: the first of two identical templates, 111233;
  "Frag Grenade" read as Class VIII Fragmentation Grenade 45447). 1212 and 1218: TaRapedia's kinds and icon numbers, the
  lowest Uncommon template whose band holds the level (1218: Uncommon, not TaRapedia's purple with mods).

All ESTIMATE (set 8b):
- The Juggernaut: a new row of class 7722, level 42 (Khulago's Thrax), name 0 (the client's ranked Juggernaut names have
  no level band in any source), 16700 HP and armor 300, and the Devil's Den Juggernaut's two attacks (1/180 at 480-720,
  428/1 at 1080-1540): the Devil's Den Juggernaut to its Thrax as this one to the Crucible's Thrax 531018.
- Ugoretz: a copy of Executioner Derge 520013 (class 10504, his six attacks), level 43, 10905 HP, armor 308 (as Molitor).
- Positions: the three Juggernaut pools on the open ground in front of Khulago Base (no source gives them); Ugoretz at
  TaRapedia's /loc on the navmesh, standing; the terminal at the tunnel mouth, not at the client's console.
- 1215: the terminal is two actors (the first for 1, the hacked one for 2); the program at the accept.
- 1216: 2000 credits (TaRapedia; TR MMO DB says 0).
- Rewards: the lowest template of each kind whose Uncommon band holds level 35. 1215's Titan Cipher Tool: 3853
  (Tool_Avatar_Cipher_35, quality 2: no Uncommon cipher tool exists). The RPG launcher (1215) and the Leech Gun (1216):
  the names give no damage kind, physical taken.

All ESTIMATE (set 9):
- Tiersky: by the CP gate, 16 m north of Keplinger (no source gives his place).
- 1224: six towers (the client's six objectives, not the texts' eight), all open from the accept, any order; no
  prerequisite (TaRapedia "Unknown"). The tower: class 10282 UsableInertDestBaneSonicTowerV01 (no jamming tower class on
  the Crucible), the usable name override 4 "Biomechanical Jamming Tower" (key GUARDPROM_JAMMINGTOWER, another zone's),
  1500 HP (the server's Large size of this class). The six places on the rim of Khulago Base (no source gives them).
- 1214: the three spots inside the base (no source gives Frohman's spots), class 7847 as Mapping The Pools; the beacons at
  the accept.
- Rewards: 1224 the lowest template of each kind whose Uncommon band holds level 36 (Teleract Rocket Launcher: physical,
  the name gives no kind). 1214 the lowest Uncommon of each kind and the nearest icon number in the band of level 35
  (Stealth V04 Vest, Graviton V04 Helmet, Hazmat V03 Helmet, Motor Assist V04 Boots).

All ESTIMATE (fix job H):
- The Ultra Reconstructor in the open middle of the Junkyard (164, 265.44, -76), not TaRapedia's "upper centre": the client
  cannot walk to the north-centre ledge (to-fix row 64).

All ESTIMATE (set 11a):
- Stats: no source gives the levels or HP. Shanul and Dhimand copies of Lohen (level 40, 4500 HP, armor 300); the
  Incurables level 40, 8409 HP, armor 285 (as the Junkyard's Technicians); Jaikev and Parvatee level 42, 12000 HP, armor
  300; one attack for all four (weapon 23198's 174/51, the Lasher's 557-779 damage, 2.1 s, damage_type 0).
- The Incurables: Brute (male class, name 9521) and Slayer (female class, name 9617) only; the client has more names
  (9520-9524, 9607, 9609, 9616) and TaRapedia names flamethrowers and bots. Nine pools of four (radius 5; 770053 and 770055 points: their radius-5 draw reached 20-40 m off), 90 s respawn
  (back with the copy); places chosen by probe (north corridor, junction room, Sanctum hub, north room, Throne Room).
- Positions: Sheira 12 m north of her upstream spot; Shanul in the dormitory; Dhimand in the hallway by the Sanctum (not
  behind the Sanctum door); Paliv by the north door display; Jaikev and Parvatee in the Throne Room; the door marker 8 m
  outside link 97's circle; Tempa at TaRapedia's /loc (-307, 180, 326).
- 1444: level 40 (no source); all three objectives open at once.
- 1402: no prerequisite (none known); the turn-in at Tempa on the Crucible.
- Rewards: 1445 TaRapedia's stacks by their client names, one to choose (as 1161); 1444 the lowest Uncommon of each kind
  and the nearest icon number in the band of level 40 (Mech V04 Gloves, Stealth V04 Helmet, Hazmat V03 Gloves, Graviton
  V02 Boots); 1402 "Modification" read as the grade [4] modules (as 1204): Health Bonus, Resist Physical, Resist Photonic
  (Light), Steal Power.

All ESTIMATE (set 12a):
- Stats: Ruben (copy of Sherer, female) and Brian (copy of DeLisi) level 40, 4500 HP, armor 300. Brian run 7, walk 2 (as
  the Plateau escorts) so his 1521 copy can follow; his public pool posed Standing so he does not stroll.
- Places: Ruben 4 m off TaRapedia's spot (seen from every side there); Brian 28 m in from the arrival; the Brain room's
  mouth, Brian's place at the Brain, the container on the lower level, the switches at the east room's south entrance and
  on the Ore room's ring, the three attackers on the Brain room's north side: chosen by probe, the container and the
  switches 20 m or more from every Claviger pool's spawn reach. The door marker 18 m south of link 98.
- Classes: the client names the container (189) and the switches (190, 191) but ties no class to them: 26580 (Warden Bot
  crate) and 26578 (Warden Bot generic switch) are ours; 25897 UsableTwoStateWardenbotOreMachineV01 is a candidate for the
  ore switch (not usable as the client has it: target_flag 0).
- 1521 timing: Brian follows 3 s after the accept, walks to the Brain at 3 m/s; the escort area is 8 m round the mouth.
- Levels: 1577 40 (MMODB row), 1521 and 1522 35 (MMODB; a player says 38-40, 0.3).
- 1521: the flow (3 and 8 at once, then 7, 4, 5, 6) and the three attackers (count and kind: the facility's own
  Claviger); no spare-parts item (the client has none); Brian cannot die.
- 1522: the switches thrown in turn (1 opens 2), no timer.
- Rewards: 1577 the lowest Uncommon of each kind and the nearest icon number in the band of level 40 (Mech V03 Vest,
  Graviton V02 Vest, Hazmat V03 Vest, MotorAssist V07 Helmet); 1521 the lowest Uncommon of each kind in the band of level 35
  (Blade Physical, Polarity Gun Fire, Rocket Launcher EMP, Shotgun Physical); 1522 the grade [4] modules (Armor
  Absorption, Regen Bonus, Health Regen Bonus, Power Regen Bonus).

## Deviations (client/server limits)

- 1521: the escort checks the player at the Brain room's mouth, not Brian (Brian follows); Brian cannot die, so "Keep
  Brian Safe" is the three Clavigers killed; the spare parts are no item.

- 1444: Shanul's text sends the player for the keycards of Atali and Umank (1401, set 11b, not built); Paliv and Dhimand
  are reached without them, and no Sanctum door stands in the way.

- 1143: one counter of distinct corpses does the work of the client's twenty per-corpse objectives.
- 1145: no failure for being caught with the wire.
- 1148: a player who did Calendar Girl (1144) before set 6 has no flag and hears Melching's "neither" text (203).
- 1170: a token objective reads the Grupa's mission as completed, not the token in the bag (to-fix row 20).
- 1232: the Smart Mines lie buried until a player is about 8 m off (ESTIMATE, no source gives the range), then come up
  with their own birth (155/7528) and fight (MawLurk, as the Maws). They go down again with the Maw's despawn (424/1), as
  the client has no despawn for them: check in the real client that a mine shows when it comes up and is hidden when it
  goes down.
- 1232: the mines are named "Necromite" (name 433): the client has no Smart Mine creature name or class (fix job F).
- 1232: a mine that goes off by itself is nobody's kill (no count, experience or loot), as the client text says it must
  be shot first (fix job F).
- 1241: Derge stands on the rampart; his patrol is not built.
- 1203, 1204: the Reconstructor Bot only shoots: no heal, no shield drain, no micromech clone (TaRapedia).
- 1208: the processors drop only from the Crucible's Shield Drones. A mission drop names one map (MissionItemDropDefinition),
  so the drones of other maps (TaRapedia user note: "I did it on Mires") give none.
- 1185: Molitor and his pack stand in one pool; his patrol on the road is not built.
- 1519, 1529, 1538: "Meet Cirkin", "Return to Cirkin" and "Return to Captain Cheung" have no client talk: built as a walk
  into a 10 m area round the NPC. The area fires only on a step into it: a player who already stands in it when the
  objective opens must step out and in again (to-fix row 57).
- 1529: the client's tracker objective 5 with three counters is hidden; the block count is on hidden objective 7 (to-fix
  row 56).
- 1538: the keycard does not gate the lockbox by a requirement, only by the objective order. If the keycard is gone (sold
  or destroyed) after 2, the lockbox use is refused; abandon and accept again (to-fix row 55).
- 1519: the sack drops for the killer only (open Crucible, upstream loot rule; to-fix row 58).
- 1529, 1538: the markers show only on the player's current map (the bridge road on the Crucible, Cirkin in the Junkyard).
- 1217, 1218: the Kardash Eloh Obelisk is not 1217's receiver nor 1218's giver (a server mission giver or receiver must be
  a creature NPC): 1217 is turned in by radio, 1218 is offered by radio.
- 1217: the walk up to the obelisk (hidden step 2) fires only on a step into the 6 m area: an arrival or relog inside it
  is no entry; step out and back in.
- 1217: the Eloh Resonator is not checked (a requirement takes only mission-issued items); 1538, which gives it, is the
  prerequisite.
- 1218: after an abandon the offer comes back on the next arrival on the Crucible (leave and return, or relog).
- 1212: a foot soldier killed anywhere counts (a kill objective takes no area), not only in the Khulago Base trenches.
- 1215: the client's text says "Enter the Bane structure ... access the mainframe within"; the terminal stands at the
  tunnel's mouth, as the server navmesh does not reach the console inside.
- 1216: Ugoretz stands; his patrol ("patrolling the northwest area", TaRapedia) is not built.
- 1215 (as 1538's keycard, row 55): the program is a mission item: a destroy, sale or trade of it is refused while 1215
  is held, so it cannot be lost before the hack. Should it be gone anyway (a GM removal), the hack cannot be done:
  abandon and accept again for a new program.

- 1214: no air strike after a beacon ("bomb drops", client offer text). No mission action or scene intent calls an air
  strike: the fire support strike is a player ability's (AbilityManager.FireSupport), the Torcastra carpet bomb that
  prison's own code, and a falling bomb object (PlacedUsables) goes off on its own clock for every player.

- 1152 and 1153: the fork is built as designed (keep the secret fails 1152), not as the retail known issue (refusing
  completes 1153 and fails 1152).

## Server data that differs from the sources (not changed)

- Private Drake is female on TaRapedia; the upstream row uses class 3846. Not changed.
- Awol Captain Cheung 510015 stood in the Staal cluster; the client names him leader of the Awol Camp (upstream D3).
  Moved in set 6.

## Server data changed by set 1 (upstream rows)

- Moved: DeLisi, Drake (from the placeholder cluster near 947, 152, 180 into Outpost Intrepid); Vitto, Donal, Rigs (from
  the placeholder cluster near -17, 188, 323 into their quarters).
- Packages (upstream D3): Rigs 510025 gets 1231; D'Ambrose 510023 gets 1301 (was 1229, Munit's).

## Server data changed by set 2 (upstream rows)

- Moved: Lohen 510024 (from the Staal placeholder cluster, -13.9, 187.85, 328.6, into his office, -215, 244.91, 326);
  Haggus 510026 (from the same cluster, -20.4, 187.77, 326.5, to -400, 249.68, 270, Labbna Quarter); Fiorella 510022
  (from the Outpost Intrepid cluster, 944.4, 170.86, 185, to 959, 144.68, 111); Cugin 510032 (from y 137, 44 m under the
  Staal Centrum floor, -188.8, 137.08, 264.2, to -174.3, 196.68, 280.2).
- Added: local teleporters 909 and 910 for the Kappa Grupa's office (the client's Brann statics had no server rows).

- The Staal walks fix (`20271108000500`) moved Merchant Vargo 510033 out of the Staal Centrum crafting station (kraftwerk
  22 at the same point, upstream D11) to (-190.5, 180.79, 267.2). The fix `20271108000800` (to-fix row 43) moved him to
  (-186, 180.78, 270): from the end of the walk from Munit the station hid him.

## Server data changed by set 3 (upstream rows)

- None moved. Added: Medical Technician Halprin, the Warden Bot and the Lasher creature rows and their pools (upstream D7:
  the World DB had no Lasher on any map, no open-world Warden Bot on the Crucible and no Halprin).

## Server data changed by set 4 (upstream rows)

- Moved: Executioner Derge's pool 520013 from the Fort Intrepid control point flag (757.1, 171.1, 549.1), shared with the
  Thrax Commander 520014 and 27 m from the control point hospital, to the rampart (859, 188.68, 476) (upstream D10).
- Added: the Smart Mine and the Reconstructor Bot creature rows and their pools (the World DB had no Smart Mine and no
  Reconstructor Bot on any map).

## Server data changed by set 5 (upstream rows)

- None moved. Added: Viddia Sigal Nema, Top Khana Sigal Pico, Trista, a Crucible Shield Drone, Preceptor Molitor and their
  pools (the World DB had no row for them and no Shield Drone on the Crucible).
- The marker fix before set 4 (`20271108000300`) moved the upstream Flaregasher pools 540139-540141 (off the Staal Junkyard
  door and off the navmesh) and 540144 (off the Thunderhead border) (upstream D9).

## Server data changed by set 6 (upstream rows)

- Moved (upstream D3, the placeholder clusters): Awol Captain Cheung 510015 from (-15.7, 187.78, 323.1) to the Awol Camp
  (-953, 309.56, 255); Awol Lieutenant Deirdre 510016 from (-11, 188.52, 326.5) to the Staal Centrum (-230, 180.86, 324);
  Lieutenant Sherer 510030 from (944.4, 152, 176.4) to (969, 144.68, 108); Intelligence Officer D'Ambrose 510023 from
  (942, 152, 180.7) to (975, 144.68, 110).
- None added. Packages unchanged (D'Ambrose has 1301 from set 1).

## Server data changed by set 7 (upstream rows)

- None moved. Added: Cirkin 770030, Krubb 770031, Mungrel 770032, Sedge 770033 (squad 770031 with the Shield Drone
  770022), Kalamit 770034, Lamontix 770035, the Technician 770036 (pools 770036, 770037), the Ultra Reconstructor 770038
  (upstream had no row for them: to-fix row 51).

## Server data changed by fix job F (20271108001000)

- Upstream Flaregasher pool 540141 (moved by 20271108000300): moved off a lava flow to (-368, 249.68, 70) and made a point
  (radius 15 to 0); 1149's marker 3 on it.
- Ours: the mine pools 770014-770016, Warden Bot pools 770009 and 770011, Reconstructor pools 770017 and 770018, the Lasher
  pools 770012 and 770013, Shield Drone pool 770024 and Molitor's pool 770029 made points (radius 0); 770011, 770012,
  770013, 770024 and 770029 moved off lava (to (-636, 249.76, 543), (-752, 285.6, 368), (-780, 286.04, 356),
  (415, 170.08, 113), (-853, 286.64, 359)) with their markers.

## Server data changed by fix job G (20271108001200)

- Ours: Shield Drone pool 770026 made a point (radius 6 to 0); not moved.

## Server data changed by set 8a (upstream rows)

- Moved (upstream D3, the placeholder cluster inside the Prometheus Outpost control point model): Major Keplinger 510031
  from (402.2, 169.77, 384.4) to (379, 168.47, 384); Larai Zupa Madias 510029 from (402.2, 169.77, 391.4) to (378.9,
  168.3, 367.6). Captain Tiersky 510021 stays inside the model (set 9, 1224).
- Added: Professor Tiernan 770039 (Crucible) and Captain Amandla Botha 770040 (Ashen Desert); upstream had no row for them.

## Server data changed by set 9 (upstream rows)

- Moved (upstream D3, the placeholder cluster inside the Prometheus Outpost control point model): Captain Tiersky 510021
  from (404.2, 169.77, 387.9) to (379, 168.36, 400); his pool stays control point 31's AFS link (kind 2).

## Server data changed by fix job H (20271108001500)

- Ours: the Ultra Reconstructor's point pool 770038 moved from (-14, 335.14, 244) to (164, 265.44, -76), 1529's marker 9
  with it.

## Server data changed by set 11a (upstream rows)

- Moved: Supervisor Sheira 510038 (Ward) from (-18, 62.61, -99.8), the medic Siebring's and the hospital teleporter's spot,
  to (-18, 62.61, -88); Larai Sigal Tempa 510028 (upstream D3, the Staal placeholder cluster) from (-20.4, 187.77, 319.7)
  to (-307, 180.79, 326).
- Added: Shanul 770045, Dhimand 770046, the Incurable Brute 770047 (attack 770047), the Incurable Slayer 770048, Jaikev
  770049, Parvatee 770050; pools 770045-770057 (upstream had no row for them).

## Server data changed by set 12a (upstream rows)

- Added: Information Analyst Ruben 770058 (Crucible) and Cipher Specialist Brian 770059 (Chaukas), their pools 770058 and
  770059 and Brian's pool pose (upstream had no row for them). No upstream row moved.
