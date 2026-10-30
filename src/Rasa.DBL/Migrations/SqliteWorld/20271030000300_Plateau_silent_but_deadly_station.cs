using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Silent But Deadly (1187) to the Ten Ton Hammer guide's places and the station's door
    /// (PlateauSilentButDeadlyV1, PlateauSilentButDeadlyStationV1): the generator at the guide's marker, the Bane
    /// computer on the station's west side, the security detail with Taskmaster Yvik in the south-east room, and a
    /// sealed field in each of the station's two service entrances that the blown generator takes down. The mission
    /// is written again (its rows hold the old places), and with it the yard fields' take-downs on objective 4
    /// (PlateauSilentButDeadlyFieldsV1), which go with the mission's rows. Data only.
    /// </summary>
    public partial class Plateau_silent_but_deadly_station : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSilentButDeadlyFieldsV1.Down())
                migrationBuilder.Sql(sql);
            PlateauSilentButDeadlyV1.Down(migrationBuilder);
            PlateauSilentButDeadlyV1.Up(migrationBuilder);
            foreach (var sql in PlateauSilentButDeadlyFieldsV1.Up())
                migrationBuilder.Sql(sql);
            foreach (var sql in PlateauSilentButDeadlyStationV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSilentButDeadlyStationV1.Down())
                migrationBuilder.Sql(sql);
            // The mission keeps the new places; its detail's first actor (Yvik, gone with the rows above) is plain security.
            migrationBuilder.Sql(PlateauSilentButDeadlyStationV1.DetailWithoutYvikSql());
        }
    }
}
