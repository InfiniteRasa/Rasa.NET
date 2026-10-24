# Plateau missions: sources (cross-reference)

The 66 missions of the Plateau quest PR, taken from the full mission cross-reference of the 1.16.5.0
client. For each mission: the client text and objectives, the sources found (TaRapedia, TR MMO DB, Ellatha,
Ten Ton Hammer, archived fan and official pages, patch notes) and a rebuild summary with the source of each
value. Values without a source are marked ESTIMATE. "This PR" rows (where present) show the data this PR adds.

Playtest status and the deviations from the original game: `docs/plateau-playtest.md`.

| Zone | Missions |
| --- | ---: |
| Maligo Base | 3 |
| Plateau | 61 |
| Pools | 1 |
| Ustor Yard | 1 |

## Maligo Base

### 876 — Showdown


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone (low confidence): Maligo Base missions with a sourced level are at levels 30, 30, 30 — derived from 3 missions in this file; too few or too spread for a range
  - reward: 17500 XP; 1200 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Showdown)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Maligo Base only
- **Client:** Colonel Bosley has received additional intel from the recovering AFS troop which indicates that a Bane officer may have a keypass permitting access to the second level of Maligo. Locate and eliminate this target on sight. Radio back to the Colonel once the target has been taken out.
  - objectives (client): Bane Officer located! Track down and eliminate him; ITEM - Obtain Level 1 Access Keypass
