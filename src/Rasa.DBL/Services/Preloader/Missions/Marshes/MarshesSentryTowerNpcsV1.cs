using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 5 (the sentry tower, Far West Watch) NPCs, Valverde Marshes (map 1454): Sergeant McDonald on the
    /// higher Forean tree platform by the AFS sentry tower between the two control points (1874 giver and receiver), Warrior
    /// Dikah on the raised platform of Far West Watch (1875 giver), Lt. Kiang inside the sentry tower (1875 receiver, 1876 and
    /// 1877 giver and receiver), and Overseer Nelg (1877's boss; no pool: the mission sets him down for the player). The
    /// server had none of them.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760316-760399 free): 760316 McDonald, 760317 Dikah, 760318 Kiang (creature, npc_package, spawn pool each; McDonald and
    /// Kiang creature_appearance too, Dikah his weapon); 760319 Nelg (creature, creature_stat).
    ///
    /// Sourced: client names 10327 "Sergeant McDonald", 10325 "Warrior Dikah", 10326 "Lt. Kiang", 10339 "Overseer Nelg"
    /// (creaturenamelanguage). The client has no objectiveconversation for 1874-1877, so no client package. TaRapedia NPC
    /// pages (cache): Kiang -115, 231, -319.5 "Inside the AFS sentry tower", McDonald -124, 224, -307.6 "On the higher of the
    /// Forean tree platforms" (both "Between the 2 CPs"), Dikah -521, 222, -179 "On raised platform" (Far West Watch). Ten Ton
    /// Hammer Eastern Marshes: Dikah -523, 222, -176; Kiang's watch tower -116, 230, -319; McDonald -125, 224, -311; "the
    /// outpost where Overseer Nelg lives at 170,224,55". Client 1877 offer: Nelg "dropshipped on top of a building ... east of
    /// Palados". World DB: the Marshes overseers (520032-520035, class 10504, level 38, 4300 hit points, armour 390); Sergeant
    /// Elway 510169 and Lieutenant Morrison 510162 (Falcon Hold, class 3846, six appearance slots); the tower teleporter 573
    /// "Tower Teleport: Research Points" at the sentry tower's foot (-117.6, 217.3, -323.9).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - McDonald a copy of Sergeant Elway's row and look, Kiang of Lieutenant Morrison's (the server's AFS sergeant and
    ///    lieutenant at Falcon Hold), with their names; server-only packages (their ids), as Horsh and Brazos.
    ///  - Dikah a friendly Forean Spearman (7034) with the spear (10532), as set 1's Warrior Veska, at level 37 (the set's).
    ///  - At TaRapedia's /loc, heights from the navmesh (probe of 2026-11-02). Kiang's tower floor (230.88) is a navmesh island
    ///    of its own (no walk from the ground; the player climbs in the client), 14 m above the tower teleporter.
    ///  - Nelg a copy of Overseer Nivvik's row (520034: class 10504, his attacks), level 38 and 12000 hit points (the triage),
    ///    armour 390 (the Marshes overseers'). At Ten Ton Hammer's point inside his post (navmesh 224.68; an island of its own,
    ///    15 m from the walked-to deck east of it), not on the roof: the client's "on top of a building" is the crew's photo.
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); down takes out only these rows.
    /// </summary>
    public static class MarshesSentryTowerNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;

        public const uint McDonald = 760316, McDonaldName = 10327, McDonaldPackage = 760316;
        public const uint Elway = 510169;               // Sergeant Elway, Falcon Hold: McDonald's row and look
        public static readonly (double X, double Y, double Z) McDonaldAt = (-124, 224.08, -307.6);

        public const uint Dikah = 760317, DikahName = 10325, DikahPackage = 760317;
        public const uint DikahClass = 7034, DikahWeapon = 10532;   // NPC_Forean_Spearman, the spear (as Warrior Veska)
        public const uint DikahLevel = 37, DikahHitPoints = 1000, Friendly = 1;
        public static readonly (double X, double Y, double Z) DikahAt = (-521, 222.09, -179);

        public const uint Kiang = 760318, KiangName = 10326, KiangPackage = 760318;
        public const uint Morrison = 510162;            // Lieutenant Morrison, Falcon Hold: Kiang's row and look
        public static readonly (double X, double Y, double Z) KiangAt = (-115, 230.88, -319.5);

        public const uint Nelg = 760319, NelgName = 10339;
        public const uint Nivvik = 520034;              // Overseer Nivvik - Marshes: Nelg's row (class 10504, attacks)
        public const uint NelgLevel = 38, NelgHitPoints = 12000, NelgArmor = 390;
        public static readonly (double X, double Y, double Z) NelgAt = (170, 224.68, 55);

        public static IEnumerable<string> Up()
        {
            yield return Copy(McDonald, "Sergeant McDonald - Marshes, sentry tower (1874)", McDonaldName, Elway);
            yield return Look(McDonald, Elway);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({McDonald}, {McDonaldPackage}, 'Sergeant McDonald - Marshes');";
            yield return Pool(McDonald, McDonaldAt.X, McDonaldAt.Y, McDonaldAt.Z, MapContextId, McDonald, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Dikah}, 'Warrior Dikah - Marshes, Far West Watch (1875)', {DikahClass}, {Friendly}, {DikahLevel}, " +
                         $"{DikahHitPoints}, {DikahName}, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Dikah}, 13, {DikahWeapon}, 1);";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Dikah}, {DikahPackage}, 'Warrior Dikah - Marshes');";
            yield return Pool(Dikah, DikahAt.X, DikahAt.Y, DikahAt.Z, MapContextId, Dikah, 1, 1, 20, 0);

            yield return Copy(Kiang, "Lt. Kiang - Marshes, sentry tower (1875, 1876, 1877)", KiangName, Morrison);
            yield return Look(Kiang, Morrison);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Kiang}, {KiangPackage}, 'Lt. Kiang - Marshes');";
            yield return Pool(Kiang, KiangAt.X, KiangAt.Y, KiangAt.Z, MapContextId, Kiang, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Nelg}, 'Overseer Nelg - Marshes (1877, the mission sets him down)', class_id, faction, {NelgLevel}, " +
                         $"{NelgHitPoints}, {NelgName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {Nivvik};";
            yield return Stat(Nelg, NelgHitPoints, NelgArmor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({McDonald}, {Dikah}, {Kiang});";
            yield return $"DELETE FROM npc_package WHERE id IN ({McDonald}, {Dikah}, {Kiang});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({McDonald}, {Dikah}, {Kiang});";
            yield return $"DELETE FROM creature_stat WHERE id = {Nelg};";
            yield return $"DELETE FROM creature WHERE id IN ({McDonald}, {Dikah}, {Kiang}, {Nelg});";
        }

        private static string Copy(uint id, string comment, uint name, uint source) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, action2, " +
            $"action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {source};";

        private static string Look(uint id, uint source) =>
            "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
            $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {source};";
    }
}
