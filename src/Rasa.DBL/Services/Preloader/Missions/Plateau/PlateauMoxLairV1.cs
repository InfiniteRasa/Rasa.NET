using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Mox Lair west of Fort Defiance (map 1497) for Crystal Blue Persuasion (954) and Alpha Mox (1027):
    /// the server had no Mox there, and its Mox Alpha Male stood on the cliff top over it, off the navmesh.
    ///
    /// Sourced: "hunting Mox in the Mox Lair to the west of Fort Defiance" (client 5183); "Kill the Mox
    /// Alpha Male in the Mox Lair west of Fort Defiance" (client 5684); "The Mox are found in front of and
    /// inside the cave marked on your map" (TaRapedia Crystal Blue Persuasion); "The Alpha Mox is in the
    /// back of the cave" (TaRapedia Alpha Mox); "In a cave before Empower logos (-443,1080)" (Ellatha Mox
    /// Alpha Male). The client's map has a crystal cavern on x -443 west of Fort Defiance - a tunnel room
    /// (TerraTunnelRoomEndV02, -443, 384, 797), Foreas cavern crystals, stalactites and stalagmites - and
    /// behind it an Eloh corridor with a Logos dispenser (ArchElohLogosDispenserBase, -443, 380, 881). The
    /// cavern's floor is 371-379 m, under 460-540 m of terrain.
    ///
    /// Reconstructed (ESTIMATE): three pools of the server's Plateau Mox (540016, level 28) in the cavern -
    /// one at its mouth, two inside - 2-3 each, respawn 300 s, radius 6; the Mox Alpha Male (520053) moves
    /// from (-447.01, 529.98, 1048.25) to the cavern's back before the Logos corridor. Each place is on the
    /// navmesh's ground, reached from Trinity Bridge. Down takes out the pools and puts the Alpha back.
    ///
    /// Ids: pools 650036-650038, free before.
    /// </summary>
    public static class PlateauMoxLairV1
    {
        public const uint MapContextId = 1497;
        public const uint Mox = 540016;                 // Mox - Plateau
        public const uint AlphaMale = 520053;           // Mox Alpha Male - Plateau, creature and pool
        public const uint FirstPool = 650036, LastPool = 650038;
        public const uint RespawnSeconds = 300, Radius = 6;

        /// <summary>(pool, x, y, z): the mouth, then inside.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] Pools =
        {
            (650036, -443, 376.25, 760),
            (650037, -438, 371.28, 790),
            (650038, -448, 376.44, 810),
        };

        public static readonly (double X, double Y, double Z) AlphaBefore = (-447.01, 529.98, 1048.25);
        public static readonly (double X, double Y, double Z) AlphaAt = (-443, 379.28, 822);

        public static IEnumerable<string> Up()
        {
            foreach (var pool in Pools)
                yield return Pool(pool.Id, pool.X, pool.Y, pool.Z, MapContextId, Mox, 2, 3, RespawnSeconds, Radius);
            yield return PlateauFortDefianceNpcsV1.MoveSql(AlphaMale, AlphaAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return PlateauFortDefianceNpcsV1.MoveSql(AlphaMale, AlphaBefore);
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool};";
        }
    }
}
