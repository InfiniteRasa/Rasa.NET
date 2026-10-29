using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Giant Killer (939): Goliath, "a giant Kael" (TaRapedia), is 1.5 times the size of his model
    /// (creature_scale). His class, Bane_Kael_Standard_Boss (25413), has the same mesh as his
    /// guards' Bane_Kael_Standard (4046), mesh 14434 (client entityclass), so he stood as tall as
    /// they did (user playtest 2026-10-22). No source gives his retail size: 1.5 is an ESTIMATE,
    /// chosen by the user.
    ///
    /// Down deletes the row.
    /// </summary>
    public static class PlateauGiantKillerScaleV1
    {
        public const uint Goliath = PlateauGiantKillerV1.Goliath;
        public const double Scale = 1.5;

        public static void Up(MigrationBuilder migration) =>
            migration.Sql($"INSERT INTO {CreatureScaleEntry.TableName} (id, scale) " +
                          $"VALUES ({Goliath}, {Scale.ToString("0.0#", CultureInfo.InvariantCulture)});");

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"DELETE FROM {CreatureScaleEntry.TableName} WHERE id = {Goliath};");
    }
}
