using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Giant Killer (939): Goliath's group walks its beat (PlateauGiantKillerPatrolV1) at 2 m/s, a
    /// little over the default stroll of 1.6 m/s (the game server's Patrols.Pace): the user's choice
    /// after a playtest (2026-10-22), where the group jogged it at the Kael's walk_speed of 5. One way
    /// takes about 2 min 10 s. Its other settings are the defaults: Goliath leads, no acting leader,
    /// and he comes back with the pool's top-up.
    ///
    /// Down deletes the row.
    /// </summary>
    public static class PlateauGiantKillerPaceV1
    {
        public const uint Pool = PlateauGiantKillerV1.GoliathPool;
        public const double Pace = 2.0;

        public static void Up(MigrationBuilder migration) =>
            migration.Sql($"INSERT INTO {SpawnPoolPatrolGroupEntry.TableName} (id, acting_leader, leader_top_up, pace) " +
                          $"VALUES ({Pool}, 0, 1, {Pace.ToString("0.0#", CultureInfo.InvariantCulture)});");

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"DELETE FROM {SpawnPoolPatrolGroupEntry.TableName} WHERE id = {Pool};");
    }
}
