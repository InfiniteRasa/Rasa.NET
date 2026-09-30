using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Gear handed to one person rather than looted or sold - bound on character, not tradable,
    /// not sellable (its footlocker is still its own): the Game Master outfit, the veteran rewards
    /// (green beret, brass knuckles), the British General outfit and its face, and the British beret
    /// handout. 15 templates, which were tradable and sellable like any armour. Only rows still
    /// at those defaults; Down puts them back on the rows still holding what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Bind_gm_veteran_and_event_gear : Migration
    {
        public static readonly string[] Classes =
        {
            "Armor_Special_GameMaster_Boots", "Armor_Special_GameMaster_Gloves", "Armor_Special_GameMaster_Helmet",
            "Armor_Special_GameMaster_Legs", "Armor_Special_GameMaster_Vest",
            "Armor_Special_VetReward_Green_Beret", "Armor_Special_VetReward_BrassKnuckles",
            "Armor_Special_General_British_Boots", "Armor_Special_General_British_Gloves", "Armor_Special_General_British_Helmet",
            "Armor_Special_General_British_Legs", "Armor_Special_General_British_Vest", "AvatarSwap_Special_General_British_Face",
            "Armor_Special_British_Beret_Handout",
        };

        private static string Templates =>
            $"id in (select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c join {EntityClassEntry.TableName} e on e.id = c.itemClassId"
            + $" where e.class_name in ({string.Join(", ", Classes.Select(n => $"'{n}'"))}))";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set bound_to_character_flag = 1, not_tradable_flag = 1, has_sellable_flag = 0"
                + $" where bound_to_character_flag = 0 and not_tradable_flag = 0 and has_sellable_flag = 1 and {Templates};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set bound_to_character_flag = 0, not_tradable_flag = 0, has_sellable_flag = 1"
                + $" where bound_to_character_flag = 1 and not_tradable_flag = 1 and has_sellable_flag = 0 and {Templates};");
        }
    }
}
