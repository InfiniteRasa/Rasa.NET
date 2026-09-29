using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// Account rewards - AccountReward_*: the emotes, pets and companions, armour paints and
    /// their schematics, the Soyuz model rockets, the veteran titles, beacons and booster, the
    /// holiday snowballs and launcher, the space helmet, and the Sunset emotes and pets - are
    /// bound on character, not tradable and not sellable, like the other gear handed to one
    /// person (Bind_gm_veteran_and_event_gear). The footlocker still takes them. The three
    /// companion tokens are already mission items (Flag_mission_items), so of the 105 templates
    /// this sets 102. Only rows still at the defaults; Down puts them back on the rows still
    /// holding what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Bind_account_reward_items : Migration
    {
        private const string Prefix = "AccountReward_";

        /// <summary>A prefix compare rather than LIKE, whose '_' wildcard is escaped differently in MySQL and SQLite.</summary>
        private static string Templates =>
            $"id in (select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c join {EntityClassEntry.TableName} e on e.id = c.itemClassId"
            + $" where substr(e.class_name, 1, {Prefix.Length}) = '{Prefix}')";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set bound_to_character_flag = 1, not_tradable_flag = 1, has_sellable_flag = 0"
                + $" where bound_to_character_flag = 0 and not_tradable_flag = 0 and has_sellable_flag = 1 and not_placable_in_lockbox_flag = 0 and {Templates};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set bound_to_character_flag = 0, not_tradable_flag = 0, has_sellable_flag = 1"
                + $" where bound_to_character_flag = 1 and not_tradable_flag = 1 and has_sellable_flag = 0 and not_placable_in_lockbox_flag = 0 and {Templates};");
        }
    }
}
