using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// spawnpool.respown_time is in tenths of a second (SpawnPoolManager: 900 is a minute and a
    /// half). The reconstructed Wilderness pools (WildernessWaveAPopulationV1.Spawn and the
    /// migrations that use it) were all written 20, meant as seconds, so what is killed in them
    /// was back two seconds later: the three Pinhole Falls Caverns egg-layers most of all.
    ///
    /// The hostile ones are put at the Wilderness pools' own times (WildernessSpawnpoolPreloader:
    /// squads 90 s, packs 120 s, overseers 300 s) and the mortars at a Bane mortar's
    /// (BaneMortars.Respawn, 300 s); the egg-layers, which Mama Miasma! needs three of, at 20 s. The friendly ones - the escort ranger, Ranger Milpas,
    /// Sgt. Pierre - and the Treeback herd stay at 20, as a mission NPC's pool is
    /// (MissionNpcSpawnpoolPreloader).
    ///
    /// A pool is changed only if it is still at 20, so one a GM has set by hand stays as they
    /// set it; Down puts back 20 only where this put its value.
    /// </summary>
    public static class WildernessRespawns
    {
        /// <summary>What they were written with: 2 s.</summary>
        public const uint Written = 20;

        public const uint EggLayer = 200;
        public const uint Squad = 900;
        public const uint Pack = 1200;
        public const uint Overseer = 3000;

        public static readonly IReadOnlyList<(uint Pool, uint Respawn, string What)> Pools = new (uint, uint, string)[]
        {
            (630040, EggLayer, "Pinhole Miasma Egg-Layer"),
            (630041, EggLayer, "Pinhole Miasma Egg-Layer"),
            (630042, EggLayer, "Pinhole Miasma Egg-Layer"),
            (630046, Squad, "Light Bender Sniper"),
            (630047, Squad, "Light Bender Sniper"),
            (630048, Squad, "Light Bender Sniper"),
            (630049, Squad, "Light Bender Sniper"),
            (630050, Squad, "Light Bender Sniper"),
            (630051, Squad, "Light Bender Sniper"),
            (630071, Squad, "Wilderness Lightbender"),
            (630072, Squad, "Wilderness Lightbender"),
            (630073, Squad, "Wilderness Lightbender"),
            (630074, Squad, "Wilderness Lightbender"),
            (630100, Pack, "Forean Machina"),
            (630101, Pack, "Forean Machina"),
            (630102, Pack, "Forean Machina"),
            (630120, Pack, "Wilderness Predator"),
            (630121, Pack, "Wilderness Predator"),
            (630076, BaneMortars.Respawn, "Wilderness Bane mortar"),
            (630077, BaneMortars.Respawn, "Wilderness Bane mortar"),
            (630078, BaneMortars.Respawn, "Wilderness Bane mortar"),
            (630079, BaneMortars.Respawn, "Wilderness Bane mortar"),
            (630130, Overseer, "Thrax Overseer Skeev"),
        };

        public static IEnumerable<string> Up() =>
            Pools.GroupBy(pool => pool.Respawn).Select(group =>
                $"update {SpawnPoolEntry.TableName} set respown_time = {group.Key} " +
                $"where respown_time = {Written} and id in ({string.Join(", ", group.Select(pool => pool.Pool))});");

        public static IEnumerable<string> Down() =>
            Pools.GroupBy(pool => pool.Respawn).Select(group =>
                $"update {SpawnPoolEntry.TableName} set respown_time = {Written} " +
                $"where respown_time = {group.Key} and id in ({string.Join(", ", group.Select(pool => pool.Pool))});");
    }
}
