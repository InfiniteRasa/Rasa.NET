using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The AFS turrets Add_afs_turrets put on the maps' mounts (TurretSpawnpoolPreloader), turned to
    /// face as their mounts do (Turret_facings).
    ///
    /// A map stores a piece's orientation as a quaternion (w, x, y, z). The turret patch read it as
    /// (x, y, z, w), which for a mount turned by a yaw t about the vertical gives pi - t: a mount
    /// facing 0 got a turret facing pi, one facing pi/2 or -pi/2 the right way by chance. Each row
    /// here is the turret's pool, its map, the facing it was given and its mount's own, read from
    /// the 1.16.5.0 client's .map with the quaternion in its order.
    ///
    /// Left out: the 21 already facing their mounts' way; the three by Alia Das, which
    /// Alia_das_npcs and Wilderness_post_npcs turn; and five whose mounts are not stood upright on
    /// the map (Mires 550037 and 550038, Warnet Caverns 550081, the Temporal Chamber 550120,
    /// Retread Caves 550127: their quaternions tip them over), whose facing has no one reading.
    ///
    /// Each turret turns only from the facing it was given; Down turns back only one still facing
    /// its mount's way. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class TurretFacings
    {
        public sealed record Facing(uint Pool, uint Map, double Was, double Is);

        public static readonly Facing[] Turrets =
        {
            new(550001, 1734, -2.3562, -0.7854),
            new(550002, 1734, 3.1416, 0.0),
            new(550003, 1734, 3.1416, 0.0),
            new(550005, 1734, 1.1592, 1.9824),
            new(550006, 1993, 3.1416, 0.0),
            new(550007, 1993, 3.1416, 0.0),
            new(550008, 1977, 1.9843, 1.1573),
            new(550009, 1977, -1.9427, -1.1989),
            new(550010, 1977, -1.1573, -1.9843),
            new(550011, 1911, 0.7854, 2.3562),
            new(550012, 1911, 0.0, 3.1416),
            new(550013, 1911, 0.7854, 2.3562),
            new(550015, 1911, 0.0, 3.1416),
            new(550016, 1911, 0.8818, 2.2598),
            new(550017, 1911, -2.3562, -0.7854),
            new(550018, 1911, -2.3562, -0.7854),
            new(550019, 1911, 1.0746, 2.067),
            new(550020, 1911, -2.3562, -0.7854),
            new(550021, 1911, -2.3562, -0.7854),
            new(550023, 2028, 3.1416, 0.0),
            new(550025, 2028, 3.1416, 0.0),
            new(550026, 2028, 3.1416, 0.0),
            new(550027, 2203, 0.2873, 2.8543),
            new(550028, 2203, 1.8581, 1.2835),
            new(550029, 2203, 2.6435, 0.4981),
            new(550030, 1761, -2.1943, -0.9473),
            new(550031, 1761, -2.1943, -0.9473),
            new(550032, 1761, 3.0601, 0.0815),
            new(550033, 1761, -2.1943, -0.9473),
            new(550034, 1761, 3.1416, 0.0),
            new(550035, 1761, 3.1416, 0.0),
            new(550036, 1761, 3.1416, 0.0),
            new(550039, 1759, 3.1416, 0.0),
            new(550040, 1759, 3.1416, 0.0),
            new(550041, 1759, 3.1416, 0.0),
            new(550042, 1764, -2.3562, -0.7854),
            new(550043, 1764, 3.1416, 0.0),
            new(550044, 1764, 3.1416, 0.0),
            new(550045, 1764, 1.4923, 1.6493),
            new(550046, 1764, 1.4923, 1.6493),
            new(550047, 1764, 3.1416, 0.0),
            new(550048, 1764, 3.1416, 0.0),
            new(550049, 1764, 0.0785, 3.0631),
            new(550050, 1764, 0.0, 3.1416),
            new(550052, 1764, 0.7854, 2.3562),
            new(550053, 1764, -2.3562, -0.7854),
            new(550054, 1764, -2.3562, -0.7854),
            new(550059, 1985, 3.1416, 0.0),
            new(550060, 2327, 2.6189, 0.5227),
            new(550061, 2327, 3.1416, 0.0),
            new(550062, 2375, 2.6189, 0.5227),
            new(550063, 2375, 3.1416, 0.0),
            new(550064, 1148, -0.0314, -3.1102),
            new(550065, 1148, 3.1416, 0.0),
            new(550068, 1148, 3.1416, 0.0),
            new(550069, 1148, -2.1991, -0.9425),
            new(550070, 1148, 3.1416, 0.0),
            new(550071, 1148, 3.1416, 0.0),
            new(550074, 1244, 3.1416, 0.0),
            new(550075, 1244, 3.1416, 0.0),
            new(550076, 1244, 3.1416, 0.0),
            new(550077, 1244, 3.1416, 0.0),
            new(550078, 1244, 3.1416, 0.0),
            new(550079, 1244, 3.1416, 0.0),
            new(550080, 1384, 3.1416, 0.0),
            new(550085, 1430, -2.042, -1.0996),
            new(550086, 1430, -2.8274, -0.3142),
            new(550087, 1430, 2.3562, 0.7854),
            new(550088, 1430, 3.1416, 0.0),
            new(550089, 1430, -2.042, -1.0996),
            new(550091, 2051, -0.1571, -2.9845),
            new(550092, 2051, 1.4923, 1.6493),
            new(550093, 2051, 1.2566, 1.885),
            new(550094, 2051, -2.3562, -0.7854),
            new(550095, 2051, -0.0785, -3.0631),
            new(550096, 2051, 1.4923, 1.6493),
            new(550097, 2051, -0.8639, -2.2777),
            new(550098, 2051, -0.8639, -2.2777),
            new(550099, 2051, -0.1571, -2.9845),
            new(550100, 2051, 3.1416, 0.0),
            new(550101, 2051, 0.7854, 2.3562),
            new(550103, 2051, 3.1416, 0.0),
            new(550107, 2047, 3.1416, 0.0),
            new(550108, 1454, 0.5092, 2.6324),
            new(550109, 1454, 2.8654, 0.2762),
            new(550110, 1454, 3.1416, 0.0),
            new(550111, 1454, 3.1416, 0.0),
            new(550112, 1454, -1.8729, -1.2687),
            new(550113, 1497, 3.1416, 0.0),
            new(550114, 1497, 3.1416, 0.0),
            new(550115, 1497, 0.3034, 2.8382),
            new(550116, 1497, -0.6572, -2.4844),
            new(550117, 1497, 3.1416, 0.0),
            new(550118, 1497, 3.1416, 0.0),
            new(550119, 1497, 3.1416, 0.0),
            new(550123, 2029, 0.0, 3.1416),
            new(550124, 2029, 0.0, 3.1416),
            new(550126, 1304, 3.1416, 0.0),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var t in Turrets)
                yield return Turn(t, t.Was, t.Is);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var t in Turrets)
                yield return Turn(t, t.Is, t.Was);
        }

        private static string Turn(Facing t, double from, double to) =>
            $"UPDATE spawnpool SET rotation = {N(to)} WHERE id = {t.Pool} AND map_context_id = {t.Map} AND ABS(rotation - {N(from)}) < 0.001;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
