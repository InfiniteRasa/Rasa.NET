using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The Snowball (AccountReward_Holiday_Consumable_Snowball, 131481) is not Character Unique.
    /// Promo_vendor_tokens_and_unique put the flag on everything John and Elm sell; the Snowball
    /// is used up one at a time when thrown (ABILITY_NULL 528's item requirement), so a
    /// character is meant to carry more than one. It stacks to 5000 (itemclass 30547), and John
    /// sells it by the stack at his 1 credit each: the client's vendor window offers a quantity
    /// for a stackable item, and RequestVendorPurchase takes up to a stack. Down puts the flag
    /// back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261103000000_Snowball_stacks_not_unique")]
    public partial class Snowball_stacks_not_unique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 0 where id = {PromoVendors.SnowballTemplateId};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateEntry.TableName} set has_character_unique_flag = 1 where id = {PromoVendors.SnowballTemplateId};");
        }
    }
}
