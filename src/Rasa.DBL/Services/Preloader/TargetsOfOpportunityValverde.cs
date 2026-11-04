using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;

namespace Rasa.Services.Preloader
{
    using Data;
    using Structures.World;

    public static partial class TargetsOfOpportunitySeed
    {
        /// <summary>
        /// The rest of the Targets of Opportunity of Plateau (1586) and Valverde Pools (1594): the
        /// rows of Valverde_targets_of_opportunity, as SQL. <see cref="Zones"/> put in only their
        /// kills, and "Complete All N" had nothing to complete it (forum bug report 2026-10-30, "It
        /// says there's 10 but only seeing 2"). Now each has every objective the client gives it
        /// (missionobjective.pyo), and the "all" objective is the sum of them.
        ///
        /// Plateau, "Complete All 10 Targets of Opportunity" (69): the waypoints (1), the five bosses
        /// (6, the sum of the client's hidden 62-66), the Logos (8), the story missions (55), Kael (56)
        /// and Mox (57) as before, 50 Maligo Elite Guards (58), the CID murder (59), the Penumbra
        /// agent (60) and the operations (61).
        /// Pools, "Complete All 8" (74): the waypoints (1), Thrax (5), the ten special Bane (6, the sum
        /// of the hidden 64-73), the Logos (8), the three operations (40, the sum of the hidden 59-61),
        /// the story missions (55), demon pups (56) and Striders (57).
        ///
        /// Counted as the titles that are already given for the same things count them, so the
        /// two agree: the waypoints are the map's teleporter rows of type Waypoint (WaypointTitles),
        /// the Logos its shrines (LogosTitles), the bosses the creature rows of BossTitles' names.
        /// Those titles (Plateau Pathfinder, Assassin, Mentalist; Pools Wanderer, Hunters Hunter,
        /// Mentalist) stay theirs: the objectives give none. The others are given here: Plateau
        /// Sentinel, Detective, Secret Agent, Operative and Veteran, Pools Operative and Veteran, and
        /// Master of Plateau and Pools Master for the "all" objective (titledata 403, 404, 408, 411,
        /// 412, 413, 474, 476, 478).
        ///
        /// Sources: the client (objectives, texts, counters, titles); TaRapedia Plateau and Pools
        /// Targets of Opportunity (the boss names and places, the CID chain Blue Flu - Looking For
        /// Clues, the Penumbra chain Clean Slate - Mind Slayer, Lieutenant Colonel Doss as Plateau's
        /// giver and receiver, 70,000 XP for Pools); Ten Ton Hammer Ustor Yard guide ("complete the
        /// Barbie mission to get Operations credit": 1190); TaRapedia and Ten Ton Hammer The Mad
        /// Bomber (856, the last of the Retread Caves); the known issues of 2007-11-14 ("Completing
        /// all the missions in Lamna Armory and Turpis Refinery do not give credit for completing
        /// the operations", "all the mission in Valverde Pools and the instances ... the Pools
        /// Story missions").
        ///
        /// ESTIMATE: no source lists the story missions. As upstream's Wilderness list
        /// (WildernessAliaOpeningV1.Targets): the zone's own missions and its instances', one group
        /// each, either of an exclusive pair in one group; not Logos fetches, the Targets of
        /// Opportunity, or content of a later deployment (Velon Hollow). Left out as well: what no
        /// one can finish on this server yet - the five Trinity Bridge Logos missions, whose goal is
        /// in another battlefield's instance, the chain after them that requires them (935 Array Of
        /// Hope to 1181 Send a Message), and 965 Mislaid Plans (a missing mechanic). Also ESTIMATE:
        /// Maligo Base's operation is 877 One Way Trip; Lamna Armory's and Turpis Refinery's are all
        /// their missions but the transport in (1305); the order of the hidden boss objectives; the
        /// Maligo Elite Guard counted by its row (750012), not the Guardmaster; Plateau's 70,000 XP
        /// (Pools'); Pools stays a radio mission (Scout Ryan, its giver, is not in the world); no
        /// clone credit (no mission gives one here).
        ///
        /// Three of Pools' ten special Bane had no row: Cainynix (a Howler), Grumble (a Treemite)
        /// and Oakril (a Treelurker). They are put where TaRapedia has them, on the navmesh's ground,
        /// level 35 as the other seven, health and armour as their formula
        /// (TargetsOfOpportunityGivers.Boss), attacks from their kin scaled by 2^(levels/8):
        /// Cainynix from Kennilaxx (Palisades, 28), Grumble from the Ashen Desert Tree Mite (39).
        /// No Treelurker stands anywhere: Oakril's claw and frighten (ActionId 183, 181) are the
        /// client's (actiondata), scaled as a boss's on a base argument.
        ///
        /// No Strider stood on the Pools but the boss Krammitron, so "Kill 10 Striders" (57) had
        /// only him. TaRapedia's Eastern Listening Post: "striders may spawn" at its beacon in a large
        /// crater (240.4, -141.2). A pool of two or three Thrax Cavalrymen (the Strider, class
        /// Vehicle_Bane_Strider_V1 9804, as the Foreas Base past's PlateauForeasBasePastNpcsV1: level
        /// 30, its health, armour and attacks) is put on the crater's floor 20 m south-west of the
        /// beacon (the beacon's own ground is a 44 m2 pocket of the navmesh), its respawn as the
        /// Pools' Thrax pools' (1200). ESTIMATE: the pool's size and the place in the crater.
        ///
        /// Character: a held mission with fewer objectives than its definition is cleared on login,
        /// so every holder of 1586 or 1594 is given the new objectives' rows (<see cref="CharUp"/>),
        /// as OneOffs does. Waypoints, Logos and missions done before count on the next login
        /// (ExistingFactObjectiveIds, the history aggregations); bosses killed before do not.
        /// </summary>
        public static class Valverde
        {
            public const uint PlateauMissionId = 1586, PoolsMissionId = 1594;
            public const uint Doss = 510184;
            public const uint PlateauAll = 69, PoolsAll = 74;
            public const uint Waypoints = 1, Bosses = 6, Logos = 8, Story = 55;
            public const uint EliteGuards = 58, Murder = 59, Penumbra = 60, PlateauOperations = 61;
            public const uint PoolsOperations = 40, RetreadCaves = 59, LamnaArmory = 60, TurpisRefinery = 61;
            public const uint Experience = 70000;
            public const uint EliteGuard = 750012, EliteGuardCount = 50;
            public const uint EvidenceId = 4;

