using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 10 creatures, Ligo Ashen Desert (map 1734), for Ida Hayner's four orders: Designer Jewelry Gone
    /// Wrong (1287), All the Latest Rage! (1293), I Wouldn't Wear It, But... (1289) and A New Fad (1291). The four named
    /// creatures (Sally, Wendy, Molly, Trudy) and the three kinds the server had on no Ashen Desert pool (the Granitour, the
    /// Lasher, the Beam Manta) are new; 1287's Ashen Atta Soldiers and Workers are set 2's. Ida Hayner's own pool moves off the
    /// Shadow's Edge weapon vendor she stood on.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 10 790360-790399):
    ///  - 790360 Sally (creature, creature_stat), 790361 her pool;
    ///  - 790362 the Lasher (creature, creature_stat), 790363 and 790364 its pools; 790365 Molly, 790366 her pool;
    ///  - 790367 the Beam Manta, 790368-790370 its pools; 790371 Trudy, 790372 her pool;
    ///  - 790373 the Granitour, 790374 and 790375 its pools; 790376 Wendy, 790377 her pool.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9359 "Sally", 9360 "Molly", 9361 "Trudy", 9362 "Wendy". The client has no
    ///    name for a Lasher or a Granitour (the World DB rows 770008 and 540031 have name 0); the Beam Manta row 540040 is 8023
    ///    "Beam Manta Pilot".
    ///  - Places: Sally TaRapedia (-439, -258) (Ellatha forum (Kelisco) "approximately -460, -230"; research group 3 reads it
    ///    as about (-460, -287) on the map view (ESTIMATE conversion), 36 m off); Wendy TaRapedia "/loc -190 580, near
    ///    the marker for Coheri Pools" (Ellatha forum (Kelisco) "-215, 630"; research group 3 reads
    ///    it as about (-215, 573) on the map view, ESTIMATE conversion); Molly TaRapedia "a little notch in the hillside directly
    ///    SW of the Ashoka teleport pad (275, -659)"; Trudy TaRapedia "-242 771 on top of hill, north of Coheri Pools"; the
    ///    Granitour at Coheri Pools (the client's 1293 log and its map marker (-188, 291, 544)); the Beam Manta "along the
    ///    eastern side of the Boneyard, the Indra Pass, and the northeast part of Maji Field" (the client's 1291 log); the
    ///    Lashers "in the Ashen Desert" (1289's log; 1300's "throughout the Ashen Desert, including the ridge west of White
    ///    Oasis").
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, triage U1, D7): Ida Hayner's pool 510011 stood on the Shadow's Edge
    /// weapon vendor's (500018) spot (-167.1, 296.3, -709), the two NPCs in one place. Moved a few metres in the same shop.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Classes (triage D8; World DB entityclass names, the choice of class ours): Sally 22627
    ///    Creature_Atta_Soldier_Standard_Boss (the client's "Ashen Atta Soldier called Sally"), Molly 10331 Creature_Lasher_Boss, Trudy 10317 Creature_Beam_Manta_Boss, Wendy 25620
    ///    Creature_Granitour_Adult_Boss; the normals 7477 Creature_Lasher, 6441 Creature_Beam_Manta, 10164
    ///    Creature_Granitour_Young (the only Granitour class with World DB rows).
    ///  - The normals copy the World DB rows of their kind nearest the zone band, as set 2's Atta: the Lasher 770008 (L40,
    ///    the Crucible's), the Beam Manta 540040 (L40, Thunderhead), the Granitour 540031 (L42, Abyss), with their actions,
    ///    hit points and armour.
    ///  - The bosses: Sally a copy of the Ashen Atta boss 520002 (Atta Imperial Guard - Ashen Desert: L44, 4900 hit points,
    ///    armour 420) with her name; Molly, Trudy and Wendy on the same level, hit points and armour (the Ashen boss rows
    ///    520002-520007 all have them) with the actions of the World DB rows of their family: Molly the Crucible Lasher boss
    ///    Kalamit's (770034), Trudy the Beam Manta boss Plains Devil Lord's (520049), Wendy the Granitour 540031's (no
    ///    Granitour boss row). Each a pool of one, radius 0, 50 s respawn (the Ashen boss pools' 500 tenths).
    ///  - The normals' pools: three a pool, radius 0, 120 s respawn (the Ashen pools' 1200 tenths), as sets 2 and 7.
    ///  - Places: by the probe of 2026-11-10 (the client files and the server navmesh): each pool's point on the navmesh
    ///    ground, round it one navmesh floor joined to its waypoint, a client floor under open sky, walked to on the client's
    ///    floor (AshenFixBTests.ClientFloorLine: 1.1 m a metre, knee, chest and head lines clear on the line and a player's
    ///    half width to each side) from the nearest waypoint and back; 60 m or more from other hostile pools and 85 m or more
    ///    from the NPCs (the bosses among their own kind).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts Ida
    /// Hayner back where upstream had her.
    /// </summary>
    public static class AshenDesignerHidesNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint PerPool = 3, RespawnTenths = 1200, BossRespawnTenths = 500, PoolRadius = 0;
        public const uint BossLevel = 44, BossHitPoints = 4900, BossArmor = 420;

        /// <summary>Sally: id, name, class, the row she copies; her pool.</summary>
        public const uint Sally = 790360, SallyName = 9359, SallyClass = 22627, SallyCopy = 520002, SallyPool = 790361;

        /// <summary>The Lasher: id, the row it copies; Molly: id, name, class, the row whose actions she takes; her pool.</summary>
        public const uint Lasher = 790362, LasherCopy = 770008;
        public const uint Molly = 790365, MollyName = 9360, MollyClass = 10331, MollyCopy = 770034, MollyPool = 790366;

        /// <summary>The Beam Manta: id, the row it copies; Trudy.</summary>
        public const uint Manta = 790367, MantaCopy = 540040;
        public const uint Trudy = 790371, TrudyName = 9361, TrudyClass = 10317, TrudyCopy = 520049, TrudyPool = 790372;

        /// <summary>The Granitour: id, the row it copies; Wendy.</summary>
        public const uint Granitour = 790373, GranitourCopy = 540031;
        public const uint Wendy = 790376, WendyName = 9362, WendyClass = 25620, WendyCopy = 540031, WendyPool = 790377;

        public static readonly (double X, double Y, double Z) SallyAt = (-439, 299.63, -258);
        public static readonly (double X, double Y, double Z) MollyAt = (299, 295.08, -658);  // ESTIMATE: 24 m east of TaRapedia's notch (275, -659): the notch floor is stacked and a Fithik pool stands 22 m off
        public static readonly (double X, double Y, double Z) TrudyAt = (-198, 299.48, 648);  // ESTIMATE: the north end of the ridge's floor the navmesh joins; TaRapedia's hilltop (-242, 771) is joined to nothing
        public static readonly (double X, double Y, double Z) WendyAt = (-180, 286.25, 570);  // ESTIMATE: the Coheri Pools' open floor, north of the Granitours

        /// <summary>The Lasher pools by the road west of Ashoka: pool, point.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] LasherPools =
        {
            (790363, (330, 280.44, -585)),
            (790364, (280, 285.88, -578))
        };

        /// <summary>The Beam Manta pools, one in each place the client names: the eastern Boneyard, the Indra Pass, northeast Maji Field.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] MantaPools =
        {
            (790368, (764, 305.49, -322)),   // ESTIMATE: the eastern Boneyard's open floor
            (790369, (216, 328.58, 486)),    // ESTIMATE: the Indra Pass' floor joined to the White Oasis side
            (790370, (-189, 295.66, 635))    // ESTIMATE: northeast Maji Field, south of Trudy's ridge
        };

        /// <summary>The Granitour pools at Coheri Pools: pool, point.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] GranitourPools =
        {
            (790374, (-185, 291.08, 520)),   // ESTIMATE: the Coheri Pools' open floor
            (790375, (-215, 295.15, 525))
        };

        public const uint NextFreeId = 790378;

        /// <summary>Ida Hayner's pool (World DB), the place before and the place now.</summary>
        public const uint IdaHayner = AshenLeadInNpcsV1.IdaHayner;
        public static readonly (double X, double Y, double Z) IdaBefore = (-167.10000610351562, 296.27, -709.0);
        public static readonly (double X, double Y, double Z) IdaAt = (-161, 296.57, -710);  // ESTIMATE: 6 m off the weapon vendor 500018, whose x/z she shared

        /// <summary>The bosses: id, comment, the row they copy, class, name, pool, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Class, uint Name, uint Pool, (double X, double Y, double Z) At)[] Bosses =
        {
            (Sally, "Sally - Ashen Desert, Mauna Barrens (1287)", SallyCopy, SallyClass, SallyName, SallyPool, SallyAt),
            (Molly, "Molly - Ashen Desert, west of Ashoka (1289)", MollyCopy, MollyClass, MollyName, MollyPool, MollyAt),
            (Trudy, "Trudy - Ashen Desert, north-east Maji Field (1291)", TrudyCopy, TrudyClass, TrudyName, TrudyPool, TrudyAt),
            (Wendy, "Wendy - Ashen Desert, Coheri Pools (1293)", WendyCopy, WendyClass, WendyName, WendyPool, WendyAt)
        };

        /// <summary>The normals: id, comment, the row they copy, their pools.</summary>
        public static readonly (uint Id, string Comment, uint Copy, (uint Pool, (double X, double Y, double Z) At)[] Pools)[] Normals =
        {
            (Lasher, "Lasher - Ashen Desert, west of Ashoka (1289)", LasherCopy, LasherPools),
            (Manta, "Beam Manta - Ashen Desert, Boneyard, Indra Pass, Maji Field (1291)", MantaCopy, MantaPools),
            (Granitour, "Granitour - Ashen Desert, Coheri Pools (1293)", GranitourCopy, GranitourPools)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, pools) in Normals)
            {
                yield return Copy(id, comment, copy, "class_id", "level", "max_hp", "name_id");
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {copy};";
                foreach (var (pool, at) in pools)
                    yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, id, PerPool, PerPool, RespawnTenths, PoolRadius);
            }
            foreach (var (id, comment, copy, cls, name, pool, at) in Bosses)
            {
                yield return Copy(id, comment, copy, N(cls), N(BossLevel), N(BossHitPoints), N(name));
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {id}, body, mind, spirit, {BossHitPoints}, {BossArmor} FROM creature_stat WHERE id = {copy};";
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, id, 1, 1, BossRespawnTenths, PoolRadius);
            }
            yield return Place(IdaHayner, IdaAt) + At(IdaBefore);
        }

        public static IEnumerable<string> Down()
        {
            // Only while Ida still stands where set 10 put her: a later move of her pool is not undone.
            yield return Place(IdaHayner, IdaBefore) + At(IdaAt);
            var pools = Normals.SelectMany(row => row.Pools.Select(pool => pool.Pool)).Concat(Bosses.Select(row => row.Pool)).OrderBy(id => id);
            var creatures = Normals.Select(row => row.Id).Concat(Bosses.Select(row => row.Id)).OrderBy(id => id).ToList();
            yield return $"DELETE FROM spawnpool WHERE id IN ({string.Join(", ", pools.Select(N))});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({string.Join(", ", creatures.Select(N))});";
            yield return $"DELETE FROM creature WHERE id IN ({string.Join(", ", creatures.Select(N))});";
        }

        /// <summary>A copy of a World DB creature row with its own id and comment; class, level, hit points and name as given (a column name keeps the row's).</summary>
        private static string Copy(uint id, string comment, uint copy, string cls, string level, string hitPoints, string name) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, {S(comment)}, {cls}, faction, {level}, {hitPoints}, {name}, run_speed, walk_speed, " +
            $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {D(at.X)}, pos_y = {D(at.Y)}, pos_z = {D(at.Z)} WHERE id = {pool}";

        /// <summary>The guard of an update: the pool still at <paramref name="at"/> (its stored values, to 0.01 m).</summary>
        private static string At((double X, double Y, double Z) at) =>
            $" AND ABS(pos_x - {D(at.X)}) < 0.01 AND ABS(pos_y - {D(at.Y)}) < 0.01 AND ABS(pos_z - {D(at.Z)}) < 0.01;";

        private static string N(uint value) => value.ToString(CultureInfo.InvariantCulture);

        private static string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
