using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The other thirteen zones get their Bane camps and creature lairs, the way Divide got its own
    /// in Add_spawn_areas: 790 pools on the areas mined from the client's map placements, across
    /// Abyss, Ashen Desert, Crucible, Descent, Howling Maw, Incline, Marshes, Mires, Palisades, Plains,
    /// Plateau, Pools and Thunderhead; 106 creature rows, one per role and zone; and 163 creature
    /// actions, one per role, zone and attack.
    ///
    /// What is new against Divide is the attacks. Divide's garrison reuses the fork's old rows,
    /// whose damage was typed for level 5-15; a level 44 Atta with them would be a nuisance. Every
    /// garrison creature here has its own creature_action rows, built from the client's own CR_*
    /// creature action for its family (the Kael's ground pound and smash, the Linker's chest and
    /// hand blasts, the Atta soldier's acid spit and bite, the Treeback's stomp and howl, the
    /// Warnet's zap, the Warden Bot's laser and shock) at the argument the client keys by tier, with
    /// the client's range, reuse and windup. Damage is the client's base at that argument scaled by
    /// level as the server scales abilities, times 0.25 - which puts a minion's hit at 4-5% of a
    /// same-level player's base health, where the fork's Thrax, Hunter and Filcher rows already
    /// sit. The rank and file's guns are the weapon pairs the fork's rows use, set straight to that
    /// share. One constant in gen_world_spawns.py retunes all of it.
    ///
    /// The remaining lairs the mining found and this does not place: Magmonix skeletons (Crucible,
    /// Incline; the client has no CR_MAGMONIX action to give them), the Eloh Temples spawner
    /// usables (scripted), and the corpse fields (staged fights, not spawns). Wilderness keeps its
    /// hand-placed pools; Earth, Bootcamp and the wargame maps are not zones.
    ///
    /// Sources per row: world_spawns.csv, world_creatures.csv, world_creature_actions.csv.
    /// Down deletes the closed id ranges these rows occupy.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_world_spawn_areas : Migration
    {
        private const uint IdMin = 531001;
        private const uint IdMax = 539999;
        private const uint ActionIdMin = 53001;
        private const uint ActionIdMax = 53999;

        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new WorldCreatureActionPreloader(),
            new WorldCreaturePreloader(),
            new WorldCreatureStatsPreloader(),
            new WorldSpawnpoolPreloader()
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
