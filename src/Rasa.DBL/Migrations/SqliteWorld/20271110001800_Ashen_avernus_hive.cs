using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 18, Avernus Outpost (squad instance 2110), the hive: Captain Tananda, Sgt. Frogga, Colonel
    /// Vandeweghe, the Eloh Zuriel, the Queen's Chamber's Bane and their pool, and the Atta Teleporter pads 917-920 new
    /// (AshenAvernusHiveNpcsV1), with Assessing The Atta (1536, AshenAssessingTheAttaV1), God Save the Queen (1537,
    /// AshenGodSaveTheQueenV1) and The Big Secret (1581, AshenTheBigSecretV1). Sorts after set 17 (20271110001700). It updates
    /// no row it did not insert itself.
    /// </summary>
    public partial class Ashen_avernus_hive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenAvernusHiveNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            foreach (var sql in AshenAvernusHiveNpcsV1.PadsUp())
                migrationBuilder.Sql(sql);
            AshenAssessingTheAttaV1.Up(migrationBuilder);
            AshenGodSaveTheQueenV1.Up(migrationBuilder);
            AshenTheBigSecretV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenTheBigSecretV1.Down(migrationBuilder);
            AshenGodSaveTheQueenV1.Down(migrationBuilder);
            AshenAssessingTheAttaV1.Down(migrationBuilder);
            foreach (var sql in AshenAvernusHiveNpcsV1.PadsDown())
                migrationBuilder.Sql(sql);
            foreach (var sql in AshenAvernusHiveNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
