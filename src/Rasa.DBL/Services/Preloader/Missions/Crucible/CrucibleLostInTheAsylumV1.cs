using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Lost in the Asylum (native mission 1444), the Incurables Ward (squad instance 2141): Supervisor Sheira sent three of her
    /// men into the Ward to fix the water system and none came back; the player finds Mechanic Shanul, Technician Paliv and
    /// Engineer Dhimand.
    ///
    /// Native: texts 12638-12642 (missionconversation): log "Travel into the Incurables Ward and find Mechanic Shanul,
    /// Technician Paliv, and Engineer Dhimand. ...", offer "I sent three of my best men into the ward, but they not be
    /// returning. ...", turn-in "Poor Paliv. That is too bad. I am being glad to hear that Shanul and Dhimand are alive. ...";
    /// objectives (missionobjective, no counters) 1 "Find Mechanic Shanul" 12682/12683, 2 "Find Technician Paliv"
    /// 12685/12686, 3 "Find Engineer Dhimand" 12688/12689; objective conversations (1444,1) package 1664 (Shanul) 12684 "...
    /// you will need to recover keycards from Jaikev's lieutenants, Atali and Umank.", (1444,2) package 1727 12687 "Technician
    /// Paliv is clearly dead. ...", (1444,3) package 1700 (Dhimand) 12690 "... I need to shut down those valves. Tell Sheira
    /// I'll be back as soon as my job is done!". Paliv's body: class 25474 UsableNPCBrannCorpseMaleV01 "Fresh Brann Corpse"
    /// (a corpse that talks, augmentation 52).
    ///
    /// Sourced: TaRapedia Lost in the Asylum: giver and reward giver Supervisor Sheira (Incurables Ward); requires Sadistic
    /// Sabotage; 170,000 XP, 8,000 credits; one of Audiodyne Mech Armor Gloves (icon 6), Olympia Stealth Armor Helmet (5), Titan
    /// Hazmat Armor Gloves (4), CryoGen Graviton Armor Boots (1) (cache, 0.5); Paliv a "Fresh Brann Corpse". No source gives
    /// the level.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (the Ward's creatures' level; no source). The three objectives open at once, any order; each a talk.
    ///  - The keycards and the Sanctum door are 1401's (set 11b, not built): Paliv and Dhimand are reached without them
    ///    (deviation). The places (CrucibleIncurablesWardNpcsV1): Shanul in the dormitory east of the arrival; Paliv's body by
    ///    the north door display (94, 78.5, 421; 4 m east of the spot first taken, (90, 79.21, 424), which a
    ///    wall hid from the west ring places); Dhimand in the hallway between the Throne Room and the Sanctum hub; the last
    ///    two walked to through the north airlock.
    ///  - Paliv a personal talking corpse (25474, the Bootcamp corpse's way, as Missing in Action 1863) in the squad's Ward.
    ///  - The scene bound to the Ward (BoundMapContextId): taken at Sheira in the squad's copy.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40, the nearest icon
    ///    number where the band has none (Mech V04 Gloves 34116, Stealth V04 Helmet 40373, Hazmat V03 Gloves 21249 (39-43),
    ///    Graviton V02 Boots 23569), one to choose. A marker at each find.
    /// </summary>
    public static class CrucibleLostInTheAsylumV1
    {
        public const uint MissionId = 1444;
        public const uint Level = 40;
        public const uint Sheira = CrucibleIncurablesWardNpcsV1.Sheira;
        public const uint MapContextId = CrucibleIncurablesWardNpcsV1.Ward;
        public const uint FindShanul = 1, FindPaliv = 2, FindDhimand = 3;
        public const double MarkerRadius = 8;

        public const uint CorpseClass = 25474;          // UsableNPCBrannCorpseMaleV01 "Fresh Brann Corpse"
        public const uint CorpsePackage = 1727;         // objectiveconversation 1444,2
        public const string CorpseRole = "paliv-body";
        public static readonly ScenePosition CorpseAt = new(94f, 78.5f, 421f);
        public const double CorpseOrientation = 0;

        public const uint Experience = 170000;
        public const uint Credits = 8000;
        public const uint MechGloves = 34116;           // Armor_T3_Mech_V04_UNC_Gloves_40_to_44
        public const uint StealthHelmet = 40373;        // Armor_T3_Stealth_V04_UNC_Helmet_40_to_44
        public const uint HazmatGloves = 21249;         // Armor_T2_Hazmat_V03_UNC_Gloves_39_to_43
        public const uint GravitonBoots = 23569;        // Armor_T3_Graviton_V02_UNC_Boots_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Lost in the Asylum", 12638, Sheira, Sheira, Level);
            mission.Reward(Experience, Credits, new[] { MechGloves, StealthHelmet, HazmatGloves, GravitonBoots });
            mission.Prerequisite(CrucibleSadisticSabotageV1.MissionId);

            mission.Objective(FindShanul, 12682, 12683, 0, MissionObjectiveState.Incomplete);
            mission.Objective(FindPaliv, 12685, 12686, 1, MissionObjectiveState.Incomplete);
            mission.Objective(FindDhimand, 12688, 12689, 2, MissionObjectiveState.Incomplete);

            mission.Transition(FindShanul);
            mission.Conversation(FindShanul, CrucibleIncurablesWardNpcsV1.ShanulPackage);
            mission.Action(FindShanul, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(FindPaliv);
            mission.Conversation(FindPaliv, CorpsePackage);
            mission.Transition(FindDhimand);
            mission.Conversation(FindDhimand, CrucibleIncurablesWardNpcsV1.DhimandPackage);

            var shanul = CrucibleIncurablesWardNpcsV1.ShanulAt;
            var dhimand = CrucibleIncurablesWardNpcsV1.DhimandAt;
            mission.Indicator(FindShanul, 1, shanul.X, shanul.Y, shanul.Z, MarkerRadius, MapContextId);
            mission.Indicator(FindPaliv, 2, CorpseAt.X, CorpseAt.Y, CorpseAt.Z, MarkerRadius, MapContextId);
            mission.Indicator(FindDhimand, 3, dhimand.X, dhimand.Y, dhimand.Z, MarkerRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1444",
                "Native1 'Find Mechanic Shanul' 12682/12683, 2 'Find Technician Paliv' 12685/12686, 3 'Find Engineer Dhimand' " +
                "12688/12689, no counters; objectiveconversation 1444,1 package1664 12684 (the keycards of Atali and Umank), 1444,2 " +
                "1727 12687 (Paliv clearly dead), 1444,3 1700 12690. Texts 12638-12642. Names 9550 Mechanic Shanul, 9614 Engineer " +
                "Dhimand; class25474 'Fresh Brann Corpse'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Lost_in_the_Asylum",
                "Giver/reward giver Supervisor Sheira (Incurables Ward); requires Sadistic Sabotage; 170000XP, 8000 credits; " +
                "Audiodyne Mech Armor Gloves 6, Olympia Stealth Armor Helmet 5, Titan Hazmat Armor Gloves 4, CryoGen Graviton Armor " +
                "Boots 1 (cache); Paliv a Fresh Brann Corpse. No level.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleLostInTheAsylumV1",
                "Bound to the squad's Ward 2141. Level40. Three talks open at once: Shanul 770045 (package1664), Paliv a personal " +
                "talking corpse 25474 (package1727) at 94,78.5,421, Dhimand 770046 (package1700); no keycard or Sanctum door (1401, " +
                "not built). Items 34116, 40373, 21249, 23569, one to choose. A marker at each.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Paliv's body, the player's own, in the squad's Ward.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Sheira in the squad's Ward
                MapContextId = MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition> { new(FindPaliv, CorpsePackage, 1, transitionId: 1) }
            };
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, CorpseAt,
                Orientation: CorpseOrientation, InitialObjectState: 0,
                Conversation: new SceneObjectConversation(MissionId, FindPaliv, CorpsePackage, FindPaliv));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{CorpseRole}", CorpseRole) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
