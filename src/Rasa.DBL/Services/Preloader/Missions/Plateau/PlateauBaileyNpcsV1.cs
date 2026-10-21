using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The NPCs of A Mystery Unearthed (2016), Symbolism (2017) and Logos: Vortex (1659).
    ///
    /// New public NPCs:
    ///  - General Thaddeus T. Bailey (name 6769, package 636), Fort Defiance command post: TaRapedia /loc -68,
    ///    404, 949. On the floor there, 404.15 (as Parkman, PlateauCidNpcsV1).
    ///  - Receptive Liaison Ridout (name 9939), beside him: TaRapedia /loc -62, 404, 950. On the floor, 404.15.
    ///  - The Hermit (name 4366, package 325), Valverde Pools, Fractured Butte: TaRapedia /loc -430, 921, 115.
    ///    On the ground there, 920.56.
    /// Looks (ESTIMATE): the officer's uniform of the generated Plateau NPCs (PlateauPerduNpcsV1), as Parkman's.
    ///
    /// One NPC moves (user decision): Retread McCormick (510201), whom MissionNpcSpawnpoolPreloader put on
    /// Valverde Pools (-146.7, 863.12, 517; height from StandingHeights), goes to the Marshes: client objective 2017.2 "Speak to Retread
    /// McCormick (Retread Camp: Marshes)"; TaRapedia Retread McCormick: Marshes, Retread City, /loc -268, 216,
    /// -812. On the ground there, 216.86. Down puts him back.
    ///
    /// Ids: creatures 650048-650050, pools 650053-650055, free before. Plain SQL.
    /// </summary>
    public static class PlateauBaileyNpcsV1
    {
        public const uint PlateauMap = 1497, PoolsMap = 1304, MarshesMap = 1454;
        public const uint MaleClass = PlateauPerduNpcsV1.MaleClass;

        public const uint Bailey = 650048, BaileyPool = 650053, BaileyPackage = 636;
        public const uint Ridout = 650049, RidoutPool = 650054;
        public const uint Hermit = 650050, HermitPool = 650055, HermitPackage = 325;
        public const uint McCormick = 510201, McCormickPackage = 968;

        public static readonly (double X, double Y, double Z) BaileyAt = (-68, 404.15, 949);
        public static readonly (double X, double Y, double Z) RidoutAt = (-62, 404.15, 950);
        public static readonly (double X, double Y, double Z) HermitAt = (-430, 920.56, 115);
        public static readonly (double X, double Y, double Z) McCormickBefore = (-146.7, 863.12, 517);
        public static readonly (double X, double Y, double Z) McCormickAt = (-268, 216.86, -812);

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
            foreach (var sql in Human(Bailey, 6769, "General Thaddeus T. Bailey - Fort Defiance", BaileyPackage))
                yield return sql;
            yield return Pool(BaileyPool, BaileyAt.X, BaileyAt.Y, BaileyAt.Z, PlateauMap, Bailey, 1, 1, 20, 0);
            foreach (var sql in Human(Ridout, 9939, "Receptive Liaison Ridout - Fort Defiance", null))
                yield return sql;
            yield return Pool(RidoutPool, RidoutAt.X, RidoutAt.Y, RidoutAt.Z, PlateauMap, Ridout, 1, 1, 20, 0);
            foreach (var sql in Human(Hermit, 4366, "Hermit - Pools, Fractured Butte", HermitPackage))
                yield return sql;
            yield return Pool(HermitPool, HermitAt.X, HermitAt.Y, HermitAt.Z, PoolsMap, Hermit, 1, 1, 20, 0);
            yield return Move(McCormick, McCormickAt, MarshesMap);
        }

        public static IEnumerable<string> Down()
        {
            yield return Move(McCormick, McCormickBefore, PoolsMap);
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {BaileyPool} AND {HermitPool};";
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {Bailey} AND {Hermit};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Bailey} AND {Hermit};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Bailey} AND {Hermit};";
        }

        private static string Move(uint pool, (double X, double Y, double Z) at, uint map) =>
            $"UPDATE spawnpool SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)}, map_context_id = {map} WHERE id = {pool};";

        private static IEnumerable<string> Human(uint id, uint nameId, string comment, uint? package)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({id}, '{comment}', {MaleClass}, 1, 32, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, {slot}, {cls}, {color});";
            if (package.HasValue)
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
        }
    }
}
