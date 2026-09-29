using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Bead times by weapon family (WeaponBeadTiers): itemtemplate_weapon.aim_rate, which was the
    /// placeholder 1 on all 2440 rows - a full standing bead in 80 ms, the same for every weapon.
    /// Fast 0.08 (pistols, net guns, injection guns), Medium 0.04 (rifles, polarity guns,
    /// Torqueshell rifles), Slow 0.026667 (machine guns, leech guns), Very Slow 0.02 (launchers),
    /// None 100 (shotguns, propellant guns, blades, staves). Like Retune_weapon_heat, only rows
    /// still holding the placeholder are touched, so a row corrected by hand is left alone; Down
    /// puts back the placeholder on the rows still holding what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Retune_weapon_bead : Migration
    {
        private const double Placeholder = 1;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (rate, templates) in WeaponBeadTiers.Templates)
                migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set aim_rate = {Format(rate)} where aim_rate = {Format(Placeholder)} and id in ({string.Join(", ", templates)});");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (rate, templates) in WeaponBeadTiers.Templates)
                migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set aim_rate = {Format(Placeholder)} where aim_rate = {Format(rate)} and id in ({string.Join(", ", templates)});");
        }

        private static string Format(double value) => value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
    }
}
