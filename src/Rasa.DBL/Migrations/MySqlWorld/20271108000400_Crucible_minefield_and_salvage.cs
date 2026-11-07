using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 4, the minefield and the salvage: the Bane Smart Mines west of the Fort Intrepid ruins, the
    /// Reconstructor Bots by the Chaukas Robotics Facility and Executioner Derge moved onto the Fort Intrepid rampart
    /// (CrucibleMinefieldAndSalvageNpcsV1), with Private Drake's Minesweeper (1232, CrucibleMinesweeperV1), Captain DeLisi's
    /// Funeral Derge (1241, CrucibleFuneralDergeV1) and Labbna Sigal Haggus' Salvage Duty and More Salvage Duty (1203, 1204,
    /// CrucibleSalvageDutyV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_minefield_and_salvage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleMinefieldAndSalvageNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleMinesweeperV1.Up(migrationBuilder);
            CrucibleFuneralDergeV1.Up(migrationBuilder);
            CrucibleSalvageDutyV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleSalvageDutyV1.Down(migrationBuilder);
            CrucibleFuneralDergeV1.Down(migrationBuilder);
            CrucibleMinesweeperV1.Down(migrationBuilder);
            foreach (var sql in CrucibleMinefieldAndSalvageNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
