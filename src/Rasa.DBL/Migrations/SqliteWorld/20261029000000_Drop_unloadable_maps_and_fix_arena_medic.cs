using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// Two map_info rows the client cannot load, and the CELLAR Arena's hospital on the wrong map.
    ///
    ///  - 1991 test_lridout_outpostcombat: 1991 is a map template id, not a game context. The client
    ///    has no gamecontext row for it, so a Wonkavate there stops in BeginMapLoading
    ///    (wonkavator.py) and leaves the player on the loading screen.
    ///  - 2233 test_pvpcontrolpoint: the context points at map template 2237, whose .map does not
    ///    ship (only test_pvpcontrolpoint01, template 2238, does, and no context uses that), so the
    ///    client stops with "Map '%s' not found!" (gamemap.py).
    ///
    /// Nothing else in the world data refers to either. Both were offered by the GM map pickers.
    ///
    /// Teleporter 480, "Hospital: CELLAR Arena Medic", was on 2259 (adv_wargame_indoorarena); the
    /// client's own marker for it is on 20000009, adv_afs_arena - the CELLAR Arena - at the same
    /// coordinates, inside that map and outside the other's.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Drop_unloadable_maps_and_fix_arena_medic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from {MapInfoEntry.TableName} where map_context_id in (1991, 2233);");
            migrationBuilder.Sql($"update {TeleporterEntry.TableName} set map_context_id = 20000009 where id = 480 and map_context_id = 2259;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {TeleporterEntry.TableName} set map_context_id = 2259 where id = 480 and map_context_id = 20000009;");
            migrationBuilder.Sql($"insert into {MapInfoEntry.TableName} (map_context_id, map_name, map_version, base_region) values (1991, 'test_lridout_outpostcombat', 114, 0), (2233, 'test_pvpcontrolpoint', 140, 0);");
        }
    }
}
