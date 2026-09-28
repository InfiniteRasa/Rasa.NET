using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// character_title was keyed on character_id alone, and autoincremented it, so a character
    /// could hold one title. It is keyed on (character_id, title_id) now, one row per title:
    /// the autoincrement comes off character_id first, since MySQL keeps an auto column in a key.
    ///
    /// Down keeps each character's lowest title id, the one row per character the old key allows.
    /// </summary>
    public partial class Key_character_title_per_title : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `character_title` MODIFY `character_id` int unsigned NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE `character_title` DROP PRIMARY KEY, ADD PRIMARY KEY (`character_id`, `title_id`);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE `t` FROM `character_title` AS `t`
    JOIN `character_title` AS `k` ON `k`.`character_id` = `t`.`character_id` AND `k`.`title_id` < `t`.`title_id`;");
            migrationBuilder.Sql("ALTER TABLE `character_title` DROP PRIMARY KEY, ADD PRIMARY KEY (`character_id`);");
            migrationBuilder.Sql("ALTER TABLE `character_title` MODIFY `character_id` int unsigned NOT NULL AUTO_INCREMENT;");
        }
    }
}