- **TaRapedia** [Showdown](https://tabularasa.fandom.com/wiki/Showdown): giver: Radio; at: Maligo Base; zone: Maligo Base; follow-up: Coup; XP: 17500; credits: 1200; reward from: Radio
  - overview: Colonel Bosley has received additional intel from the recovering AFS troop which indicates that a Bane officer may have a keypass permitting access to the second level of Maligo. Locate and eliminate this target on sight. Radio back to the Colonel once the target has been taken out.
  - objectives: • Bane Officer located! Track down and eliminate him • ITEM - Obtain Level 1 Access Keypass
  - walkthrough: Watchman Zyrutha spawns at 232, 106. Kill him for the key.
- **TaRapedia mission list** [Maligo Base mission list](https://tabularasa.fandom.com/wiki/Maligo_Base_mission_list): zone Plateau, given by Headquarters
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-3886.html](https://web.archive.org/web/20090116224305/http://planettr.com:80/forums/archive/index.php/t-3886.html): “This was really funny yesterday, i got stuck outside the map of Maligo Base. It was at the bit where the third level closest to the Bane boss for the Showdown mission was, and i was trying to climb over a few chemical containers when i ran out of the map... i was on top of the world! Literally! Just thought to say this, as it wasn't a fun time trying to get back in. Thank god for /stuck!”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “that prevented the detonators from respawning after re-taking the mission. / Minor typos corrected on the completion text of the Maligo Base mission ‘Showdown’. / Minor grammar corrections done to opening text of the Maligo Base mission ‘Showdown’”
- **WarCry TR** [tr.warcry.com:80/forums/read/191.53430](https://web.archive.org/web/20080202212646/http://tr.warcry.com:80/forums/read/191.53430): “hat prevented the detonators from respawning after re-taking the mission. / * Minor typos corrected on the completion text of the Maligo Base mission 'Showdown'. / * Minor grammar corrections done to opening text of the Maligo Base mission 'Showdown'”
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named in client text)

- **Chain** (explicit prerequisite links): Showdown chain (2 missions), step 1
  - Leads to: Coup (id 878) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Coup), [TaRapedia](https://tabularasa.fandom.com/wiki/Showdown)

### 878 — Coup [instance]


- **Instance:** Maligo Base — NPC Downed Prisoner (named as giver/receiver) is in Maligo Base — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Downed_Prisoner)
- **Rebuild summary:**
  - level: 30 — source: [TR MMO DB](https://web.archive.org/web/20120731174424/http://tr.mmodb.com/quests/coup-252.php)
  - level ESTIMATE from zone (low confidence): Maligo Base missions with a sourced level are at levels 30, 30, 30 — derived from 3 missions in this file; too few or too spread for a range
  - reward: 50000 XP; 1000 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Coup)
  - reward: 1000 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20120731174424/http://tr.mmodb.com/quests/coup-252.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Downed Prisoner: Maligo Base, /loc 378, -22, -179 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Downed_Prisoner)
- **Client:** A wounded prisoner has informed you of danger to others being held captive. You must locate Squad Leader Altera and help in any way you can.
  - objectives (client): Locate and speak with Squad Leader Altera; Find Ammunition and Supplies for the prisoners to the south — Scavenge the storage rooms for supplies and search downed combatant's corpses for ammunition that can be used by the freed prisoners that are barricaded to the …; Storm the control room to the West and release the prisoners.; Regroup back with Squad Leader Altera; Report to Information Specialist Neuman
- **TaRapedia** [Coup](https://tabularasa.fandom.com/wiki/Coup): giver: Downed Prisoner; at: Maligo Base; zone: Maligo Base; requires: Showdown; XP: 50000; credits: 1000; reward from: Squad Leader Alter
  - overview: A wounded prisoner has informed you of danger to others being help captive. You must locate Squad Leader Alter and help in any way you can.
  - objectives: • Locate and speak with Squad Leader Alter. • Find Ammunition and Supplies for the prisoners to the south. • Regroup back with Squad Leader Alter. • Storm the control room to the West and release the prisoners.
  - walkthrough: Walkthrough
- **TaRapedia mission list** [Maligo Base mission list](https://tabularasa.fandom.com/wiki/Maligo_Base_mission_list): zone Plateau, given by Downed Prisoner
- **TR MMO DB** [Coup](https://web.archive.org/web/20120731174424/http://tr.mmodb.com/quests/coup-252.php): level: 30; starts at npc: Downed Prisoner; starts in zone: Plateau; objectives: Locate and speak with Squad Leader Alter, Find Ammunition and Supplies for the Prisoners to the South, Regroup with Squad Leader Alter, Storm the Control Room to the west and release the prisoners; reward: 1000
  - related mob Squad Leader Alter (not in the archived bestiary pages)
- **NPCs (TaRapedia NPC pages):**
  - [Downed Prisoner](https://tabularasa.fandom.com/wiki/Downed_Prisoner): Maligo Base, /loc 378, -22, -179 (named as giver/receiver)
  - [Squad Leader Alter](https://tabularasa.fandom.com/wiki/Squad_Leader_Alter): Maligo Base, Maligo Base, /loc 234, -22, -125 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Showdown chain (2 missions), step 2
  - Requires: Showdown (id 876) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Coup), [TaRapedia](https://tabularasa.fandom.com/wiki/Showdown)

### 879 — Self Preservation


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone (low confidence): Maligo Base missions with a sourced level are at levels 30, 30, 30 — derived from 3 missions in this file; too few or too spread for a range
  - reward: 40000 XP; 1000 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Self_Preservation)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Maligo Base only
- **Client:** Colonel Bosley has informed you of a series of security CPUs that are online somewhere on the northern portion of the first floor. Locate and eliminate all of the CPU units you come across.
  - objectives (client): Destroy all Security CPU Units
- **TaRapedia** [Self Preservation](https://tabularasa.fandom.com/wiki/Self_Preservation): giver: Radio; at: Maligo Base; zone: Maligo Base; XP: 40000; credits: 1000; reward from: Radio
  - overview: Colonel Bosley has informed you of a series of security CPUs that are online somewhere on the northern portion of the first floor. Locate and eliminate all of the CPU units you come across.
  - objectives: • Destroy all Security CPU Units.
  - walkthrough: Walkthrough
- **TaRapedia mission list** [Maligo Base mission list](https://tabularasa.fandom.com/wiki/Maligo_Base_mission_list): zone Plateau, given by Headquarters
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named in client text)

## Plateau

### 555 — Behind Enemy Lines


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4850 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4,850 credits; lvl 30; lvl 29; lvl 28; lvl 29 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Behind_Enemy_Lines)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
- **Client:** General Thaddeus T. Bailey wants you to use the secret tunnel to sneak behind enemy lines, locate the AFS MASH unit at Camp Resistance, and report to Colonel Bosley, the camp's C.O.
  - objectives (client): Report to Col. Bosley. — Locate Camp Resistance and report to Colonel Bosley.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau))
  - reward: 40000 XP; 4850 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest
  - prerequisites: Requires completed mission 867
- **TaRapedia** [Behind Enemy Lines](https://tabularasa.fandom.com/wiki/Behind_Enemy_Lines) [stub]: giver: Headquarters; at: Radio; zone: Plateau; requires: The Stainless Steel Bloodhound; follow-up: None; XP: 40,000; credits: 4,850; items: lvl 30; lvl 29; lvl 28; lvl 29; reward from: Colonel Bosley
  - overview: Objectives: • Report to Col. Bosley.
  - objectives: • Report to Col. Bosley.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Headquarters
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)
  - [General Thaddeus T. Bailey](https://tabularasa.fandom.com/wiki/General_Thaddeus_T._Bailey): Plateau, Fort Defiance, /loc -68, 404, 949 (named in client text)

- **Chain** (explicit prerequisite links): Lookout Down Below chain (3 missions), step 3
  - Requires: The Stainless Steel Bloodhound (id 867) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Behind_Enemy_Lines), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 557 — Cryptic Messages


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cryptic_Messages); 30 — source: [TR MMO DB](https://web.archive.org/web/20081202023242/http://tr.mmodb.com/quests/cryptic-messages-247.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Legs, Stealth Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cryptic_Messages)
  - reward: 4800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081202023242/http://tr.mmodb.com/quests/cryptic-messages-247.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Comm Officer Devinchy: Plateau, Wedge Rock Outpost, /loc 363, 369, 360 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Comm_Officer_Devinchy)
  - location: Preceptor Vorvaak (Thrax Soldier), zone Plateau — North of the central Trinity Bridge — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Preceptor+Vorvaak&id=97)
- **Client:** Retrieve a Bane Decryption Device for Communications Officer Devinchy at Wedge Rock Outpost.
  - objectives (client): Speak to Comm Officer McKinley. — Ask Comm Officer McKinley at Wedge Rock Outpost where you might locate a Bane Decryption Device.; Acquire a Decryption Device. — Locate Thrax Preceptor Vorvaak somewhere near Trinity Bridge and obtain his Bane Decryption Device.; Return to Devinchy. — Bring the Decryption Device back to Comm Officer Devinchy at Wedge Rock Outpost.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Comm Officer Devinchy
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Legs, Stealth Armor Legs
- **TaRapedia** [Cryptic Messages](https://tabularasa.fandom.com/wiki/Cryptic_Messages) [stub]: giver: Comm Officer Devinchy; at: Wedge Rock Outpost; zone: Plateau; level: 32 (possibly less); requires: None; follow-up: None; XP: 45,000; credits: 4,800; items: Equipment; reward from: Headquarters
  - overview: Retrieve a Bane Decryption Device for Communications Officer Devinchy at Wedge Rock Outpost.
  - objectives: Speak to Comm Officer McKinley. • Ask Comm Officer McKinley at Wedge Rock Outpost where you might locate a Bane Decryption Device.; Acquire a Decryption Device. • Locate Thrax Preceptor Vorvaak somewhere near Trinity Bridge and obtain his Bane Decryption Device. • 0/1; Return to Devinchy. • Bring the Decryption Device back to Comm Officer Devinchy at Wedge Rock Outpost.
  - walkthrough: Preceptor Vorvaak (Thrax Soldier) wanders the Eloh Forum around coordinates -70, 535. Generally he spawns just in front (north) of the Eloh bridge to the east (of the three), but I have found him wandering on the south side of the bridge on occasion.
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Comm Officer Devinchy
- **TR MMO DB** [Cryptic Messages](https://web.archive.org/web/20081202023242/http://tr.mmodb.com/quests/cryptic-messages-247.php): level: 30; starts at npc: Comm Officer Devinchy; starts in zone: Plateau; objectives: Speak to Comm Officer McKinley, Speak to General Thaddeus T. Bailey; reward: 4800
  - related mob Comm Officier McKinley: level 33, Plateau — source: [TR MMO DB bestiary](https://web.archive.org/web/20071015135029/http://tr.mmodb.com:80/bestiary/comm-officier-mckinley-158.php)
- **Where (locations / coordinates):**
  - Preceptor Vorvaak (Thrax Soldier), zone Plateau — North of the central Trinity Bridge — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Preceptor+Vorvaak&id=97)
- **NPCs (TaRapedia NPC pages):**
  - [Comm Officer Devinchy](https://tabularasa.fandom.com/wiki/Comm_Officer_Devinchy): Plateau, Wedge Rock Outpost, /loc 363, 369, 360 (named as giver/receiver)
- **Ellatha boss NPC** [Preceptor Vorvaak](https://www.ellatha.com/tr/bossnpcview.asp?key=Preceptor+Vorvaak&id=97): Thrax Soldier; zone Plateau; North of the central Trinity Bridge; needed for: Cryptic Messages Plateau Targets of Opportunity (Ellatha lists this mission under "Needed for Quests")

### 713 — A Bridge Too Far


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB](https://web.archive.org/web/20120731174048/http://tr.mmodb.com/quests/a-bridge-too-far-238.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 6200 credits; choose 1 of: Bio Armor Legs, Mech Armor Vest, Stealth Armor Boots, Reflective Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 6,200 credits; or; or; or — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far)
  - reward: 6200 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20120731174048/http://tr.mmodb.com/quests/a-bridge-too-far-238.php)
  - reward: “The Juggernaut patrols the northern end of the Eloh Bridge around -170, 374, 322 and once you've retrieved your CPU head back to Peterson who will reward you with 6200 credits and your choice of Cryogen Bio Armor Legs, …” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - reward: “You'll be rewarded with 4650 credits and your choice of a Vextronics Pulse Disperser, ChiTech Cryogenic Injector Gun, Vextronics Incendiary Leech Gun, or Animatics Incendiary Shotgun.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - drops / mission items: TIP: THe Prototype Juggernaut CPU can be found on the north side of Trinity Bridge — source: [TR MMO DB](https://web.archive.org/web/20120731174048/http://tr.mmodb.com/quests/a-bridge-too-far-238.php)
  - location: NPC Field Lt. Peterson: Plateau, Fort Defiance, /loc -66, 401, 931 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Lt._Peterson)
  - location: Prototype Juggernaut (Juggernaut), zone Plateau — North of the central Trinity Bridge — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Prototype+Juggernaut&id=98)
  - location: Peterson at -66, 401, 928. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **Client:** Join the battle at Trinity Bridge and recover the CPU from the latest model of Bane Juggernauts. When you retrive it, deliver it to Field Lt. Peterson at Fort Defiance. Peterson also recommends grouping with other soldiers before making the attempt.
  - objectives (client): Retrieve a Prototype Juggernaut CPU. — Retrieve the CPU from the remains of a Prototype Bane Juggernaut.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Field Lt. Peterson
  - reward: 45000 XP; 6200 credits; choose 1 of: Bio Armor Legs, Mech Armor Vest, Stealth Armor Boots, Reflective Armor Gloves
  - prerequisites: Requires completed mission 887
- **TaRapedia** [A Bridge Too Far](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far): giver: Field Lt. Peterson; at: Fort Defiance; zone: Plateau; requires: The Defiant Ones; follow-up: The Beer Hunter; XP: 45,000; credits: 6,200; items: or; or; or; reward from: Field Lt. Peterson
  - overview: • Join the battle at Trinity Bridge and recover the CPU from the latest model of Bane Juggernaut Prototype. When you retrieve it, deliver it to Field Lt. Peterson at Fort Defiance.
  - objectives: • Retrieve a .
  - walkthrough: The Prototype Juggernaut is usually on the north side of Trinity Bridge, right in the middle. Occasionally it spawns on the south side of the Trinity Bridge. It's no more difficult than the random Juggernauts that spawn in the area. The only difficulty is that the AFS NPCs in the area might steal your kill, so be sure to out-damage them.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Field Lt. Peterson
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “A Bridge Too Far - Named after the classic World War II movie covering Operation Market Garden (we educate also here at Ten Ton Hammer) this mission is given by Field Lt. Peterson at -66, 401, 928. His task isn't difficult, just retrieve the CPU from a friggin Prototype Juggernaut. Oh, don't forget your tank. If you are a class who can drop machinery such as a Sniper or Spy, …”
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “A Bridge Too Far. Peterson is throwing his buddy a bachelor party and needs you to score him some beer. He'll send you to the Tavern which is located east to talk with Bartender McLaughlin. McLaughlin wants 10 Saccharo roots to make what you need. Head out in the field and locate 10 to harvest. I found all mine near -136, 373, 436 and while you may not see many, they do …”
- **Ten Ton Hammer** [guide-to-eloh-vale](https://www.tentonhammer.com/guides/guide-to-eloh-vale): “A Bridge Too Far! Mining for Information You receive this mission from Base Commander Marquette inside Divide's Foreas Base Headquarters. You need to find the information inside the Timora Mines Instance in the southern part of Concordia Divide. Follow this guide for help in progressing this mission. When you've finished Timora Mines, return to Foreas Base and receive Map …”
- **TR MMO DB** [A Bridge Too Far](https://web.archive.org/web/20120731174048/http://tr.mmodb.com/quests/a-bridge-too-far-238.php): level: 30; starts at npc: Field Lt. Peterson; starts in zone: Plateau; objectives: Retrieve a Prototype Juggernaut CPU, Return to Field Lt. Peterson; description: TIP: THe Prototype Juggernaut CPU can be found on the north side of Trinity Bridge; reward: 6200; series: The Defiant Ones
- **TR MMO DB quest series** [The Defiant Ones](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/): A Bridge Too Far (level 30, reward 6200) → The Beer Hunter (level 30, reward 4650) → The Defiant Ones (level 30, reward 0) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - Prototype Juggernaut (Juggernaut), zone Plateau — North of the central Trinity Bridge — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Prototype+Juggernaut&id=98)
  - Peterson at -66, 401, 928. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - The Juggernaut patrols the northern end of the Eloh Bridge around -170, 374, 322 and once you've retrieved your CPU head back to Peterson who will reward you with 6200 credits and your choice of Cryogen Bio Armor Legs, CryoGen Mech Armor Vest, CryoGen Stealth … — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - I found all mine near -136, 373, 436 and while you may not see many, they do respawn quickly. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **NPCs (TaRapedia NPC pages):**
  - [Field Lt. Peterson](https://tabularasa.fandom.com/wiki/Field_Lt._Peterson): Plateau, Fort Defiance, /loc -66, 401, 931 (named as giver/receiver)
- **Ellatha boss NPC** [Prototype Juggernaut](https://www.ellatha.com/tr/bossnpcview.asp?key=Prototype+Juggernaut&id=98): Juggernaut; zone Plateau; North of the central Trinity Bridge; needed for: A Bridge Too Far Plateau Targets of Opportunity (Ellatha lists this mission under "Needed for Quests")
- **Drops / item sources:**
  - TIP: THe Prototype Juggernaut CPU can be found on the north side of Trinity Bridge — source: [TR MMO DB](https://web.archive.org/web/20120731174048/http://tr.mmodb.com/quests/a-bridge-too-far-238.php)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 2
  - Requires: The Defiant Ones (id 887) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Beer Hunter (id 855) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Beer_Hunter), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Defiant Ones", row 1 of 3 ([list](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 719 — Mind Slayer


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Slayer); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 100000 XP; 6600 credits; choose 1 of: Cryogenic Blade, Cryogenic Disperser, Virulent Injector Gun, Virulent Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 100,000 XP; 6,600 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Slayer)
  - reward: 6600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Agent Perdu: Plateau, Fort Defiance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Agent_Perdu)
  - location: Inquisitor Krakatus (Technician), zone Plateau — Inside Derak Prison — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Inquisitor+Krakatus&id=95)
- **Client:** Infilitrate the Derak Prison Facilty in Desolation Quarry and assassinate Thrax Inquisitor Krakatus. Retrieve his datapad with the schematics for the Bane Mind Control Implants and bring it to Special Agent Perdu at Fort Defiance.
  - objectives (client): Kill Inquisitor Krakatus. — Assassinate Inquisitor Krakatus at the Derak Prison Facility inside Desolation Quarry and retrieve his datapad.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Special Agent Perdu
  - reward: 100000 XP; 6600 credits; choose 1 of: Cryogenic Blade, Cryogenic Disperser, Virulent Injector Gun, Virulent Leech Gun
  - prerequisites: Requires completed mission 936
- **TaRapedia** [Mind Slayer](https://tabularasa.fandom.com/wiki/Mind_Slayer) [stub]: giver: Agent Perdu; at: Fort Defiance; zone: Plateau; level: 32 (possibly less); requires: Mind Bender; XP: 100,000; credits: 6,600; items: Equipment; reward from: Agent Perdu
  - overview: Infilitrate the Derak Prison Facilty in Desolation Quarry and assassinate Thrax Inquisitor Krakatus. Retrieve his datapad with the schematics for the Bane Mind Control Implants and bring it to Agent Perdu at Fort Defiance.
  - objectives: Kill Inquisitor Krakatus. • Assassinate Inquisitor Krakatus at the Derak Prison Facility inside Desolation Quarry and retreive his datapad. • 0/1
  - walkthrough: Krakatus is in the back section of the jail. He was resistant to fire and ice, and died quickly to sonic. Immune to Electric. Died quickly with photonic attacks too.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Agent Perdu
- **TR MMO DB quest series** [Clean Slate](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/): Clean Slate (level 30, reward 0) → Mind Bender (level 30, reward 4800) → Mind Slayer (level 30, reward 6600) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - Inquisitor Krakatus (Technician), zone Plateau — Inside Derak Prison — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Inquisitor+Krakatus&id=95)
- **NPCs (TaRapedia NPC pages):**
  - [Agent Perdu](https://tabularasa.fandom.com/wiki/Agent_Perdu): Plateau, Fort Defiance (named as giver/receiver)
  - [Inquisitor Krakatus](https://tabularasa.fandom.com/wiki/Inquisitor_Krakatus): Plateau (named in client text)
- **Ellatha boss NPC** [Inquisitor Krakatus](https://www.ellatha.com/tr/bossnpcview.asp?key=Inquisitor+Krakatus&id=95): Technician; zone Plateau; Inside Derak Prison; needed for: Plateau Targets of Opportunity Mind Slayer (Ellatha lists this mission under "Needed for Quests")

- **Chain** (explicit prerequisite links): Clean Slate chain (6 missions), step 5
  - Requires: Mind Bender (id 905) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Slayer)
  - Requires: The Results Are In (id 936) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Results_Are_In), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Clean Slate", row 3 of 3 ([list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 855 — The Beer Hunter


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Pulse Disperser, Cryogenic Injector Gun, Incendiary Leech Gun, Incendiary Shotgun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4,650 credits; lvl 30 Vextronics Pulse Disperser or; lvl 27 ChiTech Cryogenic Injector Gun or; lvl 29 Vextronics Incendiary Leech Gun or; lvl 27 Animatics Incendiary Shotgun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Beer_Hunter)
  - reward: 4650 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/)
  - reward: “You'll be rewarded with 4650 credits and your choice of a Vextronics Pulse Disperser, ChiTech Cryogenic Injector Gun, Vextronics Incendiary Leech Gun, or Animatics Incendiary Shotgun.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Field Lt. Peterson: Plateau, Fort Defiance, /loc -66, 401, 931 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Lt._Peterson)
  - location: I found all mine near -136, 373, 436 and while you may not see many, they do respawn quickly. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **Client:** Go to the Fort Defiance Tavern and speak to Bartender McLaughlin about Lieutenant Peterson's bachelor party arrangements.
  - objectives (client): Speak to Bartender McLaughlin. — Speak to Bartender McLaughlin at the Fort Defiance Tavern.; Gather Saccharo Roots. — Gather ten Saccharo Roots from the plants growing around the perimeter of the Eloh Forum near Trinity Bridge.; Return to Bartender McLaughlin. — Bring the Saccharo Roots to Bartender McLaughlin at the Fort Defiance Tavern.; Inform Lt. Peterson. — Inform Lt. Peterson that the party arrangements have been made.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Field Lt. Peterson
  - reward: 40000 XP; 4650 credits; choose 1 of: Pulse Disperser, Cryogenic Injector Gun, Incendiary Leech Gun, Incendiary Shotgun
  - prerequisites: Requires completed mission 713
- **TaRapedia** [The Beer Hunter](https://tabularasa.fandom.com/wiki/The_Beer_Hunter) [stub]: giver: Field Lt. Peterson; at: Fort Defiance; zone: Plateau; requires: A Bridge Too Far; XP: 40,000; credits: 4,650; items: lvl 30 Vextronics Pulse Disperser or; lvl 27 ChiTech Cryogenic Injector Gun or; lvl 29 Vextronics Incendiary Leech Gun or; lvl 27 Animatics Incendiary Shotgun; reward from: Field Lt. Peterson
  - overview: • Go to the Fort Defiance Tavern and speak to Bartender McLaughlin about Field Lt. Peterson's bachelor party arrangements.
  - objectives: • Speak to Bartender McLaughlin • Gather s • Return to Bartender McLaughlin • Inform Field Lt. Peterson
  - walkthrough: Saccharo Roots are little green circular plants that can be found north of Trinity Bridge. They take quite a while to loot, so be sure there are no patrols nearby to shoot you and interrupt.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Field Lt. Peterson
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “The Beer Hunter - This missions begins with Field Lt. Peterson and requires the completion of A Bridge Too Far. Peterson is throwing his buddy a bachelor party and needs you to score him some beer. He'll send you to the Tavern which is located east to talk with Bartender McLaughlin. McLaughlin wants 10 Saccharo roots to make what you need. Head out in the field and locate 10 …”
- **TR MMO DB quest series** [The Defiant Ones](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/): A Bridge Too Far (level 30, reward 6200) → The Beer Hunter (level 30, reward 4650) → The Defiant Ones (level 30, reward 0) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - I found all mine near -136, 373, 436 and while you may not see many, they do respawn quickly. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **NPCs (TaRapedia NPC pages):**
  - [Field Lt. Peterson](https://tabularasa.fandom.com/wiki/Field_Lt._Peterson): Plateau, Fort Defiance, /loc -66, 401, 931 (named as giver/receiver)
  - [Bartender McLaughlin](https://tabularasa.fandom.com/wiki/Bartender_McLaughlin): Plateau, Fort Defiance, /loc 19, 399, 925 (named in client text)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 3
  - Requires: A Bridge Too Far (id 713) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Beer_Hunter), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Defiant Ones", row 2 of 3 ([list](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 867 — The Stainless Steel Bloodhound


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB](https://web.archive.org/web/20081204222709/http://tr.mmodb.com/quests/the-stainless-steel-bloodhound-246.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Virulent Injector Gun, Pulse Chaingun, Pulse Pistol, Laser Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4650 credits; AccuMax Virulent Injector Gun rarity=green level=32; ChiTech Pulse Chaingun rarity=green level=33; Teleract Pulse Pistol rarity=green level=30; Vitalius Laser Cannon rarity=green level=29 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound)
  - reward: 4650 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081204222709/http://tr.mmodb.com/quests/the-stainless-steel-bloodhound-246.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Field Sgt. Garde: Plateau, Valverde Chasm, /loc -103, 269, 183 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Sgt._Garde)
  - location: The bot floats for quite a long way, crosses the stream and eventually finds a cave entrance at -459.6, 273.1, 135.7. — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound)
- **Client:** Rendezvous with Field Sergeant Garde at the teleporter platform deep inside Valverde Chasm.
  - objectives (client): Escort the Sentinel. — Follow the Modified Sentinel on its pre-programmed path to find out how the Bane are infiltrating Valverde Chasm; Tell Garde you're ready. — When you are ready, tell Ranger Garde to activate the Modified Bane Sentinel.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Field Sgt. Garde
  - reward: 40000 XP; 4650 credits; choose 1 of: Virulent Injector Gun, Pulse Chaingun, Pulse Pistol, Laser Cannon
  - prerequisites: Requires completed mission 970
- **TaRapedia** [The Stainless Steel Bloodhound](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound): giver: Field Sgt. Garde; at: Valverde Chasm; zone: Plateau; requires: Lookout Down Below; follow-up: Behind Enemy Lines; XP: 40,000; credits: 4650; items: AccuMax Virulent Injector Gun rarity=green level=32; ChiTech Pulse Chaingun rarity=green level=33; Teleract Pulse Pistol rarity=green level=30; Vitalius Laser Cannon rarity=green level=29; reward from: Radio Mission
  - overview: Rendezvous with Field Sergeant Garde at the telporter platform deep inside Valverde Chasm.
  - objectives: • Tell Garde you're ready. • Escort the Sentinel
  - walkthrough: Once you indicate you are ready, the Sentinel bot will start floating (slooooowly) beside the stream. Follow it and protect it. We ran into little resistance during the escort and the bot never came under fire, so I'm not sure if it is possible to fail due to the bot being destroyed. The bot floats for quite a long way, crosses the stream and eventually finds a cave entrance at -459.6, 273.1, 135.7. You should get a Radio Mission granting you the reward and then another with the follow-up quest, Behind Enemy Lines. We've run this mission several times and the only consistent thig is that it …
- **TaRapedia mission list** [Valverde Chasm mission list](https://tabularasa.fandom.com/wiki/Valverde_Chasm_mission_list): zone Plateau, area Valverde Chasm, given by Field Sgt. Garde
- **TR MMO DB** [The Stainless Steel Bloodhound](https://web.archive.org/web/20081204222709/http://tr.mmodb.com/quests/the-stainless-steel-bloodhound-246.php): level: 30; starts at npc: Field Sgt. Garde; starts in zone: Plateau; objectives: Tell Garde you are ready, Escort the Sentinel Bot; description: When you tell Field Sgt. Garde that you are ready a Sentinel Bot will start moving along the stream. Your goal is to escort it and make sure it isn't destroyed. Eventually you will receive a radio transmission and the quest will complete.; reward: 4650
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “Caves of Donn - What Evil Lurks: Mission now fails if you don’t keep the Foreans alive. / Plateau – “The Stainless Steel Bloodhound” – Mission will now fail if escort dies. / Plateau – “The Stainless Steel Bloodhound” – Escort now only drops junk loot if killed.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_novem …](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007): “Maps and Missions Updates / Fix to Plateau “The Stainless Steel Bloodhound” to correctly spawn escorts for multiple groups, simultaneously. / Fix to Lightbender Sniper spawns in Lower Eloh Creek pillbox on Wilderness.”
- **RGTR official site** [www.rgtr.com/news/patch_notes_public_test/patch_notes_and_known_issues …](https://web.archive.org/web/20081117081928/http://www.rgtr.com/news/patch_notes_public_test/patch_notes_and_known_issues_1_5.html): “Maw should be able to effectively defeat their Swampgrubber prey much more effectively now. / Maps and Missions Updates Fix to Plateau “The Stainless Steel Bloodhound” to correctly spawn escorts for multiple groups, simultaneously . / Fix to Lightbender Sniper spawns in Lower Eloh Creek pillbox on Wilderness.”
- **Stratics TR forum** [boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Number=11987&page= …](https://web.archive.org/web/20071221003621/http://boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Number=11987&page=0&view=collapsed&sb=5&o=&fpart=1): “effectively now. / Maps and Missions Updates / Fix to Plateau The Stainless Steel Bloodhound to correctly spawn escorts / for multiple groups, simultaneously . / Fix to Lightbender Sniper spawns in Lower Eloh Creek pillbox on”
- **Where (locations / coordinates):**
  - The bot floats for quite a long way, crosses the stream and eventually finds a cave entrance at -459.6, 273.1, 135.7. — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound)
- **NPCs (TaRapedia NPC pages):**
  - [Field Sgt. Garde](https://tabularasa.fandom.com/wiki/Field_Sgt._Garde): Plateau, Valverde Chasm, /loc -103, 269, 183 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Lookout Down Below chain (3 missions), step 2
  - Requires: Lookout Down Below (id 970) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Behind Enemy Lines (id 555) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Behind_Enemy_Lines), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 877 — One Way Trip [instance]


- **Instance:** Maligo Base — client objective/log text: "Search Maligo Base for the location of the prototype Wormhole device that the Bane are working on."
- **Instance:** Ustor Yard — client objective/log text: "Gain Access to Maligo Bridge Through Ustor Yard's Western Exit."
- **Rebuild summary:**
  - level: 33 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip); 25 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 90,000 XP; 8,000 credits; lvl 30; lvl 30; lvl 30; lvl 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
- **Client:** Secure Ustor Yard to gain access to the Maligo Base bridge. Once inside, locate the Bane Wormhole Prototype and destroy it. Report to Colonel Bosley at Camp Resistance when the job is done.
  - objectives (client): Locate the Bane Wormhole Device — Search Maligo Base for the location of the prototype Wormhole device that the Bane are working on.; Power down Wormhole Device — Disable power to the Wormhole device by locating its powercube and placing it into the standby powercube socket.; Determine why there are Bane and Prisoners slaughtered — Search the second level for any survivors and determine what caused the massacre.; ITEM - Obtain Level 3 Access Keypass; Gain Access to Maligo Bridge Through Ustor Yard's Western Exit.; Rendezvous With Field Lt. Rapp in Ustor Yard.; Find out how to power down the wormhole — Speak with Demolitionist Maybauer to determine the best way to powerdown the wormhole.; Destroy the wormhole prototype — Go down into the wormhole chamber and place a bomb, destroying it before any mind controlled prisoners make their way into the device.; Return to Colonel Bosley; Wormhole Powercube obtained … (+2 more)
- **TaRapedia** [One Way Trip](https://tabularasa.fandom.com/wiki/One_Way_Trip): giver: Colonel Bosley; at: AFS Camp Resistance; zone: Plateau; level: 33 (possibly less); requires: Blueprint For Victory; follow-up: South of the Border; New Orders From The General; XP: 90,000; credits: 8,000; items: lvl 30; lvl 30; lvl 30; lvl 30; reward from: Colonel Bosley
  - overview: Secure Ustor Yard to gain access to the Maligo Base bridge. Once inside, locate the Bane Wormhole Protoype and destroy it. Report to Colonel Bosley at Camp Resistance when the job is done.
  - objectives: • Rendezvous With Field Lt. Rapp in Ustor Yard. • Gain Access to Maligo Bridge Through Ustor Yard's Western Exit. • Locate the Bane Wormhole Device • Determine why there are Bane and Prisoners slaughtered • Find out how to power down the wormhole • Power down Wormhole Device • ITEM - Obtain Level 3 Access Keypass • Maligo Base: Level 3 Keypass 0/1 • Speak with Demolitionist Maybauer for further …
  - walkthrough: After patch 1.4, this mission needs to be done in a particular order, otherwise the progression will stall and the mission cannot be completed. Here's the order you need to do it in. • DO NOT cross the security beams near “Special Projects” in the Level 01:Service Corridor (this triggers the completion of “Locate the Bane Wormhole Device”) • First Speak to Altera to “Determine why there are Bane and Prisoners slaughtered (NOTE: DO NOT speak to Neuman to complete this objective) • Then Speak to Neuman (not Maybauer) to “Find out how to power down the wormhole” • NOW “Locate the Bane Wormhole …
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **TaRapedia mission list** [Maligo Base mission list](https://tabularasa.fandom.com/wiki/Maligo_Base_mission_list): zone Plateau, given by Colonel Bosley
- **IGN TR boards** [vnboards.ign.com/tabula_rasa_general_board/b22750/106585290/r106594805 …](https://web.archive.org/web/20080518034441/http://vnboards.ign.com/tabula_rasa_general_board/b22750/106585290/r106594805/): “* Mission: “One Way Trip” (Maligo Base) – Fixed the “Speak to Demolitionist Maybauer” objective. NPC Maybauer will talk to players now and complete the objective, allowing players to progress through the mission.”
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa/b22750/104941824/p1/](https://web.archive.org/web/20071021052253/http://vnboards.ign.com:80/tabula_rasa/b22750/104941824/p1/): “? Abyss - ?Twisted Metal? ? Neobot kills will now increment counter properly. / ? Plateau/Maligo Base - ?One Way Trip? ? Final dialog objective with Colonel Bosley will now complete correctly. / ? Abyss - Archeron Post waypoint now working properly.”
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?](https://web.archive.org/web/20080302101520/http://vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?): “* Level 1 and Level 3 Access Key passes will now drop for every player in Maligo Base regardless of distance. / * "One Way Trip" should now be possible to complete on additional attempts if a player has failed the mission. / * Three new bosses added to Bane Supply Depot.”
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1804.html](https://web.archive.org/web/20090117035937/http://planettr.com:80/forums/archive/index.php/t-1804.html): “and boots you from the game, when you come back your missions have failed and you have to run the ENTIRE instance again. Case in point, the Mission, ONE WAY TRIP involving Ulstor and Milago Base. Make it thru Ulstor, get into the Base then the game has an error and kicks me. I log in the entire mission has failed requiring me to relog it and run thru Ulstor AGAIN. Please please please fix this issue with failing missions due to GAME ERRORS. This …”
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-3649.html](https://web.archive.org/web/20090116233513/http://planettr.com:80/forums/archive/index.php/t-3649.html): “There are a number of bug fixes for this instance that players should see in the next patch. / Unless that fix happens to split the "One Way Trip" mission into a few separate missions, my personal opinion is that it isn't fully "fixed". / The Ustor Yard/Maligo Base instances themselves are quite interesting and fun, but the One Way Trip frustration often leaves a terrible taste in most players' mouthes. A lot of what I have observed in General …”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_decem …](https://web.archive.org/web/20071218075407/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_december_2007): “* Mission: “One Way Trip” - added player messages to final mission objective in instance: Maligo Base in order to remind players how much time they have left to complete the objective.”
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 4
  - Requires: Blueprint For Victory (id 1046) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory), [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip)
  - Leads to: South Of The Border (id 1068) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip), [TaRapedia](https://tabularasa.fandom.com/wiki/South_Of_The_Border)
  - Leads to: New Orders From The General (id 1541) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General), [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip)

### 887 — The Defiant Ones


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 22500 XP; 3200 credits; Class VI Standard Med Pack, Class VI Res Trauma Kit — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 22,500 XP; 3,200 credits; Consumables — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones)
  - reward: 0 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/)
  - reward: “You'll get 3200 credits and a bit of experience for this cakewalk.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Transportation Officer Mamede: Fort Defiance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Transportation_Officer_Mamede)
  - location: The Defiant Ones - This mission begins in Fort Defiance with Transportation Officer Mamede around -265, 420, 859. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - location: Pearson in the command center around -66, 401, 928. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **Client:** Report to Field Lieutenant Peterson at Fort Defiance High Command.
  - objectives (client): Report to Field Lt. Peterson. — Report to Field Lt. Peterson at Fort Defiance High Command.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Transportation Officer Mamede → Field Lt. Peterson
  - reward: 22500 XP; 3200 credits; items: Class VI Standard Med Pack ×2, Class VI Res Trauma Kit
- **TaRapedia** [The Defiant Ones](https://tabularasa.fandom.com/wiki/The_Defiant_Ones) [stub]: giver: Transportation Officer Mamede; at: Fort Defiance; zone: Plateau; level: 30 (possibly less); requires: None; follow-up: A Bridge Too Far; Blue Flu; Crystal Blue Persuasion; XP: 22,500; credits: 3,200; items: Consumables; reward from: Field Lt. Peterson
  - overview: Report to Field Lieutenant Peterson at Fort Defiance High Command.
  - objectives: Report to Field Lt. Peterson. • Report to Field Lt. Peterson at Fort Defiance High Command.
  - walkthrough: This quest must be completed to open up all of the Fort Defiance missions.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Transportation Officer Mamede
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “The Defiant Ones - This mission begins in Fort Defiance with Transportation Officer Mamede around -265, 420, 859. After welcoming you to the "ass crack" of Valverde, she wants you to report to Field Lt. Pearson in the command center around -66, 401, 928. Wow, talk about easy. You'll get 3200 credits and a bit of experience for this cakewalk. style="margin: 10px; float: right; …”
- **TR MMO DB quest series** [The Defiant Ones](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/): A Bridge Too Far (level 30, reward 6200) → The Beer Hunter (level 30, reward 4650) → The Defiant Ones (level 30, reward 0) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - The Defiant Ones - This mission begins in Fort Defiance with Transportation Officer Mamede around -265, 420, 859. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - Pearson in the command center around -66, 401, 928. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **NPCs (TaRapedia NPC pages):**
  - [Transportation Officer Mamede](https://tabularasa.fandom.com/wiki/Transportation_Officer_Mamede): Fort Defiance (named as giver/receiver)
  - [Field Lt. Peterson](https://tabularasa.fandom.com/wiki/Field_Lt._Peterson): Plateau, Fort Defiance, /loc -66, 401, 931 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 1
  - Leads to: A Bridge Too Far (id 713) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Crystal Blue Persuasion (id 954) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Blue Flu (id 1014) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blue_Flu), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Defiant Ones", row 3 of 3 ([list](https://web.archive.org/web/20120731160614/http://tr.mmodb.com/quests/seriesname-the_defiant_ones/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 895 — Murder Most Foul


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Stealth Armor Helmet, Graviton Armor Gloves, Hazmat Armor Gloves, Motor Assist Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40000 XP — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Murder_Most_Foul)
  - reward: “Report back to Crais who will reward you with 4650 credits and your choice of HellStorm Stealth Armor Helmet, Vitalius Gravitron Armor Gloves, Wellcare Hazmat Gloves, or Pulsar Motor Assist Armor Gloves.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Security Guard Norton: Plateau, Fort Defiance, /loc -187, 398, 1016 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Security_Guard_Norton)
- **Client:** MP Sergeant Norton has been brutally murdered. Report his death to CID Commander Provost at the office of the AFS Criminal Investigations Division.
  - objectives (client): Speak to Commander Provost. — Report the murdered MP to CID Commander Provost at AFS Criminal Investigations Division.; Speak to Det. Crais. — Speak to CID Detective Crais about the murder victim you found.; Scan the crime scene. — Use the CID Crimecorder to scan the crime scene.; Return to Det. Crais. — Tell Crais about the results of the Crimecorder scan.; A Forean Suspect?; Upload the Crimecorder data. — Upload the Crimecorder data to the CID Mainframe Computer to view the results.; Tell Crais about the suspect. — Tell Detective Crais at CID headquarters about the Forean suspect.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver MP Sergeant Norton → CID Detective Crais
  - reward: 40000 XP; 4650 credits; choose 1 of: Stealth Armor Helmet, Graviton Armor Gloves, Hazmat Armor Gloves, Motor Assist Armor Gloves
  - prerequisites: Requires completed mission 1014
- **TaRapedia** [Murder Most Foul](https://tabularasa.fandom.com/wiki/Murder_Most_Foul): giver: Security Guard Norton; at: Fort Defiance; zone: Plateau; requires: Blue Flu; follow-up: Looking For Clues; XP: 40000; reward from: CID Detective Crais
  - overview: This quest can start two ways, one from a solder with a cold, or directly. When you scan the body, no tool needed even though mission description tells you, there isnt any. Just head over there to click on the corpse to 'scan' it. Now a radio will appear over a forean, but dont activate him at this …
  - objectives: • Speak to Commander Provost. • Speak to Det. Crais. • Scan the crime scene. • Tell Crais about the suspect. • Upload the Crimecorder data. • Return to Det. Crais.
  - walkthrough: The "crimecorder" doesn't appear in inventory anywhere, but you have already been given it.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Security Guard Norton
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “Murder Most Foul - This mission requires you to finish Blue Flu and is offered immediately upon completion. Who killed Security Guard Norton and why? Head back and talk to Provost who will send you to speak with CID Detective Crais. He'll ask you to go scan the crime scene so head back to the body to get your update. You'll next have to talk to an Unknown Forean Suspect (how …”
- **NPCs (TaRapedia NPC pages):**
  - [Security Guard Norton](https://tabularasa.fandom.com/wiki/Security_Guard_Norton): Plateau, Fort Defiance, /loc -187, 398, 1016 (named as giver/receiver)
  - [CID Detective Crais](https://tabularasa.fandom.com/wiki/CID_Detective_Crais): Plateau, Fort Defiance, /loc -230, 421, 938 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 3
  - Requires: Blue Flu (id 1014) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blue_Flu), [TaRapedia](https://tabularasa.fandom.com/wiki/Murder_Most_Foul), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Looking For Clues (id 928) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Looking_For_Clues), [TaRapedia](https://tabularasa.fandom.com/wiki/Murder_Most_Foul), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 905 — Mind Bender


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Gloves, Mech Armor Gloves, Stealth Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender)
  - reward: 4800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Agent Perdu: Plateau, Fort Defiance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Agent_Perdu)
- **Client:** Locate four mind controlled soldiers wandering the battlefield between Fort Defiance and Trinity Bridge: Lieutenant Jackson, Major Hoyle, Private Tillman and Sergeant Harris. Use the Mind Control Chip Extraction Device to remove their implants and return them to Special Agent Perdu at Fort Defiance.
  - objectives (client): Extract Jackson's implant. — Extract the Mind Control Implant from Lieutenant Jackson.; Extract Hoyle's Implant. — Extract the Mind Control Implant from Major Hoyle.; Extract Tillman's Implant. — Extract the Mind Control Implant from Private Tillman.; Extract Harris' Implant. — Extract the fifth Mind Control Implant from Sergeant Harris.; Find Lt. Jackson. — Locate Lieutenant Jackson on the battlefield.; Find Major Hoyle. — Locate Major Hoyle on the battlefield.; Find Private Tillman. — Locate Private Tillman on the battlefield.; Find Sergeant Harris. — Locate Sergeant Harris on the battlefield.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Special Agent Perdu
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Gloves, Mech Armor Gloves, Stealth Armor Gloves
  - prerequisites: Requires completed mission 1983
- **TaRapedia** [Mind Bender](https://tabularasa.fandom.com/wiki/Mind_Bender) [stub]: giver: Agent Perdu; at: Fort Defiance; zone: Plateau; level: 32 (possibly less); requires: Clean Slate; follow-up: Photo Ops; XP: 45,000; credits: 4,800; items: Equipment; reward from: Agent Perdu
  - overview: Locate four mind controlled soldiers wandering the battlefield between Fort Defiance and Trinity Bridge: Lieutenant Jackson, Major Hoyle, Private Tillman and Sergeant Harris. Use the Mind Control Chip Extraction Device to remove their implants and return them to Agent Perdu at Fort Defiance.
  - objectives: Find Lt. Jackson. • Extract Jackson's implant. • Extract the Mind Control Implant from Lieutenant Jackson. • 0/1; Find Major Hoyle.; Extract Hoyle's Implant. • Extract the Mind Control Implant from Major Hoyle. • 0/1; Find Private Tillman.; Extract Tillman's Implant. • Extract the Mind Control Implant from Private Tillman. • 0/1; Find Sergeant Harris.; Extract Harris' Implant. • Extract Mind …
  - walkthrough: The soldiers will automatically return to base after you remove their implants. There is no escort portion in this mission. NOTE: DO NOT SHOOT ALL THE AMMO FROM THE EXTRACTOR; YOU GET IT CAN NOT BE RELOADED! ALSO, YOU MUST HAVE POSSESSION OF THE EXTRACTOR WHEN YOU TURN IN THE MISSION OR YOU WILL NOT BE ABLE TO COMPLETE IT.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Agent Perdu
- **TR MMO DB quest series** [Clean Slate](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/): Clean Slate (level 30, reward 0) → Mind Bender (level 30, reward 4800) → Mind Slayer (level 30, reward 6600) (series order as listed on the page; reward unit not stated)
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?](https://web.archive.org/web/20080302101520/http://vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?): “* Added Mission indicators for "Insertion Point" on the Bane Mortars / * Added additional Mission Indicators for "Mind Bender" / * Kardash Atta Colony: Improved player navigation through the map.”
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-3313.html](https://web.archive.org/web/20090118024508/http://planettr.com:80/forums/archive/index.php/t-3313.html): “Bugged Mind Bender Mission on Plateau [Archive] - PlanetTR.com - Unofficial Tabula Rasa Forums / PlanetTR.com - Unofficial Tabula Rasa Forums > Tabula Rasa > Tech Support & Bugs > Bugged Mind Bender Mission on Plateau”
- **NPCs (TaRapedia NPC pages):**
  - [Agent Perdu](https://tabularasa.fandom.com/wiki/Agent_Perdu): Plateau, Fort Defiance (named as giver/receiver)

- **Chain** (explicit prerequisite links): Clean Slate chain (6 missions), step 2
  - Requires: Clean Slate (id 714) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Clean_Slate), [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender)
  - Requires: Clean Slate (id 1983) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Clean_Slate), [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Mind Slayer (id 719) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Slayer)
  - Leads to: Photo Ops (id 929) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender), [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Clean Slate", row 2 of 3 ([list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 928 — Looking For Clues


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Stealth Armor Helmet, Hazmat Armor Boots, Reflective Armor Vest, Motor Assist Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; or; or; or — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Looking_For_Clues)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC CID Detective Crais: Plateau, Fort Defiance, /loc -230, 421, 938 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/CID_Detective_Crais)
- **Client:** Investigate the murder of the MP at Fort Defiance and report the results to Detective Crais at CID Headquarters at Fort Defiance.
  - objectives (client): Question Private Remick. — Locate Private Remick at Wedge Rock Outpost and question him about the murder.; Search for Pendrik. — Locate the illegal craps game in the Fort Defiance bunker and ask the players about Pendrik, the Forean murder suspect.; Question Private Remick. — Question Private Remick at Wedge Rock Outpost about the murder weapon.; Question Luminary Sampei. — Question Luminary Sampei at New Velon Village about the Forean murder suspect.; Tell Crais about the suspect. — Remind Detective Crais about the Forean suspect you saw at the crime scene.; Kill Pendrick! — Pendrik will not be taken alive! Kill him!; Return to Detective Crais. — Report Pendrik's death to Detective Crais at CID Headquarters at Fort Defiance.; Return to Detective Crais. — Report back to Detective Crais at CID Headquarters inside Fort Defiance.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver CID Detective Crais
  - reward: 40000 XP; 4650 credits; choose 1 of: Stealth Armor Helmet, Hazmat Armor Boots, Reflective Armor Vest, Motor Assist Armor Boots
  - prerequisites: Requires completed mission 895
- **TaRapedia** [Looking For Clues](https://tabularasa.fandom.com/wiki/Looking_For_Clues): giver: CID Detective Crais; at: Fort Defiance; zone: Plateau; requires: Murder Most Foul; XP: 40,000; items: or; or; or; reward from: CID Detective Crais
  - overview: • Investigate the murder of the MP at Fort Defiance and report the results to Detective Crais at CID Headquarters at Fort Defiance.
  - objectives: • Tell Crais about the suspect. • Question Luminary Sampei. • Question Private Remick. • Search for Pendrik.
  - walkthrough: When you confront Pendrick two options are presented: • Let Pendrick off with a warning. • Attempt to arrest Pendrik. ( When you choose second option,you will need to kill him.He is very easy)
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by CID Detective Crais
- **TR MMO DB NPC** [CID Detective Crais](https://web.archive.org/web/20080308191612/http://tr.mmodb.com:80/bestiary/cid-detective-crais-679.php): Quest NPC, zone Plateau (starts this quest)
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_decem …](https://web.archive.org/web/20071218075407/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_december_2007): “Mission: Looking for Clues: If pendrick is killed by engineer turrets, the player does not receive credit for having killed him and will not be able to complete the mission.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “Abyss Targets of Opportunity can now be completed by cloned characters. / Plateau “Looking For Clues” will no longer break if Pendrik is killed by another player or AFS Turret. / Crafting progress bar timer decreases correctly for Disassembly when there are multiple possible outputs.”
- **NPCs (TaRapedia NPC pages):**
  - [CID Detective Crais](https://tabularasa.fandom.com/wiki/CID_Detective_Crais): Plateau, Fort Defiance, /loc -230, 421, 938 (named as giver/receiver)
  - [Private Remick](https://tabularasa.fandom.com/wiki/Private_Remick): Wedge Rock Outpost (named in client text)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 4
  - Requires: Murder Most Foul (id 895) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Looking_For_Clues), [TaRapedia](https://tabularasa.fandom.com/wiki/Murder_Most_Foul), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 929 — Photo Ops


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Outpost Cmdr. Russ: Plateau, Wedge Rock Outpost, /loc 449, 378, 367 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ)
- **Client:** Locate the Surveillance Camera Hub at the bottom of Desolation Quarry and retrieve the Bane Datablade from the central console. Bring the Datablade to Science Officer Clark at Wedge Rock Outpost for analysis.
  - objectives (client): Get survellance data #1. — Destroy the first Bane Surveillance Sentinel and obtain its surveillance data.; Get surveillance data #2. — Destroy the second Bane Surveillance Sentinel and obtain its surveillance data.; Get surveillance data #3. — Destroy the third Bane Surveillance Sentinel and obtain its surveillance data.; Get surveillance data #4. — Destroy the fourth Bane Surveillance Sentinel and obtain its surveillance data.; Get surveillance data #5. — Destroy the fifth Bane Surveillance Sentinel and obtain its surveillance data.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Outpost Cmdr. Russ → Science Officer Clark
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots
  - prerequisites: Requires completed mission 905
- **TaRapedia** [Photo Ops](https://tabularasa.fandom.com/wiki/Photo_Ops): giver: Outpost Cmdr. Russ; at: Wedge Rock Outpost; zone: Plateau; level: 32 (possibly less); requires: Mind Bender; follow-up: The Results Are In; XP: 45,000; credits: 4,800; items: Equipment; reward from: Science Officer Clark
  - overview: Locate the Surveillance Camera Hub at the bottom of Desolation Quarry and retrieve the Bane Datablade from the central console. Bring the Datablade to Science Officer Clark at Wedge Rock Outpost for analysis.
  - objectives: Get surveillance data #1. • Destroy the first Bane Surveillance Sentinel and obtain its surveillance data. • 0/1; Get surveillance data #2. • Destroy the second Bane Surveillance Sentinel and obtain its surveillance data. • 0/1; Get surveillance data #3. • Destroy the third Bane Surveillance Sentinel and obtain its surveillance data. • 0/1; Get surveillance data #4. • Destroy the fourth Bane …
  - walkthrough: The surveillance bots are randomly patrolling different levels of Desolation Quarry. When they're outside of your draw distance, look for the small white dot indicating an elite mob. Each bot is numbered according to the Data Disk they drop, so you should work your way down the spiral to the quarry gradually picking them up as you go. Once the bots see you, they usually call in a big group of Thrax who can really bust you up depending on your class. If you take a Medic with you, you should make short work of them, but it might be tough to solo as some classes. Note: Surveillance Bot One is …
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Outpost Cmdr. Russ
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1365.html](https://web.archive.org/web/20071209022816/http://planettr.com:80/forums/archive/index.php/t-1365.html): “* Guardian Prominence now has a hospital vendor. / * Sentinel #5 for Crucible mission “Photo Ops” is now spawning correctly. / * “Acquire all waypoints” objective in “Crucible Targets of Opportunity” is now completed correctly.”
- **NPCs (TaRapedia NPC pages):**
  - [Outpost Cmdr. Russ](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ): Plateau, Wedge Rock Outpost, /loc 449, 378, 367 (named as giver/receiver)
  - [Science Officer Clark](https://tabularasa.fandom.com/wiki/Science_Officer_Clark): Plateau, Wedge Rock Outpost, /loc 464, 378, 366 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Clean Slate chain (6 missions), step 3
  - Requires: Mind Bender (id 905) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender), [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Results Are In (id 936) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Results_Are_In), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 930 — The Book Of Velonar


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Book_of_Velonar); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Armor Module: Health Bonus [4], Armor Module: Power Bonus [4], Weapon Module: Crit Hit Bonus [4], Weapon Module: Armor Piercing [4] — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4,650 credits; Modifications — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Book_of_Velonar)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Luminary Sampei: Plateau, New Velon Village, /loc 259, 352, 619 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Luminary_Sampei)
  - location: - Inside North most hut (locketed at small crate at coordinates -473, 393, -8) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Book_of_Velonar)
- **Client:** A sacred Forean text, The Book Of Velonar, was lost in the attack on Velon Das. Travel to the ruins of the village, recover the text, and return it safely to Luminary Sampei at New Velon Village. The ruins are located on the far side of Valverde Chasm, southwest of Trinity Bridge.
  - objectives (client): Retrieve the Book of Velonar. — Travel to the ruins of Velon Das and locate the lost Book Of Velonar.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Luminary Sampei
  - reward: 40000 XP; 4650 credits; choose 1 of: Armor Module: Health Bonus [4], Armor Module: Power Bonus [4], Weapon Module: Crit Hit Bonus [4], Weapon Module: Armor Piercing [4]
- **TaRapedia** [The Book of Velonar](https://tabularasa.fandom.com/wiki/The_Book_of_Velonar): giver: Luminary Sampei; at: New Velon Village; zone: Plateau; level: 32 (possibly less); requires: None; follow-up: None; XP: 40,000; credits: 4,650; items: Modifications; reward from: Luminary Sampei
  - overview: A sacred Forean text, The Book Of Velonar, was lost in the attack on Velon Das. Travel to the ruins of the village, recover the text, and return it safely to Luminary Sampei at New Velon Village. The ruins are located on the far side of Valverde Chasm, southwest of Trinity Bridge.
  - objectives: Retrieve the Book of Velonar. • Travel to the ruins of Velon Das and locate the lost Book Of Velonar. • 0/1
  - walkthrough: The book is within a Forean Satchel on the ground somewhere inside the ruins of Velon Das. Note that there are a number of locked crates within the ruins as well, which some players believe to be the container, but these are unrelated. - Inside North most hut (locketed at small crate at coordinates -473, 393, -8)
- **TaRapedia mission list** [New Velon Village mission list](https://tabularasa.fandom.com/wiki/New_Velon_Village_mission_list): zone Plateau, area New Velon Village, given by Luminary Sampei
- **TR MMO DB NPC** [Ranger Porthus](https://web.archive.org/web/20081201171053/http://tr.mmodb.com/bestiary/ranger-porthus-683.php): Quest NPC, zone Plateau (related to this quest)
- **Where (locations / coordinates):**
  - - Inside North most hut (locketed at small crate at coordinates -473, 393, -8) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Book_of_Velonar)
- **NPCs (TaRapedia NPC pages):**
  - [Luminary Sampei](https://tabularasa.fandom.com/wiki/Luminary_Sampei): Plateau, New Velon Village, /loc 259, 352, 619 (named as giver/receiver)

### 931 — Bomb Voyage


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Bomb_Voyage); 30 — source: [TR MMO DB](https://web.archive.org/web/20080310011543/http://tr.mmodb.com:80/quests/bomb-voyage-248.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Bomb_Voyage)
  - reward: 4800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080310011543/http://tr.mmodb.com:80/quests/bomb-voyage-248.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Outpost Cmdr. Russ: Plateau, Wedge Rock Outpost, /loc 449, 378, 367 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ)
- **Client:** Plant an Altitude Bomb in six outgoing cargo crates near the landing pad at the bottom of Desolation Quarry. When you have planted all six bombs, return to Outpost Commander Russ at Wedge Rock Outpost.
  - objectives (client): Plant 6 Altitude Bombs. — Plant Altitude Bombs in six outgoing Bane cargo containers at Desolation Quarry.; Return to Commander Russ. — Report back to Outpost Commander Russ at Wedge Rock Outpost.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Outpost Cmdr. Russ
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest
- **TaRapedia** [Bomb Voyage](https://tabularasa.fandom.com/wiki/Bomb_Voyage): giver: Outpost Cmdr. Russ; at: Wedge Rock Outpost; zone: Plateau; level: 32 (possibly less); requires: None; follow-up: None; XP: 45,000; credits: 4,800; items: Equipment; reward from: Outpost Cmdr. Russ
  - overview: Plant an Altitude Bomb in six outgoing cargo crates near the landing pad at the bottom of Desolation Quarry. When you have planted all six bombs, return to Outpost Commander Russ at Wedge Rock Outpost.
  - objectives: Plant 6 Altitude Bombs. • Plant Altitude Bombs in six outgoing Bane cargo containers at Desolation Quarry. • Altitude Bombs Planted 0/6
  - walkthrough: The six crates are at the bottom of Desolation Quarry, right next to the Stalker. You will have to run down there through some baddies; take your time and combine this with Photo Ops. Also, when done, drop a 2 way waypoint so you can return to complete Mind Slayer. Remember WP's area active for 5 minutes so move it, Soldier!
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Outpost Cmdr. Russ
- **TR MMO DB** [Bomb Voyage](https://web.archive.org/web/20080310011543/http://tr.mmodb.com:80/quests/bomb-voyage-248.php): level: 30; starts at npc: Outpost Cmdr. Russ; starts in zone: Plateau; objectives: Plant Altitude Bombs in outgoing Bane Cargo Containers at Desolation Quarry (0/6), Return to Outpost Cmdr. Russ; description: TIP: The crates are at the bottom of Desolation Quarry.; reward: 4800
- **NPCs (TaRapedia NPC pages):**
  - [Outpost Cmdr. Russ](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ): Plateau, Wedge Rock Outpost, /loc 449, 378, 367 (NPC page lists this mission)

### 933 — Graveyard Shift


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 (unconfirmed) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Graveyard_Shift); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Gloves, Mech Armor Gloves, Stealth Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Graveyard_Shift)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Loremaster Talisen: Plateau, New Velon Village, /loc 292, 349, 563 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Loremaster_Talisen)
- **Client:** Kill twelve Tomb Miasmas and six Elder Tomb Miasmas at the Forean burial ground atop Mount Reverence. Return to Loremaster Talisen at New Velon Village when you have complete the task.
  - objectives (client): Kill 12 Tomb Miasmas. — Kill 12 Tomb Miasmas at the Mount Reverence burial grounds.; Kill 6 Tomb Miasma Bulls. — Kill 6 Tomb Miasma Bulls at the Mount Reverence burial grounds.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Loremaster Talisen
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Gloves, Mech Armor Gloves, Stealth Armor Gloves
- **TaRapedia** [Graveyard Shift](https://tabularasa.fandom.com/wiki/Graveyard_Shift): giver: Loremaster Talisen; at: New Velon Village; zone: Plateau; level: 30 (unconfirmed); requires: None; follow-up: None; XP: 45,000; credits: 4,800; items: Equipment; reward from: Loremaster Talisen
  - overview: Kill twelve Tomb Miasmas and six Elder Tomb Miasmas at the Forean burial ground atop Mount Reverance. Return to Loremaster Talisen at New Velon Village when you have complete the task.
  - objectives: Kill 12 Tomb Miasmas • Tomb Miasmas killed 0/12; Kill 6 Tomb Miasma Bulls • Tomb Miasma Bulls killed 0/6
  - walkthrough: Miasma are found at Vogren's Tomb. Typically only 4-5 bulls spawn at one time. Respawn rate is about 5 minutes. Remember that Miasma's are vulnerable to electric damage and immune to cold damage. They also phase out when damaged and cannot be damaged again until they rematerialize. The best attack is a strong Lightening bolt followed by shotgun blast. Wait until they rematerialize then repeat the attack. Do not use high rate of fire weapons such as pistols, polarity guns, leech guns and chainguns.
- **TaRapedia mission list** [New Velon Village mission list](https://tabularasa.fandom.com/wiki/New_Velon_Village_mission_list): zone Plateau, area New Velon Village, given by Loremaster Talisen
- **NPCs (TaRapedia NPC pages):**
  - [Loremaster Talisen](https://tabularasa.fandom.com/wiki/Loremaster_Talisen): Plateau, New Velon Village, /loc 292, 349, 563 (named as giver/receiver)

### 934 — A Voice From The Past


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 22500 XP; 3200 credits; Class VI Standard Med Pack, Class VI Res Trauma Kit — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 22,500 XP; 3,200 credits; Consumables — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Caretaker Tendahl: Plateau, Mount Reverance, /loc -781, 442.6, 431.7 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Caretaker_Tendahl)
- **Client:** The Caretaker of Mount Reverence asks that you enter Vogren's Tomb and pay your respects to the architect of Trinity Bridge.
  - objectives (client): Investigate Vogren's Tomb. — Investigate Vogren's Tomb on Mount Reverence.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau))
  - reward: 22500 XP; 3200 credits; items: Class VI Standard Med Pack ×2, Class VI Res Trauma Kit
- **TaRapedia** [A Voice From The Past](https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past) [stub]: giver: Caretaker Tendahi; at: Mount Reverence; zone: Plateau; level: 32 (possibly less); requires: None; follow-up: Trinity Bridge Logos: Past; XP: 22,500; credits: 3,200; items: Consumables; reward from: Holographic Projection Device
  - overview: The Caretaker of Mount Reverance asks that you enter Vogren's Tomb and pay your respects to the architect of Trinity Bridge.
  - objectives: Investigate Vogren's Tomb. • Investigate Vogren's Tomb on Mount Reverance.
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Caretaker Tendahl
- **NPCs (TaRapedia NPC pages):**
  - [Caretaker Tendahl](https://tabularasa.fandom.com/wiki/Caretaker_Tendahl): Plateau, Mount Reverance, /loc -781, 442.6, 431.7 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 1
  - Leads to: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 935 — Array Of Hope [instance]


- **Instance:** Temporal Chamber — client log 5031: open the portal on Trinity Bridge, enter it and investigate what lies beyond; the hand-in (package 1199) is beyond it (ESTIMATE) — checked by hand
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB](https://web.archive.org/web/20080907092249/http://tr.mmodb.com/quests/array-of-hope-255.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 42500 XP; 4000 credits; choose 1 of: Class VIII Standard Med Pack, Class VIII Incendiary Grenade, Class VIII Advanced Med Pack, Class VIII EMP Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 42,500 XP; 4,000 credits; Class VIII Standard Med Pack (3); Class VIII Incendiary Grenade (3); Class VIII Advanced Med Pack (2); Class VIII EMP Grenade (3) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope)
  - reward: 4000 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080907092249/http://tr.mmodb.com/quests/array-of-hope-255.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Use the six Logos you have acquired to activate the six Logos Conduits and open a portal in the center of the Trinity Bridge. Enter the portal and investigate that which lies beyond.
  - objectives (client): Activate Trinity Bridge. — Activate the portal on Trinity Bridge and see what lies beyond.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau))
  - reward: 42500 XP; 4000 credits; choose 1 of: Class VIII Standard Med Pack ×3, Class VIII Incendiary Grenade ×3, Class VIII Advanced Med Pack ×2, Class VIII EMP Grenade ×3
  - prerequisites: Requires completed mission 1044; Requires completed mission 1047; Requires completed mission 1048; Requires completed mission 1049; Requires completed mission 1051; Requires completed mission 1057
- **TaRapedia** [Array of Hope](https://tabularasa.fandom.com/wiki/Array_of_Hope): giver: Vogren's Hologram; at: Vogren's Tomb; zone: Plateau; requires: Trinity Bridge Logos: Past; Trinity Bridge Logos: Disperse; Trinity Bridge Logos: Future; Trinity Bridge Logos: Logos; Trinity Bridge Logos: Distance; Trinity Bridge Logos: Far; follow-up: Future Sacrifice; XP: 42,500; credits: 4,000; items: Class VIII Standard Med Pack (3); Class VIII Incendiary Grenade (3); Class VIII Advanced Med Pack (2); Class VIII EMP Grenade (3); reward from: Eloh Hologram
  - overview: Use the six Logos you have acquired to activate the six Logos Conduits and open a portal in the center of the Trinity Bridge. Enter the portal and investigate that which lies beyond.
  - objectives: • Activate Trinity Bridge
  - walkthrough: Activate the six conduits situated around Trinity Bridge, two on each bank and one on each of the side bridges. The portal is in the center of the middle bridge.
- **TaRapedia mission list** [Temporal Chamber mission list](https://tabularasa.fandom.com/wiki/Temporal_Chamber_mission_list): zone Plateau, given by Vogren's Hologram
- **TR MMO DB** [Array of Hope](https://web.archive.org/web/20080907092249/http://tr.mmodb.com/quests/array-of-hope-255.php): level: 30; starts in zone: Plateau; objectives: Activate Trinity Bridge; description: This quest begins from Vogren's Hologram in Plateau. The quest then takes place inside of the Temporal Chamber. It requires you have collected the Trinity Bridge Logos (Disperse, Far, Future, Logos, Distance and Past); reward: 4000; series: Array of Hope
- **TR MMO DB quest series** [Array Of Hope](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/): Array of Hope (level 30, reward 4000) → Array of Hope (level 30, reward 5550) → Foreas Base Past (level 30, reward 9250) → Send a Message (level 30, reward 9250) → The Final Assault (level 30, reward 9250) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Colonel Hudson](https://web.archive.org/web/20080404145752/http://tr.mmodb.com:80/bestiary/colonel-hudson-687.php): Quest NPC, zone Temporal Chamber (related to this quest)
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “Plateau – “Array Of Hope” – The Trinity Bridge portal to the Temporal Chamber can now be entered by players without the Trinity Bridge Array Logos, but only if it has been activated by a player who already has all six Logos.”
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (named as giver/receiver)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 4
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Requires: Trinity Bridge Logos: Future (id 1047) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Requires: Trinity Bridge Logos: Disperse (id 1048) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Requires: Trinity Bridge Logos: Logos (id 1049) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Requires: Trinity Bridge Logos: Far (id 1051) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Requires: Trinity Bridge Logos: Distance (id 1057) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Future Sacrifice (id 1171) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), [TaRapedia](https://tabularasa.fandom.com/wiki/Future_Sacrifice), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Array Of Hope", row 1 of 5 ([list](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 936 — The Results Are In


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 22500 XP; 3200 credits; Class VI Standard Med Pack, Class VI Res Trauma Kit — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 22,500 XP; 3,200 credits; Consumables — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Results_Are_In)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Science Officer Clark: Plateau, Wedge Rock Outpost, /loc 464, 378, 366 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Science_Officer_Clark)
- **Client:** Special Agent Perdu needs to speak with you regarding the surveillance data you acquired from Desolation Quarry. Report to him immediately outside the Fort Defiance gates.
  - objectives (client): Meet with Special Agent Perdu. — Meet Special Agent Perdu outside the Fort Defiance gates.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Science Officer Clark → Special Agent Perdu
  - reward: 22500 XP; 3200 credits; items: Class VI Standard Med Pack ×2, Class VI Res Trauma Kit
  - prerequisites: Requires completed mission 929
- **TaRapedia** [The Results Are In](https://tabularasa.fandom.com/wiki/The_Results_Are_In): giver: Science Officer Clark; at: Wedge Rock Outpost; zone: Plateau; requires: Photo Ops; follow-up: Mind Slayer; XP: 22,500; credits: 3,200; items: Consumables; reward from: Agent Perdu
  - overview: Penumbra Agent Perdu needs to speak with you regarding the surveillance data you acquired from Desolation Quarry. Report to him immediately outside the Fort Defiance gates.
  - objectives: Meet with Agent Perdu. • Meet Agent Perdu outside the Fort Defiance gates.
  - walkthrough: Talk to Agent Perdu outside Fort Defiance gate.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Agent Perdu
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Science Officer Clark
- **NPCs (TaRapedia NPC pages):**
  - [Science Officer Clark](https://tabularasa.fandom.com/wiki/Science_Officer_Clark): Plateau, Wedge Rock Outpost, /loc 464, 378, 366 (named as giver/receiver)
  - [Agent Perdu](https://tabularasa.fandom.com/wiki/Agent_Perdu): Plateau, Fort Defiance (named as giver/receiver)

- **Chain** (explicit prerequisite links): Clean Slate chain (6 missions), step 4
  - Requires: Photo Ops (id 929) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Photo_Ops), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Results_Are_In), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Mind Slayer (id 719) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Results_Are_In), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 939 — Giant Killer


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Giant_Killer); 25 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 4950 credits; choose 1 of: Cryogenic Blade, Cryogenic Disperser, Virulent Injector Gun, Virulent Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; 4,950 credits; Vextronics Cryogenic Blade OR; Vextronics Cryogenic Disperser OR; Vextronics Virulent Injector Gun OR; Vextronics Virulent Leech Gun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Giant_Killer)
  - reward: 0 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
  - location: Goliath (Kael): /loc -440,-230, zone Plateau — Patrols east of Camp Resistance along the north ridge, near -250, -350 — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Goliath&id=22)
- **Client:** The MASH unit at Camp Resistence can't get the wounded from the Maligo base battle back to camp for triage because of an enormous Kael named Goliath who patrols the Plateau, due east of Camp Resistance. Kill Goliath and return with his head, as proof.
  - objectives (client): Get Goliath's Head. — Kill Goliath and recover his head.; Return to Bosley. — Bring Goliath's head to Colonel Bosley at Camp Resistance.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Colonel Bosley
  - reward: 50000 XP; 4950 credits; choose 1 of: Cryogenic Blade, Cryogenic Disperser, Virulent Injector Gun, Virulent Leech Gun
- **TaRapedia** [Giant Killer](https://tabularasa.fandom.com/wiki/Giant_Killer): giver: Colonel Bosley; at: AFS Camp Resistance; zone: Plateau; level: 30; requires: None; follow-up: Incommunicado; XP: 50,000; credits: 4,950; items: Vextronics Cryogenic Blade OR; Vextronics Cryogenic Disperser OR; Vextronics Virulent Injector Gun OR; Vextronics Virulent Leech Gun; reward from: Colonel Bosley
  - overview: The MASH unit at Camp Resistence can't get the wounded from the Maligo base battle back to camp for triage because of an enormous Kael named Goliath who patrols the Plateau due east of Camp Resistance. Kill Goliath and return with his head as proof.
  - objectives: Get Goliath's Head • Kill Goliath and recover his head. • 0/1; Return to Bosley.
  - walkthrough: Goliath patrols with two-to-four other Kael in the area immediately east of AFS Camp Resistance. The pack tends to patrol between the ruins and about halfway to Camp Resistence. They are best attacked at the furthest point west of their patrol area. Try and engage him away from other Thrax patrols, or you'll have quite a fight on your hands. Remember Kael are most vulnerable to electric damage, then photonic; they are immune to fire and physical damage. Virulent and cold damage are also effective.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **Ten Ton Hammer** [plateau-targets-of-opportunity](https://www.tentonhammer.com/guides/plateau-targets-of-opportunity): “Giant Killer: Kill 30 Kael - While Kael aren't abundant they aren't too difficult to find in this zone. You can sit happily at any of the Control Points, wait around just outside Wedge Rock Outpost, or head east of Camp Resistance to take out a group of them including Goliath. Remember these creatures are completely immune to both fire and physical so bring your approrpriate …”
- **Ten Ton Hammer** [plateau-targets-of-opportunity](https://www.tentonhammer.com/guides/plateau-targets-of-opportunity): “Giant Killer mission. He does roam but if you move just east of Camp Resistance you are usually very likely to run into him and a few guards. It is an all Kael unit so be sure to bring some sonic or laser weapons and don't get in the middle of them. Inquisitor Krakatus: This Bane Technician is inside Derak Prison. This is located in the quarry just east of the southeast …”
- **TR MMO DB quest series** [Giant Killer](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/): Giant Killer (level 25, reward 0) → Incommunicado (level 25, reward 6600) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - Goliath (Kael): /loc -440,-230, zone Plateau — Patrols east of Camp Resistance along the north ridge, near -250, -350 — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Goliath&id=22)
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)
  - [Goliath](https://tabularasa.fandom.com/wiki/Goliath): Plateau (named in client text)
- **Ellatha boss NPC** [Goliath](https://www.ellatha.com/tr/bossnpcview.asp?key=Goliath&id=22): Kael; /loc -440,-230; zone Plateau; Patrols east of Camp Resistance along the north ridge, near -250, -350; needed for: Plateau Targets of Opportunity Giant Killer (Ellatha lists this mission under "Needed for Quests")

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 1
  - Leads to: Incommunicado (id 1040) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Giant_Killer), [TaRapedia](https://tabularasa.fandom.com/wiki/Incommunicado), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Giant Killer", row 1 of 2 ([list](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 954 — Crystal Blue Persuasion


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB](https://web.archive.org/web/20081202022733/http://tr.mmodb.com/quests/crystal-blue-persuasion-241.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; choose 1 of: High Grade Cartridges, High Grade Power Cells, High Grade Canister Ammunition, High Grade Rockets — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 100 Cartridges or; 100 Cartridges or; 100 Cartridges or; 100 Cartridges — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion)
  - reward: 0 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731160528/http://tr.mmodb.com/quests/seriesname-crystal_blue_persuasion/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Senior Engineer Mauer: Plateau, Fort Defiance, /loc -61, 402, 940 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Senior_Engineer_Mauer)
- **Client:** Collect 12 Mox Crystals by hunting Mox in the Mox Lair to the west of Fort Defiance. Deliver the crystals Senior Engineer Mauer at Fort Defiance High Command.
  - objectives (client): Collect 12 Mox Crystals. — Collect 12 Mox Crystals by hunting Mox in their lair near the Fort Defiance perimeter.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Senior Engineer Mauer
  - reward: 40000 XP; choose 1 of: High Grade Cartridges ×100, High Grade Power Cells ×100, High Grade Canister Ammunition ×100, High Grade Rockets ×100
  - prerequisites: Requires completed mission 887
- **TaRapedia** [Crystal Blue Persuasion](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion) [stub]: giver: Senior Engineer Mauer; at: Fort Defiance; zone: Plateau; requires: The Defiant Ones; follow-up: Alpha Mox; XP: 40,000; items: 100 Cartridges or; 100 Cartridges or; 100 Cartridges or; 100 Cartridges
  - overview: Collect 12 Mox Crystals by hunting Mox in the Mox Lair to the west of Fort Defiance. Deliver the crystals to Senior Engineer Mauer at Fort Defiance High Command.
  - objectives: • Collect 12 Mox Crystals.
  - walkthrough: The Mox are found in front of and inside the cave marked on your map. Killing the Mox with turrets will make the crystals fail to drop.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Senior Engineer Mauer
- **TR MMO DB** [Crystal Blue Persuasion](https://web.archive.org/web/20081202022733/http://tr.mmodb.com/quests/crystal-blue-persuasion-241.php): level: 30; starts at npc: Senior Engineer Mauer; starts in zone: Plateau; objectives: Collect 12 Mox Crystals, Return to Senior Engineer Mauer; description: NOTE: This quest opens up after you have completed the Defiant Ones quest, and starts a new line.; series: Crystal Blue Persuasion
  - related mob Mox: level 32, Plateau — source: [TR MMO DB bestiary](https://web.archive.org/web/20071015125416/http://tr.mmodb.com:80/bestiary/mox-149.php)
- **TR MMO DB quest series** [Crystal Blue Persuasion](https://web.archive.org/web/20120731160528/http://tr.mmodb.com/quests/seriesname-crystal_blue_persuasion/): Alpha Mox (level 31, reward 4800) → Crystal Blue Persuasion (level 30, reward 0) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Senior Engineer Mauer](https://tabularasa.fandom.com/wiki/Senior_Engineer_Mauer): Plateau, Fort Defiance, /loc -61, 402, 940 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 2
  - Requires: The Defiant Ones (id 887) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Alpha Mox (id 1027) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Alpha_Mox), [TaRapedia](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Crystal Blue Persuasion", row 2 of 2 ([list](https://web.archive.org/web/20120731160528/http://tr.mmodb.com/quests/seriesname-crystal_blue_persuasion/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 965 — Mislaid Plans


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4650 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mislaid_Plans)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC General Thaddeus T. Bailey: Plateau, Fort Defiance, /loc -68, 404, 949 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/General_Thaddeus_T._Bailey)
- **Client:** Retrieve the missing battle plans at Pinnacle Watchtower and return them to General Bailey at Fort Defiance.
  - objectives (client): Retrive the battle plans. — Retrieve the missing battle plans from the ruins of Pinnacle Watchtower.; RADIATION WARNING! — You are taking damage from radiation in the area! Complete the mission quickly!
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver General Thaddeus T. Bailey
  - reward: 45000 XP; 4650 credits
- **TaRapedia** [Mislaid Plans](https://tabularasa.fandom.com/wiki/Mislaid_Plans) [stub]: giver: General Thaddeus T. Bailey; at: Fort Defiance; zone: Plateau; XP: 45,000
  - overview: Retrieve the missing battle plans at Pinnacle Watchtower and return them to General Bailey at Fort Defiance.
  - objectives: • Retrieve the battle plans.
  - walkthrough: Look for a usable courier bag on the ground. It is generally located on the far side of the wrecked watchtower. The radiation is nothing to worry if your armor is in good condition.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by General Thaddeus T. Bailey
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “entered by players without the Trinity Bridge Array Logos, but only if it has been activated by a player who already has all six Logos. / Plateau – “Mislaid Plans” - unrelated treasure crate has been removed from area to prevent it being confused with mission useable. / Wilderness bosses Horntail, Archfiend and Old Scratch now spawn individually without group aggro, to allow them to be soloed for mission “The Three Devils.””
- **RGTR official site** [www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html](https://web.archive.org/web/20081117172617/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html): “e entered by players without the Trinity Bridge Array Logos, but only if it has been activated by a player who already has all six Logos. / Plateau – “Mislaid Plans” - unrelated treasure crate has been removed from area to prevent it being confused with mission useable. / Wilderness bosses Horntail, Archfiend and Old Scratch now spawn individually without group aggro, to allow them to be soloed for mission “The Three Devils.””
- **Stratics TR forum** [boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Board=trdiscussion …](https://web.archive.org/web/20071208191810/http://boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Board=trdiscussion&Number=12221&Forum=&Words=&Searchpage=0&Limit=25&Main=12172&Search=true&where=&Name=&daterange=&newerval=&newertype=&olderval=&oldertype=&bodyprev=): “, the missions do get buggier as you leave Plains and move on to higher level areas. Plateau, in particular, has missions for the Book of Velanor and Mislaid plans. The book is nearly impossible to find in a bane-occupied burning ruin and the plans have a very low drop rate (saw my bro do it). Maligo base is notorious for breaking its mission chain and some missions have silent timers.”
- **NPCs (TaRapedia NPC pages):**
  - [General Thaddeus T. Bailey](https://tabularasa.fandom.com/wiki/General_Thaddeus_T._Bailey): Plateau, Fort Defiance, /loc -68, 404, 949 (NPC page lists this mission)

### 970 — Lookout Down Below


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 20000 XP; 3100 credits; Class VI Advanced Med Pack, Class VI Armor Charger, Class VI Incendiary Grenade, Class VI EMP Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 20000 XP; 3100 credits; Class VI Advanced Med Pack (2); Class VI Armor Charger (2); Class VI Incendiary Grenade (4); Class VI EMP Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Lookout_Down_Below)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Outpost Cmdr. Russ: Plateau, Wedge Rock Outpost, /loc 449, 378, 367 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ)
- **Client:** Rendezvous with Field Sergeant Garde near the teleporter platform in Valverde Chasm.
  - objectives (client): Rendezvous with Sgt. Garde. — Rendezvous with Field Sergeant Garde at the teleporter pad in Valverde Chasm.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Outpost Cmdr. Russ → Field Sgt. Garde
  - reward: 20000 XP; 3100 credits; items: Class VI Advanced Med Pack ×2, Class VI Armor Charger ×2, Class VI Incendiary Grenade ×4, Class VI EMP Grenade ×4
- **TaRapedia** [Lookout Down Below](https://tabularasa.fandom.com/wiki/Lookout_Down_Below): giver: Outpost Cmdr. Russ; at: Wedge Rock Outpost; zone: Plateau; follow-up: The Stainless Steel Blodhound; XP: 20000; credits: 3100; items: Class VI Advanced Med Pack (2); Class VI Armor Charger (2); Class VI Incendiary Grenade (4); Class VI EMP Grenade (4); reward from: Field Sgt. Garde
  - overview: Rendezvous with Field Sergeant Gard near the teleporter platform in valverde Chasm.
  - objectives: • Rendezvous with Sgt. Garde
  - walkthrough: Talk to Field Sergeant Garde, easy XP.
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Outpost Cmdr. Russ
- **NPCs (TaRapedia NPC pages):**
  - [Outpost Cmdr. Russ](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ): Plateau, Wedge Rock Outpost, /loc 449, 378, 367 (named as giver/receiver)
  - [Field Sgt. Garde](https://tabularasa.fandom.com/wiki/Field_Sgt._Garde): Plateau, Valverde Chasm, /loc -103, 269, 183 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Lookout Down Below chain (3 missions), step 1
  - Leads to: The Stainless Steel Bloodhound (id 867) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1014 — Blue Flu


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); guide mentions “Blue Flu. You'll need to be level 30 to begin and it's a fairly simple FedEx” (context only; may be a creature or item level) — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/plateau-targets-of-opportunity)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 20000 XP; 3100 credits; choose 1 of: Class VI Advanced Med Pack, Class VI EMP Grenade, Class VI Res Trauma Kit, Class VI Concussion Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 20000 XP; Pathogex Motor Assist Armor Helmet v7 or; Pathogex Hazmat Armor Helmet v3 or; Pathogex Reflective Armor Helmet v2 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blue_Flu)
  - reward: “Interacting with his corpse will reward you with 3100 credits and your choice of a Class VI Advanced MedPak, Class VI Emp Grenade, Class VI Res Trauma Kit, or Class VI Concussions Grenade” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - reward: “Report back to Crais who will reward you with 4650 credits and your choice of HellStorm Stealth Armor Helmet, Vitalius Gravitron Armor Gloves, Wellcare Hazmat Gloves, or Pulsar Motor Assist Armor Gloves.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC MP Lieutenant Parkman: Plateau, Fort Defiance, /loc -72, 404, 951 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/MP_Lieutenant_Parkman)
  - location: This mission begins with MP Lieutenant Parkman in the command post around -70, 404, 951. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - location: Head upstairs to CID Commander Provost around -219, 421, 931 and he'll ask you to go relieve Guard Norton over in the supply area. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **Client:** The majority of MPs at Fort Defiance's Criminal Investigations Division are out with the Forean Flu. Report to CID Commander Provost at CID Headquarters and see what you can do to help.
  - objectives (client): Report to the Fort Defiance Warehouse. — Go relieve Security Guard Norton in the storage warehouse at the back of Fort Defiance.; Report to CID Headquarters. — Report to CID Commander Provost at the Criminal Investigations Division office at Fort Defiance.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver MP Lieutenant Parkman → MP Sergeant Norton
  - reward: 20000 XP; 3100 credits; choose 1 of: Class VI Advanced Med Pack, Class VI EMP Grenade, Class VI Res Trauma Kit, Class VI Concussion Grenade
  - prerequisites: Requires completed mission 887
- **TaRapedia** [Blue Flu](https://tabularasa.fandom.com/wiki/Blue_Flu): giver: MP Lieutenant Parkman; at: Fort Defiance; zone: Plateau; requires: The Defiant Ones; follow-up: Murder Most Foul; XP: 20000; items: Pathogex Motor Assist Armor Helmet v7 or; Pathogex Hazmat Armor Helmet v3 or; Pathogex Reflective Armor Helmet v2; reward from: Security Guard Norton
  - overview: The majority of MPs at Fort Defiance's Criminal Investigations Division are out with the Forean Flu. Report to CID Commander Provost at CID Headquarters and see what you can do to help.
  - objectives: • Report to CID Headquarters • Report to the Fort Defiance Warehouse.
  - walkthrough: This part of the quest is straightforward. Talk to NPC, then the next etc.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by MP Lieutenant Parkman
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “Blue Flu - Queue up the Who because you are going to solve a murder mystery. This mission begins with MP Lieutenant Parkman in the command post around -70, 404, 951. He's got the sniffles..ok he calls it the "flu." He'll send you to the Criminal Investigator Division to learn more about where it came from. Head upstairs to CID Commander Provost around -219, 421, 931 and he'll …”
- **Ten Ton Hammer** [fort-defiance-part-1](https://www.tentonhammer.com/guides/fort-defiance-part-1): “Blue Flu and is offered immediately upon completion. Who killed Security Guard Norton and why? Head back and talk to Provost who will send you to speak with CID Detective Crais. He'll ask you to go scan the crime scene so head back to the body to get your update. You'll next have to talk to an Unknown Forean Suspect (how much shadier can you get?) Head back up to Provosts area …”
- **Ten Ton Hammer** [plateau-targets-of-opportunity](https://www.tentonhammer.com/guides/plateau-targets-of-opportunity): “Blue Flu. You'll need to be level 30 to begin and it's a fairly simple FedEx style series with just a bit of running around. For more information about how to complete it check out our guide to Fort Defiance Part 2. Plateau Giant Killer: Kill 30 Kael - While Kael aren't abundant they aren't too difficult to find in this zone. You can sit happily at any of the Control Points, …”
- **Where (locations / coordinates):**
  - This mission begins with MP Lieutenant Parkman in the command post around -70, 404, 951. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - Head upstairs to CID Commander Provost around -219, 421, 931 and he'll ask you to go relieve Guard Norton over in the supply area. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
  - You'll find him in a storage area around -185, 398, 1016, or what is left of him. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/fort-defiance-part-1)
- **NPCs (TaRapedia NPC pages):**
  - [MP Lieutenant Parkman](https://tabularasa.fandom.com/wiki/MP_Lieutenant_Parkman): Plateau, Fort Defiance, /loc -72, 404, 951 (named as giver/receiver)
  - [Security Guard Norton](https://tabularasa.fandom.com/wiki/Security_Guard_Norton): Plateau, Fort Defiance, /loc -187, 398, 1016 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 2
  - Requires: The Defiant Ones (id 887) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blue_Flu), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Defiant_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Murder Most Foul (id 895) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blue_Flu), [TaRapedia](https://tabularasa.fandom.com/wiki/Murder_Most_Foul), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1015 — Scavenger Hunt


- **Rebuild summary:**
  - level: 31 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt); 25 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; Carton of Cigarettes — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; None credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt)
  - drops / mission items: These items randomly drop from any type of Thrax in Plateau — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt)
  - location: NPC Private "Scrounger" Scarpelo: Plateau, AFS Camp Resistance, /loc -622, 442, -227 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Private_%22Scrounger%22_Scarpelo)
- **Client:** Private Scrounger Scarpelli wants you to get five Thrax Insignia Patches and a Thrax Ceremonial Dagger. He'll trade them for the carton of Hillsboro Lights that Major Lulu Lonigan requested.
  - objectives (client): Collect 5 Insignia Patches. — Collect five Thrax Insignia Patches for Private Scrounger, to trade for Major Lonigan's cigarettes.; Get a Thrax Ceremonial Dagger. — Obtain a Thrax Ceremonial Dagger for Private Scrounger, to trade for Major Lonigan's cigarettes.
- **This PR:** revision `plateau_1_16_5`, level 31, solo, category 10000056 (Battlefield (Plateau)), giver Private "Scrounger" Scarpelo
  - reward: 50000 XP; items: Carton of Cigarettes
  - prerequisites: Requires the Scrounger's talk in Comic Relief (objective 10)
- **TaRapedia** [Scavenger Hunt](https://tabularasa.fandom.com/wiki/Scavenger_Hunt) [stub]: giver: Private "Scrounger" Scarpelo; at: AFS Camp Resistance; zone: Plateau; level: 32 (possibly less); requires: Comic Relief (one objective); follow-up: The Stalk Market; XP: 50,000; credits: None; reward from: Private "Scrounger" Scarpelo
  - overview: Private Scrounger Scarpelli wants you to get five Thrax Insignia Patches and a Thrax Ceremonial Dagger. He'll trade them for the carton of Hillsboro Lights that Major Lulu Lonigan requested.
  - objectives: Collect 5 Insignia Patches. • Collect five Thrax Insignia Patches for Private Scrounger, to trade for Major Lonigan's cigarettes. • x 5; Get a Thrax Ceremonial Dagger. • Obtain a Thrax Ceremonial Dagger for Private Scrounger, to trade for Major Lonigan's cigarettes. • x 1
  - walkthrough: These items randomly drop from any type of Thrax in Plateau.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Private "Scrounger" Scarpelo
- **NPCs (TaRapedia NPC pages):**
  - [Private "Scrounger" Scarpelo](https://tabularasa.fandom.com/wiki/Private_%22Scrounger%22_Scarpelo): Plateau, AFS Camp Resistance, /loc -622, 442, -227 (named as giver/receiver)
- **Drops / item sources:**
  - These items randomly drop from any type of Thrax in Plateau — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt)

- **Chain** (explicit prerequisite links): Comic Relief chain (3 missions), step 2
  - Requires: Comic Relief (id 1251) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt)
  - Leads to: The Stalk Market (id 1017) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stalk_Market), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1017 — The Stalk Market


- **Rebuild summary:**
  - level: 31 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 31 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stalk_Market)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 100000 XP; 6600 credits; choose 1 of: Electric Blade, Kinetic Staff, Pulse Pistol, Direct Healing Disc — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 100,000 XP; 6,600 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stalk_Market)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Private "Scrounger" Scarpelo: Plateau, AFS Camp Resistance, /loc -622, 442, -227 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Private_%22Scrounger%22_Scarpelo)
- **Client:** Hunt Bane Stalkers on the battlefield, south of Trinity Bridge, and collect the following parts for Private "Scrounger" Scarpeli at Camp Resistance: 6 Actuators, 4 Gyrostabilizers and 4 Protonic Fuel Cells.
  - objectives (client): Obtain 6 Stalker Actuators. — Obtain 6 Stalker Actuators for Private Scrounger.; Obtain 4 Gyrostabilizers. — Obtain 4 Stalker Gyrostabilizers for Private Scrounger.; Obtain 4 Fuel Cells. — Obtain 4 Protonic Fuel Cells for Private Scrounger.
- **This PR:** revision `plateau_1_16_5`, level 31, solo, category 10000056 (Battlefield (Plateau)), giver Private "Scrounger" Scarpelo
  - reward: 100000 XP; 6600 credits; choose 1 of: Electric Blade, Kinetic Staff, Pulse Pistol, Direct Healing Disc
  - prerequisites: Requires completed mission 1015
- **TaRapedia** [The Stalk Market](https://tabularasa.fandom.com/wiki/The_Stalk_Market): giver: Private "Scrounger" Scarpelo; at: AFS Camp Resistance; zone: Plateau; level: 31; requires: Scavenger Hunt; XP: 100,000; credits: 6,600; reward from: Private "Scrounger" Scarpelo
  - overview: Hunt Bane Stalkers on the battlefield, south of Trinity Bridge, and collect the following parts for Private "Scrounger" Scarpeli at Camp Resistance: 6 Actuators, 4 Gyrostabilizers and 4 Protonic Fuel Cells.
  - objectives: Obtain 6 Stalker Actuators. • Obtain 6 Stalker Actuators for Private Scrounger. • 0/6; Obtain 4 Gyrostabilizers. • Obtain 4 Stalker Gyrostabilizers for Private Scrounger. • 0/4 -- yes, Geo; Obtain 4 Fuel Cells. • Obtain 4 Protonic Fuel Cells for Private Scrounger. • 0/4
  - walkthrough: The Stalkers can be found south of Trinity Bridge, near the path to the mountains between the two bases. Frequently they will appear as a pair. Be careful in trying to tangle with 2 or more simultaneously. Also, note that there are many AFS Mechs to help out in the area. Make sure that you cause most of the damage for the kill or you will not get the drops. The Stalkers found in Incline & Mires also drop the mission items. Because of the geography, the Stalkers will commonly fall in ways that make them difficult to loot. Use the auto loot all (default shift-f) while looking at the triangle …
- **NPCs (TaRapedia NPC pages):**
  - [Private "Scrounger" Scarpelo](https://tabularasa.fandom.com/wiki/Private_%22Scrounger%22_Scarpelo): Plateau, AFS Camp Resistance, /loc -622, 442, -227 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Comic Relief chain (3 missions), step 3
  - Requires: Scavenger Hunt (id 1015) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Stalk_Market), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1027 — Alpha Mox


- **Rebuild summary:**
  - level: 31 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 31 — source: [TR MMO DB](https://web.archive.org/web/20120731170930/http://tr.mmodb.com/quests/alpha-mox-242.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Alpha_Mox)
  - reward: 4800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20120731170930/http://tr.mmodb.com/quests/alpha-mox-242.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Senior Engineer Mauer: Plateau, Fort Defiance, /loc -61, 402, 940 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Senior_Engineer_Mauer)
  - location: Mox Alpha Male (Mox), zone Plateau — In a cave before Empower logos (-443,1080) — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Mox+Alpha+Male&id=96)
- **Client:** Kill the Mox Alpha Male in the Mox Lair west of Fort Defiance and retrieve his crystal. Return the crystal to Senior Engineer Mauer at Fort Defiance High Command.
  - objectives (client): Acquire the Alpha Mox's crystal. — Kill the Mox Alpha Male and retrieve his crystal.
- **This PR:** revision `plateau_1_16_5`, level 31, solo, category 10000056 (Battlefield (Plateau)), giver Senior Engineer Mauer
  - reward: 45000 XP; 4800 credits
  - prerequisites: Requires completed mission 954
- **TaRapedia** [Alpha Mox](https://tabularasa.fandom.com/wiki/Alpha_Mox): giver: Senior Engineer Mauer; at: Fort Defiance; zone: Plateau; requires: Crystal Blue Persuasion; XP: 45,000; credits: 4,800; reward from: Senior Engineer Mauer
  - overview: Kill the Mox Alpha Male in the Mox Lair west of Fort Defiance and retrieve his crystal. Return the crystal to Senior Engineer Mauer at Fort Defiance High Command.
  - objectives: • Acquire the Alpha Mox's crystal.
  - walkthrough: The Alpha Mox is in the back of the cave.
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Senior Engineer Mauer
- **TR MMO DB** [Alpha Mox](https://web.archive.org/web/20120731170930/http://tr.mmodb.com/quests/alpha-mox-242.php): level: 31; starts at npc: Senior Engineer Mauer; starts in zone: Plateau; objectives: Acquire the Alpha Mox's Crystal, Return to Senior Engineer Mauer; reward: 4800; series: Crystal Blue Persuasion
  - related mob Mox Alpha Male (not in the archived bestiary pages)
- **TR MMO DB quest series** [Crystal Blue Persuasion](https://web.archive.org/web/20120731160528/http://tr.mmodb.com/quests/seriesname-crystal_blue_persuasion/): Alpha Mox (level 31, reward 4800) → Crystal Blue Persuasion (level 30, reward 0) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - Mox Alpha Male (Mox), zone Plateau — In a cave before Empower logos (-443,1080) — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Mox+Alpha+Male&id=96)
- **NPCs (TaRapedia NPC pages):**
  - [Senior Engineer Mauer](https://tabularasa.fandom.com/wiki/Senior_Engineer_Mauer): Plateau, Fort Defiance, /loc -61, 402, 940 (named as giver/receiver)
  - [Mox Alpha Male](https://tabularasa.fandom.com/wiki/Mox_Alpha_Male): Plateau (named in client text)
- **Ellatha boss NPC** [Mox Alpha Male](https://www.ellatha.com/tr/bossnpcview.asp?key=Mox+Alpha+Male&id=96): Mox; zone Plateau; In a cave before Empower logos (-443,1080); needed for: Alpha Mox Plateau Targets of Opportunity (Ellatha lists this mission under "Needed for Quests")

- **Chain** (explicit prerequisite links): The Defiant Ones chain (8 missions), step 3
  - Requires: Crystal Blue Persuasion (id 954) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Alpha_Mox), [TaRapedia](https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Crystal Blue Persuasion", row 1 of 2 ([list](https://web.archive.org/web/20120731160528/http://tr.mmodb.com/quests/seriesname-crystal_blue_persuasion/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1034 — Going Postal


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 (unconfirmed) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Going_Postal)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Going_Postal)
  - drops / mission items: Porthus can be found on the cliff overlooking Valverde chasm — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Going_Postal)
  - location: NPC Ranger Kaely: New Velon Village, /loc 286, 349, 569 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Ranger_Kaely)
- **Client:** Rendevous with Ranger Porthus on the far side of Valverde Chasm, near Desolation Quarry, and obtain a satchel of letters from the Forean Village of Palados. Bring the satchel to Ranger Kaely at New Velon Village.
  - objectives (client): Locate Ranger Porthus. — Redezvous with Ranger Porthus and get the satchel of letters from the village of Palados.; Get mail satchel. — Obtain the mail satchel for the Velon Das Refugees.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Ranger Kaely
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots
- **TaRapedia** [Going Postal](https://tabularasa.fandom.com/wiki/Going_Postal) [stub]: giver: Ranger Kaely; at: New Velon Village; zone: Plateau; level: 30 (unconfirmed); requires: None; follow-up: None; XP: 45,000; credits: 4,800; items: Equipment; reward from: Ranger Kaely
  - overview: Rendevous with Ranger Porthus on the far side of Valverde Chasm, near Desolation Quarry, and obtain a satchel of letters from the Forean Village of Palados. Bring the satchel to Ranger Kaely at New Velon Village.
  - objectives: Locate Ranger Porthus. • Redezvous with Ranger Porthus and get the satchel of letters from the village of Palados.; Get mail satchel. • Obtain the mail satchel for the Velon Das Refugees. • 0/1
  - walkthrough: Porthus can be found on the cliff overlooking Valverde chasm. Sometimes he will be engaged in combat and nearly falling off the cliff. Kill his attackers and he will return to his normal position. Note: Using an Exobiologist and a clone to do this mission I have never been able to find the satchel of letters. This may be bugged.
- **TaRapedia mission list** [New Velon Village mission list](https://tabularasa.fandom.com/wiki/New_Velon_Village_mission_list): zone Plateau, area New Velon Village, given by Ranger Kaely
- **NPCs (TaRapedia NPC pages):**
  - [Ranger Kaely](https://tabularasa.fandom.com/wiki/Ranger_Kaely): New Velon Village, /loc 286, 349, 569 (NPC page lists this mission)
- **Drops / item sources:**
  - Porthus can be found on the cliff overlooking Valverde chasm — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Going_Postal)

### 1040 — Incommunicado


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Incommunicado); 25 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 100000 XP; 6600 credits; choose 1 of: Laser Chaingun, Electric Net Gun, Laser Pistol, Endothermic Polarity Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 100,000 XP; 6,600 credits; Shinobi Laser Chaingun rarity=green level=33; Teleract Electric Net Gun rarity=green level=31; Shinobi Laser Pistol rarity=green level=30; Shinobi Endothermic Polarity Gun rarity=green level=33 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Incommunicado)
  - reward: 6600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
- **Client:** Locate Alpha Squad Commander Carville, Beta Squad Commander Petrie and Delta Squad Commander Locke and deliver a Modified AFS Communicator to each officer. The Commanders and their Recon Squads are on patrol in Martyr's Canyon, the chasm directly behind Maligo Base. When you have delivered all …
  - objectives (client): Deliver a comm unit to Carville. — Deliver a Modified AFS Communicator to Alpha Squad Commander Carville.; Deliver a comm unit to Petrie. — Deliver a Modified AFS Communicator to Beta Squad Commander Petrie.; Deliver a comm unit to Locke. — Deliver a Modified AFS Communicator to Delta Squad Commander Locke.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Colonel Bosley
  - reward: 100000 XP; 6600 credits; choose 1 of: Laser Chaingun, Electric Net Gun, Laser Pistol, Endothermic Polarity Gun
  - prerequisites: Requires completed mission 939
- **TaRapedia** [Incommunicado](https://tabularasa.fandom.com/wiki/Incommunicado) [stub, bug noted]: giver: Colonel Bosley; at: AFS Camp Resistance; zone: Plateau; level: 32 (possibly less); requires: Giant Killer; follow-up: Canyon Sweep (during mission); Blueprint For Victory (during mission); Toxic Detox (during mission); XP: 100,000; credits: 6,600; items: Shinobi Laser Chaingun rarity=green level=33; Teleract Electric Net Gun rarity=green level=31; Shinobi Laser Pistol rarity=green level=30; Shinobi Endothermic Polarity Gun rarity=green level=33; reward from: Colonel Bosley
  - overview: Locate Alpha Squad Commander Carville, Beta Squad Commander Petrie and Delta Squad Commander Locke and deliver a Modified AFS Communicator to each officer. The Commanders and their Recon Squads are on patrol in Martyr's Canyon, the chasm directly behind Maligo Base. When you have delivered all …
  - objectives: • Deliver a comm unit to Carville. • Deliver a Modified AFS Communicator to Alpha Squad Commander Carville. • Deliver a comm unit to Petrie. • Deliver a Modified AFS Communicator to Beta Squad Commander Petrie. • Deliver a comm unit to Locke. • Deliver a Modified AFS Communicator to Delta Squad Commander Locke.
  - walkthrough: Follow the path across the creek from AFS Camp Resistance then down the mountain into Maligo Creek. Squad Alpha is just up the hill. Continue up the mountain path; Squad Beta is just before the zone to Pools. Continue on the mountain path and then jump down to rendezvous with Squad Delta.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **TR MMO DB quest series** [Giant Killer](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/): Giant Killer (level 25, reward 0) → Incommunicado (level 25, reward 6600) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)
  - [Beta Squad Commander Petrie](https://tabularasa.fandom.com/wiki/Beta_Squad_Commander_Petrie): Plateau, Martyr's Canyon (named in client text)
  - [Delta Squad Commander Locke](https://tabularasa.fandom.com/wiki/Delta_Squad_Commander_Locke): Plateau, Maligo Creek, /loc 164, 323, -771 (named in client text)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 2
  - Requires: Giant Killer (id 939) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Giant_Killer), [TaRapedia](https://tabularasa.fandom.com/wiki/Incommunicado), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Canyon Sweep (id 1045) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Canyon_Sweep)
  - Leads to: Blueprint For Victory (id 1046) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory)
  - Leads to: Toxic Detox (id 1053) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Toxic_Detox)
- **TR MMO DB quest series:** "Giant Killer", row 2 of 2 ([list](https://web.archive.org/web/20120731163753/http://tr.mmodb.com/quests/seriesname-giant_killer/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1044 — Trinity Bridge Logos: Past


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 31 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 31 (unconfirmed) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 4950 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; 4,950 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as "Past" from a subterranean vault in Martyr's Canyon, south of Maligo Base. When you have acquired it, return to Vogren's Tomb on Mount Reverence.
  - objectives (client): Acquire "Past." — Acquire the Logos Element "Past."
- **This PR:** revision `plateau_1_16_5`, level 31, solo, category 10000056 (Battlefield (Plateau))
  - reward: 50000 XP; 4950 credits
  - prerequisites: Requires completed mission 934
- **TaRapedia** [Trinity Bridge Logos: Past](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past): giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; level: 31 (unconfirmed); requires: A Voice From The Past; follow-up: Trinity Bridge Logos: Disperse; Trinity Bridge Logos: Future; Trinity Bridge Logos: Logos; Trinity Bridge Logos: Far; Trinity Bridge Logos: Distance; XP: 50,000; credits: 4,950; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as "Past" from a subterranean vault in Martyr's Canyon, south of Maligo Base. When you have acquired it, return to Vogren's Tomb on Mount Reverance.
  - objectives: Acquire "Past." • Acquire the Logos element Past.
  - walkthrough: This logos is inside a cave which can be reached from the Maligo Creek. I was not able to determine the conditions for passing through the wall blocking this logos. When I first entered it was solid and prevented passage. After coming back from killing the Bane patrol I was able to walk right through it.
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 2
  - Requires: A Voice From The Past (id 934) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trinity Bridge Logos: Future (id 1047) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Future), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trinity Bridge Logos: Disperse (id 1048) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Disperse), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trinity Bridge Logos: Logos (id 1049) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Logos), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trinity Bridge Logos: Far (id 1051) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trinity Bridge Logos: Distance (id 1057) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Distance), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1045 — Canyon Sweep


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Canyon_Sweep)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 55000 XP; 5100 credits; choose 1 of: Electric Injector Gun, Electric Chaingun, Sonic Cannon, Sonic Shotgun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 55,000 XP; 5,100 credits; Eclipse Electric Injector Gun rarity=green level=32; Teleract Electric Chaingun rarity=green level=33; Animatics Sonic Cannon rarity=green level=34; Shinobi Sonic Shotgun rarity=green level=32 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Canyon_Sweep)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Alpha Squad Commander Carvelle: Plateau, Martyr's Canyon, /loc -432, 320, -797 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Alpha_Squad_Commander_Carvelle)
- **Client:** Kill twelve Elite Maligo Guards patrolling Martyr's Canyon behind Maligo Base, then report back to Alpha Squad Commander Carville, on patrol in Martyr's Canyon.
  - objectives (client): Kill 12 Maligo Elite Guards. — Kill Twelve Maligo Elite Guards patrolling Martyr's Canyon.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Alpha Squad Commander Carvelle
  - reward: 55000 XP; 5100 credits; choose 1 of: Electric Injector Gun, Electric Chaingun, Sonic Cannon, Sonic Shotgun
  - prerequisites: Requires Carvelle's communicator delivered (Incommunicado objective 1)
- **TaRapedia** [Canyon Sweep](https://tabularasa.fandom.com/wiki/Canyon_Sweep) [bug noted]: giver: Alpha Squad Commander Carvelle; at: Martyr's Canyon; zone: Plateau; level: 32 (possibly less); requires: Incommunicado (one objective); follow-up: None; XP: 55,000; credits: 5,100; items: Eclipse Electric Injector Gun rarity=green level=32; Teleract Electric Chaingun rarity=green level=33; Animatics Sonic Cannon rarity=green level=34; Shinobi Sonic Shotgun rarity=green level=32; reward from: Alpha Squad Commander Carvelle
  - overview: Kill twelve Elite Maligo Guards patrolling Martyr's Canyon behind Maligo Base, then report back to Alpha Squad Commander Carville, on patrol in Martyr's Canyon.
  - objectives: • Kill 12 Maligo Elite Guards.
  - walkthrough: Patrols of Elite guards are tough to kill solo, consider bringing a squad.
- **TaRapedia mission list** [Martyr's Canyon mission list](https://tabularasa.fandom.com/wiki/Martyr%27s_Canyon_mission_list): zone Plateau, area Martyr's Canyon, given by Alpha Squad Commander Carvelle
- **NPCs (TaRapedia NPC pages):**
  - [Alpha Squad Commander Carvelle](https://tabularasa.fandom.com/wiki/Alpha_Squad_Commander_Carvelle): Plateau, Martyr's Canyon, /loc -432, 320, -797 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 3
  - Requires: Incommunicado (id 1040) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Canyon_Sweep)

### 1046 — Blueprint For Victory


- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 55000 XP; 5100 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 55,000 XP; 5,100 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Beta Squad Commander Petrie: Plateau, Martyr's Canyon — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Beta_Squad_Commander_Petrie)
- **Client:** Kill Maligo Elite Guardmasters to retrieve the blueprints for Maligo Base. When you've acquired the blueprints, take them to Colonel Bosley at Camp Resistance.
  - objectives (client): Acquire the Maligo Blueprints. — Kill Maligo Elite Guardmasters to retrieve the Maligo Base Blueprints; Bring the blueprints to Col. Bosley. — Take the Maligo Base Blueprints to Colonel Bosley at Camp resistance for analysis.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Beta Squad Commander Petrie → Colonel Bosley
  - reward: 55000 XP; 5100 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet
  - prerequisites: Requires Petrie's communicator delivered (Incommunicado objective 2)
- **TaRapedia** [Blueprint For Victory](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory): giver: Beta Squad Commander Petrie; at: Martyr's Canyon; zone: Plateau; level: 32 (possibly less); requires: Incommunicado (one objective); follow-up: One Way Trip; XP: 55,000; credits: 5,100; reward from: Colonel Bosley
  - overview: Kill Maligo Elite Guardmasters to retrieve the blueprints for Maligo Base. When you've acquired the blueprints, take them to Colonel Bosley at Camp Resistance.
  - objectives: • Acquire the Maligo Blueprints. • Kill Maligo Elite Guardmasters to retrieve the Maligo Base Blueprints • Maligo Base Blueprints 0/1 • Bring the blueprints to Col. Bosley. • Take the Maligo Base Blueprints to Colonel Bosley at Camp resistance for analysis.
  - walkthrough: The blueprints drop randomly from Maligo Elite Guardmasters around Martyr's Canyon.
- **TaRapedia mission list** [Martyr's Canyon mission list](https://tabularasa.fandom.com/wiki/Martyr%27s_Canyon_mission_list): zone Plateau, area Martyr's Canyon, given by Beta Squad Commander Petrie
- **NPCs (TaRapedia NPC pages):**
  - [Beta Squad Commander Petrie](https://tabularasa.fandom.com/wiki/Beta_Squad_Commander_Petrie): Plateau, Martyr's Canyon (NPC page lists this mission)
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 3
  - Requires: Incommunicado (id 1040) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory)
  - Leads to: One Way Trip (id 877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Blueprint_For_Victory), [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip)

### 1047 — Trinity Bridge Logos: Future


- **Instance mentioned:** Guardian Prominence — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Future)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 65000 XP; 5700 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 5,700 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Future)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as "Future" from Guardian Prominance on Concordia Wilderness and bring it back to Vogren's Tomb at Mount Reverence on Valverde Plateau.
  - objectives (client): Acquire "Future." — Acquire the Logos Element "Future."
- **This PR:** revision `plateau_1_16_5`, level 33, solo, category 10000056 (Battlefield (Plateau))
  - reward: 65000 XP; 5700 credits
  - prerequisites: Requires completed mission 1044
- **TaRapedia** [Trinity Bridge Logos: Future](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Future) [stub]: giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; level: 33 (possibly less); requires: Trinity Bridge Logos: Past; credits: 5,700; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as "Future" from Guardian Prominance on Concordia Wilderness and bring it back to Vogren's Tomb at Mount Reverance on Valverde Plateau.
  - objectives: Acquire "Future." • Acquire the Logos Element "Future."
  - walkthrough: You Have to be at least lvl 36 to open the door and there is a quest to open it after you find the people from the downed drop ship also a lvl 36 quest( you get it inside the instance)
- **TaRapedia mission list** [Guardian Prominence mission list](https://tabularasa.fandom.com/wiki/Guardian_Prominence_mission_list): zone Wilderness, given by Vogren's Hologram
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 3
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Future), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1048 — Trinity Bridge Logos: Disperse


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 5,400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Disperse)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as "Disperse" from the Forean Village Ruins on Valverde Marshes and bring it back to Vogren's Tomb at Mount Reverence on Valverde Plateau.
  - objectives (client): Acquire "Disperse." — Acquire the Logos element "Disperse."
- **This PR:** revision `plateau_1_16_5`, level 33, solo, category 10000056 (Battlefield (Plateau))
  - reward: 65000 XP; 5400 credits
  - prerequisites: Requires completed mission 1044
- **TaRapedia** [Trinity Bridge Logos: Disperse](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Disperse): giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; requires: Trinity Bridge Logos: Past; credits: 5,400; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as "Disperse" from the Forean Village Ruins on Valverde Marshes and bring it back to Vogren's Tomb at Mount Reverance on Valverde Plateau.
  - objectives: Acquire "Disperse." • Acquire the Logos element "Disperse."
  - walkthrough: See Dirty Secrets for more info on this. Please read all the missions and note the bug that occurs if you fail to kill the boss (The Choices We Make) and collect his access key to the logos center.
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 3
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Disperse), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1049 — Trinity Bridge Logos: Logos


