using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 13 NPCs, for the Thrax hybrid arc: Dark Genesis (1856), The Gene Machine (1857), Designer Genes (1881)
    /// and Genome Sweet Genome (1899). The World DB has no row of Special Agent Stark or of a Skeev in the Crucible; Special
    /// Agent Capriulo 510035 and Viddea Scientist Eugin 510036 are upstream (MissionNpcCreaturePreloader), in the Staal
    /// placeholder cluster.
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-12a used 770000-770059):
    ///  - 770060 Stark (creature, creature_appearance, creature_stat, npc_package, spawn pool) at Outpost Intrepid, east of the
    ///    wormhole;
    ///  - 770061 Skeev who talks (creature, creature_appearance, creature_stat, npc_package) and 770062 Skeev who fights
    ///    (creature, creature_appearance, creature_stat): scene creatures of 1899, no pool;
    ///  - teleporters 911 (the Khulago tunnel's control room) and 912 (the laboratory's east end), local_teleporter_link both
    ///    ways;
    ///  - moved: Capriulo and Eugin, out of the Staal placeholder cluster to TaRapedia's places.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 10300 "Special Agent Stark", 10301 "Special Agent Capriulo", 10303
    ///    "Viddea Scientist Eugin", 6734 "Skeev". Packages (client objectiveconversation, 1.0): 2335 Capriulo (1856,1; 1899,3),
    ///    2337 Eugin (1856,2; 1899,2), 2371 Skeev (1899,7), 2372 the terminal (1899,5). Stark has no client package (no
    ///    objective talks with him): a server-only one, his id, as Ruben.
    ///  - Stark "east of the wormhole" (TaRapedia Dark Genesis, 1020, 145, 64; 0.5), "near the wormhole, toward the crafting
    ///    station" (IGN-HYB, 0.3). Capriulo (-304, 197, 379), the nightclub on the west upper floor; Eugin (-232, 196, 473), the
    ///    Viddea Quarter (TaRapedia Dark Genesis, 0.5).
    ///  - The laboratory: "a circular room with a teleporter dominating the center" past the doorway at (276, 160, -370), Skeev
    ///    at (-124, 200, -288) (TTH, 0.5); the teleporter at (114, 140, -366), Skeev at (-136, 200, -288) (TaRapedia, 0.5).
    ///    The server navmesh has no walk from the tunnel to the laboratory (probe 2026-11-08), upstream has no teleporter row
    ///    for it and the client no LOCAL_TELEPORTER marker: the two pads join them.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Stark a copy of Captain DeLisi's row and look (510019, class 3846), level 40, 4500 hit points and armour 300, friendly,
    ///    stands still. His place 13 m south of TaRapedia's spot (that spot is 8 m from the arena link 99's circle): 28 m from
    ///    link 99's centre, seen from 16 of 16 places 2 m round.
    ///  - Skeev a copy of the Wilderness Overseer Skeev 630130 (class 28589 NPC_Bane_Miniboss_Overseer_Skeev, its look and its
    ///    rifle, action 41), level 40, Molitor's 10905 hit points and armour 308. The talker neutral (faction 3, as Nakrinx:
    ///    he seeks no one and no one seeks him) and still; the fighter hostile with Derge's run 9 and walk 5.
    ///  - Capriulo and Eugin each on TaRapedia's spot (Capriulo 3 m off it, on the nightclub floor), seen from 16 of 16 places,
    ///    walked to from the Staal waypoint and the lower Kappa pad.
    ///  - The pads: class 8044 UsableTwoStateBaneTeleporterV01 (a Bane pad, seen; no static stands there), each on the navmesh
    ///    and a client floor, the laboratory one 21.5 m outside the reach of the nearest Thrax pool (531109).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows and puts Capriulo and Eugin back.
    /// </summary>
    public static class CrucibleHybridArcNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1, Hostile = 0, Neutral = 3, NpcLevel = 40;

        public const uint Stark = 770060, StarkName = 10300, StarkPackage = 770060;
        public static readonly (double X, double Y, double Z) StarkAt = (1019, 144.75, 50);
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;

        public const uint Capriulo = 510035, CapriuloPackage = 2335;
        public const uint Eugin = 510036, EuginPackage = 2337;

        /// <summary>The upstream Wilderness Skeev the two copies are made of.</summary>
        public const uint WildernessSkeev = 630130;
        public const uint SkeevTalker = 770061, SkeevFighter = 770062, SkeevName = 6734, SkeevPackage = 2371;
        public const uint SkeevLevel = 40;
        public const uint SkeevHitPoints = CrucibleStaalSigalsNpcsV1.MolitorHitPoints, SkeevArmor = CrucibleStaalSigalsNpcsV1.MolitorArmor;
        public const uint SkeevRunSpeed = 9, SkeevWalkSpeed = 5;

        /// <summary>The pads: in the Khulago tunnel's control room, and in the laboratory's east end.</summary>
        public const uint TunnelPad = 911, LabPad = 912, LocalTeleporter = 1, PadClass = 8044;
        public static readonly (double X, double Y, double Z) TunnelPadAt = (114, 139.88, -370);
        public static readonly (double X, double Y, double Z) LabPadAt = (11, 201.7, -286);

        /// <summary>The pools moved: pool, the place before (upstream), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Capriulo, (-17.5, 187.77, 317.6000061035156), (-303, 196.68, 376)),
            (Eugin, (-13.899999618530273, 187.77, 317.6000061035156), (-232, 195.69, 473))
        };

        public static (double X, double Y, double Z) CapriuloAt => Moves.Single(move => move.Pool == Capriulo).Now;
        public static (double X, double Y, double Z) EuginAt => Moves.Single(move => move.Pool == Eugin).Now;

        public const uint NextFreeId = 770063;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Stark}, 'Special Agent Stark - Outpost Intrepid (1856)', class_id, {Friendly}, {NpcLevel}, {HitPoints}, " +
                         $"{StarkName}, 0, 0, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {DeLisi};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Stark}, slot_id, Class_id, color FROM creature_appearance WHERE id = {DeLisi};";
            yield return Stat(Stark, HitPoints, Armor);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Stark}, {StarkPackage}, 'Special Agent Stark (1856)');";
            yield return Pool(Stark, StarkAt.X, StarkAt.Y, StarkAt.Z, MapContextId, Stark, 1, 1, 20, 0);

            foreach (var (id, comment, faction, run, walk) in new[]
            {
                (SkeevTalker, "Skeev - Khulago laboratory, talks (1899)", Neutral, 0U, 0U),
                (SkeevFighter, "Skeev - Khulago laboratory, fights (1899)", Hostile, SkeevRunSpeed, SkeevWalkSpeed)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {faction}, {SkeevLevel}, {SkeevHitPoints}, {SkeevName}, {run}, {walk}, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {WildernessSkeev};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {WildernessSkeev};";
                yield return Stat(id, SkeevHitPoints, SkeevArmor);
            }
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({SkeevTalker}, {SkeevPackage}, 'Skeev (1899)');";

            foreach (var move in Moves)
                yield return Place(move.Pool, move.Now);

            yield return Pad(TunnelPad, "Local Teleporter: Khulago tunnel, to the laboratory", TunnelPadAt);
            yield return Pad(LabPad, "Local Teleporter: Khulago laboratory, to the tunnel", LabPadAt);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({TunnelPad}, {LabPad});";
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({LabPad}, {TunnelPad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({TunnelPad}, {LabPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({TunnelPad}, {LabPad});";
            foreach (var move in Moves.Reverse())
                yield return Place(move.Pool, move.Before);
            yield return $"DELETE FROM npc_package WHERE id = {SkeevTalker};";
            foreach (var id in new[] { SkeevFighter, SkeevTalker })
            {
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
            yield return $"DELETE FROM spawnpool WHERE id = {Stark};";
            yield return $"DELETE FROM npc_package WHERE id = {Stark};";
            yield return $"DELETE FROM creature_stat WHERE id = {Stark};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Stark};";
            yield return $"DELETE FROM creature WHERE id = {Stark};";
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
