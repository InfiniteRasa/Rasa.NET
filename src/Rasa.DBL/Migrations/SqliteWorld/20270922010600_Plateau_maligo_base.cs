using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Maligo Base and the Ustor Bridge (PlateauMaligoBaseV1): the NPCs, the keypass officers, the links from Ustor
    /// Yard's west exit and the local teleporters; and Showdown (876, PlateauShowdownV1), Coup (878, PlateauCoupV1)
    /// and One Way Trip (877, PlateauOneWayTripV1). Data only.
    /// </summary>
    public partial class Plateau_maligo_base : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauMaligoBaseV1.Up())
                migrationBuilder.Sql(sql);
            PlateauShowdownV1.Up(migrationBuilder);
            PlateauCoupV1.Up(migrationBuilder);
            PlateauOneWayTripV1.Up(migrationBuilder);
            foreach (var sql in PlateauContactsOutOfAggroV1.Up())
                migrationBuilder.Sql(sql);
            foreach (var sql in PlateauMaligoOfficerRespawnV1.Up())
                migrationBuilder.Sql(sql);
            PlateauMaligoOfficerSquadRespawnV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMaligoOfficerSquadRespawnV1.Down(migrationBuilder);
            foreach (var sql in PlateauMaligoOfficerRespawnV1.Down())
                migrationBuilder.Sql(sql);
            foreach (var sql in PlateauContactsOutOfAggroV1.Down())
                migrationBuilder.Sql(sql);
            PlateauOneWayTripV1.Down(migrationBuilder);
            PlateauCoupV1.Down(migrationBuilder);
            PlateauShowdownV1.Down(migrationBuilder);
            foreach (var sql in PlateauMaligoBaseV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
