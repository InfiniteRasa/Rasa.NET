using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: eight scene creatures of 548, 664, 891 and 920 onto the server navmesh, each where the client files
    /// have a floor too (MarshesOnTheNavmeshV1; MissionGroundTests had them off the navmesh). Down puts back the old
    /// places. Data only.
    /// </summary>
    public partial class Marshes_on_the_navmesh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesOnTheNavmeshV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesOnTheNavmeshV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
