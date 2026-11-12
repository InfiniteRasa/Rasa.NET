using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// creature_tint: the two tints of a creature's body, for one given colours of its own
    /// (CreatureTintEntry). No rows: every creature is tinted as before (Creature.TintAtSpawn).
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271107000000_Add_creature_tint")]
    public class Add_creature_tint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: CreatureTintEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    hue = table.Column<uint>(type: "int unsigned", nullable: true),
                    hue2 = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_tint", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: CreatureTintEntry.TableName);
        }
    }
}
