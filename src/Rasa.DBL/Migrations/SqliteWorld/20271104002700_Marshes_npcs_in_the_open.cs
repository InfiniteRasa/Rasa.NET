using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix rows 21 and 22: Conscript Kaav (and his 1028 marker) out of the depot's pipes onto its open
    /// floor, and Lt. Kiang out of the sentry tower's unreachable room to the tower's foot (MarshesNpcsInTheOpenV1). Down puts
    /// them back. Data only.
    /// </summary>
    public partial class Marshes_npcs_in_the_open : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesNpcsInTheOpenV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesNpcsInTheOpenV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
