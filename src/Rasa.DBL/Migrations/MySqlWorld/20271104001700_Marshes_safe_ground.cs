using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix rows 1 and 4: Warrior Dikah (set 5) and the Village Ruins NPCs (sets 14 and 15) out of the
    /// hostile pools' reach, with their markers, and Dikah's stats (MarshesSafeGroundV1). Data only.
    /// </summary>
    public partial class Marshes_safe_ground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesSafeGroundV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesSafeGroundV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