            public const uint MasterOfPlateau = 412, PlateauVeteran = 411, PlateauOperative = 408, PlateauDetective = 403,
                PlateauSecretAgent = 404, PlateauSentinel = 413;
            public const uint PoolsMaster = 478, PoolsVeteran = 476, PoolsOperative = 474;

            /// <summary>The map's teleporter rows of type Waypoint (2), as WaypointTitles counts them.</summary>
            public static readonly uint[] PlateauWaypoints = { 52, 53, 54, 55, 56, 66, 155, 280, 282, 285, 287 };
            public static readonly uint[] PoolsWaypoints = { 48, 165, 166, 169, 289, 291, 293, 410, 411 };

            /// <summary>The Logos with a shrine on the map, as LogosTitles counts them.</summary>
            public static readonly uint[] PlateauLogos = { 48, 57, 85, 105, 148, 150, 194, 250, 302, 314 };
            public static readonly uint[] PoolsLogos = { 192, 198, 246, 261, 270, 350, 352, 353, 364, 407 };

            /// <summary>A hidden boss objective: the client's objective and texts, and the boss's creature row.</summary>
            public sealed record Boss(uint ObjectiveId, uint NameTextId, uint BodyTextId, string Name, uint CreatureId);

            public static readonly Boss[] PlateauBosses =
            {
                new(62, 14847, 14848, "Goliath", 520051),
                new(63, 14849, 14850, "Inquisitor Krakatus", 520052),
                new(64, 14851, 14852, "Mox Alpha Male", 520053),
                new(65, 14853, 14854, "Preceptor Vorvaak", 520054),
                new(66, 14855, 14856, "Prototype Juggernaut", 750026)
            };

            public static readonly Boss[] PoolsBosses =
            {
                new(64, 16892, 16893, "Goriam", 520056),
                new(65, 16896, 16897, "Krammitron", 520059),
                new(66, 16894, 16895, "Iceram", 520057),
                new(67, 16898, 16899, "Cainynix", 650247),
                new(68, 16900, 16901, "Grumble", 650248),
                new(69, 16902, 16903, "Cavalon", 520055),
                new(70, 16904, 16905, "Oakril", 650249),
                new(71, 16906, 16907, "Orax", 520061),
                new(72, 16908, 16909, "Irix", 520058),
                new(73, 16910, 16911, "Lililax", 520060)
            };

