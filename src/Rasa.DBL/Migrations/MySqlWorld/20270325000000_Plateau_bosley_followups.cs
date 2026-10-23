using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Colonel Bosley's follow-ups to One Way Trip and Maligo's last radio mission: Colonel "Snake" Washington at the Snake
    /// Pit (PlateauSnakePitV1); South Of The Border (1068, PlateauSouthOfTheBorderV1), New Orders From The General (1541,
    /// PlateauNewOrdersFromTheGeneralV1) and Self Preservation (879, PlateauSelfPreservationV1). Data only.
    /// </summary>
    public partial class Plateau_bosley_followups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSnakePitV1.Up())
                migrationBuilder.Sql(sql);
            PlateauSouthOfTheBorderV1.Up(migrationBuilder);
            PlateauNewOrdersFromTheGeneralV1.Up(migrationBuilder);
            PlateauSelfPreservationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauSelfPreservationV1.Down(migrationBuilder);
            PlateauNewOrdersFromTheGeneralV1.Down(migrationBuilder);
            PlateauSouthOfTheBorderV1.Down(migrationBuilder);
            foreach (var sql in PlateauSnakePitV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
