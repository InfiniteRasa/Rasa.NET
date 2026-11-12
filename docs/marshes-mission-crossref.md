# Valverde Marshes missions: sources (cross-reference)

The 63 missions of the Valverde Marshes quest PR, taken from the full mission cross-reference of the 1.16.5.0
client. For each mission: the client text and objectives, the sources found (TaRapedia, TR MMO DB, Ellatha,
Ten Ton Hammer, archived fan and official pages, patch notes) and a rebuild summary with the source of each
value. Values without a source are marked ESTIMATE. "This PR" rows (where present) show the data this PR adds.

Playtest status and the deviations from the original game: `docs/marshes-playtest.md`.

| Zone | Missions |
| --- | ---: |
| Marshes | 63 |

## Marshes

### 497 — Forean Flu


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB](https://web.archive.org/web/20090214092425/http://tr.mmodb.com:80/quests/forean-flu-313.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 60000 XP; 5250 credits; choose 1 of: Bio Armor Boots, Mech Armor Boots, Graviton Armor Helmet, Reflective Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 60,000 XP; 5,250 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Forean_Flu)
  - reward: 5250 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20090214092425/http://tr.mmodb.com:80/quests/forean-flu-313.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Shaman Masai: Marshes, Plateau Point, /loc -735, 217, 171 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Shaman_Masai)
- **Client:** Escort the Forean Warrior to Field Medic DuBois in Falcon Hold Outpost.
  - objectives (client): Escort the Forean Warrior to Falcon Hold
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Shaman Masai → Field Medic DuBois
  - reward: 60000 XP; 5250 credits; choose 1 of: Bio Armor Boots, Mech Armor Boots, Graviton Armor Helmet, Reflective Armor Legs
- **TaRapedia** [Forean Flu](https://tabularasa.fandom.com/wiki/Forean_Flu): giver: Shaman Masai; at: Plateau Point; zone: Marshes; requires: none; follow-up: none; XP: 60,000; credits: 5,250; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Field Medic DuBois
  - overview: Escort the Forean Warrior to Field Medic DuBois in Falcon Hold Outpost.
  - objectives: • Escort the Forean Warrior to Falcon Hold.
  - walkthrough: Escort the forean warrior from Plateau Point to Falcon Hold by following the north road.
- **TaRapedia mission list** [Plateau Point mission list](https://tabularasa.fandom.com/wiki/Plateau_Point_mission_list): zone Marshes, area Plateau Point, given by Shaman Masai
- **TR MMO DB** [Forean Flu](https://web.archive.org/web/20090214092425/http://tr.mmodb.com:80/quests/forean-flu-313.php): level: 33; starts at npc: Shaman Masai; starts in zone: Marshes; objectives: Escort the Forean Warrior to Field Medic Dubois at Falcon Hold; description: TIP: Escort the Forean Warrior to Field Medic Dubois at Plateau Point. This is along the road to the north.; reward: 5250
  - related mob Field Medic Dubois (not in the archived bestiary pages)
- **NPCs (TaRapedia NPC pages):**
  - [Shaman Masai](https://tabularasa.fandom.com/wiki/Shaman_Masai): Marshes, Plateau Point, /loc -735, 217, 171 (named as giver/receiver)
  - [Field Medic DuBois](https://tabularasa.fandom.com/wiki/Field_Medic_DuBois): /loc -148, 219, 621 (named as giver/receiver)

### 498 — Cannister Collection


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cannister_Collection); 33 — source: [TR MMO DB](https://web.archive.org/web/20120731172213/http://tr.mmodb.com/quests/cannister-collection-136.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Cryogenic Injector Gun, Pulse Chaingun, Electric Rifle, Rocket Launcher — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; Vitalius Cryogenic Injector Gun lvl 37; Teleract Pulse Chaingun lvl 33; Animatics Electric Rifle lvl 36; Teleract Rocket Launcher lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cannister_Collection)
  - reward: 5400 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20120731172213/http://tr.mmodb.com/quests/cannister-collection-136.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sergeant Starling: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Starling)
- **Client:** Fight Bane in the western marshes. If you find any objects of interest, return with them to Sergeant Starling in Paludos.
  - objectives (client): Track down information on Bane gas harvesting
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant Starling
  - reward: 65000 XP; 5400 credits; choose 1 of: Cryogenic Injector Gun, Pulse Chaingun, Electric Rifle, Rocket Launcher
- **TaRapedia** [Cannister Collection](https://tabularasa.fandom.com/wiki/Cannister_Collection): giver: Sergeant Starling; at: Paludos; zone: Marshes; level: 35; requires: none; follow-up: none; XP: 65,000; credits: 5,400; items: Vitalius Cryogenic Injector Gun lvl 37; Teleract Pulse Chaingun lvl 33; Animatics Electric Rifle lvl 36; Teleract Rocket Launcher lvl 34; reward from: Sergeant Starling
  - overview: Track the Bane forces North and West of Paludos and retrieve the Bane Data pot for Sergeant Starling.
  - objectives: • Track down information on Bane gas harvesting
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Sergeant Starling
- **TR MMO DB** [Cannister Collection](https://web.archive.org/web/20120731172213/http://tr.mmodb.com/quests/cannister-collection-136.php): level: 33; starts at npc: Sergeant Starling; starts in zone: Marshes; objectives: Track down information on the Bane Gas Harvesters, Return to Sergeant Starling; reward: 5400
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Starling](https://tabularasa.fandom.com/wiki/Sergeant_Starling): /loc -268, 216, -812 (named as giver/receiver)

### 539 — What's that Gas?


- **Rebuild summary:**
  - level: 34 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F); 33 — source: [TR MMO DB](https://web.archive.org/web/20081204222849/http://tr.mmodb.com/quests/whats-that-gas-134.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Cryogenic Grenade Launcher, Incendiary Leech Gun, Electric Net Gun, Pistol — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; Teleract Cryogenic RPG Launcher lvl 35; ChiTech Incendiary Leech Gun lvl 34; Eclipse Electric Net Gun lvl 36; Hellstrom Pistol lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
  - reward: 5400 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081204222849/http://tr.mmodb.com/quests/whats-that-gas-134.php)
  - drops / mission items: : If we are to know where the sickness comes from, we must know where it lies — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
  - drops / mission items: Calla Fern Fronds are dropped by the Swamp Grubbers in the area around Serenity Pass northwest of Retread City — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
  - location: NPC Shaman Gali: Marshes, /loc -286, 218, 66 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Shaman_Gali)
  - location: /loc 153 218 370 Karlman 04:09, 10 March 2008 (UTC) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
- **Client:** Collect the 5 calla fern fronds from east of Paludos or northeast of Falcon Hold, 10 Swamp Grubber hides, 5 Maw glands, and 5 Mox hearts. Return them to Shaman Geli in Paludos for analysis.
  - objectives (client): Gather 5 Calla Fern Fronds; Gather 10 Swamp Grubber Hides; Collect 5 Maw Glands; Collect 10 Mox Hearts
- **This PR:** revision `marshes_1_16_5`, level 34, solo, category 10000051 (Battlefield (Marshes)), giver Shaman Geli
  - reward: 65000 XP; 5400 credits; choose 1 of: Cryogenic Grenade Launcher, Incendiary Leech Gun, Electric Net Gun, Pistol
- **TaRapedia** [What's that Gas?](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F) [stub]: giver: Shaman Gali; at: Paludos; zone: Marshes; level: 34; requires: none; XP: 65,000; credits: 5,400; items: Teleract Cryogenic RPG Launcher lvl 35; ChiTech Incendiary Leech Gun lvl 34; Eclipse Electric Net Gun lvl 36; Hellstrom Pistol lvl 35; reward from: Shaman Gali
  - overview: Collect the 5 calla fern fronds from east of Paludos or northeast of Falcon Hold, 10 swamp grubber hides, 5 maw glands, and 5 mox hearts. Return them to Shaman Geli in Paludos for analysis.
  - objectives: • Gather 5 Calla Fern Fronds • Gather 10 Swamp Grubber Hides • Collect 5 Maw Glands • Collect 10 Mox Hearts
  - walkthrough: Calla Fern Fronds are dropped by the Swamp Grubbers in the area around Serenity Pass northwest of Retread City. Was able to pick them NE of Paludos, just NE of the North Est watch.. /loc 153 218 370 Karlman 04:09, 10 March 2008 (UTC)
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Shaman Geli
- **TR MMO DB** [What's That Gas](https://web.archive.org/web/20081204222849/http://tr.mmodb.com/quests/whats-that-gas-134.php): level: 33; starts at npc: Shaman Gali; starts in zone: Marshes; objectives: Gather 5 Calla Fern Fronds, Gather 10 Swamp Grubber Hides, Collect 5 Maw Glands, Collect 10 Mox Hearts; reward: 5400
- **Where (locations / coordinates):**
  - /loc 153 218 370 Karlman 04:09, 10 March 2008 (UTC) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
- **NPCs (TaRapedia NPC pages):**
  - [Shaman Gali](https://tabularasa.fandom.com/wiki/Shaman_Gali): Marshes, /loc -286, 218, 66 (named as giver/receiver)
  - [Shaman Geli](https://tabularasa.fandom.com/wiki/Shaman_Geli): /loc -268, 216, -812 (named as giver/receiver)
- **Drops / item sources:**
  - : If we are to know where the sickness comes from, we must know where it lies — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)
  - Calla Fern Fronds are dropped by the Swamp Grubbers in the area around Serenity Pass northwest of Retread City — source: [TaRapedia](https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F)

### 541 — Roll out the Welcome Wagon


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Roll_out_the_Welcome_Wagon); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5700 credits; choose 1 of: Laser Chaingun, Incendiary Net Gun, Exothermic Polarity Gun, Sonic Shotgun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,700 credits; AccuMax Laser Chaingun lvl 38; ChiTech Incendiary Net Gun lvl 36; Vextronics Exothermic Polarity Gun lvl 38; Vitalius Sonic Shotgun lvl 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Roll_out_the_Welcome_Wagon)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
- **Client:** Watch for Overseer Cast'ka inspecting the southern Bane guard towers. Elminate him and report back to Elder Toida in Paludos.
  - objectives (client): Eliminate the Bane Boss, Cast'ka, and report back to Elder Toida
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Elder Toida
  - reward: 75000 XP; 5700 credits; choose 1 of: Laser Chaingun, Incendiary Net Gun, Exothermic Polarity Gun, Sonic Shotgun
- **TaRapedia** [Roll out the Welcome Wagon](https://tabularasa.fandom.com/wiki/Roll_out_the_Welcome_Wagon): giver: Elder Toida; at: Paludos; zone: Marshes; level: 36; requires: none; follow-up: none; XP: 75,000; credits: 5,700; items: AccuMax Laser Chaingun lvl 38; ChiTech Incendiary Net Gun lvl 36; Vextronics Exothermic Polarity Gun lvl 38; Vitalius Sonic Shotgun lvl 37; reward from: Elder Toida
  - overview: Watch for Overseer Cast'ka inspecting the southern Bane guard towers. Elminate him and report back to Elder Toida in Paludos.
  - objectives: • Elimination the Bane Boss, Cast'ka, and report back to Elder Toida.
  - walkthrough: He's usually found by the southern gate of the Bane Refueling Station in a big mob of Thrax. If you wait long enough he will come out and wander.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Elder Toida
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (named as giver/receiver)

### 542 — Bring Down the Kael


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB](https://web.archive.org/web/20080905085804/http://tr.mmodb.com/quests/bring-down-the-kael-132.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 150000 XP; 7600 credits; choose 1 of: Graviton Armor Vest, Reflective Armor Vest, Bio Armor Boots, Motor Assist Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 150,000 XP; 7600 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Bring_Down_the_Kael)
  - reward: 7600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080905085804/http://tr.mmodb.com/quests/bring-down-the-kael-132.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
- **Client:** Free the transported Forean prisoners on the Long Walk by eliminating all of their Thrax guardians. Report back to Elder Toida in Paludos on completion.
  - objectives (client): Eliminate the Bane Prison Guards to free the Escorted Foreans
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Elder Toida
  - reward: 150000 XP; 7600 credits; choose 1 of: Graviton Armor Vest, Reflective Armor Vest, Bio Armor Boots, Motor Assist Armor Vest
- **TaRapedia** [Bring Down the Kael](https://tabularasa.fandom.com/wiki/Bring_Down_the_Kael): giver: Elder Toida; at: Paludos; zone: Marshes; XP: 150,000; credits: 7600; reward from: Elder Toida
  - overview: The guards to be killed are in the Burning Mire, marching their prisoners along the Eastern side of the long defensive line separating the Mire from the rest of the map. Completing P'reo Das or one of the missions inside that operation triggers this mission.
  - objectives: • Eliminate the Bane Prison Guards to free the Escorted Foreans
  - walkthrough: Can be found at approximately 420, -300, they walk to the east of the dotted line on the map, and don't seem to fight back. [Idaho, Cassiopeia, Nov. 3, 2008] Found them at 500, 100, myself. (FenLee, Nov. 15, 2008) Found them at 502, 134.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Elder Toida
- **TR MMO DB** [Bring Down the Kael](https://web.archive.org/web/20080905085804/http://tr.mmodb.com/quests/bring-down-the-kael-132.php): level: 33; starts at npc: Elder Toida; starts in zone: Marshes; objectives: Slay the Bane Prison Guards to free the Escorted Foreans, Return to Elder Toida; reward: 7600
- **TR MMO DB NPC** [Elder Toida](https://web.archive.org/web/20081121082939/http://tr.mmodb.com/bestiary/elder-toida-575.php): Quest NPC, location Paludos, zone Marshes (starts this quest)
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (named as giver/receiver)

### 546 — A Taste of Their Own Medicine [instance]


- **Instance:** Logos Research Facility — client objective/log text: "Rendezvous With Specialist Hax in Logos Research Facility"
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB](https://web.archive.org/web/20080516063715/http://tr.mmodb.com:80/quests/a-taste-of-their-own-medicine-135.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Helmet, Mech Armor Gloves, Stealth Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine)
  - reward: 5550 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080516063715/http://tr.mmodb.com:80/quests/a-taste-of-their-own-medicine-135.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sergeant Fitz: /loc -211.1, 27.9, 230.2 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Fitz)
- **Client:** Rendezvous with Specialist Hax in the Logos Research Facility and help him hack into the Bane security system.
  - objectives (client): Secure the Bane Security Room for Specialist Hax.; Protect Specialist Hax While He Infiltrates the Bane Computer System.; HIDDEN!; HIDDEN!!; Rendezvous With Specialist Hax in Logos Research Facility
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant Fitz → Specialist Hax
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Helmet, Mech Armor Gloves, Stealth Armor Gloves
- **TaRapedia** [A Taste of Their Own Medicine](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine): giver: Sergeant Fitz; at: Temple of Paludos; zone: Marshes; requires: none; follow-up: Freedom's Just Another Word; Slay On Target; XP: 70,000; credits: 5,550; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Specialist Hax
  - overview: Rendezvous with Specialist Hax in Logos Research Facility and help him hack into the Bane security system.
  - objectives: • Rendezvous with Specialist Hax in Logos Research Facility.
  - walkthrough: Simply follow Specialist Hax and kill the enemies as they stream into the room.
- **TaRapedia mission list** [Logos Research Facility mission list](https://tabularasa.fandom.com/wiki/Logos_Research_Facility_mission_list): zone Marshes, given by Sergeant Fitz
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Sergeant Fitz
- **TR MMO DB** [A Taste Of Their Own Medicine](https://web.archive.org/web/20080516063715/http://tr.mmodb.com:80/quests/a-taste-of-their-own-medicine-135.php): level: 33; starts at npc: Sergeant Fitz; starts in zone: Marshes; objectives: Rendezvous with Specialist Hax in Logos Research Facility; reward: 5550; series: A Taste Of Their Own Medicine
  - related mob Specialist Hax (not in the archived bestiary pages)
- **TR MMO DB quest series** [A Taste Of Their Own Medicine](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/): A Taste Of Their Own Medicine (level 33, reward 5550) → Freedom's Just Another Word (level 35, reward 5700) → Leggo My Logos (level 35, reward 7600) → Slay On Target (level 35, reward 5700) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Specialist Hax](https://web.archive.org/web/20080516090148/http://tr.mmodb.com:80/bestiary/specialist-hax-601.php): Quest NPC, zone Logos Research Facility (related to this quest)
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Fitz](https://tabularasa.fandom.com/wiki/Sergeant_Fitz): /loc -211.1, 27.9, 230.2 (named as giver/receiver)

- **Chain** (explicit prerequisite links): A Taste of Their Own Medicine chain (5 missions), step 1
  - Leads to: Freedom's Just Another Word (id 548) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine), [TaRapedia](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Slay on Target (id 718) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine), [TaRapedia](https://tabularasa.fandom.com/wiki/Slay_On_Target), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "A Taste Of Their Own Medicine", row 1 of 4 ([list](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 548 — Freedom's Just Another Word


- **Instance mentioned:** Logos Research Facility — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB](https://web.archive.org/web/20080516071337/http://tr.mmodb.com:80/quests/freedoms-just-another-word-335.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5700 credits; choose 1 of: Virulent Disperser, Pulse Staff, Cryogenic Net Gun, Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,700 credits; Vextronics Virulent Disperser; Vextronics Pulse Staff; Teleract Cryogenic Net Gun; ChiTech Leech Gun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word)
  - reward: 5700 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080516071337/http://tr.mmodb.com:80/quests/freedoms-just-another-word-335.php)
  - drops / mission items: TIP: The key drops from a boss in the control room — source: [TR MMO DB](https://web.archive.org/web/20080516071337/http://tr.mmodb.com:80/quests/freedoms-just-another-word-335.php)
  - location: zone Marshes only
- **Client:** Obtain a security clearance pass which will open the Logos Research level 2 cell block.
  - objectives (client): Locate the Security Level 3 Keypass; Release the Prisoners from Cell block A.; Release the Prisoners from Cell block B.; Release the Prisoners from Cell block C.
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Specialist Hax → Captain Sakai
  - reward: 75000 XP; 5700 credits; choose 1 of: Virulent Disperser, Pulse Staff, Cryogenic Net Gun, Leech Gun
  - prerequisites: Requires completed mission 546
- **TaRapedia** [Freedom's Just Another Word](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word): giver: Specialist Hax; at: Logos Research Facility; zone: Marshes; requires: A Taste of Their Own Medicine; follow-up: Leggo My Logos; XP: 75,000; credits: 5,700; items: Vextronics Virulent Disperser; Vextronics Pulse Staff; Teleract Cryogenic Net Gun; ChiTech Leech Gun; reward from: Captain Sakai
  - overview: Free Captain Sakai from the cell blocks in the Logos Research Facility.
  - objectives: • Locate the Security Level 3 Keypass • Release the Prisoners from Cell block A • Release the Prisoners from Cell block B • Release the Prisoners from Cell block C
  - walkthrough: Kill your way to the control room overlooking the cell blocks, kill the boss holding the key, then open each cell block from the terminals in the same room.
- **TaRapedia mission list** [Logos Research Facility mission list](https://tabularasa.fandom.com/wiki/Logos_Research_Facility_mission_list): zone Marshes, given by Specialist Hax
- **TR MMO DB** [Freedom's Just Another Word](https://web.archive.org/web/20080516071337/http://tr.mmodb.com:80/quests/freedoms-just-another-word-335.php): level: 35; starts at npc: Specialist Hax; starts in zone: Logos Research Facility; objectives: Locate the Security Level 3 Keypass, Release the Prisoners from Cell Block A, Release the Prisoners from Cell Block B, Release the Prisoners from Cell Block C, Speak to Captain Sakai; description: TIP: The key drops from a boss in the control room. You can then open each of the cell blocks from the terminals in the control room.; reward: 5700; series: A Taste Of Their Own Medicine
  - related mob Captain Sakai (not in the archived bestiary pages)
- **TR MMO DB quest series** [A Taste Of Their Own Medicine](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/): A Taste Of Their Own Medicine (level 33, reward 5550) → Freedom's Just Another Word (level 35, reward 5700) → Leggo My Logos (level 35, reward 7600) → Slay On Target (level 35, reward 5700) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Captain Sakai](https://web.archive.org/web/20081121154136/http://tr.mmodb.com/bestiary/captain-sakai-744.php): Quest NPC, zone Logos Research Facility (related to this quest)
- **TR MMO DB NPC** [Specialist Hax](https://web.archive.org/web/20080516090148/http://tr.mmodb.com:80/bestiary/specialist-hax-601.php): Quest NPC, zone Logos Research Facility (starts this quest)
- **Drops / item sources:**
  - TIP: The key drops from a boss in the control room — source: [TR MMO DB](https://web.archive.org/web/20080516071337/http://tr.mmodb.com:80/quests/freedoms-just-another-word-335.php)

- **Chain** (explicit prerequisite links): A Taste of Their Own Medicine chain (5 missions), step 2
  - Requires: A Taste of Their Own Medicine (id 546) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine), [TaRapedia](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Leggo My Logos (id 550) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word), [TaRapedia](https://tabularasa.fandom.com/wiki/Leggo_My_Logos), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "A Taste Of Their Own Medicine", row 2 of 4 ([list](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** A Taste of Their Own Medicine (546) — source: the chain links above

### 550 — Leggo My Logos


- **Instance mentioned:** Logos Research Facility — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB](https://web.archive.org/web/20080516095045/http://tr.mmodb.com:80/quests/leggo-my-logos-336.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 80000 XP; 7600 credits; choose 1 of: Sonic Torqueshell Rifle, Cryogenic Blade, Pulse Cannon, Virulent Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 7,600 credits; Vextronics Sonic Torqueshell Rifle; Teleract Cryogenic Blade; ChiTech Pulse Cannon; Vextronics Virulent Leech Gun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Leggo_My_Logos)
  - reward: 7600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080516095045/http://tr.mmodb.com:80/quests/leggo-my-logos-336.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sergeant Fitz: /loc -211.1, 27.9, 230.2 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Fitz)
- **Client:** Destroy the primary and secondary receptive extraction tanks in the level 3 area and retrieve the Logos powering the facility.
  - objectives (client): Destroy Primary Maroxim Extraction Device to Obtain Extracted Receptive DNA; Retrieve Logos Element From Logos Analyzer; Get Bombs From Nearby Bane Crate.; Analyzation Systems Unstable. Escape Logos Research Before the Facility …; Destroy Secondary Maroxim Extraction Device to Obtain Extracted Receptive DNA; Escaped From Logos Research Self Destruct Successfully.; Speak With Lieutenant Russell.; Hidden Failed Self Destruct!; Logos Analysis Lab Security Breached. Disable Logos Analyzer Forcefield; Escaped From Logos Research. Return for Debriefing.
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Captain Sakai → Sergeant Fitz
  - reward: 80000 XP; 7600 credits; choose 1 of: Sonic Torqueshell Rifle, Cryogenic Blade, Pulse Cannon, Virulent Leech Gun
  - prerequisites: Requires completed mission 548
- **TaRapedia** [Leggo My Logos](https://tabularasa.fandom.com/wiki/Leggo_My_Logos) [stub]: giver: Captain Sakai; at: Logos Research Facility; zone: Marshes; requires: Freedom's Just Another Word; follow-up: None; credits: 7,600; items: Vextronics Sonic Torqueshell Rifle; Teleract Cryogenic Blade; ChiTech Pulse Cannon; Vextronics Virulent Leech Gun; reward from: Sergeant Fitz
  - overview: Destroy the primary and secondary receptive extraction tanks in the level 3 area and retrieve the logos stone powering the facility.
  - objectives: • Get Bombs From Nearby Bane Crate • Destroy Primary Maroxim Extraction Device to Obtain Extracted Receptive DNA • Destroy Secondary Maroxim Extraction Device to Obtain Extracted Receptive DNA • Logos Analysis Lab Security Breached. Disable Logos Analyzer Forcefield • Retrieve Logos Element From Logos Analyzer • Analyzation Systems Unstable. Escape Logos Research Before the Facility Explodes. • …
  - walkthrough: Kill your way through the base, blow up the tanks, and take the logos stone. If you die during your escape, head back into the facility towards Specialist Hax until the escape objective completes. Once you obtain the Logos element, the easiest way to escape is to use Personal Waypoint to get back to the entrance. Once you teleport the objective will complete.
- **TaRapedia mission list** [Logos Research Facility mission list](https://tabularasa.fandom.com/wiki/Logos_Research_Facility_mission_list): zone Marshes, given by Captain Sakai
- **TR MMO DB** [Leggo My Logos](https://web.archive.org/web/20080516095045/http://tr.mmodb.com:80/quests/leggo-my-logos-336.php): level: 35; starts at npc: Captain Sakai; starts in zone: Logos Research Facility; objectives: Get Bombs from Nearby Base Crate, Destroy Primary Maroxim Extraction Device to Obtain Extracted Receptive DNA, Destroy Secondary Maroxim Extraction Device to Obtain Extracted Receptive DNA, Disable Logos Analyzer Forcefield, Retrieve Logos Element from Logos Analyzer, Escape Logos Research Before …; reward: 7600; series: A Taste Of Their Own Medicine
  - related mob Sergeant Fitz (not in the archived bestiary pages)
- **TR MMO DB quest series** [A Taste Of Their Own Medicine](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/): A Taste Of Their Own Medicine (level 33, reward 5550) → Freedom's Just Another Word (level 35, reward 5700) → Leggo My Logos (level 35, reward 7600) → Slay On Target (level 35, reward 5700) (series order as listed on the page; reward unit not stated)
- **TR MMO DB NPC** [Captain Sakai](https://web.archive.org/web/20081121154136/http://tr.mmodb.com/bestiary/captain-sakai-744.php): Quest NPC, zone Logos Research Facility (starts this quest)
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1365.html](https://web.archive.org/web/20071209022816/http://planettr.com:80/forums/archive/index.php/t-1365.html): “wn anchors were actually below ground (although no errors were thrown) and re-ran path finding. / * Fixed bug with the timer in Logos Research mission Leggo My Logos. You now have the full displayed time to get out of there. / * Fixed TOO bug across all TOO missions on that would prevent Operations objective from completing if player had already completed the required missions.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_13th_novem …](https://web.archive.org/web/20071127113348/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_13th_november_2007): “pawn anchors were actually below ground (although no errors were thrown) and re-ran path finding. / Fixed bug with the timer in Logos Research mission Leggo My Logos. You now have the full displayed time to get out of there. / Fixed TOO bug across all TOO missions on that would prevent Operations objective from completing if player had already completed the required missions.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_novem …](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007): “gone in lieu of miasmas and the AFS folks now follow you through the map instead of the Foreans. / Fixed bug with the timer in Logos Research mission Leggo My Logos. You now have the full displayed time to get out of there. / Torcastra Prison – Info Sabotage: Instead of hitting a trigger to start the cut scene, you now use a switch. This should help avoid seeing the cut scene while in battle.”
- **Stratics TR forum** [boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Number=11987&page= …](https://web.archive.org/web/20071221003621/http://boards.stratics.com:80/php-bin/tr/showflat.php?Cat=&Number=11987&page=0&view=collapsed&sb=5&o=&fpart=1): “lieu of miasmas and the AFS folks now follow you through the map instead of the / Foreans. / Fixed bug with the timer in Logos Research mission Leggo My Logos. You now / have the full displayed time to get out of there. / Torcastra Prison  Info Sabotage: Instead of hitting a trigger to start the”
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Fitz](https://tabularasa.fandom.com/wiki/Sergeant_Fitz): /loc -211.1, 27.9, 230.2 (named as giver/receiver)

- **Chain** (explicit prerequisite links): A Taste of Their Own Medicine chain (5 missions), step 3
  - Requires: Freedom's Just Another Word (id 548) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word), [TaRapedia](https://tabularasa.fandom.com/wiki/Leggo_My_Logos), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: To a Subterranean Chamber (id 1674) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "A Taste Of Their Own Medicine", row 3 of 4 ([list](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 663 — Insertion Point


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 170000 XP; 6000 credits; choose 1 of: Class VIII Advanced Med Pack, Class VIII EMP Grenade, Class VIII Res Trauma Kit, Class VIII Concussion Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 170,000 XP; 6,000 credits; Class VIII Advanced Med Pack (6); Class VIII EMP Grenade (9); Class VIII Rest Trauma Kit (3); Class VIII Concussion Grenade (9) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Insertion_Point)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
  - location: At position (-88.2, 80.0, -160.4) there is a terminal to disable by a simple click all the mortar. — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Insertion_Point)
- **Client:** Infiltrate the Bane Supply Depot to the southeast by means of force or stealth. Once inside, find a way to disable the Bane mortars prior to the final AFS aerial assault.
  - objectives (client): Method 2: Infiltrate The Depot by Stealth; Method 1: Infiltrate The Depot by Force; Disable the above ground Bane Mortars — Disable the mortars above ground by means of destruction or by deactivation through a Bane terminal.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Elder Toida
  - reward: 170000 XP; 6000 credits; choose 1 of: Class VIII Advanced Med Pack ×6, Class VIII EMP Grenade ×9, Class VIII Res Trauma Kit ×3, Class VIII Concussion Grenade ×9
  - prerequisites: Requires level 37
- **TaRapedia** [Insertion Point](https://tabularasa.fandom.com/wiki/Insertion_Point): giver: Elder Toida; at: Paludos; zone: Marshes; requires: 37 lvl; follow-up: None; XP: 170,000; credits: 6,000; items: Class VIII Advanced Med Pack (6); Class VIII EMP Grenade (9); Class VIII Rest Trauma Kit (3); Class VIII Concussion Grenade (9); reward from: Headquarters
  - overview: Objectives: • Method 1: Infiltrate The Depot by Force • Method 2: Infiltrate The Depot by Stealth • Disable the above ground Bane Mortars
  - objectives: • Method 1: Infiltrate The Depot by Force • Method 2: Infiltrate The Depot by Stealth • Disable the above ground Bane Mortars
  - walkthrough: The force and stealth options refer to charging in the front gate or circling around the north edge to the back. To disable all mortar at once, go in the underground entering by the back of the base (entrance "maroxin storage" on the map), then follow the corridor. At position (-88.2, 80.0, -160.4) there is a terminal to disable by a simple click all the mortar. The mortar switch is located south west of the large circular room in the depot. Moosaman 15:15, 15 November 2008 (UTC) To stealth the missions, you must access the rear entrance without setting off the alarm. Stay well away from the …
- **TaRapedia mission list** [Bane Supply Depot mission list](https://tabularasa.fandom.com/wiki/Bane_Supply_Depot_mission_list): zone Marshes, given by Elder Toida
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?](https://web.archive.org/web/20080302101520/http://vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?): “* Fixed Waypoint inside Bane Supply Depot / * Added Mission indicators for "Insertion Point" on the Bane Mortars / * Added additional Mission Indicators for "Mind Bender"”
- **Where (locations / coordinates):**
  - At position (-88.2, 80.0, -160.4) there is a terminal to disable by a simple click all the mortar. — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Insertion_Point)
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (NPC page lists this mission)

- **Requires (name not matched to a mission):** 37 lvl — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Insertion_Point)

### 664 — Survivors


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20120731170916/http://tr.mmodb.com/quests/startingzone-bane_supply_depot/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 80000 XP; 6000 credits; choose 1 of: Class VIII Basic Med Pack, Class VIII Fragmentation Grenade, Class VIII EMP Bomb, Class VIII Cryogenic Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 80,000 XP; 6,000 credits; Class VIII Basic Med Pack (12); Class VIII Fragmentation Grenade (6); Class VIII EMP Bomb (2); Class VIII Cryogenic Grenade (9) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Survivors)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: Maligo Base: After using teleporter at 36 -62 -183 movement controls do not function correctly. — source: [PlayTR EU](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007)
- **Client:** Locate the downed C6801 hedgehog crew in the northeast. The last transmission received from them indicated some hostile activity in the region. Report to HQ once you've briefed with the survivors.
  - objectives (client): Locate the C6801 crew before they are killed; Bring Copilot Ortega supplies from downed dropships — Scavenge the downed dropships for supplies and return it to Ortega and his crew.; Eliminate the surrounding Bane attacking the Survivors Camp
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes))
  - reward: 80000 XP; 6000 credits; choose 1 of: Class VIII Basic Med Pack ×12, Class VIII Fragmentation Grenade ×6, Class VIII EMP Bomb ×2, Class VIII Cryogenic Grenade ×9
- **TaRapedia** [Survivors](https://tabularasa.fandom.com/wiki/Survivors): giver: Headquarters; at: Radio; zone: Marshes; requires: None; follow-up: None; XP: 80,000; credits: 6,000; items: Class VIII Basic Med Pack (12); Class VIII Fragmentation Grenade (6); Class VIII EMP Bomb (2); Class VIII Cryogenic Grenade (9); reward from: C-6801 Copilot Ortega
  - overview: Locate the downed C6801 hedgehog crew in the northwest. The last transmission received from them indicated some hostile activity in the region. Report to HQ once you've briefed with the survivors.
  - objectives: • Locate the C6801 crew before they are killed • Eliminate the surrounding Bane attacking the Survivors Camp • Bring Copilot Ortega supplies from downed dropships
  - walkthrough: You can put off accepting the mission until you arrive at the survivors camp northeast of the depot but do NOT kill off the bane attacking the survivors first. Once you arrive at the proper spot the mission will go from finding the survivors to killing the bane attacking them. If you kill them first then the mission cannot correct count down the remaining Bane left to be killed. Neofit26 13:23, 17 May 2008 (UTC) However, as of 1.7, make sure you accept this mission before you finish the other mission in this instance ("Insertion Point"), it will count towards your number of Operations in the …
- **TaRapedia mission list** [Bane Supply Depot mission list](https://tabularasa.fandom.com/wiki/Bane_Supply_Depot_mission_list): zone Marshes, given by Headquarters
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?](https://web.archive.org/web/20080302101520/http://vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?): “* Added check for "Final Task" to see if target creature has already been killed inside of Devil's Den. / * Updated mission log text for "Survivors". / * Fixed Waypoint inside Bane Supply Depot”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_janua …](https://web.archive.org/web/20080122184400/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_17th_january_2008_test_server): “Increased the hitpoints for the Fuelling Capsule on Pravus Research for the mission ‘A Means of Production’. / Timer for Bane Supply Depot mission ‘Survivors’ has been updated to 10 minutes. / You can now get the Bane Supply Depot mission ‘Survivor’s one time only, if you fail the mission or complete it to the second objective until the instance has been reset.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_26th_febru …](https://web.archive.org/web/20080303001652/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_26th_february_2008): “Added check for “Final Objective” to see if target creature has already been killed inside of Devil’s Den / Updated mission log text for “Survivors" / Fixed Waypoint inside Bane Supply Depot”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_novem …](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007): “Maligo Base: After using teleporter at 36 -62 -183 movement controls do not function correctly. / Map: Marshes: Bane Supply Depot: Mission: Survivors: Crates at downed dropships do not always give mission items. / Mission Error:Ashen Desert: “Gas Shortages” mission cannot be completed due to faulty bomb receptacles on gas pumps if you are disconnected from the server during the mission.”
- **RGTR official site** [rgtr.com:80/news/patch_notes/patch_notes_and_known_issues_109.html](https://web.archive.org/web/20071011062613/http://rgtr.com:80/news/patch_notes/patch_notes_and_known_issues_109.html): “Mission: It Lies in Ruins / Issue: Mission does not complete / continue when you talk to Lt. Morrison. / Mission: Survivors / Issue: The mission is not being offered to the players / Arieki Ligo Thunderhead”
- **RGTR official site** [www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html](https://web.archive.org/web/20081117172617/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_0.html): “Increased the hitpoints for the Fueling Capsule on Pravus Research for the mission ‘A Means of Production’. / Timer for Bane Supply Depot mission ‘Survivors’ has been updated to 10 minutes. / You can now get the Bane Supply Depot mission ‘Survivor’s’ one time only until the instance is reset. If you fail the mission or complete it to the second objective you will not be able to reacquire the mission until the instance has been reset.”
- **Where (locations / coordinates):**
  - Maligo Base: After using teleporter at 36 -62 -183 movement controls do not function correctly. — source: [PlayTR EU](https://web.archive.org/web/20071201203306/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_29th_november_2007)

### 718 — Slay on Target


- **Instance mentioned:** Logos Research Facility — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB](https://web.archive.org/web/20080413105143/http://tr.mmodb.com:80/quests/slay-on-target-337.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5700 credits; choose 1 of: Class VII EMP Bomb, Class VII Cryogenic Grenade, Class VII Standard Med Pack — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,700 credits; Class VII EMP Bomb; Class VII Cryogenic Grenade; Class VII Standard Med Pack — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Slay_On_Target)
  - reward: 5700 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080413105143/http://tr.mmodb.com:80/quests/slay-on-target-337.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Infiltrate the experimentation rooms, destroy the lab equipment and gather a lab sample.
  - objectives (client): Eliminate Coroner Larvax; Eliminate Dissector Xynnr.; Eliminate Overseer Mesgel.; Destroy Bane Lab Equipment.; Gather DNA Specimen for AFS Analysis.; Alarm signaled by Mesgel! Destroy the teleporter to stop the influx of troops.; HIDDEN - Part 3 Required!
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Specialist Hax
  - reward: 75000 XP; 5700 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Cryogenic Grenade ×4, Class VII Standard Med Pack ×2
  - prerequisites: Requires completed mission 546
- **TaRapedia** [Slay On Target](https://tabularasa.fandom.com/wiki/Slay_On_Target) [stub]: giver: Specialist Hax; at: Logos Research Facility; zone: Marshes; requires: A Taste of Their Own Medicine; follow-up: None; XP: 75,000; credits: 5,700; items: Class VII EMP Bomb; Class VII Cryogenic Grenade; Class VII Standard Med Pack; reward from: Headquarters
  - overview: Infiltrate the experimentation rooms, destroy the lab equipment, and gather a lab sample.
  - objectives: Destroy Bane Lab Equipment: • Lab Equiment Destroyed - 0/6 Gather DNA Specimen for AFS Analysis: • DNA Sample Jar - 0/1 Eliminate Coroner Larvax: • Logos Research Facility Keycard - Level 1 - 0/1 Eliminate Dissector Xynnr: • Logos Research Facility Keycard - Level 2 - 0/1 Eliminate Overseer Mesgel: • Logos Research Facility Keycard - Level 3 - 0/1
  - walkthrough: Kill the bosses, destroy the equipment, and get a sample from one of the shelves. If you fail to kill the third boss, which is quite likely, you get to destroy a teleporter instead. Be sure to enter each room to obtain the objective to kill each boss before you actually kill the boss.
- **TaRapedia mission list** [Logos Research Facility mission list](https://tabularasa.fandom.com/wiki/Logos_Research_Facility_mission_list): zone Marshes, given by Specialist Hax
- **TR MMO DB** [Slay On Target](https://web.archive.org/web/20080413105143/http://tr.mmodb.com:80/quests/slay-on-target-337.php): level: 35; starts at npc: Specialist Hax; starts in zone: Logos Research Facility; objectives: Destroy Bane Lab Equipment, Gather DNA Specimen for AFS Analysis; description: TIP: Slay the three Bosses in the Lab and then destroyt the equipment. You can then get the sample from a shelf in the lab.; reward: 5700; series: A Taste Of Their Own Medicine
- **TR MMO DB quest series** [A Taste Of Their Own Medicine](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/): A Taste Of Their Own Medicine (level 33, reward 5550) → Freedom's Just Another Word (level 35, reward 5700) → Leggo My Logos (level 35, reward 7600) → Slay On Target (level 35, reward 5700) (series order as listed on the page; reward unit not stated)
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1365.html](https://web.archive.org/web/20071209022816/http://planettr.com:80/forums/archive/index.php/t-1365.html): “* Logos Research: Necromites ambush should only spawn once now. / * Logos Research – Slay on Target: DNA sample usables are now mission activated, so you can’t steal extra DNA. / * Logos Research – Added mini boss into logos analyzer room.”
- **PlayTR EU** [eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issue …](https://web.archive.org/web/20090105235435/http://eu.playtr.com/en/news_article/deployment_93_patchnotes_and_known_issues_5th_june_2008_test_server): “le: Refusing to help the AWOLs completes the Other Side of the Coin mission, and fails the Stray Cats mission. This should be the opposite. / Mission: Slay on Target (Logos Research Facility): Slay on Target mission may not be able to be completed. / Some boss mobs are not dropping loot when defeated.”

- **Chain** (explicit prerequisite links): A Taste of Their Own Medicine chain (5 missions), step 2
  - Requires: A Taste of Their Own Medicine (id 546) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine), [TaRapedia](https://tabularasa.fandom.com/wiki/Slay_On_Target), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "A Taste Of Their Own Medicine", row 4 of 4 ([list](https://web.archive.org/web/20120731164150/http://tr.mmodb.com/quests/seriesname-a_taste_of_their_own_medicine/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** A Taste of Their Own Medicine (546) — source: the chain links above

### 864 — Bringing the Spirit Home


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Bringing_the_Spirit_Home); 33 — source: [TR MMO DB](https://web.archive.org/web/20081010175805/http://tr.mmodb.com/quests/bringing-the-spirit-home-311.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5700 credits; choose 1 of: Hazmat Armor Legs, Motor Assist Armor Legs, Reflective Armor Legs, Stealth Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,700 credits; lvl 39; lvl 38; lvl 39; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Bringing_the_Spirit_Home)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Bargas: Temple of Paludos, /loc -205, 230, 40 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Bargas)
  - location: Forean Machina (Forean Machina), level 9, zone Caves of Donn — Spawns near the Logos Shrine "Trap" — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Forean+Machina&id=37)
- **Client:** Put down 4 Forean Machina in the eastern marshes, then bring the remains of their spirit to an urn on a Forean altar. Return to Elder Bargas once you have completed your task.
  - objectives (client): Place the Forean spirit icons at one of the the altars.; Euthanize 4 Forean Machina and collect their Spirit Icons
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Elder Bargas
  - reward: 75000 XP; 5700 credits; choose 1 of: Hazmat Armor Legs, Motor Assist Armor Legs, Reflective Armor Legs, Stealth Armor Legs
- **TaRapedia** [Bringing the Spirit Home](https://tabularasa.fandom.com/wiki/Bringing_the_Spirit_Home) [stub]: giver: Elder Bargas; at: Temple of Paludos; zone: Marshes; level: 36; XP: 75,000; credits: 5,700; items: lvl 39; lvl 38; lvl 39; lvl 35; reward from: Elder Bargas
  - overview: Put down 4 Forean Machina in the eastern marshes, then bring the remains of their spirit to an urn on a Forean altar. Return to Elder Bargas once you have completed your task.
  - objectives: • Euthanize 4 Forean Machina and collect their Spirit Icons • Place the Forean spirit icons at one of the the altars.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Elder Bargas
- **TR MMO DB** [Bringing the Spirit Home](https://web.archive.org/web/20081010175805/http://tr.mmodb.com/quests/bringing-the-spirit-home-311.php): level: 33; starts at npc: Elder Bargas; starts in zone: Marshes
- **Where (locations / coordinates):**
  - Forean Machina (Forean Machina), level 9, zone Caves of Donn — Spawns near the Logos Shrine "Trap" — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Forean+Machina&id=37)
- **NPCs (TaRapedia NPC pages):**
  - [Elder Bargas](https://tabularasa.fandom.com/wiki/Elder_Bargas): Temple of Paludos, /loc -205, 230, 40 (named as giver/receiver)
- **Ellatha boss NPC** [Forean Machina](https://www.ellatha.com/tr/bossnpcview.asp?key=Forean+Machina&id=37): Forean Machina; level 9; zone Caves of Donn; Spawns near the Logos Shrine "Trap"; needed for: Too Quiet (boss named in client text)

### 881 — Reaping the Harvest


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Bio Armor Vest, Stealth Armor Boots, Hazmat Armor Legs, Reflective Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: A couple of them are located at (52, 72) and (50, 7). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest)
- **Client:** Gather gas samples from 5 of the Bane gas harvesters in the swamp. Return these samples to Researcher Vetter at Falcon Hold.
  - objectives (client): Collect Samples from the Bane Gas Harvesters
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Researcher Vetter
  - reward: 65000 XP; 5400 credits; choose 1 of: Bio Armor Vest, Stealth Armor Boots, Hazmat Armor Legs, Reflective Armor Gloves
- **TaRapedia** [Reaping the Harvest](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest): giver: Researcher Vetter; at: Falcon Hold; zone: Marshes; requires: none; follow-up: Beware: Buried Gas Lines; Hassling the Harvesters; XP: 65,000; credits: 5,400; reward from: Researcher Vetter
  - overview: Gather gas samples from 5 of the Bane gas harvesters in the swamp. Return the samples to Researcher Vetter in Falcon Hold.
  - objectives: Collect samples from Gas Harvesters: • Gas Harvesting Samples: 0/5
  - walkthrough: A couple of them are located at (52, 72) and (50, 7). You can finish the mission just going between the two.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Researcher Vetter
- **TR MMO DB NPC** [Researcher Vetter](https://web.archive.org/web/20081205004339/http://tr.mmodb.com/bestiary/researcher-vetter-726.php): Quest NPC, location Falcon Hold, zone Marshes (starts this quest)
- **Where (locations / coordinates):**
  - A couple of them are located at (52, 72) and (50, 7). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest)
- **NPCs (TaRapedia NPC pages):**
  - [Researcher Vetter](https://tabularasa.fandom.com/wiki/Researcher_Vetter): no location on page (named as giver/receiver)

- **Chain** (explicit prerequisite links): Reaping the Harvest chain (4 missions), step 1
  - Leads to: Hassling the Harvesters (id 890) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters), [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Beware: Buried Gas Lines (id 913) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines), [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: A Butcher, a Baker, and a Caretaker (id 968) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 888 — Tread Lightly


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Class VII Fragmentation Grenade, Class VII EMP Bomb, Class VII Cryogenic Grenade, Class VII Standard Med Pack — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65000 XP; Pathogex Motor Assist Armor Helmet v7 or; Pathogex Hazmat Armor Helmet v3 or; Pathogex Reflective Armor Helmet v3 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Tread_Lightly)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Duvall: Marshes, /loc -593, 216, -438 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Duvall)
- **Client:** Escort Retread Otto through the woods to the Retread Camp in the southeast. Report in to Retread Vincent once you arrive.
  - objectives (client): Escort Retread Otto to the Retread Camp
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Duvall → Retread Vincent
  - reward: 65000 XP; 5400 credits; choose 1 of: Class VII Fragmentation Grenade ×6, Class VII EMP Bomb ×2, Class VII Cryogenic Grenade ×6, Class VII Standard Med Pack ×8
- **TaRapedia** [Tread Lightly](https://tabularasa.fandom.com/wiki/Tread_Lightly) [stub]: giver: Retread Duvall; at: The Marsh; zone: Marshes; XP: 65000; items: Pathogex Motor Assist Armor Helmet v7 or; Pathogex Hazmat Armor Helmet v3 or; Pathogex Reflective Armor Helmet v3; reward from: Retread Vincent
  - overview: Overview Escort Retread Otto through the woods to the Retread camp in the southeast. Report in to Retread Vincent once you arrive.
  - objectives: • Objective 1 • Description
  - walkthrough: Walkthrough Head towards the Retread City, and speak to the man outside the armour and weapons building.
- **TR MMO DB NPC** [Retread Duvall](https://web.archive.org/web/20081201171215/http://tr.mmodb.com/bestiary/retread-duvall-739.php): Quest NPC, location Stalker Woods, zone Marshes (starts this quest)
- **NPCs (TaRapedia NPC pages):**
  - [Retread Duvall](https://tabularasa.fandom.com/wiki/Retread_Duvall): Marshes, /loc -593, 216, -438 (named as giver/receiver)
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (named as giver/receiver)

### 889 — AWOL Andy


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/AWOL_Andy); 33 — source: [TR MMO DB](https://web.archive.org/web/20080829152127/http://tr.mmodb.com/quests/awol-andy-133.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 130000 XP; 7200 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 130,000 XP; 7,200 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/AWOL_Andy)
  - reward: 7200 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20080829152127/http://tr.mmodb.com/quests/awol-andy-133.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC MP Price: Marshes, /loc -289, 218, 110 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/MP_Price)
- **Client:** Check the last known locations for Lieutenant Bruncke. Once he's located, escort him back to MP Price in Paludos.
  - objectives (client): Locate Private Bruncke; Locate Lieutenant Bruncke and escort him back to MP Price; Locate Lieutenant Bruncke and escort him back to Paludos; Search a Last Known Location for Lieutenant Bruncke; Search a Last Known Location for Lieutenant Bruncke; Search a Last Known Location for Lieutenant Bruncke; Search a Last Known Location for Lieutenant Bruncke
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver MP Price
  - reward: 130000 XP; 7200 credits; choose 1 of: Bio Armor Legs, Graviton Armor Legs, Mech Armor Helmet, Stealth Armor Helmet
- **TaRapedia** [AWOL Andy](https://tabularasa.fandom.com/wiki/AWOL_Andy): giver: MP Price; at: Paludos; zone: Marshes; level: 37; requires: none; follow-up: none; XP: 130,000; credits: 7,200; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: MP Price
  - overview: Check the last known locations for Lieutenant Bruncke. Once he's located, escort him back to MP Price in Paludos. Time remaining: 30:00
  - objectives: • Locate Lieutenant Bruncke and escort him back to Paludos • Search a Last Known Location for Lieutenant Bruncke • Search a Last Known Location for Lieutenant Bruncke • Search a Last Known Location for Lieutenant Bruncke • Search a Last Known Location for Lieutenant Bruncke
  - walkthrough: You need to walk over the top of each mission waypoint indicator until you reach the last one, whereupon you will have to escort Bruncke back to Price's location in Paludos.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by MP Price
- **TR MMO DB** [AWOL Andy](https://web.archive.org/web/20080829152127/http://tr.mmodb.com/quests/awol-andy-133.php): level: 33; starts at npc: MP Price; starts in zone: Marshes; objectives: Locate Lt. Bruncke and Escort Him back to Paludos; reward: 7200
- **NPCs (TaRapedia NPC pages):**
  - [MP Price](https://tabularasa.fandom.com/wiki/MP_Price): Marshes, /loc -289, 218, 110 (named as giver/receiver)

### 890 — Hassling the Harvesters


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Destroy 5 Bane Gas Harvesters, then check in with Retread Kline in Paludos.
  - objectives (client): Destroy the Bane Gas Harvesters
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Retread Kline
  - reward: 65000 XP; 5400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest
  - prerequisites: Requires completed mission 881
- **TaRapedia** [Hassling the Harvesters](https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters): giver: Retread Kline; at: Paludos; zone: Marshes; level: 37; requires: Reaping the Harvest; follow-up: none; XP: 65,000; credits: 5,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Retread Kline
  - overview: Destroy 5 Bane Gas Harvesters, then check in with Retread Kline in Paludos.
  - objectives: • Destroy the Bane Gas Harvesters
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Retread Kline

- **Chain** (explicit prerequisite links): Reaping the Harvest chain (4 missions), step 2
  - Requires: Reaping the Harvest (id 881) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters), [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 891 — Never Surrender


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 130000 XP; 7000 credits; choose 1 of: Class VII Standard Med Pack, Class VII Advanced Med Pack, Class VII Incendiary Grenade, Class VII EMP Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 130,000 XP; 7,000 credits; Class VII Standard Med Pack X 3; Class VII Advanced Med Pack X 2; Class VII Incendiary Grenade X 3; Class VII EMP Grenade X 3 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Never_Surrender)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Hold back the Bane attack on Tola's Village
  - objectives (client): Hold back the Bane attack on P'reo Das.
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Forean Warrior Tola → Forean Elder Kirii
  - reward: 130000 XP; 7000 credits; choose 1 of: Class VII Standard Med Pack ×3, Class VII Advanced Med Pack ×2, Class VII Incendiary Grenade ×3, Class VII EMP Grenade ×3
- **TaRapedia** [Never Surrender](https://tabularasa.fandom.com/wiki/Never_Surrender): giver: Forean Warrior Tola; at: P'reo Das; zone: Marshes; requires: None; follow-up: Cleaning House; XP: 130,000; credits: 7,000; items: Class VII Standard Med Pack X 3; Class VII Advanced Med Pack X 2; Class VII Incendiary Grenade X 3; Class VII EMP Grenade X 3; reward from: Forean Elder Kirii
  - overview: Hold back the Attack on Tola's Village. (No Time limit)
  - objectives: • Hold Back the Attack on P'reo Das.
  - walkthrough: Pretty straight-forward. Proceed north and repel the Bane invasion.
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Forean Warrior Tola

- **Chain** (explicit prerequisite links): Never Surrender chain (2 missions), step 1
  - Leads to: Cleaning House (id 894) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cleaning_House), [TaRapedia](https://tabularasa.fandom.com/wiki/Never_Surrender), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 892 — Picking Up the Pieces


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 60000 XP; 5250 credits; choose 1 of: Flame Thrower, Cryogenic Staff, Cryogenic Net Gun, Virulent Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 60,000 XP; 5,250 credits; Vextronics Flame Thrower; Vextronics Cryogenic Staff; Vextronics Cryogenic Net Gun; Vextronics Virulent Leech Gun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Picking_up_the_Pieces)
  - drops / mission items: Relics can be found on Forean corpses — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Picking_up_the_Pieces)
  - location: zone Marshes only
- **Client:** Retreive Forean relics from the village ruins.
  - objectives (client): Retrieve Forean Relics From the Fallen
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Forean Shaman Rakla
  - reward: 60000 XP; 5250 credits; choose 1 of: Flame Thrower, Cryogenic Staff, Cryogenic Net Gun, Virulent Leech Gun
- **TaRapedia** [Picking up the Pieces](https://tabularasa.fandom.com/wiki/Picking_up_the_Pieces): giver: Forean Shaman Rakla; at: P'reo Das; zone: Marshes; requires: None; follow-up: None; XP: 60,000; credits: 5,250; items: Vextronics Flame Thrower; Vextronics Cryogenic Staff; Vextronics Cryogenic Net Gun; Vextronics Virulent Leech Gun; reward from: Forean Shaman Rakla
  - overview: Retrieve the Forean Relics from the village ruins.
  - objectives: • Retrieve Relics from Foreans: 0/5
  - walkthrough: Relics can be found on Forean corpses. There are at least 4 bodies in P'reo Das village, and at least 2 in Ra'a Das.
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Forean Shaman Rakla
- **TR MMO DB NPC** [Forean Shaman Rakla](https://web.archive.org/web/20081121082806/http://tr.mmodb.com/bestiary/forean-shaman-rakla-742.php): Quest NPC, location P'reao Das, zone Marshes (starts this quest)
- **Drops / item sources:**
  - Relics can be found on Forean corpses — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Picking_up_the_Pieces)

### 893 — Needle in a Bane Camp


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB](https://web.archive.org/web/20080413105138/http://tr.mmodb.com:80/quests/needle-in-a-bane-camp-330.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 30000 XP; 3600 credits; choose 1 of: Class VII EMP Bomb, Class VII Power Charger, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 3,600 credits; 3x ClassVII EMP Bomb; 3x Class VII Power Charger; 6x Class VII Frag. Grenade; 6x Class VII Cryo. Grenade — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Needle_in_a_Bane_Camp)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Rescue the hidden Forean village from Bane attack.
  - objectives (client): Make contact with Forean encampment in Village Ruins.
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes))
  - reward: 30000 XP; 3600 credits; choose 1 of: Class VII EMP Bomb ×3, Class VII Power Charger ×3, Class VII Fragmentation Grenade ×6
  - prerequisites: Requires completed mission 1076
- **TaRapedia** [Needle in a Bane Camp](https://tabularasa.fandom.com/wiki/Needle_in_a_Bane_Camp) [stub]: giver: Headquarters; at: P'reo Das; zone: Marshes; requires: The Lost Ones; follow-up: None; credits: 3,600; items: 3x ClassVII EMP Bomb; 3x Class VII Power Charger; 6x Class VII Frag. Grenade; 6x Class VII Cryo. Grenade
  - overview: Save the Foreans.
  - objectives: • Make contact with Forean encampment in Village Ruins.
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Radio Mission
- **TR MMO DB** [Needle in a Bane Camp](https://web.archive.org/web/20080413105138/http://tr.mmodb.com:80/quests/needle-in-a-bane-camp-330.php): level: 35; starts in zone: Marshes; description: NOTE: This quest is given by a radio transmission, but we have no further details at this time. Please submit some.

- **Chain** (explicit prerequisite links): The Lost Ones chain (3 missions), step 2
  - Requires: The Lost Ones (id 1076) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Needle_in_a_Bane_Camp), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **Requires an instance mission:** The Lost Ones (1076) — source: the chain links above

### 894 — Cleaning House


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB](https://web.archive.org/web/20081202022904/http://tr.mmodb.com/quests/cleaning-house-334.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 130000 XP; 7000 credits; choose 1 of: Sonic Torqueshell Rifle, Laser Pistol, Electric Chaingun, Sonic Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 130,000 XP; 7,000 credits; Vextronics Sonic Torqueshell Rifle; Chitech Laser Pistol; Vextronics Electric Chaingun; Vextronics Sonic RPG Launcher — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cleaning_House)
  - reward: 7000 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081202022904/http://tr.mmodb.com/quests/cleaning-house-334.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Destroy Bane pumps which are poisoning the area and rescue captive Foreans.
  - objectives (client): Destroy Pumping Apparatus; Rescue Foreans Captured at Ra'a Das; Rescue Foreans Captured at E'bae Das; Rescue Forean Captured By the Bane; Destroy Pump 1; Destroy Pump 2; Destroy Pump 3; Destroy Pump 4; Escort Foreans Out of P'reo Das; Rescue Foreans Captured at He'kal Das … (+3 more)
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Forean Elder Kirii → Forean Warrior Tola
  - reward: 130000 XP; 7000 credits; choose 1 of: Sonic Torqueshell Rifle, Laser Pistol, Electric Chaingun, Sonic Cannon
  - prerequisites: Requires completed mission 891
- **TaRapedia** [Cleaning House](https://tabularasa.fandom.com/wiki/Cleaning_House): giver: Forean Elder Kirii; at: P'reo Das; zone: Marshes; requires: Never Surrender; follow-up: None; XP: 130,000; credits: 7,000; items: Vextronics Sonic Torqueshell Rifle; Chitech Laser Pistol; Vextronics Electric Chaingun; Vextronics Sonic RPG Launcher; reward from: Forean Warrior Tola
  - overview: Rescue the Prisoners and Destroy the pumps that are feeding the poisons into the ground and return to Forean Warrior Tola.
  - objectives: • Rescue Forean Captured By the Bane: 0/2 • Rescue Foreans Captured at He'kal Das • [Optional] Locate Additional Survivors • Destroy Pump 1 • Destroy Pump 2 • Destroy Pump 3 • Destroy Pump 4 • Escort Foreans Out of P'reo Das
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Forean Elder Kirii
- **TR MMO DB** [Cleaning House](https://web.archive.org/web/20081202022904/http://tr.mmodb.com/quests/cleaning-house-334.php): level: 35; starts at npc: Forean Elder Kirii; starts in zone: Marshes; objectives: Rescue Forean Captured by the Bane (0/4), Destroy Pump 1, Destroy Pump 2, Destroy Pump 3, Destroy Pump 4, Speak to Forean Warrior Tola; reward: 7000
  - related mob Forean Warrior Tola (not in the archived bestiary pages)

- **Chain** (explicit prerequisite links): Never Surrender chain (2 missions), step 2
  - Requires: Never Surrender (id 891) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Cleaning_House), [TaRapedia](https://tabularasa.fandom.com/wiki/Never_Surrender), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 913 — Beware: Buried Gas Lines [instance]


- **Instance:** Bane Supply Depot — client objective/log text: "Use the detonators to blow up the 8 Bane pipeline supply joints in the pipeline fields of the eastern Marshes to …"
- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 140000 XP; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest, Motor Assist Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 140,000 XP; none credits; lvl 33; plus one of:; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: zone Marshes only
- **Client:** Use the detonators to blow up the 8 Bane pipeline supply joints in the pipeline fields of the eastern Marshes to disrupt gas delivery to the Bane Supply Depot. Report back to Lieutenant Brazos on completion of your objective.
  - objectives (client): Destroy the first pipeline supply joint; Destroy the second pipeline supply joint; Destroy the third pipeline supply joint; Destroy the fourth pipeline supply joint
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Lieutenant Brazos
  - reward: 140000 XP; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest, Motor Assist Armor Boots
  - prerequisites: Requires completed mission 881
- **TaRapedia** [Beware: Buried Gas Lines](https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines): giver: Lieutenant Brazos; at: Paludos; zone: Marshes; level: 37; requires: Reaping the Harvest; follow-up: none; XP: 140,000; credits: none; items: lvl 33; plus one of:; lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lieutenant Brazos
  - overview: Use the detonators to blow up the 8 Bane pipeline supply joints in the pipeline fields of the eastern Marshes to disrupt gas delivery to the Bane Supply Depot. Report back to Lieutenant Brazos on completion of your objective.
  - objectives: • Destroy the first pipeline supply joint • Destroy the second pipeline supply joint • Destroy the third pipeline supply joint • Destroy the fourth pipeline supply joint
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Lieutenant Brazos

- **Chain** (explicit prerequisite links): Reaping the Harvest chain (4 missions), step 2
  - Requires: Reaping the Harvest (id 881) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines), [TaRapedia](https://tabularasa.fandom.com/wiki/Reaping_the_Harvest), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 914 — Highway Robbery


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery); 33 — source: [TR MMO DB](https://web.archive.org/web/20081202022239/http://tr.mmodb.com/quests/highway-robbery-314.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Adrenaline Booster, Class VII Incendiary Grenade, Class VII EMP Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 3,700 credits; Class VII Advanced Med Pack (2); Class VII Adrenaline Booster (2); Class VII Incendiary Grenade (4); Class VII EMP Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery)
  - reward: 3700 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081202022239/http://tr.mmodb.com/quests/highway-robbery-314.php)
  - reward: “McDonald for 7600 credits and choice of Graviton Helmet or Bio/Mech/Stealth Gloves.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
  - location: Fight your way into the Bane CP at 166,232,57. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **Client:** Steal medical supplies from the med tents in Paludos or Falcon's Hold and return them to Retread Vincent-- or report his request to the commanding officer in the Falcon Hold. No matter your choice, return to Retread Vincent when you've completed your objective.
  - objectives (client): Option: Steal Medical Supplies from the AFS hospitals; Option: Rat out the Retreads; Report back to Retread Vincent
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack ×2, Class VII Adrenaline Booster ×2, Class VII Incendiary Grenade ×4, Class VII EMP Grenade ×4
- **TaRapedia** [Highway Robbery](https://tabularasa.fandom.com/wiki/Highway_Robbery) [stub]: giver: Retread Vincent; at: Retread City; zone: Marshes; level: 36; requires: none; follow-up: Production Destruction; XP: 35,000; credits: 3,700; items: Class VII Advanced Med Pack (2); Class VII Adrenaline Booster (2); Class VII Incendiary Grenade (4); Class VII EMP Grenade (4); reward from: Commander Figgins
  - overview: Steal medical supplies from the med tents in Paludos or Falcon's Hold and return them to Retread Vincent -- or report his request to the commanding officer in the AFS camp in Paludos. No matter your choice, return to Retread Vincent when you've completed your objective.
  - objectives: • Option: Steal Medical Supplies from the AFS hospitals • Option: Rat out the Retreads • Report back to Retread Vincent
  - walkthrough: If player steals medical supplies from medical crates in Paludos and Falcon Hold, the Bane Teleporter in Retread City is accessible to the character through entire game. Therefore, this option is very important. Other choice will make player to fight through the front door to complete missions inside Bane Refueling Station. If you choose to steal the supplies, you don't have to go to both bases to get them. They spawn in a very short period of time so you can grab them from just one base faster than going to both.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Highway Robbery . Gather 2 cores, then return to Sgt. McDonald for 7600 credits and choice of Graviton Helmet or Bio/Mech/Stealth Gloves. Data Disaster : Information came in to Sgt. McDonald that a nearby AFS outpost was overrun, and the Bane may have taken vital data on AFS operations. That data must be retrieved, and then purged from the enemy computers. Fight your way into …”
- **TR MMO DB** [Highway Robbery](https://web.archive.org/web/20081202022239/http://tr.mmodb.com/quests/highway-robbery-314.php): level: 33; starts at npc: Retread Vincent; starts in zone: Marshes; objectives: Steal Medical Supplies from the AFS Hospital or Rat out the Retreads, Return to Retread Vincent; reward: 3700
  - related mob Commander Figgins (not in the archived bestiary pages)
- **Where (locations / coordinates):**
  - Fight your way into the Bane CP at 166,232,57. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)
  - [Commander Figgins](https://tabularasa.fandom.com/wiki/Commander_Figgins): /loc -186, 219, 628 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Highway Robbery chain (3 missions), step 1
  - Leads to: Production Destruction (id 916) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery), [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Production Destruction (id 1033) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery), [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 916 — Production Destruction


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Legs, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 150,000 XP; 7,600 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
- **Client:** Get into the Bane Strider Production Facility by any means and shut down production by destroying 5 production kiosks and eliminating the two Bane overseers - Vesh and Koorva. Report back to Retread Vincent at the Retread Camp when you've finished.
  - objectives (client): Destroy the Production Matrix in the Strider Bay; Eliminate Overseer Vesh; Eliminate Factory Overseer Koorva
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Legs, Stealth Armor Vest
  - prerequisites: Requires completed mission 914; Highway Robbery's choice: the supplies stolen
- **TaRapedia** [Production Destruction](https://tabularasa.fandom.com/wiki/Production_Destruction) [stub]: giver: Lieutenant Holloway; at: Retread City; zone: Marshes; level: 35; requires: Highway Robbery; follow-up: none; XP: 150,000; credits: 7,600; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lieutenant Holloway
  - overview: Objectives: • Destroy the Production Matrix in the Strider Bay • Eliminate Overseer Vesh • Eliminate Factory Overseer Koorva
  - objectives: • Destroy the Production Matrix in the Strider Bay • Eliminate Overseer Vesh • Eliminate Factory Overseer Koorva
  - walkthrough: It's no longer a teleporter into the base this mission has been completely changed. You now have a Mech squadmate who is rather useful, but due to the difficulty of the mobs and the journey to get to the production facility this mission will take a long time. I advise being a few levels over the mission to complete it or otherwise it is an exercise in frustration. • The rewards have also changed - they are now modification recipes for level 35 armour.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)
  - [Lieutenant Holloway](https://tabularasa.fandom.com/wiki/Lieutenant_Holloway): /loc -285, 216, -753 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Highway Robbery chain (3 missions), step 2
  - Requires: Highway Robbery (id 914) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery), [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 917 — Divided We Fall


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Divided_We_Fall); 33 — source: [TR MMO DB](https://web.archive.org/web/20081202023153/http://tr.mmodb.com/quests/divided-we-fall-131.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Hazmat Armor Boots, Motor Assist Armor Boots, Reflective Armor Boots, Stealth Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 34; lvl 33; lvl 34; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Divided_We_Fall)
  - reward: 5400 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081202023153/http://tr.mmodb.com/quests/divided-we-fall-131.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
- **Client:** Locate Ranger Alia, Shaman Siva, Warrior Veska, and Ranger Helka among the Forean refugee trains. Speak with them to arrange a meeting in Fort Defiance, then report back to Elder Toida in Paludos.
  - objectives (client): Locate Ranger Alia; Locate Shaman Siva; Locate Warrior Veska; Locate Ranger Helka
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Elder Toida
  - reward: 65000 XP; 5400 credits; choose 1 of: Hazmat Armor Boots, Motor Assist Armor Boots, Reflective Armor Boots, Stealth Armor Legs
- **TaRapedia** [Divided We Fall](https://tabularasa.fandom.com/wiki/Divided_We_Fall) [stub]: giver: Elder Toida; at: Paludos; zone: Marshes; level: 35; requires: none; follow-up: none; XP: 65,000; credits: 5,400; items: lvl 34; lvl 33; lvl 34; lvl 35; reward from: Elder Toida
  - overview: Locate Ranger Alia, Shaman Siva, Warrior Veska, and Ranger Helka among the Forean refugee trains. Speak with them to arrange a meeting at Fort Defiance, the report back to Elder Toida in Paludos.
  - objectives: • Locate Ranger Alia • Locate Shaman Siva • Locate Warrior Veska • Locate Ranger Helka
  - walkthrough: • Ranger Alia is patrolling the road between Plateau Point and Falcon Hold. • Shaman Siva can be found in the south west of Paludos City. • Warrior Veska can be found in the ruins between Paludos and The Eastern Mud Pit. • Ranger Helka's corpse can be found southwest of Paludos City, close to Far West Watch.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Elder Toida
- **TR MMO DB** [Divided We Fall](https://web.archive.org/web/20081202023153/http://tr.mmodb.com/quests/divided-we-fall-131.php): level: 33; starts at npc: Elder Toida; starts in zone: Marshes; objectives: Locate Ranger Alia, Locate Shaman Siva, Locate Warrior Veska, Locate Ranger Helka, Return to Elder Toida; reward: 5400
  - related mob Ranger Alia (not in the archived bestiary pages)
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (named as giver/receiver)
  - [Ranger Alia](https://tabularasa.fandom.com/wiki/Ranger_Alia): Marshes (named in client text)
  - [Shaman Siva](https://tabularasa.fandom.com/wiki/Shaman_Siva): Marshes, /loc -291, 218, 25 (named in client text)
  - [Warrior Veska](https://tabularasa.fandom.com/wiki/Warrior_Veska): Marshes, /loc -38, 219, 49 (named in client text)
  - [Ranger Helka](https://tabularasa.fandom.com/wiki/Ranger_Helka): Marshes, /loc -564, 217, -53 (named in client text)

### 919 — Beinvenidos del Voro


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5750 credits; choose 1 of: Bio Armor Helmet, Stealth Armor Gloves, Graviton Armor Legs, Motor Assist Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,750 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Beinvenidos_del_Voro)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sergeant Fitz: /loc -211.1, 27.9, 230.2 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Fitz)
- **Client:** Gather Bane datablades from the Stalker production facility command consoles, then return them to Sergeant Fitz in Paludos.
  - objectives (client): Collect the Datablades from the Stalker Production Command Consoles
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes))
  - reward: 75000 XP; 5750 credits; choose 1 of: Bio Armor Helmet, Stealth Armor Gloves, Graviton Armor Legs, Motor Assist Armor Legs
- **TaRapedia** [Beinvenidos del Voro](https://tabularasa.fandom.com/wiki/Beinvenidos_del_Voro) [stub]: giver: Radio; at: Bane Supply Depot; zone: Marshes; requires: none; follow-up: none; XP: 75,000; credits: 5,750; reward from: Sergeant Fitz
  - overview: Retrieve the Bane Datablades from the Stalker Production facility inside the General Bane Supply depot(Not the Instance).
  - objectives: • Collect the DataBlades from the Stalker Production Command Consoles.
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Sergeant Fitz
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Fitz](https://tabularasa.fandom.com/wiki/Sergeant_Fitz): /loc -211.1, 27.9, 230.2 (named as giver/receiver)

### 920 — Head East, Young Man


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Helmet, Hazmat Armor Legs, Motor Assist Armor Legs, Reflective Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; lvl 35; lvl 34; lvl 38; lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Head_East%2C_Young_Man)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Commander Figgins: /loc -186, 219, 628 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Commander_Figgins)
- **Client:** Report to Lieutenant Shaw at the eastern guard station and defend against the inbound Bane attack force.
  - objectives (client): Report to Lieutenant Shaw at the Eastern Lookout; Defend the Lookout Point against the Bane Attack; Speak to Ranger Meeko
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Commander Figgins → Ranger Meeko
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Helmet, Hazmat Armor Legs, Motor Assist Armor Legs, Reflective Armor Legs
- **TaRapedia** [Head East, Young Man](https://tabularasa.fandom.com/wiki/Head_East%2C_Young_Man): giver: Commander Figgins; at: Falcon Hold; zone: Marshes; requires: none; follow-up: none; XP: 70,000; credits: 5,550; items: lvl 35; lvl 34; lvl 38; lvl 34; reward from: Ranger Meeko
  - overview: Report to Lieutenant Shaw at the eastern guard station and defend against the inbound Bane attack force.
  - objectives: • Report to Lieutenant Shaw at the Eastern Lookout. • Defend the Lookout Point against the Bane Attack • Speak to Ranger Meeko.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Commander Figgins
- **NPCs (TaRapedia NPC pages):**
  - [Commander Figgins](https://tabularasa.fandom.com/wiki/Commander_Figgins): /loc -186, 219, 628 (named as giver/receiver)

### 925 — The Regulator: Part I


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Power Charger, Class VII Fragmentation Grenade, Class VII Cryogenic Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 3,700 credits; Class VII Advanced Med Pack (2); Class VII Power Charger (2); Class VII Fragmentation Grenade (4); Class VII Cryogenic Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_1)
  - reward: 3700 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread McCormick: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_McCormick)
- **Client:** Report to Retread Wilson outside Academy Point. He'll provide scrap metal for the Nanoviral Regulator as well as instructions for further actions.
  - objectives (client): Get Scrap Metal from Retread Wilson at the Logos Control Station
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread McCormick → Retread Wilson
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack ×2, Class VII Power Charger ×2, Class VII Fragmentation Grenade ×4, Class VII Cryogenic Grenade ×4
- **TaRapedia** [The Regulator: Part 1](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_1) (base-title title match): giver: Retread McCormick; at: Retread City; zone: Marshes; requires: none; follow-up: The Regulator: Part 2; XP: 35,000; credits: 3,700; items: Class VII Advanced Med Pack (2); Class VII Power Charger (2); Class VII Fragmentation Grenade (4); Class VII Cryogenic Grenade (4); reward from: Retread Wilson
  - overview: Report to Retread Wilson outside Academy Point. He'll provide the scrap metal for the Nanoviral Regulator as well as instructions for further actions.
  - objectives: • Get the scrap metal from Retread Wilson at the Logos Control Station
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread McCormick
- **TR MMO DB quest series** [The Regulator](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/): The Regulator: Part 2 (level 35, reward 0) → The Regulator: Part 3 (level 35, reward 1800) → The Regulator: Part 4 (level 35, reward 1800) → The Regulator: Part I (level 35, reward 3700) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Retread McCormick](https://tabularasa.fandom.com/wiki/Retread_McCormick): /loc -268, 216, -812 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Regulator: Part I chain (4 missions), step 1
  - Leads to: The Regulator: Part II (id 1028) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_1), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Regulator", row 4 of 4 ([list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 959 — Tastes Like Grubber


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Armor Module: Resist Electric [4], Armor Module: Resist Fire [4], Armor Module: Resist Ice [4], Armor Module: Resist Virulent [4] — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; Modification: Electric Resistance lvl 30; Modification: Fire Resistance lvl 30; Modification: Ice Resistance lvl 30; Modification: Virulent Resistance lvl 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Tastes_Like_Grubber)
  - reward: 5400 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731161656/http://tr.mmodb.com/quests/seriesname-tastes_like_grubber/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Research Chemist Horsh: /loc -168, 219, 593 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsh)
- **Client:** Bring 10 Swamp Grubber meat samples back to Research Chemist Horsch in Falcon's Hold. Grubbers can be found throughout the western marshes.
  - objectives (client): Gather meat from the Swamp Grubbers
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Research Chemist Horsh
  - reward: 65000 XP; 5400 credits; choose 1 of: Armor Module: Resist Electric [4], Armor Module: Resist Fire [4], Armor Module: Resist Ice [4], Armor Module: Resist Virulent [4]
- **TaRapedia** [Tastes Like Grubber](https://tabularasa.fandom.com/wiki/Tastes_Like_Grubber): giver: Research Chemist Horsh; at: Falcon Hold; zone: Marshes; requires: none; follow-up: Fixing the Food Chain; XP: 65,000; credits: 5,400; items: Modification: Electric Resistance lvl 30; Modification: Fire Resistance lvl 30; Modification: Ice Resistance lvl 30; Modification: Virulent Resistance lvl 30; reward from: Research Chemist Horsh
  - overview: Bring 10 Swamp Grubber meat samples back to Research Chemist Horsch in Falcon Hold. Grubbers can be found throughout the western marshes.
  - objectives: • Gather meat from the Swamp Grubbers.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Research Chemist Horsch
- **TR MMO DB quest series** [Tastes Like Grubber](https://web.archive.org/web/20120731161656/http://tr.mmodb.com/quests/seriesname-tastes_like_grubber/): Fixing the Food Chain (level 33, reward 3600) → Tastes Like Grubber (level 33, reward 5400) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Research Chemist Horsh](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsh): /loc -168, 219, 593 (named as giver/receiver)
  - [Research Chemist Horsch](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsch): /loc -268, 216, -812 (named in client text)

- **Chain** (explicit prerequisite links): Tastes Like Grubber chain (2 missions), step 1
  - Leads to: Fixing the Food Chain (id 961) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain), [TaRapedia](https://tabularasa.fandom.com/wiki/Tastes_Like_Grubber), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Tastes Like Grubber", row 2 of 2 ([list](https://web.archive.org/web/20120731161656/http://tr.mmodb.com/quests/seriesname-tastes_like_grubber/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 961 — Fixing the Food Chain


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain); 33 — source: [TR MMO DB](https://web.archive.org/web/20081202022840/http://tr.mmodb.com/quests/fixing-the-food-chain-308.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 32500 XP; 3600 credits; choose 1 of: Class VII EMP Bomb, Class VII Adrenaline Booster, Class VII Fragmentation Grenade, Class VII Cryogenic Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 32,500 XP; 3,600 credits; Class VII EMP Bomb (2); Class VII Adrenaline Booster (2); Class VII Fragmentation Grenade (4); Class VII Cryogenic Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain)
  - reward: 3600 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20081202022840/http://tr.mmodb.com/quests/fixing-the-food-chain-308.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Research Chemist Horsh: /loc -168, 219, 593 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsh)
- **Client:** Distribute the 10 bags of Mox feed among the troughs located in the northern Marshes. Report back to Research Chemist Horsch in Falcon Hold on completion.
  - objectives (client): Deliver modified feed to the Mox Feeders
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Research Chemist Horsh
  - reward: 32500 XP; 3600 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Adrenaline Booster ×2, Class VII Fragmentation Grenade ×4, Class VII Cryogenic Grenade ×4
  - prerequisites: Requires completed mission 959
- **TaRapedia** [Fixing the Food Chain](https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain): giver: Research Chemist Horsh; at: Falcon Hold; zone: Marshes; level: 37; requires: Tastes Like Grubber; follow-up: none; XP: 32,500; credits: 3,600; items: Class VII EMP Bomb (2); Class VII Adrenaline Booster (2); Class VII Fragmentation Grenade (4); Class VII Cryogenic Grenade (4); reward from: Research Chemist Horsh
  - overview: Distribute the 10 bags of Mox feed among the troughs located in the northern Marshes. Report back to Research Chemist Horsh in Falcon Hold upon completion.
  - objectives: • Deliver modified feed to the Mox Feeders • Feeders filled 0 / 10
  - walkthrough: The Mox feeders are spread out around Falcon Hold. You don't need to go to all of them, you can simply stay around the area southeast of Falcon Hold and fill those feeders there, since they can be filled more than once. It's handy to have Marshes Targets of Opportunity and the What's that Gas? missions while doing this since the area is crawling with Mox, a couple of Maws and some Swamp Grubbers as well.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Research Chemist Horsch
- **TR MMO DB** [Fixing the Food Chain](https://web.archive.org/web/20081202022840/http://tr.mmodb.com/quests/fixing-the-food-chain-308.php): level: 33; starts at npc: Research Chemist Horsch; starts in zone: Marshes; objectives: Deliver the modified feeders to Mox Feeders, Return to Research Chemist Horsch; description: TIP: Mox Feeders are near Falcon Hold.; reward: 3600; series: Tastes Like Grubber
  - related mob Mox Feeder (not in the archived bestiary pages)
- **TR MMO DB quest series** [Tastes Like Grubber](https://web.archive.org/web/20120731161656/http://tr.mmodb.com/quests/seriesname-tastes_like_grubber/): Fixing the Food Chain (level 33, reward 3600) → Tastes Like Grubber (level 33, reward 5400) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Research Chemist Horsh](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsh): /loc -168, 219, 593 (named as giver/receiver)
  - [Research Chemist Horsch](https://tabularasa.fandom.com/wiki/Research_Chemist_Horsch): /loc -268, 216, -812 (named in client text)

- **Chain** (explicit prerequisite links): Tastes Like Grubber chain (2 missions), step 2
  - Requires: Tastes Like Grubber (id 959) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain), [TaRapedia](https://tabularasa.fandom.com/wiki/Tastes_Like_Grubber), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "Tastes Like Grubber", row 1 of 2 ([list](https://web.archive.org/web/20120731161656/http://tr.mmodb.com/quests/seriesname-tastes_like_grubber/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 968 — A Butcher, a Baker, and a Caretaker


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker); 33 — source: [TR MMO DB](https://web.archive.org/web/20081205004934/http://tr.mmodb.com/quests/a-butcher,-a-baker,-and-a-caretaker-312.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 160000 XP; 7800 credits; choose 1 of: Electric Blade, Area Healing Disc, Incendiary Staff, Shotgun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 160,000 XP; 7,800 credits; Vitalius Electric Blade lvl 36; Phoenix Conical Healing Disc lvl 40; ChiTech Shotgun lvl 37; Vextronics Incendiary Staff lvl 40 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Ranger Taavik: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Ranger_Taavik)
  - location: Caretaker Fik (Caretaker): /loc 459 , 11, zone Marshes — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Caretaker+Fik&id=84)
  - location: Commander Barzak (Thrax): /loc 490 , 413, zone Marshes — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Commander+Barzak&id=82)
- **Client:** Locate Caretaker Fik, Commander Sto, and Commander Barzak and assassinate them. Report back to Ranger Taavik in Paludos.
  - objectives (client): Assassinate Caretaker Fik; Assassinate Commander Sto; Assassinate Commander Barzak
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Ranger Taavik
  - reward: 160000 XP; 7800 credits; choose 1 of: Electric Blade, Area Healing Disc, Incendiary Staff, Shotgun
  - prerequisites: Requires completed mission 881
- **TaRapedia** [A Butcher, a Baker, and a Caretaker](https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker): giver: Ranger Taavik; at: Paludos; zone: Marshes; level: 36; requires: Reaping the Harvest; follow-up: none; XP: 160,000; credits: 7,800; items: Vitalius Electric Blade lvl 36; Phoenix Conical Healing Disc lvl 40; ChiTech Shotgun lvl 37; Vextronics Incendiary Staff lvl 40; reward from: Ranger Taavik
  - overview: Locate Caretaker Fik, Commander Sto, and Comander Barzak and assassinate them. Report back to Ranger Taavik in Paludos.
  - objectives: • Assassinate Caretaker Fik • Assassinate Commander Sto • Assassinate Commander Barzak
- **TaRapedia mission list** [Paludos mission list](https://tabularasa.fandom.com/wiki/Paludos_mission_list): zone Marshes, area Paludos, given by Ranger Taavik
- **TR MMO DB** [A Butcher, A Baker, and a Caretaker](https://web.archive.org/web/20081205004934/http://tr.mmodb.com/quests/a-butcher,-a-baker,-and-a-caretaker-312.php): level: 33; starts at npc: Ranger Taavik; starts in zone: Marshes
- **Where (locations / coordinates):**
  - Caretaker Fik (Caretaker): /loc 459 , 11, zone Marshes — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Caretaker+Fik&id=84)
  - Commander Barzak (Thrax): /loc 490 , 413, zone Marshes — source: [Ellatha boss NPC](https://www.ellatha.com/tr/bossnpcview.asp?key=Commander+Barzak&id=82)
- **NPCs (TaRapedia NPC pages):**
  - [Ranger Taavik](https://tabularasa.fandom.com/wiki/Ranger_Taavik): /loc -268, 216, -812 (named as giver/receiver)
- **Ellatha boss NPC** [Caretaker Fik](https://www.ellatha.com/tr/bossnpcview.asp?key=Caretaker+Fik&id=84): Caretaker; /loc 459 , 11; zone Marshes; needed for: Marshes Targets of Opportunity (boss named in client text)
- **Ellatha boss NPC** [Commander Barzak](https://www.ellatha.com/tr/bossnpcview.asp?key=Commander+Barzak&id=82): Thrax; /loc 490 , 413; zone Marshes; needed for: Marshes Targets of Opportunity (boss named in client text)
- **Ellatha boss NPC** [Commander Sto](https://www.ellatha.com/tr/bossnpcview.asp?key=Commander+Sto): Thrax; /loc 362 , -168; zone Marshes; needed for: Marshes Targets of Opportunity (boss named in client text)
- **Ellatha boss NPC** [Commander Sto](https://www.ellatha.com/tr/bossnpcview.asp?key=Commander+Sto&id=85): Thrax; /loc 362 , -168; zone Marshes; needed for: Marshes Targets of Opportunity (boss named in client text)

- **Chain** (explicit prerequisite links): Reaping the Harvest chain (4 missions), step 2
  - Requires: Reaping the Harvest (id 881) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1023 — Rest and Recreation


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Weapon Module: Crit Hit Bonus [4], Armor Module: Body Bonus [4], Armor Module: Mind Bonus [4], Armor Module: Spirit Bonus [4] — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; Modification: Crit Hit Increase lvl 30; Modification: Body Bonus lvl 30; Modification: Mind Bonus lvl 30; Modification: Spirit Bonus lvl 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Rest_and_Recreation)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sergeant Elway: /loc -187, 219, 620 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Elway)
- **Client:** Escort the replacement squad safely to Lieutenant Belfast east of Falcon Hold. He'll have instructions on your next troop drop. When all troops have been safely rotated, report back to Sergeant Elway in Falcon Hold.
  - objectives (client): Escort the first group to Lieutenant Belfast; Escort the troops to Lieutenant Delaio's lookout.; Escort the troops to Lieutenant Urqhart's lookout.; Escort the final troop back to Falcon Hold. — Return to Sergeant Elway.; Report in to Lieutenant Belfast; Report in to Lieutenant Delaio; Report in to Lieutenant Urqhart
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant Elway
  - reward: 70000 XP; 5550 credits; choose 1 of: Weapon Module: Crit Hit Bonus [4], Armor Module: Body Bonus [4], Armor Module: Mind Bonus [4], Armor Module: Spirit Bonus [4]
- **TaRapedia** [Rest and Recreation](https://tabularasa.fandom.com/wiki/Rest_and_Recreation): giver: Sergeant Elway; at: Falcon Hold; zone: Marshes; requires: none; follow-up: none; XP: 70,000; credits: 5,550; items: Modification: Crit Hit Increase lvl 30; Modification: Body Bonus lvl 30; Modification: Mind Bonus lvl 30; Modification: Spirit Bonus lvl 30; reward from: Sergeant Elway
  - overview: Escort the replacement squad to Lieutenant Belfast east of Falcon Hold. He'll have instructions on your next troop drop. When all troops have been rotated, report back to Sergeant Elway in Falcon Hold.
  - objectives: • Escort the first group to Lieutenant Belfast • Report in to Lieutenant Belfast • Escort the troops to Lieutenant Deliao's lookout • Report in to Lieutenant Deliao • Escort the troops to Lietenant Urqhart's lookout • Report in to Lietenant Urqhart • Escort the final troop back to Falcon Hold.
  - walkthrough: You have only 10 minutes for delivery each group.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Sergeant Elway
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Elway](https://tabularasa.fandom.com/wiki/Sergeant_Elway): /loc -187, 219, 620 (named as giver/receiver)

### 1028 — The Regulator: Part II


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35500 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Armor Charger, Class VII Concussion Grenade, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,500 XP; 3,700 credits; Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2)
  - reward: 0 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread McCormick: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_McCormick)
- **Client:** Locate the Bane Traitor outside the Bane Supply Depot. Speak to him to get a Bane nanoviral sample.
  - objectives (client): Get the Bane Nanovirus Tracker from the Bane Traitor
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Retread Wilson → Conscript Kaav
  - reward: 35500 XP; 3700 credits; choose 1 of: Class VII Advanced Med Pack ×2, Class VII Armor Charger ×2, Class VII Concussion Grenade ×4, Class VII Fragmentation Grenade ×4
  - prerequisites: Requires completed mission 925
- **TaRapedia** [The Regulator: Part 2](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2) (base-title title match): giver: Retread Wilson; at: Marshes; zone: Marshes; level: 37; requires: The Regulator: Part 1; follow-up: The Regulator: Part 3; XP: 35,500; credits: 3,700; items: Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4); reward from: Conscript Kaav
  - overview: Locate the Bane Traitor outside the Bane Supply Depot. Speak to him to get a nanoviral sample.
  - objectives: • Get the Nanovirus Tracker from the Bane Traitor.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread McCormick
- **TR MMO DB quest series** [The Regulator](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/): The Regulator: Part 2 (level 35, reward 0) → The Regulator: Part 3 (level 35, reward 1800) → The Regulator: Part 4 (level 35, reward 1800) → The Regulator: Part I (level 35, reward 3700) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Retread McCormick](https://tabularasa.fandom.com/wiki/Retread_McCormick): /loc -268, 216, -812 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): The Regulator: Part I chain (4 missions), step 2
  - Requires: The Regulator: Part I (id 925) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_1), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Regulator: Part III (id 1029) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Regulator", row 1 of 4 ([list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1029 — The Regulator: Part III


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 32500 XP; 5550 credits; choose 1 of: Incendiary Injector Gun, Electric Chaingun, Pulse Pistol, Pulse Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 32,500 XP; 5,550 credits; AccuMax Incendiary Injector Gun lvl 37; Vitalius Electric Chaingun lvl 38; ChiTech Pulse Pistol lvl 35; AccuMax Pulse Cannon lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3)
  - reward: 1800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread McCormick: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_McCormick)
- **Client:** Take down a Bane Strider, retrieve its power core, then report back to Retread Wilson at the Logos Research Base Camp.
  - objectives (client): Collect a power core from a Bane Strider
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Conscript Kaav → Retread Wilson
  - reward: 32500 XP; 5550 credits; choose 1 of: Incendiary Injector Gun, Electric Chaingun, Pulse Pistol, Pulse Cannon
  - prerequisites: Requires completed mission 1028
- **TaRapedia** [The Regulator: Part 3](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3) (base-title title match): giver: Conscript Kaav; at: The Refueling Station; zone: Marshes; level: 37; requires: The Regulator: Part 2; follow-up: The Regulator: Part 4; XP: 32,500; credits: 5,550; items: AccuMax Incendiary Injector Gun lvl 37; Vitalius Electric Chaingun lvl 38; ChiTech Pulse Pistol lvl 35; AccuMax Pulse Cannon lvl 34; reward from: Retread Wilson
  - overview: Take down a Bane Strider, retrieve its power core, then report back to Retread Wilson at the Logos Research Base Camp.
  - objectives: • Collect a power core from a Bane Strider • Strider Power Core 0 / 1
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread McCormick
- **TR MMO DB quest series** [The Regulator](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/): The Regulator: Part 2 (level 35, reward 0) → The Regulator: Part 3 (level 35, reward 1800) → The Regulator: Part 4 (level 35, reward 1800) → The Regulator: Part I (level 35, reward 3700) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Retread McCormick](https://tabularasa.fandom.com/wiki/Retread_McCormick): /loc -268, 216, -812 (listed under this NPC on a TaRapedia mission list)

- **Chain** (explicit prerequisite links): The Regulator: Part I chain (4 missions), step 3
  - Requires: The Regulator: Part II (id 1028) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Regulator: Part IV (id 1030) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Regulator", row 2 of 4 ([list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1030 — The Regulator: Part IV


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest, Nanoviral Regulator — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4)
  - reward: 1800 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread McCormick: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_McCormick)
  - location: Located at 0.8 , 207.3 (/loc -2, 216, 29) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4)
- **Client:** Locate an AFS viral scrapper in the Paludos Ruins and deliver it to McCormick in the Retread Camp in the South.
  - objectives (client): Locate an AFS viral scrapper in the Paludos Ruins
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Wilson → Retread McCormick
  - reward: 70000 XP; 5550 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Mech Armor Helmet, Stealth Armor Vest, Nanoviral Regulator
  - prerequisites: Requires completed mission 1029
- **TaRapedia** [The Regulator: Part 4](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4) (base-title title match): giver: Retread Wilson; at: Marshes; zone: Marshes; requires: The Regulator: Part 3; follow-up: None; XP: 70,000; credits: 5,550; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Retread McCormick
  - overview: Locate an AFS Viral Scrapper in Paludos Ruins and deliver it to Retread McCormick at the Retread camp in the south.
  - objectives: • Locate a AFS Viral scrapper in the Paludos Ruins.
  - walkthrough: Located at 0.8 , 207.3 (/loc -2, 216, 29)
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread McCormick
- **TR MMO DB quest series** [The Regulator](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/): The Regulator: Part 2 (level 35, reward 0) → The Regulator: Part 3 (level 35, reward 1800) → The Regulator: Part 4 (level 35, reward 1800) → The Regulator: Part I (level 35, reward 3700) (series order as listed on the page; reward unit not stated)
- **Where (locations / coordinates):**
  - Located at 0.8 , 207.3 (/loc -2, 216, 29) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4)
- **NPCs (TaRapedia NPC pages):**
  - [Retread McCormick](https://tabularasa.fandom.com/wiki/Retread_McCormick): /loc -268, 216, -812 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Regulator: Part I chain (4 missions), step 4
  - Requires: The Regulator: Part III (id 1029) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Regulator", row 3 of 4 ([list](https://web.archive.org/web/20120731165522/http://tr.mmodb.com/quests/seriesname-the_regulator/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1033 — Production Destruction


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Legs, Stealth Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 150,000 XP; 7,600 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
- **Client:** Get into the Bane Strider Production Facility by any means and shut down production by destroying 5 production kiosks and eliminating the two Bane overseers - Vesh and Koorva. Report back to Lieutenant Holloway at the Retread Camp when you've finished.
  - objectives (client): Destroy the Production Matrix in the Strider Bay; Eliminate Overseer Vesh; Eliminate Factory Overseer Koorva
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Lieutenant Holloway
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Vest, Graviton Armor Vest, Mech Armor Legs, Stealth Armor Vest
  - prerequisites: Requires completed mission 914; Highway Robbery's choice: the AFS told
- **TaRapedia** [Production Destruction](https://tabularasa.fandom.com/wiki/Production_Destruction) [stub]: giver: Lieutenant Holloway; at: Retread City; zone: Marshes; level: 35; requires: Highway Robbery; follow-up: none; XP: 150,000; credits: 7,600; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lieutenant Holloway
  - overview: Objectives: • Destroy the Production Matrix in the Strider Bay • Eliminate Overseer Vesh • Eliminate Factory Overseer Koorva
  - objectives: • Destroy the Production Matrix in the Strider Bay • Eliminate Overseer Vesh • Eliminate Factory Overseer Koorva
  - walkthrough: It's no longer a teleporter into the base this mission has been completely changed. You now have a Mech squadmate who is rather useful, but due to the difficulty of the mobs and the journey to get to the production facility this mission will take a long time. I advise being a few levels over the mission to complete it or otherwise it is an exercise in frustration. • The rewards have also changed - they are now modification recipes for level 35 armour.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)
  - [Lieutenant Holloway](https://tabularasa.fandom.com/wiki/Lieutenant_Holloway): /loc -285, 216, -753 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Highway Robbery chain (3 missions), step 2
  - Requires: Highway Robbery (id 914) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Highway_Robbery), [TaRapedia](https://tabularasa.fandom.com/wiki/Production_Destruction), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1052 — Trapped!


- **Instance mentioned:** Logos Research Facility — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 38 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 38 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trapped%21); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 5800 credits; choose 1 of: Grenade Launcher, Virulent Leech Gun, Cryogenic Net Gun, Pulse Rifle — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 5,800 credits; AccuMax RPG Launcher lvl 35; ChiTech Virulent Leech Gun lvl 39; Teleract Cryogenic Net Gun lvl 36; Vitalius Pulse Rifle lvl 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trapped%21)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
  - location: Simply show up to coordinates near (15, -535), kill everything surrounding Samuel BEFORE you accept the mission, and babysit him for about 4 minutes (it took about 13 min as of patch 1.5). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trapped%21)
- **Client:** Locate Retread Samuel in the Bane Refueling Station area and help his group fight off the Bane attacks. Report back to Retread Vincent in the Retread Camp once his people have escaped.
  - objectives (client): Locate Samuel in the Bane Depot Exterior; Defend the Retreads from the incoming Bane attack
- **This PR:** revision `marshes_1_16_5`, level 38, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent
  - reward: 75000 XP; 5800 credits; choose 1 of: Grenade Launcher, Virulent Leech Gun, Cryogenic Net Gun, Pulse Rifle
- **TaRapedia** [Trapped!](https://tabularasa.fandom.com/wiki/Trapped%21) [stub]: giver: Retread Vincent; at: Retread City; zone: Marshes; level: 38; requires: none; follow-up: none; XP: 75,000; credits: 5,800; items: AccuMax RPG Launcher lvl 35; ChiTech Virulent Leech Gun lvl 39; Teleract Cryogenic Net Gun lvl 36; Vitalius Pulse Rifle lvl 36; reward from: Retread Vincent
  - overview: Rescue a Retread group, led by Retread Samuel, trapped in the Bane Refueling Station.
  - objectives: • Locate Samuel in the Depot Exterior. • Defend the Retreads from the incoming Bane attack
  - walkthrough: Despite the fact the mission log itself does not have a timer there is one in effect with this mission. Simply show up to coordinates near (15, -535), kill everything surrounding Samuel BEFORE you accept the mission, and babysit him for about 4 minutes (it took about 13 min as of patch 1.5). At such time the mission will complete. The mission will fail if Samuel dies, which is why it's important to kill all enemies first, or if you leave the immediate vicinity so it's very important to stay with him even if there are no enemies around. If you find there is no mission indicator when you …
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **Where (locations / coordinates):**
  - Simply show up to coordinates near (15, -535), kill everything surrounding Samuel BEFORE you accept the mission, and babysit him for about 4 minutes (it took about 13 min as of patch 1.5). — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Trapped%21)
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)

### 1076 — The Lost Ones [instance]


- **Instance:** P'reo Das — client objective/log text: "Plant a signal spike in P'reo Das to reestablish the communication link."
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 30000 XP; 3500 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Armor Charger, Class VII Cryogenic Grenade, Class VII Incendiary Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 30,000 XP; 3,500 credits; Class VII Advanced Med Pack (3); Class Vil Armor Charger (3); Class VII Cryogenic Grenade (6); Class VII Incendiary Grenade (6) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Lost_Ones)
  - reward: 3500 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Lieutenant Morrison: Marshes, /loc -205, 219, 600 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Lieutenant_Morrison)
- **Client:** Re-establish communication lines in Forean villages near P'reo Das.
  - objectives (client): Plant a signal spike in P'reo Das to reestablish the communication link.; Travel to P'reo Das to reestablish a communications link.
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Lieutenant Morrison
  - reward: 30000 XP; 3500 credits; choose 1 of: Class VII Advanced Med Pack ×3, Class VII Armor Charger ×3, Class VII Cryogenic Grenade ×6, Class VII Incendiary Grenade ×6
- **TaRapedia** [The Lost Ones](https://tabularasa.fandom.com/wiki/The_Lost_Ones): giver: Lieutenant Morrison; at: Falcon Hold; zone: Marshes; requires: None; follow-up: The Choices We Make; XP: 30,000; credits: 3,500; items: Class VII Advanced Med Pack (3); Class Vil Armor Charger (3); Class VII Cryogenic Grenade (6); Class VII Incendiary Grenade (6); reward from: Headquarters
  - overview: Re-establish communication lines in Forean villages near P'reo Das.
  - objectives: • Travel to P'reo Das to reestablish a communications link.
- **TaRapedia mission list** [Falcon Hold mission list](https://tabularasa.fandom.com/wiki/Falcon_Hold_mission_list): zone Marshes, area Falcon Hold, given by Lieutenant Morrison
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Lieutenant Morrison
- **TR MMO DB quest series** [The Lost Ones](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/): The Choices We Make (level 33, reward 7000) → The Lost Ones (level 33, reward 3500) (series order as listed on the page; reward unit not stated)
- **NPCs (TaRapedia NPC pages):**
  - [Lieutenant Morrison](https://tabularasa.fandom.com/wiki/Lieutenant_Morrison): Marshes, /loc -205, 219, 600 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Lost Ones chain (3 missions), step 1
  - Leads to: Needle in a Bane Camp (id 893) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Needle_in_a_Bane_Camp), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Choices We Make (id 1078) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Choices_We_Make), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Lost_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Lost Ones", row 2 of 2 ([list](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.

### 1077 — Dirty Secrets


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 1750 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 1750 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Dirty_Secrets)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
- **Client:** Proceed to Central Operations and retrieve the Logos element powering the toxin pumps.
  - objectives (client): Proceed to Central Operations and retrieve the Logos element powering the toxin …; Destroy Bane Equipment and Gather Toxin Sample; Return Toxin Sample to Elder Toida in Palados
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Forean Elder Qunle → Elder Toida
  - reward: 35000 XP; 1750 credits
- **TaRapedia** [Dirty Secrets](https://tabularasa.fandom.com/wiki/Dirty_Secrets) [bug noted]: giver: Forean Elder Qunle; at: P'reo Das; zone: Marshes; credits: 1750; reward from: Elder Toids
  - overview: Retrieve Disperse logos
  - objectives: • Proceed to Central Operations and retrieve the Logos element powering the toxin pumps. • Destroy Bane Equipment and Gather Toxin Sample • Return Toxin Sample to Elder Toida in Palados
  - walkthrough: So far this mission is still bugged if you miss killing Myrquar. (6/25/08) Resetting the map wil not give you the missions back that were failed earlier so obtaining the key is not possible. This is the work around (7/06/08) Leave instance (mission will fail) and let it reset. Return to instance and proceed to the hidden Fornean Village to get mission. Travel to the chamber where the force fields will now open for you. Beware of the transporter to the right - every few minutes Bane will drop in on you. The objective is to the left and is guarded by a boss. Retrieve logos and new mission will …
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Forean Elder Qunle
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (named as giver/receiver)

### 1078 — The Choices We Make


- **Instance mentioned:** P'reo Das — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 7000 credits; choose 1 of: Electric Torqueshell Rifle, Photonic Blade, Pulse Cannon, Cryogenic Leech Gun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 7,000 credits; Vextronics Electric Torqueshell Rifle; Vextronics Photonic Blade; Chitech Pulse Cannon; Vitalius Cryogenic Leech Gun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Choices_We_Make)
  - reward: 7000 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB quest series list](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Elder Toida: /loc -205, 230, 28 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Elder_Toida)
- **Client:** Terminate Operation Master Myrquar before he leaves on a transport then indentify and disable the Bane base power.
  - objectives (client): Intercept and Terminate Operation Master Myrquar; Enter Central Operations and Retrieve the Element Powering the Bane Outpost.; Destory Bane Equipment and Gather Toxin Sample; Return Toxin Sample to Elder Toida in Palados
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes))
  - reward: 65000 XP; 7000 credits; choose 1 of: Electric Torqueshell Rifle, Photonic Blade, Pulse Cannon, Cryogenic Leech Gun
  - prerequisites: Requires completed mission 1076
- **TaRapedia** [The Choices We Make](https://tabularasa.fandom.com/wiki/The_Choices_We_Make) [stub]: giver: Headquarters; at: P'reo Das; zone: Marshes; requires: The Lost Ones; follow-up: None; credits: 7,000; items: Vextronics Electric Torqueshell Rifle; Vextronics Photonic Blade; Chitech Pulse Cannon; Vitalius Cryogenic Leech Gun
  - overview: Terminate Operation Master Myrquar before he leaves on a transport, then identify and disable the Bane power supply.
  - objectives: • Intercept and Terminate Operation Master Myrquar. (00:25:00)
- **TaRapedia mission list** [P'reo Das mission list](https://tabularasa.fandom.com/wiki/P%27reo_Das_mission_list): zone Marshes, given by Radio Mission
- **TR MMO DB quest series** [The Lost Ones](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/): The Choices We Make (level 33, reward 7000) → The Lost Ones (level 33, reward 3500) (series order as listed on the page; reward unit not stated)
- **PlanetTR** [planettr.com:80/forums/archive/index.php/t-1365.html](https://web.archive.org/web/20071209022816/http://planettr.com:80/forums/archive/index.php/t-1365.html): “* P’reo Das / o Mission: The Choices We Make / o Issue: Mission objective tells the player to complete the mission by leaving the instance but doing that fails the mission.”
- **RGTR official site** [www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_1_4.html](https://web.archive.org/web/20081117172815/http://www.rgtr.com/news/patch_notes/patch_notes_and_known_issues_1_4.html): “P’reo Das / Mission: The Choices We Make / Issue: Mission objective tells the player to complete the mission by leaving the instance but doing that fails the mission.”
- **NPCs (TaRapedia NPC pages):**
  - [Elder Toida](https://tabularasa.fandom.com/wiki/Elder_Toida): /loc -205, 230, 28 (named as giver/receiver)

- **Chain** (explicit prerequisite links): The Lost Ones chain (3 missions), step 2
  - Requires: The Lost Ones (id 1076) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Choices_We_Make), [TaRapedia](https://tabularasa.fandom.com/wiki/The_Lost_Ones), this PR's World data (`mission_content_definition` / `mission_reward_*`)
- **TR MMO DB quest series:** "The Lost Ones", row 1 of 2 ([list](https://web.archive.org/web/20120731162321/http://tr.mmodb.com/quests/seriesname-the_lost_ones/)). The row order is an ESTIMATE of the play order. The list does not say which mission unlocks which.
- **Requires an instance mission:** The Lost Ones (1076) — source: the chain links above

### 1084 — Calling all Parts


- **Rebuild summary:**
  - level: 33 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 33 — source: [TR MMO DB](https://web.archive.org/web/20120731173115/http://tr.mmodb.com/quests/calling-all-parts-316.php)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Virulent Injector Gun, Sonic Cannon, Electric Rifle, Laser Chaingun — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; Chitech Virulent Injector Gun; Vextronics Sonic Cannon; Vextronics Electric Rifle; Teleract Laser Chaingun — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Calling_All_Parts)
  - reward: 5550 (the page gives no unit; probably credits — ESTIMATE) — source: [TR MMO DB](https://web.archive.org/web/20120731173115/http://tr.mmodb.com/quests/calling-all-parts-316.php)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
- **Client:** Head out to the battlefield and take out the appropriate Bane to find a stalker motherboard, a caretaker AI controller, and a machina brain implant. Report back to Retread Vincent in the Retread Settlement with the goods.
  - objectives (client): Scavenge a Motherboard from a Stalker; Scavenge an AI Controller from a Caretaker; Scavenge a Brain Implant from a Forean Machina
- **This PR:** revision `marshes_1_16_5`, level 33, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent
  - reward: 70000 XP; 5550 credits; choose 1 of: Virulent Injector Gun, Sonic Cannon, Electric Rifle, Laser Chaingun
  - prerequisites: Requires completed mission 1088
- **TaRapedia** [Calling All Parts](https://tabularasa.fandom.com/wiki/Calling_All_Parts) [stub]: giver: Retread Vincent; at: Retread City; zone: Marshes; requires: none; follow-up: none; XP: 70,000; credits: 5,550; items: Chitech Virulent Injector Gun; Vextronics Sonic Cannon; Vextronics Electric Rifle; Teleract Laser Chaingun; reward from: Retread Vincent
  - overview: Objectives:
  - objectives: 32px Dialogue: Briefing: Retread Vincent: • Debriefing: Retread Vincent: • Not bad, Not bad. These are in decent shape, a little gooey though. Next time you could at least wipe the guts off. Now lets build you a stalker controller.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **TR MMO DB** [Calling All Parts](https://web.archive.org/web/20120731173115/http://tr.mmodb.com/quests/calling-all-parts-316.php): level: 33; starts at npc: Retread Vincent; starts in zone: Marshes; reward: 5550
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Retread Arena: Beat Street chain (4 missions), step 4
  - Requires: Retread Arena: Legend or Fall (id 1088) — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1085 — Retread Arena: Beat Street


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb, Class VII Power Charger, Class VII EMP Grenade, Class VII Concussion Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 3,700 credits; Class VII EMP Bomb (2); Class VII Power Charger (2); Class VII EMP Grenade (4); Class VII Concussion Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Beat_Street)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Karl: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Karl)
- **Client:** Speak to Retread Foster to gain access to the arena. Defeat Retreads Withers and Ingram, then exit the arena and report back to Retread Karl.
  - objectives (client): Take out the Retread Lightweight Fighters in the Arena; Speak to Retread Foster to enter the Arena
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Karl
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Power Charger ×2, Class VII EMP Grenade ×4, Class VII Concussion Grenade ×4
- **TaRapedia** [Retread Arena: Beat Street](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Beat_Street): giver: Retread Karl; at: Retread City; zone: Marshes; follow-up: Retread Arena: Smackdown; XP: 35,000; credits: 3,700; items: Class VII EMP Bomb (2); Class VII Power Charger (2); Class VII EMP Grenade (4); Class VII Concussion Grenade (4); reward from: Retread Karl
  - objectives: Speak to Retread Foster to gain access to the arena. Defeat Retreads Withers and Ingram, then exit the arena and report back to Retread Karl.
  - walkthrough: Two Elite mobs per round. Adeptus 18:36, 13 December 2007 (UTC)
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Karl
- **NPCs (TaRapedia NPC pages):**
  - [Retread Karl](https://tabularasa.fandom.com/wiki/Retread_Karl): /loc -268, 216, -812 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Retread Arena: Beat Street chain (4 missions), step 1
  - Leads to: Retread Arena: Smackdown (id 1087) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Beat_Street), [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1087 — Retread Arena: Smackdown


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb, Class VII Adrenaline Booster, Class VII Concussion Grenade, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 3,700 credits; Class VII EMP Bomb (2); Class VII Adrenaline Booster (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Karl: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Karl)
- **Client:** Speak to Retread Foster, then enter the arena and fight the middleweight Retread champions. Report back to Retread Karl when you've won.
  - objectives (client): Take out the Retread Middleweight Fighters in the Arena; Speak to Retread Foster to enter the Arena
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Karl
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Adrenaline Booster ×2, Class VII Concussion Grenade ×4, Class VII Fragmentation Grenade ×4
  - prerequisites: Requires completed mission 1085
- **TaRapedia** [Retread Arena: Smackdown](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown): giver: Retread Karl; at: Retread City; zone: Marshes; requires: Retread Arena: Beat Street; follow-up: Retread Arena: Legend or Fall; XP: 35,000; credits: 3,700; items: Class VII EMP Bomb (2); Class VII Adrenaline Booster (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4); reward from: Retread Karl
  - overview: • Speak to Retread Foster then enter the arena, and fight the Retread middleweight champions. Report back to Retread Karl when you've won.
  - objectives: • Speak to Retread Foster to enter the Arena • Time remaining: 00:03:00 • Take out the Retread Middleweight Fighters in the Arena • Time remaining: 00:05:00 • Retread King Defeated 0 / 1 • Retread Baker Defeated 0 / 1
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Karl
- **NPCs (TaRapedia NPC pages):**
  - [Retread Karl](https://tabularasa.fandom.com/wiki/Retread_Karl): /loc -268, 216, -812 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Retread Arena: Beat Street chain (4 missions), step 2
  - Requires: Retread Arena: Beat Street (id 1085) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Beat_Street), [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Retread Arena: Legend or Fall (id 1088) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Legend_or_Fall), [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1088 — Retread Arena: Legend or Fall


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb, Class VII Armor Charger, Class VII Cryogenic Grenade, Class VII Incendiary Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 35,000 XP; 3,700 credits; Class VII EMP Bomb (2); Class VII Armor Charger (2); Class VII Cryogenic Grenade (2); Class VII Incendiary Grenade (2) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Legend_or_Fall)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Karl: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Karl)
- **Client:** Speak to Retread Foster to gain access to the arena. Defeat the Retread Champion - Toro... or don't.
  - objectives (client): Take out the Retread Heavyweight Fighter... or don't.; Speak to Retread Foster to enter the Arena; Take out the Retread Heavyweight Fighter... or don't.
- **This PR:** revision `marshes_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Karl
  - reward: 35000 XP; 3700 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Armor Charger ×2, Class VII Cryogenic Grenade ×2, Class VII Incendiary Grenade ×2
  - prerequisites: Requires completed mission 1087
- **TaRapedia** [Retread Arena: Legend or Fall](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Legend_or_Fall): giver: Retread Karl; at: Retread City; zone: Marshes; requires: Retread Arena: Smackdown; follow-up: None; XP: 35,000; credits: 3,700; items: Class VII EMP Bomb (2); Class VII Armor Charger (2); Class VII Cryogenic Grenade (2); Class VII Incendiary Grenade (2); reward from: Retread Karl
  - overview: Speak to Retread Foster to gain access to the arena. Defeat the Retread Champion - Toro... or don't.
  - objectives: • Speak to Retread Foster to enter the Arena • Take out the Retread Heavyweight Fighter... or don't.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Karl
- **NPCs (TaRapedia NPC pages):**
  - [Retread Karl](https://tabularasa.fandom.com/wiki/Retread_Karl): /loc -268, 216, -812 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Retread Arena: Beat Street chain (4 missions), step 3
  - Requires: Retread Arena: Smackdown (id 1087) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Legend_or_Fall), [TaRapedia](https://tabularasa.fandom.com/wiki/Retread_Arena%3A_Smackdown), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Calling all Parts (id 1084) — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1673 — It Lies in Ruins


- **Rebuild summary:**
  - level: 35 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/It_Lies_in_Ruins); 33 — source: [TR MMO DB quest list](https://web.archive.org/web/20080407025711/http://tr.mmodb.com:80/quests/startingzone-marshes/)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 75000 XP; 3800 credits; choose 1 of: Hazmat Armor Vest, Mech Armor Vest, Motor Assist Armor Vest, Reflective Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 75,000 XP; 3,800 credits; lvl 39; lvl 35; lvl 38; lvl 39 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/It_Lies_in_Ruins)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
- **Client:** Report in to Lieutenant Morrison at the Falcon Hold Outpost to learn more about the situation in the Marshes.
  - objectives (client): Report in to Lieutenant Morrison in Falcon Hold
- **This PR:** revision `plateau_1_16_5`, level 35, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent → Lieutenant Morrison
  - reward: 75000 XP; 3800 credits; choose 1 of: Hazmat Armor Vest, Mech Armor Vest, Motor Assist Armor Vest, Reflective Armor Vest
  - prerequisites: Requires completed mission 1751
- **TaRapedia** [It Lies in Ruins](https://tabularasa.fandom.com/wiki/It_Lies_in_Ruins): giver: Retread Vincent; at: Retread City; zone: Marshes; level: 35; requires: Retreads to Retreads; XP: 75,000; credits: 3,800; items: lvl 39; lvl 35; lvl 38; lvl 39; reward from: Lieutenant Morrison
  - overview: • Retread Vincent claims he is as puzzled as you about the Eloh rumors, but he's tipping you off that someone at Falcon Hold may be able to help.
  - objectives: • Report in to Lieutenant Morrison in Falcon Hold
  - walkthrough: For some bizarre reason, the game does not directly follow up on the cliffhanger announcement at the end of this mission. The story probably unfolds through other missions elsewhere.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **RGTR official site** [rgtr.com:80/news/patch_notes/patch_notes_and_known_issues_109.html](https://web.archive.org/web/20071011062613/http://rgtr.com:80/news/patch_notes/patch_notes_and_known_issues_109.html): “Mission: Dogtag Detail / Issue: AFS bodies with dogtags are nowhere to be found. / Mission: It Lies in Ruins / Issue: Mission does not complete / continue when you talk to Lt. Morrison. / Mission: Survivors”
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)
  - [Lieutenant Morrison](https://tabularasa.fandom.com/wiki/Lieutenant_Morrison): Marshes, /loc -205, 219, 600 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Giant Killer chain (11 missions), step 8
  - Requires: Retreads to Retreads (id 1751) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/It_Lies_in_Ruins), [TaRapedia](https://tabularasa.fandom.com/wiki/Retreads_to_Retreads), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1674 — To a Subterranean Chamber


- **Rebuild summary:**
  - level: 38 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 38 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 40000 XP; 3900 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Armor Charger, Class VII EMP Grenade, Class VII Concussion Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 40,000 XP; 3,900 credits; Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII EMP Grenade (4); Class VII Concussion Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Vincent: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Vincent)
- **Client:** Swim to the secret location in the lake near Retread City to locate and speak with the Eloh hidden nearby.
  - objectives (client): Locate and speak with the Eloh Guardian
- **This PR:** revision `marshes_1_16_5`, level 38, solo, category 10000051 (Battlefield (Marshes)), giver Retread Vincent → Jumna
  - reward: 40000 XP; 3900 credits; choose 1 of: Class VII Advanced Med Pack ×2, Class VII Armor Charger ×2, Class VII EMP Grenade ×4, Class VII Concussion Grenade ×4
  - prerequisites: Requires completed mission 550
- **TaRapedia** [To a Subterranean Chamber](https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber) [stub]: giver: Retread Vincent; at: Retread City; zone: Marshes; level: 38; requires: Leggo My Logos; XP: 40,000; credits: 3,900; items: Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII EMP Grenade (4); Class VII Concussion Grenade (4); reward from: Retread Vincent
  - overview: Locate and speak with the Eloh Guardian via teleporter by Retread City.
  - objectives: • Locate and speak with the Eloh Guardian
  - walkthrough: From Vincent, head to the lake that is right of the city. Within the horseshoe shape of three larger rocks, there is a crevice to stand in the water in the center. Doing this will transport you to the hidden chamber. From here, approach the Eloh hologram.
- **TaRapedia mission list** [Retread City mission list](https://tabularasa.fandom.com/wiki/Retread_City_mission_list): zone Marshes, area Retread City, given by Retread Vincent
- **NPCs (TaRapedia NPC pages):**
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (NPC page lists this mission)
  - [Jumna](https://tabularasa.fandom.com/wiki/Jumna): /loc -160, 111, -902 (named as giver/receiver)

- **Chain** (explicit prerequisite links): A Taste of Their Own Medicine chain (5 missions), step 4
  - Requires: Leggo My Logos (id 550) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1863 — Missing in Action


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Missing_in_Action)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Class VII Advanced Med Pack, Class VII Armor Charger, Class VII Cryogenic Grenade, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII Cryogenic Grenade (4); Class VII Fragmentation Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Missing_in_Action)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Jeska: Marshes, Stalker Woods, /loc -777, 270, -552 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Jeska)
- **Client:** Investigate Lt. Koffman's last known whereabouts and report back to Retread Jeska.
  - objectives (client): Locate Lt. Koffman to the east of High Point Retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Jeska
  - reward: 65000 XP; 5400 credits; choose 1 of: Class VII Advanced Med Pack ×2, Class VII Armor Charger ×2, Class VII Cryogenic Grenade ×4, Class VII Fragmentation Grenade ×4
- **TaRapedia** [Missing in Action](https://tabularasa.fandom.com/wiki/Missing_in_Action): giver: Retread Jeska; at: Stalker Woods; zone: Marshes; level: 36; requires: none; follow-up: none; XP: 65,000; credits: 5,400; items: Class VII Advanced Med Pack (2); Class VII Armor Charger (2); Class VII Cryogenic Grenade (4); Class VII Fragmentation Grenade (4); reward from: Retread Jeska
  - overview: Investigate Lt. Koffman's last known whereabouts and report back to Retread Jeska.
  - objectives: • Locate Lt. Koffman to the east of High Point Retreat.
- **NPCs (TaRapedia NPC pages):**
  - [Retread Jeska](https://tabularasa.fandom.com/wiki/Retread_Jeska): Marshes, Stalker Woods, /loc -777, 270, -552 (NPC page lists this mission)

### 1864 — Light the Fires


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Light_the_Fires)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 32500 XP; 3600 credits; choose 1 of: Class VII EMP Bomb, Class VII Power Charger, Class VII Concussion Grenade, Class VII Fragmentation Grenade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 32,500 XP; 3,600 credits; Class VII EMP bomb (2); Class VII Power Charger (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Light_the_Fires)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Dudley: Marshes, Stalker Woods, /loc -784, 243, --523 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Dudley)
- **Client:** Visit the Forean fire pits throughout marshes and ensure they are lit properly.
  - objectives (client): Light the fire NE of High Point Retreat.; Light the first fire west of Paludos.; Light the second fire west of Paludos.; Light the third fire west of Paludos.; Light the fire at High Point Retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Dudley
  - reward: 32500 XP; 3600 credits; choose 1 of: Class VII EMP Bomb ×2, Class VII Power Charger ×2, Class VII Concussion Grenade ×4, Class VII Fragmentation Grenade ×4
- **TaRapedia** [Light the Fires](https://tabularasa.fandom.com/wiki/Light_the_Fires): giver: Retread Dudley; at: Stalker Woods; zone: Marshes; level: 36; requires: none; follow-up: none; XP: 32,500; credits: 3,600; items: Class VII EMP bomb (2); Class VII Power Charger (2); Class VII Concussion Grenade (4); Class VII Fragmentation Grenade (4); reward from: Retread Dudley
  - overview: Visit the Forean fire pits throughout marshes and esnure they are lit properly.
  - objectives: • Light the fire NE of High Point Retreat. • Light the first fire west of Palados. • Light the second fire west of Palados. • Light the third fire west of Palados. • Light the fire at High Point Retreat.
  - walkthrough: This mission is pretty straight-forward and combinable with other missions that take into the western quadrant. Take them counterclockwise and you are on your way back.
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Dudley
- **NPCs (TaRapedia NPC pages):**
  - [Retread Dudley](https://tabularasa.fandom.com/wiki/Retread_Dudley): Marshes, Stalker Woods, /loc -784, 243, --523 (NPC page lists this mission)

### 1865 — Gun Control


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Hazmat Armor Gloves, Motor Assist Armor Gloves, Bio Armor Helmet, Reflective Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 34; lvl 33; lvl 35; lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Lou: Marshes, Stalker Woods, /loc -776, 241, -542 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Lou)
- **Client:** Head to the east and gather some materials off the Thrax Warrant Officers and Lightbenders.
  - objectives (client): Gather 10 Thrax Warrant Officer armor fragments.; Gather 5 Lightbender rifle power cores.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Lou
  - reward: 65000 XP; 5400 credits; choose 1 of: Hazmat Armor Gloves, Motor Assist Armor Gloves, Bio Armor Helmet, Reflective Armor Gloves
- **TaRapedia** [Gun Control](https://tabularasa.fandom.com/wiki/Gun_Control): giver: Retread Lou; at: Stalker Woods; zone: Marshes; requires: none; follow-up: Gun Control - Part II; XP: 65,000; credits: 5,400; items: lvl 34; lvl 33; lvl 35; lvl 34; reward from: Retread Lou
  - overview: Head to the east and gather some materials off the Thrax Warrant Officers and Lightbenders.
  - objectives: • Gather 10 Thrax Warrant Officer armor fragments. • Gather 5 Lightbender rifle power cores.
  - walkthrough: Found all the Thrax and Lightbenders required just down the hill. Karlman 06:12, 5 March 2008 (UTC)
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Lou
- **NPCs (TaRapedia NPC pages):**
  - [Retread Lou](https://tabularasa.fandom.com/wiki/Retread_Lou): Marshes, Stalker Woods, /loc -776, 241, -542 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Gun Control chain (5 missions), step 1
  - Leads to: Gun Control - Part II (id 1866) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1866 — Gun Control - Part II


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Mech Armor Helmet, Graviton Armor Legs, Stealth Armor Helmet, Bio Armor Legs — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Lou: Marshes, Stalker Woods, /loc -776, 241, -542 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Lou)
- **Client:** Head north and kill 10 mox and gather their electrical essence.
  - objectives (client): Gather the electrical discharge from 10 mox.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Lou
  - reward: 65000 XP; 5400 credits; choose 1 of: Mech Armor Helmet, Graviton Armor Legs, Stealth Armor Helmet, Bio Armor Legs
  - prerequisites: Requires completed mission 1865
- **TaRapedia** [Gun Control - Part II](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II): giver: Retread Lou; at: Stalker Woods; zone: Marshes; level: 36; requires: Gun Control; follow-up: Gun Control - Part III; XP: 65,000; credits: 5,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Retread Lou
  - overview: Head north and kill 10 mox and gather their electrical essence.
  - objectives: • Gather the electrical discharge from 10 mox. • Number of Mox Killed for Electrical Discharge 0 / 10
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Lou
- **NPCs (TaRapedia NPC pages):**
  - [Retread Lou](https://tabularasa.fandom.com/wiki/Retread_Lou): Marshes, Stalker Woods, /loc -776, 241, -542 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Gun Control chain (5 missions), step 2
  - Requires: Gun Control (id 1865) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Gun Control - Part III (id 1867) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1867 — Gun Control - Part III


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Lou: Marshes, Stalker Woods, /loc -776, 241, -542 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Lou)
- **Client:** Deliver the CGH 9000 Ultra Electric Chaingun to Shaman Masai.
  - objectives (client): Deliver CGH 9000 Ultra Electric Chaingun to Shaman Masai.; You've been sidetracked - speak with Retread Duvall.; Return to Shaman Masai without the gun.; HIDDEN - here so I can add the flag or just complete.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Lou → Shaman Masai
  - reward: 65000 XP; 5400 credits
  - prerequisites: Requires completed mission 1866
- **TaRapedia** [Gun Control - Part III](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III): giver: Retread Lou; at: Stalker Woods; zone: Marshes; level: 36; requires: Gun Control - Part II; follow-up: Gun Control - Part IV; XP: 65,000; credits: 5,400; reward from: Shaman Masai
  - overview: Deliver CGH 9000 Ultra Electric Chaingun to Shaman Masai
  - objectives: • Deliver CGH 9000 Ultra Electric Chaingun to Shaman Masai
  - walkthrough: If you take the road north and pass Retread Duvall, You are presented with another option to this mission:; Image:Gunpart3.jpg
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Lou
- **NPCs (TaRapedia NPC pages):**
  - [Retread Lou](https://tabularasa.fandom.com/wiki/Retread_Lou): Marshes, Stalker Woods, /loc -776, 241, -542 (NPC page lists this mission)
  - [Shaman Masai](https://tabularasa.fandom.com/wiki/Shaman_Masai): Marshes, Plateau Point, /loc -735, 217, 171 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Gun Control chain (5 missions), step 3
  - Requires: Gun Control - Part II (id 1866) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Gun Control - Part IV (id 1868) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Gun Control - Part IV (id 1904) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1868 — Gun Control - Part IV


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 32500 XP; 3600 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 32,500 XP; 3,600 credits; Teleract Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Shaman Masai: Marshes, Plateau Point, /loc -735, 217, 171 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Shaman_Masai)
- **Client:** Return to Retread Lou in High Point Retreat.
  - objectives (client): Return to Retread Lou at High Point Retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Shaman Masai → Retread Lou
  - reward: 32500 XP; 3600 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon
  - prerequisites: Requires completed mission 1867; Gun Control - Part III ended with flag 1867001 = 1
- **TaRapedia** [Gun Control - Part IV](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV): giver: Shaman Masai; at: Plateau Point; zone: Marshes; level: 36; requires: Gun Control - Part III; follow-up: none; XP: 32,500; credits: 3,600; items: Teleract Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34; reward from: Retread Lou
  - overview: Return to Retread Lou at High Point Retreat
  - objectives: • Return to Retread Lou at High Point Retreat
- **TaRapedia mission list** [Plateau Point mission list](https://tabularasa.fandom.com/wiki/Plateau_Point_mission_list): zone Marshes, area Plateau Point, given by Shaman Masai
- **NPCs (TaRapedia NPC pages):**
  - [Shaman Masai](https://tabularasa.fandom.com/wiki/Shaman_Masai): Marshes, Plateau Point, /loc -735, 217, 171 (NPC page lists this mission)
  - [Retread Lou](https://tabularasa.fandom.com/wiki/Retread_Lou): Marshes, Stalker Woods, /loc -776, 241, -542 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Gun Control chain (5 missions), step 4
  - Requires: Gun Control - Part III (id 1867) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1869 — Now Can You Hear Me?


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Area Healing Disc, Cryogenic Staff, Sonic Pistol, Cryogenic Blade — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; ChiTech Radial Healing Disc lvl 35; Audiodyne Cryogenic Staff lvl 35; Teleract Sonic Pistol lvl 35; ChiTech Cryogenic Blade lvl 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Now_Can_You_Hear_Me%3F)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Dudley: Marshes, Stalker Woods, /loc -784, 243, --523 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Dudley)
- **Client:** Place 3 communication signal boosters in key locations between High Point Retreat and Paludos to enhance the communication signal strength.
  - objectives (client): Place spike north of High Point Retreat.; Place spike north east of High Point Retreat.; Place spike east of High Point Retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Dudley
  - reward: 70000 XP; 5550 credits; choose 1 of: Area Healing Disc, Cryogenic Staff, Sonic Pistol, Cryogenic Blade
- **TaRapedia** [Now Can You Hear Me?](https://tabularasa.fandom.com/wiki/Now_Can_You_Hear_Me%3F): giver: Retread Dudley; at: Stalker Woods; zone: Marshes; requires: none; follow-up: none; XP: 70,000; credits: 5,550; items: ChiTech Radial Healing Disc lvl 35; Audiodyne Cryogenic Staff lvl 35; Teleract Sonic Pistol lvl 35; ChiTech Cryogenic Blade lvl 36; reward from: Retread Dudley
  - overview: • Place 3 communication signal boosters in key locations between High Point Retreat and Palados to enhance the communication signal strength.
  - objectives: • Place spike north of High Point Retreat. • Place spike north east of High Point Retreat. • Place spike east of High Point Retreat.
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Dudley
- **NPCs (TaRapedia NPC pages):**
  - [Retread Dudley](https://tabularasa.fandom.com/wiki/Retread_Dudley): Marshes, Stalker Woods, /loc -784, 243, --523 (NPC page lists this mission)

### 1870 — Data Disaster


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Reflective Armor Legs, Hazmat Armor Legs, Motor Assist Armor Legs, Mech Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; lvl 34; lvl 34; lvl 38; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)
  - reward: “Upstairs youll find the Bane CommLink terminal from which to take the data, and then its back to McDonald for his reward to you of 5550 credits and choice of Mech Vest, or Reflective/Hazmat/Motor Assist Legs.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: Fight your way into the Bane CP at 166,232,57. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - location: Need a Little Help Here : Archer Thoung is a Forean Ranger stationed at a tiny outpost at -127,220,-341. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **Client:** Recover AFS Troop Placement Data Acquired by the Bane. It is likely being stored in a data hub east of Paludos.
  - objectives (client): Recover AFS troop placement data acquired by the Bane.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant McDonald
  - reward: 70000 XP; 5550 credits; choose 1 of: Reflective Armor Legs, Hazmat Armor Legs, Motor Assist Armor Legs, Mech Armor Vest
  - prerequisites: Requires Under the Radar - Part II (1876 or 1877)
- **TaRapedia** [Data Disaster](https://tabularasa.fandom.com/wiki/Data_Disaster): giver: Sergeant McDonald; at: Central Guard Tower; zone: Marshes; level: 37; requires: Under the Radar - Part II; follow-up: none; XP: 70,000; credits: 5,550; items: lvl 34; lvl 34; lvl 38; lvl 35; reward from: Sergeant McDonald
  - overview: Recover AFS Troop Placement Data Acquired by the Bane. It is likely being stored in a data hub east of Palados.
  - objectives: • Recover AFS troop placement data acquired by the Bane.
  - walkthrough: Mission starts at a guard tower between Research Point and Bane Assertion Camp
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Data Disaster : Information came in to Sgt. McDonald that a nearby AFS outpost was overrun, and the Bane may have taken vital data on AFS operations. That data must be retrieved, and then purged from the enemy computers. Fight your way into the Bane CP at 166,232,57. Upstairs youll find the Bane CommLink terminal from which to take the data, and then its back to McDonald for …”
- **Where (locations / coordinates):**
  - Fight your way into the Bane CP at 166,232,57. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - Need a Little Help Here : Archer Thoung is a Forean Ranger stationed at a tiny outpost at -127,220,-341. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 3
  - Requires: Under the Radar - Part II (id 1876) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)
  - Requires: Under the Radar - Part II (id 1877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)

### 1871 — Stop and Smell the Maw Flowers


- **Instance mentioned:** Retread Caves — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Area Repair Tool, Sonic Torqueshell Rifle, Rifle, Sonic Disperser — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; ChiTech Area Repair Tool lvl 35; Vextronics Sonic Torqueshell Rifle lvl 33; Teleract Rifle lvl 36; Teleract Sonic Disperser lvl 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Stop_and_Smell_the_Maw_Flowers)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Retread Duvall: Marshes, /loc -593, 216, -438 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Retread_Duvall)
- **Client:** Head north and gather 5 Maw flowers.
  - objectives (client): Gather Maw Flowers north of High Point Retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes)), giver Retread Duvall
  - reward: 65000 XP; 5400 credits; choose 1 of: Area Repair Tool, Sonic Torqueshell Rifle, Rifle, Sonic Disperser
- **TaRapedia** [Stop and Smell the Maw Flowers](https://tabularasa.fandom.com/wiki/Stop_and_Smell_the_Maw_Flowers): giver: Retread Duvall; at: Stalker Woods; zone: Marshes; requires: none; follow-up: none; XP: 65,000; credits: 5,400; items: ChiTech Area Repair Tool lvl 35; Vextronics Sonic Torqueshell Rifle lvl 33; Teleract Rifle lvl 36; Teleract Sonic Disperser lvl 37; reward from: Retread Vincent
  - overview: •; Gather Maw Flowers north of High Point Retreat.
  - objectives: • Gather Maw Flowers north of High Point Retreat.
- **TaRapedia mission list** [Stalker Woods mission list](https://tabularasa.fandom.com/wiki/Stalker_Woods_mission_list): zone Marshes, area Stalker Woods, given by Retread Duvall
- **NPCs (TaRapedia NPC pages):**
  - [Retread Duvall](https://tabularasa.fandom.com/wiki/Retread_Duvall): Marshes, /loc -593, 216, -438 (named as giver/receiver)
  - [Retread Vincent](https://tabularasa.fandom.com/wiki/Retread_Vincent): /loc -268, 216, -812 (named as giver/receiver)

### 1873 — Need a Little Help Here


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Need_a_Little_Help_Here)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 70000 XP; 5550 credits; choose 1 of: Weapon Module: Crit Hit Bonus [4], Armor Module: Armor Absorption [4], Tool Module: Threat Reduction [4], Weapon Module: Armor Piercing [4] — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 70,000 XP; 5,550 credits; Modification: Threat Reduction lvl 30; Modification: Armor Piercing lvl 30; Modification: Crit Hit Increase lvl 30; Modification: Armor Absorption lvl 30 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Need_a_Little_Help_Here)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Archer Thoung: Marshes, /loc -126.4, 221, -343.4 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Archer_Thoung)
  - location: Need a Little Help Here : Archer Thoung is a Forean Ranger stationed at a tiny outpost at -127,220,-341. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - location: The large crates holding the Medical Booster Packs are found in a Bane outpost where Overseer Nelg lives at 170,224,55. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **Client:** Gather Marsh Sickness Medical Boost from the Bane and give them to soldiers stationed along the marsh.
  - objectives (client): Find supplies stolen by the Bane's northbound patrol.; Inject Pvt. Brady with the marsh sickness medical boost.; Inject Archer Zui with the marsh sickness medical boost.; Inject Warrior Dahn with the marsh sickness medical boost.; Inject Elder Nez with the marsh sickness medical boost.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Archer Thoung
  - reward: 70000 XP; 5550 credits; choose 1 of: Weapon Module: Crit Hit Bonus [4], Armor Module: Armor Absorption [4], Tool Module: Threat Reduction [4], Weapon Module: Armor Piercing [4]
- **TaRapedia** [Need a Little Help Here](https://tabularasa.fandom.com/wiki/Need_a_Little_Help_Here) [stub]: giver: Archer Thoung; at: Far West Watch; zone: Marshes; level: 37; requires: none; follow-up: none; XP: 70,000; credits: 5,550; items: Modification: Threat Reduction lvl 30; Modification: Armor Piercing lvl 30; Modification: Crit Hit Increase lvl 30; Modification: Armor Absorption lvl 30; reward from: Archer Thoung
  - overview: Text
  - objectives: • Find supplies stolen by the Bane's northbound patrol. • Inject Pvt. Brady with the marsh sickness medical boost. • Inject Elder Nez with the marsh sickness medical boost. • Inject Warrior Dahn with the marsh sickness medical boost. • Inject Archer Zui with the marsh sickness medical boost.
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Need a Little Help Here : Archer Thoung is a Forean Ranger stationed at a tiny outpost at -127,220,-341. The marsh sickness gets to everyone out here, and regular doses of a special medical concoction keeps the troops healthy for a short time. The latest delivery was ambushed by the Bane, and the medical supplies were left out to rot. You need to locate the supplies, retrieve …”
- **Where (locations / coordinates):**
  - Need a Little Help Here : Archer Thoung is a Forean Ranger stationed at a tiny outpost at -127,220,-341. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - The large crates holding the Medical Booster Packs are found in a Bane outpost where Overseer Nelg lives at 170,224,55. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **NPCs (TaRapedia NPC pages):**
  - [Archer Thoung](https://tabularasa.fandom.com/wiki/Archer_Thoung): Marshes, /loc -126.4, 221, -343.4 (NPC page lists this mission)

### 1874 — Mortar Madness


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 130000 XP; 7200 credits; choose 1 of: Motor Assist Armor Helmet, Stealth Armor Legs, Hazmat Armor Helmet, Reflective Armor Helmet — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 130,000 XP; 7,200 credits; lvl 33; lvl 35; lvl 34; lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Mortar_Madness)
  - reward: “When you have destroyed 3 mortars, hightail it back to McDonald and snatch your 7200 credits reward, and choice of Stealth Legs, or Motor Assist/Hazmat/Reflective Helmet.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sgt. McDonald: Marshes, /loc -124, 224, -307.6 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sgt._McDonald)
  - location: Mortar Madness : Sergeant McDonald at -125,224,-311 has a big problem with the big mortars out in the marsh. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **Client:** Take out mortars surrounding the Bane strongholds to the east of Far West Watch.
  - objectives (client): Destroy 3 Bane Mortars at Bane Bases East of Far West Watch
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant McDonald
  - reward: 130000 XP; 7200 credits; choose 1 of: Motor Assist Armor Helmet, Stealth Armor Legs, Hazmat Armor Helmet, Reflective Armor Helmet
- **TaRapedia** [Mortar Madness](https://tabularasa.fandom.com/wiki/Mortar_Madness) [bug noted]: giver: Sgt. McDonald; at: Far West Watch; zone: Marshes; requires: none; follow-up: none; XP: 130,000; credits: 7,200; items: lvl 33; lvl 35; lvl 34; lvl 34; reward from: Sgt. McDonald
  - overview: Take out the Mortars surrounding the Bane strongholds to the east of Far West Watch.
  - objectives: • Destroy 3 Bane Mortars at Bane Bases East of Far West Watch
  - walkthrough: Mission starts at an AFS sentry tower between Control Point: Research Point and Control Point: Bane Assertion Camp.
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Mortar Madness : Sergeant McDonald at -125,224,-311 has a big problem with the big mortars out in the marsh. The mortars are tearing apart the troops, and they need to be taken out. Head east and destroy 3 of the monsters. The mortars are found along the Bane fortification walls in the east, along a north to south line. You have to watch out for plenty of roaming Thrax, Kael …”
- **Where (locations / coordinates):**
  - Mortar Madness : Sergeant McDonald at -125,224,-311 has a big problem with the big mortars out in the marsh. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **NPCs (TaRapedia NPC pages):**
  - [Sgt. McDonald](https://tabularasa.fandom.com/wiki/Sgt._McDonald): Marshes, /loc -124, 224, -307.6 (NPC page lists this mission)

### 1875 — Under the Radar


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; Eclipse Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar)
  - reward: “She completes Under the Radar with 5400 credits and choice of Chaingun, Shotgun, Virulent Injector or Sonic Cannon.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - reward: “Under the Radar with 5400 credits and choice of Chaingun, Shotgun, Virulent Injector or Sonic Cannon.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - drops / mission items: Kiang is concerned about what she found on that black box — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - location: NPC Warrior Dikah: Marshes, Far West Watch, /loc -521, 222, -179 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Warrior_Dikah)
  - location: Just take box (-51, -365) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar)
  - location: He waits in a nearly destroyed Forean village known as the Far West Watch at -523,222,-176. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **Client:** Gather the black box from the downed AFS dropship east of Far West Watch.
  - objectives (client): Gather the black box from the downed dropship east of Far West Watch.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Warrior Dikah → Lt. Kiang
  - reward: 65000 XP; 5400 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon
- **TaRapedia** [Under the Radar](https://tabularasa.fandom.com/wiki/Under_the_Radar): giver: Warrior Dikah; at: Far West Watch; zone: Marshes; follow-up: Under the Radar - Part II; The Core of the Matter; XP: 65,000; credits: 5,400; items: Eclipse Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34; reward from: Lt. Kiang
  - overview: Gather the black box from the downed AFS dropship east of Far West Watch.
  - objectives: • Gather the black box from the downed dropship east of Far West Watch. • AFS Dropship Black Box 0 / 1
  - walkthrough: Just take box (-51, -365)
- **TaRapedia mission list** [Far West Watch mission list](https://tabularasa.fandom.com/wiki/Far_West_Watch_mission_list): zone Marshes, area Far West Watch, given by Warrior Dikah
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Under the Radar : Before heading down to this post, visit Warrior Dikah. He waits in a nearly destroyed Forean village known as the Far West Watch at -523,222,-176. An AFS dropship was supposed to be delivering supplies several days ago, but hasnt shown up. The latest radar data shows the ship may have gone down somewhere to the east. Locate the downed ship, recover the black …”
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “Under the Radar with 5400 credits and choice of Chaingun, Shotgun, Virulent Injector or Sonic Cannon. Under the Radar  Part II : Lt. Kiang is concerned about what she found on that black box. The Bane are massing in far larger numbers that was previously thought. The large base to the east needs to be infiltrated, and the crew in the downed dropship may have found a way. A …”
- **Where (locations / coordinates):**
  - Just take box (-51, -365) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar)
  - He waits in a nearly destroyed Forean village known as the Far West Watch at -523,222,-176. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - Look for the dropship and crate holding the black box at -51,216,-366. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - Beeline to the Lieutenant in a watch tower at -116,230,-319. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - The computer you seek is right outside the CP at 101,226,-289. — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
- **NPCs (TaRapedia NPC pages):**
  - [Warrior Dikah](https://tabularasa.fandom.com/wiki/Warrior_Dikah): Marshes, Far West Watch, /loc -521, 222, -179 (NPC page lists this mission)
  - [Lt. Kiang](https://tabularasa.fandom.com/wiki/Lt._Kiang): Marshes, /loc -115, 231, -319.5 (named as giver/receiver)
- **Drops / item sources:**
  - Kiang is concerned about what she found on that black box — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 1
  - Leads to: Under the Radar - Part II (id 1876) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Under the Radar - Part II (id 1877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: The Core of the Matter (id 1878) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Core_of_the_Matter), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1876 — Under the Radar - Part II


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 140000 XP; 7400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Stealth Armor Boots, Mech Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 140,000 XP; 7,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Lt. Kiang: Marshes, /loc -115, 231, -319.5 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Lt._Kiang)
- **Client:** Engage control point to the east and gather info on Bane troop movement.
  - objectives (client): Engage control point to the east and gather info on Bane troop movement.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Lt. Kiang
  - reward: 140000 XP; 7400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Stealth Armor Boots, Mech Armor Boots
  - prerequisites: Requires completed mission 1875
- **TaRapedia** [Under the Radar - Part II](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II) [stub, bug noted]: giver: Lt. Kiang; at: Far West Watch; zone: Marshes; level: 37; requires: Under the Radar; follow-up: This Calls For Destruction; XP: 140,000; credits: 7,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lt. Kiang
  - overview: Engage control point to the east and gather info on bane troop movement.
  - objectives: • Engage control point to the east and gather info on bane troop movement. • Bane Troop Data 0 / 1
- **NPCs (TaRapedia NPC pages):**
  - [Lt. Kiang](https://tabularasa.fandom.com/wiki/Lt._Kiang): Marshes, /loc -115, 231, -319.5 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 2
  - Requires: Under the Radar (id 1875) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Data Disaster (id 1870) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)
  - Leads to: This Calls For Destruction (id 1879) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)

### 1877 — Under the Radar - Part II


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 140000 XP; 7400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Stealth Armor Boots, Mech Armor Boots — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 140,000 XP; 7,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Lt. Kiang: Marshes, /loc -115, 231, -319.5 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Lt._Kiang)
- **Client:** Intercept Overseer Nelg and obtain data on the Bane troop movements in marshes.
  - objectives (client): Intercept Overseer Nelg and obtain data on the Bane troop movements in Marshes.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Lt. Kiang
  - reward: 140000 XP; 7400 credits; choose 1 of: Bio Armor Boots, Graviton Armor Boots, Stealth Armor Boots, Mech Armor Boots
  - prerequisites: Requires completed mission 1875
- **TaRapedia** [Under the Radar - Part II](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II) [stub, bug noted]: giver: Lt. Kiang; at: Far West Watch; zone: Marshes; level: 37; requires: Under the Radar; follow-up: This Calls For Destruction; XP: 140,000; credits: 7,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lt. Kiang
  - overview: Engage control point to the east and gather info on bane troop movement.
  - objectives: • Engage control point to the east and gather info on bane troop movement. • Bane Troop Data 0 / 1
- **NPCs (TaRapedia NPC pages):**
  - [Lt. Kiang](https://tabularasa.fandom.com/wiki/Lt._Kiang): Marshes, /loc -115, 231, -319.5 (NPC page lists this mission)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 2
  - Requires: Under the Radar (id 1875) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II), this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - Leads to: Data Disaster (id 1870) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Data_Disaster)
  - Leads to: This Calls For Destruction (id 1879) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)

### 1878 — The Core of the Matter


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Helmet, Mech Armor Gloves, Stealth Armor Gloves — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 150,000 XP; 7,600 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Core_of_the_Matter)
  - reward: “McDonald for 7600 credits and choice of Graviton Helmet or Bio/Mech/Stealth Gloves.” — source: [Ten Ton Hammer](https://www.tentonhammer.com/guides/guide-to-eastern-marshes)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Sgt. McDonald: Marshes, /loc -124, 224, -307.6 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sgt._McDonald)
- **Client:** Gather 2 Bane power cores from the crates in the eastern Bane base near the Bane Supply Depot.
  - objectives (client): Gather 2 Bane power cores from the crates in Bane base near Bane Supply Depot.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant McDonald
  - reward: 150000 XP; 7600 credits; choose 1 of: Bio Armor Gloves, Graviton Armor Helmet, Mech Armor Gloves, Stealth Armor Gloves
  - prerequisites: Requires completed mission 1875
- **TaRapedia** [The Core of the Matter](https://tabularasa.fandom.com/wiki/The_Core_of_the_Matter): giver: Sergeant McDonald; at: Central Guard Tower; zone: Marshes; requires: Under the Radar; follow-up: none; XP: 150,000; credits: 7,600; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Sergeant McDonald
  - overview: • Gather 2 Bane Power Cores from the Bane base near Bane Supply Depot.
  - objectives: • Gather 2 Bane Power Cores from the Bane base near Bane Supply Depot • Bane Power Core 0/2
  - walkthrough: Mission starts at a guard tower between Research Point and Bane Assertion Camp
- **Ten Ton Hammer** [guide-to-eastern-marshes](https://www.tentonhammer.com/guides/guide-to-eastern-marshes): “The Core of the Matter : Sergeant McDonald overheard you talking with Lt. Kiang earlier. Since youll be making a trip out to the Bane Base in the east, the Sergeant wants you to pick up some supplies. Find some boxes with new power cores in them, as the AFS can use them to power up weapons and machinery. The crates with the power cores are found all over the Bane Base in the …”
- **IGN TR boards** [vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?](https://web.archive.org/web/20080302101520/http://vnboards.ign.com:80/tabula_rasa_general_board/b22750/106459602/p1/?): “* Adjusted objective text for "Damn Dirty Bots". / * Fixed typos for dialog in "The Core of the Matter". / * Individual Bosses can no longer be repeatedly killed for credit for the Incline ToO.”
- **PlayTR EU** [eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_26th_febru …](https://web.archive.org/web/20080303001652/http://eu.rgtr.com:80/en/news_article/patch_notes_and_known_issues_26th_february_2008): “Adjusted objective text for “Darn Dirty Robots" / Fixed typos for dialog in “The Core of the Matter" / Individual Bosses can no longer be repeatedly killed for credit for the Incline TOO”
- **NPCs (TaRapedia NPC pages):**
  - [Sgt. McDonald](https://tabularasa.fandom.com/wiki/Sgt._McDonald): Marshes, /loc -124, 224, -307.6 (NPC page lists this mission)
  - [Lt. Kiang](https://tabularasa.fandom.com/wiki/Lt._Kiang): Marshes, /loc -115, 231, -319.5 (named in client text)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 2
  - Requires: Under the Radar (id 1875) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/The_Core_of_the_Matter), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 1879 — This Calls For Destruction


- **Instance mentioned:** Bane Supply Depot — the name appears in the client mission text or the TaRapedia page; not shown to happen there (ESTIMATE)
- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; choose 1 of: Stealth Armor Vest, Bio Armor Vest, Mech Armor Legs, Graviton Armor Vest — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 65,000 XP; 5,400 credits; lvl 35; lvl 35; lvl 35; lvl 35 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Lt. Kiang: Marshes, /loc -115, 231, -319.5 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Lt._Kiang)
- **Client:** Upload a virus to 3 bane computers then destroy their primary generator.
  - objectives (client): Upload 3 AFS viruses to the Bane Computers in the base outside of Bane Supply …; Destroy the Main Generator.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Lt. Kiang
  - reward: 65000 XP; 5400 credits; choose 1 of: Stealth Armor Vest, Bio Armor Vest, Mech Armor Legs, Graviton Armor Vest
  - prerequisites: Requires Under the Radar - Part II (1876 or 1877)
- **TaRapedia** [This Calls For Destruction](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction): giver: Lt. Kiang; at: Central Guard Tower; zone: Marshes; level: 37; requires: Under the Radar - Part II; follow-up: none; XP: 65,000; credits: 5,400; items: lvl 35; lvl 35; lvl 35; lvl 35; reward from: Lt. Kiang
  - overview: Upload a virus to 3 bane computers then destroy their primary generator.
  - objectives: • Upload 3 AFS viruses to the Bane Computers in the base outside of Bane Supply Depot.
- **NPCs (TaRapedia NPC pages):**
  - [Lt. Kiang](https://tabularasa.fandom.com/wiki/Lt._Kiang): Marshes, /loc -115, 231, -319.5 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Under the Radar chain (6 missions), step 3
  - Requires: Under the Radar - Part II (id 1876) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)
  - Requires: Under the Radar - Part II (id 1877) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction), [TaRapedia](https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II)

### 1904 — Gun Control - Part IV


- **Rebuild summary:**
  - level: 36 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 36 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 32500 XP; 3600 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: 32,500 XP; 3,600 credits; Teleract Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34 — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV)
  - drops / mission items: not found (not critical, or not a collect mission)
  - location: NPC Shaman Masai: Marshes, Plateau Point, /loc -735, 217, 171 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Shaman_Masai)
- **Client:** Return to Retread Duvall in the forest outside High Point Retreat.
  - objectives (client): Return to Retread Duval at the forest north of high point retreat.
- **This PR:** revision `marshes_1_16_5`, level 36, solo, category 10000051 (Battlefield (Marshes))
  - reward: 32500 XP; 3600 credits; choose 1 of: Pulse Chaingun, Shotgun, Virulent Injector Gun, Sonic Cannon
  - prerequisites: Requires completed mission 1867; Gun Control - Part III ended with flag 1867001 = 2
- **TaRapedia** [Gun Control - Part IV](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV): giver: Shaman Masai; at: Plateau Point; zone: Marshes; level: 36; requires: Gun Control - Part III; follow-up: none; XP: 32,500; credits: 3,600; items: Teleract Pulse Chaingun lvl 33; Audiodyne Shotgun lvl 37; Vextronics Virulent Injector Gun lvl 37; ChiTech Sonic Cannon lvl 34; reward from: Retread Lou
  - overview: Return to Retread Lou at High Point Retreat
  - objectives: • Return to Retread Lou at High Point Retreat
- **TaRapedia mission list** [Plateau Point mission list](https://tabularasa.fandom.com/wiki/Plateau_Point_mission_list): zone Marshes, area Plateau Point, given by Shaman Masai
- **NPCs (TaRapedia NPC pages):**
  - [Shaman Masai](https://tabularasa.fandom.com/wiki/Shaman_Masai): Marshes, Plateau Point, /loc -735, 217, 171 (NPC page lists this mission)
  - [Retread Lou](https://tabularasa.fandom.com/wiki/Retread_Lou): Marshes, Stalker Woods, /loc -776, 241, -542 (named as giver/receiver)
  - [Retread Duvall](https://tabularasa.fandom.com/wiki/Retread_Duvall): Marshes, /loc -593, 216, -438 (named as giver/receiver)

- **Chain** (explicit prerequisite links): Gun Control chain (5 missions), step 4
  - Requires: Gun Control - Part III (id 1867) — source: [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III), [TaRapedia](https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV), this PR's World data (`mission_content_definition` / `mission_reward_*`)

### 2009 — Soyuz Salvage: Marshes


- **Rebuild summary:**
  - level: 37 — source: this PR's World data (`mission_content_definition` / `mission_reward_*`); 37 — source: [web](https://tabularasa.fandom.com/wiki/Updates/2008-08-14)
  - level ESTIMATE from zone: the Marshes zone level range is 36-40 — source: [Ellatha map: Marshes](https://www.ellatha.com/tr/Marshes_Maps_Logos.asp) (a zone range, not this mission's own level)
  - level ESTIMATE from zone: most Marshes missions are level 33–37 (median 35, 10th–90th percentile) — derived from 65 missions in this file with a sourced level
  - reward: 65000 XP; 5400 credits; Soyuz ISS Model Rocket — source: this PR's World data (`mission_content_definition` / `mission_reward_*`)
  - reward: Soyuz ISS Model Rocket (reward listed on the sister mission Soyuz Salvage: Plateau) — source: [web](https://tabularasa.fandom.com/wiki/Updates/2008-08-14)
  - drops / mission items: The giver comes from client_log, except for Marshes, where no giver is named — source: [Web](https://tabularasa.fandom.com/wiki/Updates/2008-08-14)
  - location: NPC Sergeant Starling: /loc -268, 216, -812 — source: [TaRapedia NPC](https://tabularasa.fandom.com/wiki/Sergeant_Starling)
- **Client:** Locate one of the pieces of the Orbital Module from General British's Soyuz, somewhere in Valverde Marshes.
  - objectives (client): Salvage the Soyuz Debris — Locate and salvage the Soyuz orbital module Debris that crashed somewhere in Valverde Marshes.
- **This PR:** revision `marshes_1_16_5`, level 37, solo, category 10000051 (Battlefield (Marshes)), giver Sergeant Starling
  - reward: 65000 XP; 5400 credits; items: Soyuz ISS Model Rocket
- **Web:** zone: Valverde Marshes; level: 37; items: Soyuz ISS Model Rocket (reward listed on the sister mission Soyuz Salvage: Plateau) — One of five 'Soyuz Salvage' missions added in Deployment 11 (Aug 2008) for the Operation Immortality event honoring Richard Garriott's spaceflight; players could complete only one of the five. They were removed in Deployment 12 (Sep 2008) once all Soyuz pieces had been recovered for display at … Sources: [TaRapedia: Updates/2008-08-14 (Deployment 11 patch notes)](https://tabularasa.fandom.com/wiki/Updates/2008-08-14), [TaRapedia: Updates/2008-09-18 (Deployment 12 patch notes)](https://tabularasa.fandom.com/wiki/Updates/2008-09-18), [TaRapedia: Soyuz Salvage (index page)](https://tabularasa.fandom.com/wiki/Soyuz_Salvage)
- **NPCs (TaRapedia NPC pages):**
  - [Sergeant Starling](https://tabularasa.fandom.com/wiki/Sergeant_Starling): /loc -268, 216, -812 (named as giver/receiver)
- **Drops / item sources:**
  - The giver comes from client_log, except for Marshes, where no giver is named — source: [Web](https://tabularasa.fandom.com/wiki/Updates/2008-08-14)