            /// <summary>The story missions, one group each; a group of two is either of an exclusive pair.</summary>
            public static readonly uint[][] PlateauStory =
            {
                // Giant Killer to One Way Trip and on (Bosley).
                new uint[] { 939 }, new uint[] { 1040 }, new uint[] { 1045 }, new uint[] { 1046 }, new uint[] { 1053 },
                new uint[] { 877 }, new uint[] { 1068 }, new uint[] { 1541 },
                // Maligo Base.
                new uint[] { 876 }, new uint[] { 878 }, new uint[] { 879 },
                // Ustor Yard.
                new uint[] { 1187 }, new uint[] { 1189 }, new uint[] { 1188 }, new uint[] { 1190 },
                // Lookout Down Below to Behind Enemy Lines.
                new uint[] { 970 }, new uint[] { 867 }, new uint[] { 555 },
                // The Defiant Ones and its three branches; the CID chain.
                new uint[] { 887 }, new uint[] { 713 }, new uint[] { 855 }, new uint[] { 954 }, new uint[] { 1027 },
                new uint[] { 1014 }, new uint[] { 895 }, new uint[] { 928 },
                // Clean Slate to Mind Slayer (Penumbra).
                new uint[] { 1983 }, new uint[] { 905 }, new uint[] { 929 }, new uint[] { 936 }, new uint[] { 719 },
                // A Voice From The Past, as far as can be done.
                new uint[] { 934 }, new uint[] { 1044 },
                // Comic Relief and on.
                new uint[] { 1251 }, new uint[] { 1015 }, new uint[] { 1017 },
                // The rest.
                new uint[] { 557 }, new uint[] { 930 }, new uint[] { 931 }, new uint[] { 933 }, new uint[] { 1034 }, new uint[] { 1825 }
            };

            public static readonly uint[][] PoolsStory =
            {
                // Death Patrol and the supply chain.
                new uint[] { 1315 }, new uint[] { 1321 }, new uint[] { 1322 }, new uint[] { 1323 }, new uint[] { 1324 },
                new uint[] { 1326 }, new uint[] { 1066 }, new uint[] { 1328 },
                // The Retread chain; Poison Control or Taking the Poison.
                new uint[] { 560 }, new uint[] { 561 }, new uint[] { 562 }, new uint[] { 563 }, new uint[] { 1329 },
                new uint[] { 1330 }, new uint[] { 1331 }, new uint[] { 1368, 1590 },
                // The Retread Caves.
                new uint[] { 1132 }, new uint[] { 1135 }, new uint[] { 1133 }, new uint[] { 1134 }, new uint[] { 1131 },
                new uint[] { 1136 }, new uint[] { 1138 }, new uint[] { 856 },
                // Bane Investigation.
                new uint[] { 932 }, new uint[] { 937 }, new uint[] { 938 }, new uint[] { 941 },
                // Lamna Armory, Turpis Refinery.
                new uint[] { 896 }, new uint[] { 898 }, new uint[] { 903 },
                new uint[] { 1305 }, new uint[] { 882 }, new uint[] { 884 }, new uint[] { 885 }, new uint[] { 886 }, new uint[] { 1079 },
                // The rest, and the Plateau chain played out from Retread Camp.
                new uint[] { 1025 }, new uint[] { 1314 }, new uint[] { 1308 }, new uint[] { 904 }, new uint[] { 1751 }
            };

            public static readonly uint[][] PlateauOperationMissions = { new uint[] { 1190 }, new uint[] { 877 } };
            public static readonly uint[][] RetreadCavesMissions = { new uint[] { 856 } };
            public static readonly uint[][] LamnaArmoryMissions = { new uint[] { 896 }, new uint[] { 898 }, new uint[] { 903 } };
            public static readonly uint[][] TurpisRefineryMissions =
                { new uint[] { 882 }, new uint[] { 884 }, new uint[] { 885 }, new uint[] { 886 }, new uint[] { 1079 } };

            /// <summary>An objective added: the client's ids, and the server's comment.</summary>
            public sealed record Added(uint MissionId, uint ObjectiveId, uint NameTextId, uint BodyTextId, uint? CounterTextId,
                bool Counted, string Comment);

            /// <summary>Every objective added, in the order of the log.</summary>
            public static readonly Added[] Objectives =
            {
                new(PlateauMissionId, Waypoints, 14827, 14828, 16277, true, "Plateau waypoints"),
                new(PlateauMissionId, Bosses, 14844, 14845, 14846, true, "Plateau bosses"),
                new(PlateauMissionId, Logos, 14857, 14858, 16278, true, "Plateau Logos"),
                new(PlateauMissionId, Story, 14859, 14860, null, false, "Plateau story missions"),
                new(PlateauMissionId, EliteGuards, 14836, 14837, 14838, true, "Maligo Elite Guards"),
                new(PlateauMissionId, Murder, 14829, 14830, 16279, true, "the CID murder"),
                new(PlateauMissionId, Penumbra, 14831, 14832, 16286, true, "Penumbra agent"),
                new(PlateauMissionId, PlateauOperations, 14839, 14840, 19732, true, "Plateau operations"),
                new(PoolsMissionId, Waypoints, 15177, 15178, null, false, "Pools waypoints"),
                new(PoolsMissionId, Bosses, 15191, 15192, 15193, true, "Pools special Bane"),
                new(PoolsMissionId, Logos, 15206, 15207, null, false, "Pools Logos"),
                new(PoolsMissionId, PoolsOperations, 15208, 15209, 15210, true, "Pools operations"),
                new(PoolsMissionId, Story, 15217, 15218, 15219, true, "Pools story missions"),
                new(PoolsMissionId, RetreadCaves, 15211, 15212, null, false, "Retread Caves"),
                new(PoolsMissionId, LamnaArmory, 15213, 15214, null, false, "Lamna Armory"),
                new(PoolsMissionId, TurpisRefinery, 15215, 15216, null, false, "Turpis Refinery")
            };

