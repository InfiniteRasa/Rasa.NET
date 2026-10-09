# Wilderness native contracts

This is the reference for the outdoor Concordia Wilderness rollout on map
`1220`, client `1.16.5.0`, content revision `wilderness_1_16_5`. It records
native identities, exclusions and explicit reconstruction decisions. Paired
World migrations deploy the content; automated behavior and the still-unperformed
native-client/live-MySQL acceptance are separate evidence.

Use the existing [migration workflow](missions.md) and
[normalized content/typed script interfaces](mission-reference.md). Do not add
a runtime JSON catalog, another mission engine, a publication step, or client
changes. Preserve Bootcamp `1990/1992/1994/1995/2005`, its normal/retry/skip
qualification, and its existing Rogers handoff.

## Evidence and coverage

`N` means native identity or an explicit native text fact. `S` means the existing
public research. `P` means a preserved, nonoperational server-catalog rule.
`R` means an explicit reconstruction of missing server facts.
Native presentation does not prove availability, prerequisite semantics, loot
rules, coordinates, or original server transitions.

The reconciliation examined **all 1,168 native mission identities**, not just
the original 66 location-reference candidates. It joined 74 researched hub
rows against the complete title and Logos-token sets, then checked location
and contact references across mission, objective, and conversation text.
This produces **99 native candidates** and two source titles without a native
identity. All candidates have a disposition:

| Disposition | Native IDs | Meaning |
| --- | ---: | --- |
| Outdoor release | 64 | Enabled by this rollout, including mutually exclusive/recovery branches and partially completable 1449 |
| Instance-dependent | 13 | Preserve the real instance objective or prerequisite; do not make an outdoor substitute |
| Other-zone | 16 | Includes existing Bootcamp handoff references, which remain enabled under their own revision |
| Retired | 3 | Explicit later-retail exclusions |
| Conditional event | 2 | Not normal outdoor progression or implicitly active events |
| Source-only unmatched | 1 | Native identity exists but an outdoor runtime identity is not established |

The 74-row hub index omits the prose-only capstone `701`. Full-table work also
finds native-only outdoor `1526`, `2010`, and `2011`; these are not silently
dropped because the wiki has no matching hub row. Matching an unrelated
mission's anecdote about Wilderness does not make that mission outdoor content.

### Reproducible native source

The source is the generated `client` tables in the local `trpython` repository,
revision `167d2b88b48e040c5e244e6f932518c52638f4f9`, reconciled against server
baseline `9ccc98c674dabcfd4e7b332159ce3df9691d8fe8`.

| Native table | Rows | SHA-256 |
| --- | ---: | --- |
| `missionconversation.pyo_dis` | 5,821 | `1680a3a69db5f8835de8bee3e9372fcf8a68056b44acf6fd2edcdd1001810391` |
| `missionobjective.pyo_dis` | 3,454 | `7e4f4f477ca77f8cded278410a8975b4b72bef8694b3ab7d8df47093237c8562` |
| `objectiveconversation.pyo_dis` | 1,727 | `c3a0a48a2c1300b9a225acad22897ba56bdc257e2ddf5996653488faedc7b3d0` |
| `language\english\missiontextlanguage.pyo_dis` | 32,988 | `bc78b38b5f89044bb4ac2b8956f00a56111a85a6db94203236c35e07e4b2ec38` |

Parse only the `lookup` assignment using `ast.literal_eval`; never execute
decompiled Python. Normalize Python-2 single-digit hex escapes before parsing.
English mission strings use `(textId, languageVariant)` keys, not a bare text ID.
The compiled archive member inventory corroborates presence, not byte-for-byte
equivalence with these decompiled files.

The
[immutable test expectations](../src/Rasa.Test/Missions/Wilderness/WildernessMissionCases.cs)
preserve all 99 candidates' mission-text identities, **221 objective identities**
and **110 objective-conversation identities**, including empty native strings.
They contain no executable mission definitions and no full proprietary dialogue.
The session's `w0-native-contracts.json` additionally contains the extraction
hashes, evidence, reward/prerequisite decisions and item-template candidates. It
is a research artifact, not a file consumed by Game.

Mission conversation kinds are `1=name`, `2=log`, `3=opening`, `4=finishing`,
`5=reward`, `6=reminder`. Objective conversation keys are
`(mission, objective, NPC package, player flag, kind)`, where kind
`1=completion`, `2=reminder`, `3=choice body`, `4/5/6=choice slots 1/2/3`.
Do not confuse package, creature, spawn, item-class, and item-template IDs.
An empty string with a real ID stays that ID, not `0` or a replacement text.

The native mission category is `10000044` ("Battlefield (Wilderness)") in
`missioncategorylanguage.pyo_dis`. It requires an unsigned 32-bit value through
World content, the immutable mission definition and the native constant-data
tuple. The old byte field cannot represent it; category `1` is Crucible, not a
Wilderness fallback.

