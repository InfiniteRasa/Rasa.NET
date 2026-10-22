using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Giant Killer (939): Goliath's Kael walk 5 m to each side of him, the first rank 2.5 m and
    /// the second 7.5 m behind (spawnpool_patrol_group.spacing; the game server's
    /// PatrolGroups.AbreastOf), not the default 3 m: at 3 m the big models walked too close
    /// together (user playtest 2026-10-22). 5 m is an ESTIMATE chosen by the user; it fits inside
    /// the 5.5 m the beat keeps from a wall on each side (PlateauGiantKillerPatrolV1).
    ///
    /// Sets the column on the row PlateauGiantKillerPaceV1 inserted; Down sets it back to null.
    /// </summary>
    public static class PlateauGiantKillerSpacingV1
    {
        public const uint Pool = PlateauGiantKillerV1.GoliathPool;
        public const double Spacing = 5.0;

        public static void Up(MigrationBuilder migration) =>
            migration.Sql($"UPDATE {SpawnPoolPatrolGroupEntry.TableName} " +
                          $"SET spacing = {Spacing.ToString("0.0#", CultureInfo.InvariantCulture)} WHERE id = {Pool};");

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"UPDATE {SpawnPoolPatrolGroupEntry.TableName} SET spacing = NULL WHERE id = {Pool};");
    }
}
