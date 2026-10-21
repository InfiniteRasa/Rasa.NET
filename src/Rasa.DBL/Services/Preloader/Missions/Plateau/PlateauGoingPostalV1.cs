using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Going Postal (native mission 1034), Plateau: Ranger Kaely at New Velon Village sends the player to
    /// Ranger Porthus, the courier from Palados, for a satchel of letters to the Velon Das refugees.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5776-5779 and 6362 (missionconversation);
    /// objectives 1 "Locate Ranger Porthus." 5785/6363 and 2 "Get mail satchel." 8093/8094, no counters
    /// (missionobjective); Porthus's line on package 1113 for objective 1 (5786: "This satchel contains
    /// letters to the people of Velon Das ...") (objectiveconversation, flag 1). The satchel is item class
    /// 22088 "Mail Satchel From Palados" (template 45008). Kaely is creature 510189, Porthus 510190.
    ///
    /// Sourced: "Rendevous with Ranger Porthus on the far side of Valverde Chasm, near Desolation Quarry"
    /// (client 5777); Desolation Quarry region marker 75 (373, 284, -282) (uimapmarker). TaRapedia Going
    /// Postal (stub): giver and receiver Kaely, level "30 (unconfirmed)", no requirement, 45,000 XP, 4,800
    /// credits, "Equipment"; "Porthus can be found on the cliff overlooking Valverde chasm."
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Porthus moves from the chasm's floor under Trinity Bridge (-96.6, 269.11, 179.7), where the NPC
    ///    seed put him for "Valverde Chasm", to the chasm's south rim 240 m north of the quarry marker
    ///    (460, 346.36, -60): the rim's floor on the client's terrain, the chasm floor 80 m below to the
    ///    north, on the navmesh reached from Trinity Bridge.
    ///  - Porthus hands over the satchel at objective 1; objective 2 completes with it (ObjectiveState);
    ///    Kaely keeps it at turn-in.
    ///  - "Equipment" read as the four Uncommon V04 boots of the 30-34 band, one of each armor type, no
    ///    module. Indicator on Porthus.
    /// </summary>
    public static class PlateauGoingPostalV1
    {
        public const uint MissionId = 1034;
        public const uint Level = 30;
        public const uint Kaely = 510189;
        public const uint Porthus = 510190, PorthusPackage = 1113;
        public const uint SatchelTemplate = 45008, SatchelClass = 22088;
        public const string SatchelKey = "mail-satchel";

        public static readonly (double X, double Y, double Z) PorthusAt = (460, 346.36, -60);
        public static readonly (double X, double Y, double Z) PorthusBefore = (-96.6, 269.11, 179.7);

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioBoots = 46003;             // Armor_T3_Bio_V04_UNC_Boots_30_to_34
        public const uint GravitonBoots = 24821;        // Armor_T3_Graviton_V04_UNC_Boots_30_to_34
        public const uint MechBoots = 33947;            // Armor_T3_Mech_V04_UNC_Boots_30_to_34
        public const uint StealthBoots = 40074;         // Armor_T3_Stealth_V04_UNC_Boots_30_to_34

        /// <summary>Porthus to the rim: his own pool, the only one he is in.</summary>
        public static IEnumerable<string> MovePorthus() => new[] { MoveSql(PorthusAt) };

        public static IEnumerable<string> RestorePorthus() => new[] { MoveSql(PorthusBefore) };

        private static string MoveSql((double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {PlateauCreatureSqlV1.F(at.X)}, pos_y = {PlateauCreatureSqlV1.F(at.Y)}, " +
            $"pos_z = {PlateauCreatureSqlV1.F(at.Z)} WHERE id = {Porthus};";

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in MovePorthus())
                migration.Sql(sql);

            var mission = new PlateauMissionDataV1(migration, MissionId, "Going Postal", 5776, Kaely, Kaely, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });

            mission.Objective(1, 5785, 6363, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 8093, 8094, 1, MissionObjectiveState.Incomplete);

            // 1: Porthus hands over the satchel.
            mission.Transition(1);
            mission.Conversation(1, PorthusPackage);
            mission.Action(1, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("porthus-hands-satchel", MissionId, SatchelKey, SatchelTemplate, 1));
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);

            // 2: the satchel in hand - it comes with objective 1 (a transition completes only its own objective).
            mission.Transition(2);
            mission.ObjectiveState(2, 1, MissionObjectiveState.Completed);

            mission.Indicator(1, 1, PorthusAt.X, PorthusAt.Y, PorthusAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1034",
                "Native1 'Locate Ranger Porthus' 5785/6363, 2 'Get mail satchel' 8093/8094, no counters; objectiveconversation " +
                "1034,1,1113 text5786. Texts 5776-5779, 6362. Satchel class22088/template45008. Desolation Quarry marker75 (373,284,-282).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Going_Postal",
                "Stub. Giver/receiver Ranger Kaely, New Velon Village; level '30 (unconfirmed)'; no requirement; 45000XP, 4800 " +
                "credits, 'Equipment'; Porthus on the cliff overlooking Valverde chasm, sometimes in combat.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauGoingPostalV1",
                "Porthus pool510190 moved from the chasm floor (-96.6,269.11,179.7) to the south rim (460,346.36,-60), 240m " +
                "north of the quarry. Objective1 conversation issues the satchel (taken at turn-in); 2 completes on 1. " +
                "Equipment read as Uncommon V04 boots 30-34: 46003, 24821, 33947, 40074.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding(SatchelKey, SatchelTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            });
        }

        public static void Down(MigrationBuilder migration)
        {
            PlateauMissionDataV1.Remove(migration, MissionId);
            foreach (var sql in RestorePorthus())
                migration.Sql(sql);
        }
    }
}
