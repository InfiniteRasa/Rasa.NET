using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The control point Prestige Vendors sell the Attribute Respec Token for 999 prestige, the
    /// Skill Respec Token for 500 and the Experience Booster for 150, in place of the Pistol they
    /// were seeded with (CpPrestigeStock). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the Pistol back and the three templates' buy_price back to 1.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270101000000_Cp_prestige_stock")]
    public class Cp_prestige_stock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CpPrestigeStock.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CpPrestigeStock.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
