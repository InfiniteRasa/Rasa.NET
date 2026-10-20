using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// What the four control point Prestige Vendors (creature name 10504 "CP Prestige Vendor",
    /// vendor package 139) sell, and for how much prestige (Cp_prestige_stock).
    ///
    /// They were generated from the stock of the one hand-entered prestige vendor, Prestige Supply
    /// Vendor Twin Pillars (67), whose only row was template 145, a Pistol
    /// (Weapon_Avatar_Pistol_Physical_CMN_01_to_04) - a placeholder, and all they sold. The
    /// client's vendor package carries a vendor type and a currency and no list of goods.
    ///
    /// They sell, in this order at the counter:
    ///  - Attribute Respec Token (122034, class 29110 Consumable_Respec_Attributes) for 999,
    ///  - Skill Respec Token (122035, class 29111 Consumable_Respec_Skills) for 500,
    ///  - Experience Booster (122036, class 29112 Consumable_XP_Booster) for 150.
    ///
    /// A prestige vendor charges each item's template buy_price, in prestige (Vendor.PriceOf,
    /// VendorPackages); these vendors have no vendor_price row. The three templates were at 1,
    /// the loot_value of their classes, and no vendor sold them, so their buy_price is the
    /// prestige price. sell_price stays as it is.
    ///
    /// The Pistol's row is made the Attribute Respec Token's, and Down makes it the Pistol's again,
    /// so the table's rows keep their order through both.
    ///
    /// Guarded: only those four, and only while they are on package 139; a token is added only to
    /// a vendor that does not sell it already, and a price changes only from the value it had.
    /// Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class CpPrestigeStock
    {
        /// <summary>The control point Prestige Vendors: Iapyx, River-base Krimm, Northwest AFS, Bane Guard Station.</summary>
        public static readonly uint[] Vendors = { 500100, 500186, 500286, 500308 };

        public const uint PrestigePackage = 139;

        /// <summary>What they sold: the Pistol copied from Prestige Supply Vendor Twin Pillars.</summary>
        public const uint Placeholder = 145;

        public const uint AttributeRespecToken = 122034;
        public const uint SkillRespecToken = 122035;
        public const uint ExperienceBooster = 122036;

        /// <summary>Their buy_price before.</summary>
        public const int PriceWas = 1;

        /// <summary>The stock, in counter order, with its prestige price.</summary>
        public static readonly (uint Template, int Price)[] Stock =
        {
            (AttributeRespecToken, 999),
            (SkillRespecToken, 500),
            (ExperienceBooster, 150)
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            // The Pistol's row becomes the first token's, so the counter keeps its place in the table.
            yield return Replace(Placeholder, AttributeRespecToken);

            foreach (var (template, _) in Stock)
                yield return Add(template);

            foreach (var (template, price) in Stock)
                yield return Price(template, PriceWas, price);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (template, price) in Stock)
                yield return Price(template, price, PriceWas);

            yield return $"DELETE FROM {VendorItemEntry.TableName} WHERE id IN ({OnPackage}) " +
                         $"AND item_template_id IN ({string.Join(", ", Stock.Skip(1).Select(s => s.Template))});";

            yield return Replace(AttributeRespecToken, Placeholder);
            yield return Add(Placeholder);
        }

        /// <summary>The four, while they are still prestige vendors.</summary>
        private static string OnPackage =>
            $"SELECT id FROM {VendorEntry.TableName} WHERE package_id = {PrestigePackage} AND id IN ({string.Join(", ", Vendors)})";

        private static string Add(uint template) =>
            $"INSERT INTO {VendorItemEntry.TableName} (id, item_template_id) " +
            $"SELECT v.id, {template} FROM {VendorEntry.TableName} v " +
            $"WHERE v.package_id = {PrestigePackage} AND v.id IN ({string.Join(", ", Vendors)}) " +
            $"AND NOT EXISTS (SELECT 1 FROM {VendorItemEntry.TableName} i WHERE i.id = v.id AND i.item_template_id = {template}) " +
            "ORDER BY v.id;";

        private static string Replace(uint from, uint to) =>
            $"UPDATE {VendorItemEntry.TableName} SET item_template_id = {to} WHERE item_template_id = {from} AND id IN ({OnPackage});";

        private static string Price(uint template, int from, int to) =>
            $"UPDATE {ItemTemplateEntry.TableName} SET buy_price = {to} WHERE id = {template} AND buy_price = {from};";
    }
}
