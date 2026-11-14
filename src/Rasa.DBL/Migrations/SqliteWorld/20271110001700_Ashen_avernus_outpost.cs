using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 17, Avernus Outpost (squad instance 2110), the outpost: Captain Sera Karter, Commander Drazen,
    /// Captain Petrovich's two rows, his squad's rows and the Atta Teleporter pads 915/916 new (AshenAvernusOutpostNpcsV1), with
    /// Overly Sensitive (1473, AshenOverlySensitiveV1), Petrovich's Last Stand (1533, AshenPetrovichsLastStandV1) and Petrovich's
    /// Run (1535, AshenPetrovichsRunV1). Sorts after set 16 (20271110001600).
    /// </summary>
    public partial class Ashen_avernus_outpost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenAvernusOutpostNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            foreach (var sql in AshenAvernusOutpostNpcsV1.PadsUp())
                migrationBuilder.Sql(sql);
            AshenOverlySensitiveV1.Up(migrationBuilder);
            AshenPetrovichsLastStandV1.Up(migrationBuilder);
            AshenPetrovichsRunV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenPetrovichsRunV1.Down(migrationBuilder);
            AshenPetrovichsLastStandV1.Down(migrationBuilder);
            AshenOverlySensitiveV1.Down(migrationBuilder);
            foreach (var sql in AshenAvernusOutpostNpcsV1.PadsDown())
                migrationBuilder.Sql(sql);
            foreach (var sql in AshenAvernusOutpostNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
