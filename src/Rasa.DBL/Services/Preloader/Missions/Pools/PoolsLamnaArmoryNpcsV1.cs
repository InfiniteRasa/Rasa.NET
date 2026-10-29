using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Lamna Armory (Lamnas Armory, squad instance 1465) for its three missions, Bane Battle Plans (896), Operation
    /// Overlord (898) and Blow Them All Up (903): the three givers inside the entrance, the three key bosses of 898 and the
    /// Overlord with his Kael. The server had only the Lamna Medical Team (hospital, vendor) and one Thrax pool there.
    ///
    /// Sourced: client names 8454 "Captain Arimoro", 4386 "Scout Bromfield", 8582 "Colonel Herring", 8888 "Gaite", 8885
    /// "Kwel", 8886 "Fearce", 8571 "Commander Vanduahl", 7722 "Kael" (creaturenamelanguage); the client has no package for
    /// any of the three givers. TaRapedia Colonel Herring: Brann, AFS, male, level 36, "just inside the instance entrance".
    /// Ten Ton Hammer Lamna Armory guide: on entry Bromfield, Arimoro and Herring stand together; Gaite a level 36 Thrax
    /// grenadier, Kewl a level 36 rifleman, Fearce a level 36 lightbender; the Overlord Commander Vanduahl a level 36 hunter
    /// with a level 35 Kael pet. TaRapedia Operation Overlord: "a Thrax grenadier in the chambers immediately west of the
    /// mission giver; a standard Thrax infantry (black carapace) located in the testing labs below; and a Lightbender
    /// sniper located in the Storage area ... each of these three respawns rather rapidly"; "Careful, he's got a pet Kael
    /// in there." The door sets a player down at (409.1, 9.6, -15.9) (map_link 28).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The givers: AFS, male, level 36 (Herring's), the Snake Pit's officer uniform (a copy of Riggins's look), in the
    ///    entrance hall 13-21 m inside the door; packages server-only ids (their own), as Riggins's.
    ///  - The key bosses: public pools (they respawn), each a copy of the Mires creature of its kind at level 36 with three
    ///    times its hit points (as Monkey Wrench's officer), at places on the navmesh of the rooms TaRapedia names; own
    ///    respawn 180 s. In the squad's copy a cleared pool comes back on the squad clock instead (SquadInstanceState, live
    ///    45 min), as Monkey Wrench's officer: a key is a drop for the killer only, while 898 is open. Fearce: the Lightbender class 7120 (Light Bender Sniper) with its two attacks at level 36 damage (the
    ///    grenadier's). The engineers round Fearce are left out.
    ///  - Vanduahl: a copy of Hunter Lieutenant - Marshes (level 36) with twice its hit points and without its pet call;
    ///    his Kael a copy of the Ustor Yard Kael at level 35. Both personal (Operation Overlord's scene), no pool.
    ///
    /// Ids: creatures 760213-760220, pools 760213-760218, actions 760038-760039, free before. Plain SQL, valid on SQLite
    /// and MySQL; down takes out only these rows.
    /// </summary>
    public static class PoolsLamnaArmoryNpcsV1
    {
        public const uint MapContextId = 1465;          // adv_foreas_valverde_pools_test_weapons_center
        public const uint MaleClass = Plateau.PlateauMaligoBaseV1.MaleClass;
        public const uint GiverLevel = 36;
        public const uint OutfitOf = PoolsSnakePitNpcsV1.Riggins;

        public const uint Arimoro = 760213, ArimoroName = 8454, ArimoroPackage = 760213;
        public const uint Bromfield = 760214, BromfieldName = 4386, BromfieldPackage = 760214;
        public const uint Herring = 760215, HerringName = 8582, HerringPackage = 760215;

        public const uint Gaite = 760216, GaiteName = 8888;
        public const uint Kwel = 760217, KwelName = 8885;
        public const uint Fearce = 760218, FearceName = 8886;
        public const uint Vanduahl = 760219, VanduahlName = 8571;
        public const uint Kael = 760220, KaelName = 7722;
        public const uint BossLevel = 36, KaelLevel = 35;
        public const uint BossHitPoints = 4460 * 3, BossArmor = 170;
        public const uint VanduahlHitPoints = 10406 * 2, VanduahlArmor = 340;
        public const uint KaelHitPoints = 9000, KaelArmor = 210;
        public const uint BossRespawn = 1800;           // spawnpool.respown_time, tenths of a second: 180 s

        public const uint GrenadierClass = 9354, ThraxClass = 20757, LightbenderClass = 7120, HunterClass = 10166, KaelClass = 4046;
        public const uint FearceRange = 760038, FearceQuill = 760039;

        public static readonly (double X, double Y, double Z) DoorArrival = (409.1057, 9.5831, -15.9087);
        public static readonly (double X, double Y, double Z) BromfieldAt = (396, 9.9, -13.5);
        public static readonly (double X, double Y, double Z) ArimoroAt = (392, 9.8, -18.5);
        public static readonly (double X, double Y, double Z) HerringAt = (388, 9.75, -13.5);

        /// <summary>The command room west of the givers, where the battle plans are.</summary>
        public static readonly (double X, double Y, double Z) GaiteAt = (320, 7.92, -16);
        /// <summary>The testing labs below.</summary>
        public static readonly (double X, double Y, double Z) KwelAt = (272, -24.15, 16);
        /// <summary>The storage area.</summary>
        public static readonly (double X, double Y, double Z) FearceAt = (88, 9.61, -224);

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Giver(Arimoro, "Captain Arimoro - Lamna Armory", ArimoroName, ArimoroPackage, ArimoroAt))
                yield return sql;
            foreach (var sql in Giver(Bromfield, "Scout Bromfield - Lamna Armory", BromfieldName, BromfieldPackage, BromfieldAt))
                yield return sql;
            foreach (var sql in Giver(Herring, "Colonel Herring - Lamna Armory", HerringName, HerringPackage, HerringAt))
                yield return sql;

            // Gaite: Thrax Grenadier - Mires; Kwel: Thrax Soldier - Mires; their attacks shared.
            yield return Creature(Gaite, "Gaite - Lamna Armory (Operation Overlord, Security Key)", GrenadierClass, BossLevel, BossHitPoints,
                GaiteName, 53086, 53087);
            yield return Stat(Gaite, BossHitPoints, BossArmor);
            yield return Creature(Kwel, "Kwel - Lamna Armory (Operation Overlord, Weaponsmith Key)", ThraxClass, BossLevel, BossHitPoints,
                KwelName, 53084, 53085);
            yield return Stat(Kwel, BossHitPoints, BossArmor);
            yield return Action(FearceRange, "Fearce - Lamna Armory lightbender range", 1, 149, 0.5, 60, 1500, 0, 190, 285, 0);
            yield return Action(FearceQuill, "Fearce - Lamna Armory lightbender_quill", 202, 1, 1, 10, 4000, 2266, 190, 285, 1);
            yield return Creature(Fearce, "Fearce - Lamna Armory (Operation Overlord, Quartermaster Key)", LightbenderClass, BossLevel,
                BossHitPoints, FearceName, FearceRange, FearceQuill);
            yield return Stat(Fearce, BossHitPoints, BossArmor);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Fearce}, slot_id, Class_id, color FROM creature_appearance WHERE id = 630046;";

            // Vanduahl: Hunter Lieutenant - Marshes without hunter_pet; his Kael: the Ustor Yard Kael's attacks.
            yield return Creature(Vanduahl, "Commander Vanduahl - Lamna Armory, the Overlord (Operation Overlord)", HunterClass, BossLevel,
                VanduahlHitPoints, VanduahlName, 53082, 53083);
            yield return Stat(Vanduahl, VanduahlHitPoints, VanduahlArmor);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Vanduahl}, slot_id, Class_id, color FROM creature_appearance WHERE id = 531054;";
            yield return Creature(Kael, "Kael - Lamna Armory, pet of the Overlord (Operation Overlord)", KaelClass, KaelLevel, KaelHitPoints,
                KaelName, 53135, 53136, 54011);
            yield return Stat(Kael, KaelHitPoints, KaelArmor);

            yield return Pool(Gaite, GaiteAt.X, GaiteAt.Y, GaiteAt.Z, MapContextId, Gaite, 1, 1, BossRespawn, 0);
            yield return Pool(Kwel, KwelAt.X, KwelAt.Y, KwelAt.Z, MapContextId, Kwel, 1, 1, BossRespawn, 0);
            yield return Pool(Fearce, FearceAt.X, FearceAt.Y, FearceAt.Z, MapContextId, Fearce, 1, 1, BossRespawn, 0);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var id in new[] { Fearce, Kwel, Gaite })
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
            foreach (var id in new[] { Kael, Vanduahl, Fearce, Kwel, Gaite })
            {
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
            yield return $"DELETE FROM creature_action WHERE id IN ({FearceRange}, {FearceQuill});";
            foreach (var id in new[] { Herring, Bromfield, Arimoro })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        /// <summary>A giver in the Snake Pit's officer uniform, on the Armory's map.</summary>
        private static IEnumerable<string> Giver(uint id, string comment, uint name, uint package, (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {MaleClass}, 1, {GiverLevel}, 1000, {name}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
        }
    }
}
