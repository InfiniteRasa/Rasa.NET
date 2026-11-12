using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Map link 41, the Valverde Marshes door to the Bane Supply Depot, arrives inside the depot on its floor instead of on
    /// link 39, the depot's way out (BaneSupplyDepotDoorArrival). Data only, plain SQL; Down puts back the old arrival.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271106000000_Bane_supply_depot_door_arrival")]
    public class Bane_supply_depot_door_arrival : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BaneSupplyDepotDoorArrival.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BaneSupplyDepotDoorArrival.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
