using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 1, the lead-ins: Sergeant Bernard Ames, Lt. Colonel Trent Ivan, Lt. Colonel Dania Caldwell and
    /// Captain Akio Yamamoto, and the upstream rows set 1 uses moved out of their placeholder spots (Colonel Mason Linier,
    /// Captain Marcus Emmons, Captain Daris Linier) (AshenLeadInNpcsV1), with Ames' The Dustiest Place on Earth (1250,
    /// AshenDustiestPlaceV1), Harris' Report to Stonewall Ridge (1273, AshenReportToStonewallV1), Emmons' Fithik Infestation
    /// (1274, AshenFithikInfestationV1), Ivan's Good Day to Die (1283, AshenGoodDayToDieV1) and Caldwell's You Can't Drink Ash
    /// (1284, AshenCantDrinkAshV1). Data only, plain SQL (CrucibleSql).
    /// </summary>
    public partial class Ashen_lead_ins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenLeadInNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenDustiestPlaceV1.Up(migrationBuilder);
            AshenReportToStonewallV1.Up(migrationBuilder);
            AshenFithikInfestationV1.Up(migrationBuilder);
            AshenGoodDayToDieV1.Up(migrationBuilder);
            AshenCantDrinkAshV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenCantDrinkAshV1.Down(migrationBuilder);
            AshenGoodDayToDieV1.Down(migrationBuilder);
            AshenFithikInfestationV1.Down(migrationBuilder);
            AshenReportToStonewallV1.Down(migrationBuilder);
            AshenDustiestPlaceV1.Down(migrationBuilder);
            foreach (var sql in AshenLeadInNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
