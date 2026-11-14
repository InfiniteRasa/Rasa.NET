using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 11b, the Incurables Ward wings: Engineer Dhimand made a walker (CrucibleLeakyPipesV1.NpcUp) and Leaky
    /// Pipes (1398, CrucibleLeakyPipesV1). Blood, Fire, and Steel (1401) is not built: its wings sit behind client map statics
    /// the server cannot open (NOTES-crucible-triage.md, set 11b). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_incurables_ward_wings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleLeakyPipesV1.NpcUp())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleLeakyPipesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleLeakyPipesV1.Down(migrationBuilder);
            foreach (var sql in CrucibleLeakyPipesV1.NpcDown())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
