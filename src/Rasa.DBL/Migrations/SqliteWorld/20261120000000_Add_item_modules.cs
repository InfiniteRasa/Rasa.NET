using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// module_class and module_effect: the item modules - what an item carries in its four
    /// module slots - and what each does, with the 867 modules of the client's own crafting
    /// data and the 290 effect rows its text gives the values of (ItemModuleSeed).
    ///
    /// Down drops both tables.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_item_modules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ModuleClassEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    variant_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    level = table.Column<uint>(type: "INTEGER", nullable: false),
                    class_set_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_class_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    extract_cost = table.Column<uint>(type: "INTEGER", nullable: false),
                    integrate_cost = table.Column<uint>(type: "INTEGER", nullable: false),
                    salvage_gain = table.Column<uint>(type: "INTEGER", nullable: false),
                    upgrade_cost = table.Column<uint>(type: "INTEGER", nullable: false),
                    upgrade_module_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_class", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: ModuleEffectEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    module_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    effect_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    set_level = table.Column<uint>(type: "INTEGER", nullable: false),
                    flat_value = table.Column<double>(type: "REAL", nullable: false),
                    linear_value = table.Column<double>(type: "REAL", nullable: false),
                    exp_value = table.Column<double>(type: "REAL", nullable: false),
                    arg1 = table.Column<int>(type: "INTEGER", nullable: true),
                    arg2 = table.Column<int>(type: "INTEGER", nullable: true),
                    arg3 = table.Column<int>(type: "INTEGER", nullable: true),
                    arg4 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_effect", x => x.id);
                });

            foreach (var insert in ItemModuleSeed.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: ModuleEffectEntry.TableName);
            migrationBuilder.DropTable(name: ModuleClassEntry.TableName);
        }
    }
}
