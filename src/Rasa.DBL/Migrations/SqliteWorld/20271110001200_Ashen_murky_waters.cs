using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 12, Murky Waters' two sides: two Lasher pools on the ridge west of White Oasis Post
    /// (AshenMurkyWatersNpcsV1), Water Purification (1299, the repair, AshenWaterPurificationV1), Neurotoxins (1300, the
    /// poison, AshenNeurotoxinsV1) and Poisoning the Wellwater (1302, AshenPoisoningTheWellwaterV1).
    /// </summary>
    public partial class Ashen_murky_waters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenMurkyWatersNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenWaterPurificationV1.Up(migrationBuilder);
            AshenNeurotoxinsV1.Up(migrationBuilder);
            AshenPoisoningTheWellwaterV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenPoisoningTheWellwaterV1.Down(migrationBuilder);
            AshenNeurotoxinsV1.Down(migrationBuilder);
            AshenWaterPurificationV1.Down(migrationBuilder);
            foreach (var sql in AshenMurkyWatersNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
