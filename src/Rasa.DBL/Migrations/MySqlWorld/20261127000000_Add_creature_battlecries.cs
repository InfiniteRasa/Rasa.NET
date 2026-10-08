using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// creature_battlecry: the battle cry package a creature class, or one creature row, cries
    /// with (the game server's Battlecries). A creature with no row is silent, so every
    /// creature is unchanged until it is given one.
    ///
    /// And the rows that can be told from the client: 58 classes whose name carries the
    /// creature its package's audio sets name, and the Proving Grounds' Training Officer, who
    /// has the drill sergeant's (CreatureBattlecryPreloader).
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_creature_battlecries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: CreatureBattlecryEntry.TableName,
                columns: table => new
                {
                    scope = table.Column<uint>(type: "int unsigned", nullable: false),
                    target_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    package_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_battlecry", x => new { x.scope, x.target_id });
                });

            new CreatureBattlecryPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: CreatureBattlecryEntry.TableName);
        }
    }
}
