using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 15: the quest NPCs stand still (no run or walk speed) and at least an aggro range and a
    /// margin outside every hostile pool's circle (MarshesNpcsStandStillV1), and Trapped!'s crew with them
    /// (MarshesTrappedV3 in place of MarshesTrappedV2). Down puts back the speeds, the places and MarshesTrappedV2. Data only.
    /// </summary>
    public partial class Marshes_npcs_stand_still : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesNpcsStandStillV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesTrappedV2.Down(migrationBuilder);
            MarshesTrappedV3.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesTrappedV3.Down(migrationBuilder);
            MarshesTrappedV2.Up(migrationBuilder);
            foreach (var sql in MarshesNpcsStandStillV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
