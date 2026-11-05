using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Missing in Action (native mission 1863), Valverde Marshes: Retread Jeska at High Point Retreat sends the player to
    /// look for Lt. Koffman east of the camp; the player finds her body.
    ///
    /// Native: texts 19099-19103 (missionconversation: log "Investigate Lt. Koffman's last known whereabouts and report back to Retread Jeska.", offer "the AFS sent an officer to check out our lines of communication", turn-in "another brave soul falls to the marsh"); objective 1 "Locate Lt. Koffman to
    /// the east of High Point Retreat." 19365/19366, no counter (missionobjective); objective conversation 1863,1 package
    /// 2367 flag 1 COMPLETION text 19367 "You stare at the body of this fallen soldier, her dog tags clearly proclaiming that
    /// she is Lt. Koffman ..." (objectiveconversation). Name 10330 "Dead Retread Koffman" (creaturenamelanguage). Class
    /// 24072 UsableNPCHumFCorpseV01 (a female human corpse that talks, augmentation 52; mesh 29292).
    ///
    /// Sourced: TaRapedia Missing in Action: giver and receiver Retread Jeska (Stalker Woods); level 36; 65,000 XP, 5,400
    /// credits; one of 2 Class VII Advanced Med Pack, 2 Armor Charger, 4 Cryogenic Grenade, 4 Fragmentation Grenade.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Koffman a personal talking corpse (24072, the Bootcamp corpse's way, BootcampCorpseDialogueV6) with the client's
    ///    package; one talk completes 1. 60 m east of the waypoint the ground is a navmesh island (probe of 2026-11-02), so
    ///    she lies on the walked-to lowland 84 m east-south-east of it (-700, 217.39, -560).
    ///  - The rewards the client classes' lowest templates (925's and 1028's), one stack to choose. A marker at the body.
    /// </summary>
    public static class MarshesMissingInActionV1
    {
        public const uint MissionId = 1863;
        public const uint Level = 36;
        public const uint Jeska = MarshesStalkerWoodsNpcsV1.Jeska;
        public const uint Locate = 1;

        public const uint CorpseClass = 24072;          // UsableNPCHumFCorpseV01
        public const uint CorpsePackage = 2367;
        public const string CorpseRole = "koffman-body";
        public static readonly ScenePosition CorpseAt = new(-700f, 217.39f, -560f);
        public const double MarkerRadius = 10;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint MedPack = 45062, MedPacks = 2;                        // Class VII Advanced Med Pack
        public const uint ArmorCharger = 118898, ArmorChargers = 2;             // Class VII Armor Charger
        public const uint CryoGrenade = 111048, CryoGrenades = 4;               // Class VII Cryogenic Grenade
        public const uint FragGrenade = 45446, FragGrenades = 4;                // Class VII Fragmentation Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Missing in Action", 19099, Jeska, Jeska, Level);
            mission.Reward(Experience, Credits,
                new[] { (MedPack, MedPacks), (ArmorCharger, ArmorChargers), (CryoGrenade, CryoGrenades), (FragGrenade, FragGrenades) },
                new (uint, uint)[0]);

            mission.Objective(Locate, 19365, 19366, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Locate);
            mission.Conversation(Locate, CorpsePackage);
            mission.Action(Locate, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Locate, 1, CorpseAt.X, CorpseAt.Y, CorpseAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1863",
                "Native1 'Locate Lt. Koffman to the east of High Point Retreat.' 19365/19366, no counter; objectiveconversation " +
                "1863,1 package2367 flag1 completion text19367 (her dog tags). Name10330 Dead Retread Koffman. Corpse class " +
                "24072 UsableNPCHumFCorpseV01 (augmentation52).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Missing_in_Action",
                "Giver/receiver Retread Jeska (Stalker Woods); level36; 65000XP, 5400 credits; one of 2 Class VII Advanced Med " +
                "Pack, 2 Armor Charger, 4 Cryogenic Grenade, 4 Fragmentation Grenade; Koffman found dead.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesMissingInActionV1",
                "A personal talking corpse 24072 (package2367) on the walked-to lowland east of High Point Retreat " +
                "(-700,217.39,-560): 60 m east is a navmesh island. One talk completes 1. Items 45062x2, 118898x2, 111048x4, " +
                "45446x4. A marker at the body.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition> { new(Locate, CorpsePackage, 1, transitionId: 1) }
            };
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, CorpseAt,
                Orientation: 0, InitialObjectState: 0, Conversation: new SceneObjectConversation(MissionId, Locate, CorpsePackage, Locate));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{CorpseRole}", CorpseRole) } };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
