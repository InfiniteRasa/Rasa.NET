using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// AWOL Andy (native mission 889), Valverde Marshes: MP Price in Paludos sends the player to find Lieutenant Bruncke,
    /// who went off a few days ago, at his last known locations, and to bring him back.
    ///
    /// Native: texts 4650-4653, 7084 (missionconversation): log "Check the last known locations for Lieutenant Bruncke.
    /// Once he's located, escort him back to MP Price in Paludos.", offer "We've had reports of him at a couple locations in
    /// the area", turn-in "A few days in the brig will get Bruncke sorted right out."; objectives 1 "Locate Private
    /// Bruncke" 4658/7085, 2 "Locate Lieutenant Bruncke and escort him back to MP Price" 4659/7086, 3 "... back to Paludos"
    /// 4660/7087, 4-7 "Search a Last Known Location for Lieutenant Bruncke" 4661-4664/7088-7091 (missionobjective);
    /// objective conversation 889,1 package 386 flag 1 COMPLETION 4665 (Bruncke: "lead the way - I'll follow you in").
    /// Indicator name 140 "Possible location of Private Bruncke" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia AWOL Andy: giver and receiver MP Price (Paludos); level 37; 130,000 XP, 7,200 credits; one of
    /// Teleract Bio Armor Legs 4, Titan Graviton Armor Legs 4, Teleract Mech Armor Helmet 4, ChiTech Stealth Armor Helmet 4
    /// (level 35); 4 "Search a Last Known Location" objectives. World DB: Price 510163 (package 386) at (-317.6, 218.5, -2.8).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Package 386 is Bruncke's (user decision U1: Price takes a server-only package, MarshesEscortsNpcsV1), so 1 is done
    ///    only at Bruncke.
    ///  - Four search places round Paludos on ground walked to from the road (probe of 2026-11-02): west (-520, 40),
    ///    north-west (-450, 120), south (-330, -160), east (-200, -100); each of 4-7 (optional) is done by walking into 20 m of
    ///    its place, any order. Bruncke (the run's own, MarshesEscortsNpcsV1.Bruncke) stands at the east one (7) from the
    ///    accept; his talk completes 1, opens 2 and he follows. Walking into 25 m of Price completes 2 (the path from the
    ///    road ends 15 m short of Price: Paludos's navmesh is cut off) and Bruncke is taken to the brig. His death fails 1
    ///    or 2. Objective 3 (the same with "Paludos") left out: 2 says it.
    ///  - The rewards the client block 120013-120016 (Bio Legs, Graviton Legs, Mech Helmet, Stealth Helmet V04 35-39 =
    ///    TaRapedia's four, "4" read as V04); the lowest of the four blocks that match (120013, 120025, 120093, 120342).
    ///  - Markers: 4-7 the client's 140 at their places, 1 unnamed at the four places, 2 at Price.
    /// </summary>
    public static class MarshesAwolAndyV1
    {
        public const uint MissionId = 889;
        public const uint Level = 37;
        public const uint Price = MarshesEscortsNpcsV1.Price;
        public const uint Locate = 1, Escort = 2;
        public const uint Found = 1, Arrived = 2, PriceArea = 5;
        public const uint SearchName = 140;             // missionobjectiveindicatorlanguage "Possible location of Private Bruncke"
        public const double SearchRadius = 20, PriceRadius = 25;

        /// <summary>The search objectives, their texts and places; Bruncke stands at the last.</summary>
        public static readonly (uint Objective, uint Name, uint Body, ScenePosition At)[] Searches =
        {
            (4, 4661, 7088, new(-520f, 217.48f, 40f)),
            (5, 4662, 7089, new(-450f, 217.06f, 120f)),
            (6, 4663, 7090, new(-330f, 215.12f, -160f)),
            (7, 4664, 7091, new(-200f, 215.66f, -100f)),
        };

        public static ScenePosition BrunckeAt => Searches[^1].At;
        public static readonly ScenePosition PriceAt = new(-317.6f, 218.48f, -2.8f);

        public const uint Experience = 130000;
        public const uint Credits = 7200;
        public const uint BioLegs = 120013;             // Armor_T3_Bio_V04_UNC_Legs_35_to_39
        public const uint GravitonLegs = 120014;        // Armor_T3_Graviton_V04_UNC_Legs_35_to_39
        public const uint MechHelmet = 120015;          // Armor_T3_Mech_V04_UNC_Helmet_35_to_39
        public const uint StealthHelmet = 120016;       // Armor_T3_Stealth_V04_UNC_Helmet_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "AWOL Andy", 4650, Price, Price, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, GravitonLegs, MechHelmet, StealthHelmet });
            mission.Scenario(Found, "Bruncke found: he follows", 0);
            mission.Scenario(Arrived, "Bruncke back at Price", 0);

            mission.Objective(Locate, 4658, 7085, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Escort, 4659, 7086, 1, MissionObjectiveState.Inactive);
            var ordinal = 2U;
            foreach (var (objective, name, body, _) in Searches)
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete, required: false);

            // 1: Bruncke's talk; he follows, 2 opens.
            mission.Transition(Locate);
            mission.Conversation(Locate, MarshesEscortsNpcsV1.BrunckePackage);
            mission.Action(Locate, 1, MissionActionKind.RevealObjective, targetObjective: Escort);
            mission.Action(Locate, 2, MissionActionKind.ActivateObjective, targetObjective: Escort, state: MissionObjectiveState.Incomplete);
            mission.Action(Locate, 3, MissionActionKind.StartScenario, scenario: Found);
            // 2: back at Price.
            mission.Area(PriceArea, MarshesMissionDataV1.MapContextId, PriceAt.X, PriceAt.Y, PriceAt.Z, PriceRadius);
            mission.Transition(Escort);
            mission.EnterArea(Escort, PriceArea);
            mission.Action(Escort, 1, MissionActionKind.StartScenario, scenario: Arrived);
            mission.Action(Escort, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Escort, 5, PriceAt.X, PriceAt.Y, PriceAt.Z, PriceRadius);
            // 4-7: the search places.
            var marker = 1U;
            foreach (var (objective, _, _, at) in Searches)
            {
                var area = objective - 3;
                mission.Area(area, MarshesMissionDataV1.MapContextId, at.X, at.Y, at.Z, SearchRadius);
                mission.Transition(objective);
                mission.EnterArea(objective, area);
                mission.NamedIndicator(objective, SearchName, at.X, at.Y, at.Z, SearchRadius);
                mission.Indicator(Locate, marker++, at.X, at.Y, at.Z, SearchRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/889",
                "Native1 'Locate Private Bruncke' 4658/7085, 2 escort to MP Price 4659/7086, 3 to Paludos 4660/7087, 4-7 'Search a " +
                "Last Known Location' 4661-4664/7088-7091; objectiveconversation 889,1 pkg386 4665. Texts 4650-4653. Name 4663 " +
                "Lieutenant Bruncke. Indicator name 140.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/AWOL_Andy",
                "Giver/receiver MP Price (Paludos); level37; 130000XP, 7200 credits; one of Teleract Bio Armor Legs4, Titan " +
                "Graviton Armor Legs4, Teleract Mech Armor Helmet4, ChiTech Stealth Armor Helmet4 (lvl35); 4 search objectives.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesAwolAndyV1",
                "Package 386 moved to Bruncke 760334 (U1), Price a server-only package. Four search places round Paludos (20 m, " +
                "optional); Bruncke at the east one, his talk completes 1 and he follows; 25 m from Price completes 2; his death " +
                "fails it. 3 left out. Items the client block 120013-120016, one to choose.", 0.3);

            mission.Enable(MarshesEscortsNpcsV1.Scene(MissionId,
                new MarshesEscortsNpcsV1.Leg(Escort, Found, Arrived, Locate, MarshesEscortsNpcsV1.Bruncke, new[] { BrunckeAt })));
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
