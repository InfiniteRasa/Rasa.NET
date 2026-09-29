using System.Linq;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// John the Swag Vendor also sells the three companion tokens - Pre-Order, Collector's
    /// Edition and GameStop - at the end of his list, and everything John and Elm sell is
    /// Character Unique: the client's tooltip says so, and RequestVendorPurchase refuses a
    /// template the character already holds.
    ///
    /// The flag goes on the item template, so it holds wherever the item came from; all 104
    /// were clear (only Armor_Unique_* carries it, Flag_junk_and_unique_items). Down clears it
    /// on the rows still holding it, before taking the tokens back off John's list.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261102000000_Promo_vendor_tokens_and_unique")]
    public partial class Promo_vendor_tokens_and_unique : Migration
    {
        private static string Vendors => $"{PromoVendors.JohnId}, {PromoVendors.ElmId}";

        private static string Tokens => string.Join(", ", PromoVendors.CompanionTokens);

        private static string Stock => $"select item_template_id from {VendorItemEntry.TableName} where id in ({Vendors})";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"insert into {VendorItemEntry.TableName} (id, item_template_id) "
                + string.Join(" union all ", PromoVendors.CompanionTokens.Select(t => $"select {PromoVendors.JohnId}, {t}"))
                + ";");

            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 1 where has_character_unique_flag = 0 and id in ({Stock});");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 0 where has_character_unique_flag = 1 and id in ({Stock});");

            migrationBuilder.Sql($"delete from {VendorItemEntry.TableName} where id = {PromoVendors.JohnId} and item_template_id in ({Tokens});");
        }
    }
}
