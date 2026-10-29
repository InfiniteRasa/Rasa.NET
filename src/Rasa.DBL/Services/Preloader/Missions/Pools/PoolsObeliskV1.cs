using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Eloh obelisk in the Bane fort at the western edge of Valverde Pools (map 1304), for Sabotage the Tower (937)
    /// and Bane Jamming (938). The 1.16.5 client's map has no obelisk there (the nearest are at (-379, 924, 181) and
    /// (-222, 860, 540)), and the server had none.
    ///
    /// Sourced: Ten Ton Hammer Pools guide: "Eloh obelisk - Just south of the Snakepit is a small Bane controlled base
    /// guarding an obelisk"; the Pools ToO guide: "the Bane base around the Eloh Obelisk"; 932's terminal text: "a major
    /// installation here centered around a tall structure towards the rear of the fortifications"; 937's log: "the Bane
    /// personnel and their equipment protecting the Eloh Obelisk".
    ///
    /// Reconstructed (ESTIMATE): ArchElohObeliskV01 (client class 10259, mesh 24878, no augmentation: a plain model, as
    /// the .map's obelisks) at the fort's rear, (-848, 791.4, -20) on the navmesh (user, 2026-10-25); public, as a
    /// world_destructible row that PlacedDestructibles puts down as scenery with no state.
    ///
    /// Id: world_destructible 760017, free before. Plain SQL, valid on SQLite and MySQL; down takes out only this row.
    /// </summary>
    public static class PoolsObeliskV1
    {
        public const uint Id = 760017;
        public const uint ObeliskClass = 10259;         // ArchElohObeliskV01
        public static readonly (double X, double Y, double Z) At = (-848, 791.4, -20);

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, " +
                "creature_id, creature_count, side, comment) " +
                $"VALUES ({Id}, {PoolsMissionDataV1.MapContextId}, {ObeliskClass}, {F(At.X)}, {F(At.Y)}, {F(At.Z)}, 0, 0, 0, 0, 0, " +
                "'Eloh obelisk in the Bane fort, western Valverde Pools');";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM world_destructible WHERE id = {Id};";
        }

        private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
