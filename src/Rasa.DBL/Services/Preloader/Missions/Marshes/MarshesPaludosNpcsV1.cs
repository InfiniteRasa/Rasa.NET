using System.Collections.Generic;
using System.Globalization;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    using Structures.World;

    /// <summary>
    /// The Marshes set 1 (Paludos) NPCs and pools, Valverde Marshes (map 1454): Elder Toida's three living siblings for
    /// Divided We Fall (917) and the mission-only creature pools east of Paludos for What's that Gas? (539). The server had
    /// none of them.
    ///
    /// Id block (checked 2026-11-02 against the World DB and the code: no creature, spawnpool, npc_package or npc_squad row
    /// and no constant uses 760300-760399; the Pools batch ends at 760245): the Marshes missions take 760300-760399.
    /// Set 1 uses 760300-760308:
    ///  - 760300 Ranger Alia (creature, npc_package; her squad type, path and spawn share the id);
    ///  - 760301 Shaman Siva, 760302 Warrior Veska (creature, npc_package, spawn pool each);
    ///  - 760303-760308 the six mission pools of 539 (spawn pools of existing Marshes creature rows, no new creature).
    ///
    /// Sourced: client names 8601 "Ranger Alia", 8602 "Shaman Siva", 8600 "Warrior Veska" (creaturenamelanguage); their
    /// packages from objectiveconversation (917,1) 985 Alia, (917,2) 986 Siva, (917,3) 984 Veska. TaRapedia Divided We
    /// Fall and the NPC pages: Siva (-291, 218, 25) "Inside Paludos", Veska (-38, 219, 49); TaRapedia history rev 23786
    /// (2007-12-24): "Ranger alia found patrolling on the road at -381, 671". That pair is a map-view value: with the
    /// Marshes' map-view to /loc rule (z = map Y - 160, NOTES-marshes-triage) it is (-381, 511), 20 m from the navmesh road
    /// from Plateau Point to Falcon Hold (navmesh probe of 2026-11-02), which TaRapedia names as her road.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Classes: Alia NPC_Forean_Archer 7036 with the Forean bow (10529) ("Ranger"), Siva NPC_Forean_Shaman 7035 with the
    ///    staff (10533), Veska NPC_Forean_Spearman 7034 with the spear (10532), the slot-13 weapons the server's other Forean
    ///    NPCs carry (World DB creature_appearance). Level 35 (the mission's), 1000 hit points, friendly (faction 1).
    ///  - Alia's beat: an NPC squad of one (as PoolsSupplyNpcsV1's Nakrinx, her own type's leader a bystander: she takes up
    ///    no fight), walked there and back on four corners of the navmesh road through TaRapedia's point, 10 s at each end.
    ///  - Heights from the navmesh (probe of 2026-11-02).
    ///  - 539's pools: two each of Swamp Grubber 540014, Maw 531052 and Mox 540013 (the Marshes rows; the open world has
    ///    only 2 Grubber, 6 Maw and 3 Mox pools, World DB) east of Paludos (the client log names "east of Paludos" for the
    ///    ferns), three creatures a pool, radius 6, 120 s respawn (the Marshes pools' respawn_time 1200, World DB).
    ///
    /// Plain SQL, valid on SQLite and MySQL; down takes out only these rows.
    /// </summary>
    public static class MarshesPaludosNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Level = 35, HitPoints = 1000, Friendly = 1;

        public const uint Alia = 760300, AliaName = 8601, AliaPackage = 985, AliaClass = 7036, AliaWeapon = 10529;
        public const uint Siva = 760301, SivaName = 8602, SivaPackage = 986, SivaClass = 7035, SivaWeapon = 10533;
        public const uint Veska = 760302, VeskaName = 8600, VeskaPackage = 984, VeskaClass = 7034, VeskaWeapon = 10532;

        public static readonly (double X, double Y, double Z) SivaAt = (-291, 218.5, 25);
        public static readonly (double X, double Y, double Z) VeskaAt = (-38, 219.2, 49);

        /// <summary>Alia's squad type, path and spawn share her id.</summary>
        public const uint AliaSquad = Alia;
        public const string AliaSquadName = "Ranger Alia on the road", AliaRoute = "Ranger Alia's road, Plateau Point - Falcon Hold";
        public const byte Formation = 0, LeaderBystander = 1, Mode = 1;   // a wedge; she a bystander; there and back
        public const double Spacing = 3, EngageRange = 0, Tether = 30;
        public const uint EndPauseMs = 10000, RespawnSeconds = 300;

        /// <summary>
        /// Alia's beat: four corners of the navmesh path from the Plateau Point waypoint to the Falcon Hold waypoint, round
        /// TaRapedia's (-381, 511) (navmesh probe of 2026-11-02). Walked back the same way.
        /// </summary>
        public static readonly (double X, double Y, double Z)[] AliaBeat =
        {
            (-460.4, 217.5, 460.4), (-420.0, 216.9, 472.4), (-370.0, 215.3, 492.0), (-313.6, 216.9, 524.0)
        };

        /// <summary>The centre of Alia's beat and a marker radius that takes in all of it.</summary>
        public static readonly (double X, double Y, double Z) AliaBeatCentre = (-387, 216.5, 492.2);
        public const double AliaBeatRadius = 85;

        /// <summary>The facing of her first leg, east-north-east (AbilityManager.YawTowards, as PoolsSupplyNpcsV1.SpawnRotation).</summary>
        public const double AliaRotation = -1.8595;

        public const uint SwampGrubber = 540014, Maw = 531052, Mox = 540013;
        public const uint PerPool = 3, PoolRadius = 6, PoolRespawnTenths = 1200;

        /// <summary>539's mission pools east of Paludos (navmesh heights).</summary>
        public static readonly (uint Id, uint Creature, double X, double Y, double Z)[] GasPools =
        {
            (760303, SwampGrubber, -140, 214.2, 0), (760304, SwampGrubber, -100, 215.2, 40),
            (760305, Maw, -158.8, 220.3, 40.4), (760306, Maw, -120, 216.9, 90),
            (760307, Mox, -200, 217.3, -20), (760308, Mox, -150, 217.2, 150)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Npc(Alia, "Ranger Alia - Marshes road (Divided We Fall)", AliaClass, AliaName, AliaWeapon, AliaPackage))
                yield return sql;
            yield return $"INSERT INTO {NpcSquadTypeEntry.TableName} (id, name, formation, spacing, engage_range, tether, leader_bystander) " +
                $"VALUES ({AliaSquad}, {S(AliaSquadName)}, {Formation}, {N(Spacing)}, {N(EngageRange)}, {N(Tether)}, {LeaderBystander});";
            yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                $"VALUES ({AliaSquad}, 0, {Alia}, 1, 1);";
            yield return $"INSERT INTO {NpcSquadPathEntry.TableName} (id, map_context_id, name, mode) " +
                $"VALUES ({AliaSquad}, {MapContextId}, {S(AliaRoute)}, {Mode});";
            for (var step = 0; step < AliaBeat.Length; step++)
                yield return $"INSERT INTO {NpcSquadPathPointEntry.TableName} (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) " +
                    $"VALUES ({AliaSquad}, {step}, {N(AliaBeat[step].X)}, {N(AliaBeat[step].Y)}, {N(AliaBeat[step].Z)}, " +
                    $"{(step == 0 || step == AliaBeat.Length - 1 ? EndPauseMs : 0)}, 0, 0);";
            var start = AliaBeat[0];
            yield return $"INSERT INTO {NpcSquadSpawnEntry.TableName} (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, " +
                "respawn_seconds, enabled, comment, arrival) " +
                $"VALUES ({AliaSquad}, {MapContextId}, {AliaSquad}, {AliaSquad}, {N(start.X)}, {N(start.Y)}, {N(start.Z)}, {N(AliaRotation)}, " +
                $"{RespawnSeconds}, 1, {S(AliaSquadName)}, 0);";

            foreach (var sql in Npc(Siva, "Shaman Siva - Paludos (Divided We Fall)", SivaClass, SivaName, SivaWeapon, SivaPackage))
                yield return sql;
            yield return Pool(Siva, SivaAt.X, SivaAt.Y, SivaAt.Z, MapContextId, Siva, 1, 1, 20, 0);
            foreach (var sql in Npc(Veska, "Warrior Veska - Marshes (Divided We Fall)", VeskaClass, VeskaName, VeskaWeapon, VeskaPackage))
                yield return sql;
            yield return Pool(Veska, VeskaAt.X, VeskaAt.Y, VeskaAt.Z, MapContextId, Veska, 1, 1, 20, 0);

            foreach (var (id, creature, x, y, z) in GasPools)
                yield return Pool(id, x, y, z, MapContextId, creature, PerPool, PerPool, PoolRespawnTenths, PoolRadius);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (id, _, _, _, _) in GasPools)
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
            yield return $"DELETE FROM {NpcSquadSpawnEntry.TableName} WHERE id = {AliaSquad};";
            yield return $"DELETE FROM {NpcSquadPathPointEntry.TableName} WHERE path_id = {AliaSquad};";
            yield return $"DELETE FROM {NpcSquadPathEntry.TableName} WHERE id = {AliaSquad};";
            yield return $"DELETE FROM {NpcSquadMemberEntry.TableName} WHERE type_id = {AliaSquad};";
            yield return $"DELETE FROM {NpcSquadTypeEntry.TableName} WHERE id = {AliaSquad};";
            foreach (var id in new[] { Veska, Siva, Alia })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        /// <summary>A friendly Forean NPC with its weapon and client package (no pool: the caller adds one or a squad).</summary>
        private static IEnumerable<string> Npc(uint id, string comment, uint classId, uint name, uint weapon, uint package)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, {S(comment)}, {classId}, {Friendly}, {Level}, {HitPoints}, {name}, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, 13, {weapon}, 1);";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
