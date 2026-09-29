using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessLateHubContactsV1
    {
        public static void Up(MigrationBuilder migration)
        {
            foreach (var id in new uint[] { 105, 129, 131 })
                Human(migration, id, 3846, 510006);
            foreach (var id in new uint[] { 123, 124, 128, 140 })
                Human(migration, id, 6339, 510005);
            migration.UpdateData("creature", "id", 123U,
                new[] { "run_speed", "walk_speed" }, new object[] { 6U, 2U });
            migration.UpdateData("creature", "id", 98U, "action1", 17U);
            migration.UpdateData("creature", "id", 99U, "name_id", 6711U);
            migration.InsertData("npc_package", new[] { "id", "package_id", "comment" },
                new object[,]
                {
                    { 91U, 567U, "Council Advisor Todae" },
                    { 92U, 566U, "Council Luminary Doyan" },
                    { 93U, 111U, "Council Elder Nula" },
                    { 94U, 117U, "Dr. Eleanor Corman" },
                    { 95U, 120U, "Dr. Samuel Corman - Wilderness" },
                    { 96U, 609U, "AFS Officer Burke" },
                    { 98U, 102U, "Ranger Anjuhi" },
                    { 99U, 570U, "Ranger Tirna" },
                    { 102U, 590U, "Forean Apprentice Juvak" },
                    { 105U, 504U, "Corporal Mendelson" },
                    { 106U, 227U, "Council Elder Baruhi" },
                    { 108U, 602U, "Dr. Gwen Duvall" },
                    { 113U, 577U, "Council Elder Gadfly" },
                    { 119U, 450U, "Lieutenant Commander Parsons" },
                    { 124U, 121U, "Medic Quincy Corman" },
                    { 129U, 415U, "Outpost Commander Taylor" },
                    { 131U, 219U, "Private Moore" },
                    { 510005U, 505U, "George Corman" }
                });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new uint[]
            {
                91, 92, 93, 94, 95, 96, 98, 99, 102, 105, 106, 108, 113, 119, 124, 129, 131, 510005
            })
                migration.DeleteData("npc_package", "id", id);
            migration.UpdateData("creature", "id", 99U, "name_id", 135U);
            migration.UpdateData("creature", "id", 98U, "action1", 0U);
            migration.UpdateData("creature", "id", 123U,
                new[] { "run_speed", "walk_speed" }, new object[] { 0U, 0U });
            foreach (var id in new uint[] { 105, 123, 124, 128, 129, 131, 140 })
            {
                migration.Sql($"DELETE FROM creature_appearance WHERE id = {id};");
                migration.UpdateData("creature", "id", id, "class_id", 29423U);
            }
        }

        private static void Human(MigrationBuilder migration, uint id, uint classId, uint appearanceSource)
        {
            migration.UpdateData("creature", "id", id, "class_id", classId);
            migration.Sql("INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {appearanceSource};");
        }
    }
}
