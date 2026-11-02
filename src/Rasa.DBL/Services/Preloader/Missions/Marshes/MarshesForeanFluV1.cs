using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Forean Flu (native mission 497), Valverde Marshes: Shaman Masai at Plateau Point sends a wounded Forean Warrior with
    /// the player to Field Medic DuBois at Falcon Hold.
    ///
    /// Native: texts 1233-1236, 7165 (missionconversation): log "Escort the Forean Warrior to Field Medic DuBois in Falcon
    /// Hold Outpost.", offer "escort one of our wounded warriors up to Falcon Hold in the north", turn-in "We'll find this
    /// fellow a bed and some bandages"; objective 1 "Escort the Forean Warrior to Falcon Hold" 4942/7166, no counter
    /// (missionobjective); no objective conversation. Class 6043 Redshirt_Forean_Spearman "Forean Warrior" (no creature name).
    ///
    /// Sourced: TaRapedia Forean Flu: giver Shaman Masai (Plateau Point), receiver Field Medic DuBois (Falcon Hold); 60,000
    /// XP, 5,250 credits; one of CryoGen Bio Armor Boots, Hellstrom Mech Armor Boots, Pulsar Graviton Armor Helmet, Voltrox
    /// Reflective Armor Legs (level 35). TR MMO DB: level 33. World DB: DuBois 510161 at (-142.7, 219.1, 613.6).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The warrior the run's own creature (MarshesEscortsNpcsV1.Warrior), beside Masai at the accept, following the player
    ///    (MarshesEscortScene); walking into 15 m of DuBois completes 1 and he is taken in; his death fails 1 (and the
    ///    mission). The navmesh path Plateau Point - Falcon Hold is 655 m (probe of 2026-11-02); no route is set.
    ///  - The rewards the lowest Uncommon templates of the kinds and bands (set 1's rule; no client block matches), one to
    ///    choose. Markers at DuBois.
    /// </summary>
    public static class MarshesForeanFluV1
    {
        public const uint MissionId = 497;
        public const uint Level = 33;
        public const uint Masai = MarshesGunControlNpcsV1.Masai, DuBois = 510161;
        public const uint Escort = 1, Area = 1, Arrived = 2;
        public const double ArrivalRadius = 15;

        public static readonly ScenePosition WarriorAt = new(-733f, 217.95f, 173f);
        public static readonly ScenePosition DuBoisAt = new(-143.9f, 219.28f, 614.2f);

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint BioBoots = 18750;             // Armor_T3_Bio_V02_UNC_Boots_35_to_39
        public const uint MechBoots = 32677;            // Armor_T3_Mech_V02_UNC_Boots_35_to_39
        public const uint GravitonHelmet = 23808;       // Armor_T3_Graviton_V02_UNC_Helmet_35_to_39
        public const uint ReflectiveLegs = 36019;       // Armor_T2_Reflective_V02_UNC_Legs_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Forean Flu", 1233, Masai, DuBois, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, MechBoots, GravitonHelmet, ReflectiveLegs });
            mission.Scenario(Arrived, "The warrior at DuBois", 0);

            mission.Objective(Escort, 4942, 7166, 0, MissionObjectiveState.Incomplete);
            mission.Area(Area, MarshesMissionDataV1.MapContextId, DuBoisAt.X, DuBoisAt.Y, DuBoisAt.Z, ArrivalRadius);
            mission.Transition(Escort);
            mission.EnterArea(Escort, Area);
            mission.Action(Escort, 1, MissionActionKind.StartScenario, scenario: Arrived);
            mission.Action(Escort, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Escort, 1, DuBoisAt.X, DuBoisAt.Y, DuBoisAt.Z, ArrivalRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/497",
                "Native1 'Escort the Forean Warrior to Falcon Hold' 4942/7166, no counter; no objectiveconversation. Texts " +
                "1233-1236 (log: to Field Medic DuBois in Falcon Hold Outpost). Class 6043 'Forean Warrior', no creature name.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Forean_Flu",
                "Giver Shaman Masai (Plateau Point), receiver Field Medic DuBois (Falcon Hold); 60000XP, 5250 credits; one of " +
                "CryoGen Bio Armor Boots, Hellstrom Mech Armor Boots, Pulsar Graviton Armor Helmet, Voltrox Reflective Armor Legs " +
                "(lvl35). TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesForeanFluV1",
                "Personal warrior 760332 (class 6043, L35, 4000 HP) follows the player from Masai; 15 m from DuBois completes 1; " +
                "his death fails it. Items the lowest Uncommon of the bands: 18750, 32677, 23808, 36019, one to choose.", 0.3);

            mission.Enable(MarshesEscortsNpcsV1.Scene(MissionId,
                new MarshesEscortsNpcsV1.Leg(Escort, 0, Arrived, null, MarshesEscortsNpcsV1.Warrior, new[] { WarriorAt })));
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
