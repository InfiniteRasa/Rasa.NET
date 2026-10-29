using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Nakrinx and his bodyguards on patrol south of the Thrax Barracks, and Lieutenant Chester's package
    /// (PoolsSupplyNpcsV1), with the supply chain: The Supply Master (1322, PoolsSupplyMasterV1), Eastern Listening Post
    /// (1323, PoolsEasternListeningPostV1), Smash and Grab (1324, PoolsSmashAndGrabV1) and Spoils of War (1326,
    /// PoolsSpoilsOfWarV1). Data only.
    /// </summary>
    public partial class Pools_supply_chain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsSupplyNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsSupplyMasterV1.Up(migrationBuilder);
            PoolsEasternListeningPostV1.Up(migrationBuilder);
            PoolsSmashAndGrabV1.Up(migrationBuilder);
            PoolsSpoilsOfWarV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsSpoilsOfWarV1.Down(migrationBuilder);
            PoolsSmashAndGrabV1.Down(migrationBuilder);
            PoolsEasternListeningPostV1.Down(migrationBuilder);
            PoolsSupplyMasterV1.Down(migrationBuilder);
            foreach (var sql in PoolsSupplyNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
