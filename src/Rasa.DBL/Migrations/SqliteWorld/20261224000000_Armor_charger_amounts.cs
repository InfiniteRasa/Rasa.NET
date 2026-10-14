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
    /// It was 20261223000000, the timestamp Random_name_quotes has. A World that ran it under
    /// that id runs it again under this one, which the guard on the old values leaves as it is,
    /// and its history row for the old id is removed.
    ///
    /// Down puts the client data's numbers back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261224000000_Armor_charger_amounts")]
    public class Armor_charger_amounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ArmorChargerAmounts.Up)
                migrationBuilder.Sql(sql);

            migrationBuilder.Sql(ArmorChargerAmounts.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ArmorChargerAmounts.Down)
                migrationBuilder.Sql(sql);
        }
    }
}
