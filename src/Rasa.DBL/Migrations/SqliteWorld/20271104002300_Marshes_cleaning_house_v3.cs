using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 16: Cleaning House's survivors as their own creature rows (MarshesCleaningHouseSurvivorsV1)
    /// and each camp talk spoken by its own NPC (MarshesCleaningHouseV3 in place of MarshesCleaningHouseV2). Down writes V2
    /// again and takes the rows out. Data only.
    /// </summary>
    public partial class Marshes_cleaning_house_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesCleaningHouseSurvivorsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesCleaningHouseV2.Down(migrationBuilder);
            MarshesCleaningHouseV3.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesCleaningHouseV3.Down(migrationBuilder);
            MarshesCleaningHouseV2.Up(migrationBuilder);
            foreach (var sql in MarshesCleaningHouseSurvivorsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
