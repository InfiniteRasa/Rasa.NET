using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Crucible
{
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Crucible;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Ligo Crucible missions (CrucibleMissionDataV1, revision crucible_1_16_5): set 1 at Outpost Intrepid, Corporal
    /// Shandy's The Last Men Standing (1142), Captain DeLisi's A Fort Called Intrepid (1150) and The Wire (1145), and Private
    /// Drake's The Departed (1143), with Shandy, Recon Sergeant Mullins and Top Khana Grupa Munit, and the upstream rows set 1
    /// uses moved and given the client's packages (Crucible_outpost_intrepid).
    ///
    /// Why these tests: the client's text ids, packages and item classes are written out here from the client tables, not
    /// taken from the data classes, so a wrong id fails; the NPC rows, rewards, prerequisites and markers are pinned; the
    /// migration rolls back to the one directly before it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleMissionTests
    {
        /// <summary>The Crucible missions the World migrations enable on the Crucible revision, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            CrucibleLastMenStandingV1.MissionId, CrucibleDepartedV1.MissionId, CrucibleFortCalledIntrepidV1.MissionId,
            CrucibleWireV1.MissionId
        };

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): The Last Men Standing's one at DeLisi; The
        /// Departed's one round the fallen (hidden 22 has none); A Fort Called Intrepid's one at the terminal; The Wire's five,
        /// one at each NPC.
        /// </summary>
        internal const int Indicators = 8;

        private const string OutpostIntrepid = "20271108000000_Crucible_outpost_intrepid";

        private const uint Shandy = 770000, Mullins = 770001, Munit = 770002;
        private const uint DeLisi = 510019, Drake = 510034, Vitto = 510027, Donal = 510037, Rigs = 510025, DAmbrose = 510023;

        /// <summary>
        /// The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), the target that
        /// rolls <paramref name="migration"/> back: not a fixed id, so another migration slotted in between cannot be rolled
        /// back with it unseen.
        /// </summary>
        internal static string Previous(DbContext world, string migration)
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
                (1142, Shandy, DeLisi, 40), (1143, Drake, Drake, 40), (1150, DeLisi, DeLisi, 40), (1145, DeLisi, DeLisi, 40)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(1U, entry.CategoryId, "Battlefield (Crucible), missioncategorylanguage 1");
            }
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The new NPCs: the client's names (creaturenamelanguage 9060, 9328, 9062) and packages (objectiveconversation 1145,6
        /// 1454 and 1145,3 1229; Shandy's server-only), friendly, standing still, with their own hit points, on the Crucible;
        /// the upstream rows set 1 uses at their new places, Rigs with the client's 1231, D'Ambrose with 1301 (not Munit's).
        /// </summary>
        [TestMethod]
        public void TheNpcsHaveTheClientsNamesAndPackages()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            foreach (var (npc, name, package, cls, level) in new (uint, uint, uint, uint, uint)[]
            {
                (Shandy, 9060, 770000, 3846, 39), (Mullins, 9328, 1454, 3848, 40), (Munit, 9062, 1229, 7776, 40)
            })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, level, 1U, cls, 0U, 0U), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    creature.RunSpeed, creature.WalkSpeed), $"{npc}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((4500, 300), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.IsTrue(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc} has a look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((1993U, npc, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"{npc} pool");
            }

            // The places (TaRapedia's for Shandy, DeLisi, Drake and Rigs; the quarters' region points for the Grupas).
            foreach (var (pool, x, z) in new (uint, double, double)[]
            {
                (Shandy, 987.07, 59.65), (Mullins, 292.49, 736.36), (Munit, -215, 205), (DeLisi, 952, 106), (Drake, 952.46, 110.89),
                (Vitto, -300, 320), (Donal, -216, 472), (Rigs, -392.77, 276.23)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            var packages = world.NpcPackageEntries.AsNoTracking().Where(row => row.Id == Rigs || row.Id == DAmbrose || row.Id == DeLisi ||
                row.Id == Vitto || row.Id == Donal).AsEnumerable().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual((1231U, 1301U, 1219U, 1225U, 1227U), (packages[Rigs], packages[DAmbrose], packages[DeLisi], packages[Vitto], packages[Donal]));
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1229), "only Munit has 1229");
        }

        /// <summary>
        /// The client's own texts: the ids are written out here from the client tables (missionconversation,
        /// missionobjective), not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (1142, 8344, new (uint, uint, uint, uint?)[] { (1, 8533, 8534, null) }),
                (1143, 8353, new (uint, uint, uint, uint?)[] { (1, 8358, 8359, null), (22, 20331, 20332, 20333) }),
                (1150, 8414, new (uint, uint, uint, uint?)[] { (1, 8574, 8575, null) }),
                (1145, 8386, new (uint, uint, uint, uint?)[] { (6, 9997, 9998, null), (1, 8647, 8648, null), (2, 8650, 8651, null),
                    (3, 8653, 8654, null), (5, 8662, 8663, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
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

            // The talks at the client's packages (objectiveconversation 1142,1 1219; 1150,1 1245; 1145,1 1225; 1145,2 1227;
            // 1145,3 1229; 1145,5 1231; 1145,6 1454), each with the client's variant 1.
            var talks = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.Kind == MissionTriggerKind.Conversation)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.ObjectiveId, entry.NpcPackageId, entry.PlayerFlagId))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint?, uint?)[]
            {
                (1142, 1, 1219, 1), (1145, 1, 1225, 1), (1145, 2, 1227, 1), (1145, 3, 1229, 1), (1145, 5, 1231, 1), (1145, 6, 1454, 1),
                (1150, 1, 1245, 1)
            }, talks);
        }

        /// <summary>The chain 1142 -> 1150 -> 1145 (TaRapedia); The Departed needs none; none repeats.</summary>
        [TestMethod]
        public void OnlyTheFollowUpsRequireAMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.Kind, entry.RequiredMissionId)).OrderBy(entry => entry.MissionId).ToArray();
            CollectionAssert.AreEqual(new (uint, MissionPrerequisiteKind, uint?)[]
            {
                (1145, MissionPrerequisiteKind.MissionCompleted, 1150), (1150, MissionPrerequisiteKind.MissionCompleted, 1142)
            }, rows);
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            foreach (var mission in MissionIds)
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
            foreach (var (mission, experience, credits, items) in new (uint, uint, uint, (uint, uint)[])[]
            {
                (1142, 37500, 3800, new[] { (111232U, 2U), (118907U, 2U), (111038U, 4U), (111028U, 4U) }),
                (1143, 75000, 5700, new[] { (19802U, 1U), (34116U, 1U), (36488U, 1U), (24474U, 1U) }),
                (1150, 75000, 5700, new[] { (39985U, 1U), (25119U, 1U), (21100U, 1U), (31547U, 1U) }),
                (1145, 75000, 5700, new[] { (33729U, 1U), (20802U, 1U), (23569U, 1U), (30167U, 1U) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                CollectionAssert.AreEqual(items, harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                    .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
            }
        }

        /// <summary>The indicators: all on the Crucible map (MissionMarkerProjection), keyed above UnnamedFrom (the client names none of them).</summary>
        [TestMethod]
        public void IndicatorsAreOnTheCrucibleAndAboveUnnamed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var row in indicators)
            {
                Assert.AreEqual(1993U, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId,
                    new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on the Crucible");
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom, $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
        }

        /// <summary>Every visible Crucible objective has a marker; hidden objectives have none.</summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = CrucibleMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var hidden = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .SelectMany(entry => System.Text.RegularExpressions.Regex.Match(entry.Bindings, "\"HiddenObjectiveIds\"\\s*:\\s*\\[([^\\]]*)\\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(id => (entry.MissionId, uint.Parse(id.Trim()))))
                .ToHashSet();
            CollectionAssert.AreEquivalent(new[] { (1143U, 22U) }, hidden.ToArray(), "The Departed's item tracker is hidden");
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(9, objectives, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
        }

        /// <summary>The markers stand at their objective's target: an NPC, the terminal, the field of the fallen.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (1142, 1, 952, 106, 8),                             // DeLisi
                (1143, 1, 802.3, 570, 70),                          // round the twenty fallen
                (1150, 1, 809.74, 676.74, 10),                      // the terminal
                (1145, 6, 292.49, 736.36, 8), (1145, 1, -300, 320, 8), (1145, 2, -216, 472, 8), (1145, 3, -215, 205, 8),
                (1145, 5, -392.77, 276.23, 8)                       // Mullins, Vitto, Donal, Munit, Rigs
            };
            Assert.HasCount(Indicators, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 0.1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators, rows);

            // The fallen inside their marker, 10 m in from its edge.
            foreach (var at in CrucibleDepartedV1.CorpsesAt)
                Assert.IsLessThan(CrucibleDepartedV1.FieldRadius - 10, Vector2.Distance(new(at.X, at.Z), new(802.3f, 570f)), $"a corpse at ({at.X}, {at.Z}) in the marker");
        }

        /// <summary>No scene object of set 1 is of a class the client draws as its "Default" pyramid (mesh 776).</summary>
        [TestMethod]
        public void NoSceneObjectIsOfAClassTheClientDrawsAsDefault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var meshes = harness.World.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.MeshId);
            var objects = 0;
            foreach (var scene in Scenes(harness))
                foreach (var actor in scene.Scene.Actors.Values.Where(actor => actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Object))
                {
                    objects++;
                    Assert.IsTrue(meshes.TryGetValue(actor.TemplateId, out var mesh), $"{scene.MissionId} {actor.Role}: class {actor.TemplateId}");
                    Assert.AreNotEqual(776U, mesh, $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} has the Default mesh");
                }
            Assert.AreEqual(20 + 1, objects, "The Departed's twenty fallen and the terminal");
        }

        /// <summary>
        /// The use argument the client sends for each usable class of set 1 (usabledata lookup[class][0], or 1 if it is
        /// None: Usable.__init__), written out from the client table: the human soldier corpses' entries are None.
        /// </summary>
        internal static readonly Dictionary<uint, uint> ClientUseArguments = new()
        {
            [24995] = 1, [7865] = 1, [24989] = 1, [24990] = 1,     // UsableTreasureDispHumCorpseMaleV01-V04 (1143; lookup[0] None, the client sends 1)
        };

        [TestMethod]
        public void EveryUsableSceneObjectTakesTheClientsUseArgument()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var used = 0;
            foreach (var scene in Scenes(harness))
                foreach (var actor in scene.Scene.Actors.Values.Where(actor => actor.UseAction != null))
                {
                    used++;
                    Assert.IsTrue(ClientUseArguments.TryGetValue(actor.TemplateId, out var argument),
                        $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} has no pinned client use argument");
                    Assert.AreEqual(argument, actor.UseAction.ActionArgId,
                        $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} is used with the client's argument");
                    Assert.AreEqual(12U, actor.NameOverrideId, $"{actor.Role}: the client's name 12 'Fort Intrepid Casualty'");
                }
            Assert.AreEqual(20, used, "The Departed's twenty fallen (1150's terminal talks)");
        }

        /// <summary>
        /// Crucible_outpost_intrepid takes out its four missions and three NPCs and puts the upstream rows back (places and
        /// packages); the Marshes missions stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void OutpostIntrepidRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 510025"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'crucible_1_16_5'"));
            uint Package(uint npc) => world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId;
            (double, double, double) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (row.PosX, row.PosY, row.PosZ);
            }

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual((3, 3, 3, 3, 1, 4), All());
            Assert.AreEqual(1301U, Package(DAmbrose));
            Assert.AreEqual((952.0, 144.5, 106.0), At(DeLisi));

            world.GetService<IMigrator>().Migrate(Previous(world, OutpostIntrepid));
            world.ChangeTracker.Clear();
            Assert.IsFalse(MissionIds.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.AreEqual(1229U, Package(DAmbrose), "D'Ambrose has upstream's package again");
            foreach (var (pool, before, _) in CrucibleOutpostIntrepidNpcsV1.Moves)
                Assert.AreEqual(before, At(pool), $"pool {pool} back at its upstream place");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == MarshesWelcomeWagonV1.MissionId &&
                entry.ContentRevision == MarshesMissionDataV1.Revision), "the Marshes missions stay");

            world.GetService<IMigrator>().Migrate(OutpostIntrepid);
            world.ChangeTracker.Clear();
            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual((3, 3, 3, 3, 1, 4), All());
            Assert.AreEqual(1301U, Package(DAmbrose));
        }

        internal static List<(uint MissionId, Rasa.Missions.Content.MissionSceneDefinition Scene)> Scenes(WildernessRuntimeTestHarness harness) =>
            harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision)
                .ToList().Select(entry => (entry.MissionId, System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                    entry.Bindings, Rasa.Missions.Content.MissionContentCodec.Options))).ToList();
    }
}
