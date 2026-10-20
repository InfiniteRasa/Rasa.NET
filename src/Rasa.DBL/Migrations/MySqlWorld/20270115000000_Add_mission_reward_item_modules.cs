using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
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
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270115000000_Add_mission_reward_item_modules")]
    public partial class Add_mission_reward_item_modules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
                migrationBuilder.AddColumn<uint>(
                    name: column,
                    table: MissionRewardItemEntry.TableName,
                    type: "int(11) unsigned",
                    nullable: false,
                    defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
                migrationBuilder.DropColumn(
                    name: column,
                    table: MissionRewardItemEntry.TableName);
        }

        private static readonly string[] Columns = { "module_1", "module_2", "module_3", "module_4" };
    }
}
