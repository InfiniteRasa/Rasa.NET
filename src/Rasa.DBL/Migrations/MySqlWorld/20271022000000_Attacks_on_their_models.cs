using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ember's, the Quicksilver Magmonix', the Pacified Thrax' and Queen Kardash's attacks put on
    /// animations their models have (AttacksOnTheirModels). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// It was 20271021000000, the timestamp Instance_creature_kits has. A World that ran it under
    /// that id runs it again under this one, which its guards leave as it is, and its history row
    /// for the old id is removed.
    ///
    /// Down puts the old ones back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271022000000_Attacks_on_their_models")]
    public class Attacks_on_their_models : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttacksOnTheirModels.Up())
                migrationBuilder.Sql(sql);

            migrationBuilder.Sql(AttacksOnTheirModels.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttacksOnTheirModels.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
