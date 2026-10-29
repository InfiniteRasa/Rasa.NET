using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Maligo Elite Guards of Martyr's Canyon and Septic Bog, Plateau (map 1497), for Canyon
    /// Sweep (1045), Blueprint For Victory (1046) and Toxic Detox (1053). None had a creature row;
    /// the client has their names only: Maligo Elite Guard (creaturenamelanguage 8853) and Maligo
    /// Elite Guardmaster (8854).
    ///
    /// Sourced: "souped up" Bane with a powerful blast, improved shields and a larger aggro range,
    /// in patrols, most of them south towards the Pools zone line; Elite Guards round the Toxic
    /// Waste Pumps (TaRapedia, Ten Ton Hammer). The guardmasters are "commanders of the Maligo
    /// Elite Guard" (mission text 5883). Nothing gives their level, strength, number or look.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Looks, picked from the client's models to match an unsourced description (dark armor
    ///    with orange or crimson glow; heavily plated Kael leaders): the guard is
    ///    Bane_Thrax_Soldier_Pistol_V2_CP_Boss (29059, creature_thrax_lieutenant_v02: charcoal
    ///    armor, orange-red glow; client flags power THUG, armor LIEUTENANT, boss marker); the
    ///    guardmaster is Bane_Kael_Armored_Boss (25414, creature_thrax_kael_v02: dark layered
    ///    plates, orange glow; power and armor LIEUTENANT, boss marker).
    ///  - Guard level 31: hit points and armor of the Thrax Commander - Plateau (531086, same
    ///    power and armor flags) taken to 31 (x 2^(1/8) a level, +10 armor a level); the Thrax
    ///    pistol (WEAPON_ATTACK argument 1), the Commander's kick and the Thrax force blast
    ///    (the "powerful blast"), damage the argument's at level 31 as FamilyAbilities does it,
    ///    the force blast at half its boss value and twice its reuse.
    ///  - Guardmaster level 32: the Kael - Plateau (531088) taken to 32 and doubled as a
    ///    lieutenant, armor 330; its four Kael attacks at x 2^(4/8) x 1.5. No shield or launcher
    ///    ability: the description's are not in any source.
    ///  - Six packs of one guardmaster and two or three guards, wandering 12 m round their pool
    ///    (group patrols are not supported): four on the south side of the canyon floor, which is
    ///    one walk area with the three commanders (the high ground further south is not reachable
    ///    from it on the server's navmesh), two on the Septic Bog shore round the pumps. Each
    ///    pool stands on the navmesh's ground. Respawn 300 s.
    ///
    /// Ids: creatures 750012 and 750013, creature_action 750012-750018, pools 750012-750017,
    /// free before. Plain SQL, valid on SQLite and MySQL: no model maps these tables. Down takes
    /// out only these rows.
    /// </summary>
    public static class PlateauMaligoGuardsV1
    {
        public const uint MapContextId = 1497;
        public const uint Guard = 750012;
        public const uint Guardmaster = 750013;
        public const uint GuardClass = 29059;           // Bane_Thrax_Soldier_Pistol_V2_CP_Boss
        public const uint GuardmasterClass = 25414;     // Bane_Kael_Armored_Boss
        public const uint GuardLevel = 31, GuardmasterLevel = 32;
        public const uint GuardHitPoints = 6747, GuardmasterHitPoints = 8409;
        public const uint FirstAction = 750012, LastAction = 750018;
        public const uint FirstPool = 750012, LastPool = 750017;
        public const uint RespawnSeconds = 300;

        private sealed record Creature(uint Id, string Comment, uint Class, uint NameId, uint Level, uint HitPoints,
            uint Armor, uint[] Actions);

        private static readonly Creature[] Creatures =
        {
            new(Guard, "Maligo Elite Guard - Plateau", GuardClass, 8853, GuardLevel, GuardHitPoints, 290,
                new uint[] { 750012, 750013, 750014 }),
            new(Guardmaster, "Maligo Elite Guardmaster - Plateau", GuardmasterClass, 8854, GuardmasterLevel, GuardmasterHitPoints, 330,
                new uint[] { 750015, 750016, 750017, 750018 }),
        };

        /// <summary>(id, description, action, argument, range min, range max, reuse, windup, damage min, max, type)</summary>
        private static readonly (uint Id, string Description, uint Action, uint Arg, double Min, double Max,
            uint Cooldown, uint Windup, uint DamageMin, uint DamageMax, uint Type)[] Actions =
        {
            (750012, "Maligo Elite Guard weapon 1/1", 1, 1, 1.0, 20.0, 800, 0, 113, 170, 6),
            (750013, "Maligo Elite Guard thrax_kick", 397, 3, 0.5, 3.0, 10000, 333, 84, 128, 1),
            (750014, "Maligo Elite Guard thrax_force_blast", 451, 1, 1.0, 40.0, 10000, 500, 106, 210, 7),
            (750015, "Maligo Elite Guardmaster kael_ground_pound", 171, 1, 0.5, 3.0, 4000, 1233, 494, 660, 1),
            (750016, "Maligo Elite Guardmaster kael_smash", 433, 1, 0.5, 3.0, 4000, 1033, 549, 770, 1),
            (750017, "Maligo Elite Guardmaster kael_tectonic_strike", 497, 1, 1.0, 20.0, 3500, 1033, 221, 331, 7),
            (750018, "Maligo Elite Guardmaster kael_rushing_blow", 496, 1, 8.0, 50.0, 15000, 0, 331, 439, 1),
        };

        /// <summary>(pool, where, guards min, max): on the navmesh's ground.</summary>
        public static readonly (uint Id, string Place, double X, double Y, double Z, uint GuardsMin, uint GuardsMax)[] Packs =
        {
            (750012, "Martyr's Canyon west", -340, 320.74, -790, 2, 3),
            (750013, "Martyr's Canyon below Petrie", -160, 381.48, -830, 2, 3),
            (750014, "Martyr's Canyon south", 40, 380.58, -840, 2, 3),
            (750015, "Martyr's Canyon south ledge", 90, 408.43, -880, 2, 3),
            (750016, "Septic Bog south-west shore", 565, 441.93, -525, 2, 3),
            (750017, "Septic Bog north-east shore", 615, 444.04, -470, 2, 3),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var action in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, " +
                    $"min_damage, max_damage, damage_type) VALUES ({action.Id}, '{action.Description}', {action.Action}, {action.Arg}, " +
                    $"{F(action.Min)}, {F(action.Max)}, {action.Cooldown}, {action.Windup}, {action.DamageMin}, {action.DamageMax}, {action.Type});";

            foreach (var creature in Creatures)
            {
                var slots = new uint[8];
                creature.Actions.CopyTo(slots, 0);
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"VALUES ({creature.Id}, '{creature.Comment}', {creature.Class}, 0, {creature.Level}, {creature.HitPoints}, " +
                    $"{creature.NameId}, 9, 5, {string.Join(", ", slots)});";
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"VALUES ({creature.Id}, 15, 15, 15, {creature.HitPoints}, {creature.Armor});";
            }

            foreach (var pack in Packs)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"VALUES ({pack.Id}, 0, 0, {RespawnSeconds * 10}, {F(pack.X)}, {F(pack.Y)}, {F(pack.Z)}, 0, {MapContextId}, " +
                    $"{Guardmaster}, 1, 1, {Guard}, {pack.GuardsMin}, {pack.GuardsMax}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 12);";
        }

        private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Guard}, {Guardmaster});";
            yield return $"DELETE FROM creature WHERE id IN ({Guard}, {Guardmaster});";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {FirstAction} AND {LastAction};";
        }
    }
}
