using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 15, Indra Caverns (squad instance 2055) and its Brann facility: Major Darkua Diren, Sergeant Jonas
    /// Drof, Scout Hansa Naimi, Thrax Overseer Kuutk and the Indra Watch Post's two Bane pools new (AshenIndraFacilityNpcsV1),
    /// with Indra Caverns (1559, AshenIndraCavernsV1), Threshold (1360, AshenThresholdV1) and Vindication (1395,
    /// AshenVindicationV1). Sorts after set 14 (20271110001400).
    /// </summary>
    public partial class Ashen_indra_caverns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenIndraFacilityNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenIndraCavernsV1.Up(migrationBuilder);
            AshenThresholdV1.Up(migrationBuilder);
            AshenVindicationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenVindicationV1.Down(migrationBuilder);
            AshenThresholdV1.Down(migrationBuilder);
            AshenIndraCavernsV1.Down(migrationBuilder);
            foreach (var sql in AshenIndraFacilityNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
