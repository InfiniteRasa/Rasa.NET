using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Armor Charger's Class VI and VII restore what their item text says, 4400 and 6800
    /// armour, in place of Class VII's and Class VIII's (ArmorChargerAmounts). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down puts the client data's numbers back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261223000000_Armor_charger_amounts")]
    public class Armor_charger_amounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ArmorChargerAmounts.Up)
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ArmorChargerAmounts.Down)
                migrationBuilder.Sql(sql);
        }
    }
}
