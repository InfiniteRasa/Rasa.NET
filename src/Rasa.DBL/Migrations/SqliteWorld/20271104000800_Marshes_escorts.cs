using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 9, the escorts: the escorted creatures, Lieutenants Belfast, Delaio and Urqhart, and MP Price's
    /// package 386 moved to Lieutenant Bruncke (MarshesEscortsNpcsV1), with Forean Flu (497, MarshesForeanFluV1), Tread
    /// Lightly (888, MarshesTreadLightlyV1), AWOL Andy (889, MarshesAwolAndyV1) and Rest and Recreation (1023,
    /// MarshesRestAndRecreationV1), all on the marshes.escort scene. Data only.
    /// </summary>
    public partial class Marshes_escorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesEscortsNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesForeanFluV1.Up(migrationBuilder);
            MarshesTreadLightlyV1.Up(migrationBuilder);
            MarshesAwolAndyV1.Up(migrationBuilder);
            MarshesRestAndRecreationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesRestAndRecreationV1.Down(migrationBuilder);
            MarshesAwolAndyV1.Down(migrationBuilder);
            MarshesTreadLightlyV1.Down(migrationBuilder);
            MarshesForeanFluV1.Down(migrationBuilder);
            foreach (var sql in MarshesEscortsNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
