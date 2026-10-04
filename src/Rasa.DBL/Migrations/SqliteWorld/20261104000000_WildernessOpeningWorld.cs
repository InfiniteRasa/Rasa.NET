using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Linq;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class WildernessOpeningWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Services.Preloader.Missions.Wilderness.WildernessOpeningWorldV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Services.Preloader.Missions.Wilderness.WildernessOpeningWorldV1.Down(migrationBuilder);
            // The preceding PR105 data-only migration has no entity mappings in its target model.
            foreach (var operation in migrationBuilder.Operations)
            {
                if (operation is DeleteDataOperation delete)
                    delete.KeyColumnTypes = new[] { "INTEGER" };
                if (operation is UpdateDataOperation update)
                {
                    update.KeyColumnTypes = new[] { "INTEGER" };
                    update.ColumnTypes = update.Columns.Select(column => column switch
                    {
                        "pos_x" or "pos_y" or "pos_z" or "rotation" => "REAL",
                        _ => "INTEGER"
                    }).ToArray();
                }
            }
        }
    }
}
