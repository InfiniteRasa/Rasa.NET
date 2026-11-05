using System.Collections.Generic;
using System.Globalization;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    using Structures.World;

    /// <summary>
    /// The Marshes set 12 (east and north) NPCs and creatures, Valverde Marshes (map 1454): Elder Bargas at the Temple of
    /// Paludos and two Forean Machina pools east and north of Paludos (864), the Kael prison caravan on the Long Walk (542),
    /// Lieutenant Shaw at the eastern guard station with the creatures of his defence and Ranger Meeko (920).
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-03 against the World DB and the code of all worktrees:
    /// 760352-760399 free): 760352 Bargas (creature, creature_appearance, npc_package, pool), 760353 and 760354 the Machina
    /// pools (MarshesRetreadArenaNpcsV1's creature 760351), 760355 the Forean Prisoner Leader (creature, creature_appearance;
    /// his squad type, path and spawn share the id), 760356 the prison guard (creature, creature_stat), 760357 Shaw (creature,
    /// creature_appearance, npc_package, pool), 760358 Shaw's stand-in (creature, creature_appearance, creature_stat,
    /// npc_package), 760359 the guard station's AFS soldier (creature, creature_appearance, creature_stat), 760360 the
    /// Stalker of the attack (creature, creature_stat), 760361 Meeko (creature, creature_appearance, npc_package; no pool).
    ///
    /// Sourced: client names 8536 "Elder Bargas", 8625 "Lieutenant Shaw", 8627 "Ranger Meeko", 5311 "Forean Prisoner
    /// Leader" (creaturenamelanguage); Shaw's package 1012 and Meeko's 1013 (objectiveconversation 920,1 and 920,3).
    /// TaRapedia: Elder Bargas at the Temple of Paludos (-205, 230, 40); Bring Down the Kael's caravan along "the Eastern
    /// side of the long defensive line" (TaR-hist rev 20877), "approximately 500, 315" (rev 34767), (420, -300), (500, 100),
    /// (502, 134) (current page). Ten Ton Hammer: Lieutenant Shaw "at his guard station at 90,216,535". World DB: Elder
    /// Toida 510160 (Bargas' look), Ranger Taavik 510164 (Meeko's), Lieutenant Morrison 510162 (Shaw's), the Torcastra
    /// Forean Prisoner Leader 660023, the Marshes Thrax Soldier 531045, Stalker 540002, the Alia Das AFS squad Rifleman
    /// 670003 (class 29423, name 8717, its rifle; AliaDasAfsSquad).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Bargas a copy of Toida's row and look with his name, a server-only package (his id: the client has no talk on him),
    ///    at TaRapedia's /loc on the temple's upper floor. That floor is a navmesh island (probe of 2026-11-03: no walk from
    ///    the Paludos or the Falcon Hold waypoint); players stood there (TaRapedia), as Kiang's tower (set 5).
    ///  - The Machina: three each at (-250, 213.89, 200) north of Paludos and (-100, 217.11, 150) east of it (the client's
    ///    "east and north"; the triage's two groups of three), 120 s respawn, walked to from the Falcon Hold waypoint.
    ///  - The caravan: one NPC squad (as PoolsSupplyNpcsV1's Nakrinx): the prisoner leader (NEUTRAL, faction 3, as Nakrinx:
    ///    a friendly one his own Thrax guards take up a fight with, so the squad stands; no attack; a bystander:
    ///    npc_squad_type.leader_bystander; a player may shoot him) and four guards of their own row (a copy of the Thrax Soldier, so only they count),
    ///    in a wedge 3 m apart, on a 30 m tether. One prisoner, not the debriefing's three: only a squad's leader can be a
    ///    bystander (NpcSquads), and the client has the one "Forean Prisoner Leader". The beat: the navmesh corners from
    ///    (480, 320) on the Long Walk south to (420, -300), there and back (path mode Back), 10 s at each end; sent again 300 s
    ///    (ESTIMATE, PoolsSupplyNpcsV1's Nakrinx; the triage's 20 minutes is too long for one caravan per player's four kills)
    ///    after the last of it falls or, the prisoner left alone, after the last guard falls.
    ///  - Shaw a copy of Morrison's row and look at Ten Ton Hammer's place, public; his stand-in (the defence's leader, the
    ///    escort stats, as MarshesRetreadCityNpcsV1's Samuel) stands in for him while 920 runs. The guard station's soldier a
    ///    friendly copy of the AFS Rifleman with the escort stats (two of them stand by Shaw). The Stalker of the attack a
    ///    copy of the Marshes Stalker (its own row: no Calling all Parts motherboard from it). Meeko a copy of Taavik's row
    ///    and look with his name and the client's package; he comes only in the player's 920 (MarshesHeadEastV1).
    /// </summary>
    public static class MarshesEastAndNorthNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Toida = 510160, Taavik = 510164, Morrison = 510162, TorcastraPrisonerLeader = 660023;
        public const uint ThraxSoldier = 531045, MarshesStalker = 540002, AfsRifleman = AliaDasAfsSquad.Rifleman;
        public const uint EscortHealth = MarshesRetreadCityNpcsV1.EscortHealth, EscortArmor = MarshesRetreadCityNpcsV1.EscortArmor;

        public const uint Bargas = 760352, BargasName = 8536, BargasPackage = Bargas;
        public static readonly (double X, double Y, double Z) BargasAt = (-205, 230.28, 40);

        public const uint Machina = MarshesRetreadArenaNpcsV1.Machina;
        public const uint NorthMachinaPool = 760353, EastMachinaPool = 760354;
        public static readonly (double X, double Y, double Z) NorthMachinaAt = (-250, 213.89, 200);
        public static readonly (double X, double Y, double Z) EastMachinaAt = (-100, 217.11, 150);

        public const uint Prisoner = 760355, PrisonerName = 5311, PrisonerLevel = 33;
        public const uint Neutral = 3;                  // as PoolsSupplyNpcsV1's Nakrinx: his own guards do not fight him
        public const uint Guard = 760356, Guards = 4;
        public const uint Shaw = 760357, ShawName = 8625, ShawPackage = 1012;
        public const uint ShawStandIn = 760358, ShawLevel = 36;
        public const uint StationSoldier = 760359, StationSoldierLevel = 33;
        public const uint Stalker = 760360;
        public const uint Meeko = 760361, MeekoName = 8627, MeekoPackage = 1013, MeekoLevel = 33;
        public static readonly (double X, double Y, double Z) ShawAt = (90.4, 216.88, 535);

        /// <summary>The caravan's squad type, its route and its spawn share the prisoner's id.</summary>
        public const uint Squad = Prisoner;
        public const string SquadName = "The Kael prison caravan", Route = "The Kael prison caravan, the Long Walk";

        /// <summary>A wedge, 3 m apart; its own creatures' aggro range; the guards no further than 30 m from the prisoner; he a bystander.</summary>
        public const byte Formation = 0, LeaderBystander = 1;
        public const double Spacing = 3, EngageRange = 0, Tether = 30;

        /// <summary>There and back: at either end the squad turns and walks the points back.</summary>
        public const byte Mode = 1;
        public const uint EndPauseMs = 10000, RespawnSeconds = 300;    // ESTIMATE, as PoolsSupplyNpcsV1: one caravan is one player's four kills

        /// <summary>
        /// The way south, the corners of the navmesh's path from (480, 320) by (500, 100) and (460, -100) to (420, -300) (a
        /// navmesh probe of 2026-11-03). The way back is the same corners in turn.
        /// </summary>
        public static readonly (double X, double Y, double Z)[] LongWalk =
        {
            (480, 216.9, 320), (458.4, 219.5, 302.8), (468.8, 219.9, 217.2), (471.6, 217.9, 194.8), (476.4, 220.5, 172.4),
            (479.2, 222.3, 156.8), (487.6, 218.1, 149.6), (489.6, 217.1, 131.2), (500, 217, 100), (489.6, 216.9, 80),
            (460, 217.1, -100), (420, 219.6, -300),
        };

        /// <summary>The start, facing the first leg (south-west; NpcSquads' yaw, AbilityManager.YawTowards).</summary>
        public const double SpawnRotation = 0.898;

        /// <summary>10 s at each end, none between.</summary>
        public static uint PauseAt(int step) => step == 0 || step == LongWalk.Length - 1 ? EndPauseMs : 0;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, row, name, package, at) in new[]
            {
                (Bargas, "Elder Bargas - Marshes, Temple of Paludos (864)", Toida, BargasName, BargasPackage, BargasAt),
                (Shaw, "Lieutenant Shaw - Marshes, eastern guard station (920)", Morrison, ShawName, ShawPackage, ShawAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            yield return Pool(NorthMachinaPool, NorthMachinaAt.X, NorthMachinaAt.Y, NorthMachinaAt.Z, MapContextId, Machina,
                MarshesRetreadArenaNpcsV1.Machinas, MarshesRetreadArenaNpcsV1.Machinas, MarshesRetreadArenaNpcsV1.MachinaRespawnTenths,
                MarshesRetreadArenaNpcsV1.MachinaRadius);
            yield return Pool(EastMachinaPool, EastMachinaAt.X, EastMachinaAt.Y, EastMachinaAt.Z, MapContextId, Machina,
                MarshesRetreadArenaNpcsV1.Machinas, MarshesRetreadArenaNpcsV1.Machinas, MarshesRetreadArenaNpcsV1.MachinaRespawnTenths,
                MarshesRetreadArenaNpcsV1.MachinaRadius);

            // The caravan: the prisoner (neutral, no attack) and his guards.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Prisoner}, 'Forean Prisoner Leader - Marshes, the Long Walk (542)', class_id, {Neutral}, {PrisonerLevel}, max_hp, " +
                         $"{PrisonerName}, run_speed, walk_speed, 0, 0, 0, 0, 0, 0, 0, 0 FROM creature WHERE id = {TorcastraPrisonerLeader};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Prisoner}, slot_id, Class_id, color FROM creature_appearance WHERE id = {TorcastraPrisonerLeader};";
            foreach (var (id, comment, row, faction, level, health) in new (uint, string, uint, string, string, string)[]
            {
                (Guard, "Thrax Prison Guard - Marshes, the Long Walk (542)", ThraxSoldier, "faction", "level", "max_hp"),
                (Stalker, "Stalker - Marshes, the attack on the eastern guard station (920)", MarshesStalker, "faction", "level", "max_hp")
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, {faction}, {level}, {health}, name_id, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {row};";
            }

            // Shaw's stand-in, the station's soldier and Meeko: friendly; the first two the escort stats.
            foreach (var (id, comment, row, name, level, stats) in new (uint, string, uint, string, uint, bool)[]
            {
                (ShawStandIn, "Lieutenant Shaw - Marshes (Head East, Young Man stand-in)", Morrison, ShawName.ToString(), ShawLevel, true),
                (StationSoldier, "AFS Soldier - Marshes, eastern guard station (920)", AfsRifleman, "name_id", StationSoldierLevel, true),
                (Meeko, "Ranger Meeko - Marshes, eastern guard station (920)", Taavik, MeekoName.ToString(), MeekoLevel, false)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, 1, {level}, {(stats ? EscortHealth.ToString() : "max_hp")}, {name}, " +
                             $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                             $"FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                if (stats)
                    yield return Stat(id, EscortHealth, EscortArmor);
            }
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({ShawStandIn}, {ShawPackage}, 'Lieutenant Shaw - Marshes (Head East, Young Man)');";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Meeko}, {MeekoPackage}, 'Ranger Meeko - Marshes');";

            yield return $"INSERT INTO {NpcSquadTypeEntry.TableName} (id, name, formation, spacing, engage_range, tether, leader_bystander) " +
                $"VALUES ({Squad}, {S(SquadName)}, {Formation}, {N(Spacing)}, {N(EngageRange)}, {N(Tether)}, {LeaderBystander});";
            yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                $"VALUES ({Squad}, 0, {Prisoner}, 1, 1);";
            yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                $"VALUES ({Squad}, 1, {Guard}, {Guards}, {Guards});";
            yield return $"INSERT INTO {NpcSquadPathEntry.TableName} (id, map_context_id, name, mode) " +
                $"VALUES ({Squad}, {MapContextId}, {S(Route)}, {Mode});";
            for (var step = 0; step < LongWalk.Length; step++)
                yield return $"INSERT INTO {NpcSquadPathPointEntry.TableName} (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) " +
                    $"VALUES ({Squad}, {step}, {N(LongWalk[step].X)}, {N(LongWalk[step].Y)}, {N(LongWalk[step].Z)}, {PauseAt(step)}, 0, 0);";
            var start = LongWalk[0];
            yield return $"INSERT INTO {NpcSquadSpawnEntry.TableName} (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, " +
                "respawn_seconds, enabled, comment, arrival) " +
                $"VALUES ({Squad}, {MapContextId}, {Squad}, {Squad}, {N(start.X)}, {N(start.Y)}, {N(start.Z)}, {N(SpawnRotation)}, " +
                $"{RespawnSeconds}, 1, {S(SquadName)}, 0);";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM {NpcSquadSpawnEntry.TableName} WHERE id = {Squad};";
            yield return $"DELETE FROM {NpcSquadPathPointEntry.TableName} WHERE path_id = {Squad};";
            yield return $"DELETE FROM {NpcSquadPathEntry.TableName} WHERE id = {Squad};";
            yield return $"DELETE FROM {NpcSquadMemberEntry.TableName} WHERE type_id = {Squad};";
            yield return $"DELETE FROM {NpcSquadTypeEntry.TableName} WHERE id = {Squad};";
            yield return $"DELETE FROM spawnpool WHERE id IN ({Bargas}, {NorthMachinaPool}, {EastMachinaPool}, {Shaw});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Bargas}, {Shaw}, {ShawStandIn}, {Meeko});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Guard}, {ShawStandIn}, {StationSoldier}, {Stalker});";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Bargas} AND {Meeko};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Bargas} AND {Meeko};";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
