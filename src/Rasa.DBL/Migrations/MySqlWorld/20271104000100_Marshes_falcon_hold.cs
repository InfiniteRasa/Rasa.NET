using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 2, Falcon Hold: Research Chemist Horsh and the two Swamp Grubber pools west of Falcon Hold
    /// (MarshesFalconHoldNpcsV1), with Retread Vincent's It Lies in Ruins (1673, MarshesItLiesInRuinsV1, on the Plateau
    /// revision with Retreads to Retreads), Horsh's Tastes Like Grubber (959, MarshesTastesLikeGrubberV1) and Fixing the Food
    /// Chain (961, MarshesFixingTheFoodChainV1). Data only.
    /// </summary>
    public partial class Marshes_falcon_hold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesFalconHoldNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesItLiesInRuinsV1.Up(migrationBuilder);
            MarshesTastesLikeGrubberV1.Up(migrationBuilder);
            MarshesFixingTheFoodChainV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesFixingTheFoodChainV1.Down(migrationBuilder);
            MarshesTastesLikeGrubberV1.Down(migrationBuilder);
            MarshesItLiesInRuinsV1.Down(migrationBuilder);
            foreach (var sql in MarshesFalconHoldNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
