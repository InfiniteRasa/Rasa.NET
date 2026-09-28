using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// character_title was keyed on character_id alone, and autoincremented it, so a character
    /// could hold one title. It is keyed on (character_id, title_id) now, one row per title.
    /// SQLite cannot change a primary key in place, so the table is rebuilt with its rows.
    ///
    /// Down keeps each character's lowest title id, the one row per character the old key allows.
    /// </summary>
    public partial class Key_character_title_per_title : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TABLE ""ef_temp_character_title"" (
    ""character_id"" INTEGER NOT NULL,
    ""title_id"" INTEGER NOT NULL,
    CONSTRAINT ""PK_character_title"" PRIMARY KEY (""character_id"", ""title_id""));");
            migrationBuilder.Sql(@"INSERT INTO ""ef_temp_character_title"" (""character_id"", ""title_id"")
    SELECT ""character_id"", ""title_id"" FROM ""character_title"";");
            migrationBuilder.Sql(@"DROP TABLE ""character_title"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_character_title"" RENAME TO ""character_title"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TABLE ""ef_temp_character_title"" (
    ""character_id"" INTEGER NOT NULL CONSTRAINT ""PK_character_title"" PRIMARY KEY AUTOINCREMENT,
    ""title_id"" INTEGER NOT NULL);");
            migrationBuilder.Sql(@"INSERT INTO ""ef_temp_character_title"" (""character_id"", ""title_id"")
    SELECT ""character_id"", MIN(""title_id"") FROM ""character_title"" GROUP BY ""character_id"";");
            migrationBuilder.Sql(@"DROP TABLE ""character_title"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_character_title"" RENAME TO ""character_title"";");
        }
    }
}
