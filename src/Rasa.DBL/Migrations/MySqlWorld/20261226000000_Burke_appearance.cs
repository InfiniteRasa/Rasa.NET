using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Lieutenant Burke (creature 96) is given a head and clothes: the AFS officer's outfit the
    /// other lieutenants on his body wear, six creature_appearance rows (BurkeAppearance). He had
    /// none, and his swapset body stood with no head. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows away again, each only as it was put in.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261226000000_Burke_appearance")]
    public class Burke_appearance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurkeAppearance.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurkeAppearance.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
