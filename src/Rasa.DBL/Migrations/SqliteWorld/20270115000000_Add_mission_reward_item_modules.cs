using System.Linq;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Structures.World;

    /// <summary>
    /// Modules on mission reward items: mission_reward_item.module_1 to module_4, a module_class
    /// id in each of the item's four slots or 0 for an empty one, as items.module_1 to module_4
    /// hold them. Every row starts with all four 0: no reward carries a module until one is
    /// given one.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270115000000_Add_mission_reward_item_modules")]
    public partial class Add_mission_reward_item_modules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
                migrationBuilder.AddColumn<uint>(
                    name: column,
                    table: MissionRewardItemEntry.TableName,
                    type: "int(11)",
                    nullable: false,
                    defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // As SQL, not DropColumn: the SQLite provider drops a column by rebuilding the table
            // from the model of the migration before, and the one before this is not one that
            // has it - "SQLite does not support this migration operation". SQLite itself has
            // dropped columns since 3.35.
            foreach (var column in Columns.Reverse())
                migrationBuilder.Sql($"alter table {MissionRewardItemEntry.TableName} drop column {column};");
        }

        private static readonly string[] Columns = { "module_1", "module_2", "module_3", "module_4" };
    }
}
