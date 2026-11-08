using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 12a NPCs, for Get to Chaukas (1577), Control the Brain (1521) and Fix the Factory (1522), the Chaukas
    /// Robotics Facility (squad instance 2146). The World DB has no row of Information Analyst Ruben or Cipher Specialist
    /// Brian. The facility's own Warden Bot Claviger pools (531119-531133, creature 531023) are upstream and stay as they are.
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-11a used 770000-770057):
    ///  - 770058 Ruben (creature, creature_appearance, creature_stat, npc_package, spawn pool) on the Crucible, at Outpost
    ///    Intrepid's Front Lines exit;
    ///  - 770059 Brian (creature, creature_appearance, creature_stat, npc_package, spawn pool, spawnpool_pose) in Chaukas, at
    ///    the entrance.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage): 9724 "Information Analyst Ruben", 9725 "Cipher Specialist Brian". Brian's
    ///    package 1800 (client objectiveconversation 1577,1; 1521,5; 1521,7). Ruben has no client package (no objective talks
    ///    with him in 1577; 1525 has no objectives): a server-only one, his id, as Corporal Shandy.
    ///  - Ruben "by the Front Lines exit of Outpost Intrepid" (PlanetTR-2842, 0.3); TaRapedia Information Analyst Ruben
    ///    962, 152, 152 (0.5). Brian "has secured the entrance" (TaRapedia Get to Chaukas, cache, 0.5); the Control Room "north
    ///    of this location" (TaRapedia Control the Brain, cache, 0.5). Ruben is female (TaRapedia).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Ruben a copy of Lieutenant Sherer's row and look (510030, class 3848 NPC_Human_Swapset_Female), Brian of Captain
    ///    DeLisi's (510019, as Shandy). Level 40, 4500 hit points and armour 300 as the other new NPCs, friendly.
    ///  - Ruben stands still (no speed, as the upstream givers). Brian walks with the player in 1521 (its scene's copy of his
    ///    row): run 7, walk 2 as the Plateau escorts (PlateauTheseOldBonesV1). His public pool's pose Standing (NpcPoses) keeps
    ///    the public Brian at his post: a posed creature does not stroll. The scene's copy has no pool pose and follows.
    ///  - The places (probe 2026-11-07, navmesh and client files): each on the navmesh and a client floor with nothing over the
    ///    head, outside every hostile pool's circle by ReachClearance or more, seen from 15 or 16 of 16 places 2 m round, walked
    ///    to from the arrival or the Outpost waypoint with no step the client fails, no lava. Ruben 4 m off TaRapedia's spot
    ///    (there 14 of 16 sides seen). Brian 28 m from the arrival, 35 m or more outside the Claviger circles.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows.
    /// </summary>
    public static class CrucibleChaukasNpcsV1
    {
        public const uint Crucible = CrucibleMissionDataV1.MapContextId;
        public const uint Chaukas = 2146;               // adv_arieki_ligo_crucible_chaukas, a squad instance (SquadInstancePolicies)
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1, NpcLevel = 40;

        /// <summary>Chaukas's arrival (map link 98's destination) and its door back (map link 126, radius 6).</summary>
        public static readonly (double X, double Y, double Z) ArrivalAt = (-31.9056, 17.2305, -282.1061);
        /// <summary>Chaukas's door on the Crucible (map link 98, radius 6).</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (-528.5979, 249.4359, 581.6433);
        /// <summary>The floor before the door on the Crucible, 18 m south of it (a marker for the way in).</summary>
        public static readonly (double X, double Y, double Z) DoorWayAt = (-528.6, 249.62, 563.6);

        public const uint Ruben = 770058, RubenName = 9724, RubenPackage = 770058;
        public static readonly (double X, double Y, double Z) RubenAt = (966, 152.73, 156);
        public const uint Brian = 770059, BrianName = 9725, BrianPackage = 1800;
        public static readonly (double X, double Y, double Z) BrianAt = (-38, 17.44, -256);
        public const uint BrianRunSpeed = Plateau.PlateauTheseOldBonesV1.EscortSpeed, BrianWalkSpeed = Plateau.PlateauTheseOldBonesV1.EscortWalk;
        public const byte StandingPose = 1;             // Data.NpcPose.Standing

        /// <summary>The upstream rows the new NPCs copy: Lieutenant Sherer (Ruben), Captain DeLisi (Brian).</summary>
        public const uint Sherer = CrucibleOutpostIntrepidNpcsV1.Sherer, DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;

        /// <summary>The facility's Warden Bot Claviger (upstream creature 531023, class 6961 Creature_Prison_Bot_Warden, level 43).</summary>
        public const uint Claviger = 531023;
        /// <summary>The Claviger pools in the Brain room (upstream, radius 6, 6.5 and 15).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At, double Radius)[] BrainRoomPools =
        {
            (531127, (-16, 15.2, 10.1), 6), (531128, (-15.6, 16, 114.5), 6.5), (531129, (23.6, 16, 37.8), 15)
        };

        public const uint NextFreeId = 770060;

        /// <summary>Where an NPC of set 12a stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool == Ruben ? RubenAt : BrianAt;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, package, map, at, run, walk) in new[]
            {
                (Ruben, "Information Analyst Ruben - Intrepid exit (1577)", Sherer, RubenName, RubenPackage,
                    Crucible, RubenAt, 0U, 0U),
                (Brian, "Cipher Specialist Brian - Chaukas (1577,1521,1522)", DeLisi, BrianName,
                    BrianPackage, Chaukas, BrianAt, BrianRunSpeed, BrianWalkSpeed)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {NpcLevel}, {HitPoints}, {name}, {run}, {walk}, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, map, id, 1, 1, 20, 0);
            }
            yield return $"INSERT INTO spawnpool_pose (id, pose) VALUES ({Brian}, {StandingPose});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool_pose WHERE id = {Brian};";
            foreach (var id in new[] { Brian, Ruben })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
