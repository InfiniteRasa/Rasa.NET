using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The AFS control-point turrets put back on their mounts (Afs_turret_bases). Afs_turret_mount_heights
    /// raised all 84 fence placements 2.56 m, the height of a dedicated platform's gun mount, and left them
    /// floating: the standard gun's origin is the foot of its round base, not a platform's top.
    ///
    /// Round bases: 30 placements stand at the end of the diagonal walkway a fence piece sends out
    /// (ArchHumOutpostFenceCornerV03/V11). The piece builds the gun's round base in, at its
    /// Human_base_turret bone, 6.24 m up; the collision disc under it is at the same height. The
    /// seed had these guns 1.5-2.1 m off the base's centre and 0.28-0.31 m under its floor, so they
    /// move to the bone: x, y and z from the 1.16.5.0 client's .map and .geo files. The gun's model
    /// starts 0.45 m over its origin, on the 0.5 m base. The Bane turret on the same spot moves to
    /// the same x and z, and stands on top of the AFS base: its mortar launcher starts 0.12 m over its
    /// origin, so 0.38 m over the bone.
    ///
    /// Wall tops: the other 54 stand on fence corners with no base, and go back down to the wall
    /// top the seed measured for them (the 2.56 m raise undone). Their Bane turrets were never moved.
    ///
    /// The twelve dedicated platforms (ArchHumanBaseTurretPlatformV01) are untouched.
    ///
    /// Guarded like Afs_turret_mount_heights: a pool moves only from the position it was given, as
    /// the creature it was given, and is recorded so Down moves back only what Up moved and still
    /// stands where Up put it.
    /// </summary>
    public static class AfsTurretBases
    {
        public const uint AfsTurretClass = 4064;    // Emplacement_AFS_Turret_Standard
        public const uint BaneTurretClass = 7482;   // Emplacement_Bane_Turret_Standard
        public const double RaisedBy = 2.56;
        public const double BaneOverBase = 0.38;

        /// <summary>A spot on a round base: its two pools and creatures, the seed's position, and the base's bone.</summary>
        public sealed record Base(uint AfsPool, uint BanePool, uint Map, uint AfsCreature, uint BaneCreature,
            double SeedX, double SeedY, double SeedZ, double X, double Y, double Z);

        /// <summary>A spot on a wall top with no base: the AFS pool and its seed position.</summary>
        public sealed record Wall(uint Pool, uint Map, uint Creature, double X, double Y, double Z);

        public static readonly Base[] Bases =
        {
            new(551121, 551122, 1220, 550018, 551001, 159.20, 168.96, -28.21, 158.112, 169.241, -27.012),
            new(551123, 551124, 1220, 550018, 551001, 236.86, 168.94, -28.14, 237.988, 169.241, -27.112),
            new(551133, 551134, 1244, 550016, 551003, -222.81, 177.94, -758.81, -223.988, 178.241, -759.888),
            new(551135, 551136, 1244, 550016, 551003, -222.80, 177.96, -681.21, -223.888, 178.241, -680.012),
            new(551137, 551138, 1244, 550016, 551003, -97.21, 177.96, -758.80, -96.112, 178.241, -759.988),
            new(551139, 551140, 1244, 550016, 551003, -97.14, 177.94, -681.14, -96.012, 178.241, -680.112),
            new(551141, 551142, 1244, 550016, 551003, -170.80, 177.96, -661.21, -171.888, 178.241, -660.012),
            new(551143, 551144, 1244, 550016, 551003, -125.14, 177.94, -661.14, -124.012, 178.241, -660.112),
            new(551145, 551146, 1244, 550016, 551003, -170.43, 177.94, -778.43, -171.988, 178.241, -779.888),
            new(551147, 551148, 1244, 550016, 551003, -125.40, 177.94, -778.61, -124.112, 178.241, -779.988),
            new(551155, 551156, 1454, 550023, 551006, -370.80, 221.96, -269.21, -371.888, 222.240, -268.011),
            new(551179, 551180, 1497, 550024, 551004, -435.80, 381.93, 421.79, -436.888, 382.211, 422.988),
            new(551181, 551182, 1497, 550024, 551004, -334.14, 381.91, 421.86, -333.012, 382.211, 422.888),
            new(551217, 551218, 1761, 550008, 551009, 393.18, 265.94, -30.81, 392.000, 266.241, -31.890),
            new(551219, 551220, 1761, 550008, 551009, 333.18, 265.94, -18.81, 332.000, 266.241, -19.890),
            new(551229, 551230, 1764, 550011, 551007, 219.86, 437.94, 291.86, 220.988, 438.241, 292.888),
            new(551231, 551232, 1764, 550011, 551007, 142.20, 437.96, 291.79, 141.112, 438.241, 292.988),
            new(551237, 551238, 1764, 550011, 551007, -89.14, 435.94, 563.86, -88.012, 436.241, 564.889),
            new(551239, 551240, 1764, 550011, 551007, -89.21, 435.96, 470.20, -88.112, 436.241, 469.012),
            new(551241, 551242, 1764, 550011, 551007, -174.80, 435.96, 563.79, -175.888, 436.241, 564.988),
            new(551243, 551244, 1764, 550011, 551007, -174.81, 435.94, 470.19, -175.988, 436.241, 469.112),
            new(551249, 551250, 1911, 550004, 551011, 658.20, 389.96, -127.21, 657.112, 390.241, -126.012),
            new(551251, 551252, 1911, 550004, 551011, 730.20, 389.96, -127.21, 729.112, 390.241, -126.012),
            new(551253, 551254, 1911, 550004, 551011, 847.86, 389.94, -127.14, 848.988, 390.241, -126.112),
            new(551255, 551256, 1911, 550004, 551011, 847.79, 389.96, -252.80, 848.888, 390.241, -253.988),
            new(551257, 551258, 1911, 550004, 551011, 686.19, 389.94, -240.81, 685.012, 390.241, -241.888),
            new(551283, 551284, 2051, 550019, 551014, -553.80, 206.96, -294.21, -554.888, 207.241, -293.012),
            new(551285, 551286, 2051, 550019, 551014, -448.14, 206.94, -294.14, -447.012, 207.241, -293.112),
            new(551287, 551288, 2051, 550019, 551014, -400.14, 206.94, -322.14, -399.012, 207.241, -321.112),
            new(551289, 551290, 2051, 550019, 551014, -400.21, 206.96, -467.80, -399.112, 207.241, -468.988),
        };

        public static readonly Wall[] Walls =
        {
            new(551101, 1148, 550015, -189.71, 65.00, 11.27),
            new(551103, 1148, 550015, -189.75, 65.00, 42.50),
            new(551105, 1148, 550015, -189.50, 64.82, 123.39),
            new(551107, 1148, 550015, -200.00, 64.82, 157.50),
            new(551109, 1148, 550015, -306.14, 65.00, 11.26),
            new(551111, 1148, 550015, -252.50, 64.82, 169.50),
            new(551113, 1148, 550015, -342.36, 65.10, 79.40),
            new(551115, 1148, 550015, -342.18, 65.00, 150.82),
            new(551125, 1220, 550018, 145.74, 172.40, -79.10),
            new(551127, 1220, 550018, 254.34, 172.40, -75.23),
            new(551129, 1220, 550018, 144.70, 172.60, -127.19),
            new(551131, 1220, 550018, 254.74, 172.40, -110.69),
            new(551151, 1454, 550023, -327.85, 223.82, -241.50),
            new(551153, 1454, 550023, -288.25, 223.81, -241.50),
            new(551157, 1454, 550023, -249.50, 223.81, -284.25),
            new(551163, 1497, 550024, 75.85, 378.00, 394.28),
            new(551165, 1497, 550024, 75.86, 378.00, 441.87),
            new(551167, 1497, 550024, 126.00, 378.09, 383.64),
            new(551169, 1497, 550024, 126.00, 377.82, 452.50),
            new(551171, 1497, 550024, -408.85, 383.82, 469.50),
            new(551173, 1497, 550024, -361.00, 383.82, 469.50),
            new(551175, 1497, 550024, -349.11, 384.00, 356.87),
            new(551177, 1497, 550024, -420.75, 384.00, 356.87),
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
            new(551221, 1761, 550008, 301.64, 268.09, 8.15),
            new(551223, 1761, 550008, 301.65, 268.10, 44.00),
            new(551225, 1764, 550011, 146.86, 440.00, 229.27),
            new(551227, 1764, 550011, 215.50, 439.82, 229.14),
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
            new(551291, 2051, 550019, -538.60, 208.90, -459.18),
        };

        /// <summary>One pool's move: from where it stands to where it goes, for a creature of a class.</summary>
        public sealed record Move(uint Pool, uint Map, uint Creature, uint Class, (double X, double Y, double Z) From, (double X, double Y, double Z) To);

        public static IEnumerable<Move> Moves()
        {
            foreach (var b in Bases)
            {
                yield return new Move(b.AfsPool, b.Map, b.AfsCreature, AfsTurretClass,
                    (b.SeedX, b.SeedY + RaisedBy, b.SeedZ), (b.X, b.Y, b.Z));
                yield return new Move(b.BanePool, b.Map, b.BaneCreature, BaneTurretClass,
                    (b.SeedX, b.SeedY, b.SeedZ), (b.X, b.Y + BaneOverBase, b.Z));
            }

            foreach (var w in Walls)
                yield return new Move(w.Pool, w.Map, w.Creature, AfsTurretClass, (w.X, w.Y + RaisedBy, w.Z), (w.X, w.Y, w.Z));
        }

        public static IEnumerable<string> Up()
        {
            yield return "CREATE TABLE IF NOT EXISTS afs_turret_base_repair (pool_id INTEGER NOT NULL PRIMARY KEY);";

            foreach (var move in Moves())
            {
                yield return $"INSERT INTO afs_turret_base_repair (pool_id) SELECT id FROM spawnpool WHERE {Guard(move, move.From)} " +
                    "AND NOT EXISTS (SELECT 1 FROM afs_turret_base_repair r WHERE r.pool_id=spawnpool.id);";
                yield return Update(move, move.From, move.To);
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var move in Moves())
                yield return Update(move, move.To, move.From);

            yield return "DROP TABLE afs_turret_base_repair;";
        }

        private static string Update(Move move, (double X, double Y, double Z) from, (double X, double Y, double Z) to) =>
            $"UPDATE spawnpool SET pos_x={F(to.X)}, pos_y={F(to.Y)}, pos_z={F(to.Z)} WHERE {Guard(move, from)} " +
            "AND EXISTS (SELECT 1 FROM afs_turret_base_repair r WHERE r.pool_id=spawnpool.id);";

        private static string Guard(Move move, (double X, double Y, double Z) at) =>
            $"id={move.Pool} AND map_context_id={move.Map} AND creature_1_Id={move.Creature} " +
            $"AND ABS(pos_x-({F(at.X)}))<0.001 AND ABS(pos_y-({F(at.Y)}))<0.001 AND ABS(pos_z-({F(at.Z)}))<0.001 " +
            $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id=spawnpool.creature_1_Id AND c.class_id={move.Class})";

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
