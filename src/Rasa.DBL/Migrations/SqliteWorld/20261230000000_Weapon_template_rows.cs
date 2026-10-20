using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Every template of a weapon class is given an itemtemplate_weapon row (WeaponTemplateRows):
    /// 2674 had none, and the client raises on the first shot or reload of a weapon without one,
    /// the Cryogenic Shotgun among them. Each copies the row of another template of its class, or of
    /// a class with the same attack, with its own melee damage. Data only, in plain SQL: no model
    /// of its own.
    ///
    /// A template that already has a row keeps it. Down deletes the rows of the templates the
    /// seed left without one.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261230000000_Weapon_template_rows")]
    public class Weapon_template_rows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WeaponTemplateRows.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WeaponTemplateRows.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
