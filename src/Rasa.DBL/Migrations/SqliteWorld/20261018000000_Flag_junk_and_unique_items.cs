using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// Two item properties the class names state and the client has a value for:
    /// - Loot_Junk_* (150 templates: Atta mandibles, Bane logic controllers ...) are quality JUNK
    ///   (generated.client.quality 7, ItemQuality_Junk), not NORMAL;
    /// - Armor_Unique_Eloh_* (101 templates, levels 30-50) are Character Unique, which the
    ///   client's tooltip shows as "Character Unique".
    /// Only rows still at NORMAL / not unique; Down puts those back on the rows still holding
    /// what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Flag_junk_and_unique_items : Migration
    {
        private const int Normal = 2;
        private const int Junk = 7;

        /// <summary>
        /// Templates whose class name starts with the prefix. A prefix compare rather than LIKE:
        /// '_' is a LIKE wildcard, and escaping it is spelt differently in MySQL and SQLite.
        /// </summary>
        private static string OfClass(string prefix) =>
            $"id in (select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c join {EntityClassEntry.TableName} e on e.id = c.itemClassId"
            + $" where substr(e.class_name, 1, {prefix.Length}) = '{prefix}')";

        private const string JunkClasses = "Loot_Junk_";
        private const string UniqueClasses = "Armor_Unique_";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set quality_id = {Junk} where quality_id = {Normal} and {OfClass(JunkClasses)};");
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 1 where has_character_unique_flag = 0 and {OfClass(UniqueClasses)};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 0 where has_character_unique_flag = 1 and {OfClass(UniqueClasses)};");
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set quality_id = {Normal} where quality_id = {Junk} and {OfClass(JunkClasses)};");
        }
    }
}