- **Instance mentioned:** Logos Research Facility — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Logos)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 5,400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Logos)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as "Logos" from The Logos Research Facility on Valverde Marshes and bring it back to Vogren's Tomb at Mount Reverence on Valverde Plateau.
  - objectives (client): Acquire "Logos." — Acquire the Logos Element "Logos."
- **This PR:** revision `plateau_1_16_5`, level 33, solo, category 10000056 (Battlefield (Plateau))
  - reward: 65000 XP; 5400 credits
  - prerequisites: Requires completed mission 1044
- **TaRapedia** [Trinity Bridge Logos: Logos](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Logos) [stub]: giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; level: 33 (possibly less); requires: Trinity Bridge Logos: Past; credits: 5,400; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as "Logos" from The Logos Research Facility on Valverde Marshes and bring it back to Vogren's Tomb at Mount Reverance on Valverde Plateau.
  - objectives: Acquire "Logos." • Acquire the Logos Element "Logos."
- **TaRapedia mission list** [Logos Research Facility mission list](https://tabularasa.fandom.com/wiki/Logos_Research_Facility_mission_list): zone Marshes, given by Vogren's Hologram
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 3
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Logos), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1051 — Trinity Bridge Logos: Far


- **Instance mentioned:** Turpis Refinery — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Far)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Far)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as "Far" from Valverde Pools and bring it back to Vogren's Tomb at Mount Reverence on Valverde Plateau.
  - objectives (client): Acquire "Far." — Acquire the Logos Element "Far."
