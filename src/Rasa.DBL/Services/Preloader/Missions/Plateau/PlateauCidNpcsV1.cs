using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Fort Defiance's murder case NPCs (Blue Flu 1014, Murder Most Foul 895, Looking For Clues 928).
    ///
    /// New public NPC:
    ///  - MP Lieutenant Parkman (name 8768), Blue Flu's giver, in the command post: TaRapedia /loc -72, 404,
    ///    951; Ten Ton Hammer "around -70, 404, 951". On the floor there, 404.15.
    /// New templates, the missions' scenes spawn them for the player alone (user decision: personal):
    ///  - Unknown Forean Suspect (name 8577, package 966) near the crime scene: TaRapedia /loc -158, 398, 1011.
    ///  - Pendrik (name 8633, package 1017) and two Craps Players (name 8634) in the craps game. Remick (client
    ///    5080): "one of the cement bunkers at Fort Defiance. More than likely, the one closest to New Velon".
    ///    ESTIMATE: the fort's east room with the low tables (ArchHumGenObjTableV03, 15-39, 399.7, 917-929).
    ///  - Pendrik's hostile copy, for "Attempt to arrest Pendrik" (TaRapedia: "you will need to kill him. He is
    ///    very easy"): the server cannot turn a friendly creature hostile.
    /// Looks (ESTIMATE): the Foreans as the client's NPC_Forean_Gunner (7033) with the goo gun (6238), the
    /// hostile copy its Redshirt_Forean_Gunner (6239) with the Forean Gunner's shot 1/116 (as creature 51),
    /// level 30, 1500 hit points; Parkman and the players in the officer's uniform of the generated Plateau
    /// NPCs (PlateauPerduNpcsV1).
    ///
    /// Two NPCs move from the Fort Defiance dropship marker, where MissionNpcSpawnpoolPreloader put them:
    ///  - MP Sergeant Norton (510187) to the supply warehouse at the back of the fort: TaRapedia /loc -187, 398,
    ///    1016; Ten Ton Hammer -185, 398, 1016; client 5552 "guarding the supply warehouse at the back of the
    ///    fort". On its floor, 398.75. He lies down (NpcPose.LyingDown 7; user decision): client 5550 "appears to
    ///    be unconscious", 4700 "MURDERED".
    ///  - CID Detective Crais (510175) to CID headquarters: TaRapedia /loc -230, 421, 938, beside Commander
    ///    Provost (510174, -224, 421.08, 935). On its floor, 421.
    /// Down puts both back and takes the pose away.
    ///
    /// Ids: creatures 650043-650047, action 650030, pool 650052, free before. Plain SQL.
    /// </summary>
    public static class PlateauCidNpcsV1
    {
        public const uint MapContextId = 1497;
        public const uint MaleClass = PlateauPerduNpcsV1.MaleClass;
        public const uint ForeanClass = 7033;            // NPC_Forean_Gunner
        public const uint HostileForeanClass = 6239;     // Redshirt_Forean_Gunner
        public const uint GooGun = 6238;                 // Weapon_Creature_Forean_GooGun

        public const uint Parkman = 650043, ParkmanPool = 650052;
        public const uint Suspect = 650044, SuspectPackage = 966;
        public const uint Pendrik = 650045, PendrikPackage = 1017;
        public const uint HostilePendrik = 650046, PendrikShot = 650030;
        public const uint CrapsPlayer = 650047;
        public const uint Norton = 510187, NortonPackage = 959;
        public const uint Provost = 510174, ProvostPackage = 962;
        public const uint Crais = 510175, CraisPackage = 963;
        public const byte LyingDown = 7;

        public static readonly (double X, double Y, double Z) ParkmanAt = (-72, 404.15, 951);
        public static readonly (double X, double Y, double Z) ProvostAt = (-224, 421.08, 935);

        /// <summary>(pool, where it was, where it goes).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Moves =
        {
            (Norton, (-190.1, 423.45, 865.7), (-187, 398.75, 1016)),
            (Crais, (-182.5, 419.28, 873.3), (-230, 421, 938)),
        };

        /// <summary>The generated Plateau NPCs' officer uniform (slot, class, color), as PlateauPerduNpcsV1's.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] Outfit =
        {
            (2, 4021, 4294934528),      // NPC_Clothing_Officer_1_Boots
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (14, 9781, 4278655809),     // AvatarSwap hair
            (15, 4023, 4294934528),     // NPC_Clothing_Officer_1_Torso
            (16, 4022, 4294934528),     // NPC_Clothing_Officer_1_Legs
            (17, 24008, 4286690539),    // AvatarSwap face, Caucasian
        };

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Human(Parkman, 8768, "MP Lieutenant Parkman - Fort Defiance"))
                yield return sql;
            yield return Pool(ParkmanPool, ParkmanAt.X, ParkmanAt.Y, ParkmanAt.Z, MapContextId, Parkman, 1, 1, 20, 0);

            foreach (var sql in Forean(Suspect, 8577, SuspectPackage, "Unknown Forean Suspect - Fort Defiance"))
                yield return sql;
            foreach (var sql in Forean(Pendrik, 8633, PendrikPackage, "Pendrik - Fort Defiance craps game"))
                yield return sql;
            yield return Action(PendrikShot, "Pendrik - Plateau weapon 1/116", 1, 116, 1.0, 20.0, 2500, 0, 40, 70, 1);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({HostilePendrik}, 'Pendrik resisting arrest - Fort Defiance', {HostileForeanClass}, 0, 30, 1500, 8633, 9, 5, " +
                $"{PendrikShot}, 0, 0, 0, 0, 0, 0, 0);";
            yield return Stat(HostilePendrik, 1500, 70);
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({HostilePendrik}, 13, {GooGun}, 1);";
            foreach (var sql in Human(CrapsPlayer, 8634, "Craps Player - Fort Defiance craps game"))
                yield return sql;

            foreach (var move in Moves)
                yield return PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.At);
            yield return $"INSERT INTO spawnpool_pose (id, pose) VALUES ({Norton}, {LyingDown});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool_pose WHERE id = {Norton};";
            foreach (var move in Moves)
                yield return PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.Before);
            yield return $"DELETE FROM spawnpool WHERE id = {ParkmanPool};";
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {Parkman} AND {CrapsPlayer};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Parkman} AND {CrapsPlayer};";
            yield return $"DELETE FROM creature_stat WHERE id = {HostilePendrik};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Parkman} AND {CrapsPlayer};";
            yield return $"DELETE FROM creature_action WHERE id = {PendrikShot};";
        }

        private static IEnumerable<string> Human(uint id, uint nameId, string comment)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({id}, '{comment}', {MaleClass}, 1, 32, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, {slot}, {cls}, {color});";
        }

        private static IEnumerable<string> Forean(uint id, uint nameId, uint package, string comment)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({id}, '{comment}', {ForeanClass}, 1, 30, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, 13, {GooGun}, 1);";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
        }
    }
}
