using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Each Incommunicado (1040) delivery marks its commander: delivering his communicator sets a
    /// mission flag of his to 1, and his own mission is offered from then on - Carvelle's Canyon
    /// Sweep (1045), Petrie's Blueprint For Victory (1046), Locke's Toxic Detox (1053). TaRapedia
    /// gives each of them the requirement "Incommunicado (one objective)" (ESTIMATE: read as that
    /// commander's delivery). Flags are kept on the character, so a delivery counts even when
    /// Incommunicado is abandoned later. A delivery made before this migration set no flag.
    ///
    /// Action 3 of each delivery objective (1 takes the communicator, 2 on objective 1 pays the
    /// reward). Down takes out only these rows.
    /// </summary>
    public static class PlateauIncommunicadoFlagsV1
    {
        public const uint CarvelleFlag = 1040001, PetrieFlag = 1040002, LockeFlag = 1040003;

        private static readonly (uint Objective, uint Flag, string Commander)[] Deliveries =
        {
            (1, CarvelleFlag, "Carvelle"),
            (2, PetrieFlag, "Petrie"),
            (3, LockeFlag, "Locke"),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = PlateauMissionDataV1.Existing(migration, PlateauIncommunicadoV1.MissionId);
            foreach (var delivery in Deliveries)
                mission.Action(delivery.Objective, 3, MissionActionKind.SetPlayerFlag,
                    playerFlag: delivery.Flag, playerFlagValue: 1,
                    comment: $"Mark {delivery.Commander}'s communicator delivered");
        }

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"DELETE FROM mission_action WHERE mission_id = {PlateauIncommunicadoV1.MissionId} " +
                          $"AND content_revision = '{PlateauMissionDataV1.Revision}' AND action_id = 3;");
    }
}
