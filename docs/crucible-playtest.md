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
  770002, -215, 196.5, 205) and Rigs (Labbna, moved to -392.77, 249.46, 276.23). A marker at each. Turn in at DeLisi:
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
  (new, 770003, -197, 180.7, 319), Merchant Cugin (moved to -174.3, 196.7, 280.2), Merchant Vargo (not moved, -192.5,
  180.5, 264.2), Merchant Yeg (new, 770004, -153.7, 196.9, 346.9). Bunko, Cugin and Yeg each give their Protection Money.
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
  counts; markers at pools 540139-540141 by the Staal Junkyard door). Turn in at Rigs: 75000 XP, the Labbna Syndicate
  Token (50317) and one of 20355, 28634, 33193, 35591.

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
- 1149: any Flare Gasher counts (creature flag 85, as Targets of Opportunity 1589). Markers at the three pools by the
  Staal Junkyard door.
  Roscoe (1146, class 7338, flag 85) is a Flare Gasher too: with 1149 open, his kill counts 1 of the 20.
- Melching and Vargo are not moved (their upstream places pass the probe).

## Deviations (client/server limits)

- 1143: one counter of distinct corpses does the work of the client's twenty per-corpse objectives.
- 1145: no failure for being caught with the wire.
- 1148: until Calendar Girl (1144) is built, every player hears Melching's "neither" text (203).

## Server data that differs from the sources (not changed)

- Private Drake is female on TaRapedia; the upstream row uses class 3846. Not changed.
- Awol Captain Cheung 510015 stands in the Staal cluster; the client names him leader of the Awol Camp (upstream D3). Not
  moved in set 1.

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