- **This PR:** revision `plateau_1_16_5`, level 33, solo, category 10000056 (Battlefield (Plateau))
  - reward: 65000 XP; 5400 credits
  - prerequisites: Requires completed mission 1044
- **TaRapedia** [Trinity Bridge Logos: Far](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Far): giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; level: 33 (possibly less); requires: Trinity Bridge Logos: Past; XP: 65,000; credits: 5,400; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as "Far" from Valverde Pools and bring it back to Vogren's Tomb at Mount Reverance on Valverde Plateau.
  - objectives: • Acquire "Far." • Acquire the Logos Element "Far."
  - walkthrough: Head to Pools Turpis Refinery. If you want to avoid thrax patrols you can swim and/or walk trough the river downstream. This is relatively secure compared going by foot through the countryside. In Turpis Refinery do the missions to acquire the logo .
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 3
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Far), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1053 — Toxic Detox


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Toxic_Detox)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 4950 credits; choose 1 of: Area Repair Tool, Sonic Disperser, Rifle, Sonic Torqueshell Rifle — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; 4,950 credits; Dynamic Radial Repair Tool; ChiTech Sonic Disperser rarity=green level=32; Vextronics Rifle rarity=green level=31; Vitalius Sonic Torqueshell Rifle rarity=green level=33 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Toxic_Detox)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Delta Squad Commander Locke: Plateau, Maligo Creek, /loc 164, 323, -771 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Delta_Squad_Commander_Locke)
- **Client:** Destroy five Bane Toxic Waste Pumps at the Septic Bog on the plateau at the eastern end of Martyr's Canyon, then return to Delta Squad Commander Locke on patrol in Martyr's Canyon.
  - objectives (client): Destroy 5 Pumps. — Destroy five Bane Toxic Waste Pumps at Septic Bog.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Delta Squad Commander Locke
  - reward: 50000 XP; 4950 credits; choose 1 of: Area Repair Tool, Sonic Disperser, Rifle, Sonic Torqueshell Rifle
  - prerequisites: Requires Locke's communicator delivered (Incommunicado objective 3)
