using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The 200 tool templates with an itemtemplate_weapon row (tool_type 1-6: healing disc, armor
    /// augmentation tool, cipher, tissue extractor, salvage tool, field repair tool) were given the
    /// guns' placeholder alternate attack: action 1/133 for 25 physical at 80 m with an area of
    /// 1, and a range of 80. The client turns that into an alternate attack on every tool
    /// (weapon.py Recv_WeaponInfo: altInfo from altActionId), so a healing disc could be swung for
    /// 25 at pistol reach, and into every tool's tooltip: "Optimal Range: 80m / Alt Fire:
    /// Healing: 25 / Area Range 1m" (tooltipwindow.py).
    ///
    /// A tool has no alternate attack: alt_action_id, alt_action_arg_id and the alt damage, type,
    /// range and area all go to 0, which ItemInfo reads as none (no WeaponAltInfo) and WeaponInfo
    /// sends as None. Its range is its own action's maximum range from the client's actiondata
    /// actionArguments, by the attack action of its weapon class:
    ///  - healing disc direct (147/1) 30, radial (147/5) 10;
    ///  - field repair direct (198/1) 30, radial (198/3) 10;
    ///  - armor augmentation (199/1) 20;
    ///  - cipher (258/14) 5;
    ///  - salvage (172/168) and tissue extraction (172/169) 5.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row changes only while it still holds the
    /// placeholder, and Down puts it back only on a row still as Up left it.
    /// </summary>
    public static class ToolTemplatesNoAltFire
    {
        public sealed record Tool(uint ToolType, uint ActionId, uint ArgId, uint Range, string Name);

        public static readonly IReadOnlyList<Tool> Tools = new[]
        {
            new Tool(1, 147, 1, 30, "healing disc, direct"),
            new Tool(1, 147, 5, 10, "healing disc, radial"),
            new Tool(6, 198, 1, 30, "field repair tool, direct"),
            new Tool(6, 198, 3, 10, "field repair tool, radial"),
            new Tool(2, 199, 1, 20, "armor augmentation tool"),
            new Tool(3, 258, 14, 5, "cipher tool"),
            new Tool(5, 172, 168, 5, "salvage tool"),
            new Tool(4, 172, 169, 5, "tissue extractor")
        };

        /// <summary>The placeholder every tool row had: alt action 1/133, 25 type 1 at 80 m, area 1 type 1, range 80.</summary>
        private const string Placeholder =
            "alt_action_id = 1, alt_action_arg_id = 133, alt_max_damage = 25, alt_damage_type = 1, alt_range = 80, alt_ae_radius = 1, alt_ae_type = 1, `range` = 80";

        private const string PlaceholderHeld =
            "alt_action_id = 1 AND alt_action_arg_id = 133 AND alt_max_damage = 25 AND alt_damage_type = 1 AND alt_range = 80 AND alt_ae_radius = 1 AND alt_ae_type = 1 AND `range` = 80";

        private const string NoAlt =
            "alt_action_id = 0, alt_action_arg_id = 0, alt_max_damage = 0, alt_damage_type = 0, alt_range = 0, alt_ae_radius = 0, alt_ae_type = 0";

        private const string NoAltHeld =
            "alt_action_id = 0 AND alt_action_arg_id = 0 AND alt_max_damage = 0 AND alt_damage_type = 0 AND alt_range = 0 AND alt_ae_radius = 0 AND alt_ae_type = 0";

        public static IEnumerable<string> Up()
        {
            foreach (var tool in Tools)
                yield return $"UPDATE {ItemTemplateWeaponEntry.TableName} SET {NoAlt}, `range` = {tool.Range} " +
                    $"WHERE tool_type = {tool.ToolType} AND {PlaceholderHeld} AND id IN ({TemplatesOf(tool)});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var tool in Tools)
                yield return $"UPDATE {ItemTemplateWeaponEntry.TableName} SET {Placeholder} " +
                    $"WHERE tool_type = {tool.ToolType} AND {NoAltHeld} AND `range` = {tool.Range} AND id IN ({TemplatesOf(tool)});";
        }

        private static string TemplatesOf(Tool tool) =>
            "SELECT ic.itemTemplateId FROM itemtemplate_itemclass ic JOIN weaponclass wc ON wc.id = ic.itemClassId " +
            $"WHERE wc.attack_action_id = {tool.ActionId} AND wc.attack_action_arg_id = {tool.ArgId}";
    }
}
