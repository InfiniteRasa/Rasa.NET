using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessAdditionalWorldV1
    {
        public static void Up(MigrationBuilder migration)
        {
            migration.InsertData("creature_action",
                new[] { "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage" },
                new object[,]
                {
                    { 630052U, "Light Bender Sniper (reconstructed range)", 1U, 149U, 0.5, 60.0, 800U, 0U, 15U, 25U },
                    { 630103U, "Forean Machina (native weapon profile)", 1U, 154U, 0.5, 40.0, 1200U, 0U, 20U, 30U }
                });
            WildernessWaveAPopulationV1.Creature(migration, 630046, 76, 7120, 0, 7, 600, 406, 9, 5,
                "Light Bender Sniper (reconstructed)");
            migration.UpdateData("creature", "id", 630046U, "action1", 630052U);
            migration.Sql("INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                "SELECT 630046, slot_id, Class_id, color FROM creature_appearance WHERE id = 76;");
            WildernessWaveAPopulationV1.Spawn(migration, 630046, 630046, 243.340721, 227.445074, 241.396381, -1.9331716839989537);
            WildernessWaveAPopulationV1.Spawn(migration, 630047, 630046, 255.207849, 230.380794, 263.403659, -1.593360459534133);
            WildernessWaveAPopulationV1.Spawn(migration, 630048, 630046, 345.332512, 230.320493, 177.36105, -1.7367993608386485);
            WildernessWaveAPopulationV1.Spawn(migration, 630049, 630046, 327.942408, 233.363417, 184.94948, -1.43718286305095);
            WildernessWaveAPopulationV1.Spawn(migration, 630050, 630046, 265.933766, 239.813626, 298.236076, -0.4228539207043388);
            WildernessWaveAPopulationV1.Spawn(migration, 630051, 630046, 259.940889, 230.19304, 246.037072, -2.018010821944118);

            WildernessWaveAPopulationV1.Creature(migration, 630100, 110, 6236, 0, 9, 900, 0, 9, 5,
                "Forean Machina (reconstructed population)");
            migration.UpdateData("creature", "id", 630100U, "action1", 630103U);
            migration.InsertData("creature_appearance", new[] { "id", "slot_id", "Class_id", "color" },
                new object[] { 630100U, 13U, 6019U, 1U });
            WildernessWaveAPopulationV1.Spawn(migration, 630100, 630100, -72, 216.892554, 120, 0.4092564);
            WildernessWaveAPopulationV1.Spawn(migration, 630101, 630100, -116, 216.282193, 100, -1.308520799);
            WildernessWaveAPopulationV1.Spawn(migration, 630102, 630100, -93, 209.812367, 48, -2.945533632);

            WildernessWaveAPopulationV1.Creature(migration, 630120, 520022, 3902, 0, 10, 1500, 0, 9, 5,
                "Wilderness Predator (reconstructed)");
            WildernessWaveAPopulationV1.Spawn(migration, 630120, 630120, -725, 278.9202, 695);
            WildernessWaveAPopulationV1.Spawn(migration, 630121, 630120, -700, 277.33453, 670);
            migration.UpdateData("spawnpool", "id", 510005U,
                new[] { "pos_x", "pos_y", "pos_z", "rotation" },
                new object[] { -698.0, 170.233, -345.0, -2.35619449 });
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.UpdateData("spawnpool", "id", 510005U,
                new[] { "pos_x", "pos_y", "pos_z", "rotation" },
                new object[] { -721.2, 227.06, -423.0, 0.0 });
            foreach (var id in new uint[]
            {
                630046, 630047, 630048, 630049, 630050, 630051,
                630100, 630101, 630102, 630120, 630121
            })
                migration.DeleteData("spawnpool", "id", id);
            foreach (var id in new uint[] { 630046, 630100 })
                migration.Sql($"DELETE FROM creature_appearance WHERE id = {id};");
            foreach (var id in new uint[] { 630046, 630100, 630120 })
                migration.DeleteData("creature", "id", id);
            foreach (var id in new uint[] { 630052, 630103 })
                migration.DeleteData("creature_action", "id", id);
        }
    }
}
