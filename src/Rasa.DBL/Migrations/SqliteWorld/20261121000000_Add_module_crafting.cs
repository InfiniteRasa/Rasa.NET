using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// module_item and modifiable_class: what the crafting station's module pages go by - the
    /// item templates that are modules, 5,380 of them, and the 4,957 item classes that take
    /// modules, each with its class set (ModuleCraftingSeed). Both are the client's own tables.
    ///
    /// Down drops both tables.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_module_crafting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ModuleItemEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    module_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    strength = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: ModifiableClassEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    class_set_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modifiable_class", x => x.id);
                });

            foreach (var insert in ModuleCraftingSeed.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: ModifiableClassEntry.TableName);
            migrationBuilder.DropTable(name: ModuleItemEntry.TableName);
        }
    }
}
