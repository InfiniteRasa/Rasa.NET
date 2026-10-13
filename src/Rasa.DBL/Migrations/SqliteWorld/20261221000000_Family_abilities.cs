using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Spawned creatures given the CR_* family abilities their kin have and they lacked
    /// (FamilyAbilities): creature_action 76001-76058 and the slots that point at them or at the
    /// kin's own rows. Data only, in plain SQL: no model of its own.
    ///
    /// Down empties the slots, puts the replaced rows back, and deletes 76001-76058.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261221000000_Family_abilities")]
    public class Family_abilities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FamilyAbilities.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FamilyAbilities.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
