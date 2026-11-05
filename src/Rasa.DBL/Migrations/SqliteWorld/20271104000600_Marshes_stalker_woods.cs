using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 7, Stalker Woods (High Point Retreat): Retread Jeska, Retread Dudley, Retread Duvall and a Maw pool
    /// (MarshesStalkerWoodsNpcsV1), with Jeska's Missing in Action (1863, MarshesMissingInActionV1), Dudley's Light the Fires
    /// (1864, MarshesLightTheFiresV1) and Now Can You Hear Me? (1869, MarshesNowCanYouHearMeV1), and Duvall's Stop and Smell
    /// the Maw Flowers (1871, MarshesMawFlowersV1). Data only.
    /// </summary>
    public partial class Marshes_stalker_woods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesStalkerWoodsNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesMissingInActionV1.Up(migrationBuilder);
            MarshesLightTheFiresV1.Up(migrationBuilder);
            MarshesNowCanYouHearMeV1.Up(migrationBuilder);
            MarshesMawFlowersV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesMawFlowersV1.Down(migrationBuilder);
            MarshesNowCanYouHearMeV1.Down(migrationBuilder);
            MarshesLightTheFiresV1.Down(migrationBuilder);
            MarshesMissingInActionV1.Down(migrationBuilder);
            foreach (var sql in MarshesStalkerWoodsNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