- **TaRapedia** [Toxic Detox](https://tabularasa.fandom.com/wiki/Toxic_Detox): giver: Delta Squad Commander Locke; at: Maligo Creek; zone: Plateau; level: 32 (possibly less); requires: Incommunicado (one objective); follow-up: None; XP: 50,000; credits: 4,950; items: Dynamic Radial Repair Tool; ChiTech Sonic Disperser rarity=green level=32; Vextronics Rifle rarity=green level=31; Vitalius Sonic Torqueshell Rifle rarity=green level=33; reward from: Delta Squad Commander Locke
  - overview: Destroy five Bane Toxic Waste Pumps at the Septic Bog on the plateau at the eastern end of Martyr's Canyon, then return to Delta Squad Commander Locke on patrol in Martyr's Canyon.
  - objectives: Destroy 5 Pumps. • Destroy five Bane Toxic Waste Pumps at Septic Bog. • Bane Toxic Waste Pumps Destroyed 0/5
  - walkthrough: There are quite a few Elite Maligo Guards around the pumps, consider bringing a squad.
- **TaRapedia mission list** [Martyr's Canyon mission list](https://tabularasa.fandom.com/wiki/Martyr%27s_Canyon_mission_list): zone Plateau, area Martyr's Canyon, given by Delta Squad Commander Locke
- **NPCs (TaRapedia NPC pages):**
  - [Delta Squad Commander Locke](https://tabularasa.fandom.com/wiki/Delta_Squad_Commander_Locke): Plateau, Maligo Creek, /loc 164, 323, -771 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 3
  - Requires: Incommunicado (id 1040) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Toxic_Detox)

### 1057 — Trinity Bridge Logos: Distance


- **Instance mentioned:** Lamnas Armory — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Distance)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Distance)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Vogren's Hologram: Plateau, Vogren's Tomb, /loc -673, 466, 539 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram)
- **Client:** Retrieve the Logos known as element "Distance" from the Lamnas Armory in Valverde Pools and bring it back to Vogren's Tomb at Mount Reverence on Valverde Plateau.
  - objectives (client): Acquire "Distance." — Acquire the Logos Element "Distance."
- **This PR:** revision `plateau_1_16_5`, level 33, solo, category 10000056 (Battlefield (Plateau))
  - reward: 65000 XP; 5400 credits
  - prerequisites: Requires completed mission 1044
- **TaRapedia** [Trinity Bridge Logos: Distance](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Distance): giver: Holographic Projection Device; at: Vogren's Tomb; zone: Plateau; level: 33 (possibly less); requires: Trinity Bridge Logos: Past; XP: 65,000; credits: 5,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Holographic Projection Device
  - overview: Retrieve the Logos known as element "Distance" from the Lamnas Armory in Valverde Pools and bring it back to Vogren's Tomb at Mount Reverance on Valverde Plateau.
  - objectives: • Acquire "Distance." • Acquire the Logos Element "Distance."
