using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ustor Yard's door, Field Lieutenant Rapp and the yard's Bane (PlateauUstorYardV1), and her three missions:
    /// Silent But Deadly (1187, PlateauSilentButDeadlyV1), Interception! (1189, PlateauInterceptionV1) and Monkey
    /// Wrench (1188, PlateauMonkeyWrenchV1). Data only.
    /// </summary>
    public partial class Plateau_ustor_yard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauUstorYardV1.Up())
                migrationBuilder.Sql(sql);
            PlateauSilentButDeadlyV1.Up(migrationBuilder);
            PlateauInterceptionV1.Up(migrationBuilder);
            PlateauMonkeyWrenchV1.Up(migrationBuilder);
            foreach (var sql in PlateauSilentButDeadlyFieldsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSilentButDeadlyFieldsV1.Down())
                migrationBuilder.Sql(sql);
            PlateauMonkeyWrenchV1.Down(migrationBuilder);
            PlateauInterceptionV1.Down(migrationBuilder);
            PlateauSilentButDeadlyV1.Down(migrationBuilder);
            foreach (var sql in PlateauUstorYardV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
