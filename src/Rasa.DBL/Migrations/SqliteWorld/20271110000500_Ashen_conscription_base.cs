using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 5, the Bane Conscription Base: the four trench mortars each their own row, Linker pools round the
    /// base and Overseer Naxveik moved into the generators' compound (AshenConscriptionBaseNpcsV1), with Captain Akio Yamamoto's
    /// Death from Above (1352, AshenDeathFromAboveV1), Lieutenant Gabriel Infanger's Turning Out the Lights (1354,
    /// AshenTurningOutTheLightsV1) and Lieutenant Jan Blood's The Strongest Link (1355, AshenStrongestLinkV1). Data only, plain
    /// SQL (CrucibleSql).
    /// </summary>
    public partial class Ashen_conscription_base : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenConscriptionBaseNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenDeathFromAboveV1.Up(migrationBuilder);
            AshenTurningOutTheLightsV1.Up(migrationBuilder);
            AshenStrongestLinkV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenStrongestLinkV1.Down(migrationBuilder);
            AshenTurningOutTheLightsV1.Down(migrationBuilder);
            AshenDeathFromAboveV1.Down(migrationBuilder);
            foreach (var sql in AshenConscriptionBaseNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
