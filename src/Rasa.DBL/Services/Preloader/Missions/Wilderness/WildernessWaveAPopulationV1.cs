using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessWaveAPopulationV1
    {
        public static void Up(MigrationBuilder migration)
        {
            Creature(migration, 630010, 37, 26833, 1, 5, 800, 9519, 6, 2, "Ranger Milpas (reconstructed)");
            Spawn(migration, 630010, 630010, 778, 303.32, 127, 0.581905118);

            Creature(migration, 630040, 88, 10240, 0, 7, 900, 0, 4, 2, "Pinhole Miasma Egg-Layer (reconstructed)");
            Spawn(migration, 630040, 630040, 330, 203.8, 526);
            Spawn(migration, 630041, 630040, 338.5, 213.1, 552);
            Spawn(migration, 630042, 630040, 364, 212, 535);

            Creature(migration, 630043, 110, 6038, 1, 7, 900, 0, 4, 2, "Non-aggressive Treeback herd (reconstructed)");
            Spawn(migration, 630043, 630043, 450, 288.49, 584);
            Spawn(migration, 630044, 630043, 473, 288.54892, 585);
            Spawn(migration, 630045, 630043, 460, 288.54892, 601);

            Creature(migration, 630070, 110, 6340, 1, 7, 1000, 3097, 6, 2, "Sgt. Pierre (reconstructed)");
            Spawn(migration, 630070, 630070, -279, 170.1, 87, -1.42855361);
            migration.InsertData("creature_appearance", new[] { "id", "slot_id", "Class_id", "color" },
                new object[,]
                {
                    { 630070U, 1U, 26677U, 4286886614U },
                    { 630070U, 2U, 4021U, 4294934528U },
                    { 630070U, 3U, 26673U, 22120U },
                    { 630070U, 15U, 4023U, 4294934528U },
                    { 630070U, 16U, 4022U, 4294934528U },
                    { 630070U, 17U, 24008U, 4286690539U }
                });

            Creature(migration, 630071, 76, 7120, 0, 7, 600, 0, 9, 5, "Wilderness Lightbender (reconstructed)");
            migration.Sql("INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                "SELECT 630071, slot_id, Class_id, color FROM creature_appearance WHERE id = 76;");
            Spawn(migration, 630071, 630071, -236, 170.962898, 92);
            Spawn(migration, 630072, 630071, -245, 170.627199, 115);
            Spawn(migration, 630073, 630071, -240, 171.359632, 57);
            Spawn(migration, 630074, 630071, -300, 171.359632, 102);

            foreach (var id in new uint[] { 630076, 630077, 630078, 630079 })
                Creature(migration, id, 9, 7482, 0, 6, 750, 0, 0, 0, "Wilderness Bane mortar (reconstructed)");
            Spawn(migration, 630076, 630076, 360.8660888671875, 218.5, 95.262939453125);
            Spawn(migration, 630077, 630077, 212.7183837890625, 227.7, 286.189453125);
            Spawn(migration, 630078, 630078, 185.026123046875, 238.3, 399.9229736328125);
            Spawn(migration, 630079, 630079, 99.7130355834961, 232.5, 551.562255859375);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new uint[]
            {
                630010, 630040, 630041, 630042, 630043, 630044, 630045,
                630070, 630071, 630072, 630073, 630074, 630076, 630077, 630078, 630079
            })
                migration.DeleteData("spawnpool", "id", id);
            foreach (var id in new uint[] { 630070, 630071 })
                migration.Sql($"DELETE FROM creature_appearance WHERE id = {id};");
            foreach (var id in new uint[] { 630010, 630040, 630043, 630070, 630071, 630076, 630077, 630078, 630079 })
                migration.DeleteData("creature", "id", id);
        }

        internal static void Creature(MigrationBuilder migration, uint id, uint source, uint classId,
            uint faction, uint level, uint hitPoints, uint name, uint runSpeed, uint walkSpeed, string comment) =>
            migration.Sql(
                "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, " +
                "run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"SELECT {id}, '{comment}', {classId}, {faction}, {level}, {hitPoints}, {name}, {runSpeed}, {walkSpeed}, " +
                $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {source};");

        internal static void Spawn(MigrationBuilder migration, uint id, uint creature,
            double x, double y, double z, double orientation = 0) =>
            migration.InsertData("spawnpool",
                new[]
                {
                    "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
                    "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
                    "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
                    "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
                    "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
                    "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
                    "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
                },
                new object[]
                {
                    id, (byte)0, (byte)0, 20U, x, y, z, orientation, 1220U, creature, (byte)1, (byte)1,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0
                });
    }
}
