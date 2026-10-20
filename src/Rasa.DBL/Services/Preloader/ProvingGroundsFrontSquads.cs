using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The fighting west of the fort in the Proving Grounds (adv_bootcamp, map context 1985) once
    /// the fort is taken back (Proving_grounds_front_squads): our squads, each spawning where a GM
    /// stood and faced, read off the server's .where line, and holding there. The Bane they fight
    /// are the loose groups of Thrax Infantry Initiates already on the way on (Bootcamp route
    /// Thrax, three or four at a spot); they and our squads fight whatever comes within their 18 m,
    /// and the player.
    ///
    ///  - AFS infantry squads (three spots): three soldiers, each drawn at random as it is sent
    ///    (spawnpool_draw): an Infantryman with a rifle (creaturenamelanguage 8716;
    ///    Redshirt_Human_Soldier_Light_Male 29423 with the bridge soldiers' rifle 27220, its shot
    ///    72003) two times in four, an Infantryman with a pistol (Weapon_Human_Redshirt_Pistol_
    ///    Physical 6271, its shot 510218) one time in four, and a Field Gunner (8720;
    ///    Redshirt_Human_Soldier_Heavy_Male 29433) with the red laser machine gun
    ///    (Weapon_Human_Redshirt_MachineGun_Laser 20534) one time in four. The gun's attack is the
    ///    one its weapon class has in the client (weaponclass: WEAPON_MACHINEGUN 149, argument 4,
    ///    damage type 6).
    ///  - Forean squads (three spots): two Forean Archer Initiates (7986; Redshirt_Forean_Archer
    ///    6415 with Weapon_Creature_Forean_Bow 6453, its shot creature_action 28) and a Forean
    ///    Warrior (Redshirt_Forean_Spearman 6043, whose name is the class's, with
    ///    Weapon_Creature_Forean_Spear 6042 and the spearman's melee, creature_action 5, 174/11:
    ///    his model has no animation for the spear's thrust, 17's 1/305).
    ///
    /// Friendly (faction 1), and a match for the Thrax there (levels 10 to 12): level 11, 660
    /// hit points, 33 body, mind and spirit and 132 armour, as the level 11 route Thrax have. They
    /// run at 7 and walk at 2, so they close on what they fight. A squad is sent again 120 s
    /// after the last of it fell (1200 tenths of a second, as the Thrax), and what it lost while
    /// some of it stood comes back on the same clock. A pool of no radius: its three stand within 2 m
    /// of the spot either way, on the ground (SpawnPoolManager.SpawnPoint). A radius would have
    /// them put on any part of the navmesh polygons it touches, which on that open ground is as
    /// much as 17 m off.
    ///
    /// The pools are scripted (mode 2), so on their own they send nothing - before the fort is
    /// taken there is nothing of ours out there. Capture the Flag's scene wakes them on the
    /// player's map when Youngblood comes in, held by him as he holds the fort
    /// (BootcampFrontSquadsV13), so a map made again has them going from the start.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row goes in only if its id is free, and
    /// Down takes out only the rows that are still these.
    /// </summary>
    public static class ProvingGroundsFrontSquads
    {
        public const uint Map = 1985;

        public const uint Rifleman = 400301, Pistoleer = 400302, FieldGunner = 400303, Archer = 400304, Warrior = 400305;
        public const uint LaserMachineGun = 400301;

        public const uint LightSoldier = 29423, HeavySoldier = 29433, ForeanArcherClass = 6415, ForeanSpearmanClass = 6043;
        public const uint InfantrymanName = 8716, FieldGunnerName = 8720, ArcherInitiateName = 7986;
        public const uint Rifle = 27220, Pistol = 6271, LaserGun = 20534, Bow = 6453, Spear = 6042;
        public const uint RifleShot = 72003, PistolShot = 510218, BowShot = 28, SpearMelee = 5;

        /// <summary>Tenths of a second: 120 s, as the route Thrax.</summary>
        public const uint Respawn = 1200;
        public const double Radius = 0;
        public const byte SquadSize = 3;

        public const uint Level = 11, Health = 660, Attribute = 33, Armor = 132;

        public sealed record Npc(uint Id, string Comment, uint ClassId, uint NameId, uint Weapon, uint Action1, uint Action2 = 0);

        public sealed record Slot(uint Creature, byte Min, byte Max);

        public sealed record Squad(uint Pool, double X, double Y, double Z, double Rotation, bool Afs, string Where);

        public static readonly Npc[] Npcs =
        {
            new(Rifleman, "Proving Grounds front AFS Infantryman, rifle", LightSoldier, InfantrymanName, Rifle, RifleShot),
            new(Pistoleer, "Proving Grounds front AFS Infantryman, pistol", LightSoldier, InfantrymanName, Pistol, PistolShot),
            new(FieldGunner, "Proving Grounds front AFS Field Gunner, laser machine gun", HeavySoldier, FieldGunnerName, LaserGun, LaserMachineGun),
            new(Archer, "Proving Grounds front Forean Archer Initiate", ForeanArcherClass, ArcherInitiateName, Bow, BowShot),
            new(Warrior, "Proving Grounds front Forean Warrior, spear", ForeanSpearmanClass, 0, Spear, SpearMelee),
        };

        /// <summary>
        /// An AFS squad: drawn three at a time, a slot's chance its max count over all (2:1:1).
        /// The min counts are unused by a draw and kept at 0.
        /// </summary>
        public static readonly Slot[] AfsSlots = { new(Rifleman, 0, 2), new(Pistoleer, 0, 1), new(FieldGunner, 0, 1) };

        /// <summary>A Forean squad: two archers and a warrior, every time.</summary>
        public static readonly Slot[] ForeanSlots = { new(Archer, 2, 2), new(Warrior, 1, 1) };

        /// <summary>Where each squad spawns and faces, as the GM's .where lines read.</summary>
        public static readonly Squad[] Squads =
        {
            new(400301, 28.5039, 106.5273, 120.0586, 2.202, true, "AFS squad below the fort's west wall"),
            new(400302, -14.168, 97.4805, 126.9297, 1.2675, true, "AFS squad by Karem Zul's ground"),
            new(400303, -55.0938, 94.0234, 105.918, 6.2469, false, "Forean squad on the west slope"),
            new(400304, -103.0469, 88.7656, 107.5977, 2.4409, false, "Forean squad at the far west"),
            new(400305, -69.0586, 91.2734, 55.3398, 1.2062, false, "Forean squad on the southwest rise"),
            new(400306, -85.7109, 85.7656, 83.6484, 1.1449, true, "AFS squad between the Thrax groups"),
        };

        public static uint[] Pools => Squads.Select(squad => squad.Pool).ToArray();

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"SELECT {LaserMachineGun}, 'Proving Grounds front Field Gunner laser machine gun', 149, 4, 1, 20, 1000, 0, 10, 15, 6 " +
                $"FROM entityclass e WHERE e.id = {LaserGun} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {LaserMachineGun});";

            foreach (var n in Npcs)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {n.Id}, {S(n.Comment)}, {n.ClassId}, 1, {Level}, {Health}, {n.NameId}, 7, 2, {n.Action1}, {n.Action2}, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {n.ClassId} AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {n.Action1}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {n.Id});";

                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                    $"SELECT {n.Id}, 13, {n.Weapon}, 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {n.Id} AND a.slot_id = 13);";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {n.Id}, {Attribute}, {Attribute}, {Attribute}, {Health}, {Armor} FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {n.Id});";
            }

            foreach (var squad in Squads)
            {
                var slots = SlotsOf(squad);
                var columns = new List<string>();
                for (var i = 0; i < 6; i++)
                {
                    var slot = i < slots.Length ? slots[i] : null;
                    columns.Add($"{slot?.Creature ?? 0}, {slot?.Min ?? 0}, {slot?.Max ?? 0}");
                }

                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {squad.Pool}, 2, 0, {Respawn}, {N(squad.X)}, {N(squad.Y)}, {N(squad.Z)}, {N(squad.Rotation)}, {Map}, {string.Join(", ", columns)}, {N(Radius)} " +
                    $"FROM creature c WHERE c.id = {slots[0].Creature} AND c.comment = {S(CommentOf(slots[0].Creature))} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {squad.Pool});";

                if (squad.Afs)
                    yield return $"INSERT INTO {SpawnPoolDrawEntry.TableName} (id, count) SELECT {squad.Pool}, {SquadSize} FROM spawnpool s " +
                        $"WHERE s.id = {squad.Pool} AND s.creature_1_Id = {Rifleman} AND NOT EXISTS (SELECT 1 FROM {SpawnPoolDrawEntry.TableName} x WHERE x.id = {squad.Pool});";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var squad in Squads)
            {
                var first = SlotsOf(squad)[0].Creature;
                if (squad.Afs)
                    yield return $"DELETE FROM {SpawnPoolDrawEntry.TableName} WHERE id = {squad.Pool} " +
                        $"AND EXISTS (SELECT 1 FROM spawnpool s WHERE s.id = {squad.Pool} AND s.creature_1_Id = {first});";
                yield return $"DELETE FROM spawnpool WHERE id = {squad.Pool} AND creature_1_Id = {first} AND map_context_id = {Map};";
            }

            foreach (var n in Npcs)
            {
                var unused = $"NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {n.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id))";
                var ours = $"EXISTS (SELECT 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)}) AND {unused}";
                yield return $"DELETE FROM creature_stat WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature_appearance WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature WHERE id = {n.Id} AND comment = {S(n.Comment)} AND {unused};";
            }

            yield return $"DELETE FROM creature_action WHERE id = {LaserMachineGun} AND description = 'Proving Grounds front Field Gunner laser machine gun' " +
                $"AND NOT EXISTS (SELECT 1 FROM creature c WHERE {LaserMachineGun} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        public static Slot[] SlotsOf(Squad squad) => squad.Afs ? AfsSlots : ForeanSlots;

        private static string CommentOf(uint creature) => System.Array.Find(Npcs, n => n.Id == creature).Comment;

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
