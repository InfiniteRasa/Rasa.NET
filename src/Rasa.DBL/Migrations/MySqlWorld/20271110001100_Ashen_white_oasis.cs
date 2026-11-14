using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 11, White Oasis Post: Doctor Harold Meeks and Agent Hale new, Lieutenant Dania Fontecilla moved off
    /// the control point's console (AshenWhiteOasisNpcsV1), with Analyzing the Harvesters (1295, AshenAnalyzingHarvestersV1),
    /// Murky Waters (1298, AshenMurkyWatersV1), Prying Eyes (1303, AshenPryingEyesV1) and Gas Shortages (1356,
    /// AshenGasShortagesV1).
    /// </summary>
    public partial class Ashen_white_oasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenWhiteOasisNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenAnalyzingHarvestersV1.Up(migrationBuilder);
            AshenMurkyWatersV1.Up(migrationBuilder);
            AshenPryingEyesV1.Up(migrationBuilder);
            AshenGasShortagesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenGasShortagesV1.Down(migrationBuilder);
            AshenPryingEyesV1.Down(migrationBuilder);
            AshenMurkyWatersV1.Down(migrationBuilder);
            AshenAnalyzingHarvestersV1.Down(migrationBuilder);
            foreach (var sql in AshenWhiteOasisNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
