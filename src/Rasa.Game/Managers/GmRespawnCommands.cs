using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// .respawn: a game master's look at how a spawn pool comes back (RespawnPace).
    ///
    ///  - .respawn: the pool of the creature you have selected - its kind, its row's time and the pace it keeps,
    ///    what of it stands, and when it is back if it is down or short.
    ///  - .respawn &lt;poolId&gt;: the same for a pool of the map you are on, by its spawnpool id - one that is down, whose
    ///    creatures cannot be selected.
    ///  - .respawn &lt;poolId&gt; now: makes that pool due at once, as if its time were up (GameMaster): the next pass of
    ///    the spawn pools sets it down, or tops it up.
    ///
    /// Reading is at Observer, as .cover is; making a pool due changes the world and is at GameMaster.
    /// </summary>
    public static class GmRespawnCommands
    {
        public const GmLevel Level = GmLevel.Observer;

        public static void Respawn(Client gm, string[] parts)
        {
            var player = gm?.Player;
            var map = player?.MapChannel;

            if (map == null)
                return;

            var pool = PoolFor(map, player, parts, out var problem);

            if (pool == null)
            {
                CommunicatorManager.Instance.SystemMessage(gm, problem);
                return;
            }

            if (parts.Length > 2 && parts[2] == "now")
            {
                if (!ChatCommandsManager.HasLevel(gm, GmLevel.GameMaster))
                {
                    CommunicatorManager.Instance.SystemMessage(gm, $".respawn <poolId> now needs account level {(byte)GmLevel.GameMaster}.");
                    return;
                }

                pool.UpdateTimer = pool.RespawnTime;
                CommunicatorManager.Instance.SystemMessage(gm, $"pool {pool.DbId} is due: it comes back on the next pass.");
                return;
            }

            foreach (var line in RespawnPace.Describe(pool))
                CommunicatorManager.Instance.SystemMessage(gm, line);
        }

        /// <summary>The pool asked about: by its id among the map's, or the selected creature's.</summary>
        internal static SpawnPool PoolFor(MapChannel map, Manifestation player, IReadOnlyList<string> parts, out string problem)
        {
            problem = null;

            if (parts.Count > 1)
            {
                if (!uint.TryParse(parts[1], out var id))
                {
                    problem = "usage: .respawn [poolId [now]]";
                    return null;
                }

                var pool = map.SpawnPools.FirstOrDefault(candidate => candidate?.DbId == id);

                if (pool == null)
                    problem = $"No spawn pool {id} on this map.";

                return pool;
            }

            var creature = EntityManager.Instance.GetCreature(player.Target);

            if (creature?.SpawnPool == null)
                problem = "Select a creature of a spawn pool, or name the pool: .respawn <poolId>";

            return creature?.SpawnPool;
        }
    }
}