- **TaRapedia mission list** [Lamna Armory mission list](https://tabularasa.fandom.com/wiki/Lamna_Armory_mission_list): zone Pools, given by Vogren's Hologram
- **TaRapedia mission list** [Mount Reverance mission list](https://tabularasa.fandom.com/wiki/Mount_Reverance_mission_list): zone Plateau, area Mount Reverance, given by Vogren's Hologram
- **NPCs (TaRapedia NPC pages):**
  - [Vogren's Hologram](https://tabularasa.fandom.com/wiki/Vogren%27s_Hologram): Plateau, Vogren's Tomb, /loc -673, 466, 539 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 3
  - Requires: Trinity Bridge Logos: Past (id 1044) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Distance), [TaRapedia](https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos%3A_Past), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1068 — South Of The Border


- **Rebuild summary:**
  - level: 25 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 25,000 XP; 3,300 credits; Class VI Advanced Med Pack (2); Class VI Power Charger (2); Class VI Concussion Grenade (4); Class VI EMP Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/South_Of_The_Border)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
- **Client:** Deliver a crate of Medical Supplies to Colonel Snake Washington at "The Snakepit," an AFS outpost in Valverde Pools.
  - objectives (client): Deliver supplies to "Snake." — Deliver a crate of Medical Supplies to Colonel "Snake" Washington in Valverde Pools.
- **TaRapedia** [South Of The Border](https://tabularasa.fandom.com/wiki/South_Of_The_Border) [stub]: giver: Colonel Bosley; at: AFS Camp Resistance; zone: Plateau; requires: One Way Trip; XP: 25,000; credits: 3,300; items: Class VI Advanced Med Pack (2); Class VI Power Charger (2); Class VI Concussion Grenade (4); Class VI EMP Grenade (4); reward from: Colonel 'Snake' Washington
  - overview: • Deliver a crate of Medical Supplies to Colonel Snake Washington at "The Snakepit," an AFS outpost in Valverde Pools.
  - objectives: • Deliver supplies to "Snake."
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)
  - [Colonel 'Snake' Washington](https://tabularasa.fandom.com/wiki/Colonel_%27Snake%27_Washington): Pools, The Snakepit, /loc -981, 915, 413 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 5
  - Requires: One Way Trip (id 877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip), [TaRapedia](https://tabularasa.fandom.com/wiki/South_Of_The_Border)
- **Requires an instance mission:** One Way Trip (877) — source: the chain links above

### 1167 — The Final Assault [instance]


- **Instance:** Temporal Chamber — client log 8578: destroy the Security Control Stations of the underground facility, past the teleporter of 1171 — checked by hand
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TR MMO DB](https://web.archive.org/web/20080404145832/http://tr.mmodb.com:80/quests/the-final-assault-257.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 140000 XP; 9250 credits; choose 1 of: Rocket Launcher, Rifle, Incendiary Net Gun, Pulse Pistol — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 140,000 XP; 9,250 credits; Eclipse Rocket Launcher; Vextronics Rifle; Vextronics Incendiary Net Gun; Vextronics Pulse Pistol — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Final_Assault)
  - reward: 9250 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080404145832/http://tr.mmodb.com:80/quests/the-final-assault-257.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Destroy the 3 Security Control Stations that maintain the force field to the underground facility. Once the force field is down, place the detonator on the Bane Power Core.
  - objectives (client): Destroy Security Control Station Alpha — Destroy the 3 Security Control Stations to gain access to the underground Power Core Facility.; Place Detonator on Power Core — Place the detonator on the Bane Central Power Core.; Destroy Security Control Station Bravo — Destroy the 3 Security Control Stations to gain access to the underground Power Core Facility.; Destroy Security Control Station Charlie — Destroy the 3 Security Control Stations to gain access to the underground Power Core Facility.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Colonel Hudson → Veryin
  - reward: 140000 XP; 9250 credits; choose 1 of: Rocket Launcher, Rifle, Incendiary Net Gun, Pulse Pistol
  - prerequisites: Requires completed mission 1171
- **TaRapedia** [The Final Assault](https://tabularasa.fandom.com/wiki/The_Final_Assault): giver: Colonel Hudson; at: Temporal Chamber; zone: Plateau; requires: Future Sacrifice; follow-up: Foreas Base Past; XP: 140,000; credits: 9,250; items: Eclipse Rocket Launcher; Vextronics Rifle; Vextronics Incendiary Net Gun; Vextronics Pulse Pistol; reward from: Eloh Hologram
  - overview: Destroy the 3 Security Control Stations that maintain the force field to the underground facility. Once the force field is down, place the detonator on the Bane Power Core.
  - objectives: • Destroy Security Control Station Alpha • Destroy Security Control Station Bravo • Destroy Security Control Station Charlie • Place Detonator on Power Core
  - walkthrough: Be careful of the Striders, you can use the rubble for cover.
- **TaRapedia mission list** [Temporal Chamber mission list](https://tabularasa.fandom.com/wiki/Temporal_Chamber_mission_list): zone Plateau, given by Colonel Hudson
- **TR MMO DB** [The Final Assault](https://web.archive.org/web/20080404145832/http://tr.mmodb.com:80/quests/the-final-assault-257.php): level: 30; starts at npc: Colonel Hudson; starts in zone: Temporal Chamber; objectives: Destroy Security Control Station Alpha, Destroy Security Control Station Bravo, Destroy Security Control Station Charlie, Place Detonator on Power Core, Return to the Eloh Hologram; reward: 9250; series: Array of Hope
- **TR MMO DB quest series** [Array Of Hope](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/): Array of Hope (level 30, reward 4000) → Array of Hope (level 30, reward 5550) → Foreas Base Past (level 30, reward 9250) → Send a Message (level 30, reward 9250) → The Final Assault (level 30, reward 9250) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Colonel Hudson](https://web.archive.org/web/20080404145752/http://tr.mmodb.com:80/bestiary/colonel-hudson-687.php): Quest NPC, zone Temporal Chamber (starts this quest)
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1365.html](https://web.archive.org/web/20071209022816/http://planettr.com:80/forums/archive/index.php/t-1365.html): “* Temporal Chamber / o Mission: The Final Assault / o Issue: Leaving the instance without completing The Final Assault can break the mission making completing the mission chain impossible.”
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-2227.html](https://web.archive.org/web/20090116224024/http://planettr.com:80/forums/archive/index.php/t-2227.html): “The Final Assault Mission in the Eloh Bridge instance is not "allowed" to complete at this time. Mission parameters have been completed ... just can't turn in the mission to the Eloh Hologram contact.”
- **RGTR official site** [www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_1_4.html](https://web.archive.org/web/20081117172815/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_1_4.html): “Temporal Chamber / Mission: The Final Assault / Issue: Leaving the instance without completing The Final Assault can break the mission making completing the mission chain impossible.”

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 6
  - Requires: Future Sacrifice (id 1171) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Future_Sacrifice), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Final_Assault), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Foreas Base Past (id 1178) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Foreas_Base_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Final_Assault)
- **TR MMO DB quest series:** "Array Of Hope", row 5 of 5 ([list](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** Future Sacrifice (1171) — source: the chain links above

### 1171 — Future Sacrifice [instance]


- **Instance:** Temporal Chamber — client log 8730 / objective 8752: go through the teleporter and speak to Colonel Hudson on the other side — checked by hand
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 35000 XP; 5550 credits; choose 1 of: Class VII Res Trauma Kit, Class VII Concussion Grenade, Class VII Advanced Med Pack, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 5,550 credits; Class VII Res Trauma Kit; Class VII Cuncussion Grenade (3); Class VII Advanced Med Pack (6); Class VII Fragmentation Grenade (3) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Future_Sacrifice)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Go through the teleporter to Veryin's right and speak to Colonel Hudson on the other side.
  - objectives (client): Speak with Colonel Hudson. — Go through the teleporter to the Eloh's right and seek Colonel Hudson on the other side.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Veryin → Colonel Hudson
  - reward: 35000 XP; 5550 credits; choose 1 of: Class VII Res Trauma Kit, Class VII Concussion Grenade ×3, Class VII Advanced Med Pack ×6, Class VII Fragmentation Grenade ×3
  - prerequisites: Requires completed mission 935
- **TaRapedia** [Future Sacrifice](https://tabularasa.fandom.com/wiki/Future_Sacrifice): giver: Eloh Hologram; at: Temporal Chamber; zone: Plateau; requires: Array of Hope; follow-up: The Final Assault; XP: 35,000; credits: 5,550; items: Class VII Res Trauma Kit; Class VII Cuncussion Grenade (3); Class VII Advanced Med Pack (6); Class VII Fragmentation Grenade (3); reward from: Colonel Hudson
  - overview: Go through the teleporter to Veryin's right and speak to Colonel Hudson on the other side.
  - objectives: • Speak with Colonel Hudson
  - walkthrough: Just step through the teleporter and talk to Colonel Hudson.
- **TaRapedia mission list** [Temporal Chamber mission list](https://tabularasa.fandom.com/wiki/Temporal_Chamber_mission_list): zone Plateau, given by Eloh Hologram

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 5
  - Requires: Array Of Hope (id 935) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Array_of_Hope), [TaRapedia](https://tabularasa.fandom.com/wiki/Future_Sacrifice), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Final Assault (id 1167) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Future_Sacrifice), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Final_Assault), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **Requires an instance mission:** Array Of Hope (935) — source: the chain links above

### 1178 — Foreas Base Past [instance]


- **Instance:** Temporal Chamber — client objective/log text: "Return to the Temporal Chamber"
- **Rebuild summary:**
  - level: 30 — source: [TR MMO DB](https://web.archive.org/web/20080404145822/http://tr.mmodb.com:80/quests/foreas-base-past-258.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 140,000 XP; 9,250 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Foreas_Base_Past)
  - reward: 9250 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080404145822/http://tr.mmodb.com:80/quests/foreas-base-past-258.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Go through the teleporter to Veryin's left and speak to Colonel Fonseca on the other side.
  - objectives (client): Speak with Colonel Fonseca. — Go through the teleporter to the Eloh's left and seek Colonel Fonseca on the other side.; Escort Diplomat Villaran to the AFS Drop-Ship. — Safely escort Diplomat Villaran to the dropship northwest of AFS HQ.; Speak with Colonel Fonseca — Return to Colonel Fonseca in the AFS HQ.; Kill the Bane Taskmaster. — Kill Taskmaster Xykryl outside of AFS HQ.; Speak to Colonel Fonseca. — Return to Colonel Fonseca.; Return to the Temporal Chamber — Go to the teleporter north of HQ and go back to the Temporal Chamber.
- **TaRapedia** [Foreas Base Past](https://tabularasa.fandom.com/wiki/Foreas_Base_Past): giver: Eloh Hologram; at: Temporal Chamber; zone: Plateau; requires: The Final Assault; follow-up: Send a Message; XP: 140,000; credits: 9,250
  - overview: Go through the teleporter to Veryin's left and speak to Colonel Fonseca on the other side.
  - objectives: • Speak with Colonel Fonseca • Escort Diplomat Villaran to the AFS Drop-Ship • Time remaining: 00:03:00 • Speak with Colonel Fonseca • Kill the Bane Taskmaster • Speak to Colonel Fonseca • Return to the Temporal Chamber
  - walkthrough: Kill the Striders immediately, doing this will make sure the friendly NPCs can handle the assault. Chances are the taskmaster will be killed before you even get the objective to kill him, which is perfectly fine.
- **TaRapedia mission list** [Temporal Chamber mission list](https://tabularasa.fandom.com/wiki/Temporal_Chamber_mission_list): zone Plateau, given by Eloh Hologram
- **TR MMO DB** [Foreas Base Past](https://web.archive.org/web/20080404145822/http://tr.mmodb.com:80/quests/foreas-base-past-258.php): level: 30; starts in zone: Temporal Chamber; objectives: Speak with Colonel Fonseca, Escort Diplomat Villaran to the AFS Drop-Ship within 3 minutes, Speak with Colonel Fonseca, Kill the Bane Taskmaster, Speak to Colonel Fonseca, Return to the Temporal Chamber; reward: 9250; series: Array of Hope
  - related mob Colonel Fonseca (not in the archived bestiary pages)
- **TR MMO DB quest series** [Array Of Hope](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/): Array of Hope (level 30, reward 4000) → Array of Hope (level 30, reward 5550) → Foreas Base Past (level 30, reward 9250) → Send a Message (level 30, reward 9250) → The Final Assault (level 30, reward 9250) (series order as listed on the page; reward unit not stated)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 7
  - Requires: The Final Assault (id 1167) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Foreas_Base_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Final_Assault)
  - Leads to: Send a Message (id 1181) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Foreas_Base_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/Send_a_Message)
- **TR MMO DB quest series:** "Array Of Hope", row 3 of 5 ([list](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** The Final Assault (1167) — source: the chain links above

### 1181 — Send a Message [instance]


- **Instance:** Temporal Chamber — client log 8967: use the Communications Array in the Communications Room; follows 1178 Foreas Base Past (instance) — checked by hand
- **Rebuild summary:**
  - level: 30 — source: [TR MMO DB](https://web.archive.org/web/20080404145827/http://tr.mmodb.com:80/quests/send-a-message-259.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 140,000 XP; 9,250 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Send_a_Message)
  - reward: 9250 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080404145827/http://tr.mmodb.com:80/quests/send-a-message-259.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Go into the Communications Room and use the Communications Array to send a message to any remaining Eloh in the galaxy. Return to the Holographic Eloh once the message is sent.
  - objectives (client): Use the Communications Array. — Use the Communications Array at the center of the Communications Room.
- **TaRapedia** [Send a Message](https://tabularasa.fandom.com/wiki/Send_a_Message): giver: Eloh Hologram; at: Temporal Chamber; zone: Plateau; requires: Foreas Base Past; follow-up: None; XP: 140,000; credits: 9,250; reward from: Eloh Hologram
  - overview: Go into the Communications Room and use the Communications Array to send a message to any remaining Eloh in the galaxy. Return to the Holographic Eloh once the message is sent.
  - objectives: • Use the Communications Array
  - walkthrough: A little bit anticlimactic. You will receive a bonus of 25,000 experience if you completed all of the Temporal Chamber missions successfully.
- **TaRapedia mission list** [Temporal Chamber mission list](https://tabularasa.fandom.com/wiki/Temporal_Chamber_mission_list): zone Plateau, given by Eloh Hologram
- **TR MMO DB** [Send a Message](https://web.archive.org/web/20080404145827/http://tr.mmodb.com:80/quests/send-a-message-259.php): level: 30; starts in zone: Temporal Chamber; objectives: Use the Communication Key, Return to the Eloh Hologram; reward: 9250; series: Array of Hope
  - related mob Colonel Fonseca (not in the archived bestiary pages)
- **TR MMO DB quest series** [Array Of Hope](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/): Array of Hope (level 30, reward 4000) → Array of Hope (level 30, reward 5550) → Foreas Base Past (level 30, reward 9250) → Send a Message (level 30, reward 9250) → The Final Assault (level 30, reward 9250) (series order as listed on the page; reward unit not stated)

- **Chain** (explicit prerequisite links): A Voice From The Past chain (12 missions), step 8
  - Requires: Foreas Base Past (id 1178) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Foreas_Base_Past), [TaRapedia](https://tabularasa.fandom.com/wiki/Send_a_Message)
- **TR MMO DB quest series:** "Array Of Hope", row 4 of 5 ([list](https://web.archive.org/web/20120731161907/http://tr.mmodb.com/quests/seriesname-array_of_hope/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** Foreas Base Past (1178) — source: the chain links above

### 1187 — Silent But Deadly [instance]


- **Instance:** Ustor Yard — NPC Field Lieutenant Rapp (named as giver/receiver) is in Ustor Yard — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp)
- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731173712/http://tr.mmodb.com/quests/startingzone-ustor_yard/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 5000 credits; choose 1 of: Electric Injector Gun, Pulse Pistol, Incendiary Net Gun, Sonic Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40000 XP; 500 credits; Vitalius Electric Injector Gun; Animatics Pulse Pistol; Vextronics Incendiary Net Gun; Teleract Sonic Cannon — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Silent_But_Deadly)
  - reward: 5000 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Upload virus at the southern security station of Ustor Yard without getting caught to take down the force fields.
  - objectives (client): Avoid Detection While Uploading Virus to Southern Security Station.; Virus Uploaded - Bots Under AFS Control. Clear Out Bane Security Patrol.; Interface With Bane Computer and Bring Down Ustor Force Fields.; Presence Detected! Continue to Station and Kill the Security Detail in the …; Blow Generator to Open Door to the Security Station; Hidden - Activate forcefield keyboard
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Field Lieutenant Rapp
  - reward: 40000 XP; 5000 credits; choose 1 of: Electric Injector Gun, Pulse Pistol, Incendiary Net Gun, Sonic Cannon
- **TaRapedia** [Silent But Deadly](https://tabularasa.fandom.com/wiki/Silent_But_Deadly): giver: Field Lieutenant Rapp; at: Ustor Yard; zone: Plateau; requires: None; follow-up: Interception!; XP: 40000; credits: 500; items: Vitalius Electric Injector Gun; Animatics Pulse Pistol; Vextronics Incendiary Net Gun; Teleract Sonic Cannon
  - overview: Upload virus at the southern security station of Ustor Yard without getting caught to take down the force fields.
  - objectives: • Blow Generator to Open Door to the Security Station • Avoid Detection While Infiltrating Southern Security Station If the second objective is failed, i.e. you are detected, additionally: • Presence Detected! Continue to Station and Kill the Security Detail in the Station • Interface with Bane Computer and Bring Down Ustor Force Fields
  - walkthrough: You can avoid detection by using the Spy's Polymorph ability. Alternately, you can 'avoid' detection by killing a few thrax ahead, abondon and retake the quest.
- **TaRapedia mission list** [Ustor Yard mission list](https://tabularasa.fandom.com/wiki/Ustor_Yard_mission_list): zone Plateau, given by Field Lieutenant Rapp
- **TR MMO DB quest series** [Silent But Deadly](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/): Interception (level 32, reward 4650) → Monkey Wrench (level 32, reward 6600) → Silent But Deadly (level 32, reward 5000) (series order as listed on the page; reward unit not stated)
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_decem …](https://web.archive.org/web/20071218075407/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_11th_december_2007): “* Mission “Sacs and Violence” will now properly credit group members for destroying Fithik Egg Sacs. / * Mission “Silent but Deadly”: Fixed issue where bots were not turning evil when you failed this mission. / Concordia Palisades:”
- **NPCs (TaRapedia NPC pages):**
  - [Field Lieutenant Rapp](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp): Ustor Yard (named as giver/receiver)

- **Chain** (explicit prerequisite links): Running Interference chain (13 missions), step 1
  - Leads to: Interception (id 1503) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Silent_But_Deadly)
  - Leads to: Interception (id 1013) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Silent_But_Deadly)
  - Leads to: Interception! (id 1189) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Interception%21), [TaRapedia](https://tabularasa.fandom.com/wiki/Silent_But_Deadly), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Silent But Deadly", row 3 of 3 ([list](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1188 — Monkey Wrench [instance]


- **Instance:** Ustor Yard — NPC Field Lieutenant Rapp (named as giver/receiver) is in Ustor Yard — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp)
- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731173712/http://tr.mmodb.com/quests/startingzone-ustor_yard/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 6600 credits; choose 1 of: Weapon Module: Steal Armor [4], Armor Module: Regen Bonus [4], Weapon Module: Debuff Electric Resist [4], Armor Module: Resist Electric [4] — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; 6,600 credits; Modification: Steal Armor; Modification: Regen Bonus; Modification: Debuff to Electric Resist; Modification: Electric Resistance — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Monkey_Wrench)
  - reward: 6600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Head to the center of Ustor Yard and gain access to the main facility computer station.
  - objectives (client): Gain Access to Main Facility Computer From the Bane Security Officer.; Use Bane Computer Keyboard to Shut Down Facility Supply Line.; Enter Access Key to Gain Access to Facility Computer.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Field Lieutenant Rapp
  - reward: 50000 XP; 6600 credits; choose 1 of: Weapon Module: Steal Armor [4], Armor Module: Regen Bonus [4], Weapon Module: Debuff Electric Resist [4], Armor Module: Resist Electric [4]
  - prerequisites: Requires completed mission 1189
- **TaRapedia** [Monkey Wrench](https://tabularasa.fandom.com/wiki/Monkey_Wrench): giver: Field Lieutenant Rapp; at: Ustor Yard; zone: Plateau; requires: Interception!; follow-up: None; XP: 50,000; credits: 6,600; items: Modification: Steal Armor; Modification: Regen Bonus; Modification: Debuff to Electric Resist; Modification: Electric Resistance; reward from: Headquarters
  - overview: Head to the center of Ustor Yard and gain access to the main facility computer station.
  - objectives: • Gain Access to Main Facility Computer From the Bane Security Officer • Enter Access Key to Gain Access to Facility Computer • Use Bane Computer Keyboard to Shut Down Facility Supply Line
  - walkthrough: Kill the guards, use the computer.
- **TaRapedia mission list** [Ustor Yard mission list](https://tabularasa.fandom.com/wiki/Ustor_Yard_mission_list): zone Plateau, given by Field Lieutenant Rapp
- **TR MMO DB quest series** [Silent But Deadly](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/): Interception (level 32, reward 4650) → Monkey Wrench (level 32, reward 6600) → Silent But Deadly (level 32, reward 5000) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Field Lieutenant Rapp](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp): Ustor Yard (named as giver/receiver)

- **Chain** (explicit prerequisite links): Running Interference chain (13 missions), step 3
  - Requires: Interception (id 1503) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Monkey_Wrench)
  - Requires: Interception (id 1013) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Monkey_Wrench)
  - Requires: Interception! (id 1189) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Interception%21), [TaRapedia](https://tabularasa.fandom.com/wiki/Monkey_Wrench), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Silent But Deadly", row 2 of 3 ([list](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** Interception (1503), Interception (1013), Interception! (1189) — source: the chain links above

### 1189 — Interception! [instance]


- **Instance:** Ustor Yard — NPC Field Lieutenant Rapp (named as giver/receiver) is in Ustor Yard — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp)
- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 20 — source: [TR MMO DB](https://web.archive.org/web/20080407025651/http://tr.mmodb.com:80/quests/interception-438.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Class VI Basic Med Pack, Class VI Fragmentation Grenade, Class VI EMP Bomb, Class VI Cryogenic Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4,650 credits; Class VI Basic Med Pack (3); Class VI Fragmentation Grenade (3); Class VI EMP Bomb; Class VI Cryogenic Grenade (3) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Interception%21)
  - reward: 2100 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080407025651/http://tr.mmodb.com:80/quests/interception-438.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Lieutenant Jaru: Plains, Viro Relay Tower, /loc 897, 428, -129 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Lieutenant_Jaru)
- **Client:** Intercept Bane patrol to the north, access storage tanks and blow the fuel pumps to the north before a secondary patrol arrives.
  - objectives (client): Intercept Bane Patrol.; Take Out Patrol Before Secondary Patrol Arrives.; Blow Fuel Pumps in Chemical Storage to Disrupt Supply Line.; Gather Materials From Storage Tanks in Chemical Storage.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Field Lieutenant Rapp
  - reward: 40000 XP; 4650 credits; choose 1 of: Class VI Basic Med Pack ×3, Class VI Fragmentation Grenade ×3, Class VI EMP Bomb, Class VI Cryogenic Grenade ×3
  - prerequisites: Requires completed mission 1187
- **TaRapedia** [Interception!](https://tabularasa.fandom.com/wiki/Interception%21): giver: Field Lieutenant Rapp; at: Ustor Yard; zone: Plateau; requires: Silent But Deadly; follow-up: Monkey Wrench; XP: 40,000; credits: 4,650; items: Class VI Basic Med Pack (3); Class VI Fragmentation Grenade (3); Class VI EMP Bomb; Class VI Cryogenic Grenade (3)
  - overview: Intercept Bane Patrol to the north, access storage tanks and blow the fuel pumps to the north before a secondary patrol arrives.
  - objectives: • Intercept Bane Patrol. • Take Out Patrol Before Secondary Patrol Arrives. • Gather Materials From Storage Tanks. • Blow Fuel Pumps to Disrupt Supply Line.
  - walkthrough: It's a good idea to clear this area of the one time spawning Thrax first, otherwise you can end up in quite a firefight when you're on the clock to defeat the patrol in time.
- **TaRapedia mission list** [Ustor Yard mission list](https://tabularasa.fandom.com/wiki/Ustor_Yard_mission_list): zone Plateau, given by Field Lieutenant Rapp
- **TaRapedia mission list** [Viro Relay Tower mission list](https://tabularasa.fandom.com/wiki/Viro_Relay_Tower_mission_list): zone Plains, area Viro Relay Tower, given by Lieutenant Jaru
- **TR MMO DB** [Interception](https://web.archive.org/web/20080407025651/http://tr.mmodb.com:80/quests/interception-438.php): level: 20; starts at npc: Lieutenant Jaru; starts in zone: Plains; description: This is a choice mission that begins with Lieutenant Jaru. You can either let him copy the Relay Stations Operation Data or you can refuse.; reward: 2100
- **TR MMO DB quest series** [Silent But Deadly](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/): Interception (level 32, reward 4650) → Monkey Wrench (level 32, reward 6600) → Silent But Deadly (level 32, reward 5000) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Lieutenant Jaru](https://web.archive.org/web/20080408232731/http://tr.mmodb.com:80/bestiary/lieutenant-jaru-827.php): Quest NPC, zone Plains (starts this quest)
- **NPCs (TaRapedia NPC pages):**
  - [Lieutenant Jaru](https://tabularasa.fandom.com/wiki/Lieutenant_Jaru): Plains, Viro Relay Tower, /loc 897, 428, -129 (NPC page lists this mission)
  - [Field Lieutenant Rapp](https://tabularasa.fandom.com/wiki/Field_Lieutenant_Rapp): Ustor Yard (named as giver/receiver)

- **Chain** (explicit prerequisite links): Running Interference chain (13 missions), step 2
  - Requires: Running Interference (id 640) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Running_Interference)
  - Requires: Silent But Deadly (id 1187) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Interception%21), [TaRapedia](https://tabularasa.fandom.com/wiki/Silent_But_Deadly), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Trailblaze Mt. Hellas Outpost (id 638) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trailblaze_Mt._Hellas_Outpost)
  - Leads to: Incriminating Evidence (id 1063) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Incriminating_Evidence)
  - Leads to: Monkey Wrench (id 1188) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Interception%21), [TaRapedia](https://tabularasa.fandom.com/wiki/Monkey_Wrench), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Silent But Deadly", row 1 of 3 ([list](https://web.archive.org/web/20120731163805/http://tr.mmodb.com/quests/seriesname-silent_but_deadly/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** Silent But Deadly (1187) — source: the chain links above

### 1251 — Comic Relief


- **Rebuild summary:**
  - level: 31 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 31 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Comic_Relief)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 4950 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50,000 XP; 4,950 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Comic_Relief)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Corporal "Relay" O'Brien: Plateau, AFS Camp Resistance, /loc -621, 441, -234 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Corporal_%22Relay%22_O%27Brien)
- **Client:** Corporal "Relay" O'Brien lost a valuable comic book to Major Lulu Lonigan in a poker game. Get it back and he'll trade you some valuable equipment for it.
  - objectives (client): Speak to Major Lonigan. — Ask Major Lulu Lonigan about Relay's comic book.; Trade with Major Lonigan. — Trade the cigarettes to Major Lulu Lonigan for Relay's comic book.; Get Relay's comic back. — Get Relay O'Brien's Comic Book back from Major Lulu Lonigan.; Get Lulu's Cigarettes. — Obtain a Carton of Cigarettes from Private Scrounger, for Major Lulu Lonigan.; Talk to "Scrounger." — Ask Private "Scrounger" Scarpello if he can get you a carton of Hillsboro cigarettes for Major Lulu Lonigan.
- **This PR:** revision `plateau_1_16_5`, level 31, solo, category 10000056 (Battlefield (Plateau)), giver Corporal "Relay" O'Brien
  - reward: 50000 XP; 4950 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Vest, Stealth Armor Vest
- **TaRapedia** [Comic Relief](https://tabularasa.fandom.com/wiki/Comic_Relief): giver: Corporal "Relay" O'Brien; at: AFS Camp Resistance; zone: Plateau; level: 31; requires: None; follow-up: Scavenger Hunt (sub-mission); XP: 50,000; credits: 4,950; items: Equipment; reward from: Corporal "Relay" O'Brien
  - overview: Corporal "Relay" O'Brien lost a valuable comic book to Major Lulu Lonigan in a poker game. Get it back and he'll trade you some valuable equipment for it.
  - objectives: Get Relay's comic back. • Get Relay O'Brien's Comic Book back from Major Lulu Lonigan. • 0/1; Speak to Major Lonigan. • Ask Major Lulu Lonigan about Relay's comic book.; Get Lulu's Cigarettes. • Obtain a Carton of Cigarettes from Private Scrounger, for Major Lulu Lonigan. • 0/1; Talk to "Scrounger." • Ask Private "Scrounger" Scarpello if he can get you a carton of Hillsboro cigarettes for Major …
  - walkthrough: This mission consists only of talking to the mission NPCs. However, to obtain the cigarettes you have to complete Scavenger Hunt.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Corporal "Relay" O'Brien
- **NPCs (TaRapedia NPC pages):**
  - [Corporal "Relay" O'Brien](https://tabularasa.fandom.com/wiki/Corporal_%22Relay%22_O%27Brien): Plateau, AFS Camp Resistance, /loc -621, 441, -234 (named as giver/receiver)
  - [Major Lonigan](https://tabularasa.fandom.com/wiki/Major_Lonigan): Plateau, AFS Camp Resistance (named in client text)

- **Chain** (explicit prerequisite links): Comic Relief chain (3 missions), step 1
  - Leads to: Scavenger Hunt (id 1015) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Scavenger_Hunt)

### 1541 — New Orders From The General


- **Rebuild summary:**
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 55,000 XP; 5,100 credits; lvl 34; lvl 33; lvl 34; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Colonel Bosley: Plateau, AFS Camp Resistance — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Colonel_Bosley)
- **Client:** Locate Amee Corman, leader of the Retreads in Valverde Pools, and see if she has any information about the missing Eloh.
  - objectives (client): Question Amee Corman. — Question the Retread Amee Corman about the missing Eloh.
- **TaRapedia** [New Orders From The General](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General) [stub]: giver: Colonel Bosley; at: AFS Camp Resistance; zone: Plateau; requires: One Way Trip; follow-up: Weapons Trafficking; XP: 55,000; credits: 5,100; items: lvl 34; lvl 33; lvl 34; lvl 35; reward from: Amee Corman
  - overview: • Locate Amee Corman, leader of the Retreads in Valverde Pools, and see if she has any information about the missing Eloh
  - objectives: • Question Amee Corman.
- **TaRapedia mission list** [Camp Resistance mission list](https://tabularasa.fandom.com/wiki/Camp_Resistance_mission_list): zone Plateau, area Camp Resistance, given by Colonel Bosley
- **NPCs (TaRapedia NPC pages):**
  - [Colonel Bosley](https://tabularasa.fandom.com/wiki/Colonel_Bosley): Plateau, AFS Camp Resistance (named as giver/receiver)
  - [Amee Corman](https://tabularasa.fandom.com/wiki/Amee_Corman): Pools, Retread Camp (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 5
  - Requires: One Way Trip (id 877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General), [TaRapedia](https://tabularasa.fandom.com/wiki/One_Way_Trip)
  - Leads to: Weapons Trafficking (id 904) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General)
- **Requires an instance mission:** One Way Trip (877) — source: the chain links above

### 1659 — Logos: Vortex


- **Instance mentioned:** Treeback Camp — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Instance mentioned:** Ustor Yard — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex); 30 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170730/http://tr.mmodb.com/quests/startingzone-plateau/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 100000 XP; 4950 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 100,000 XP; 4,950 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Receptive Liaison Ridout: Plateau, Fort Defiance, /loc -62, 404, 950 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Receptive_Liaison_Ridout)
  - location: Logos "Vortex" shrine at map X -121, Y -126 in Plateau (marked locked) — source: [Ellatha Logos map](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp)
  - location: Take the road along the hill to Pinnacle Watchtower (-551, 387). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex)
- **Client:** Retrieve the Logos Information - Vortex
  - objectives (client): Acquire Logos Information: Vortex
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Receptive Liaison Ridout
  - reward: 100000 XP; 4950 credits
- **TaRapedia** [Logos: Vortex](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex): giver: Receptive Liaison Ridout; at: Fort Defiance; zone: Plateau; level: 30; requires: None; follow-up: None; XP: 100,000; credits: 4,950; reward from: Receptive Liaison Ridout
  - overview: Retrieve the Logos Information - Vortex
  - objectives: • Acquire Logos Information: Vortex
  - walkthrough: The following logos elements are relevant in this mission: | | PLEASE NOTE: All coordinates given here are based on looking at the map and not using the /loc command. The coordinates will be updated as soon as I can. Here's a brief walk through on how to get the Vortex Logos. First off, I recommend doing this at 29.99999 if you can. This saves you from having to repeat this on your clone. It's easy with a group (3+), but I haven't done it that way. I ran it with my Exobiologist and a friend's Grenadier, and we were both level 30 at the time. (Note: Tested this route solo with my 100%-Mind …
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by Receptive Liaison Ridout
- **Where (locations / coordinates):**
  - Logos "Vortex" shrine at map X -121, Y -126 in Plateau (marked locked) — source: [Ellatha Logos map](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp)
  - Take the road along the hill to Pinnacle Watchtower (-551, 387). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex)
  - Go north from the T to get Logos Past (-126, -498). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Logos%3A_Vortex)
- **NPCs (TaRapedia NPC pages):**
  - [Receptive Liaison Ridout](https://tabularasa.fandom.com/wiki/Receptive_Liaison_Ridout): Plateau, Fort Defiance, /loc -62, 404, 950 (NPC page lists this mission)

### 1825 — All Along The Watchtowers


- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 32 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/All_Along_The_Watchtowers)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Helmet, Graviton Armor Helmet, Mech Armor Helmet, Stealth Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 45,000 XP; 4,800 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/All_Along_The_Watchtowers)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** Colonel Thibodeau at Wedge Rock Outpost needs you to manually reboot the Remote Scanning Terminals in the three watchtowers overlooking the eastern end of Valverde Chasm.
  - objectives (client): Reboot Terminal #1 — Reboot Remote Sensor Terminal #1.; Reboot Terminal #2 — Reboot Remote Sensor Terminal #2.; Reboot Terminal #3 — Reboot Remote Sensor Terminal #3.
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Colonel Thibodeau
  - reward: 45000 XP; 4800 credits; choose 1 of: Bio Armor Helmet, Graviton Armor Helmet, Mech Armor Helmet, Stealth Armor Helmet
- **TaRapedia** [All Along The Watchtowers](https://tabularasa.fandom.com/wiki/All_Along_The_Watchtowers): giver: Colonel Thibodeau; at: Wedge Rock Outpost; zone: Plateau; level: 32 (possibly less); requires: None; follow-up: None; XP: 45,000; credits: 4,800; items: Equipment; reward from: Colonel Thibodeau
  - overview: Colonel Thibodeau at Wedge Rock Outpost needs you to manually reboot the Remote Scanning Terminals in the three watchtowers overlooking the eastern end of Valverde Chasm.
  - objectives: Reboot Terminal #1 • Reboot Remote Sensor Terminal #1.; Reboot Terminal #2 • Reboot Remote Sensor Terminal #2.; Reboot Terminal #3 • Reboot Remote Sensor Terminal #3.
  - walkthrough: Slam dunk - follow the radio icons and reboot the computers on the upper level of the 3 watchtowers. EZ XP.
- **TaRapedia mission list** [Wedge Rock Outpost mission list](https://tabularasa.fandom.com/wiki/Wedge_Rock_Outpost_mission_list): zone Plateau, area Wedge Rock Outpost, given by Colonel Thibodeau
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “‘Survivor’s one time only, if you fail the mission or complete it to the second objective until the instance has been reset. / New Plateau mission, “All Along The Watchtowers.” / Pre-order customers will now receive the mission to select their companion through radio mission upon reaching Alia Das on Wilderness.”
- **RGTR official site** [www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html](https://web.archive.org/web/20081117172617/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html): “ission or complete it to the second objective you will not be able to reacquire the mission until the instance has been reset. / New Plateau mission, “All Along The Watchtowers.” / Pre-order customers will now receive the mission to select their companion through radio mission upon reaching Alia Das on Wilderness.”
- **WarCry TR** [tr.warcry.com:80/forums/read/191.53430](https://web.archive.org/web/20080202212646/http://tr.warcry.com:80/forums/read/191.53430): “sion or complete it to the second objective you will not be able to reacquire the mission until the instance has been reset. / * New Plateau mission, "All Along The Watchtowers." / * Pre-order customers will now receive the mission to select their companion through radio mission upon reaching Alia Das on Wilderness.”

### 1974 — Deadly Silence [instance]


- **Instance:** Velon Hollow — an objective conversation's NPC package 2263 spawns in this instance (map 2136) — source: client objectiveconversation + this PR's World data
- **Rebuild summary:**
  - level: 32 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 90000 XP; 6400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 90000 XP; 6400 credits; Prodigy/Titan Boots — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Deadly_Silence)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Luminary Sampei: Plateau, New Velon Village, /loc 259, 352, 619 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Luminary_Sampei)
- **Client:** Travel to Velon Hollow and find the spiritual leaders of the Order of Velon. This includes Elder Hathen, Seer Kadali, Guardian Minash, and Shaman Taosari. Report your findings to Luminary Sampei at New Velon.
  - objectives (client): Find Elder Hathen; Find Guardian Minash; Find Shaman Taosari; Find Seer Kadali
- **This PR:** revision `plateau_1_16_5`, level 32, solo, category 10000056 (Battlefield (Plateau)), giver Luminary Sampei
  - reward: 90000 XP; 6400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Boots, Stealth Armor Boots
  - prerequisites: Requires completed mission 1981
- **TaRapedia** [Deadly Silence](https://tabularasa.fandom.com/wiki/Deadly_Silence): giver: Luminary Sampei; at: New Velon Village; zone: Plateau; requires: Village Of The Damned; XP: 90000; credits: 6400; items: Prodigy/Titan Boots; reward from: Luminary Sampei
  - overview: Find the 4 elders in Velon Hollow
  - objectives: • Find Elder Hathen • Find Guardian Minash • Find Shaman Taosari • Find Seer Kadali
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Luminary Sampei
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **RGTR official site** [www.rgtr.com/news/patch_notes_public_test/deployment_10.html](https://web.archive.org/web/20081117082235/http://www.rgtr.com/news/patch_notes_public_test/deployment_10.html): “s. Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost. Boot Camp - Tutorial text now properly instructs player to press "T" to talk to NPCs instead of "F." Boot Camp - Final mission …”
- **NPCs (TaRapedia NPC pages):**
  - [Luminary Sampei](https://tabularasa.fandom.com/wiki/Luminary_Sampei): Plateau, New Velon Village, /loc 259, 352, 619 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 2
  - Requires: Village Of The Damned (id 1981) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Deadly_Silence), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1975 — The Better Half [instance]


- **Instance:** Velon Hollow — client objective/log text: "Tala Clark was sent into Velon Hollow to investigate the Foreans living there and report back with their status. …"
- **Rebuild summary:**
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 90,000 XP; 6,400 credits; Purifier Helmet — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Better_Half)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Science Officer Clark: Plateau, Wedge Rock Outpost, /loc 464, 378, 366 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Science_Officer_Clark)
- **Client:** Tala Clark was sent into Velon Hollow to investigate the Foreans living there and report back with their status. Unfortunately, she never came back. Science Officer Clark in Wedge Rock Outpost wants you to explore the Velon Hollow and find his wife. Report back to him with any information you …
  - objectives (client): Find Tala Clark — Explore Velon Hollow and find any evidence you can of Science Officer Tala Clark.; Decision: Let Maxxum Quarsh escape or sacrifice Tala Clark? — Your actions in the next few seconds will decide who walks out of here alive.; Inform Officer Clark of Tala's Death; Inform Officer Clark that Tala is alive; (Hidden) Required, Completed when either End objective finished.
- **TaRapedia** [The Better Half](https://tabularasa.fandom.com/wiki/The_Better_Half) [stub]: giver: Science Officer Clark; at: Wedge Rock Outpost; zone: Plateau; requires: Village Of The Damned; XP: 90,000; credits: 6,400; items: Purifier Helmet; reward from: Science Officer Clark
  - overview: Tala Clark was sent into Velon Hollow to investigate the Foreans living there and report back with their status. Unfortunately, she never came back. Science Officer Clark in Wedge Rock Outpost wants you to explore the Velon Hollow and find his wife. Report back to him with any information you …
  - objectives: • Find Tala Clark • Explore Velon Hollow and find any evidence of Science Officer Tala Clark. • Decision: Let Maxxum Quarsh escape or sacrifice Tala Clark? • Inform Officer Clark that Tala is alive • OR • Inform Officer Clark of Tala's Death
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Science Officer Clark
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “e, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **PlayTR EU** [eu.playtr.com:80/en/field_training/guide/velon_hollow](https://web.archive.org/web/20090201021913/http://eu.playtr.com:80/en/field_training/guide/velon_hollow): “It was also in this spirit that I created the mission “The Better Half,” an ethical choice mission where the player’s decision is made through action. You are asked to find the missing wife of Science Officer Clark, who has been taken hostage by the Bane. When you finally find her, your actions alone will determine whether or not she comes home alive. Like much of the story that unfolds in this instance, I feel the more I can do to keep you out …”
- **NPCs (TaRapedia NPC pages):**
  - [Science Officer Clark](https://tabularasa.fandom.com/wiki/Science_Officer_Clark): Plateau, Wedge Rock Outpost, /loc 464, 378, 366 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 2
  - Requires: Village Of The Damned (id 1981) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Better_Half)

### 1976 — These Old Bones [instance]


- **Instance:** Velon Hollow — client objective/log text: "Elder Hathen is the highest ranking survivor of the Seekers of Velon. He demands to come with you into Velon Hollow, …"
- **Rebuild summary:**
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; Prodigy/Titan Gloves — source: [TaRapedia](https://tabularasa.fandom.com/wiki/These_Old_Bones)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** The Bane have overrun Velon Hollow and unleashed an infection that turned most of the Foreans into mindless zombies. Elder Hathan wants you to help him clear out the Mausoleum of the Beholden, the Shrine of the Penitent, and the Sanctuary of Renewal in order to rescue any suriving Foreans. If you …
  - objectives (client): Secure the Mausoleum of the Beholden — Eliminate all infected Foreans and Bane found inside the Mausoleum of the Beholden.; Secure the Shrine of the Penitent — Eliminate all infected Foreans and Bane found in Shrine of the Penitent.; Secure the Santuary of Renewal — Eliminate all infected Foreans and Bane found in Santuary of Renewal.; Protect Elder Hathen — Elder Hathen is the highest ranking survivor of the Seekers of Velon. He demands to come with you into Velon Hollow, and must be protected as a VIP.
- **TaRapedia** [These Old Bones](https://tabularasa.fandom.com/wiki/These_Old_Bones): giver: Elder Hathen; at: Velon Hollow; zone: Plateau; follow-up: Holy Vengeance; XP: 45000; credits: 4800; items: Prodigy/Titan Gloves; reward from: Warrior Algona
  - overview: Save survivors.
  - objectives: • Secure the Mausoleum of the Beholden • Secure the Shrine of the Penitent • Secure the Santuary of Renewal • Protect Elder Haten Briefing: Elder Hathen: • The Bane overran the Hollow and infected the water with something that is driving everyone mad! I watched as my oldest friends turned on me and killed my dear wife Matha. • A few of us are immune, but the Bane captured them all for …
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Elder Hathen
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “n Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”

- **Chain** (explicit prerequisite links): These Old Bones chain (2 missions), step 1
  - Leads to: Holy Vengeance (id 1977) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Holy_Vengeance), [TaRapedia](https://tabularasa.fandom.com/wiki/These_Old_Bones)

### 1977 — Holy Vengeance [instance]


- **Instance:** Velon Hollow — client objective/log text: "Clear the outer part of the Bane Outpost established inside Velon Hollow."
- **Rebuild summary:**
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; 4800 credits; Purifier Legs — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Holy_Vengeance)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Luminary Sampei: Plateau, New Velon Village, /loc 259, 352, 619 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Luminary_Sampei)
- **Client:** Now that the surviving Foreans are safe, you are asked to neutralize the Bane threat and recover the Law of Asperion. Return the book to Luminary Sampei at New Velon once you are finished.
  - objectives (client): Secure outer Bane Outpost — Clear the outer part of the Bane Outpost established inside Velon Hollow.; Clear inner Bane Outpost — Clear the inner part of the Bane Outpost established inside Velon Hollow.; Recover the Law of Asperion — Find and recover the holy text known as the Law of Asperion. It is believed to be stored inside the Bane Outpost.; (Hidden) Alonga Escort Scripting
- **TaRapedia** [Holy Vengeance](https://tabularasa.fandom.com/wiki/Holy_Vengeance): giver: Warrior Algona; at: Velon Hollow; zone: Plateau; requires: These Old Bones; XP: 45000; credits: 4800; items: Purifier Legs; reward from: Luminary Sampei
  - overview: Secure Outposts and recover the Law of Asperion
  - objectives: • Secure outer Bane Outpost • Clear inner Bane Outpost • Recover the Law of Asperion
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Warrior Algona
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **NPCs (TaRapedia NPC pages):**
  - [Luminary Sampei](https://tabularasa.fandom.com/wiki/Luminary_Sampei): Plateau, New Velon Village, /loc 259, 352, 619 (named as giver/receiver)

- **Chain** (explicit prerequisite links): These Old Bones chain (2 missions), step 2
  - Requires: These Old Bones (id 1976) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Holy_Vengeance), [TaRapedia](https://tabularasa.fandom.com/wiki/These_Old_Bones)
- **Requires an instance mission:** These Old Bones (1976) — source: the chain links above

### 1978 — Ancient Mysteries [instance]


- **Instance:** Velon Hollow — client objective/log text: "Recover the artifacts from Bane officers in Velon Hollow before they are lost forever."
- **Rebuild summary:**
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 4800 credits; 45000 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Ancient_Mysteries)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Plateau only
- **Client:** The Order of Velon was responsible for protecting many ancient artifacts. Since contact has stopped with Velon Hollow, the Foreans have become concerned that the Bane may have stolen them. Travel to Velon Hollow and recover any Velon Artifacts you find. Afterwards, return them to Elder Hundra at …
  - objectives (client): Recover Velon Artifacts — Recover the artifacts from Bane officers in Velon Hollow before they are lost forever.; Explore Velon Hollow — Travel to Velon Hollow.; Return to Receptive Markin OR Elder Hundra — You must decide whether to return the artifacts to the Foreans, or give them to the AFS for testing. You can not give them to both.
- **TaRapedia** [Ancient Mysteries](https://tabularasa.fandom.com/wiki/Ancient_Mysteries): giver: Elder Hundra; at: New Velon Village; zone: Plateau; requires: Village Of The Damned; credits: 4800; items: 45000; reward from: Elder Hundra
  - overview: Explore Velon Hollow
  - objectives: • Explore Velon Hollow • Recover Velon Artifacts • Recover the artifacts from Bane officers in Velon Hollow before they are lost forever. • 0/10 • Return to Receptive Markin OR Elder Hundra • You msut decide whether to return the artifacts to Foreans, or give them to the AFS for testing. You can not give them to both.
  - walkthrough: Seem to be on all bosses in instance - Thrax leaders, Forean infected and a mox too.
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Elder Hundra
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **RGTR official site** [www.rgtr.com/news/patch_notes_public_test/deployment_10.html](https://web.archive.org/web/20081117082235/http://www.rgtr.com/news/patch_notes_public_test/deployment_10.html): “vailable for players. Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost. Boot Camp - Tutorial text now properly instructs player to press "T" to talk to NPCs instead of "F." Boot Camp …”

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 2
  - Requires: Village Of The Damned (id 1981) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Ancient_Mysteries)

### 1979 — Mountain Spring Toxins [instance]


- **Instance:** Velon Hollow — client log 20824 / objective 20907: infiltrate and explore Velon Hollow (client map adv_foreas_howlingmaw_deathburrow) — checked by hand
- **Rebuild summary:**
  - level: 49 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731172600/http://tr.mmodb.com/quests/startingzone-howling_death_burrow/)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 90,000 XP; 6,400 credits; Purifier Vest — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Orivos: Howling Maw, Gangus Outpost, /loc -638.0, 199.8, -985.3 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Orivos)
- **Client:** Infiltrate Velon Hollow and find the source of the genetic virus that is being injected into the water. Once you have determined the source of the infection, you are ordered to destroy it. Report back to Outpost Commander Russ at Wedge Rock Outpost once the mission is complete.
  - objectives (client): Find the source of the toxin — Explore Velon Hollow and discover the source of the toxins causing this new infection.; Sabotage Chemical Tower 1; Sabotage Chemical Tower 2; Sabotage Chemical Tower 3; Sabotage Chemical Tower 4; Overload the Water Conversion Machine — Explore the inside of the Bane Outpost and find the primary controls for the water conversion machine. Use the controls to overload and destroy the toxin mixer …
- **TaRapedia** [Mountain Spring Toxins](https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins) [stub]: giver: Outpost Cmdr. Russ; at: Wedge Rock Outpost; zone: Plateau; requires: Village Of The Damned; XP: 90,000; credits: 6,400; items: Purifier Vest; reward from: Outpost Cmdr. Russ
  - overview: Infiltrate Velon Hollow and find the source of the genetic virus that is being injected into the water. Once you have determined the source of the infection, you are ordered to destroy it. Report back to Outpost Cmdr. Russ at Wedge Rock Outpost once the mission is complete.
  - objectives: • Find the source of the toxin • Explore Velon Hollow and discover the source of the toxins causing this new infection. • Sabotage Chemical Tower 1 • Sabotage Chemical Tower 2 • Sabotage Chemical Tower 3 • Sabotage Chemical Tower 4 • Overload the Water Conversion Machine • Explore the inside of the Bane Outpost and find the primary controls for the water conversion machine. Use the controls to …
- **TaRapedia mission list** [Gangus Outpost mission list](https://tabularasa.fandom.com/wiki/Gangus_Outpost_mission_list): zone Howling Maw, area Gangus Outpost, given by Elder Orivos
- **TaRapedia mission list** [Howling Death Burrow mission list](https://tabularasa.fandom.com/wiki/Howling_Death_Burrow_mission_list): zone Howling Maw, given by Elder Orivos
- **TaRapedia mission list** [Velon Hollow mission list](https://tabularasa.fandom.com/wiki/Velon_Hollow_mission_list): zone Plateau, given by Outpost Cmdr. Russ
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “nstance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **NPCs (TaRapedia NPC pages):**
  - [Elder Orivos](https://tabularasa.fandom.com/wiki/Elder_Orivos): Howling Maw, Gangus Outpost, /loc -638.0, 199.8, -985.3 (NPC page lists this mission)
  - [Outpost Cmdr. Russ](https://tabularasa.fandom.com/wiki/Outpost_Cmdr._Russ): Plateau, Wedge Rock Outpost, /loc 449, 378, 367 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 6
  - Requires: Village Of The Damned (id 1981) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins)
  - Requires: Choosing Sides (id 1554) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Choosing_Sides)
- **Requires an instance mission:** Choosing Sides (1554) — source: the chain links above

### 1981 — Village Of The Damned


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Village_Of_The_Damned)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; None credits; OR; OR; OR — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Village_Of_The_Damned)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Luminary Sampei: Plateau, New Velon Village, /loc 259, 352, 619 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Luminary_Sampei)
- **Client:** Luminary Sampai wants you to kill 10 Infected Foreans in the ruins of Velon Das.
  - objectives (client): Kill 10 Infected Foreans. — Kill 10 Infected Foreans among the Ruins of Velon Das.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Luminary Sampei
  - reward: 40000 XP; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet
- **TaRapedia** [Village Of The Damned](https://tabularasa.fandom.com/wiki/Village_Of_The_Damned): giver: Luminary Sampei; at: New Velon Village; zone: Plateau; level: 30; requires: None; follow-up: None; XP: 40,000; credits: None; items: OR; OR; OR; reward from: Luminary Sampei
  - overview: Luminary Sampei wants you to kill 10 Infected Foreans among the Ruins of Velon Das.
  - objectives: Kill 10 Infected Foreans. • Kill 10 Infected Foreans among the Ruins of Velon Das.
  - walkthrough: The Infected Foreans are found in the Ruins of Velon Das area of Valverde Plateau which is located at X, Y: -477.3, -209.8. They don't have a lot of health but hit fairly hard. The easiest way to get to the Ruins of Velon Das is to teleport to the Camp Resistance waypoint and go northeast.
- **TaRapedia mission list** [New Velon Village mission list](https://tabularasa.fandom.com/wiki/New_Velon_Village_mission_list): zone Plateau, area New Velon Village, given by Luminary Sampei
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4 …](https://web.archive.org/web/20090107005649/http://eu.playtr.com/en/news_article/deployment_102_patchnotes_known_issues_4th_july_2008): “Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost.”
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issue …](https://web.archive.org/web/20090105235435/http://eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issues_5th_june_2008_test_server): “Added sick Foreans to the Velon Das Ruins on Valverde Plateau. / New Plateau mission “Village Of The Damned,” available from Luminary Sampeii at New Velon Village, introduces the new “Velon Hollow” mission arc. / Plateau Mission “Clean Slate” has been reworked as a spawned escort, allowing multiple players to accept it simultaneously.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/feedback_friday_23_may_2008](https://web.archive.org/web/20080530054922/http://eu.rgtr.com:80/en/news_article/feedback_friday_23_may_2008): “ollow is a new instance off of Plateau for levels 30-32. To access Velon Hollow, you must complete the “Book of Velonar” mission arc and accept the “Village Of The Damned” mission from Luminary Sampeii at New Velon Village. He will then introduce you to the new “Velon Hollow” arc with 8 new missions.”
- **RGTR official site** [www.rgtr.com/news/patch_notes_public_test/deployment_10.html](https://web.archive.org/web/20081117082235/http://www.rgtr.com/news/patch_notes_public_test/deployment_10.html): “se missions are still available for players. Plateau: Velon Hollow - This instance is now open for players to experience, along with the new missions Village of the Damned, Ancient Mysteries, Deadly Silence, These Old Bones, Mountain Spring Toxins, Holy Vengeance, and The Better Half. These missions can be obtained at New Velon Village and Wedge Rock Outpost. Boot Camp - Tutorial text now properly instructs player to press "T" to talk to NPCs …”
- **NPCs (TaRapedia NPC pages):**
  - [Luminary Sampei](https://tabularasa.fandom.com/wiki/Luminary_Sampei): Plateau, New Velon Village, /loc 259, 352, 619 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 1
  - Leads to: Mountain Spring Toxins (id 1783) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins)
  - Leads to: Deadly Silence (id 1974) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Deadly_Silence), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Better Half (id 1975) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Better_Half)
  - Leads to: Ancient Mysteries (id 1978) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Ancient_Mysteries)
  - Leads to: Mountain Spring Toxins (id 1979) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins)
  - Leads to: The Order Of Velon (id 1982) — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1982 — The Order Of Velon [instance]


- **Instance:** Velon Hollow — an objective conversation's NPC package 2263 spawns in this instance (map 2136) — source: client objectiveconversation + this PR's World data
- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 45000 XP; Class V Standard Med Pack — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Luminary Sampei: Plateau, New Velon Village, /loc 259, 352, 619 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Luminary_Sampei)
- **Client:** Luminary Sampei at New Velon Village wants you to locate Elder Hathan, the leader of The Order Of Velon, at the Forean Monestary inside Velon Hollow.
  - objectives (client): Locate Elder Hathan. — Locate Elder Hathan inside Velon Hollow.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Luminary Sampei → Elder Hathen
  - reward: 45000 XP; items: Class V Standard Med Pack
  - prerequisites: Requires completed mission 1981
- **PlayTR EU** [eu.playtr.com:80/en/field_training/guide/velon_hollow](https://web.archive.org/web/20090201021913/http://eu.playtr.com:80/en/field_training/guide/velon_hollow): “One of our primary goals was to make sure that the environments told the story as effectively as the missions did. The Order of Velon spent centuries carving their temples out of the mountain, and we wanted players to recognize this when they discover the first of the Forean’s main living areas. Our world builders painstakingly sculpted the terrain and worked closely with our environment artists to integrate the Forean architecture.”
- **NPCs (TaRapedia NPC pages):**
  - [Luminary Sampei](https://tabularasa.fandom.com/wiki/Luminary_Sampei): Plateau, New Velon Village, /loc 259, 352, 619 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Special Deliveries chain (16 missions), step 2
  - Requires: Village Of The Damned (id 1981) — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1983 — Clean Slate


- **Rebuild summary:**
  - level: 30 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 29 (possibly less) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Clean_Slate); 30 — source: [TR MMO DB](https://web.archive.org/web/20120731174215/http://tr.mmodb.com/quests/clean-slate-233.php)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 40000 XP; 4650 credits; choose 1 of: Bio Armor Helmet, Graviton Armor Helmet, Mech Armor Helmet, Stealth Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 4,650 credits; Equipment — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Clean_Slate)
  - reward: 0 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Corporal Wainwright: Plateau, New Velon Village, /loc 194, 354, 519 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Corporal_Wainwright)
- **Client:** This soldier appears to be suffering from amnesia. Escort him to Fort Defiance and speak to Special Agent Perdu.
  - objectives (client): Escort the soldier to Agent Perdu. — Escort the amnesiac soldier to Special Agent Perdu at Fort Defiance.; Kill the soldier! — The soldier has gone bersek! Take him down!; Special Agent Perdu wants to talk to you. — Special Agent Perdu wants to talk to you about the soldier.
- **This PR:** revision `plateau_1_16_5`, level 30, solo, category 10000056 (Battlefield (Plateau)), giver Corporal Wainwright → Special Agent Perdu
  - reward: 40000 XP; 4650 credits; choose 1 of: Bio Armor Helmet, Graviton Armor Helmet, Mech Armor Helmet, Stealth Armor Helmet
- **TaRapedia** [Clean Slate](https://tabularasa.fandom.com/wiki/Clean_Slate): giver: Corporal Wainwright; at: Plateau; zone: Plateau; level: 29 (possibly less); requires: None; follow-up: Mind Bender; XP: 40,000; credits: 4,650; items: Equipment; reward from: Agent Perdu
  - overview: This soldier appears to be suffering from amnesia. Escort him to Fort Defiance and find him the help he needs. NOTE: THIS IS A TIMED MISSION. Note: Only the escort part is timed. You have as much time as you want for killing him.
  - objectives: Escort the soldier to Fort Defiance. • Escort the shell-shocked soldier to Fort Defiance and try to discover his true identity. • Time remaining: 04:00:00; Kill the soldier! • The soldier had gone berserk! Take him down!; Speak to Agent Perdu. • Agent Perdu wants to talk to you.
  - walkthrough: When the soldier returns to the Fort Defiance entrance, he turns hostile and you are directed to put him down. Several NPC's in the area will join in and sometimes you cannot even get a shot off.
- **TaRapedia mission list** [New Velon Village mission list](https://tabularasa.fandom.com/wiki/New_Velon_Village_mission_list): zone Plateau, area New Velon Village, given by Corporal Wainwright
- **Ten Ton Hammer** [plateau-targets-of-opportunity](https://www.tentonhammer.com/guides/plateau-targets-of-opportunity): “Clean Slate series of missions beginning with Corporal Wainright who I personally found just outside Fort Defiance. You'll need to escort him to the second shield and realize he's not what he seems. Killing him will continue the series with Agent Perdu. Plateau Sentinel: Kill 50 Maligo Elite Guardsmen - These souped up Bane not only pack a powerful blast but carry improved …”
- **TR MMO DB** [Clean Slate](https://web.archive.org/web/20120731174215/http://tr.mmodb.com/quests/clean-slate-233.php): level: 30; starts at npc: Corporal Wainwright; starts in zone: Plateau; objectives: Speak to Agent Perdu; series: Clean Slate
  - related mob Agent Perdu: level 40, Plateau — source: [TR MMO DB bestiary](https://web.archive.org/web/20071015134711/http://tr.mmodb.com:80/bestiary/agent-perdu-486.php)
- **TR MMO DB quest series** [Clean Slate](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/): Clean Slate (level 30, reward 0) → Mind Bender (level 30, reward 4800) → Mind Slayer (level 30, reward 6600) (series order as listed on the page; reward unit not stated)
- **BradyGames guide sample** [www.bradygames.com/content/images/9780744009439/samplechapter/tabulara …](https://web.archive.org/web/20090317004540/http://www.bradygames.com/content/images/9780744009439/samplechapter/tabularasa.pdf): “Clean Slate / 2 An Explanation / 2 of Field Duties / Tactical Maps / 1 / 3 and Mission / Briefings / Equipment and / 4 Crafting Tables / LOGOS COORDINATES / (USE /loc COMMAND) / POWER 381, 65 / 5 Enemy Intel / Expanded / LEGEND 6 Content / 1 Starting Point 3 Heavy Thrax Concentration 5 Luna Cavern, Power / WALKTHROUGH / 2 Bridge 4 Destination Outpost / Basic training isn’t a simple matter. Your first outpost assignment is Destination Outpost, a location overrun by Thrax t”
- **PlanetTR** [www.planettr.com/Default.aspx?tabid=58](https://web.archive.org/web/20061029084022/http://www.planettr.com/Default.aspx?tabid=58): “The name Tabula Rasa translates from Latin to “Clean Slate”, my view is that it refers to in-game, a new hope for Humanity against the evil Bane forces and to the game design, starting from a clean slate and brining new ideas to an ever increasing crowded MMO market. The development team was originally founded by Richard Garriott (The man behind games like Ultima and Ultima Online), Robert Garriott (Brother of Richard) and Starr Long. The …”
- **PlanetTR** [www.planettr.com:80/forums/showthread.php?s=fd7249500775a78b32f967de96 …](https://web.archive.org/web/20080202122634/http://www.planettr.com:80/forums/showthread.php?s=fd7249500775a78b32f967de9603a37b&t=3519&page=2): “Corporal Wainwright (spelling? ...name?) for the quest "Clean Slate" in plateau is a monster. It was honestly hilarious how busted he was, his armor was 100% out-regenning rage3+chaingun fire. I hit him with target painting to stop that nonsense, then got to his HP bar that didn't even move with full-auto fire on him. Had to get some help to take him down.”
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issue …](https://web.archive.org/web/20090105235435/http://eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issues_5th_june_2008_test_server): “ssion “Village Of The Damned,” available from Luminary Sampeii at New Velon Village, introduces the new “Velon Hollow” mission arc. / Plateau Mission “Clean Slate” has been reworked as a spawned escort, allowing multiple players to accept it simultaneously. / Boss level Magmonix will no longer spawn randomly with regular Magmonix. This should also increase the drop rate of any missions that require the player to kill Magmonix.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_novem …](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007): “Kardash Atta Colony loading screen should now have text. / Plateau “Clean Slate” – Corporal Wainwright will now aggro properly when escorted to the Fort Defiance entrance. / Wilderness “Too Close For Comfort” – Council Elder Solis will no longer aggro Thrax during ambush.”
- **NPCs (TaRapedia NPC pages):**
  - [Corporal Wainwright](https://tabularasa.fandom.com/wiki/Corporal_Wainwright): Plateau, New Velon Village, /loc 194, 354, 519 (named as giver/receiver)
  - [Agent Perdu](https://tabularasa.fandom.com/wiki/Agent_Perdu): Plateau, Fort Defiance (named as giver/receiver)

- **Chain** (explicit prerequisite links): Clean Slate chain (6 missions), step 1
  - Leads to: Mind Bender (id 905) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Clean_Slate), [TaRapedia](https://tabularasa.fandom.com/wiki/Mind_Bender), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Clean Slate", row 1 of 3 ([list](https://web.archive.org/web/20120731165006/http://tr.mmodb.com/quests/seriesname-clean_slate/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 2007 — Soyuz Salvage: Plateau


- **Rebuild summary:**
  - level: 34 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Soyuz_Salvage%3A_Plateau)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP; 5000 credits; Soyuz ISS Model Rocket — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Commander Grissom: Plateau, Fort Defiance, /loc -92, 399, 914 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Commander_Grissom)
- **Client:** Locate one of the pieces of the Orbital Module from General British's Soyuz, somewhere on Plateau, and return it to Commander Grissom at Fort Defiance.
  - objectives (client): Salvage the Soyuz Debris — Locate and salvage the Soyuz Orbital Module Debris that crashed somewhere on Plateau.
- **This PR:** revision `plateau_1_16_5`, level 34, solo, category 10000056 (Battlefield (Plateau)), giver Commander Grissom
  - reward: 50000 XP; 5000 credits; items: Soyuz ISS Model Rocket
- **TaRapedia** [Soyuz Salvage: Plateau](https://tabularasa.fandom.com/wiki/Soyuz_Salvage%3A_Plateau): giver: Commander Grissom; at: Fort Defiance; zone: Plateau; level: 30; reward from: Commander Grissom
  - overview: Locate one of the pieces of the Orbital Module from General British's Soyuz, somewhere on Plateau, and return it to Commander Grissom at Fort Defiance.
  - objectives: Salvage the Soyuz Debris
- **NPCs (TaRapedia NPC pages):**
  - [Commander Grissom](https://tabularasa.fandom.com/wiki/Commander_Grissom): Plateau, Fort Defiance, /loc -92, 399, 914 (NPC page lists this mission)

### 2016 — A Mystery Unearthed


- **Rebuild summary:**
  - level: 50 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 7500 XP — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 7500 XP; none credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Mystery_Unearthed)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Archeologist Wynne Topper: Wilderness, Twin Pillars Outpost, /loc -132, 232, -628 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Archeologist_Wynne_Topper)
- **Client:** General Thadeus T. Bailey needs to be made aware of Archeaologist Topper's discovery from his excavations and the construction of the new wormhole taking place near Twin Pillars on Wilderness. Topper mentioned a "missing piece to the puzzle" regarding the Eloh structures being related to the …
  - objectives (client): Report to General Thadeus T. Bailey (Plateau - Fort Defiance) — Report to General Thadeus T. Bailey at Fort Defiance on Plateau
- **This PR:** revision `plateau_1_16_5`, level 50, solo, category 10000056 (Battlefield (Plateau)), giver Archaeologist Wynne Topper → General Thaddeus T. Bailey
  - reward: 7500 XP
- **TaRapedia** [A Mystery Unearthed](https://tabularasa.fandom.com/wiki/A_Mystery_Unearthed): giver: Archeologist Wynne Topper; at: Twin Pillars Outpost; zone: Plateau; requires: none; follow-up: Symbolism; XP: 7500; credits: none; reward from: General Thaddeus T. Bailey
  - overview: Notify General Bailey of the new discoveries at Twin Pillars.
  - objectives: • Report to General Thaddeus T. Bailey in Fort Defiance on Plateau.
  - walkthrough: • Talk to General Thaddeus T. Bailey at -68, 950 in Fort Defiance on Plateau.
- **TaRapedia mission list** [Twin Pillars mission list](https://tabularasa.fandom.com/wiki/Twin_Pillars_mission_list): zone Wilderness, area Twin Pillars, given by Archeologist Wynne Topper
- **PlayTR EU** [eu.playtr.com/en/news/patch_notes/P0/](https://web.archive.org/web/20090105235011/http://eu.playtr.com/en/news/patch_notes/P0/): “he Central Dispatch Unit) inside the instance. Control Point is recommended for squads of players between level 25-32 and is repeatable. / Mission: “A Mystery Unearthed” - new level 50 mission offered by NPC Archaeologist Wynne Topper at Twin Pillars on Concordia: Wilderness. / There are now many more lootable chests on Plains and Mires.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/deployment_15_patchnotes_known_issues_d …](https://web.archive.org/web/20081221223617/http://eu.rgtr.com:80/en/news_article/deployment_15_patchnotes_known_issues_december_2008_live): “utpost on Concordia Wilderness for further instructions or, report to General Thadeus T. Bailey if you have already completed Wynne Topper’s mission “A Mystery Unearthed”.”
- **RGTR official site** [www.rgtr.com/news/patch_notes/deployment_14_11112008.html](https://web.archive.org/web/20081115092604/http://www.rgtr.com/news/patch_notes/deployment_14_11112008.html): “cheologists have unearthed an array of previously unknown Eloh Conduits at Twin Pillars Outpost. Construction is underway in the vicinity. / Mission: "A Mystery Unearthed" - new level 50 mission offered by NPC Archaeologist Wynne Topper at Twin Pillars on Concordia: Wilderness. / The non-mission lootable chests on the Plains and Mires maps have been changed in the following ways:”
- **NPCs (TaRapedia NPC pages):**
  - [Archeologist Wynne Topper](https://tabularasa.fandom.com/wiki/Archeologist_Wynne_Topper): Wilderness, Twin Pillars Outpost, /loc -132, 232, -628 (NPC page lists this mission)
  - [General Thaddeus T. Bailey](https://tabularasa.fandom.com/wiki/General_Thaddeus_T._Bailey): Plateau, Fort Defiance, /loc -68, 404, 949 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): A Mystery Unearthed chain (2 missions), step 1
  - Leads to: Symbolism (id 2017) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Mystery_Unearthed), [TaRapedia](https://tabularasa.fandom.com/wiki/Symbolism), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 2017 — Symbolism


- **Rebuild summary:**
  - level: 50 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Plateau zone level range is 30-34 — source: [Ellatha map: Plateau](https://www.ellatha.com/tr/Plateau_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Plateau missions are level 25–33 (median 30, 10th–90th percentile) — derived from 59 missions in this file with a sourced level
  - reward: 50000 XP — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 50000 XP; 0 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Symbolism)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Archeologist Wynne Topper: Wilderness, Twin Pillars Outpost, /loc -132, 232, -628 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Archeologist_Wynne_Topper)
- **Client:** Archaeologist Topper has completed the construction of the new wormhole at Twin Pillars and the excavation of the Logos Conduits surrounding it. The problem is, the AFS still hasn't been able to identify the unusual Logos symbol appearing on the ruins. I need you to go and try to find out what that …
  - objectives (client): Question the Hermit (Valverde: Pools) — Head to Valverde: Pools and speak to the Hermit. He may have info regarding the strange Logos Symbol appearing at the dig site at Twin Pillars.; Speak to Retread McCormick (Retread Camp: Marshes); Seek out the Logos Symbol (Enigma Caverns: Wilderness); Report to Archaeologist Topper (Twin Pillars: Wilderness)
- **This PR:** revision `plateau_1_16_5`, level 50, solo, category 10000056 (Battlefield (Plateau)), giver General Thaddeus T. Bailey → Archaeologist Wynne Topper
  - reward: 50000 XP
  - prerequisites: Requires completed mission 2016
- **TaRapedia** [Symbolism](https://tabularasa.fandom.com/wiki/Symbolism): giver: General Thaddeus T. Bailey; at: Fort Defiance; zone: Plateau; requires: A Mystery Unearthed; follow-up: none; XP: 50000; credits: 0; reward from: Archeologist Wynne Topper
  - overview: Identify the new Logos symbol on the Logos Conduits uncovered at Twin Pillars and find its meaning.
  - objectives: • Question the Hermit (Valverde: Pools) • Speak to Retread McCormick (Retread Camp: Marshes) • Seek out the Logos symbol (Enigma Caverns: Wilderness) • Report to Archeologist Topper (Twin Pillars: WIlderness)
  - walkthrough: • Talk to the Hermit on Pools at -430, 115. • Speak to Retread McCormick on Marshes at -292, -768. • Get the Earth (logos) in Enigma Caverns. Go into the caverns and turn left (SE) and go as far as you can then jump down into the water. Continue on SE until you get to a waterfall. Jump to the base of the waterfall and continue on SE to the door to Earth (logos) at -849, -717. Defend (logos), Yours (logos) & Planet (logos) are required to open the door. • Return to Archeologist Wynne Topper in Twin Pillars at -132, -628. • (Activating the Logos Conduits turns on the Wormhole to Earth.)
- **TaRapedia mission list** [Fort Defiance mission list](https://tabularasa.fandom.com/wiki/Fort_Defiance_mission_list): zone Plateau, area Fort Defiance, given by General Thaddeus T. Bailey
- **NPCs (TaRapedia NPC pages):**
  - [Archeologist Wynne Topper](https://tabularasa.fandom.com/wiki/Archeologist_Wynne_Topper): Wilderness, Twin Pillars Outpost, /loc -132, 232, -628 (NPC page lists this mission)
  - [General Thaddeus T. Bailey](https://tabularasa.fandom.com/wiki/General_Thaddeus_T._Bailey): Plateau, Fort Defiance, /loc -68, 404, 949 (NPC page lists this mission)
  - [Hermit](https://tabularasa.fandom.com/wiki/Hermit): /loc -430, 921, 115 (NPC page lists this mission)
  - [Retread McCormick](https://tabularasa.fandom.com/wiki/Retread_McCormick): /loc -268, 216, -812 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): A Mystery Unearthed chain (2 missions), step 2
  - Requires: A Mystery Unearthed (id 2016) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Mystery_Unearthed), [TaRapedia](https://tabularasa.fandom.com/wiki/Symbolism), this PR's World data (`mission_content_definition` / `mission_reward_*`)

## Pools

### 904 — Weapons Trafficking


- **Rebuild summary:**
  - level: 33 — source: [TR MMO DB](https://web.archive.org/web/20090210212523/http://tr.mmodb.com:80/quests/weapons-trafficking-282.php)
  - level ESTIMATE from zone: the Pools zone level range is 34-38 — source: [Ellatha map: Pools](https://www.ellatha.com/tr/Pools_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Pools missions are level 33–35 (median 33, 10th–90th percentile) — derived from 35 missions in this file with a sourced level
  - reward: 65,000 XP; 5,400 credits; AccuMax Incendiary Blade lvl 36; ChiTech Conical Healing Disc lvl 35; Teleract Cryogenic Shotgun lvl 37; Vitalius Virulent Staff lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Weapons_Trafficking)
  - reward: 5400 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20090210212523/http://tr.mmodb.com:80/quests/weapons-trafficking-282.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Amee Corman: Pools, Retread Camp — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Amee_Corman)
- **Client:** Bring five (5) weapon prototypes back to Amee Corman.
  - objectives (client): Get The Prototype Weapons; Find Prototype 1; Find Prototype 2; Find Prototype 3; Find Prototype 4; Find Prototype 5
- **TaRapedia** [Weapons Trafficking](https://tabularasa.fandom.com/wiki/Weapons_Trafficking): giver: Amee Corman; at: Retread Camp; zone: Pools; follow-up: Retreads to Retreads; XP: 65,000; credits: 5,400; items: AccuMax Incendiary Blade lvl 36; ChiTech Conical Healing Disc lvl 35; Teleract Cryogenic Shotgun lvl 37; Vitalius Virulent Staff lvl 35; reward from: Amee Corman
  - overview: Bring five (5) weapon prototypes back to Amee Corman.
  - objectives: • Find Prototype 1 • Prototype Rifle 0 / 1 • Find Prototype 2 • Prototype Pistol 0 / 1 • Find Prototype 3 • Prototype Net Gun 0 / 1 • Find Prototype 4 • Prototype Injector Gun 0 / 1 • Find Prototype 5 • Prototype Grenade Launcher 0 / 1
  - walkthrough: The items must be found in sequence. This means that possibly you will pass Bane footlockers that will eventually open with the items but not until you retrace your path.
- **TRWiki archive** [Weapons Trafficking](https://web.archive.org/web/20080117063428/http://www.tabularasawiki.org/action/view/Mission:_Weapons_Trafficking): This article is a stub . You can help the Tabula Rasa Wiki by expanding it . The voice over for this mission (from Amee Corman) is a male voice. Pools Lamna Armory Amee Corman Foreas/Valverde/Pools/Retread Camp Briefing Text "You're looking for information about the Eloh, yes? That's fine, but …
- **TaRapedia mission list** [Lamna Armory mission list](https://tabularasa.fandom.com/wiki/Lamna_Armory_mission_list): zone Pools, given by Amee Corman
- **TaRapedia mission list** [Retread Camp mission list](https://tabularasa.fandom.com/wiki/Retread_Camp_mission_list): zone Pools, area Retread Camp, given by Amee Corman
- **TR MMO DB** [Weapons Trafficking](https://web.archive.org/web/20090210212523/http://tr.mmodb.com:80/quests/weapons-trafficking-282.php): level: 33; starts at npc: Amee Corman; starts in zone: Pools; objectives: Bring 5 Weapon Prototypes to Amee Corman; description: TIP: You need to obtain the weapon prototypes from Bane Footlockers, but they must be obtained in order. You won't be able to get #2 until you have first obtained #1, etc.; reward: 5400; series: Weapons Trafficking
- **TR MMO DB quest series** [Weapons Trafficking](https://web.archive.org/web/20120731165344/http://tr.mmodb.com/quests/seriesname-weapons_trafficking/): Retreads to Retreads (level 33, reward 3500) → Weapons Trafficking (level 33, reward 5400) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Amee Corman](https://tabularasa.fandom.com/wiki/Amee_Corman): Pools, Retread Camp (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 6
  - Requires: New Orders From The General (id 1541) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/New_Orders_From_The_General)
  - Leads to: Retreads to Retreads (id 1751) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retreads_to_Retreads), [TaRapedia](https://tabularasa.fandom.com/wiki/Weapons_Trafficking)
- **TR MMO DB quest series:** "Weapons Trafficking", row 2 of 2 ([list](https://web.archive.org/web/20120731165344/http://tr.mmodb.com/quests/seriesname-weapons_trafficking/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

## Ustor Yard

### 1190 — Out of Thin Air [instance]


- **Instance:** Ustor Yard — NPC Maligo Base Prisoner (NPC page lists this mission) is in Ustor Yard — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Maligo_Base_Prisoner)
- **Instance mentioned:** Maligo Base — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 32 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731173712/http://tr.mmodb.com/quests/startingzone-ustor_yard/)
  - reward: 45000 XP; 6400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Out_of_Thin_Air)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Maligo Base Prisoner: Ustor Yard, /loc 19, 178, 121 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Maligo_Base_Prisoner)
- **Client:** Obtain an environmental protection stim pack from a Bane patrol and escort the Maligo escapee out of Ustor Yard.
  - objectives (client): Escort Maligo Prisoner Out of Ustor Yard.; Ambush Thrax Patrols in the Area and Obtain an Environmental Protection Stim …; Return to Maligo Prisoner and Give Her the Environmental Protection Stim Pack
- **TaRapedia** [Out of Thin Air](https://tabularasa.fandom.com/wiki/Out_of_Thin_Air): giver: Maligo Base Prisoner; at: Ustor Yard; zone: Ustor Yard; requires: None; follow-up: None; XP: 45000; credits: 6400; reward from: Headquarters
  - overview: Obtain an environmental protection stim pack from a Bane patrol and escort the Maligo escapee out of Ustor Yard.
  - objectives: • Ambush Thrax Patrols in the Area and Obtain an Environmental Protection Stim Pack • Return to Maligo Prisoner and Give Her the Environmental Protection Stim Pack. • Escort Maligo Prisoner Out of Ustor Yard.
  - walkthrough: When you accept the mission a small Bane patrol will spawn to the south east of the waste processing pool. Kill and loot this patrol to find the environmental stim pack. The prisoner wants to be escorted out of the west instance exit. This won't be accessible until you have completed Monkey Wrench. You should complete this and scout ahead to clear out some of the Bane before handing over the stim pack. Just clear out the Bane east of the yard before west exit. The ones in the yard respawn after death. Run into yard and stick to the south side. You can easily make it into the exit without …
- **TaRapedia mission list** [Ustor Yard mission list](https://tabularasa.fandom.com/wiki/Ustor_Yard_mission_list): zone Plateau, given by Maligo Base Prisoner
- **NPCs (TaRapedia NPC pages):**
  - [Maligo Base Prisoner](https://tabularasa.fandom.com/wiki/Maligo_Base_Prisoner): Ustor Yard, /loc 19, 178, 121 (NPC page lists this mission)

