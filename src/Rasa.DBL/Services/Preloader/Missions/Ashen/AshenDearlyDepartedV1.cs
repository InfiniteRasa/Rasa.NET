using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Dearly Departed (native mission 1285), Ligo Ashen Desert: Megumi Yamamoto at Shadow's Edge Post asks the player to find
    /// her father's wedding ring, lost while he fought the Thrax in the Boneyard; one of the Thrax has it. Captain Akio Yamamoto
    /// at Boneyard Outpost takes it back.
    ///
    /// Native: texts 10370-10373, 10374 "none" (missionconversation): log "Recover Captain Yamamoto's lost wedding ring, which
    /// was misplaced while he was fighting Thrax in the Boneyard. Yamamoto's Wedding Ring can be found on one of the many Thrax
    /// in the region. Return the ring to Captain Yamamoto ...", offer "I'm very sorry to bother you, but I have heard father
    /// speak highly of you. ..." (Megumi's), turn-in "What's this? You found it... You are bringing my wife back to me ..."
    /// (Yamamoto's); objective 1 "Recovery Yamamoto's Ring" 10375/10376, no counter (missionobjective); no objective
    /// conversation. Item class 25480 Yamamoto's Wedding Ring (template 97481). Names 9295 Megumi Yamamoto, 9303 Captain Akio
    /// Yamamoto.
    ///
    /// Sourced: TaRapedia Dearly Departed: giver Megumi Yamamoto (Shadow's Edge Post); requires Court Martial Arts; 190,000 XP,
    /// 8,400 credits; one of Pulsar Stealth Armor Boots, Pathogex Graviton Armor Helmet, Pathogex Hazmat Armor Legs, Audiodyne
    /// Motor Assist Armor Legs; the infobox's reward giver "Megumi" is contradicted by its own debriefing, by the client's log
    /// and turn-in and by TaRapedia Yamamoto's Wedding Ring ("the ring must be taken to Captain Yamamoto at Boneyard Outpost";
    /// "dropped off of a random Thrax during the mission"): receiver Yamamoto (triage D14). World DB: the Boneyard's Thrax pools
    /// 531038 (544.2, 283.2, -120, r14.4), 531039 (499, 285.5, -269.3, r10.7), 531041 (502, 277.5, -198.2, r12.6), each 2-3
    /// Thrax Soldiers (531008) and 0-1 Thrax Ravager (531009), respawn 900.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The ring a personal 15% drop (triage ESTIMATE list: the ring 15 %) off a Thrax Soldier or Ravager of Ashen Desert
    ///    (a drop is bound to creature rows and a map, not to a place: the Thrax of the Sheitan Highway and the Conscription Base
    ///    can drop it too), while 1 is open; one at most; taken at the turn-in; kept at an abandon (as Harmful Mutations'
    ///    samples), so a ring held counts again when the mission is taken again.
    ///  - Level 42 (D5, chain position). The armour the lowest Uncommon template of each kind in the band of level 42: Stealth
    ///    V02 Boots 38823, Graviton V02 Helmet 23828, Hazmat V02 Legs 20802, Motor Assist V04 Legs 29093; one to choose.
    ///  - The three Boneyard Thrax pools made point pools (radius 0), each moved to open ground 14-27 m from its upstream point
    ///    (as fix A did with 540067): upstream's radius pools (14.4, 10.7, 12.6 m) drew their Thrax with Detour's random point
    ///    round the circle, up to 47 m out (outside any marker round the point), onto rock tops over the ground (531038: two
    ///    navmesh floors 2.7-6 m apart) and 2-5 m off the rows' heights (the rows stand 1.7-2.75 m under the ground; probe of
    ///    2026-11-09, 6 rounds of respawns). On a point pool SpawnPoint sets them on the point and its rings (PointSpread) and
    ///    their strolls stay within 10 m; round each new point (ClearRadius) every navmesh floor is one floor, joined to the
    ///    Ashoka Settlement waypoint, on a client floor under open sky. The point on the client floor. Count, creatures and
    ///    respawn as upstream. Down puts upstream's rows back, to the float.
    ///  - A marker on each pool's new point, 25 m (the rings, 6.6 m, and a 10 m stroll).
    /// </summary>
    public static class AshenDearlyDepartedV1
    {
        public const uint MissionId = 1285;
        public const uint Level = 42;
        public const uint Megumi = AshenFormalComplaintsNpcsV1.Megumi;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint Recover = 1;
        public const int RingChance = 15;
        public const uint RingTemplate = 97481, RingClass = 25480;
        public const string RingKey = "yamamotos-wedding-ring";
        /// <summary>The Thrax that may drop it: Thrax Soldier - Ashen Desert, Thrax Grenadier - Ashen Desert (named Thrax Ravager by set 3).</summary>
        public const uint ThraxSoldier = 531008, ThraxRavager = 531009;
        public const double MarkerRadius = 25;

        /// <summary>
        /// The Boneyard's Thrax pools: pool; its row upstream (World DB: the single floats widened, so Down puts back the very
        /// values) and radius; its point now (radius 0, on the client floor); the marker's point (the navmesh ground there).
        /// </summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, double RadiusBefore, (double X, double Y, double Z) At, (double X, double Y, double Z) Marker)[] Pools =
        {
            (531038, (544.2000122070312, 283.20001220703125, -120), 14.4, (566, 285.1, -136), (566, 285.26, -136)),
            (531039, (499, 285.5, -269.29998779296875), 10.7, (487, 286.45, -261.3), (487, 286.68, -261.3)),
            (531041, (502, 277.5, -198.1999969482422), 12.6, (487, 271.9, -219), (487, 272.06, -219))
        };

        public const double RadiusNow = 0;

        /// <summary>How far from its point a point pool sets its creatures down: two rings of 1.1 spacings (Spacing.OnRings, as AshenFixAV1).</summary>
        public const double PointSpread = AshenFixAV1.PointSpread;

        /// <summary>How far round each point the ground is open and joined up: the rings and a 10 m stroll from the outer one.</summary>
        public const double ClearRadius = AshenFixAV1.ClearRadius;

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint StealthBoots = 38823;       // Armor_T3_Stealth_V02_UNC_Boots_40_to_44
        public const uint GravitonHelmet = 23828;     // Armor_T3_Graviton_V02_UNC_Helmet_40_to_44
        public const uint HazmatLegs = 20802;         // Armor_T2_Hazmat_V02_UNC_Legs_39_to_43
        public const uint MotorAssistLegs = 29093;    // Armor_T1_MotorAssist_V04_UNC_Legs_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Dearly Departed", 10370, Megumi, Yamamoto, Level);
            mission.Reward(Experience, Credits, new[] { StealthBoots, GravitonHelmet, HazmatLegs, MotorAssistLegs });
            mission.Prerequisite(AshenCourtMartialArtsV1.MissionId);

            mission.Objective(Recover, 10375, 10376, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ItemAcquired, RingClass, 1);
            mission.Action(Recover, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var sql in PoolsUp())
                migration.Sql(sql);
            foreach (var pool in Pools)
                mission.Indicator(Recover, marker++, pool.Marker.X, pool.Marker.Y, pool.Marker.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1285",
                "Native1 'Recovery Yamamoto's Ring' 10375/10376, no counter; no objectiveconversation. Texts 10370-10373 (the " +
                "turn-in is Captain Yamamoto's), 10374 'none'. Item class25480 Yamamoto's Wedding Ring (template97481). Names 9295 " +
                "Megumi Yamamoto, 9303 Captain Akio Yamamoto.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Dearly_Departed",
                "Giver Megumi Yamamoto (Shadow's Edge Post); requires Court Martial Arts; 190000XP, 8400 credits; Pulsar Stealth " +
                "Armor Boots, Pathogex Graviton Armor Helmet, Pathogex Hazmat Armor Legs, Audiodyne Motor Assist Armor Legs; the " +
                "ring off a random Thrax, taken to Captain Yamamoto at Boneyard Outpost (debriefing, Yamamoto's Wedding Ring).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDearlyDepartedV1",
                "Megumi 790202 new (Shadow's Edge Post). Receiver Yamamoto. The ring a personal 15% drop off Thrax Soldier 531008 " +
                "or Ravager 531009 on Ashen Desert, one, taken at the turn-in, kept at an abandon. Level42 estimated. Items 38823, " +
                "23828, 20802, 29093, one to choose. Boneyard Thrax pools 531038, 531039, 531041 made point pools on open ground 14-27 m " +
                "from their points (upstream's radius draws went 47 m out, onto rocks); a marker r25 on each.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Recover },
                Items = new()
                {
                    new MissionItemBinding(RingKey, RingTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { ThraxSoldier, ThraxRavager }, Recover, AshenMissionDataV1.MapContextId, RingChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration)
        {
            AshenMissionDataV1.Remove(migration, MissionId);
            foreach (var sql in PoolsDown())
                migration.Sql(sql);
        }

        /// <summary>Each pool from upstream's row to its point now, only where it is upstream's (a second run changes nothing).</summary>
        public static IEnumerable<string> PoolsUp() =>
            Pools.Select(pool => PoolSql(pool.Pool, pool.At, RadiusNow, pool.Before, pool.RadiusBefore));

        /// <summary>Each pool back to upstream's row, only where it is set 6's.</summary>
        public static IEnumerable<string> PoolsDown() =>
            Pools.Select(pool => PoolSql(pool.Pool, pool.Before, pool.RadiusBefore, pool.At, RadiusNow));

        private static string PoolSql(uint pool, (double X, double Y, double Z) to, double toRadius, (double X, double Y, double Z) from, double fromRadius) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, radius = {N(toRadius)} " +
            $"WHERE id = {pool} AND creature_1_Id = {ThraxSoldier} AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)} " +
            $"AND {Near("radius", fromRadius)};";

        /// <summary>A column at a value, as a single float holds it: within 0.01 (as AshenFixAV1).</summary>
        private static string Near(string column, double value) => $"ABS({column} - ({N(value)})) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
