using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Lockdown the Conscription Facility (native mission 1500), Ligo Ashen Desert to the Bane Conscription Facility: Captain
    /// Yamamoto sends the player into the facility to rendezvous with Capt. Peacock, who waits just inside.
    ///
    /// Native: texts 13394-13398 (missionconversation): title "Lockdown the Conscription Facility", log "Rendezvous with Captain
    /// Peacock at the Bane Conscription Facility", offer "We need you to get to the conscription facility and rendezvous with
    /// Capt. Peacock.", turn-in "Alright soldier, we've got a lot of work ahead of us.", reminder "Head to the Bane Conscription
    /// Facility and rendezvous with Captain Peacock."; objective 1 "Rendevous with Captain Peacock at the Bane Conscription
    /// Facility" 14058/14059, no counter (missionobjective); objective conversation (1500,1) package 1928 (Peacock) variant 1,
    /// 14060 "You got here just in time!". Name 9740 Capt. Peacock. The door: map_link 57, Ashen Desert (427.74, 305.61,
    /// 206.80) r6 to the facility (7.25, 153.5, -670.25).
    ///
    /// Sourced: TaRapedia Lockdown the Conscription Facility (0.5): giver Captain Aiko Yamamoto (Boneyard Outpost), reward giver
    /// Capt. Peacock (Bane Conscription Facility), "Capt. Peacock is just inside the instance"; follow-up Raid the Conscription
    /// Facility; 47,500 XP, 6,300 credits; Class VIII Fragmentation Grenade x3, Class VIII EMP Bomb, Class VIII Cryogenic
    /// Grenade x3, Class VIII Standard Med Pack x3. The client's log names the turn-in (Peacock, "rendezvous"): an NPC turn-in.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (the facility's band, D5). Requires The Big Secret 1581 (Zuriel names the facility; TaRapedia names none).
    ///  - Given at Yamamoto (AshenLeadInNpcsV1 790003); 1 the talk with Peacock (AshenConscriptionEntryNpcsV1 790659, package
    ///    1928) gives the reward; the turn-in at Peacock.
    ///  - The items by their client names, each a stack, one to choose: Class VIII Fragmentation Grenade (45447) x3, Class VIII
    ///    EMP Bomb (111233, the lower of its two templates) x1, Class VIII Cryogenic Grenade (111049) x3, Class VIII Standard Med
    ///    Pack (45064) x3 (the stack sizes TaRapedia's, as 1581's).
    ///  - Markers (unnamed) for 1: before the Ashen Desert door (Ashen Desert, on the plaza 21.8 m south of it) and Peacock (the
    ///    facility).
    /// </summary>
    public static class AshenLockdownTheConscriptionFacilityV1
    {
        public const uint MissionId = 1500;
        public const uint Level = 42;
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto, Peacock = AshenConscriptionEntryNpcsV1.Peacock;
        public const uint PeacockPackage = AshenConscriptionEntryNpcsV1.PeacockPackage, Variant = 1;
        public const uint Rendezvous = 1;
        public const double DoorMarkerRadius = 6, NpcMarkerRadius = 8;

        /// <summary>
        /// The Ashen Desert marker before the facility's door: on the plaza the door's walkway starts from, 21.8 m south of map_link
        /// 57 (427.74, 305.61, 206.80, r6), so its circle stays 5 m and more outside the link's (AshenGuardTests); the client floor
        /// 303.9 and the navmesh's walk to the door (the probe, 2026-11-12).
        /// </summary>
        public static readonly (double X, double Y, double Z) DoorMarkerAt = (427.7, 304, 185);

        public const uint Experience = 47500;
        public const uint Credits = 6300;
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Fragmentation Grenade (class 22964)
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)
        public const uint CryogenicGrenade = 111049, CryogenicGrenades = 3;     // Class VIII Cryogenic Grenade (class 26299)
        public const uint StandardMedPack = 45064, StandardMedPacks = 3;        // Class VIII Standard Med Pack (class 22547)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Lockdown the Conscription Facility", 13394, Yamamoto, Peacock, Level);
            mission.Prerequisite(AshenTheBigSecretV1.MissionId);
            mission.Reward(Experience, Credits,
                new[] { (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);

            mission.Objective(Rendezvous, 14058, 14059, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Rendezvous);
            mission.Conversation(Rendezvous, PeacockPackage, Variant);
            mission.Action(Rendezvous, 1, MissionActionKind.GrantReward, reward: 1);

            var peacock = AshenConscriptionEntryNpcsV1.PeacockAt;
            mission.Indicator(Rendezvous, 1, DoorMarkerAt.X, DoorMarkerAt.Y, DoorMarkerAt.Z, DoorMarkerRadius);
            mission.Indicator(Rendezvous, 2, peacock.X, peacock.Y, peacock.Z, NpcMarkerRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1500",
                "Native1 'Rendevous with Captain Peacock at the Bane Conscription Facility' 14058/14059, no counter; " +
                "objectiveconversation 1500,1 pkg1928 14060. Texts 13394-13398. Name 9740 Capt. Peacock. map_link 57 AD " +
                "(427.74,305.61,206.80) r6 -> BCF (7.25,153.5,-670.25).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Lockdown_the_Conscription_Facility",
                "Giver Captain Aiko Yamamoto (Boneyard Outpost), reward giver Capt. Peacock (Bane Conscription Facility, just inside " +
                "the instance); follow-up Raid the Conscription Facility; 47500XP, 6300 credits; Class VIII Fragmentation Grenade x3, " +
                "Class VIII EMP Bomb, Class VIII Cryogenic Grenade x3, Class VIII Standard Med Pack x3.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenLockdownTheConscriptionFacilityV1",
                "Level42; requires 1581 (estimated); Yamamoto 790003 gives, Peacock 790659 (6,153.36,-646) rewards; 1 his talk " +
                "(pkg1928). Markers: before the AD door (427.7,304,185), Peacock (BCF). Items 45447x3, 111233x1, 111049x3, 45064x3, one to choose.", 0.3);

            // The scene's map is Ashen Desert (the giver's), so the door's marker is sent there; Peacock's goes to the facility
            // through the scene's IndicatorMaps (AshenMissionDataV1.Enable), as 1559's Diren.
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = AshenMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
