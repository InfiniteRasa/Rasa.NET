using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// John the Swag Vendor and Elm the Emote Vendor in Alia Das (PromoVendorPreloaders), and the
    /// two tables they need:
    ///
    ///  - creature_actor_name: a name for a creature the client has no creaturenamelanguage
    ///    entry for. The row's name_id is 0 and the client shows this, sent as the actor name.
    ///  - vendor_price: one price for everything a vendor sells, in place of each template's
    ///    buy_price - 1 credit at both of these.
    ///
    /// New tables rather than new columns on creature and vendor: the preloaders of earlier
    /// migrations insert those tables by their entity's columns, and would no longer match the
    /// table they insert into at that point of the history.
    ///
    /// Down drops both tables and deletes the two vendors (502001-502002).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_promo_vendors : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new PromoVendorCreaturePreloader(),
            new PromoVendorActorNamePreloader(),
            new PromoVendorAppearancePreloader(),
            new PromoVendorStatsPreloader(),
            new PromoVendorSpawnpoolPreloader(),
            new PromoVendorPreloader(),
            new PromoVendorPricePreloader(),
            new PromoVendorItemPreloader()
        };

        private static readonly string[] Tables =
        {
            VendorItemEntry.TableName,
            VendorEntry.TableName,
            SpawnPoolEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureAppearanceEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: CreatureActorNameEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    actor_name = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_actor_name", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: VendorPriceEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_price = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_price", x => x.id);
                });

            foreach (var preloader in _preloaders)
                preloader.Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: VendorPriceEntry.TableName);
            migrationBuilder.DropTable(name: CreatureActorNameEntry.TableName);

            foreach (var table in Tables)
                migrationBuilder.Sql($"delete from {table} where id between {PromoVendors.FirstId} and {PromoVendors.LastId};");
        }
    }
}
