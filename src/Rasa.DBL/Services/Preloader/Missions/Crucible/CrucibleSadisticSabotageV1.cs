using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Sadistic Sabotage (native mission 1445), Ligo Crucible and the Incurables Ward (squad instance 2141): Viddia Sigal Nema
    /// sends the player down into the Incurables Ward to report to Supervisor Sheira.
    ///
    /// Native: texts 12643-12647 (missionconversation): log "Travel into the Incurables Ward, inside of Staal, and report to
    /// Supervisor Sheira.", offer "The Incurables damaged what be water processing systems in the deepest parts of Staal. ...",
    /// turn-in "Well, you are not being what I expected, but I guess you will do. Hopefully you be stronger than those
    /// dimwitted Larai."; objective 1 "Report to Supervisor Sheira" 12648/12649, no counter (missionobjective); objective
    /// conversation (1445,1) package 1640 (Sheira) text 12650 "Sigal Nema be sending you?".
    ///
    /// Sourced: TaRapedia Sadistic Sabotage: giver Viddia Sigal Nema (Staal Infirmary), reward giver Supervisor Sheira
    /// (Incurables Ward); requires Game Wardens (1147); follow-up Lost in the Asylum; 42,500 XP, 4,000 credits; Class VIII
    /// Basic Med Pack x6, Frag Grenade x3, EMP Bomb, Cryogenic Grenade x3. TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The talk with Sheira completes it; the turn-in at Sheira in the squad's Ward (the turn-in text is hers). Sheira
    ///    510038 moved 12 m north of her upstream spot (CrucibleIncurablesWardNpcsV1): it was the medic's and the hospital
    ///    teleporter's.
    ///  - The items by their client names (physicalentityclassnamelanguage), each a stack, one to choose (as The Boss Of All
    ///    Bosses 1161): 22546 Class VIII Basic Med Pack (45063) x6, 22964 Class VIII Fragmentation Grenade (45447; TaRapedia's
    ///    "Frag Grenade", as Prometheus Rising) x3, 26543 Class VIII EMP Bomb (111233) x1, 26299 Class VIII Cryogenic Grenade
    ///    (111049) x3.
    ///  - Markers: on the floor before the Ward's door on the Crucible and on Sheira in the Ward. The scene's map the
    ///    Crucible's (no actor): the marker default of a mission whose giver and receiver stand on two maps is 0.
    /// </summary>
    public static class CrucibleSadisticSabotageV1
    {
        public const uint MissionId = 1445;
        public const uint Level = 35;
        public const uint Nema = CrucibleStaalSigalsNpcsV1.Nema;
        public const uint Sheira = CrucibleIncurablesWardNpcsV1.Sheira, SheiraPackage = CrucibleIncurablesWardNpcsV1.SheiraPackage;
        public const uint MapContextId = CrucibleIncurablesWardNpcsV1.Ward;
        public const uint Report = 1;
        public const double SheiraRadius = 8, DoorMarkerRadius = 6;

        public const uint Experience = 42500;
        public const uint Credits = 4000;
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Fragmentation Grenade (class 22964)
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)
        public const uint CryoGrenade = 111049, CryoGrenades = 3;               // Class VIII Cryogenic Grenade (class 26299)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Sadistic Sabotage", 12643, Nema, Sheira, Level);
            mission.Reward(Experience, Credits,
                new[] { (BasicMedPack, BasicMedPacks), (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryoGrenade, CryoGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleGameWardensV1.MissionId);

            mission.Objective(Report, 12648, 12649, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, SheiraPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var door = CrucibleIncurablesWardNpcsV1.DoorWayAt;
            var sheira = CrucibleIncurablesWardNpcsV1.SheiraAt;
            mission.Indicator(Report, 1, door.X, door.Y, door.Z, DoorMarkerRadius, CrucibleMissionDataV1.MapContextId);
            mission.Indicator(Report, 2, sheira.X, sheira.Y, sheira.Z, SheiraRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1445",
                "Native1 'Report to Supervisor Sheira' 12648/12649, no counter; objectiveconversation 1445,1 package1640 text12650 " +
                "'Sigal Nema be sending you?'. Texts 12643-12647. Names 9069 Viddia Sigal Nema, 9517 Supervisor Sheira.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Sadistic_Sabotage",
                "Giver Viddia Sigal Nema (Staal Infirmary), reward giver Supervisor Sheira (Incurables Ward); requires Game Wardens; " +
                "follow-up Lost in the Asylum; 42500XP, 4000 credits; Class VIII Basic Med Pack x6, Frag Grenade x3, EMP Bomb, " +
                "Cryogenic Grenade x3. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleSadisticSabotageV1",
                "Sheira's talk (package1640) in the squad's Ward 2141 completes 1; Sheira 510038 moved 12 m north, off the medic's " +
                "spot. Items 45063x6, 45447x3, 111233x1, 111049x3, one to choose. Markers: before the Ward's door (Crucible), Sheira.", 0.3);

            // The scene's map is the Crucible's (the giver's): with no scene the marker default is 0 (giver and receiver on two
            // maps) and the door marker would never be sent (MissionMarkerProjection.LoadDefaults); Sheira's marker goes to
            // the Ward through the scene's IndicatorMaps (CrucibleMissionDataV1.Enable). As 1519 and 1218.
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = CrucibleMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
