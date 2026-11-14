using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 16, Indra Caverns (squad instance 2055) and the Gotha Processing Plant: Sergeant Jonas Drof's and
    /// Scout Hansa Naimi's second rows by the plant's entrance and Overlord Kretas new (AshenGothaPlantNpcsV1), with Digging for
    /// the Truth (1447, AshenDiggingForTheTruthV1), Meet Sergeant Drof (1532, AshenMeetSergeantDrofV1), Subversion (1451,
    /// AshenSubversionV1) and Desistance (1452, AshenDesistanceV1). Sorts after set 15 (20271110001500).
    /// </summary>
    public partial class Ashen_gotha_plant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenGothaPlantNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenDiggingForTheTruthV1.Up(migrationBuilder);
            AshenMeetSergeantDrofV1.Up(migrationBuilder);
            AshenSubversionV1.Up(migrationBuilder);
            AshenDesistanceV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenDesistanceV1.Down(migrationBuilder);
            AshenSubversionV1.Down(migrationBuilder);
            AshenMeetSergeantDrofV1.Down(migrationBuilder);
            AshenDiggingForTheTruthV1.Down(migrationBuilder);
            foreach (var sql in AshenGothaPlantNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
