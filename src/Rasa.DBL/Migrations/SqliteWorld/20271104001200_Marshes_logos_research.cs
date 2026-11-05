using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 13, Logos Research: Specialist Hax, Captain Sakai, the newbie medic, the Thrax Commander, Larvax,
    /// Xynnr, Mesgel, the prisoners, Russell and Brock in the Logos Research Facility, Jumna and the lake's teleporters on the
    /// Marshes (MarshesLogosResearchNpcsV1), with A Taste of Their Own Medicine (546, MarshesTasteOfTheirOwnMedicineV1),
    /// Freedom's Just Another Word (548, MarshesFreedomsJustAnotherWordV1), Leggo My Logos (550, MarshesLeggoMyLogosV1), Slay
    /// on Target (718, MarshesSlayOnTargetV1) and To a Subterranean Chamber (1674, MarshesSubterraneanChamberV1). Data only.
    /// </summary>
    public partial class Marshes_logos_research : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesLogosResearchNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesTasteOfTheirOwnMedicineV1.Up(migrationBuilder);
            MarshesFreedomsJustAnotherWordV1.Up(migrationBuilder);
            MarshesLeggoMyLogosV1.Up(migrationBuilder);
            MarshesSlayOnTargetV1.Up(migrationBuilder);
            MarshesSubterraneanChamberV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesSubterraneanChamberV1.Down(migrationBuilder);
            MarshesSlayOnTargetV1.Down(migrationBuilder);
            MarshesLeggoMyLogosV1.Down(migrationBuilder);
            MarshesFreedomsJustAnotherWordV1.Down(migrationBuilder);
            MarshesTasteOfTheirOwnMedicineV1.Down(migrationBuilder);
            foreach (var sql in MarshesLogosResearchNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
