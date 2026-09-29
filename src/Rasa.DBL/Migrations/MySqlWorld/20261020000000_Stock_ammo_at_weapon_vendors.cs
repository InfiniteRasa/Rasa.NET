using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Every weapons vendor sells all five grades of all five ammunition families.
    ///
    /// A vendor is a weapons vendor when its package is one the client's vendordata.vendorpackages
    /// types WEAPONS (4): that is what puts the weapons icon over its head and on the map. In the
    /// seed data that is 29 vendors - 27 map vendors and Weapon Supply Twin Pillars on package 14,
    /// Test Vendor 5 on package 10 - and every one of them sold only Standard Grade Cartridges and
    /// Standard Grade Power Cells. No vendor anywhere sold grades 3 to 5, and none of the map
    /// vendors sold Canisters or Rockets, so a player past the early levels, or with a fire, ice or
    /// launcher weapon, had nowhere to buy the ammunition the weapon asks for.
    ///
    /// The strategy guide: "The weapon's level determines the ammo grade it requires ... there are
    /// five grades of ammo", with Cartridges, Energy Cells, Canisters, Rockets and Pharmaceuticals.
    /// The templates are the ones whose item class is Standard, Improved, High, Select and Elite
    /// Grade of each (client physicalentityclassnamelanguage); their buy prices are already the
    /// class loot_value, which is the guide's price list.
    ///
    /// Only pairs a vendor does not already have are added, so the two it had are not doubled.
    /// Down removes the 23 this adds and leaves Standard Cartridges and Power Cells in place.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Stock_ammo_at_weapon_vendors : Migration
    {
        /// <summary>The client's WEAPONS vendor packages.</summary>
        private static readonly uint[] WeaponPackages =
        {
            10, 14, 40, 43, 45, 47, 48, 51, 53, 55, 57, 59, 60, 63, 65, 67, 68, 71, 73, 75, 77, 78, 118, 131
        };

        /// <summary>Standard, Improved, High, Select and Elite Grade, family by family, in the order they are listed at the counter.</summary>
        private static readonly uint[] Ammo =
        {
            28, 110903, 110904, 110905, 110906,     // Cartridges
            56, 110894, 110896, 110897, 110902,     // Power Cells
            32, 110898, 110899, 110901, 110900,     // Canister Ammunition
            30, 110911, 110912, 110913, 110914,     // Rockets
            636, 110907, 110908, 110909, 110910     // Pharmaceuticals
        };

        /// <summary>What every weapons vendor already sold: Standard Grade Cartridges and Power Cells.</summary>
        private static readonly uint[] AlreadyStocked = { 28, 56 };

        private static string WeaponVendors =>
            $"select id from {VendorEntry.TableName} where package_id in ({string.Join(", ", WeaponPackages)})";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var ammo = string.Join(" union all ", Ammo.Select((t, n) => $"select {t} as template_id, {n} as n"));

            migrationBuilder.Sql(
                $"insert into {VendorItemEntry.TableName} (id, item_template_id)"
                + $" select v.id, a.template_id from {VendorEntry.TableName} v cross join ({ammo}) a"
                + $" where v.package_id in ({string.Join(", ", WeaponPackages)})"
                + $" and not exists (select 1 from {VendorItemEntry.TableName} i where i.id = v.id and i.item_template_id = a.template_id)"
                + " order by v.id, a.n;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var added = Ammo.Except(AlreadyStocked);

            migrationBuilder.Sql(
                $"delete from {VendorItemEntry.TableName} where id in ({WeaponVendors})"
                + $" and item_template_id in ({string.Join(", ", added)});");
        }
    }
}
