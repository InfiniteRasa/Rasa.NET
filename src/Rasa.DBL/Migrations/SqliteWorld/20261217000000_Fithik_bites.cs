using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Fithik's own bite, Weapon_Creature_Fithik's 174/34, for the five region Fithiks that
    /// had only their lunge and acid spray, and the Hive Monarch's and Hive Master's bite (row 44)
    /// brought from 10 m to the client's 3 m (FithikBites). Data only, in plain SQL: no model of
    /// its own, and no table changes.
    ///
    /// Down takes the bites away and gives row 44 its 10 m back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261217000000_Fithik_bites")]
    public class Fithik_bites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in FithikBites.Up())
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in FithikBites.Down())
                migrationBuilder.Sql(statement);
        }
    }
}
