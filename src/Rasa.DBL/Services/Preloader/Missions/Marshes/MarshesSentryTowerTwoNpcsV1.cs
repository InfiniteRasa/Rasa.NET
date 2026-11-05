using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 6 (the sentry tower, part two) NPCs, Valverde Marshes (map 1454): Archer Thoung by the sentry tower
    /// between the two control points (1873 giver and receiver), and the four she sends the player to with the marsh sickness
    /// boosts, Pvt. Brady, Archer Zui, Warrior Dahn and Elder Nez, at the tower's foot. The server had none of them.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760320-760399 free): 760320 Thoung, 760321 Brady, 760322 Zui, 760323 Dahn, 760324 Nez (creature, npc_package, spawn
    /// pool, creature_appearance each).
    ///
    /// Sourced: client names 10324 "Archer Thoung", 10335 "Pvt. Brady", 10336 "Archer Zui", 10337 "Warrior Dahn", 10338
    /// "Elder Nez" (creaturenamelanguage); the client's objective conversations of 1873 at packages 2375 (objective 2,
    /// Brady), 2376 (3, Zui), 2377 (4, Dahn), 2378 (5, Nez) (objectiveconversation); none for Thoung. TaRapedia Need a Little
    /// Help Here: Thoung -126.4, 221, -343.4, "the sentry tower between the CPs"; Ten Ton Hammer Eastern Marshes: Archer
    /// Thoung -127, 220, -341, "her". World DB: Private Remick 510188 (class 3846, six appearance slots), the server's AFS
    /// privates; the Forean classes 7036 Archer, 7034 Spearman, 7035 Shaman with their slot-13 weapons (10529 bow, 10532
    /// spear, 10533 staff), as set 1's Alia, Veska and Siva.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Thoung, Zui: friendly Forean Archers (7036, the bow); Dahn a Spearman (7034, the spear), Nez a Shaman (7035, the
    ///    staff; the server's Forean elders are shamans), all at level 37 (the set's). Brady a copy of Private Remick's row
    ///    and look with his name. Thoung a server-only package (her id), as McDonald's.
    ///  - Thoung at TaRapedia's /loc, the four at the tower's foot (the triage), 8-16 m apart, all on the navmesh (probe of
    ///    2026-11-02) and walked to from the Research Point waypoint.
    /// </summary>
    public static class MarshesSentryTowerTwoNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint ForeanLevel = 37, ForeanHitPoints = 1000, Friendly = 1;
        public const uint ArcherClass = 7036, ArcherWeapon = 10529;     // NPC_Forean_Archer, the bow (as Ranger Alia)
        public const uint SpearmanClass = 7034, SpearmanWeapon = 10532; // NPC_Forean_Spearman, the spear (as Warrior Veska)
        public const uint ShamanClass = 7035, ShamanWeapon = 10533;     // NPC_Forean_Shaman, the staff (as Shaman Siva)

        public const uint Thoung = 760320, ThoungName = 10324, ThoungPackage = 760320;
        public static readonly (double X, double Y, double Z) ThoungAt = (-126.875, 221.08, -343.26);

        public const uint Brady = 760321, BradyName = 10335, BradyPackage = 2375;
        public const uint Remick = 510188;              // Private Remick, Wedge Rock Outpost: Brady's row and look
        public static readonly (double X, double Y, double Z) BradyAt = (-110, 217.57, -320);

        public const uint Zui = 760322, ZuiName = 10336, ZuiPackage = 2376;
        public static readonly (double X, double Y, double Z) ZuiAt = (-110, 216.93, -335);

        public const uint Dahn = 760323, DahnName = 10337, DahnPackage = 2377;
        public static readonly (double X, double Y, double Z) DahnAt = (-125, 221.08, -335);

        public const uint Nez = 760324, NezName = 10338, NezPackage = 2378;
        public static readonly (double X, double Y, double Z) NezAt = (-132.11, 221.21, -330.6);

        private static readonly uint[] All = { Thoung, Brady, Zui, Dahn, Nez };

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Forean(Thoung, "Archer Thoung - Marshes, sentry tower (1873)", ArcherClass, ThoungName, ArcherWeapon, ThoungPackage, ThoungAt))
                yield return sql;

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Brady}, 'Pvt. Brady - Marshes, sentry tower (1873)', class_id, faction, level, max_hp, {BradyName}, " +
                         "run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {Remick};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Brady}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Remick};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Brady}, {BradyPackage}, 'Pvt. Brady - Marshes');";
            yield return Pool(Brady, BradyAt.X, BradyAt.Y, BradyAt.Z, MapContextId, Brady, 1, 1, 20, 0);

            foreach (var sql in Forean(Zui, "Archer Zui - Marshes, sentry tower (1873)", ArcherClass, ZuiName, ArcherWeapon, ZuiPackage, ZuiAt))
                yield return sql;
            foreach (var sql in Forean(Dahn, "Warrior Dahn - Marshes, sentry tower (1873)", SpearmanClass, DahnName, SpearmanWeapon, DahnPackage, DahnAt))
                yield return sql;
            foreach (var sql in Forean(Nez, "Elder Nez - Marshes, sentry tower (1873)", ShamanClass, NezName, ShamanWeapon, NezPackage, NezAt))
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            var ids = string.Join(", ", All);
            yield return $"DELETE FROM spawnpool WHERE id IN ({ids});";
            yield return $"DELETE FROM npc_package WHERE id IN ({ids});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({ids});";
            yield return $"DELETE FROM creature WHERE id IN ({ids});";
        }

        private static IEnumerable<string> Forean(uint id, string comment, uint cls, uint name, uint weapon, uint package,
            (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {cls}, {Friendly}, {ForeanLevel}, {ForeanHitPoints}, {name}, 9, 5, " +
                         "0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, 13, {weapon}, 1);";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(',')]}');";
            yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
        }
    }
}
