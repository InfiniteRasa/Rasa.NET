using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Creatures where the client's regions say they live: 5 creature grounds and 191 open-country
    /// packs on 23 maps, with 40 creature rows and 62 attacks.
    ///
    /// Grounds are the places named after the family that lives there - Warden Bot Station (wardenbot),
    /// Stalker Woods (stalker), Warnet Hive (warnet), Warnet Queen's Lair (warnet), Miasma Lair
    /// (miasma). The rest of the forty or so family-named places either already have their mined lair
    /// (the Atta colonies, Treeback Ridge, the Fithik Trench and Hatchery), are safe ground (Retread
    /// City, the Retread camp in the caves), or are rooms inside instances that nothing places
    /// (Predator Bay, Bug City, Treeback Pen, Howler Kennels, Juggernaut Bay, Strider Staging Area):
    /// those are left for hand placement.
    ///
    /// Open country is every region whose own ambience, music and sky say so - forest, highland, swamp,
    /// lava field, a named place under the outdoor sky - on a map with terrain. Each gets one animal
    /// pack per 15,000 m2, at most four, of the families its zone lists in creature_families.csv, drawn
    /// by their weights: flaregasher 53, fithik 22, howler 16, mox 16, miasma 14, maw 13, warnet 12,
    /// beammanta 12, granitour 10, xanx 5, treemite 5, barbtick 5, grubber 4, filcher 2, boargar 1,
    /// amoeboid 1. By zone: Abyss 14, Ashen Desert 22, Crucible 24, Howling Maw 21, Incline 9, Marshes
    /// 12, Mires 5, Palisades 20, Plains 21, Plateau 16, Pools 17, Thunderhead 10. Never within 40 m of
    /// a friendly NPC, hospital or waypoint, 20 m of another pool's area, or inside a safe region. The
    /// density is ours: Wilderness's hand-placed animals do not follow its regions closely enough to
    /// learn one from, and Wilderness keeps them.
    ///
    /// Levels, health, armour and attacks follow Add_world_spawn_areas. region_spawns.csv,
    /// region_creatures.csv and region_creature_actions.csv carry every row with its source;
    /// gen_region_spawns.py made them. Down deletes the closed id ranges.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_region_spawns : Migration
    {
        private const uint IdMin = 540001;
        private const uint IdMax = 549999;
        private const uint ActionIdMin = 55001;
        private const uint ActionIdMax = 55999;

        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new RegionCreatureActionPreloader(),
            new RegionCreaturePreloader(),
            new RegionCreatureStatsPreloader(),
            new RegionSpawnpoolPreloader()
        };

        private static readonly string[] Tables =
        {
            SpawnPoolEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id between {IdMin} and {IdMax};");
            }

            migrationBuilder.Sql($"delete from {CreatureActionEntry.TableName} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}
