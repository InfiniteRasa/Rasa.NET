using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Two more for the Proving Grounds (adv_bootcamp, map context 1985), at the front: west of
    /// the sandbag post, nearest the rockfall in the doorwell (BootcampCaveInBreachV9), where the
    /// Thrax come out when the rocks are blown and run for the bridge past them
    /// (Bootcamp_front_line). Each where a GM stood and faced, read off the server's .where line,
    /// standing exactly there (no speed: BehaviorManager.NeverMoves).
    ///
    ///  - 400009 a Rifleman (creaturenamelanguage 8717) with his rifle at the ready (pose 2, the
    ///    client's "Weapon - Rifle - Idle"): Redshirt_Human_Soldier_Light_Male (29423) with the
    ///    bridge soldiers' rifle (27220) and its attack (creature_action 72003, WEAPON_ATTACK
    ///    134, to 20 m). Pool 400010, 4 m off the Thrax's way to the bridge (300, 64.5).
    ///  - 400010 a Forean Shaman Initiate (7890): NPC_Forean_Shaman (7035), the model of Field
    ///    Officer Oceana, with her staff (6164) and the staff attack Capture the Flag's shaman
    ///    escort had (creature_action 510214, WEAPON_ATTACK 146, to 24 m), standing at his post
    ///    (pose 1). Pool 400011.
    /// Friendly (faction 1) and level 5, as the base's soldiers; they shoot back at what comes
    /// for them. Respawn 200 s, as the sandbag post's.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row goes in only if its id is free, and
    /// Down takes out only the rows that are still these.
    /// </summary>
    public static class BootcampFrontLine
    {
        public const uint Map = 1985;

        public const uint RiflemanId = 400009, ShamanId = 400010;
        public const uint RiflemanPoolId = 400010, ShamanPoolId = 400011;

        public const uint InfantrymanClass = 29423, ForeanShamanClass = 7035;
        public const uint RiflemanName = 8717, ShamanInitiateName = 7890;
        public const uint Rifle = 27220, Staff = 6164;
        public const uint RifleAttack = 72003, StaffAttack = 510214;
        public const uint Respawn = 200;

        public const byte Standing = 1, WeaponOut = 2;

        public sealed record Npc(uint Id, string Comment, uint ClassId, uint Level, uint MaxHp, uint NameId, uint Action, uint Weapon,
            uint Body, uint Mind, uint Spirit, uint Armor);

        public sealed record Post(uint Pool, uint Creature, double X, double Y, double Z, double Rotation, byte Pose, string Who);

        public static readonly Npc[] Npcs =
        {
            new(RiflemanId, "Rifleman at the front, rifle ready", InfantrymanClass, 5, 350, RiflemanName, RifleAttack, Rifle, 12, 12, 12, 60),
            new(ShamanId, "Forean Shaman Initiate at the front", ForeanShamanClass, 5, 600, ShamanInitiateName, StaffAttack, Staff, 15, 15, 15, 60),
        };

        public static readonly Post[] Posts =
        {
            new(RiflemanPoolId, RiflemanId, 297.2773, 120.5312, 68.6289, 1.2063, WeaponOut, "Rifleman"),
            new(ShamanPoolId, ShamanId, 297.4102, 120.4805, 72.1953, 0.6313, Standing, "Forean Shaman Initiate"),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var n in Npcs)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {n.Id}, {S(n.Comment)}, {n.ClassId}, 1, {n.Level}, {n.MaxHp}, {n.NameId}, 0, 0, {n.Action}, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {n.ClassId} AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {n.Action}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {n.Id});";

                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                    $"SELECT {n.Id}, 13, {n.Weapon}, 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {n.Id} AND a.slot_id = 13);";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {n.Id}, {n.Body}, {n.Mind}, {n.Spirit}, {n.MaxHp}, {n.Armor} FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {n.Id});";
            }

            foreach (var p in Posts)
            {
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Pool}, 0, 0, {Respawn}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {Map}, {p.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND c.comment = {S(CommentOf(p.Creature))} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Pool});";

                yield return $"INSERT INTO spawnpool_pose (id, pose) SELECT {p.Pool}, {p.Pose} FROM spawnpool s " +
                    $"WHERE s.id = {p.Pool} AND s.creature_1_Id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool_pose x WHERE x.id = {p.Pool});";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var p in Posts)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {p.Pool} AND pose = {p.Pose} " +
                    $"AND EXISTS (SELECT 1 FROM spawnpool s WHERE s.id = {p.Pool} AND s.creature_1_Id = {p.Creature});";
                yield return $"DELETE FROM spawnpool WHERE id = {p.Pool} AND creature_1_Id = {p.Creature} AND map_context_id = {Map};";
            }

            foreach (var n in Npcs)
            {
                var ours = $"EXISTS (SELECT 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {n.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id))";
                yield return $"DELETE FROM creature_stat WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature_appearance WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature WHERE id = {n.Id} AND comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {n.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }
        }

        private static string CommentOf(uint creature) => System.Array.Find(Npcs, n => n.Id == creature).Comment;

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
