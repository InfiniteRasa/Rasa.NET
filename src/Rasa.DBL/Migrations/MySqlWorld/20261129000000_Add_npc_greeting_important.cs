using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// npc_greeting.important: whether the NPC is marked as having something to say - the
    /// client's important greeting, which puts a speech bubble over it while it has nothing
    /// else for the player. The client does not say which NPCs were; a game master marks one
    /// (".greeting important"), and the rows that had a line before this are not marked.
    ///
    /// One NPC starts marked, with its line (NpcGreetingSeed.Marked): Brigadier General
    /// Beacham, greeting 488. A line a game master gave him before is left as it is, and is
    /// marked only if it is that line.
    ///
    /// Down drops the column. The General keeps his line: it may have been his before.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_npc_greeting_important : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "important",
                table: NpcGreetingEntry.TableName,
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            foreach (var statement in NpcGreetingSeed.MarkStatements("insert ignore"))
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "important",
                table: NpcGreetingEntry.TableName);
        }
    }
}
