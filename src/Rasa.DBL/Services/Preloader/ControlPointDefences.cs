using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The defences of the open world's control points (Control_point_defences): the shields in their
    /// outposts' gates, and AFS soldiers at their gates, sandbags and counters. Ported from our own
    /// server's seeds - its rows are in ControlPointDefences.Rows.cs, generated, with the ids moved
    /// into 97xxxxx - and fitted to what this server's data already has.
    ///
    /// The gate shields (<see cref="FieldRows"/>, world_destructible 9708001-9708099). The zone maps
    /// build the outposts' gates - the human outpost fence's, the fort wall's 16 and 24 m gates, the
    /// Bane outpost wall's major and minor entrances - with their openings empty, and the client has
    /// a force field model for each that is the gate's model with "_forcefield" on its name. A gate is
    /// a point's if that point is the nearest on its map and within 140 m of it: 83 gates at 40 of the
    /// 41 open-world points (the Iapyx has none within reach). Where a field goes was worked out from
    /// the gate's model: its socket (hardpoint SPS_Arch) at the opening meets the field model's plug
    /// (SPP_Arch), taken to the gate's own position and turn. The fort catwalk gates have no model of
    /// their own: the 18.3 m base gate field lowered to the floor closes the 16 m one, two outpost gate
    /// fields side by side the 24 m one. Hit points are twenty times the square of the zone's level
    /// (the median level of the creatures round the point), never under 6000. Upstream already closes
    /// one of the gates, the Landing Zone fort's (world_destructible 640016, Wilderness_post_npcs): that
    /// row is linked to the point instead of a second field put in the same gate
    /// (<see cref="UpstreamFields"/>).
    ///
    /// Each shield is linked to its point (control_point_link kind <see cref="ControlPointLinkEntry.KindField"/>),
    /// and the row's side is the side that starts with the point; from then on ControlPointFields keeps
    /// it with whoever holds the point.
    ///
    /// The soldiers' roles (<see cref="RoleCreatureRows"/>, creature 9700001-9701999): for each of the
    /// fifteen open zones the AFS troops a base had - Rifleman and Infantryman (a man and a woman of
    /// each), Shotgunner, Machine Gunner, Grenadier and Field Gunner - on the four soldier bodies the
    /// client has (Redshirt_Human_Soldier_Light/Heavy, Male/Female), at the zone's level, each with
    /// its own weapon attack (<see cref="RoleActionRows"/>, the same ids) and the weapon it carries in
    /// its hands (<see cref="RoleLookRows"/>, slot 13). Their stats (creature_stat, which is what this
    /// server reads; our server took max_hp) follow the Proving Grounds front's soldiers: body, mind
    /// and spirit three times the level, armour twelve times, health the row's max_hp. None of them
    /// has the Rushing Blow - a Logos ability no AFS soldier was ever seen to use - and the two
    /// soldiers that had it from the creature actions' data (39 and 97) lose it.
    ///
    /// The posts (<see cref="GarrisonPoolRows"/>, spawnpool 9705001-9705299): behind each gate, behind
    /// the sandbags, at the counters and round the point itself, found from the maps' own props and
    /// checked against the navmesh, one to three soldiers each, ten minutes to come back; and Imperial
    /// Valley's, which had none, round its point (9707001). Each is part of its point's AFS garrison
    /// (control_point_link kind 2): there while the AFS hold the point, gone while the Bane do. The
    /// Wilderness AFS posts at the Landing Zone and by the road (spawnpool 117, 49-51), which stood
    /// three to six of the same light soldier, are given the Wilderness roles (<see cref="Recasts"/>).
    ///
    /// Plain SQL, the same on SQLite and MySQL. The rows in the 97xxxxx block are this migration's own
    /// and go in as they are; every change to a row upstream has is guarded on that row as it was, and
    /// Down undoes only what is still as Up left it.
    /// </summary>
    public static partial class ControlPointDefences
    {
        public const uint FirstField = 9708001, LastField = 9708099;
        public const uint FirstRole = 9700001, LastRole = 9701999;
        public const uint FirstPool = 9705001, LastPool = 9705299;
        public const uint ImperialValleyPool = 9707001;

        /// <summary>The two AFS soldiers that had the Rushing Blow, and the creature action each had it as.</summary>
        public static readonly (uint Creature, uint Action)[] RushingBlows = { (39, 68092), (97, 68093) };

        private const int PerStatement = 50;

        private const string PoolColumns =
            "id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius";

        /// <summary>The level of a role's creature row.</summary>
        public static uint LevelOf(object[] creature) => System.Convert.ToUInt32(creature[4]);

        /// <summary>A role's creature_stat: body, mind and spirit three times its level, armour twelve times, health its max_hp.</summary>
        public static (uint Body, uint Mind, uint Spirit, uint Health, uint Armor) StatsOf(object[] creature)
        {
            var level = LevelOf(creature);

            return (3 * level, 3 * level, 3 * level, System.Convert.ToUInt32(creature[5]), 12 * level);
        }

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            // The shields, then their links: the link names a row that is there.
            foreach (var sql in Inserts(WorldDestructibleEntry.TableName,
                         "id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment",
                         FieldRows.Select(f => new[] { f[0], f[2], f[3], f[4], f[5], f[6], f[7], f[8], 0, 0, f[9], f[10] })))
                yield return sql;

            foreach (var sql in Inserts(ControlPointLinkEntry.TableName, "control_point_id, kind, object_id",
                         FieldRows.Select(f => new[] { f[1], (object)(int)ControlPointLinkEntry.KindField, f[0] })))
                yield return sql;

            foreach (var (point, row) in UpstreamFields)
                yield return $"INSERT INTO {ControlPointLinkEntry.TableName} (control_point_id, kind, object_id) SELECT {point}, {ControlPointLinkEntry.KindField}, {row} " +
                             $"FROM {WorldDestructibleEntry.TableName} d WHERE d.id = {row} AND NOT EXISTS (SELECT 1 FROM {ControlPointLinkEntry.TableName} l " +
                             $"WHERE l.control_point_id = {point} AND l.kind = {ControlPointLinkEntry.KindField} AND l.object_id = {row});";

            // The roles: their attacks, the creatures, their stats and their weapons.
            foreach (var sql in Inserts("creature_action", "id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type", RoleActionRows))
                yield return sql;

            foreach (var sql in Inserts("creature", "id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8", RoleCreatureRows))
                yield return sql;

            foreach (var sql in Inserts("creature_stat", "id, body, mind, spirit, health, armor", RoleCreatureRows.Select(c =>
                     {
                         var s = StatsOf(c);
                         return new object[] { c[0], s.Body, s.Mind, s.Spirit, s.Health, s.Armor };
                     })))
                yield return sql;

            foreach (var sql in Inserts("creature_appearance", "id, slot_id, Class_id, color", RoleLookRows))
                yield return sql;

            // The posts, and each in its point's AFS garrison.
            foreach (var sql in Inserts("spawnpool", PoolColumns, GarrisonPoolRows.Select(p => p.Take(p.Length - 1).ToArray())))
                yield return sql;

            foreach (var sql in Inserts(ControlPointLinkEntry.TableName, "control_point_id, kind, object_id",
                         GarrisonPoolRows.Select(p => new[] { p[^1], (object)(int)ControlPointLinkEntry.KindAfsPool, p[0] })))
                yield return sql;

            foreach (var (pool, was, _, _, slots) in Recasts)
                yield return $"UPDATE spawnpool SET {Slots(slots)} WHERE id = {pool} AND creature_1_Id = {was} AND creature_2_Id = 0;";

            foreach (var (creature, action) in RushingBlows)
                yield return $"UPDATE creature SET action2 = 0 WHERE id = {creature} AND action2 = {action};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (creature, action) in RushingBlows)
                yield return $"UPDATE creature SET action2 = {action} WHERE id = {creature} AND action2 = 0;";

            foreach (var (pool, was, wasMin, wasMax, slots) in Recasts)
                yield return $"UPDATE spawnpool SET creature_1_Id = {was}, creature_1_min_count = {wasMin}, creature_1_max_count = {wasMax}, " +
                             "creature_2_Id = 0, creature_2_min_count = 0, creature_2_max_count = 0, creature_3_Id = 0, creature_3_min_count = 0, creature_3_max_count = 0, " +
                             "creature_4_Id = 0, creature_4_min_count = 0, creature_4_max_count = 0, creature_5_Id = 0, creature_5_min_count = 0, creature_5_max_count = 0 " +
                             $"WHERE id = {pool} AND creature_1_Id = {slots[0]};";

            yield return $"DELETE FROM {ControlPointLinkEntry.TableName} WHERE kind = {ControlPointLinkEntry.KindAfsPool} AND " +
                         $"(object_id BETWEEN {FirstPool} AND {LastPool} OR object_id = {ImperialValleyPool});";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool} OR id = {ImperialValleyPool};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {FirstRole} AND {LastRole};";
            yield return $"DELETE FROM creature_stat WHERE id BETWEEN {FirstRole} AND {LastRole};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {FirstRole} AND {LastRole};";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {FirstRole} AND {LastRole};";

            foreach (var (point, row) in UpstreamFields)
                yield return $"DELETE FROM {ControlPointLinkEntry.TableName} WHERE control_point_id = {point} AND kind = {ControlPointLinkEntry.KindField} AND object_id = {row};";

            yield return $"DELETE FROM {ControlPointLinkEntry.TableName} WHERE kind = {ControlPointLinkEntry.KindField} AND object_id BETWEEN {FirstField} AND {LastField};";
            yield return $"DELETE FROM {WorldDestructibleEntry.TableName} WHERE id BETWEEN {FirstField} AND {LastField};";
        }

        private static string Slots(uint[] slots)
        {
            var parts = new List<string>();

            for (var i = 0; i < 5; i++)
            {
                var n = i + 1;
                parts.Add($"creature_{n}_Id = {slots[i * 3]}, creature_{n}_min_count = {slots[i * 3 + 1]}, creature_{n}_max_count = {slots[i * 3 + 2]}");
            }

            return string.Join(", ", parts);
        }

        private static IEnumerable<string> Inserts(string table, string columns, IEnumerable<object[]> rows)
        {
            var all = rows.ToList();

            for (var i = 0; i < all.Count; i += PerStatement)
                yield return $"INSERT INTO {table} ({columns}) VALUES "
                             + string.Join(", ", all.Skip(i).Take(PerStatement).Select(row => "(" + string.Join(", ", row.Select(Literal)) + ")"))
                             + ";";
        }

        private static string Literal(object value)
        {
            return value switch
            {
                string text => "'" + text.Replace("'", "''") + "'",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                float number => number.ToString("R", CultureInfo.InvariantCulture),
                _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}
