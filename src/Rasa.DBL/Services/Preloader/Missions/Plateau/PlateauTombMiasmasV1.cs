using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Tomb Miasmas and Tomb Miasma Bulls at Vogren's Tomb on Mount Reverence (map 1497) for Graveyard
    /// Shift (933): the server had no Bull and no miasma there.
    ///
    /// Sourced: "Kill twelve Tomb Miasmas and six Elder Tomb Miasmas at the Forean burial ground atop
    /// Mount Reverence" (client 5017); names "Tomb Miasma" 8761 and "Tomb Miasma Bull" 8762
    /// (creaturenamelanguage); Mount Reverence region marker 61 (-755, 461, 497), Vogren's Tomb marker
    /// 74 (-669, 467, 540) (uimapmarker). "Miasma are found at Vogren's Tomb. Typically only 4-5 bulls
    /// spawn at one time. Respawn rate is about 5 minutes." (TaRapedia Graveyard Shift).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Both are the client's Creature_Miasma class 6170: the client has no other miasma class that
    ///    is not Bane or Infected. The Tomb Miasma has the level 30 Pools miasma's stats (540004); the
    ///    Bull is level 31 with 1.5 times its hit points and damage.
    ///  - Three pools of 2-3 miasmas and two of 2 Bulls west of the tomb, on the navmesh's ground (flat
    ///    within 3 m for 20 m round it, reached from Trinity Bridge), respawn 300 s, radius 15.
    ///
    /// Ids: creatures 650022-650023, actions 650022-650025, pools 650025-650029, free before.
    /// Down takes out only these rows.
    /// </summary>
    public static class PlateauTombMiasmasV1
    {
        public const uint MapContextId = 1497;
        public const uint MiasmaClass = 6170;           // Creature_Miasma
        public const uint GasCloud = 56011;             // every miasma's miasma_gas_cloud

        public const uint TombMiasma = 650022, TombMiasmaName = 8761, TombMiasmaLevel = 30;
        public const uint TombMiasmaHitPoints = 2652, TombMiasmaArmor = 140;
        public const uint Bull = 650023, BullName = 8762, BullLevel = 31;
        public const uint BullHitPoints = 3978, BullArmor = 150;
        public const uint FirstAction = 650022, LastAction = 650025;
        public const uint FirstPool = 650025, LastPool = 650029;
        public const uint RespawnSeconds = 300, Radius = 15;

        /// <summary>(pool, x, y, z, creature, min, max): west of Vogren's Tomb.</summary>
        public static readonly (uint Id, double X, double Y, double Z, uint Creature, uint Min, uint Max)[] Pools =
        {
            (650025, -700, 464.64, 540, TombMiasma, 2, 3),
            (650026, -700, 464.4, 510, TombMiasma, 2, 3),
            (650027, -700, 463.9, 570, TombMiasma, 2, 3),
            (650028, -730, 464.59, 540, Bull, 2, 2),
            (650029, -730, 463.88, 570, Bull, 2, 2),
        };

        public static IEnumerable<string> Up()
        {
            yield return Action(650022, "Tomb Miasma - Plateau miasma_melee", 205, 1, 0.5, 3.0, 1000, 660, 92, 185, 3);
            yield return Action(650023, "Tomb Miasma - Plateau miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 123, 154, 3);
            yield return Action(650024, "Tomb Miasma Bull - Plateau miasma_melee", 205, 1, 0.5, 3.0, 1000, 660, 138, 278, 3);
            yield return Action(650025, "Tomb Miasma Bull - Plateau miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 185, 231, 3);

            yield return Creature(TombMiasma, "Tomb Miasma - Plateau", MiasmaClass, TombMiasmaLevel, TombMiasmaHitPoints,
                TombMiasmaName, 650022, 650023, GasCloud);
            yield return Stat(TombMiasma, TombMiasmaHitPoints, TombMiasmaArmor);
            yield return Creature(Bull, "Tomb Miasma Bull - Plateau", MiasmaClass, BullLevel, BullHitPoints,
                BullName, 650024, 650025, GasCloud);
            yield return Stat(Bull, BullHitPoints, BullArmor);

            foreach (var pool in Pools)
                yield return Pool(pool.Id, pool.X, pool.Y, pool.Z, MapContextId, pool.Creature, pool.Min, pool.Max,
                    RespawnSeconds, Radius);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({TombMiasma}, {Bull});";
            yield return $"DELETE FROM creature WHERE id IN ({TombMiasma}, {Bull});";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {FirstAction} AND {LastAction};";
        }
    }
}
