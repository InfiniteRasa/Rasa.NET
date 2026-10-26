using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>Standard AFS guns on CP fence mounts need the same 2.56 m mount offset as dedicated platforms.</summary>
    public static class AfsTurretMountHeights
    {
        public const double MountOffset = 2.56;
        public sealed record Mount(uint Pool, uint Map, uint Creature, double X, double SurfaceY, double Z);

        // These 84 fence placements used the collision surface as the gun origin. The twelve
        // dedicated-platform CP placements and the original non-CP turret pools already add it.
        // Keep this migration manifest independent of the historical seed.
        public static readonly Mount[] Mounts =
        {
            new(551101, 1148, 550015, -189.71, 65.00, 11.27),
            new(551103, 1148, 550015, -189.75, 65.00, 42.50),
            new(551105, 1148, 550015, -189.50, 64.82, 123.39),
            new(551107, 1148, 550015, -200.00, 64.82, 157.50),
            new(551109, 1148, 550015, -306.14, 65.00, 11.26),
            new(551111, 1148, 550015, -252.50, 64.82, 169.50),
            new(551113, 1148, 550015, -342.36, 65.10, 79.40),
            new(551115, 1148, 550015, -342.18, 65.00, 150.82),
            new(551121, 1220, 550018, 159.20, 168.96, -28.21),
            new(551123, 1220, 550018, 236.86, 168.94, -28.14),
            new(551125, 1220, 550018, 145.74, 172.40, -79.10),
            new(551127, 1220, 550018, 254.34, 172.40, -75.23),
            new(551129, 1220, 550018, 144.70, 172.60, -127.19),
            new(551131, 1220, 550018, 254.74, 172.40, -110.69),
            new(551133, 1244, 550016, -222.81, 177.94, -758.81),
            new(551135, 1244, 550016, -222.80, 177.96, -681.21),
            new(551137, 1244, 550016, -97.21, 177.96, -758.80),
            new(551139, 1244, 550016, -97.14, 177.94, -681.14),
            new(551141, 1244, 550016, -170.80, 177.96, -661.21),
            new(551143, 1244, 550016, -125.14, 177.94, -661.14),
            new(551145, 1244, 550016, -170.43, 177.94, -778.43),
            new(551147, 1244, 550016, -125.40, 177.94, -778.61),
            new(551151, 1454, 550023, -327.85, 223.82, -241.50),
            new(551153, 1454, 550023, -288.25, 223.81, -241.50),
            new(551155, 1454, 550023, -370.80, 221.96, -269.21),
            new(551157, 1454, 550023, -249.50, 223.81, -284.25),
            new(551163, 1497, 550024, 75.85, 378.00, 394.28),
            new(551165, 1497, 550024, 75.86, 378.00, 441.87),
            new(551167, 1497, 550024, 126.00, 378.09, 383.64),
            new(551169, 1497, 550024, 126.00, 377.82, 452.50),
            new(551171, 1497, 550024, -408.85, 383.82, 469.50),
            new(551173, 1497, 550024, -361.00, 383.82, 469.50),
            new(551175, 1497, 550024, -349.11, 384.00, 356.87),
            new(551177, 1497, 550024, -420.75, 384.00, 356.87),
            new(551179, 1497, 550024, -435.80, 381.93, 421.79),
            new(551181, 1497, 550024, -334.14, 381.91, 421.86),
            new(551183, 1734, 550001, -438.50, 289.82, 142.14),
            new(551185, 1734, 550001, -438.50, 289.82, 190.00),
            new(551187, 1734, 550001, -469.00, 289.82, 220.50),
            new(551189, 1734, 550001, -492.60, 289.82, 220.50),
            new(551191, 1734, 550001, -492.61, 290.10, 111.65),
            new(551193, 1734, 550001, -469.16, 290.00, 111.84),
            new(551195, 1734, 550001, -535.14, 290.00, 177.88),
            new(551197, 1734, 550001, -535.13, 290.00, 154.24),
            new(551199, 1759, 550009, 634.13, 268.30, -311.83),
            new(551201, 1759, 550009, 518.75, 268.00, -311.33),
            new(551207, 1761, 550008, 333.84, 268.00, 79.84),
            new(551209, 1761, 550008, 428.40, 267.82, 110.50),
            new(551211, 1761, 550008, 452.00, 267.82, 110.50),
            new(551213, 1761, 550008, 462.50, 267.82, 60.39),
            new(551215, 1761, 550008, 423.88, 268.00, -26.14),
            new(551217, 1761, 550008, 393.18, 265.94, -30.81),
            new(551219, 1761, 550008, 333.18, 265.94, -18.81),
            new(551221, 1761, 550008, 301.64, 268.09, 8.15),
            new(551223, 1761, 550008, 301.65, 268.10, 44.00),
            new(551225, 1764, 550011, 146.86, 440.00, 229.27),
            new(551227, 1764, 550011, 215.50, 439.82, 229.14),
            new(551229, 1764, 550011, 219.86, 437.94, 291.86),
            new(551231, 1764, 550011, 142.20, 437.96, 291.79),
            new(551237, 1764, 550011, -89.14, 435.94, 563.86),
            new(551239, 1764, 550011, -89.21, 435.96, 470.20),
            new(551241, 1764, 550011, -174.80, 435.96, 563.79),
            new(551243, 1764, 550011, -174.81, 435.94, 470.19),
            new(551249, 1911, 550004, 658.20, 389.96, -127.21),
            new(551251, 1911, 550004, 730.20, 389.96, -127.21),
            new(551253, 1911, 550004, 847.86, 389.94, -127.14),
            new(551255, 1911, 550004, 847.79, 389.96, -252.80),
            new(551257, 1911, 550004, 686.19, 389.94, -240.81),
            new(551259, 1911, 550004, 785.20, 392.00, -248.20),
            new(551263, 1993, 550002, 383.55, 177.40, 338.11),
            new(551265, 1993, 550002, 416.27, 177.40, 339.56),
            new(551267, 1993, 550002, 353.82, 176.00, 359.90),
            new(551269, 1993, 550002, 446.50, 175.82, 360.14),
            new(551271, 1993, 550002, 352.93, 177.17, 377.36),
            new(551273, 1993, 550002, 448.76, 176.00, 370.83),
            new(551275, 2028, 550007, -85.16, 542.00, -178.21),
            new(551277, 2028, 550007, -73.85, 541.82, -89.50),
            new(551279, 2028, 550007, 8.50, 541.82, -171.86),
            new(551281, 2028, 550007, -2.00, 541.82, -89.50),
            new(551283, 2051, 550019, -553.80, 206.96, -294.21),
            new(551285, 2051, 550019, -448.14, 206.94, -294.14),
            new(551287, 2051, 550019, -400.14, 206.94, -322.14),
            new(551289, 2051, 550019, -400.21, 206.96, -467.80),
            new(551291, 2051, 550019, -538.60, 208.90, -459.18),
        };

        // Record only rows this repair actually changes. A preexisting manual correction must
        // not be lowered on rollback merely because it already had the corrected coordinates.
        public static IEnumerable<string> Up()
        {
            yield return "CREATE TABLE IF NOT EXISTS afs_turret_mount_height_repair (pool_id INTEGER NOT NULL PRIMARY KEY);";
            foreach (var mount in Mounts)
            {
                yield return $"INSERT INTO afs_turret_mount_height_repair (pool_id) SELECT id FROM spawnpool WHERE {Guard(mount, mount.SurfaceY)} " +
                    "AND NOT EXISTS (SELECT 1 FROM afs_turret_mount_height_repair r WHERE r.pool_id=spawnpool.id);";
                yield return Update(mount, mount.SurfaceY, mount.SurfaceY + MountOffset);
            }
        }
        public static IEnumerable<string> Down()
        {
            foreach (var mount in Mounts)
                yield return Update(mount, mount.SurfaceY + MountOffset, mount.SurfaceY);
            yield return "DROP TABLE afs_turret_mount_height_repair;";
        }
        private static string Update(Mount mount, double from, double to) =>
            $"UPDATE spawnpool SET pos_y={F(to)} WHERE {Guard(mount, from)} " +
            "AND EXISTS (SELECT 1 FROM afs_turret_mount_height_repair r WHERE r.pool_id=spawnpool.id);";
        private static string Guard(Mount mount, double y) =>
            $"id={mount.Pool} AND map_context_id={mount.Map} AND creature_1_Id={mount.Creature} " +
            $"AND ABS(pos_x-({F(mount.X)}))<0.001 AND ABS(pos_z-({F(mount.Z)}))<0.001 AND ABS(pos_y-({F(y)}))<0.001 " +
            "AND EXISTS (SELECT 1 FROM creature c WHERE c.id=spawnpool.creature_1_Id AND c.class_id=4064)";
        private static string F(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