Public evidence is the already completed 2026-09-28 hub research, principally
[TaRapedia's Wilderness index](https://tabularasa.fandom.com/wiki/Wilderness_mission_list)
and its linked mission pages. The opening was also cross-checked by that
research against the contemporary
[Ten Ton Hammer Alia Das guide](https://www.tentonhammer.com/guides/tabula-rasa-alia-das-mission-guide).
Its older tutorial exit is superseded, not restored. No new broad web survey
is part of this reconciliation.

## Opening contracts

| Mission | Native title ID | Objective ID: name/body IDs | Native completion identity: objective/package/flag/text |
| --- | ---: | --- | --- |
| 1407 Too Close For Comfort | 12099 | `1:12104/12105 -> 10:12128/12129` | `1/113/1/12106`; `10/168/1/12130` |
| 1069 Receptive Reception | 6038 | `1:6042/6620 -> 2:13793/13794 -> 3:13796/13797` | `2/168/1/13795`; `3/112/1/13798` |
| 479 Forming Alliances | 1011 | `1:2592/6525` | No native objective-conversation row |
| 1449 Wilderness Targets of Opportunity | 12764 | All 26 IDs retained below | No native objective-conversation rows |

1407 objective 10 is an **actual Forean Ranger escort to Solis**. Moawi's
conversation starts the task; selecting Solis's conversation without the
escort's successful arrival must not complete it. Route, actor ownership and
failure/reset behavior use the existing typed scene system and the separate
World binding inventory.
Native completion text 12106 specifies one of Moawi's Rangers, but gives no
personal name or escort package/class identity. Packages 113 and 168 identify
Moawi and Solis, not the escort. Do not select Tirna/Tarina from a legacy
creature comment and present that as native mission evidence.

1069 requires Enhance (`Logos 10`), return to Solis, then Apirka, in that order.
Reconstruction: already-owned Enhance satisfies the acquisition stage when
accepted; it does not skip either conversation. 479 requires **12 acquired
Thrax hearts**, not 12 arbitrary kills. Native class `10346` has stack size 12;
the selected existing template is `2285` (alternate `16540` has the same class).
The template selection and 100% eligible quest drop rate are reconstructions,
not a recovered native loot table.

1407 and 1449 have no additional mission predecessor. Their zone admission uses
the existing durable starting-experience qualification, so normal completion,
retry completion and authorized skip all work. Never require only `1995`
completion. `1407 -> 1069 -> 479` is corroborated by public research; its
encoding as a server prerequisite graph is reconstructed.
1449's log identifies Cimoch as the return contact, but its native mission tables
contain no NPC-package identity, and the World join found no package row for
creature 118. Do not invent a package or claim default 0 is sourced. A
mission-only giver/receiver may use the existing NPC-capable actor without a
package if protocol and validation support that path; the coordinator owns
that check. Objective-dialogue package requirements remain separate.

### 1449: offer early, preserve genuine incomplete work

Keep native IDs
`1,3,4,5,6,7,8,20,21,22,23,24,25,40,41,46,47,48,49,50,51,52,53,54,55,58`.
Ten summary targets run in parallel; objective 58 means those ten targets,
not all 26 raw rows counted as separate targets.

| Summary | Native requirement | Outdoor treatment |
| ---: | --- | --- |
| 1 | All Wilderness AFS waypoints; counter text 12789 | Seven recovered waypoint IDs below (P), not repeated unlock events |
| 3 | 40 Xanx; counter 12805 | Credit eligible kills on map 1220 |
| 4 | 30 Shield Drones; counter 12808 | Credit eligible kills on map 1220 |
| 5 | 200 Thrax soldiers; counter 12802 | Credit eligible kills on map 1220 |
| 6 | Six named officers; counter 12809 | Children 20..25 are distinct, not six kills of one officer |
| 7 | 40 Miasmas; counter 12824 | Credit eligible kills on map 1220 |
| 8 | All Wilderness Logos | Twelve recovered Logos IDs below (P); Earth 408 is excluded |
| 40 | Three actual operations; counter 12854 | **Incomplete** until Pravus, Donn and Crater Lake primary operations exist and are completed |
| 48 | All caves; counter 12865 | Six native children 49..54 need distinct outdoor region bindings; the sixth visited gives Wilderness Spelunker (title 372) |
| 55 | Outdoor and instance story missions; counter 13685 | Persist outdoor membership but **remain incomplete** without the actual instance stories |
| 58 | All ten targets | **Incomplete**, with final reward inaccessible |

Officer identities are `20=Glognar`, `21=Phlegg`, `22=Rankash`, `23=Horntail`,
`24=Old Scratch`, `25=Archfiend` (N). Native instance children `41/46/47`
have blank descriptions. The reconstruction is
`41=Pravus`, `46=Donn`, `47=Crater Lake`, following summary-text order; this is
not a recovered native child-to-instance mapping. All remain incomplete.

Cave **membership is now resolved** by the coordinator's bounded secondary
research of the ToO page's own
[List of caves](https://tabularasa.fandom.com/wiki/Wilderness_Targets_of_Opportunity#List_of_caves)
(S). This is six sources, not seven entrances:

| Source member | Published pair(s), axes unspecified | Source |
| --- | --- | --- |
| Alia Caverns | `(816,615)` | [Alia Caverns](https://tabularasa.fandom.com/wiki/Alia_Caverns) |
| Daghda's Urn cave, not the town | `(-796,760)` | [Daghda's Urn](https://tabularasa.fandom.com/wiki/Daghda%27s_Urn) |
| Enigma Caverns | `(-869,-592)` | [Enigma Caverns](https://tabularasa.fandom.com/wiki/Enigma_Caverns) |
| Pinhole Falls Cavern | No numeric coordinates supplied; plural title redirects to this same member | [Pinhole Falls Cavern](https://tabularasa.fandom.com/wiki/Pinhole_Falls_Cavern) |
| Ranja Caverns | `(-486.5,-402.8)` | [Ranja Caverns](https://tabularasa.fandom.com/wiki/Ranja_Caverns) |
| Explicitly unnamed cave on the trail south of Ranja Trail | Two entrances: `(-491,-606)` and `(-743,-571)` | [ToO cave list](https://tabularasa.fandom.com/wiki/Wilderness_Targets_of_Opportunity#List_of_caves) |

These sources do **not** map native children 49..54 to names, label the two-axis
convention, or supply heights/grounded volumes. Original bindings remain
unrecovered. The coordinator will document a stable reconstructed child mapping
and ground its XYZ volumes. This worker records the supplied membership without
extending research, assigning child IDs from list order, guessing heights, or
replacing visits with nearby Logos/instance completions.

The preserved rules in
`src\Rasa.Game\Managers\MissionDefinitionCatalog.cs`,
`CreateRecoveredInactiveDefinitions`, supply exact membership (P):

- Objective 1: waypoints `{49,50,51,57,61,73,156}`.
- Objective 8: Logos `{1,2,6,9,10,23,24,28,38,49,53,56}`.

Both are `CompleteWhenAllDistinctSubjectsObserved`, not raw event counters.
The World worker independently joined each member to current map-1220
teleporter/Logos rows and waypoint markers. That verifies the bindings without
defining membership as every seeded object. Earth Logos 408/class 30408 exists
on the map but is outside this recovered twelve-Logos rule. Membership comes
from the preserved rules, not merely from counting public quest titles.
The recovered catalog itself remains nonoperational.

Reconstruct story membership with real required outdoor story completions and
branch-aware alternatives `1392|1393`, `623|791`, `700|820`, plus actual instance
stories. Retired missions, event content and mutually exclusive class gear
missions are not unconditional requirements. A missing instance requirement
must evaluate false, not pass through an empty `All()` collection.

The public final reward is 20,000 XP, a clone credit and the Master of Wilderness
title. Credits were absent; the explicit future reconstruction is 2,000, **not
an observed retail value**. Entitlement/title numeric identities are not
established here. Nothing grants that reward in the outdoor-only phase.

## Outdoor inventory and prerequisite graph

Every row below has disposition **outdoor release**. This is an authoring
inventory, not an enabled-content count. In the order column, `[]` means
parallel work, `->` a stage boundary, and a hidden/header ID still retains its
native identity. A predecessor means completed unless the table says active
or names a branch outcome. `None` deliberately adds no unsupported mission
prerequisite; it does not bypass normal character/zone qualification.

XP/credits come from S unless marked `R`. Absence in a source was never converted
to zero. The table fixes concrete reconstruction values for implementation;
the item-reward limitations are addressed separately below.

### Alia Das

| ID / title | Predecessor or condition | Objective order / required work | XP / credits |
| --- | --- | --- | --- |
| 1407 Too Close For Comfort | None | `1 -> 10`, including escort | 4000 / 600 |
| 1069 Receptive Reception | 1407 | `1 -> 2 -> 3` | 4000 / 600 |
| 479 Forming Alliances | 1069 | `1`, 12 hearts | 4000 / 400 |
| 1390 Conscientious Objector | 479 | Release `1 -> 2 -> 8 -> 11`; arrest `1 -> 3 -> 4 -> 10`; hidden 12 | 2000 / 400 |
| 1392 Conscientious Objector - Part Two | 1390 arrest, excludes 1393 | `1`, report to Rogers | 8000 / 800 |
| 1393 Conscientious Objector - Part Two | 1390 release, excludes 1392 | `1`, different report to Rogers | 8000 / 800 |
| 422 Miner Difficulties | 1069 | `1`, Richards resupply | 5000 uncertain S, adopted R / 215 |
| 429 River Recon | 479 (uncertain S, adopted R) | **`5 -> 4`**, patrol recon before Witherspoon | 10000 / 1000 |
| 428 Supplies On The Double | 479 | `1 -> [3 deadline,2 delivery]` when crate is damaged | 5000 / 750 |
| 421 A Father's Goodbye | 428 | `3`, deliver dogtags | 6000 / 900 |
| 427 Lurking In The Shadows | 479 | `1 -> 6 -> 7`, Oliver, Fulgor/shipment, Caufield | 4000 / 400 |
| 1449 Wilderness Targets of Opportunity | None | Ten summaries; genuine instance gates remain | 20000 / 2000 R, final payout locked |
| 1638 Logos: Enhance | None | `2`, Enhance 10 | 2500 / 1500 |
| 1640 Logos: Area | 1069 | `4`, Area 1 | 3000 / 1500 |
| 921 Logos: Projectile | 1069 | `5`, Projectile 24 | 3500 / 1500 |
| 907 Logos: Damage | 1069 | `1`, Damage 6 | 3000 / 600 |
| 909 Logos: Time | 1069 | `1`, Time 28 | 3000 / 600 |
| 1639 Logos: Power | 1069 | `6`, Power 23 | 2500 / 600 |
| 1633 Logos: Attack | None | `3`, Attack 2 | 4000 / 100 |
| 911 Logos: Mind | Unknown S; no extra gate R | `3`, Mind 56 | 4000 / 1500 |
| 1741 Report to Liaison Standley | None | `1`, package 2049 at Twin Pillars | 4000 / 600 R |
| 1526 Training Day | Level 5 R; already-trained players may report | `1`, Kincaid package 2588 | 1000 R / 200 R |
| 2010 Getting It In Gear: Soldier Class | Soldier-family qualification, not 2011 | `1`, Caufield package 133 | 1000 R / 200 R |
| 2011 Getting It In Gear: Specialist Class | Specialist-family qualification, not 2010 | `1`, Caufield package 133 | 1000 R / 200 R |

1526/2010/2011 are native-only additions with definite outdoor contacts.
Native text does not establish their original equipment reward selection.
Class-family checks are reconstructed so later specialization does not
accidentally revoke a legitimate unclaimed class handoff. These offers must
not be triggered merely by client-supplied mission IDs.

### Lower Eloh Creek and Pinhole Falls

| ID / title | Predecessor or condition | Objective order / required work | XP / credits |
| --- | --- | --- | --- |
| 431 Distress On The River | 429 R, following the field-report handoff | `1 -> 2 -> 3`, Hugh Corman, Oingin, Wood | 2500 / 500 |
| 433 Snipe Hunt | 431 | `[1,7]`, six snipers plus one acquired Overseer datapad | 6000 / 900 |
| 434 Rendezvous At The LZ | 432 S | `1`, Witherspoon 208 to Randolph 212 | 3000 / 600 |
| 432 Hitting 'em Where It Hurts | None | `1`, three distinct gas harvesters | 3000 / 600 |
| 506 Mama Miasma! | 422 | `1`, **three** egg-layers N, not five S | 5000 / 750 |
| 436 The Trouble With Treebacks | 506 | `1`, place/activate emanator; nonlethal | 2500 / 500 |
| 508 Survey Says | 506 | `[2,3,4,5,6] -> 7 summary -> 8 Rogers` | 5000 / 750 |

434's native completion/reminder identity `208` supports Witherspoon rather
than the wiki's contradictory Jennings infobox. 432's contact-location
disagreement is a World binding concern, not permission to invent a package.
433's counter text 2586 names Glognar while the public report permits
Glognar/Rankash/Phlegg for the datapad. Require one eligible datapad, not all
three named kills. 508's five devices are independently consumed for credit.
Other-zone `1200` is not this mission.

### Wilderness L.Z., Twin Pillars, Memory Tree and Gellman Meadow

| ID / title | Predecessor or condition | Objective order / required work | XP / credits |
| --- | --- | --- | --- |
| 665 Cache of the Day | None | `1`, ten distinct fuel barrels S | 7000 / 350 |
| 430 Mortar By Numbers | None | `[3,4,5,6]`, four distinct mortars | 6000 / 900 |
| 549 Failure to Launch | 430 | `1`, catalyzer from package 382 | 3000 / 600 |
| 776 Soldier's Blood | None | `2`, ten blood samples for Ojy | 4000 / 300 |
| 795 Lightbender Glands | 776 | `3`, four glands | 4000 / 300 |
| 771 Droning On | 795 | `2`, six Shield Drone parts | 4000 R / 300 |
| 441 In Short Supply | 549 | `1`, Randolph 212 to Duncan 218 | 3500 / 700 |
| 442 Quarantine | None | `1 -> 2`, Duncan then analyzer package 1486 | 3500 / 700 |
| 444 Unity Among Men | 442 | `1`, test results to Eleanor | 6000 R / 1200 |
| 623 Smuggler's Blues | None | `[1,2,3] -> 4`, three deliveries; hidden 5 | 7000 / 10000 |
| 791 Suspicious Minds | **623 active**, outstanding stolen drugs | Optional `1`; `2` choice; hidden 3 | 3500 / 700 |
| 570 Traitors to the Cause | None | `1`, Baruhi at Memory Tree | 8000 / 1200 |
| 912 Logos: Self | None | `4`, Self 38 | 4000 / 1500 |
| 1634 Logos: Target | None | `4`, Target 49 | 4500 / 800 |
| 1635 Logos: Here | Mind 56 + Power 23 for shrine entry S | `2`, Here 53; no bypass of shrine gate | 4500 / 900 |
| 908 Logos: Enemy | 1069 | `1`, Enemy 9 | 4000 / 600 |
| 574 Machinations | 570 | `2 -> 3`, ten remains, then outdoor Parsons | 8000 / 1200 |
| 666 Escape Velocity | Encounter at cache; no 665-completed gate R | `1`, living Pierre escort before departure | 14000 / 1400 |
| 697 The Walking Wounded | None | `1 -> 2`, living Matthew escort, then Quincy | 8000 R / 1600 |

The wiki marks 666's Cache of the Day prerequisite uncertain. Requiring that
mission's completion would prevent the documented mid-cache rescue, so
reconstruction uses discovery at the cache. Ranja Caverns are traversed
outdoors; 697 is not deferred as an instance quest. 574 remains outdoors
although its successor 593 enters Pravus.

### Ranja Gorge and Daghda's Urn

| ID / title | Predecessor or condition | Objective order / required work | XP / credits |
| --- | --- | --- | --- |
| 425 Contents Under Pressure | 444 | **`1 -> 2 -> 5 -> 3`**, Samuel, samples, Samuel, Eleanor | 7000 R / 1350 |
| 679 Going Native | 425 | `1`, Nula | 4500 / 900 |
| 451 A Visit To The Elders | 679 | `3` header/reminder; `1 -> 2`, Doyan then Todae | 10000 / 1500 |
| 700 Traveling Medicine Show | 698 | Untimed `[1,2,3] -> 5`; hidden 6 | 11000 / 550, one medicine payout |
| 820 Traveling Medicine Show | Failed first batch, no 700/820 reward R | Timed `[1,2,3] -> 5`, parallel deadline 7; hidden 6 | 11000 / 550, recovery alternative |
| 701 Orders From High Command | **700 active with vaccine**, radio R | `1 -> [7 deadline,2 choice] -> give or 3 fight -> 8 -> 6`; hidden 5 | 22000 / 2200 |
| 860 Sacs And Violence | None | `1`, **four** egg clusters, explicit conflict below | 18000 / 800 |
| 758 Fithikally Challenged | 771 | `3`, ten spleens | 4000 / 600 |
| 787 Xanx For the Help | 758 | `3`, four pincers | 4000 / 600 |
| 769 Predatory | 787 | `2`, three Predator parts; outdoor source available | 4000 / 600 |
| 696 Xanx For The Memories | None | `1 -> 2`, Arioch/Codex, then Juvak | 7500 R / 1500 |
| 682 Childhood's End | 451 | **`2 -> 3 -> 5 -> 4 -> 6`**, not sorted ID order | 10000 / 1500 |
| 695 Herbal Remedy | 682 | `4 -> [5,1]`, three Devils and five herbs; children 6/7/8 | 22000 / 2200 |
| 698 Elixir Vitae | 695 | `1`, essence to Eleanor | 11000 / 550 |

682 has no recovered attack count. Reconstruction creates a three-Xanx
encounter and requires its actual completion before Anjuhi speaks again;
ordinary nearby kills cannot replace it. 695's unnamed child association is
reconstructed as `6=Old Scratch`, `7=Horntail`, `8=Archfiend`.

**860 contains an internal native conflict.** Objective body `6740` says four;
mission log `4476`, opening `4477`, and the public guide say six. This contract
selects **four** for the active objective, giving the concrete objective
instruction precedence (R adjudication). The six-count log remains a known
client inconsistency. No server implementation can make both counts agree
without changing one known presentation; do not hide the discrepancy.

### The medicine-show decision

Do not select 820 merely because its ID is larger. Both identities have a role:

| Evidence | 700 initial batch | 820 replacement |
| --- | --- | --- |
| Title text | 3135 | 4080 |
| Opening text | 3137, first delivery | 4082, explicitly another batch after the first was taken |
| Native delivery objectives | 1/2/3 to packages 218/130/602 | Same recipients, different objective text identities |
| Return objective | 5, package 117 | 5, package 117 |
| Explicit timed objective | None | **7**, name 4099/body 6788 |

The public report places Traveling Medicine Show after Elixir Vitae and
connects it to 701's vaccine choice, but gives no replacement timer duration.
Native 701 dialogue `3306/3179` requires a vaccine still being carried.
Therefore its server admission is reconstructed as an interruption during 700,
not after completed 700 has consumed the delivery items. Reconstruction issues
the radio offer on 700 acceptance while the batch exists.

Giving the batch to Skeev consumes the remaining assignment-owned vaccine,
fails 700, and unlocks timed replacement 820. Refusal fights Skeev and preserves
the delivery batch. The exact retail failure/reoffer predicate is unavailable;
this is a declared reconstruction consistent with the replacement opening.
700/820 cannot run as independent simultaneous offers or pay twice. Do not turn
820 into an untimed alias or add its timer to ordinary 700.

## Native choices and deadlines

| Mission / objective | Package / flag | Choice body | Choice slot 1 | Choice slot 2 | Empty slot 3 |
| --- | --- | ---: | --- | --- | ---: |
| 1390 / 1 | 1646 / 1 | 11952 | 11953: release, then 1393 | 11954: arrest, then 1392 | 11955 |
| 791 / 2 | 415 / 1 | 3851 | 3852: betray; fail 623 | 3853: deny; preserve 623, no 791 payout | 3854 |
| 701 / 2 | 595 / 1 | 3179 | 3180: give batch; recovery 820 | 3181: refuse and fight | 3182 |

These are native callbacks **1 and 2**, not zero-based indices. The native
`trpython\client\ui\conversationwindow.py`, lines 677-685, enumerates callbacks
1/2/3 but creates a link only when the corresponding text has nonzero length.
All three Wilderness topics above therefore show exactly two links, plus the
window's separate cancel button. Never map the empty third slot to an invented
outcome. 1392 and 1393 are intentional different consequences, not
duplicate-title clutter. Release escort 1390.8 ends on the Wilderness side of
the Divide boundary.

The shared validator accepts the source-backed `1/2` maps and retains existing
`1/2/3` missions. `MissionApplication.TryApplyDialogue` rejects an unauthored
callback, including callback `3` for these two-choice topics, after reconnect
and on duplicate input. The native window source SHA-256 is
`7859ca7a3495a04a03324c3a0fb53f35640bd8bca387dc1a895c03e940ca125b`.

701 retains Burke objective 8 conversations with flags `1/26/27` and text
`3343/3342/3348`. Their complete original branch mapping is not recoverable
from the surviving dialogue alone. Reconstruct flag 1 for victorious refusal,
26 for voluntary surrender, and 27 for an expired ultimatum outcome; keep this
distinction documented rather than claiming native proof. Timeout follows a
hostile refusal/fight, not automatic voluntary surrender.

| Mission / objective | Start | Deadline | Basis / expiry |
| --- | --- | ---: | --- |
| 428 / 3 | Actual damaged-crate acquisition | 300 seconds | N spoilage; R duration. Fail and clean up spoiled assignment items |
| 666 / 1 | Escort accepted, including time spent freeing Pierre | 420 seconds | S seven-minute dropship departure; fail/reset escort |
| 697 / 1 | Living wounded escort | No numeric timer | N urgency without a recovered duration; death/owner-loss failure remains authoritative |
| 701 / 7 | Burke encounter exposes Skeev ultimatum | **60 seconds** | N 6714/3179 and S; execute timeout branch |
| 820 / 7 | Replacement batch accepted | 900 seconds | N timed objective; R duration. Fail/remove batch; reaccept has a fresh deadline |

Persist absolute deadlines; reconnect cannot reset them. Terminal cleanup and
failure/reaccept use existing assignment/scene ownership, not global actor
deletion or reused stale timers.
An expired optional choice window must execute its authored timeout transition
and may continue into combat. It is not a permanent mission-wide ban on
progress. Late choices before that timeout resolves are still rejected.

## Shared World binding reconstructions

`WildernessHubWorld` delegates to fixed contact and population helpers. It
corrects existing contacts rather than creating a second quest-only copy.
The following placement/profile choices are `R`; native names, classes and
packages remain independently identified. Installing these World rows alone
does not enable a hub's mission definitions.

Mission-specific World creatures, pools and attack rows use the reserved
`630001..630199` namespace. Their unshipped `530xxx` bindings were moved before
release because PR105 owns the Divide creatures and pools in that range.
Only the explicitly authored World identities and their references changed;
native classes, item templates, coordinates and mission IDs did not. Persistent
Char outcome flags `530002` (Milpas) and `530003` (Corman finale) remain unchanged.

| Role | Canonical binding | Reconstruction or correction |
| --- | --- | --- |
| Rogers | Creature/spawn `100`, package `116` | Retain his pose and Bootcamp receiver identity; use the later native dialogue package, not a client alias or duplicate Rogers |
| Witherspoon | `101/101`, package `208` | Enable his zero-count pool and place him at `(505,238.757,223)`, beside the actual Lower Eloh waypoint rather than at Alia |
| Kincaid | `510006/510006`, name `10604`, package `2588` | Training Officer at `(774.5,294.05008,393)`; reuse the existing generic trainer service and preserve Soldier/Specialist trainers `501001/501002` |
| Quillas | `114/192`, name `9518`, package `1646` | `(783,303.317277,130)` on native platform `6086`, entity `133079561791363`; the old X/Z has no support on this deck |
| Milpas | `630010/630010`, class `26833`, name `9519` | Separate mobile unarmed Forean at `(778,303.32,127)`, with no invented package; arrest and release use measured, grounded routes |
| Supplies crate | Native object class `26721` | `(456,233.9,193)` on the bridge bank; active class shares mesh `20300` with the deleted medical dispenser, but is not claimed as a recovered retail spawn |
| Fulgor | Existing creature `76`, PR105 pool `580010`, class `10857`, effective name `10100` | Retain the real pillbox-floor actor through PR105's replacement of old pool `157`; the older "southeast of Oliver" wording conflicts with the prepared Oliver seed pose |
| Pierre | `630070/630070`, female class `6340`, name `3097` | Native text identifies her as female; the six compatible AFS appearance pieces and starting pose `(-279,170.1,87)` are reconstructed |
| Mortars | `630076..630079`, native class `7482` | Four distinct creatures on actual base-`7478` anchors, numbered south to north; the existing generic emplacement attack profile is functional reconstruction, not recovered retail mortar AI |
| Lightbenders | Creature `630071`, pools `630071..630074`, class `7120` | Ordinary compound population using the compatible Lightbender weapon/profile, separate from Fulgor and any sniper variant |
| Egg-layers | Creature `630040`, pools `630040..630042`, class `10240` | Three supported interior Pinhole cave sites, not the ordinary surface Miasma pools |
| Treeback herd | Creature `630043`, pools `630043..630045`, class `6038` | Three non-aggressive animals on the hill above the caverns; emanator activation remains nonlethal |
| George | Creature/spawn `510005` | Hospital pose `(-698,170.233,-345)`; delivery approach `(-696,170.233,-343)`, not the elevated marker |
| Corman analyzer | Object class `7123`, package `1486` | Desk root `(-124.8,222.04809,-479.2)`, yaw pi, with player approach `(-124.8,220.91783,-477.2)`; native object conversation, not a fake creature |
| Forean Machina | Creature `630100`, pools `630100..630102`, class `6236` | Ordinary corpse-loot source with native weapon `6019`, not Hominis |
| Predator | Creature `630120`, pools `630120/630121`, class `3902` | Ordinary outdoor collection source, not a substituted named boss or instance requirement |
| Skeev | Creature/spawn `630130`, class `28589`, name `6734`, package `595` | Explicit 1500-HP reconstruction deployed together with W7's manual-combat binding |

Milpas's last release points are `(900.3,277.1,44)` and
`(899.45,276,39.9)` on the Wilderness side of
the Divide tunnel. Its minimum horizontal separation from the existing map-link
trigger is `13.5486 m`, outside the trigger's `8 m` radius.

Pierre's approach is about `559 m`; boarding is a separate `84.4 m` movement
leg to `(192.2,171.1,-100.5)` on native pad `10374`, beside placed dropship
`7331`. Arrival requires the same living leased actor within `0.6 m`
horizontally and `0.35 m` vertically. The placed ship has no recovered passenger
socket. This is a spatial boarding reconstruction, not a claim of native
attachment or rendered-client acceptance.

Pierre is held out of combat while she is a captive (R). She stands in the Bane
cache of 665, `7 m` from the Atropos Linker of pool `580012` and inside the post
of pool `580005`; as an ordinary friendly creature she was sought by that
garrison and killed before anyone had spoken to her. Her public encounter is a
`ManualCombat` one (`Hold_captive_pierre`): from her spawn until the
forcefield falls she is neither sought nor damaged. Both escort routes resume
after combat, and a held actor set on such a route is in combat from there, so
from her release she can be fought and her death fails the escort. A lease that
resets holds her again.

Harvester `7906` extends `2.974437236785889 m` below its origin. Its approved
roots are `(347,233.380158210,178)`, `(338,230.731414425,251)` and
`(313.5,233.363021230,339)`, all heading zero. These are explicit small shifts
from the guide positions to supported, nearly level bases. Test the model's
physical base and reachable attack approach, not an NPC-style origin-to-floor
distance. Raising or lowering only an origin without its mesh offset is not
placement validation.

The `wilderness-horizontal-v1` navigation profile is an opt-in repair for this
map only. The measured Ranja split ramp has continuous native floor and enough
clearance for the configured `0.6 m` radius, but the old `0.4 m` grid lost
connectivity at radius erosion (`0.6 m` rounded up to `0.8 m`). The repaired
`0.2 m` grid keeps the physical agent, `51.2 m` tile width, border, region areas,
edge error and detail distance unchanged. It adds no off-mesh links or synthetic
floor. Full-map checks of the installed asset include Matthew/Quincy, the original Solis
escort, both Milpas routes, Pierre/boarding, and all five survey-site approaches.
An unused local Survey4 diagnostic pair still does not connect; that is distinct
from the verified public approach to the actual survey site.

The accepted asset SHA-256 is
`C1623440A8B4D6219B7DAF27C4E15052038409AAD9647D7EC6D2D22F04AACBF3`.
Sniper pool `630050` has a separate forward placement correction to
Z `297.786076`; its other coordinates, heading and native attack profile stay
unchanged. Neither asset verification nor source-grounding proves a native-client
walkthrough.

```powershell
dotnet run --project src\Rasa.NavMesh\Rasa.NavMesh.csproj --configuration Release -- --client "C:\Program Files (x86)\Tabula Rasa 1.16.5.0" --map adv_foreas_concordia_wilderness --profile wilderness-horizontal-v1 --out C:\Temp\wilderness-nav-candidate
```

Build into a candidate directory and compare the actual routes and native
surfaces before replacing the checked-in map. This profile is not enabled for
other maps and cannot be combined with geometry overrides. Native-client
rendering and live character traversal remain separate acceptance evidence.

## Item and reward evidence

### Supported consumable reconstruction

The selected native bomb, grenade, adrenaline, resurrection-trauma and module
templates do not have implemented server effects. Their real identities and
action metadata are not sufficient to make them functional rewards. This
rollout uses the following single reconstruction policy instead of adding new
combat or modifier systems:

| Mission | Enabled reward | Replaced unsupported templates |
| --- | --- | --- |
| 1390 | `44918 x1` OR `44917 x1` | `111247 -> 44918` |
| 422 | `44918 x2`, two fixed rows | `111247 -> 44918` |
| 432 | `44917 x1` + `44918 x1` | `111022 -> 44918` |
| 434 | `44918 x2`, two fixed rows | `111247 -> 44918` |
| 436 | `44919 x1` + `44918 x1` | `111022 -> 44918` |
| 549 | `44917 x1` OR `44918 x1` | `47593 -> 44917`, `45125 -> 44918` |
| 441 | `44918 x1` OR `44917 x1` | `45125 -> 44918` |
| 623 | `44919 x1` | `123023 -> 44919` |
| 791 | `44918 x1` + `44917 x1` | `111247 -> 44918`, `111136 -> 44917` |
| 758 | `44920 x2` + `45047 x4` | `111227 -> 44920` |
| 787 | `44919 x2` + `44920 x6` | `47594 -> 44919`, `45441 -> 44920` |
| 769 | `44921 x6` + `44920 x2` | `111227 -> 44920` |

Templates `44917/44918/44919` are Class I Basic/Standard/Advanced medpacks;
`44920/44921/45047` are their Class II counterparts. All use real healing
action `419`, levels `1..6`. XP, credits, authored quantities and distinct
selectable alternatives are unchanged. Healing does not implement EMP,
grenade, adrenaline, trauma-removal, Resist Fire or other historical module
effects.

`WildernessSupportedRewards` changes eight exact reward-row keys in the seven
already-migrated Wave A missions and adds superseding reconstruction evidence.
The original helper bodies remain fixed. `Down` restores only those keys and
removes only its evidence; it never rewrites all matching medpacks. Twin Pillars
and Ranja Gorge use these tuples directly in their first provider migrations.
No existing character inventory is converted.

The provider evidence-capacity correction keeps all authored provenance text
unchanged. Twenty-seven Wilderness notes exceeded the baseline's 256-character
MySQL column, first occurring in W2. Both W2 wrappers widen storage before
calling the content helper. The later paired `WildernessEvidenceCapacity`
migration retains the additive capacity boundary. Rollback retains `TEXT` capacity,
never truncates notes and does not modify gameplay data or earlier helper bodies.
An ordered offline test checks insertion capacity on both Up and Down paths;
this is not live MySQL acceptance.

Native preview and turn-in tests retain both one-unit fixed rows in `422` and
`434` and verify a total grant of two. Earned-reward tests exercise actual
medpack requests, recovery, healing, rollback, rejection and replay. Stacked
consumable recovery also rejects a repeated completed action, not only a
request for an already-deleted one-unit item.

### Base equipment and quest items

Functional base equipment must have the metadata its runtime consumes, not
just a template/class mapping and preview icon. PR105's `Add_armor_values`
precedes every Wilderness migration and populates `itemtemplate_armor` from
the authoritative native class `itemclass.max_hp`. These values replace the
unreleased floor-of-absorption reconstruction; they do not reconstruct historical
prefixes. `WildernessRewardEquipment` remains an explicit no-op marker in both
directions, so it neither duplicates nor deletes PR105-owned armor rows.

| Template | Native class | PR105 armor (`itemclass.max_hp`) |
| --- | ---: | ---: |
| `20250` Hazmat boots | 13618 | 71 |
| `35486` Reflective boots | 18412 | 95 |
| `26996` Motor Assist boots | 15696 | 54 |
| `20697` Hazmat legs | 13756 | 118 |
| `20399` Hazmat gloves | 13664 | 47 |
| `20846` Hazmat vest | 13802 | 142 |
| `20548` Hazmat helmet | 13710 | 95 |
| `36083` Reflective vest | 18596 | 189 |
| `12827` Hazmat boots | 13483 | 91 |
| `12855` Hazmat gloves | 13511 | 61 |
| `11567` Motor Assist boots | 6495 | 84 |
| `11568` Motor Assist legs | 6498 | 139 |
| `12887` Hazmat helmet | 13543 | 188 |
| `12831` Hazmat boots | 13487 | 141 |
| `12943` Hazmat vest | 13599 | 281 |
| `12915` Hazmat legs | 13571 | 234 |
| `13388` Reflective helmet | 18337 | 250 |
| `35784` Reflective helmet | 18504 | 126 |
| `35933` Reflective legs | 18550 | 158 |
| `13744` Motor Assist legs | 16351 | 167 |
| `28692` Motor Assist gloves | 16251 | 67 |
| `13739` Motor Assist boots | 16201 | 100 |

The level-5 cipher selection uses template `97328`, not `110835`: both map to
native class `25828`, but only `97328` has the weapon metadata needed for
ordinary reload and ammo-requiring ToolCipher use. Validate native equip/use
and armor contribution as well as preview, inventory delivery and reward replay.

Native client tables name item **classes**, not the mission's original server
template/loot assignment. The following existing-template selections are R;
their class identity, display name and stack capacity are N. The companion
artifact records alternate existing templates as well.

| Role | Selected template / native class | Quantity or use |
| --- | --- | --- |
| Saviours dogtags, 421 | 613 / 7547 | One, consumed on delivery |
| Samuel sample vials, 425 | 620 / 7574 | One recovered set; exchange after Samuel |
| Combined sample vials, 425 | 622 / 7576 | One resulting set for Eleanor |
| Alia Das ammunition, 427 | 3786 / 12714 | One shipment from Fulgor |
| Medical supplies, 428 | 686 / 7706 | One; durable damaged state controls spoilage, not a fabricated damaged-item template |
| Survey data, 508 | 741 / 7826 | Five distinct acquisitions, native stack 5 |
| Catalyzer, 549 | 747 / 7981 | One |
| Corman blood sample, 442 | 749 / 7985 | One; analysis must be real interaction |
| Machina remains, 574 | 753 / 7991 | Ten |
| Stim Dust, 623/791 | 2226 / 10095 | Three, native stack 3; deliveries and betrayal consume real stock |
| Sonic Emanator, 436 | 2230 / 10128 | One, place/activate; not a lethal substitute |
| Thrax hearts, 479 | 2285 / 10346 | Twelve, native stack 12 |
| Tinctu herb, 695 | 2326 / 10513 | Five, native stack 5 |
| Codex of Voynich, 696 | 2328 / 10520 | One from Arioch |
| Tinctu essence, 698 | 2355 / 10608 | One; distinguish class 10518's similarly named essence |
| Corman vaccination, 700/820/701 | 2356 / 10611 | Three per batch, one per recipient; separate assignment provenance |
| Thrax blood, 776 | 2524 / 11150 | Ten |
| Shield Drone parts, 771 | 2527 / 11153 | Six |
| Predator parts, 769 | 2531 / 11159 | Three |
| Xanx pincers, 787 | 2532 / 11160 | Four |
| Fithik spleens, 758 | 2533 / 11161 | Ten |
| Lightbender glands, 795 | 2557 / 11317 | Four |

Reconstruction: quest collections have a 100% eligible personal corpse drop,
one item until the target is met. Progress requires acquiring the item.
Use existing assignment-owned item rules for delivery, consumption, abandon,
failure and reconnect. Reports without a verified physical item identity use
durable dialogue/progression facts rather than invented client item IDs.
Do not grant all nearby players arbitrary shared inventory; existing party
credit/loot authority remains authoritative.

The historical source reward-item names are retained below as provenance, not
as claims about implemented effects. The supported consumable table above and
the base-equipment reconstructions determine the actual previews and grants.
Two equipment entries are
reconstructed as a single choice; a single equipment entry is fixed.
Explicitly quantified consumable bundles are fixed; unquantified consumables
use one of each listed item unless the source explicitly says OR. These
quantity/choice rules are R where the original infobox only used separators.
For missions without any recorded item reward, the explicit reconstruction
is no additional inventory-item grant, not a claim that the original reward
was empty. Logos are acquired at shrines rather than duplicated as inventory
rewards. This does not excuse the specific gear promises in 2010/2011 or the
recipe rewards recorded for 1407.

| Missions | Historical source reward item names |
| --- | --- |
| 1407 | Power Bonus recipe and Regen Bonus recipe |
| 1069; 479 | Dynamo Gloves T6 / Prodigy Boots T2; Luminar Vest T6 |
| 1390; 1392/1393 | EMP Bomb OR Basic Med Pack; Dynamo Hazmat Boots / Prodigy Reflective Boots |
| 422; 429 | EMP Bomb + Med Pack; Astra Healing Disc / Vitalius Rifle |
| 428; 421; 427 | Prodigy Repair Tool / Dynamo Cipher Tool; ChiTech Chaingun / Pulsar Boots; Phoenix Legs T6 |
| 431; 433; 434; 432 | Olympia Helmet / Vitalius Chaingun; Olympia Gloves / ChiTech Shotgun; Med Pack + EMP Bomb; Basic Med Pack + EMP Grenade |
| 506; 436; 508 | Pulsar Helmet / Olympia Boots; Advanced Med Pack + EMP Grenade; Luminar Hazmat Legs / Reflective Legs |
| 665; 430; 549; 441 | Olympia Legs / Gloves; ChiTech Leech Gun / Olympia Boots; Res Trauma Kit OR Fragmentation Grenade; Fragmentation Grenade OR Basic Med Pack |
| 442; 444; 623; 791 | AccuMax Rifle / Eclipse Shotgun; Pulsar Gloves / Legs; Fire Resistance modification; EMP Bomb + Adrenaline Booster |
| 570; 574; 666; 697 | AccuMax Chaingun / Pulsar Vest; Pulsar Helmet / Olympia Vest; Olympia Legs / Pulsar Gloves; Luminar Boots / AccuMax Pistol |
| 425; 451; 700/820; 860 | Pathogex Boots / Gloves; Astra Healing Disc / Dynamo Salvage Tool; Teleract Helmet / Olympia Legs; Vextronics Rifle / Luminar Legs |
| 758; 787; 769 | 2 EMP Bomb + 4 Advanced Med Pack; 2 Res Trauma Kit + 6 Fragmentation Grenade; 6 Standard Med Pack + 2 EMP Bomb |
| 696; 682; 695; 698 | Olympia Helmet / Vextronics Pistol; Luminar Boots / Vextronics Shotgun; Olympia Helmet / Vest; Olympia Boots / Luminar Vest |

### Bounded opening reward lookup

The native naming code resolves an item from its base template/class and its
highest-priority loot module (`gameuiutil.py:GetItemName`, lines 339-364).
**Dynamo, Prodigy and Luminar are module-driven names, not unique template
aliases.** The selected regular-family identities are:

| Prefix | Selected module ID | Native variant / tooltip | Native effect name |
| --- | ---: | --- | --- |
| Dynamo | 100008 | 5 / 1707 | Power Bonus |
| Prodigy | 100068 | 2 / 1704 | Mind Bonus |
| Luminar | 100028 | 12 / 1714 | Light Resistance |

Other native module IDs produce the same prefixes. Selecting these particular
regular-family IDs is R; it does not recover an original hidden roll or prove
their modifier strength. The older public `T6`/`T2` item labels are also not
exact current-client names: both current Hazmat and Reflective class families
use `Armor_T2` internal names. The armor choices below remain **explicit
reconstruction**, not claims that a legacy tier alias was solved. This table
preserves the researched historical candidates; the implementation selection
is stated separately below.

| Mission / source reward | Existing template / class | Native module identity | Decision |
| --- | --- | --- | --- |
| 1407 Power Bonus recipe | 122978 / 29879 | Native finished item named Armor Module: Power Bonus [1] | Not selected; a module item does not recover a functional recipe |
| 1407 Regen Bonus recipe | 122983 / 29879 | Native finished item named Armor Module: Regen Bonus [1] | Not selected; a module item does not recover a functional recipe |
| 1069 Dynamo Gloves T6 | 20399 / 13664, Hazmat Armor Gloves | 100008 | Choose one armor alternative |
| 1069 Prodigy Boots T2 | 35486 / 18412, Reflective Armor Boots | 100068 | Choose one armor alternative |
| 479 Luminar Vest T6 | 20846 / 13802, Hazmat Armor Vest | 100028 | Fixed, one |

The armor candidates are existing uncommon (`quality 3`) class variants in
the 05-to-08 class band, chosen as introductory specialist/soldier equipment
rather than inferred aliases from unrelated World items. Their existing skill
requirements are Hazmat `30/1` and Reflective `21/1`. These are verified links
and base names; the specific family/template/module selections are R. Do not
add unverified extra modifiers.

**1407 has a recipe-reward/version discrepancy.** All 160 native
fabrication recipes were checked. `spGenShared_RecipeModuleEnhancement` and
`spGenShared_ModuleClassCrafting` have zero rows, and none of the 4,852 existing
templates using Modification Schematic class 25304 belongs to that recipe set.
The Power/Regen tooltip names survive, but there is no executable matching
recipe identity in the examined target data. The older module recipes may be
obsolete rather than unresolved fabrication IDs. Finished module items 122978
and 122983 are real current-client assets, but they do not recover those recipes.

**Coordinator-owned reward capability gap:** `MissionRewardItemEntry` has no
loot-module field. `ItemInfoPacket` writes both module lists empty, and
`ItemTemplateTooltipInfoPacket` writes `None` for module IDs. A bare base
template does not produce the historical armor prefix or effect. The current
rollout does not add a separate item-modifier subsystem or pretend to implement
those effects through cosmetic names.

**Current implementation reconstruction (R):** the rollout uses functional
replacements within the existing item system rather than adding a modifier
subsystem.
Mission 1407 grants schematic 641 (Standard Grade Cartridge Ammunition) and
45072 (Class I Basic Med Pack), one each. Mission 1069 offers base Hazmat Armor
Gloves 20399 or Reflective Armor Boots 35486; mission 479 grants base Hazmat
Armor Vest 20846. The previews and granted items use their real base names,
without Dynamo/Prodigy/Luminar modifiers. This is an explicit fidelity
limitation, not recovered retail reward data or a user-approved reward choice.
The original candidates above remain recorded for a future modifier-capable
reward revision.

The artifact's `openingRewardResolution` records complete native name families,
hashes, the empty recipe sets, and the selected reconstruction tuples.
2010/2011's separate native class-gear promise is still not a recovered gear
list and cannot be silently reduced to only reconstructed XP/credits.

Missing-XP reconstructions use same-chain parity for 771 (4,000), or five times
sourced credits for 444 (6,000), 697 (8,000), and 696 (7,500). 425 uses that rule
rounded to the nearest 500 (7,000). 1741's 600 credits match the sourced
liaison-handoff scale; native-only training/gear introductions use 1,000 XP /
200 credits. All are explicit authoring choices, not historical reward claims.

## Excluded and conditional inventory

Every native candidate not in the outdoor tables is listed here. Its complete
IDs and text/conversation metadata are still retained in the test snapshot.

| Disposition | ID / title | Evidence and boundary |
| --- | --- | --- |
| Retired | 751 Boargar Acquisition | S explicit removed-as-of-Deployment-11.6 banner |
| Retired | 780 Treelurker Samples | Same explicit retirement; do not revive the Munson chain |
| Retired | 767 Mighty Miasma | Same explicit retirement; contradictory old prerequisite need not be fabricated |
| Retired, source-only title | Moving Up to the Big League, **no matched ID** | Historical tutorial bridge superseded by merged Bootcamp; no title in all native missions |
| Instance-dependent | 450 The Dead Live | Native objective 4/body 6764 and package 23 place Velns in Crater Lake |
| Instance-dependent | 1054 We Be Jammin' | N Crater Lake jammers; S instance zone |
| Instance-dependent | 1059 Ghost in the Machine | N/S Crater Lake chain; its outdoor dogtag return does not remove the instance |
| Instance-dependent | 446 Mending A Broken Heart | Outdoor objective locations, but S requires the genuine 1059 instance completion |
| Instance-dependent | 489 Centrifuge Fuel | S five crystals inside Crater Lake |
| Instance-dependent | 593 The Escapist | N/S Nylla inside Pravus; preserve outdoor 574 before this boundary |
| Instance-dependent | 1075 A Higher Calling | N Nylla's post-rescue armband; source remains inside Pravus |
| Instance-dependent | 706 The Fate Of The Callisto | Native opening 3270/recorder 3274 locate wreck/survivors at Guardian Prominence |
| Instance-dependent | 859 To the Caves | S final chamber of Donn, not an outdoor reconnaissance substitute |
| Instance-dependent | 960 Logos: Movement, Around, Chaos | S Crater Lake; native objectives 4/5/6 |
| Instance-dependent | 923 Logos: Feeling, Trap, Heal | S Donn; wiki "Healing" is the same Logos as native "Heal" |
| Instance-dependent | 924 Logos: Communication, Control, Machine | S Pravus; native objectives 6/7/8 |
| Instance-dependent | 2012 The Epic Gauntlet 1 | N separate ten-room/six-player operation behind Alia Das; no public match does not make it outdoor |
| Other-zone | 777 Soldier's Blood | N log 3770 targets Hansen/Foreas Base; outdoor mission is 776/Ojy |
| Other-zone | 1200 Survey Says | N log 9410 targets Amidon/Outpost Condor; outdoor mission is 508 |
| Other-zone | 807 Encroaching on Thoria Das | N log 3974 targets Thoria Das |
| Other-zone | 836 Incoming! | N body 6738 targets Divide/Nidu Dav; S minimum level 11 |
| Other-zone | 1742 Report to Liaison Brice | N objective/body 17004/17005 targets Divide |
| Other-zone | 1047 Trinity Bridge Logos: Future | N Guardian Prominence then Vogren's Tomb/Plateau; also instance-dependent |
| Other-zone | 1152 Stray Cats | N Staal/Fort Intrepid; dialogue 9108 only reminisces about Wilderness |
| Other-zone, protected | 1995 Calling for Reinforcements | Existing Bootcamp normal route; Rogers is a destination, not permission to replace it |
| Other-zone, protected | 2005 Calling for Reinforcements | Existing Bootcamp retry route; keep its existing revision |
| Other-zone event | 1998 Time Capsule | N capsule on Divide and report at Foreas Base |
| Other-zone event | 2006 Soyuz Salvage: Palisades | N title/location |
| Other-zone event | 2007 Soyuz Salvage: Plateau | N title/location |
| Other-zone event | 2008 Soyuz Salvage: Howling Maw | N title/location |
| Other-zone event | 2009 Soyuz Salvage: Marshes | N title/location |
| Other-zone | 2016 A Mystery Unearthed | N/S Bailey, Fort Defiance, Plateau/Valverde |
| Other-zone | 2017 Symbolism | N/S Pools and Marshes before Enigma/Twin Pillars; partial return is not an outdoor-only chain |
| Conditional event | 1999 Soyuz Salvage: Wilderness | N definitively locates debris in Wilderness and Armstrong at Twin Pillars; event activation is not established |
| Conditional event | 1769 Companion Delivery | N pre-order account entitlement, no native objectives; not a normal quest prerequisite |
| Source-only unmatched | 1167 The Final Assault | N old suicide-assault presentation and Alia Das debrief, but no reliable later-client map-1220 binding or confirmed retirement |
| Source-only unmatched title | Hoping for the Best, **no matched ID** | S Twin Pillars/Ryans; absent from all native titles and no Ryans/Hoping string in native mission language |

The public Casper arc is therefore
`450 -> [1054 -> 1059, Crater Lake] -> 446`, not an invented outdoor shortcut.
Likewise 769 and 696 remain outdoor: an optional Crater Lake farming route or
a nearby Guardian Prominence entrance is not a required instance dependency.

## Verification boundaries and handoff

`WildernessContentInventoryTests` compares this independent native snapshot
against **actual migrated World rows** and authored native dialogue references.
It also checks exclusions and preserves the existing Bootcamp definition
identities. It does not assert one freshly constructed constant against itself.
The recovered-membership check compares the frozen test expectations with the
existing production catalog, including distinct-set rule types and still-null
cave bindings; it is explicitly not an operational-content test.
The historical W0 run below reported missing opening migrations as inconclusive.
Later source-shape checks compare the installed providers with those same
frozen identities; they do not turn that historical result into gameplay
acceptance.

`WildernessCoverageTests` is the final enabled-set gate: exactly the64 reconciled
outdoor IDs plus the five protected Bootcamp definitions, all operational.
`WildernessProgressionAcceptanceTests` separately pins the39-mission Wave A
boundary and upgrades a real W1 World while retaining active1449 assignment
identity, objective counters, character flags and inventory. Conditional Soyuz
and preorder content remain disabled;1167 remains source-only unmatched rather
than being relabeled retired, and706 retains its instance dependency.

These inventory checks complement the hub tests, which exercise actual Game
entry points for exact collection thresholds, ordered conversations,
distinct-object credit, both moral outcomes, escort death/failure, deadline
boundaries, abandonment, inventory-full reward retries and reconnect.
`TestCategory=WildernessG2` includes the full Quarantine-to-Beacham chain and
its branch, radio and public-actor guards. Final hub fixtures use ordinary
latest provider data rather than helper reapplication. Native UI and live MySQL
remain separate acceptance surfaces.

The coordinator owns runtime changes, migrations, shared helpers and the single
validation lane. On 2026-09-28 the coordinator granted the native worker the
exclusive lane for:

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --configuration Release --no-restore --filter FullyQualifiedName~WildernessContentInventoryTests --logger "trx;LogFileName=w0-native-inventory.trx" --results-directory C:\Users\johmil\.copilot\session-state\1a1182d3-4def-484d-9999-e9655048b1cf\files\validation --verbosity quiet
```

That run compiled successfully and exited 0: **3 passed, 0 failed, 2
skipped/inconclusive, 5 selected**, with a reported test duration of 24 seconds.
The skipped checks explicitly report missing migrated definitions for
1407/1069/479/1449 and missing 1407 opening objectives. They do not establish
operational completeness. No owned code fix was required; full details and
individual outcomes are in `w0-native-report.md` and
`validation\w0-native-inventory.trx` in the session artifacts.

The baseline ignores `docs/*`, but a subsequent coordinator-owned shared-checkout
change adds `!docs/wilderness-missions.md`. Keep that scoped exception with the
document in a later authorized commit. This native worker did not change ignore
rules, stage files, or commit.

The remaining evidence limits are original equipment modifiers/hidden rolls,
the 1407 recipe-version discrepancy, historical class-kit selections, original
cave-to-child bindings and volumes, original timer durations, and the
four-versus-six text contradiction in 860. The supported replacements,
class-kit choices, exact story roster, radio choices and timer reconstructions
are fixed in the deployed definitions; they are not claims of recovered retail
server values. Instance-dependent `1449` work and its clone/title finale remain
incomplete on the same durable assignment. Native-client and live-MySQL
acceptance are **unperformed**. Invented client text, fake instance completion
or omission of promised quantities is not an acceptable substitute.
