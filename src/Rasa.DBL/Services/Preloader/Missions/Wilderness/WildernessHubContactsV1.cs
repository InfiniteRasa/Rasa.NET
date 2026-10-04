using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessHubContactsV1
    {
        private static readonly uint[] HumanContacts =
        {
            97, 103, 104, 115, 120, 125, 126, 127, 130, 132, 133, 134, 135, 138
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var creatureId in HumanContacts)
            {
                migration.UpdateData("creature", "id", creatureId, "class_id", 3846U);
                migration.Sql(
                    $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                    $"SELECT {creatureId}, slot_id, Class_id, color FROM creature_appearance WHERE id = 510006;");
            }

            migration.UpdateData("npc_package", "id", 100U, "package_id", 116U);
            migration.InsertData("npc_package", new[] { "id", "package_id", "comment" },
                new object[,]
                {
                    { 103U, 254U, "Arms Supplier Oliver" },
                    { 114U, 1646U, "Council Elder Quillas" },
                    { 115U, 382U, "Engineer Salter - Alia Das" },
                    { 116U, 210U, "Information Specialist Saviours" },
                    { 121U, 253U, "Lieutenant Wood" },
                    { 125U, 218U, "Medical Assistant Duncan" },
                    { 126U, 213U, "Mining Coordinator Richards" },
                    { 127U, 214U, "Mining Technician Maxwell" },
                    { 130U, 212U, "Outpost Commander Randolph" },
                    { 132U, 133U, "Quartermaster Caufield" },
                    { 134U, 2049U, "Receptive Liaison Standley" },
                    { 135U, 211U, "Recon Specialist Jennings" },
                    { 138U, 251U, "Surveyor Hugh Corman" },
                    { 139U, 252U, "Tribal Leader Oingin" },
                    { 510006U, 2588U, "Training Officer Kincaid" }
                });

            migration.UpdateData("spawnpool", "id", 101U,
                new[] { "creature_1_min_count", "creature_1_max_count" },
                new object[] { (byte)1, (byte)1 });
            migration.UpdateData("creature", "id", 101U,
                new[] { "run_speed", "walk_speed" }, new object[] { 0U, 0U });
            Move(migration, 101, 505, 238.757, 223, 0.8);
            Move(migration, 194, 493.1, 238.11785, 204.8, 4.53);
            Move(migration, 211, 864.7, 294.31787, 390, 1.8);
            Move(migration, 192, 783, 303.317277, 130, 1.030376827);

            migration.UpdateData("creature", "id", 510006U,
                new[] { "name_id", "comment" },
                new object[] { 10604U, "Training Officer Kincaid - Alia Das" });
            Move(migration, 510006, 774.5, 294.05008, 393, 0);
        }

        public static void Down(MigrationBuilder migration)
        {
            Move(migration, 510006, 810.9, 294.47, 384.7, 0);
            migration.UpdateData("creature", "id", 510006U,
                new[] { "name_id", "comment" },
                new object[] { 6932U, "Sapper Trainer Kincaid - Alia Das" });
            Move(migration, 211, 870.95703, 294.21094, 385.3711, 1.8);
            Move(migration, 192, 791.0117, 298.53516, 111.98578, 3.17);
            Move(migration, 194, -757.6211, 175.03516, -277.9297, 4.53);
            Move(migration, 101, 870, 294.21, 388, 0.8);
            migration.UpdateData("creature", "id", 101U,
                new[] { "run_speed", "walk_speed" }, new object[] { 9U, 5U });
            migration.UpdateData("spawnpool", "id", 101U,
                new[] { "creature_1_min_count", "creature_1_max_count" },
                new object[] { (byte)0, (byte)0 });

            foreach (var creatureId in new uint[]
            {
                103, 114, 115, 116, 121, 125, 126, 127, 130, 132, 134, 135, 138, 139, 510006
            })
                migration.DeleteData("npc_package", "id", creatureId);
            migration.UpdateData("npc_package", "id", 100U, "package_id", 726U);

            foreach (var creatureId in HumanContacts)
            {
                migration.Sql($"DELETE FROM creature_appearance WHERE id = {creatureId};");
                migration.UpdateData("creature", "id", creatureId, "class_id", 29423U);
            }
        }

        private static void Move(MigrationBuilder migration, uint spawnId,
            double x, double y, double z, double orientation) =>
            migration.UpdateData("spawnpool", "id", spawnId,
                new[] { "pos_x", "pos_y", "pos_z", "rotation" },
                new object[] { x, y, z, orientation });
    }
}
