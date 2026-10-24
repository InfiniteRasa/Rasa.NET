using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Temporal Chamber's last two missions: Foreas Base in the past (PlateauForeasBasePastNpcsV1: Colonel Fonseca,
    /// the scene's templates, the teleporter pair on Veryin's left); Foreas Base Past (1178, PlateauForeasBasePastV1) and
    /// Send a Message (1181, PlateauSendAMessageV1). Data only.
    /// </summary>
    public partial class Plateau_temporal_past : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauForeasBasePastNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauForeasBasePastV1.Up(migrationBuilder);
            PlateauSendAMessageV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauSendAMessageV1.Down(migrationBuilder);
            PlateauForeasBasePastV1.Down(migrationBuilder);
            foreach (var sql in PlateauForeasBasePastNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
