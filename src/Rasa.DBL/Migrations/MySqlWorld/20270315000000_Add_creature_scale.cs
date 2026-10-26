using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// creature_scale: the size of a creature's body, for one bigger or smaller than its class's
    /// model (CreatureScaleEntry). No rows: every creature keeps its model's own size.
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270315000000_Add_creature_scale")]
    public class Add_creature_scale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: CreatureScaleEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    scale = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_scale", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: CreatureScaleEntry.TableName);
        }
    }
}
