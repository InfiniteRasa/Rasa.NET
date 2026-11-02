using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ember's, the Quicksilver Magmonix', the Pacified Thrax' and Queen Kardash's attacks put on
    /// animations their models have (AttacksOnTheirModels). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down puts the old ones back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271021000000_Attacks_on_their_models")]
    public class Attacks_on_their_models : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttacksOnTheirModels.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttacksOnTheirModels.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
