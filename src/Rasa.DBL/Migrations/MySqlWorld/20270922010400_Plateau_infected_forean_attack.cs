using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Plateau's Infected Forean attacks with the Forean lightning its model animates, not the goo gun's
    /// shot (PlateauInfectedForeanAttackV1). Data only.
    /// </summary>
    public partial class Plateau_infected_forean_attack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauInfectedForeanAttackV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauInfectedForeanAttackV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
