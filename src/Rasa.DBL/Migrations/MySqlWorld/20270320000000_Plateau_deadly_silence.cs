using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The corrupted leaders of the Order of Velon in Velon Hollow (PlateauVelonElderBossesV1) and Deadly
    /// Silence (mission 1974, PlateauDeadlySilenceV1). Data only.
    /// </summary>
    public partial class Plateau_deadly_silence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauVelonElderBossesV1.Up())
                migrationBuilder.Sql(sql);
            PlateauDeadlySilenceV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauDeadlySilenceV1.Down(migrationBuilder);
            foreach (var sql in PlateauVelonElderBossesV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
