# Ligo Crucible missions: playtest list, assumptions and deviations

Missions that are implemented (data, migrations and tests pass) but not yet played through in the
game client. A checked box (`- [x]`) means the playtest is done and the mission works.

Format: `- [ ] <mission id> <name> (<zone>): <what to look at>`.

## Ligo Crucible

Set 1, Outpost Intrepid (migration `20271108000000_Crucible_outpost_intrepid`). Map 1993
(`adv_arieki_ligo_burningsteps`), log category 1 "Battlefield (Crucible)". New NPCs 770000-770002, next free id 770003.

- [ ] 1142 The Last Men Standing (Ligo Crucible): Sergeant Shandy (new, 770000, Outpost Intrepid, 987.07, 140, 59.65)
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

## Deviations (client/server limits)

- 1143: one counter of distinct corpses does the work of the client's twenty per-corpse objectives.
- 1145: no failure for being caught with the wire.

## Server data that differs from the sources (not changed)

- Private Drake is female on TaRapedia; the upstream row uses class 3846. Not changed.
- Awol Captain Cheung 510015 stands in the Staal cluster; the client names him leader of the Awol Camp (upstream D3). Not
  moved in set 1.

## Server data changed by set 1 (upstream rows)

- Moved: DeLisi, Drake (from the placeholder cluster near 947, 152, 180 into Outpost Intrepid); Vitto, Donal, Rigs (from
  the placeholder cluster near -17, 188, 323 into their quarters).
- Packages (upstream D3): Rigs 510025 gets 1231; D'Ambrose 510023 gets 1301 (was 1229, Munit's).
