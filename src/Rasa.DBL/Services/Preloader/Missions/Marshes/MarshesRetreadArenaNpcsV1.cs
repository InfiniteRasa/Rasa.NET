using System.Collections.Generic;
using Rasa.Missions.Scenes;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 11 (Retread Arena) NPCs and creatures, Valverde Marshes (map 1454): Retread Karl (1085, 1087, 1088
    /// giver and receiver) and Retread Foster (the arena door) outside the Thunderdome in Retread City, the five arena
    /// fighters (scene creatures, no pool) and a Forean Machina pool for 1084's brain implant.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760344-760399 free): 760344 Karl (creature, creature_appearance, npc_package, pool), 760345 Foster (the same), 760346
    /// Withers, 760347 Ingram, 760348 King, 760349 Baker, 760350 Toro (creature, creature_appearance, creature_stat), 760351
    /// the Forean Machina (creature, creature_stat, pool); teleporters 905 (the dome's pad) and 906 (the pad outside) and the
    /// local_teleporter_link 905 -> 906 (teleporter ids stay below 1000, PlateauMaligoBaseV1; 905-951 free in the World DB and
    /// the code of all worktrees, 2026-11-02).
    ///
    /// Sourced: client names 8636 "Retread Karl", 8918 "Retread Foster", 8904 "Retread Withers", 8903 "Retread Ingram",
    /// 8906 "Retread King", 8907 "Retread Baker", 8905 "Retread Toro" (creaturenamelanguage); Foster's package 1161
    /// (objectiveconversation 1085,2, 1087,2, 1088,2), free in the World DB. Client map: the arena building
    /// ArchHumRetread01BuildingThunderdomeV01 (-219.9, 213, -761.1). Ten Ton Hammer (Retread City): "a pair of 38's"
    /// (1085), "one level 39" (1087), "a level 40 with a propellant gun" (1088); the teleporter "right next to" Karl.
    /// TR MMO DB: Toro a "Quest Monster", Foster the arena door NPC. World DB: Retread Vincent 510168 (class 3846, six
    /// slots), the Alia Das AFS squad Rifleman 670003 (class 29423, its rifle action; AliaDasAfsSquad), the Caves of Donn Forean
    /// Machina 566012 (class 6236, no name), the Marshes Treeback 531053 (level 36, 10406 HP, armour 340).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Karl and Foster copies of Vincent's row and look; Foster at the dome's south door, Karl 8 m south of him (probe of
    ///    2026-11-02: both walked to from the Retread City waypoint). Karl takes a server-only package (his id).
    ///  - The dome's floor (r up to about 25 m, y 213.9) is a navmesh island: no walk in or out, so the arena is entered and
    ///    left by a scene transfer (MarshesRetreadArenaV1). A one-way local teleporter on the dome's west floor takes the
    ///    player out to the exit, for a player left inside (the scene called off: abandon, an error, a restart). The class
    ///    the local teleporters of Ashoka (ArchHumTeleporterV02, as PlateauMaligoBaseV1); the outside pad has no link of its
    ///    own. Retail had no such pad that we know of.
    ///  - The fighters copies of the Rifleman's row and look (the triage's "AFS soldier look", Q41: the Retread rows have no
    ///    attack), hostile, levels 38/38/39/39/40 (Ten Ton Hammer), 6000/8000/15000 HP (the triage), the Rifleman's armour.
    ///    Toro has no propellant gun (no such creature action found): the rifle.
    ///  - The Forean Machina: 566012's row at level 36 with the Treeback's stats (the triage's Q16), no name (the client has
    ///    no plain "Forean Machina"); three, 120 s respawn, on the lowland south-east of High Point Retreat near the Stalker
    ///    pool (-650, 218.44, -620), walked to from the High Point Retreat waypoint.
    /// </summary>
    public static class MarshesRetreadArenaNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, Rifleman = AliaDasAfsSquad.Rifleman, CavesMachina = 566012;

        public const uint Karl = 760344, KarlName = 8636, KarlPackage = Karl;
        public const uint Foster = 760345, FosterName = 8918, FosterPackage = 1161;
        public const uint Withers = 760346, Ingram = 760347, King = 760348, Baker = 760349, Toro = 760350;
        public const uint FighterArmor = 505;
        public const uint Machina = 760351, MachinaPool = 760351;
        public const uint MachinaLevel = 36, MachinaHitPoints = 10406, MachinaArmor = 340;
        public const uint Machinas = 3, MachinaRespawnTenths = 1200, MachinaRadius = 8;

        public static readonly (double X, double Y, double Z) KarlAt = (-219.9, 216.88, -807.1);
        public static readonly (double X, double Y, double Z) FosterAt = (-219.9, 216.9, -799.1);
        public static readonly (double X, double Y, double Z) MachinaAt = (-650, 218.44, -620);

        /// <summary>The arena: its centre (the dome's), where the player comes in, and where the player is set down outside.</summary>
        public static readonly ScenePosition ArenaCentre = new(-219.9f, 213.88f, -761.1f);
        public static readonly ScenePosition ArenaArrival = new(-219.9f, 213.88f, -746.1f);
        public const double ArenaArrivalYaw = 3.1416;   // facing south, at the fighters
        public static readonly ScenePosition ArenaExit = new(-219.9f, 216.89f, -803f);
        public const double ArenaExitYaw = 0;

        /// <summary>The way out for a player left in the dome: its pad, linked one way to a pad at the exit.</summary>
        public const uint DomePad = 905, ExitPad = 906;
        public const uint PadClass = 29648;             // ArchHumTeleporterV02, as PlateauMaligoBaseV1
        public const uint LocalTeleporter = 1;          // teleporter.type
        public static readonly (double X, double Y, double Z) DomePadAt = (-236.9, 213.88, -756.1);

        /// <summary>The fighters: id, comment, name, level, hit points.</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Level, uint HitPoints)[] Fighters =
        {
            (Withers, "Retread Withers - Marshes, Retread Arena (1085)", 8904, 38, 6000),
            (Ingram, "Retread Ingram - Marshes, Retread Arena (1085)", 8903, 38, 6000),
            (King, "Retread King - Marshes, Retread Arena (1087)", 8906, 39, 8000),
            (Baker, "Retread Baker - Marshes, Retread Arena (1087)", 8907, 39, 8000),
            (Toro, "Retread Toro - Marshes, Retread Arena (1088)", 8905, 40, 15000)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, package, at) in new[]
            {
                (Karl, "Retread Karl - Marshes, Retread City (1085, 1087, 1088)", KarlName, KarlPackage, KarlAt),
                (Foster, "Retread Foster - Marshes, Retread Arena door", FosterName, FosterPackage, FosterAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Vincent};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Vincent};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            foreach (var (id, comment, name, level, hitPoints) in Fighters)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, 0, {level}, {hitPoints}, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Rifleman};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Rifleman};";
                yield return Stat(id, hitPoints, FighterArmor);
            }

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Machina}, 'Forean Machina - Marshes, south-east of High Point Retreat (1084)', class_id, faction, " +
                         $"{MachinaLevel}, {MachinaHitPoints}, name_id, run_speed, walk_speed, action1, action2, action3, action4, " +
                         $"action5, action6, action7, action8 FROM creature WHERE id = {CavesMachina};";
            yield return Stat(Machina, MachinaHitPoints, MachinaArmor);
            yield return Pool(MachinaPool, MachinaAt.X, MachinaAt.Y, MachinaAt.Z, MapContextId, Machina, Machinas, Machinas,
                MachinaRespawnTenths, MachinaRadius);
            yield return Pad(DomePad, "Local Teleporter: Retread Arena (out)", DomePadAt, 0);
            yield return Pad(ExitPad, "Local Teleporter: Retread Arena exit", (ArenaExit.X, ArenaExit.Y, ArenaExit.Z), ArenaExitYaw);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({DomePad}, {ExitPad});";
        }

        private static string Pad(uint id, string description, (double X, double Y, double Z) at, double rotation) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporter}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, {F(rotation)}, {MapContextId});";

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({DomePad}, {ExitPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({DomePad}, {ExitPad});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({Karl}, {Foster}, {MachinaPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Karl}, {Foster});";
            yield return $"DELETE FROM creature_stat WHERE id BETWEEN {Withers} AND {Machina};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Karl} AND {Machina};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Karl} AND {Machina};";
        }
    }
}
