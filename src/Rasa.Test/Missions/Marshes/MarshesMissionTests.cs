using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Marshes
{
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Services.Preloader.Missions.Pools;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Valverde Marshes missions (MarshesMissionDataV1, revision marshes_1_16_5): set 1 at Paludos, Elder Toida's Roll
    /// out the Welcome Wagon (541) and Divided We Fall (917), Sergeant Starling's Cannister Collection (498) and Shaman
    /// Geli's What's that Gas? (539), with Ranger Alia, Shaman Siva, Warrior Veska and the mission pools east of Paludos
    /// (Marshes_paludos); set 2 at Falcon Hold, Research Chemist Horsh's Tastes Like Grubber (959) and Fixing the Food Chain
    /// (961), with Horsh and two Grubber pools west of Falcon Hold, and Retread Vincent's It Lies in Ruins (1673), which
    /// stays on the Plateau revision with Retreads to Retreads before it (PlateauMissionTests pins it there)
    /// (Marshes_falcon_hold).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesMissionTests
    {
        /// <summary>The Marshes missions the World migrations enable on the Marshes revision, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            MarshesWelcomeWagonV1.MissionId, MarshesCannisterCollectionV1.MissionId, MarshesWhatsThatGasV1.MissionId,
            MarshesDividedWeFallV1.MissionId, MarshesTastesLikeGrubberV1.MissionId, MarshesFixingTheFoodChainV1.MissionId
        };

        /// <summary>It Lies in Ruins: a Marshes mission on the Plateau revision (in PlateauMissionTests.MissionIds).</summary>
        private const uint ItLiesInRuins = MarshesItLiesInRuinsV1.MissionId;

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): Roll out the Welcome Wagon's one, on
        /// Cast'ka; Cannister Collection's three, on the western Thrax pools; What's that Gas?'s seven (the "Calla Ferns" one
        /// round the Grubber pools, and one on each of the six mission pools for 2-4); Divided We Fall's four (Alia's beat,
        /// Siva, Veska, Helka). Tastes Like Grubber's two, on its pools; Fixing the Food Chain's twelve, one on each feeder.
        /// It Lies in Ruins' one, at Morrison, is counted with the Plateau revision's (PlateauMissionTests.Indicators).
        /// </summary>
        internal const int Indicators = 29;

        private const string Paludos = "20271104000000_Marshes_paludos";
        private const string FalconHold = "20271104000100_Marshes_falcon_hold";

        /// <summary>
        /// The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), the target that
        /// rolls <paramref name="migration"/> back: not a fixed id, so another migration slotted in between cannot be rolled
        /// back with it unseen.
        /// </summary>
        private static string Previous(Microsoft.EntityFrameworkCore.DbContext world, string migration)
        {
            var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, $"{migration} has a migration before it");
            return ids[index - 1];
        }

        [TestMethod]
        public void TheMissionsAreEnabledOperationalAndGivenByTheirNpcs()
        {
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level)[]
            {
                (541, 510160, 510160, 36), (498, 510171, 510171, 35), (539, 510172, 510172, 34), (917, 510160, 510160, 35),
                (959, 760309, 760309, 33), (961, 760309, 760309, 37)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(10000051U, entry.CategoryId, "Battlefield (Marshes)");
            }

            // It Lies in Ruins: Vincent to Morrison, level 35, on the Plateau revision but in the Marshes' category.
            var ruins = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == ItLiesInRuins && entry.Enabled);
            Assert.AreEqual(PlateauMissionDataV1.Revision, ruins.ContentRevision);
            Assert.AreEqual(((uint?)510168, (uint?)510162, (uint?)35, (uint?)10000051), (ruins.GiverId, ruins.ReceiverId, ruins.Level, ruins.CategoryId));

            // Alia, Siva and Veska: friendly Foreans of level 35 with the client's names and packages, on Valverde Marshes;
            // Siva and Veska in pools, Alia a squad of one (no pool).
            foreach (var (npc, name, package, cls) in new (uint, uint, uint, uint)[]
            {
                (760300, 8601, 985, 7036), (760301, 8602, 986, 7035), (760302, 8600, 984, 7034)
            })
            {
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 35U, 1U, cls), (creature.NameId, creature.Level, creature.Faction, creature.ClassId), $"{npc}");
                Assert.AreEqual(package, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
            }
            foreach (var npc in new uint[] { 760301, 760302, 760309 })
                Assert.AreEqual(1454U, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc).MapContextId, $"{npc} map");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760300), "Alia: a squad, no pool");
            var type = harness.World.NpcSquadTypeEntries.AsNoTracking().Single(row => row.Id == 760300);
            Assert.AreEqual((byte)1, type.LeaderBystander, "Alia takes up no fight");
            CollectionAssert.AreEqual(new[] { (760300U, (byte)1, (byte)1) },
                harness.World.NpcSquadMemberEntries.AsNoTracking().Where(row => row.TypeId == 760300).AsEnumerable()
                    .Select(row => (row.CreatureId, row.MinCount, row.MaxCount)).ToArray(), "Alia alone");
            var path = harness.World.NpcSquadPathEntries.AsNoTracking().Single(row => row.Id == 760300);
            Assert.AreEqual((1454U, (byte)1), (path.MapContextId, path.Mode), "on the Marshes, there and back");
            Assert.AreEqual(4, harness.World.NpcSquadPathPointEntries.AsNoTracking().Count(row => row.PathId == 760300), "four corners");

            // Horsh: the client's name 3142, a server-only package (his id), Vetter's class, level and look (510165).
            var horsh = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760309);
            Assert.AreEqual((3142U, 3846U, 10U, 1U), (horsh.NameId, horsh.ClassId, horsh.Level, horsh.Faction), "Horsh");
            Assert.AreEqual(760309U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760309).PackageId, "Horsh's package");
            int Slots(uint id) => harness.World.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {id}").AsEnumerable().Single();
            Assert.AreEqual(6, Slots(760309), "Horsh wears Vetter's six slots");
            Assert.AreEqual(Slots(510165), Slots(760309));

            // 539's six pools of three: two each of Swamp Grubber, Maw and Mox; 959's two Swamp Grubber pools; all on the Marshes.
            foreach (var (id, creature) in new (uint, uint)[]
            {
                (760303, 540014), (760304, 540014), (760305, 531052), (760306, 531052), (760307, 540013), (760308, 540013),
                (760310, 540014), (760311, 540014)
            })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                Assert.AreEqual((1454U, creature, (byte)3, (byte)3), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount), $"pool {id}");
            }

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds.Append(ItLiesInRuins))
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The client's own texts: the ids are written out here from the client tables (missionconversation,
        /// missionobjective), not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var marshes = MarshesMissionDataV1.Revision;
            var expected = new (uint Mission, string Revision, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (541, marshes, 1454, new (uint, uint, uint, uint?)[] { (1, 1496, 7423, 1497) }),
                (498, marshes, 1238, new (uint, uint, uint, uint?)[] { (1, 1433, 8304, null) }),
                (539, marshes, 1446, new (uint, uint, uint, uint?)[] { (1, 1542, 9361, null), (2, 1543, 9362, null), (3, 1544, 9363, null),
                    (4, 1545, 9364, null) }),
                (917, marshes, 4899, new (uint, uint, uint, uint?)[] { (1, 4958, 7105, null), (2, 4960, 7106, null), (3, 4962, 7107, null),
                    (4, 4964, 7108, null) }),
                (959, marshes, 5211, new (uint, uint, uint, uint?)[] { (1, 5215, 7002, null) }),
                (961, marshes, 5221, new (uint, uint, uint, uint?)[] { (1, 5225, 7004, 5268) }),
                (1673, PlateauMissionDataV1.Revision, 16101, new (uint, uint, uint, uint?)[] { (1, 16106, 16107, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, revision, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision).ToArray();
                CollectionAssert.AreEquivalent(objectives.Select(row => row.Objective).ToArray(),
                    actual.Select(row => row.ObjectiveId).ToArray(), $"{mission} objectives");
                foreach (var (objective, name, body, counter) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.AreEqual(counter, row.ClientCounter0TextId, $"{mission}.{objective} counter0");
                    Assert.IsNull(row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }

            (uint, uint?)[] Talks(uint mission, string revision) => harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision &&
                    entry.Kind == MissionTriggerKind.Conversation).AsEnumerable()
                .Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
            // 917's three talks at the client's packages (objectiveconversation 917,1 985; 917,2 986; 917,3 984).
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 985), (2, 986), (3, 984) }, Talks(917, marshes));
            // 1673's at Morrison's (objectiveconversation 1673,1 package 299); 959 and 961 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 299) }, Talks(1673, PlateauMissionDataV1.Revision));
            Assert.IsEmpty(Talks(959, marshes));
            Assert.IsEmpty(Talks(961, marshes));
        }

        /// <summary>
        /// Set 1 has no requirement (TaRapedia, TR MMO DB). Fixing the Food Chain follows Tastes Like Grubber, and It Lies in
        /// Ruins Retreads to Retreads (TaRapedia), on one revision each. None repeats.
        /// </summary>
        [TestMethod]
        public void OnlyTheFoodChainAndTheRuinsRequireAMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CollectionAssert.AreEqual(new[] { (961U, (uint?)959) }, harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.RequiredMissionId)).ToArray());
            var ruins = harness.World.MissionPrerequisiteEntries.AsNoTracking().Single(entry => entry.MissionId == ItLiesInRuins);
            Assert.AreEqual((PlateauMissionDataV1.Revision, MissionPrerequisiteKind.MissionCompleted, (uint?)1751),
                (ruins.ContentRevision, ruins.Kind, ruins.RequiredMissionId));
            Assert.AreEqual(PlateauMissionDataV1.Revision, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == 1751 && entry.Enabled).ContentRevision, "1751 on the same revision");
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision || entry.MissionId == ItLiesInRuins));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision ||
                    entry.ContentRevision == PlateauMissionDataV1.Revision && entry.MissionId == ItLiesInRuins).ToArray();
            foreach (var mission in MissionIds.Append(ItLiesInRuins))
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    },
                    evidence.Where(entry => entry.MissionId == mission).OrderBy(entry => entry.EvidenceId)
                        .Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(), $"{mission} evidence");
        }

        /// <summary>The rewards: XP and credits of TaRapedia, the four items (or stacks) to choose one of (written out here).</summary>
        [TestMethod]
        public void TheRewardsAreTaRapediasWithOneItemToChoose()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = MarshesMissionDataV1.Revision;
            foreach (var (mission, revision, experience, credits, items) in new (uint, string, uint, uint, (uint, uint)[])[]
            {
                (541, marshes, 75000, 5700, new (uint, uint)[] { (45762, 1), (45812, 1), (48680, 1), (14937, 1) }),
                (498, marshes, 65000, 5400, new (uint, uint)[] { (48108, 1), (15150, 1), (14738, 1), (49136, 1) }),
                (539, marshes, 65000, 5400, new (uint, uint)[] { (47944, 1), (47728, 1), (15513, 1), (14404, 1) }),
                (917, marshes, 65000, 5400, new (uint, uint)[] { (20335, 1), (28614, 1), (35571, 1), (39191, 1) }),
                // Armor Module: Resist Electric, Fire, Ice, Virulent [4].
                (959, marshes, 65000, 5400, new (uint, uint)[] { (123016, 1), (123026, 1), (123031, 1), (123071, 1) }),
                // 2 Class VII EMP Bomb, 2 Class VII Adrenaline Booster, 4 Class VII Fragmentation, 4 Class VII Cryogenic Grenade.
                (961, marshes, 32500, 3600, new (uint, uint)[] { (111232, 2), (118814, 2), (45446, 4), (111048, 4) }),
                // Hazmat, Mech, Motor Assist, Reflective vests.
                (1673, PlateauMissionDataV1.Revision, 75000, 3800, new (uint, uint)[] { (20951, 1), (33193, 1), (29247, 1), (36188, 1) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                var rows = harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision)
                    .OrderBy(entry => entry.ItemId).ToArray();
                CollectionAssert.AreEqual(items, rows.Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
                Assert.IsTrue(rows.All(entry => entry.Kind == MissionRewardItemKind.Selectable), $"{mission}: all to choose");
            }
        }

        /// <summary>
        /// The indicators: all on the Marshes map, keyed above UnnamedFrom but 539's fern marker, keyed by the client's own
        /// name 5 "Calla Ferns" (missionobjectiveindicatorlanguage). It Lies in Ruins' marker names the Marshes map too.
        /// </summary>
        [TestMethod]
        public void IndicatorsAreOnTheMarshesAndOnlyTheFernsCarryAClientName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var ruins = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == ItLiesInRuins).ToArray();
            Assert.HasCount(1, ruins);
            // Each marker's map as the server finds it (MissionMarkerProjection.ResolveMap: the scene's IndicatorMaps, its map,
            // the areas or the pools); the row has no map.
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Rasa.Structures.Mission>(), null);
            catalog.Load();
            foreach (var row in indicators.Concat(ruins))
            {
                Assert.AreEqual(1454U, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId, new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on the Marshes");
                if (row.MissionId == 539 && row.ObjectiveId == 1)
                    Assert.AreEqual(5U, row.IndicatorId, "Calla Ferns");
                else
                    Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                        $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
        }

        /// <summary>Every visible Marshes objective has a marker; hidden objectives have none.</summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = MarshesMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var hidden = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .SelectMany(entry => System.Text.RegularExpressions.Regex.Match(entry.Bindings, "\"HiddenObjectiveIds\"\\s*:\\s*\\[([^\\]]*)\\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(id => (entry.MissionId, uint.Parse(id.Trim()))))
                .ToHashSet();
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(12, objectives, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
            Assert.IsTrue(world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.MissionId == ItLiesInRuins && entry.ObjectiveId == 1),
                "It Lies in Ruins' one objective too");
        }

        /// <summary>The markers stand at their objective's target: a creature's pool, an NPC, a beat, a body, a feeder.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision || entry.MissionId == ItLiesInRuins).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (541, 1, 125, -364, 15),                            // Cast'ka's pool 520032 (World DB)
                (498, 1, -470, 39.3, 30), (498, 1, -262.8, -532.4, 30), (498, 1, -319.2, -566.8, 40),   // Thrax pools 531293, 531297, 531283
                (539, 1, -120, 20, 45),                             // between the Grubber pools
                (539, 2, -140, 0, 20), (539, 2, -100, 40, 20),
                (539, 3, -158.8, 40.4, 20), (539, 3, -120, 90, 20),
                (539, 4, -200, -20, 20), (539, 4, -150, 150, 20),
                (917, 1, -387, 492.2, 85), (917, 2, -291, 25, 8), (917, 3, -38, 49, 8), (917, 4, -564, -53, 8),
                (959, 1, -250, 600, 20), (959, 1, -290, 620, 20),   // the steak pools 760310, 760311
                (1673, 1, -142.7, 618, 8)                           // Morrison's pool 510162 (World DB)
            };
            // 961's twelve: the map's Treeback Feeders (client map statics ArchForeanTreebackfeederV01).
            foreach (var (x, z) in new (double, double)[]
            {
                (-148.1, 751.5), (134.2, 297.7), (243.7, 427.2), (139.4, 668), (-1.6, 778.2), (-264.2, 663.2),
                (-422.2, 553.8), (-329.5, 448.9), (-230.3, 425.3), (-142.4, 461.8), (-78.9, 407.3), (-155.6, 321.5)
            })
                expected.Add((961, 1, x, z, 8));
            Assert.HasCount(Indicators + 1, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators + 1, rows);

            // Each of 539's pools inside its own marker; Alia's whole beat inside hers.
            foreach (var (_, _, x, _, z) in MarshesPaludosNpcsV1.GasPools.Where(pool => pool.Creature == MarshesPaludosNpcsV1.SwampGrubber))
                Assert.IsLessThan(MarshesWhatsThatGasV1.FernMarkerRadius - 10, Vector2.Distance(new((float)x, (float)z), new(-120, 20)), "a Grubber pool in the fern marker");
            foreach (var (x, _, z) in MarshesPaludosNpcsV1.AliaBeat)
                Assert.IsLessThan(MarshesPaludosNpcsV1.AliaBeatRadius, Vector2.Distance(new((float)x, (float)z), new(-387, 492.2f)), "the beat in her marker");
        }

        /// <summary>No scene object of the Marshes is of a class the client draws as its "Default" pyramid (mesh 776).</summary>
        [TestMethod]
        public void NoSceneObjectIsOfAClassTheClientDrawsAsDefault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var meshes = harness.World.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.MeshId);
            var objects = 0;
            foreach (var binding in harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().ToList()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision))
            {
                var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(binding.Bindings,
                    Rasa.Missions.Content.MissionContentCodec.Options);
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Object))
                {
                    objects++;
                    Assert.IsTrue(meshes.TryGetValue(actor.TemplateId, out var mesh), $"{binding.MissionId} {actor.Role}: class {actor.TemplateId}");
                    Assert.AreNotEqual(776U, mesh, $"{binding.MissionId} {actor.Role}: class {actor.TemplateId} has the Default mesh");
                }
            }
            Assert.AreEqual(14, objects, "Helka's corpse and pack, the twelve feed acceptors");
        }

        /// <summary>
        /// Marshes_paludos takes out its four missions, the three Foreans, Alia's squad and the six pools; the upstream
        /// Paludos NPCs, Cast'ka and the Pools missions stay. Rolled back below it, Marshes_falcon_hold's rows go first.
        /// </summary>
        [TestMethod]
        public void PaludosRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_spawn WHERE id = 760300") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_type WHERE id = 760300") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_path WHERE id = 760300"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_path_point WHERE path_id = 760300"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'marshes_1_16_5'"));
            int Upstream() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510160, 510171, 510172, 520032)");
            var setOne = new uint[] { 541, 498, 539, 917 };

            Assert.IsTrue(MissionIds.All(Has));
            // Set 2 adds Horsh (his pool and six appearance slots), two pools and two scenes (959, 961).
            Assert.AreEqual((4, 11, 4, 9, 3, 4, 5), All(), "541 has no scene");
            Assert.AreEqual(4, Upstream());

            world.GetService<IMigrator>().Migrate(Previous(world, Paludos));
            Assert.IsFalse(MissionIds.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.AreEqual(4, Upstream(), "the upstream NPCs and Cast'ka stay");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == PoolsBuildingMaterialsV1.MissionId &&
                entry.ContentRevision == PoolsMissionDataV1.Revision), "the Pools missions stay");

            world.GetService<IMigrator>().Migrate(Paludos);
            Assert.IsTrue(setOne.All(Has));
            Assert.AreEqual((3, 8, 3, 3, 3, 4, 3), All());
        }

        /// <summary>
        /// Marshes_falcon_hold takes out It Lies in Ruins, Tastes Like Grubber, Fixing the Food Chain, Horsh and the two
        /// steak pools; set 1, Retreads to Retreads and the upstream NPCs it copied or used (Vetter, Morrison, Vincent) stay.
        /// Up again, all are back.
        /// </summary>
        [TestMethod]
        public void FalconHoldRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int) SetTwo() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 760309"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760309 AND 760311"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 760309") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 760309"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (959, 961, 1673)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (959, 961, 1673)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510162, 510165, 510168)");
            var setTwo = new uint[] { 959, 961, ItLiesInRuins };
            var setOne = new uint[] { 541, 498, 539, 917 };

            Assert.IsTrue(setTwo.All(Has));
            Assert.AreEqual((1, 3, 7, 15, 9), SetTwo());
            Assert.AreEqual(3, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, FalconHold));
            Assert.IsFalse(setTwo.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0), SetTwo());
            Assert.AreEqual(3, Kept(), "Vetter, Morrison and Vincent stay");
            Assert.IsTrue(setOne.All(Has), "set 1 stays");
            Assert.IsTrue(Has(1751), "Retreads to Retreads stays");

            world.GetService<IMigrator>().Migrate(FalconHold);
            Assert.IsTrue(setTwo.All(Has));
            Assert.AreEqual((1, 3, 7, 15, 9), SetTwo());
            Assert.AreEqual(MarshesMissionDataV1.CategoryId, world.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == ItLiesInRuins).CategoryId, "the Marshes category again");
        }
    }
}