            /// <summary>Every objective added, with the hidden boss objectives.</summary>
            public static IReadOnlyList<Added> AllObjectives => Objectives
                .Concat(PlateauBosses.Select(boss => new Added(PlateauMissionId, boss.ObjectiveId, boss.NameTextId, boss.BodyTextId, null, false, boss.Name)))
                .Concat(PoolsBosses.Select(boss => new Added(PoolsMissionId, boss.ObjectiveId, boss.NameTextId, boss.BodyTextId, null, false, boss.Name)))
                .ToArray();

            private static Zone ZoneOf(uint missionId) => Zones.Single(zone => zone.MissionId == missionId);

            /// <summary>The scene binding of 1586 or 1594 with what is added to it: <see cref="TargetsOfOpportunitySeed.Scene"/> and more.</summary>
            public static MissionSceneDefinition Scene(uint missionId)
            {
                var zone = ZoneOf(missionId);
                var scene = TargetsOfOpportunitySeed.Scene(zone);
                var plateau = missionId == PlateauMissionId;
                var bosses = plateau ? PlateauBosses : PoolsBosses;

                foreach (var boss in bosses)
                {
                    scene.Credit[boss.ObjectiveId] = new MissionCreditPolicy(MissionCreditMode.NearbyParty, SquadRadius);
                    scene.ObjectiveRequirements[boss.ObjectiveId] = new MapRequirement(zone.Maps);
                }

                scene.ObjectiveAggregations = new Dictionary<uint, MissionObjectiveAggregation>
                {
                    [Bosses] = new MissionObjectiveAggregation(bosses.Select(boss => boss.ObjectiveId).ToArray(), (uint)bosses.Length, 0)
                };
                scene.ObjectiveHistoryAggregations = new Dictionary<uint, MissionHistoryAggregation>();
                scene.HiddenObjectiveIds = bosses.Select(boss => boss.ObjectiveId).ToList();
                scene.ExistingFactObjectiveIds = new List<uint> { Waypoints, Logos };

                if (plateau)
                {
                    scene.Titles[EliteGuards] = PlateauSentinel;
                    scene.Credit[EliteGuards] = new MissionCreditPolicy(MissionCreditMode.NearbyParty, SquadRadius);
                    scene.ObjectiveRequirements[EliteGuards] = new MapRequirement(zone.Maps);
                    scene.Titles[Murder] = PlateauDetective;
                    scene.Titles[Penumbra] = PlateauSecretAgent;
                    scene.Titles[PlateauOperations] = PlateauOperative;
                    scene.Titles[Story] = PlateauVeteran;
                    scene.Titles[PlateauAll] = MasterOfPlateau;
                    scene.ObjectiveHistoryAggregations[Story] = new MissionHistoryAggregation(PlateauStory);
                    scene.ObjectiveHistoryAggregations[Murder] = new MissionHistoryAggregation(new[] { new uint[] { 928 } }, 0);
                    scene.ObjectiveHistoryAggregations[Penumbra] = new MissionHistoryAggregation(new[] { new uint[] { 719 } }, 0);
                    scene.ObjectiveHistoryAggregations[PlateauOperations] = new MissionHistoryAggregation(PlateauOperationMissions, 0);
                    scene.ObjectiveAggregations[PlateauAll] = new MissionObjectiveAggregation(
                        new uint[] { Waypoints, Bosses, Logos, Story, 56, 57, EliteGuards, Murder, Penumbra, PlateauOperations }, 10);
                }
                else
                {
                    scene.Titles[PoolsOperations] = PoolsOperative;
                    scene.Titles[Story] = PoolsVeteran;
                    scene.Titles[PoolsAll] = PoolsMaster;
                    scene.ObjectiveHistoryAggregations[Story] = new MissionHistoryAggregation(PoolsStory, 0);
                    scene.ObjectiveHistoryAggregations[RetreadCaves] = new MissionHistoryAggregation(RetreadCavesMissions);
                    scene.ObjectiveHistoryAggregations[LamnaArmory] = new MissionHistoryAggregation(LamnaArmoryMissions);
                    scene.ObjectiveHistoryAggregations[TurpisRefinery] = new MissionHistoryAggregation(TurpisRefineryMissions);
                    scene.ObjectiveAggregations[PoolsOperations] = new MissionObjectiveAggregation(
                        new[] { RetreadCaves, LamnaArmory, TurpisRefinery }, 3, 0);
                    scene.HiddenObjectiveIds.AddRange(new[] { RetreadCaves, LamnaArmory, TurpisRefinery });
                    scene.ObjectiveAggregations[PoolsAll] = new MissionObjectiveAggregation(
                        new uint[] { Waypoints, 5, Bosses, Logos, PoolsOperations, Story, 56, 57 }, 8);
                }

                return scene;
            }

