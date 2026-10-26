using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The tool templates lose the guns' placeholder alternate attack and take their own action's
    /// range (ToolTemplatesNoAltFire). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the placeholder back on the rows Up changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270125000000_Tool_templates_no_alt_fire")]
    public class Tool_templates_no_alt_fire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ToolTemplatesNoAltFire.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ToolTemplatesNoAltFire.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
