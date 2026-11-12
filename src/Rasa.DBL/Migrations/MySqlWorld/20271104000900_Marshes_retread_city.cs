using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 10, Retread City: Commander Figgins, a Marshes Lieutenant Holloway, the AFS Mech squadmate and
    /// Retread Samuel's stand-in and crew (MarshesRetreadCityNpcsV1), with Highway Robbery (914, MarshesHighwayRobberyV1),
    /// Production Destruction (916 and 1033, MarshesProductionDestructionV1) and Trapped! (1052, MarshesTrappedV1, on the
    /// marshes.defence scene). Data only.
    /// </summary>
    public partial class Marshes_retread_city : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesRetreadCityNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesHighwayRobberyV1.Up(migrationBuilder);
            MarshesProductionDestructionV1.Up(migrationBuilder);
            MarshesTrappedV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesTrappedV1.Down(migrationBuilder);
            MarshesProductionDestructionV1.Down(migrationBuilder);
            MarshesHighwayRobberyV1.Down(migrationBuilder);
            foreach (var sql in MarshesRetreadCityNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
