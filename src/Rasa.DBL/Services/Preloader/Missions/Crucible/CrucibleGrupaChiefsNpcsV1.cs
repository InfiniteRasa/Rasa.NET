using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 2 NPCs, Ligo Crucible (map 1993), for the Grupa chiefs' missions (The Boss Of All Bosses 1161, The
    /// Protection Racket 1146, Smoking Guns 1148, Junkyard Dogs 1149): the new Bunko and Merchant Yeg (1146) and Vargo's pet
    /// Flare Gasher Roscoe (1146, a scene creature: no pool); the upstream rows set 2 uses moved to their places (Kappa Grupa
    /// Lohen, Merchant Cugin, Corporal Fiorella, Labbna Sigal Haggus); and the two local teleporters of the Kappa Grupa's
    /// office.
    ///
    /// Ids (the Crucible block 770000-770999, CrucibleOutpostIntrepidNpcsV1; set 1 used 770000-770002):
    ///  - 770003 Bunko, 770004 Merchant Yeg (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 770005 Roscoe (creature, creature_stat);
    ///  - teleporters 909 (lower) and 910 (upper) and their two local_teleporter_link rows (teleporter ids stay below 1000,
    ///    PlateauMaligoBaseV1; 909-945 free in the World DB and the code, MarshesLogosResearchNpcsV1 took 907-908).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9070 "Bunko", 9136 "Merchant Yeg", 9140 "Roscoe" (the client has a
    ///    second 9150 "Roscoe"). Packages (client objectiveconversation, 1.0): (1146,1) 1224 Bunko, (1146,4) 1272 Yeg, (1146,2)
    ///    1269 Cugin, (1146,3) and (1146,6) 1271 Vargo, (1161,1) 1222 Lohen, (1148,1) 1296 Fiorella, (1148,2) 1232 Haggus,
    ///    (1148,3) 1295 Melching (World DB rows have them).
    ///  - Places: the merchants "in the Staal Centrum" (client 1146 texts; World DB map_region 20336 "Staal Centrum
    ///    (panopticon)"); Lohen "at the top most floor in Staal. Easiest way to him is to use local teleporter close to the
    ///    Staal waypoint teleporter" (TaRapedia Kappa Grupa Lohen, 0.5); the client's map markers "Local Teleporter: Kappa
    ///    Gruppa's Office" at (-206.6, 184, 317.7) and (-215.2, 245.2, 319.8) and its two UsableTwoStateBrannTeleporter statics
    ///    there (MapUsables runs them); Fiorella "at Outpost Intrepid", Haggus "in the Labbna Quarter just outside of Staal"
    ///    (client 1148 texts, 1.0).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, D3): Lohen, Haggus (Staal cluster, -20 to -11, 188, 318-329), Fiorella
    /// (Outpost Intrepid cluster, 944, 171, 185) stood in the placeholder clusters at the client's map labels; Cugin stood at
    /// y 137, 44 m under the Staal Centrum floor; the office's two Brann teleporters had no local teleporter rows, so the
    /// office (its own island on the navmesh and the client floor) could not be reached.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Bunko and Yeg copies of Vargo's row and look (Brann class 7776), level 35 (the level of 1146); 4500 hit points and
    ///    armour 300 as the set 1 NPCs; friendly, standing still.
    ///  - Roscoe a copy of the Crucible Flaregasher 540030 (class 7338, its two attacks), hostile, level 35, 3000 hit points,
    ///    armour 150 (TaRapedia: "This is an easy fight").
    ///  - Every place the nearest point to its source on the server navmesh and the client floor, at least 2 m from a wall,
    ///    nothing over the head, seen from all 16 places 2 m round it, walked to from its area's waypoint (probe of
    ///    2026-11-07): Lohen 6 m from the upper teleporter in the office; Cugin at the Centrum's weapon counter
    ///    (-171.3, 196.5, 278.7), Yeg at the one at (-154.7, 196.5, 348.9), Bunko on the panopticon floor 10 m from the lower
    ///    teleporter, Vargo where upstream has him; Fiorella 8.6 m from DeLisi; Haggus 9.6 m from Rigs. Each 200 m or more
    ///    from the nearest hostile pool's circle and 57 m or more from a map link (World DB).
    ///  - The two teleporters: type 1, class 0 (the client draws the Brann statics; the tower teleporters 570-573 are rows of
    ///    class 0 too), at the statics' origins, linked both ways.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows and puts the upstream rows back as they were.
    /// </summary>
    public static class CrucibleGrupaChiefsNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1, Hostile = 0;

        public const uint Bunko = 770003, BunkoName = 9070, BunkoPackage = 1224;
        public const uint Yeg = 770004, YegName = 9136, YegPackage = 1272;
        public const uint Roscoe = 770005, RoscoeName = 9140;
        public const uint MerchantLevel = 35, RoscoeLevel = 35, RoscoeHitPoints = 3000, RoscoeArmor = 150;
        public const uint NextFreeId = 770006;

        /// <summary>The upstream rows set 2 uses: the merchants, Lohen, the three receivers of Smoking Guns, the Flaregasher.</summary>
        public const uint Lohen = 510024, Cugin = 510032, Vargo = 510033, Fiorella = 510022, Haggus = 510026, Melching = 510018;
        public const uint LohenPackage = 1222, CuginPackage = 1269, VargoPackage = 1271;
        public const uint FiorellaPackage = 1296, HaggusPackage = 1232, MelchingPackage = 1295;
        public const uint Flaregasher = 540030;

        /// <summary>The Kappa Grupa's office teleporters: by the Staal waypoint, and in the office.</summary>
        public const uint LowerPad = 909, UpperPad = 910, LocalTeleporter = 1, PadClass = 0;
        public static readonly (double X, double Y, double Z) LowerPadAt = (-206.738, 183.3619, 317.945);
        public static readonly (double X, double Y, double Z) UpperPadAt = (-214.899, 244.4991, 319.7714);

        public static readonly (double X, double Y, double Z) BunkoAt = (-197, 180.68, 319);
        public static readonly (double X, double Y, double Z) YegAt = (-153.7, 196.91, 346.9);
        /// <summary>Vargo stays where upstream has him (on the Centrum floor); Melching too (the Awol Camp).</summary>
        public static readonly (double X, double Y, double Z) VargoAt = (-192.5, 180.5, 264.20001220703125);
        public static readonly (double X, double Y, double Z) MelchingAt = (-945.9000244140625, 309.6000061035156, 246.89999389648438);
        /// <summary>Where Roscoe comes out at Vargo's call: on the Centrum floor west of Vargo, 22 m from him and ReachClearance (20 m) or more from every Crucible NPC, so that he does not attack Vargo (probe 2026-11-07, ESTIMATE).</summary>
        public static readonly (double X, double Y, double Z) RoscoeAt = (-214, 180.68, 258);

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Lohen, (-13.899999618530273, 187.85, 328.6000061035156), (-215, 244.91, 326)),
            (Cugin, (-188.8000030517578, 137.0800018310547, 264.20001220703125), (-174.3, 196.68, 280.2)),
            (Fiorella, (944.4000244140625, 170.86, 185.0), (959, 144.68, 111)),
            (Haggus, (-20.399999618530273, 187.77, 326.5), (-400, 249.68, 270))
        };

        /// <summary>The upstream pools set 2 talks to and leaves where they are.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] Kept =
        {
            (Vargo, VargoAt), (Melching, MelchingAt)
        };

        /// <summary>Where an NPC of set 2 stands: a new one's place, a moved upstream pool's new one, or a kept one's.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Bunko => BunkoAt,
            Yeg => YegAt,
            _ when Kept.Any(kept => kept.Pool == pool) => Kept.Single(kept => kept.Pool == pool).At,
            _ => Moves.Single(move => move.Pool == pool).Now
        };

        /// <summary>The new NPCs: id, comment, name, package, place (copies of Vargo's row and look).</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Bunko, "Bunko - Staal Centrum (1146)", BunkoName, BunkoPackage, BunkoAt),
            (Yeg, "Merchant Yeg - Staal Centrum (1146)", YegName, YegPackage, YegAt)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {MerchantLevel}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Vargo};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Vargo};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Roscoe}, 'Roscoe - Vargo''s pet Flare Gasher, Staal Centrum (1146)', class_id, {Hostile}, {RoscoeLevel}, " +
                         $"{RoscoeHitPoints}, {RoscoeName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {Flaregasher};";
            yield return Stat(Roscoe, RoscoeHitPoints, RoscoeArmor);
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
            yield return Pad(LowerPad, "Local Teleporter: Kappa Gruppa's Office (Staal Centrum)", LowerPadAt);
            yield return Pad(UpperPad, "Local Teleporter: Kappa Gruppa's Office (the office)", UpperPadAt);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({LowerPad}, {UpperPad});";
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({UpperPad}, {LowerPad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({LowerPad}, {UpperPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({LowerPad}, {UpperPad});";
            foreach (var (pool, before, _) in Moves)
                yield return Place(pool, before);
            yield return $"DELETE FROM creature_stat WHERE id = {Roscoe};";
            yield return $"DELETE FROM creature WHERE id = {Roscoe};";
            foreach (var (id, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporter}, {S(description)}, {N(at.X)}, {N(at.Y)}, {N(at.Z)}, 0, {MapContextId});";

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
