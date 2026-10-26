using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// An AFS infantry squad of Alia Das (Concordia Wilderness, map context 1220) that goes down
    /// to the Lower Eloh Creek front and fights the Bane there until it falls
    /// (Alia_das_afs_squad; the game server's NpcSquads).
    ///
    /// Five soldiers at level 5: an Infantryman with a pistol leading (creaturenamelanguage 8716;
    /// Redshirt_Human_Soldier_Light_Male 29423 with Weapon_Human_Redshirt_Pistol_Physical 6271 and
    /// the Bootcamp escorts' pistol shot, creature_action 510218), a Machine Gunner (8719;
    /// Redshirt_Human_Soldier_Heavy_Male 29433 with Weapon_Human_Redshirt_MachineGun_Physical
    /// 20535 and the Bootcamp Field Gunner's burst, 510219, WEAPON_MACHINEGUN 149/1) and three
    /// Riflemen (8717; 29423 with the bridge soldiers' rifle 27220 and its shot 72003), as the
    /// Proving Grounds' front squads are made. 350 hit points, 12 body, mind and spirit and 60
    /// armour, as the level 5 soldiers at the Bootcamp posts; they run at 7 and walk at 2.
    ///
    /// They teleport in on the Alia Das waypoint pad one after another, run out west off it and
    /// over to the south-east of it, and walk the road from there: up to the split, the
    /// right-hand road, the left-hand road down to the landing zone sign, over the bridge and
    /// down into the valley, where they hold and fight. The last leg and the valley look for
    /// a fight 45 m out, so they take on the Light Bender snipers and the Thrax on the far side
    /// (pools 630048, 630049 and 580002); on the road they look as far as their own aggro range.
    /// Each point is a GM's .where line, as given; the route between them is the navmesh's.
    /// Sent again 120 s after the last of them falls.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row goes in only if its id is free, and
    /// Down takes out only the rows that are still these.
    /// </summary>
    public static class AliaDasAfsSquad
    {
        public const uint Map = 1220;

        public const uint Leader = 670001, MachineGunner = 670002, Rifleman = 670003;

        /// <summary>The squad type, its route and its spawn share the one id.</summary>
        public const uint Squad = 670001;

        public const uint LightSoldier = 29423, HeavySoldier = 29433;
        public const uint InfantrymanName = 8716, RiflemanName = 8717, MachineGunnerName = 8719;
        public const uint Pistol = 6271, MachineGun = 20535, Rifle = 27220;
        public const uint PistolShot = 510218, MachineGunBurst = 510219, RifleShot = 72003;

        public const uint Level = 5, Health = 350, Attribute = 12, Armor = 60, RunSpeed = 7, WalkSpeed = 2;

        public const string Name = "Alia Das AFS squad", Route = "Alia Das to the Lower Eloh Creek valley";

        /// <summary>A wedge, 3 m apart; its own creatures' aggro range on the road.</summary>
        public const byte Formation = 0;
        public const double Spacing = 3, EngageRange = 0;

        /// <summary>The route is walked once: at its end the squad holds and fights.</summary>
        public const byte Mode = 2;

        /// <summary>How far the squad looks for a fight going down into the valley and in it.</summary>
        public const double ValleyEngageRange = 45;

        public const uint RespawnSeconds = 120;

        /// <summary>Teleports in (NpcSquads.Arrival.Teleport).</summary>
        public const byte Arrival = 1;

        public sealed record Npc(uint Id, string Comment, uint ClassId, uint NameId, uint Weapon, uint Action);

        public sealed record Member(byte Slot, uint Creature, byte Count);

        public sealed record Point(double X, double Y, double Z, bool Run, double Engage, string Where);

        public static readonly Npc[] Npcs =
        {
            new(Leader, "Alia Das AFS squad Infantryman, pistol, leader", LightSoldier, InfantrymanName, Pistol, PistolShot),
            new(MachineGunner, "Alia Das AFS squad Machine Gunner", HeavySoldier, MachineGunnerName, MachineGun, MachineGunBurst),
            new(Rifleman, "Alia Das AFS squad Rifleman", LightSoldier, RiflemanName, Rifle, RifleShot),
        };

        /// <summary>The leader first; one machine gunner, three riflemen.</summary>
        public static readonly Member[] Members = { new(0, Leader, 1), new(1, MachineGunner, 1), new(2, Rifleman, 3) };

        /// <summary>The waypoint pad, and the way they face coming off it.</summary>
        public const double SpawnX = 807.2383, SpawnY = 294.6055, SpawnZ = 391.1523, SpawnRotation = 1.3571;

        /// <summary>The route, as the GM's .where lines read; a run leg is run, the rest walked.</summary>
        public static readonly Point[] Points =
        {
            new(798.5156, 294.2539, 391.043, true, 0, "run out off the pad, west"),
            new(816.2695, 293.3867, 367.8477, true, 0, "and over, south-east"),
            new(738.3281, 289.7422, 265.8633, false, 0, "up the road to the split"),
            new(637.9531, 282.0508, 269.4141, false, 0, "the right-hand road"),
            new(453.0117, 233.9414, 195.5586, false, 0, "the left-hand road down to the landing zone sign"),
            new(435.832, 233.6992, 159.8828, false, 0, "over the bridge"),
            new(377.1211, 228.7227, 189.1172, false, ValleyEngageRange, "down into the valley, to hold and fight"),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var n in Npcs)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {n.Id}, {S(n.Comment)}, {n.ClassId}, 1, {Level}, {Health}, {n.NameId}, {RunSpeed}, {WalkSpeed}, {n.Action}, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {n.ClassId} AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {n.Action}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {n.Id});";

                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                    $"SELECT {n.Id}, 13, {n.Weapon}, 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {n.Id} AND a.slot_id = 13);";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {n.Id}, {Attribute}, {Attribute}, {Attribute}, {Health}, {Armor} FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {n.Id});";
            }

            yield return $"INSERT INTO {NpcSquadTypeEntry.TableName} (id, name, formation, spacing, engage_range) " +
                $"SELECT {Squad}, {S(Name)}, {Formation}, {N(Spacing)}, {N(EngageRange)} FROM creature c WHERE c.id = {Leader} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadTypeEntry.TableName} x WHERE x.id = {Squad});";

            foreach (var m in Members)
                yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                    $"SELECT {Squad}, {m.Slot}, {m.Creature}, {m.Count}, {m.Count} FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)} " +
                    $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {m.Creature}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadMemberEntry.TableName} x WHERE x.type_id = {Squad} AND x.slot = {m.Slot});";

            yield return $"INSERT INTO {NpcSquadPathEntry.TableName} (id, map_context_id, name, mode) " +
                $"SELECT {Squad}, {Map}, {S(Route)}, {Mode} FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadPathEntry.TableName} x WHERE x.id = {Squad});";

            for (var i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                yield return $"INSERT INTO {NpcSquadPathPointEntry.TableName} (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) " +
                    $"SELECT {Squad}, {i}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, 0, {(p.Run ? 1 : 0)}, {N(p.Engage)} FROM {NpcSquadPathEntry.TableName} r " +
                    $"WHERE r.id = {Squad} AND r.name = {S(Route)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadPathPointEntry.TableName} x WHERE x.path_id = {Squad} AND x.step = {i});";
            }

            yield return $"INSERT INTO {NpcSquadSpawnEntry.TableName} (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, respawn_seconds, enabled, comment, arrival) " +
                $"SELECT {Squad}, {Map}, {Squad}, {Squad}, {N(SpawnX)}, {N(SpawnY)}, {N(SpawnZ)}, {N(SpawnRotation)}, {RespawnSeconds}, 1, {S(Name)}, {Arrival} " +
                $"FROM {NpcSquadPathEntry.TableName} r WHERE r.id = {Squad} AND r.name = {S(Route)} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadSpawnEntry.TableName} x WHERE x.id = {Squad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM {NpcSquadSpawnEntry.TableName} WHERE id = {Squad} AND type_id = {Squad} AND comment = {S(Name)};";
            yield return $"DELETE FROM {NpcSquadPathPointEntry.TableName} WHERE path_id = {Squad} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadPathEntry.TableName} r WHERE r.id = {Squad} AND r.name = {S(Route)});";
            yield return $"DELETE FROM {NpcSquadPathEntry.TableName} WHERE id = {Squad} AND name = {S(Route)};";
            yield return $"DELETE FROM {NpcSquadMemberEntry.TableName} WHERE type_id = {Squad} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)});";
            yield return $"DELETE FROM {NpcSquadTypeEntry.TableName} WHERE id = {Squad} AND name = {S(Name)};";

            foreach (var n in Npcs)
            {
                var unused = $"NOT EXISTS (SELECT 1 FROM {NpcSquadMemberEntry.TableName} m WHERE m.creature_id = {n.Id}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {n.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id))";
                var ours = $"EXISTS (SELECT 1 FROM creature c WHERE c.id = {n.Id} AND c.comment = {S(n.Comment)}) AND {unused}";
                yield return $"DELETE FROM creature_stat WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature_appearance WHERE id = {n.Id} AND {ours};";
                yield return $"DELETE FROM creature WHERE id = {n.Id} AND comment = {S(n.Comment)} AND {unused};";
            }
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
