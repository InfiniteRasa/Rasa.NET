using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 21: the body tints of the Ashen creatures (AshenCreatureTintsV1, creature_tint): the rows of the
    /// four Ashen maps and the new hostile rows of sets 1-20. Sorts after set 20 (20271110002000). Data only, plain SQL; Down
    /// deletes only the rows that still have the values Up wrote.
    /// </summary>
    public partial class Ashen_creature_tints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenCreatureTintsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenCreatureTintsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
