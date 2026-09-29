using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Mission items (MissionItems): quality MISSION, bound on character, not tradable, not
    /// sellable, not placeable in a lockbox. Every item template had quality NORMAL or better and
    /// all of those flags the other way. The templates in the Mission inventory tab and the
    /// mission tools and keycards in MissionItems.ExtraClasses; only those still holding the
    /// defaults. Down puts the defaults back on the ones still holding what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Flag_mission_items : Migration
    {
        private static string Templates =>
            $"(inventory_category = {MissionItems.MissionCategory} or id in (select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c"
            + $" join {EntityClassEntry.TableName} e on e.id = c.itemClassId where e.class_name in ({string.Join(", ", MissionItems.ExtraClasses.Select(n => $"'{n}'"))})))";

        private static string Flags(int quality, int bound, int notTradable, int sellable, int notInLockbox, string joiner) =>
            string.Join(joiner, $"quality_id = {quality}", $"bound_to_character_flag = {bound}", $"not_tradable_flag = {notTradable}",
                $"has_sellable_flag = {sellable}", $"not_placable_in_lockbox_flag = {notInLockbox}");

        private static string Mission(string joiner) => Flags(MissionItems.MissionQuality, 1, 1, 0, 1, joiner);

        private static string Normal(string joiner) => Flags(MissionItems.NormalQuality, 0, 0, 1, 0, joiner);

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set {Mission(", ")} where {Normal(" and ")} and {Templates};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set {Normal(", ")} where {Mission(" and ")} and {Templates};");
        }
    }
}
