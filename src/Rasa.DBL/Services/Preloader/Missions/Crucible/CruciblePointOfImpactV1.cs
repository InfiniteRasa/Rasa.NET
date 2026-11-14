using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Point Of Impact (native mission 1217), Ligo Crucible: Awol Captain Cheung sends the player with the Eloh Resonator (Not
    /// My Junk's reward) to Professor Tiernan at the Eloh crash site near Devil's Pitchfork; the escape pod there answers the
    /// resonator: the Kardash Eloh Obelisk speaks the debriefing.
    ///
    /// Native: texts 9584-9587, 9588 (missionconversation; 9588 is empty): log "Take the Eloh Resonator to Professor Tiernan,
    /// the researcher in charge of the Eloh crash site near Devil's Pitchfork.", offer (Cheung) "... There's a researcher named
    /// Professor Tiernan investigating it ... Talk to Tiernan.", turn-in (the obelisk) "If you are hearing this message, the
    /// planet of Erdas is lost. Take the object within the escape pod. ..."; objective 1 "Speak to Professor Tiernan."
    /// 10540/10541, no counter (missionobjective); objectiveconversation (1217,1) package 1483 (Tiernan) 10542 "... Approach the
    /// object with the resonator and see if it will respond to you.". Usable class 24889 UsableTreasureDispElohReliquaryV01NPC
    /// "Kardash Eloh Obelisk" (mesh arch_eloh_reliquary_v01; classset 52, a talking object as 1150's monitor 11143; no
    /// usabledata row). Item class 26797 Eloh Resonator (template 117177).
    ///
    /// Sourced: TaRapedia Point Of Impact: giver Awol Captain Cheung, reward giver the Kardash Eloh Obelisk (Eloh Crash Site);
    /// requires Not My Junk; follow-up It Came From Erdas; 42,500 XP, 4,000 credits; Class VIII EMP Bomb, Cryogenic Grenade
    /// x3, Standard Med Pack x3, Incendiary Grenade x3. TR MMO DB: level 40. The items by their client names
    /// (physicalentityclassnamelanguage): 26543 Class VIII EMP Bomb (templates 111233 and 111244, the same row; the first),
    /// 26299 Class VIII Cryogenic Grenade (111049), 22547 Class VIII Standard Med Pack (45064), 26289 Class VIII Incendiary
    /// Grenade (111039).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The obelisk cannot be the receiver: a receiver is a creature with an NPC package (MissionApplication), and an object's
    ///    talk (SceneObjectConversation) completes an objective only. So: 1 Tiernan's talk opens a hidden 2, "the object
    ///    responds" (the client's texts of 1), the player walking up to the obelisk (AreaEntered, 6 m round it, as 1538's return
    ///    to Cheung); then the mission is turned in by radio (RadioCompletion), and the client shows the obelisk's message 9587
    ///    as the debriefing. The resonator is not checked (no requirement takes a held item that is not the mission's); 1538,
    ///    which gives it, is the prerequisite.
    ///  - The obelisk: the player's own object (class 24889, not interactable: it has no talk of its own here) on the escape
    ///    pod's deck (575.5, 157.27, 232), the pod's walkable top (CrucibleKardashAndPrometheusNpcsV1.PodDeckAt).
    ///  - A marker at Tiernan; hidden 2 has none (the client is not sent a hidden objective). Tiernan's text names the object,
    ///    23 m from him.
    ///  - The four items one to choose, in TaRapedia's stacks.
    /// </summary>
    public static class CruciblePointOfImpactV1
    {
        public const uint MissionId = 1217;
        public const uint Level = 40;
        public const uint Cheung = CrucibleAwolCampNpcsV1.Cheung;
        public const uint TiernanPackage = CrucibleKardashAndPrometheusNpcsV1.TiernanPackage;
        public const uint SpeakToTiernan = 1, Approach = 2;
        public const uint ObeliskArea = 1;
        public const double ObeliskRadius = 6, ObeliskHalfHeight = 4, TiernanMarkerRadius = 8;

        public const string ObeliskRole = "kardash-eloh-obelisk";
        public const uint ObeliskClass = 24889;         // UsableTreasureDispElohReliquaryV01NPC, "Kardash Eloh Obelisk"
        public static readonly ScenePosition ObeliskAt = new(575.5f, 157.27f, 232f);
        public const double ObeliskOrientation = 0;

        public const uint Experience = 42500;
        public const uint Credits = 4000;
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)
        public const uint CryogenicGrenade = 111049, CryogenicGrenades = 3;     // Class VIII Cryogenic Grenade (class 26299)
        public const uint StandardMedPack = 45064, StandardMedPacks = 3;        // Class VIII Standard Med Pack (class 22547)
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;   // Class VIII Incendiary Grenade (class 26289)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Point Of Impact", 9584, Cheung, null, Level);
            mission.RadioCompletion();
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks), (IncendiaryGrenade, IncendiaryGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleNotMyJunkV1.MissionId);

            mission.Objective(SpeakToTiernan, 10540, 10541, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Approach, 10540, 10541, 1, MissionObjectiveState.Inactive);

            // 1: Tiernan's talk opens 2.
            mission.Transition(SpeakToTiernan);
            mission.Conversation(SpeakToTiernan, TiernanPackage);
            CrucibleCalendarGirlV1.Open(mission, SpeakToTiernan, 1, 1, Approach);

            // 2, hidden: the walk up to the obelisk.
            mission.Area(ObeliskArea, CrucibleMissionDataV1.MapContextId, ObeliskAt.X, ObeliskAt.Y, ObeliskAt.Z, ObeliskRadius, ObeliskHalfHeight);
            mission.Transition(Approach);
            mission.EnterArea(Approach, ObeliskArea);
            mission.Action(Approach, 1, MissionActionKind.GrantReward, reward: 1);

            var tiernan = CrucibleKardashAndPrometheusNpcsV1.TiernanAt;
            mission.Indicator(SpeakToTiernan, 1, tiernan.X, tiernan.Y, tiernan.Z, TiernanMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1217",
                "Native1 'Speak to Professor Tiernan.' 10540/10541, no counter; objectiveconversation 1217,1 package1483 10542 ('Approach " +
                "the object with the resonator'). Texts 9584-9587 (9587 the escape pod's message), 9588 empty. Class 24889 " +
                "UsableTreasureDispElohReliquaryV01NPC 'Kardash Eloh Obelisk' (classset 52, no usabledata). Items 26543/111233, " +
                "26299/111049, 22547/45064, 26289/111039 by their client names. Name 9365 Professor Tiernan.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Point_Of_Impact",
                "Giver Awol Captain Cheung, reward giver the Kardash Eloh Obelisk (Eloh Crash Site); requires Not My Junk; follow-up It " +
                "Came From Erdas; 42500XP, 4000 credits; Class VIII EMP Bomb, Cryogenic Grenade x3, Standard Med Pack x3, Incendiary " +
                "Grenade x3. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CruciblePointOfImpactV1",
                "Tiernan 770039 (copy of Keplinger) at 589.5,154.37,250, 23 m from the escape pod. 1 his talk opens hidden 2: the 6 m " +
                "area round the player's own obelisk (24889, not interactable) on the pod's deck 575.5,157.27,232; then a radio " +
                "turn-in (the obelisk is no creature, so no receiver). Stacks 111233x1, 111049x3, 45064x3, 111039x3, one to choose. A " +
                "marker at Tiernan.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own Kardash Eloh Obelisk on the escape pod's deck, while the mission runs.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Approach }
            };
            scene.Actors[ObeliskRole] = new SceneActorDefinition(ObeliskRole, SceneActorKind.Object, ObeliskClass, ObeliskAt,
                Orientation: ObeliskOrientation, InitiallyInteractable: false);
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{ObeliskRole}", ObeliskRole) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
