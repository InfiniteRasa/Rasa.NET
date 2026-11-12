using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Specialist Hax (546's hack) with three times the escort's hit points, 12000, as Trapped!'s Samuel
    /// (MarshesHaxHoldsV1). Down puts back the escort's 4000. Data only.
    /// </summary>
    public partial class Marshes_hax_holds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesHaxHoldsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesHaxHoldsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