            // ---- the three new Pools bosses ----

            public sealed record Attack(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
                uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

            public static readonly Attack[] Attacks =
            {
                // Cainynix, from Kennilaxx's (520040, 28): 54003, 54004, 64013, x 2^(7/8).
                new(650250, "Cainynix (Howler boss) howler_melee_attack", 402, 1, 0.5, 3.0, 4000, 0, 182, 238, 1),
                new(650251, "Cainynix (Howler boss) howler_sonic_attack", 200, 1, 1.0, 40.0, 10000, 925, 95, 286, 7),
                new(650252, "Cainynix (Howler boss) howler_shriek", 512, 1, 0.0, 15.0, 30000, 1000, 0, 0, 1),
                // Grumble, from the Ashen Desert Tree Mite's (540019, 39): 55029, x 2^(-4/8).
                new(650253, "Grumble (Treemite boss) treemite_spit", 196, 1, 1.0, 20.0, 4000, 825, 143, 286, 4),
                // Oakril: no Treelurker stands anywhere. The client's actiondata: 183/1 windup 3000, reach 4, cooldown 100,
                // DAMAGE_AMOUNT 50-75, type 1; 181/1 no windup, reach 40, cooldown 8000, 48-94, type 7. Damage x 2^(34/8) x 0.5
                // (a boss on a base argument, as Kennilaxx's). Ranges as the kin's rows: a melee reach + 1 from 0.5, a ranged one from 1.
                new(650254, "Oakril (Treelurker boss) treelurker_claw", 183, 1, 0.5, 5.0, 100, 3000, 476, 714, 1),
                new(650255, "Oakril (Treelurker boss) treelurker_frighten", 181, 1, 1.0, 40.0, 8000, 0, 457, 894, 7)
            };

            public sealed record NewBoss(uint Id, string Comment, uint ClassId, uint NameId, double X, double Y, double Z, uint[] Actions)
            {
                public const uint Level = 35;
                public uint MaxHp => 100 * Level + 500;
                public uint Armor => 5 * Level + 200;
            }

            public const uint PoolsMap = 1304;

            /// <summary>Where TaRapedia has them (its x, z), at the navmesh's ground.</summary>
            public static readonly NewBoss[] NewBosses =
            {
                new(650247, "Cainynix - Pools", 10510, 10004, -560.0, 777.72327, -5.0, new uint[] { 650250, 650251, 650252 }),
                new(650248, "Grumble - Pools", 10348, 10019, -200.0, 864.3574, 600.0, new uint[] { 650253 }),
                new(650249, "Oakril - Pools", 10347, 10020, -600.0, 925.4, 650.0, new uint[] { 650254, 650255 })
            };

            /// <summary>The Pools' Striders (Thrax Cavalrymen): a public pool in the Eastern Listening Post's crater.</summary>
            public const uint Strider = 650256, StriderPool = 650256, StriderRespawn = 1200, StriderMin = 2, StriderMax = 3;
            public const double StriderX = 225.0, StriderY = 679.7, StriderZ = -155.0, StriderRadius = 10.0;

            // ---- the SQL ----

            private static readonly string[] RewardColumns =
            {
                "mission_id", "content_revision", "reward_id", "requirement", "experience", "credits", "prestige", "selection_count", "comment"
            };

            /// <summary>The ordinal of a mission's first added objective: after "complete all" (1) and its kills (2 on).</summary>
            private static uint FirstOrdinal(uint missionId) => (uint)ZoneOf(missionId).Kills.Count + 2;

            public static object[][] ObjectiveRows
            {
                get
                {
                    var rows = new List<object[]>();
                    foreach (var mission in new[] { PlateauMissionId, PoolsMissionId })
                    {
                        var ordinal = FirstOrdinal(mission);
                        foreach (var added in AllObjectives.Where(added => added.MissionId == mission))
                            rows.Add(new object[]
                            {
                                mission, Revision, added.ObjectiveId, Required, added.NameTextId, added.BodyTextId,
                                added.CounterTextId, null, null, ordinal++, Incomplete, 0, added.Comment
                            });
                    }
                    return rows.ToArray();
                }
            }

            /// <summary>The objectives that count by a trigger of their own; the rest are sums (the scene binding's aggregations).</summary>
            private static IEnumerable<(uint Mission, uint Objective, string Comment, (MissionProgressEventKind Kind, uint Subject)[] Subjects, uint? Target)> Counted()
            {
                yield return (PlateauMissionId, Waypoints, "every Plateau waypoint",
                    PlateauWaypoints.Select(id => (MissionProgressEventKind.WaypointAcquired, id)).ToArray(), (uint)PlateauWaypoints.Length);
                yield return (PlateauMissionId, Logos, "every Plateau Logos",
                    PlateauLogos.Select(id => (MissionProgressEventKind.LogosAcquired, id)).ToArray(), (uint)PlateauLogos.Length);
                yield return (PlateauMissionId, EliteGuards, $"{EliteGuardCount} Maligo Elite Guards killed",
                    new[] { (MissionProgressEventKind.CreatureKilled, EliteGuard) }, EliteGuardCount);
                foreach (var boss in PlateauBosses)
                    yield return (PlateauMissionId, boss.ObjectiveId, $"{boss.Name} killed",
                        new[] { (MissionProgressEventKind.CreatureKilled, boss.CreatureId) }, null);
                yield return (PoolsMissionId, Waypoints, "every Pools waypoint",
                    PoolsWaypoints.Select(id => (MissionProgressEventKind.WaypointAcquired, id)).ToArray(), null);
                yield return (PoolsMissionId, Logos, "every Pools Logos",
                    PoolsLogos.Select(id => (MissionProgressEventKind.LogosAcquired, id)).ToArray(), null);
                foreach (var boss in PoolsBosses)
                    yield return (PoolsMissionId, boss.ObjectiveId, $"{boss.Name} killed",
                        new[] { (MissionProgressEventKind.CreatureKilled, boss.CreatureId) }, null);
            }

            public static object[][] TransitionRows => Counted().Select(counted => new object[]
            {
                counted.Mission, Revision, counted.Objective, 1u, Required, 1u, Incomplete, Completed, counted.Comment
            }).ToArray();

            /// <summary>One trigger a subject: on counter 0 to the target where the objective counts, all of them seen where it does not.</summary>
            public static object[][] TriggerRows => Counted().SelectMany(counted => counted.Subjects.Select((subject, index) => new object[]
            {
                counted.Mission, Revision, counted.Objective, 1u, (uint)index + 1, Required, (byte)MissionTriggerKind.ProgressEvent,
                (uint)index + 1, (byte)subject.Kind, subject.Subject,
                counted.Target.HasValue ? (object)0u : null, counted.Target.HasValue ? (object)0u : null, counted.Target,
                $"{subject.Kind} {subject.Subject}"
            })).ToArray();

            public static object[][] RewardRows => new[]
            {
                new object[] { PlateauMissionId, Revision, 1u, Required, Experience, 0u, 0u, (byte)0, "Plateau Targets of Opportunity (ESTIMATE: Pools' XP)" },
                new object[] { PoolsMissionId, Revision, 1u, Required, Experience, 0u, 0u, (byte)0, "Pools Targets of Opportunity (TaRapedia: 70,000 XP)" }
            };

            private static readonly string[] RewardActionColumns =
            {
                "mission_id", "content_revision", "objective_id", "transition_id", "action_id", "requirement", "kind", "sequence", "reward_id", "comment"
            };

            /// <summary>
            /// The reward's reference, which the content validator asks for (one GrantReward action a
            /// mission). The reward is given at the turn-in: the action itself does nothing. It is on
            /// the waypoints' transition, as each mission has one.
            /// </summary>
            public static object[][] RewardActionRows => new[] { PlateauMissionId, PoolsMissionId }.Select(mission => new object[]
            {
                mission, Revision, Waypoints, 1u, 1u, Required, (byte)MissionActionKind.GrantReward, 1u, 1u, "Turn-in reward"
            }).ToArray();

            public static object[][] Evidence => new[]
            {
                new object[]
                {
                    PlateauMissionId, Revision, EvidenceId, (byte)MissionEvidenceOwnerKind.Mission, PlateauMissionId, (byte)MissionEvidenceSourceKind.Reconstruction,
                    "https://tabularasa.fandom.com/wiki/Plateau_Targets_of_Opportunity", "generated/client/missionobjective.pyo, titledata.pyo", 0.4,
                    "objectives 1, 6 (62-66), 8, 55, 58-61 added; story set and operations 877/1190 ESTIMATE; giver and receiver Doss; 70000 XP ESTIMATE"
                },
                new object[]
                {
                    PoolsMissionId, Revision, EvidenceId, (byte)MissionEvidenceOwnerKind.Mission, PoolsMissionId, (byte)MissionEvidenceSourceKind.Reconstruction,
                    "https://tabularasa.fandom.com/wiki/Pools_Targets_of_Opportunity", "generated/client/missionobjective.pyo, titledata.pyo", 0.4,
                    "objectives 1, 6 (64-73), 8, 40 (59-61), 55 added; story set and operations ESTIMATE; Cainynix, Grumble, Oakril placed; a Strider pool (Thrax Cavalrymen) in the Eastern Listening Post crater; 70000 XP"
                }
            };

            public static IEnumerable<string> WorldUp
            {
                get
                {
                    foreach (var attack in Attacks)
                        yield return "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) "
                            + $"values ({attack.Id}, {Literal(attack.Description)}, {attack.ActionId}, {attack.ArgId}, {Literal(attack.RangeMin)}, {Literal(attack.RangeMax)}, "
                            + $"{attack.Cooldown}, {attack.Windup}, {attack.MinDamage}, {attack.MaxDamage}, {attack.DamageType});";
                    foreach (var boss in NewBosses)
                    {
                        var actions = string.Join(", ", Enumerable.Range(0, 8).Select(index => index < boss.Actions.Length ? boss.Actions[index] : 0u));
                        yield return "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, "
                            + "action1, action2, action3, action4, action5, action6, action7, action8) values "
                            + $"({boss.Id}, {Literal(boss.Comment)}, {boss.ClassId}, 0, {NewBoss.Level}, {boss.MaxHp}, {boss.NameId}, 9, 5, {actions});";
                        yield return $"insert into creature_stat (id, body, mind, spirit, health, armor) values ({boss.Id}, 15, 15, 15, {boss.MaxHp}, {boss.Armor});";
                        yield return "insert into spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, "
                            + "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, "
                            + "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, "
                            + "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) values "
                            + $"({boss.Id}, 0, 0, {TargetsOfOpportunityGivers.BossRespawn}, {Literal(boss.X)}, {Literal(boss.Y)}, {Literal(boss.Z)}, 0, {PoolsMap}, "
                            + $"{boss.Id}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
                    }

                    var cavalryman = Missions.Plateau.PlateauForeasBasePastNpcsV1.Cavalryman;
                    yield return "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, "
                        + "action1, action2, action3, action4, action5, action6, action7, action8) "
                        + $"select {Strider}, 'Thrax Cavalryman - Pools (Strider)', class_id, faction, level, max_hp, name_id, run_speed, walk_speed, "
                        + $"action1, action2, action3, action4, action5, action6, action7, action8 from creature where id = {cavalryman};";
                    yield return "insert into creature_stat (id, body, mind, spirit, health, armor) "
                        + $"select {Strider}, body, mind, spirit, health, armor from creature_stat where id = {cavalryman};";
                    yield return "insert into spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, "
                        + "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, "
                        + "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, "
                        + "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) values "
                        + $"({StriderPool}, 0, 0, {StriderRespawn}, {Literal(StriderX)}, {Literal(StriderY)}, {Literal(StriderZ)}, 0, {PoolsMap}, "
                        + $"{Strider}, {StriderMin}, {StriderMax}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {Literal(StriderRadius)});";

                    yield return Insert(MissionObjectiveDefinitionEntry.TableName, ObjectiveColumns, ObjectiveRows);
                    yield return Insert(MissionObjectiveTransitionEntry.TableName, TransitionColumns, TransitionRows);
                    yield return Insert(MissionTriggerEntry.TableName, TriggerColumns, TriggerRows);
                    yield return Insert("mission_reward_definition", RewardColumns, RewardRows);
                    yield return Insert(MissionActionEntry.TableName, RewardActionColumns, RewardActionRows);
                    yield return Insert(MissionEvidenceEntry.TableName, EvidenceColumns, Evidence);
                    foreach (var mission in new[] { PlateauMissionId, PoolsMissionId })
                        yield return Binding(mission, Scene(mission));

                    // Plateau's is Doss's at Fort Defiance to give and take back, as TaRapedia has it.
                    yield return $"update mission_content_definition set giver_id = {Doss}, receiver_id = {Doss}, radio_completeable = 0 "
                        + $"where mission_id = {PlateauMissionId} and content_revision = '{Revision}' and giver_id is null and receiver_id is null;";
                    yield return $"delete from {ChannelPolicyTable} where mission_id = {PlateauMissionId} and content_revision = '{Revision}';";
                }
            }

            public static IEnumerable<string> WorldDown
            {
                get
                {
                    yield return Insert(ChannelPolicyTable, ChannelColumns, ChannelsOf(new[] { ZoneOf(PlateauMissionId) }));
                    yield return $"update mission_content_definition set giver_id = null, receiver_id = null, radio_completeable = 1 "
                        + $"where mission_id = {PlateauMissionId} and content_revision = '{Revision}' and giver_id = {Doss};";
                    foreach (var mission in new[] { PlateauMissionId, PoolsMissionId })
                    {
                        var ids = string.Join(", ", AllObjectives.Where(added => added.MissionId == mission).Select(added => added.ObjectiveId));
                        yield return Binding(mission, TargetsOfOpportunitySeed.Scene(ZoneOf(mission)));
                        yield return $"delete from {MissionEvidenceEntry.TableName} where mission_id = {mission} and content_revision = '{Revision}' and evidence_id = {EvidenceId};";
                        foreach (var table in new[] { MissionActionEntry.TableName, MissionTriggerEntry.TableName, MissionObjectiveTransitionEntry.TableName, MissionObjectiveDefinitionEntry.TableName })
                            yield return $"delete from {table} where mission_id = {mission} and content_revision = '{Revision}' and objective_id in ({ids});";
                        yield return $"delete from mission_reward_definition where mission_id = {mission} and content_revision = '{Revision}' and reward_id = 1;";
                    }
                    yield return $"delete from spawnpool where id = {StriderPool};";
                    yield return $"delete from creature_stat where id = {Strider};";
                    yield return $"delete from creature where id = {Strider};";
                    var bosses = string.Join(", ", NewBosses.Select(boss => boss.Id));
                    yield return $"delete from spawnpool where id in ({bosses});";
                    yield return $"delete from creature_stat where id in ({bosses});";
                    yield return $"delete from creature where id in ({bosses});";
                    yield return $"delete from creature_action where id in ({string.Join(", ", Attacks.Select(attack => attack.Id))});";
                }
            }

            /// <summary>The objectives on the character side: the counters each starts at 0, as taking the mission writes them.</summary>
            public static IEnumerable<string> CharUp
            {
                get
                {
                    foreach (var added in AllObjectives)
                    {
                        yield return "insert into character_mission_objective (character_id, mission_id, objective_id, objective_state) "
                            + $"select m.character_id, m.mission_id, {added.ObjectiveId}, {Incomplete} from character_mission m "
                            + "left join character_mission_objective o on o.character_id = m.character_id "
                            + $"and o.mission_id = m.mission_id and o.objective_id = {added.ObjectiveId} "
                            + $"where m.mission_id = {added.MissionId} and m.content_revision = '{Revision}' and o.character_id is null;";
                        if (added.Counted)
                            yield return "insert into character_mission_objective_counter (character_id, mission_id, objective_id, counter_id, counter_value) "
                                + $"select m.character_id, m.mission_id, {added.ObjectiveId}, 0, 0 from character_mission m "
                                + "left join character_mission_objective_counter c on c.character_id = m.character_id "
                                + $"and c.mission_id = m.mission_id and c.objective_id = {added.ObjectiveId} and c.counter_id = 0 "
                                + $"where m.mission_id = {added.MissionId} and m.content_revision = '{Revision}' and c.character_id is null;";
                    }
                }
            }

            public static IEnumerable<string> CharDown
            {
                get
                {
                    foreach (var mission in new[] { PlateauMissionId, PoolsMissionId })
                    {
                        var ids = string.Join(", ", AllObjectives.Where(added => added.MissionId == mission).Select(added => added.ObjectiveId));
                        foreach (var table in new[] { "character_mission_objective_counter", "character_mission_objective" })
                            yield return $"delete from {table} where mission_id = {mission} and objective_id in ({ids}) "
                                + $"and character_id in (select character_id from character_mission where mission_id = {mission} and content_revision = '{Revision}');";
                    }
                }
            }

            private static string Binding(uint missionId, MissionSceneDefinition scene)
            {
                var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
                if (json.Contains('\'') || json.Contains('\\'))
                    throw new InvalidOperationException($"The scene binding of mission {missionId} cannot be written as a SQL string.");
                return $"update {SceneBindingTable} set bindings = '{json}' where mission_id = {missionId} and content_revision = '{Revision}';";
            }
        }
    }
}
