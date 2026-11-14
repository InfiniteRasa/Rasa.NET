using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 11a NPCs and creatures, for Sadistic Sabotage (1445), Lost in the Asylum (1444) and Rehabilitation at
    /// Gunpoint (1402), the Incurables Ward (squad instance 2141). The World DB has no row of Mechanic Shanul, Engineer Dhimand,
    /// the Incurables, Lord Jaikev or Lady Parvatee; Supervisor Sheira 510038 and Larai Sigal Tempa 510028 are upstream.
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-9 used 770000-770044):
    ///  - 770045 Shanul, 770046 Dhimand (creature, creature_appearance, creature_stat, npc_package, spawn pool), in the Ward;
    ///  - 770047 the Incurable Brute (creature_action 770047, creature, creature_stat), 770048 the Incurable Slayer (creature,
    ///    creature_stat): two of each in each of the pools 770047-770055 in the Ward;
    ///  - 770049 Jaikev (pool 770056), 770050 Parvatee (pool 770057) (creature, creature_stat, spawn pool) in the Ward.
    ///  - Moved: Sheira 510038 (off the medic Siebring 500036 and the hospital teleporter's spot) and Tempa 510028 (out of the
    ///    Staal placeholder cluster, to TaRapedia's place).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9550 "Mechanic Shanul", 9614 "Engineer Dhimand", 9539 Jaikev, 9622
    ///    Parvatee, 9521 "Incurable Brute", 9617 "Incurable Slayer". Packages (client objectiveconversation): Shanul 1664
    ///    (1444,1 and 1401,4), Dhimand 1700 (1444,3); Sheira 1640 (1445,1). Classes (client entityclass): 23191
    ///    Redshirt_Brann_Incurable_Male, 25611 its Female, 25547 its Male_Boss, 25617 its Female_Boss; their weapon 23198
    ///    (client weaponclass, action 174, argument 51).
    ///  - Shanul "you will need to recover keycards from Jaikev's lieutenants, Atali and Umank" (client 12684); Dhimand
    ///    "repair the Warden Bots ... I need to shut down those valves" (client 12690). Jaikev a "Boss-level Incurable
    ///    Berserker", Parvatee a "Boss-level Incurable Guardian", "surrounded by several fast moving fighters"; the Incurables
    ///    "melee fighters ... flamethrowers, bladesmen" (TaRapedia 1402, cache, 0.5). Tempa "just outside the Larai Arena",
    ///    -307, 180, 326 (TaRapedia Larai Sigal Tempa, 0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Shanul and Dhimand copies of Lohen's row and look (Brann class 7776, as Cirkin), level 40, 4500 hit points and armour
    ///    300 as the other new NPCs, friendly, standing still.
    ///  - The Incurables: a Brute (male class) and a Slayer (female class) at level 40 with the Ashen Desert level 40
    ///    Technician's 8409 hit points and armour 285 (as the Junkyard's Technicians); one melee attack 770047 of their
    ///    weapon's action 174/51 with the Lasher's 557-779 damage, 2.1 s, reach 0.5-4 m, damage_type 0 (the client's weapon
    ///    table decides). Two Brutes and two Slayers in each of nine pools (radius 5; 770053 and 770055 points): 36, more than the 30 of 1402. Jaikev
    ///    and Parvatee new rows of the boss classes with the same attack at level 42, 12000 hit points and armour 300, each
    ///    one in a point pool in the Throne Room, a pool of four as the guard on each side.
    ///  - The Ward's own respawn 90 s (as the Junkyard's). In the squad's copy the Incurables come back with the copy; Jaikev
    ///    and Parvatee on their 90 s (1402's scene names their pools public-spawn actors).
    ///  - The places (probe 2026-11-07, navmesh and client files): each on the navmesh and a client floor with nothing over the
    ///    head, ReachClearance or more outside every hostile pool's circle for the friendly ones (20 m and more), seen from
    ///    16 of 16 places 2 m round each talk target, walked to from the arrival with no step or 12 m climb the client fails,
    ///    no lava. The Ward's navmesh is in islands (upstream, NOTES-upstream-bugs.md): the arrival's island holds Sheira and
    ///    Shanul; the north part (Dhimand, Paliv, the Throne Room, the north pools) joins it only at the north airlock
    ///    (78, 62.6, 70.5), where the client floor goes on (3.9 m wide, no collision) and the navmesh has a 1 m gap.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows and puts the moved upstream rows back.
    /// </summary>
    public static class CrucibleIncurablesWardNpcsV1
    {
        public const uint Crucible = CrucibleMissionDataV1.MapContextId;
        public const uint Ward = 2141;                  // adv_arieki_ligo_crucible_incurablesward, a squad instance (SquadInstancePolicies)
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1;

        /// <summary>The Ward's arrival (map link 97's destination) and its door back (map link 125, radius 6).</summary>
        public static readonly (double X, double Y, double Z) ArrivalAt = (18.2732, 70.5, -131.3076);
        /// <summary>The Ward's door on the Crucible (map link 97, radius 6), under the Staal Infirmary.</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (-216.0389, 180.5, 419.6967);
        /// <summary>The floor before the door on the Crucible, 8 m outside the door's circle (a marker for the way in).</summary>
        public static readonly (double X, double Y, double Z) DoorWayAt = (-216, 180.83, 400);
        /// <summary>The north airlock: the client walks on; the server navmesh has a 1 m gap between these two points.</summary>
        public static readonly (double X, double Y, double Z) AirlockSouthAt = (78, 62.6, 66), AirlockNorthAt = (78, 62.6, 75);

        public const uint Sheira = 510038, SheiraPackage = 1640;
        public const uint Tempa = 510028;

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Sheira, (-18.0, 62.61000061035156, -99.80000305175781), (-18, 62.61, -88)),
            (Tempa, (-20.399999618530273, 187.77, 319.70001220703125), (-307, 180.79, 326))
        };

        public static (double X, double Y, double Z) SheiraAt => Moves.Single(move => move.Pool == Sheira).Now;
        public static (double X, double Y, double Z) TempaAt => Moves.Single(move => move.Pool == Tempa).Now;

        public const uint NpcLevel = 40;
        public const uint Shanul = 770045, ShanulName = 9550, ShanulPackage = 1664;
        public static readonly (double X, double Y, double Z) ShanulAt = (60, 62.61, -70);
        public const uint Dhimand = 770046, DhimandName = 9614, DhimandPackage = 1700;
        public static readonly (double X, double Y, double Z) DhimandAt = (-40, 94.61, 386);

        /// <summary>The Incurables (1402).</summary>
        public const uint Brute = 770047, BruteName = 9521, BruteClass = 23191;
        public const uint Slayer = 770048, SlayerName = 9617, SlayerClass = 25611;
        public const uint IncurableLevel = 40, IncurableHitPoints = 8409, IncurableArmor = 285;
        public const uint Attack = 770047, AttackAction = 174, AttackArgument = 51;
        public const uint BrutesPerPool = 2, SlayersPerPool = 2, IncurablePoolRadius = 5;
        /// <summary>
        /// The Incurable pools whose radius-5 spawn draw reaches 20-40 m off over the Ward's big navmesh polygons (upstream C11;
        /// CrucibleIncurablesWardTests.TheIncurablePoolsSetTheirCreaturesDownInTheirRoom): points, all four on the centre.
        /// </summary>
        public static readonly uint[] PointPools = { 770053, 770055 };

        /// <summary>An Incurable pool's radius: IncurablePoolRadius, 0 for the PointPools.</summary>
        public static uint RadiusOf(uint pool) => PointPools.Contains(pool) ? 0 : IncurablePoolRadius;

        public static readonly (uint Pool, (double X, double Y, double Z) At)[] IncurablePools =
        {
            (770047, (76, 62.67, 90)), (770048, (76, 63.01, 132)),          // the north corridor
            (770049, (94, 70.66, 178)), (770050, (124, 70.61, 184)),        // the junction room
            (770051, (-22, 94.69, 416)), (770052, (-40, 94.61, 464)),       // the Sanctum hub
            (770053, (94, 86.21, 488)),                                     // the north room
            (770054, (-90, 94.61, 316)), (770055, (-82, 94.7, 348))         // the Throne Room guards
        };

        /// <summary>Lord Jaikev and Lady Parvatee (1402), in the Throne Room.</summary>
        public const uint Jaikev = 770049, JaikevName = 9539, JaikevClass = 25547;
        public const uint Parvatee = 770050, ParvateeName = 9622, ParvateeClass = 25617;
        public const uint BossLevel = 42, BossHitPoints = 12000, BossArmor = 300;
        public const uint JaikevPool = 770056, ParvateePool = 770057;
        public static readonly (double X, double Y, double Z) JaikevAt = (-98, 94.61, 332);
        public static readonly (double X, double Y, double Z) ParvateeAt = (-90, 94.73, 340);

        /// <summary>The Ward pools' own respawn in spawnpool.respown_time's tenths: 90 s, as the Junkyard's.</summary>
        public const uint WardRespawnTenths = CrucibleStaalJunkyardNpcsV1.JunkyardRespawnTenths;
        public const uint NextFreeId = 770058;

        /// <summary>Where an NPC or a creature's pool of set 11a stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Sheira => SheiraAt,
            Tempa => TempaAt,
            Shanul => ShanulAt,
            Dhimand => DhimandAt,
            JaikevPool => JaikevAt,
            ParvateePool => ParvateeAt,
            _ => IncurablePools.Single(entry => entry.Pool == pool).At
        };

        public static IEnumerable<string> Up()
        {
            foreach (var move in Moves)
                yield return Place(move.Pool, move.Now);

            foreach (var (id, comment, name, package, at) in new[]
            {
                (Shanul, "Mechanic Shanul - Incurables Ward dormitory (1444)", ShanulName, ShanulPackage, ShanulAt),
                (Dhimand, "Engineer Dhimand - Incurables Ward, Sanctum (1444)", DhimandName, DhimandPackage, DhimandAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {NpcLevel}, {HitPoints}, {name}, 0, 0, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8 " +
                             $"FROM creature WHERE id = {CrucibleGrupaChiefsNpcsV1.Lohen};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {CrucibleGrupaChiefsNpcsV1.Lohen};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, Ward, id, 1, 1, 20, 0);
            }

            yield return Action(Attack, "Incurable Ward weapon 174/51", AttackAction, AttackArgument, 0.5, 4, 2100, 0, 557, 779, 0);
            yield return Creature(Brute, "Incurable Brute - Incurables Ward (1402)", BruteClass, IncurableLevel, IncurableHitPoints, BruteName, Attack);
            yield return Stat(Brute, IncurableHitPoints, IncurableArmor);
            yield return Creature(Slayer, "Incurable Slayer - Incurables Ward (1402)", SlayerClass, IncurableLevel, IncurableHitPoints, SlayerName, Attack);
            yield return Stat(Slayer, IncurableHitPoints, IncurableArmor);
            yield return Creature(Jaikev, "Jaikev - Incurables Ward, the Throne Room (1402)", JaikevClass, BossLevel, BossHitPoints, JaikevName, Attack);
            yield return Stat(Jaikev, BossHitPoints, BossArmor);
            yield return Creature(Parvatee, "Parvatee - Incurables Ward, the Throne Room (1402)", ParvateeClass, BossLevel, BossHitPoints, ParvateeName,
                Attack);
            yield return Stat(Parvatee, BossHitPoints, BossArmor);

            foreach (var (pool, at) in IncurablePools)
                yield return TwoKinds(pool, at);
            yield return Pool(JaikevPool, JaikevAt.X, JaikevAt.Y, JaikevAt.Z, Ward, Jaikev, 1, 1, WardRespawnTenths, 0);
            yield return Pool(ParvateePool, ParvateeAt.X, ParvateeAt.Y, ParvateeAt.Z, Ward, Parvatee, 1, 1, WardRespawnTenths, 0);
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", new[] { Shanul, Dhimand, JaikevPool, ParvateePool }.Concat(IncurablePools.Select(pool => pool.Pool))
                .Select(pool => pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            var creatures = string.Join(", ", new[] { Shanul, Dhimand, Brute, Slayer, Jaikev, Parvatee }
                .Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM npc_package WHERE id IN ({Shanul}, {Dhimand});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({creatures});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Shanul}, {Dhimand});";
            yield return $"DELETE FROM creature WHERE id IN ({creatures});";
            yield return $"DELETE FROM creature_action WHERE id = {Attack};";
            foreach (var move in Moves.Reverse())
                yield return Place(move.Pool, move.Before);
        }

        /// <summary>A pool of two Brutes and two Slayers (two creature slots; PlateauCreatureSqlV1.Pool writes one).</summary>
        private static string TwoKinds(uint pool, (double X, double Y, double Z) at) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"VALUES ({pool}, 0, 0, {WardRespawnTenths}, {N(at.X)}, {N(at.Y)}, {N(at.Z)}, 0, {Ward}, " +
            $"{Brute}, {BrutesPerPool}, {BrutesPerPool}, {Slayer}, {SlayersPerPool}, {SlayersPerPool}, " +
            $"0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {RadiusOf(pool)});";

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
