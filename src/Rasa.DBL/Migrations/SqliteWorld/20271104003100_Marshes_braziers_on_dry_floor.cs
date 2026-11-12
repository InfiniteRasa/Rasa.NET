using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Bringing the Spirit Home's (864) two braziers out of the temple fountains' water onto the temple
    /// floor beside them (MarshesBraziersOnDryFloorV1). Down puts back the old places. Data only.
    /// </summary>
    public partial class Marshes_braziers_on_dry_floor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesBraziersOnDryFloorV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesBraziersOnDryFloorV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
