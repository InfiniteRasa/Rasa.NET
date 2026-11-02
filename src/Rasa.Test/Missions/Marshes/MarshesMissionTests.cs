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
    using Rasa.Services.Preloader.Missions.Pools;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Valverde Marshes missions (MarshesMissionDataV1, revision marshes_1_16_5): set 1 at Paludos, Elder Toida's Roll
    /// out the Welcome Wagon (541) and Divided We Fall (917), Sergeant Starling's Cannister Collection (498) and Shaman
    /// Geli's What's that Gas? (539), with Ranger Alia, Shaman Siva, Warrior Veska and the mission pools east of Paludos
    /// (Marshes_paludos).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesMissionTests
    {
        /// <summary>The Marshes missions the World migrations enable, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            MarshesWelcomeWagonV1.MissionId, MarshesCannisterCollectionV1.MissionId, MarshesWhatsThatGasV1.MissionId,
            MarshesDividedWeFallV1.MissionId
        };

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): Roll out the Welcome Wagon's one, on
        /// Cast'ka; Cannister Collection's three, on the western Thrax pools; What's that Gas?'s seven (the "Calla Ferns" one
        /// round the Grubber pools, and one on each of the six mission pools for 2-4); Divided We Fall's four (Alia's beat,
        /// Siva, Veska, Helka).
        /// </summary>
        internal const int Indicators = 15;

        private const string Paludos = "20271104000000_Marshes_paludos";

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
                (541, 510160, 510160, 36), (498, 510171, 510171, 35), (539, 510172, 510172, 34), (917, 510160, 510160, 35)
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
            foreach (var npc in new uint[] { 760301, 760302 })
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

            // 539's six pools of three: two each of Swamp Grubber, Maw and Mox, on the Marshes.
            foreach (var (id, creature) in new (uint, uint)[]
            {
                (760303, 540014), (760304, 540014), (760305, 531052), (760306, 531052), (760307, 540013), (760308, 540013)
            })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                Assert.AreEqual((1454U, creature, (byte)3, (byte)3), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount), $"pool {id}");
            }

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
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
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (541, 1454, new (uint, uint, uint, uint?)[] { (1, 1496, 7423, 1497) }),
                (498, 1238, new (uint, uint, uint, uint?)[] { (1, 1433, 8304, null) }),
                (539, 1446, new (uint, uint, uint, uint?)[] { (1, 1542, 9361, null), (2, 1543, 9362, null), (3, 1544, 9363, null),
                    (4, 1545, 9364, null) }),
                (917, 4899, new (uint, uint, uint, uint?)[] { (1, 4958, 7105, null), (2, 4960, 7106, null), (3, 4962, 7107, null),
                    (4, 4964, 7108, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
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

            // 917's three talks at the client's packages (objectiveconversation 917,1 985; 917,2 986; 917,3 984).
            var talks = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.MissionId == 917 && entry.ContentRevision == MarshesMissionDataV1.Revision &&
                    entry.Kind == MissionTriggerKind.Conversation).AsEnumerable()
                .Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 985), (2, 986), (3, 984) }, talks);
        }

        /// <summary>None of set 1 has a requirement (TaRapedia, TR MMO DB), and none repeats.</summary>
        [TestMethod]
        public void NoMissionOfSetOneRequiresAnother()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.IsFalse(harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            foreach (var mission in MissionIds)
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    },
                    evidence.Where(entry => entry.MissionId == mission).OrderBy(entry => entry.EvidenceId)
                        .Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(), $"{mission} evidence");
        }

        /// <summary>The rewards: XP and credits of TaRapedia, the four items to choose one of (written out here).</summary>
        [TestMethod]
        public void TheRewardsAreTaRapediasWithOneItemToChoose()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, experience, credits, items) in new (uint, uint, uint, uint[])[]
            {
                (541, 75000, 5700, new uint[] { 45762, 45812, 48680, 14937 }),
                (498, 65000, 5400, new uint[] { 48108, 15150, 14738, 49136 }),
                (539, 65000, 5400, new uint[] { 47944, 47728, 15513, 14404 }),
                (917, 65000, 5400, new uint[] { 20335, 28614, 35571, 39191 })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                CollectionAssert.AreEqual(items, harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision)
                    .OrderBy(entry => entry.ItemId).Select(entry => entry.ItemTemplateId).ToArray(), $"{mission} items");
            }
        }

        /// <summary>
        /// The indicators: all on the Marshes map, keyed above UnnamedFrom but 539's fern marker, keyed by the client's own
        /// name 5 "Calla Ferns" (missionobjectiveindicatorlanguage).
        /// </summary>
        [TestMethod]
        public void IndicatorsAreOnTheMarshesAndOnlyTheFernsCarryAClientName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Rasa.Structures.Mission>(), null);
            catalog.Load();
            foreach (var row in indicators)
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
            Assert.HasCount(10, objectives, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
        }

        /// <summary>The markers stand at their objective's target: a creature's pool, an NPC, a beat, a body.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (541, 1, 125, -364, 15),                            // Cast'ka's pool 520032 (World DB)
                (498, 1, -470, 39.3, 30), (498, 1, -262.8, -532.4, 30), (498, 1, -319.2, -566.8, 40),   // Thrax pools 531293, 531297, 531283
                (539, 1, -120, 20, 45),                             // between the Grubber pools
                (539, 2, -140, 0, 20), (539, 2, -100, 40, 20),
                (539, 3, -158.8, 40.4, 20), (539, 3, -120, 90, 20),
                (539, 4, -200, -20, 20), (539, 4, -150, 150, 20),
                (917, 1, -387, 492.2, 85), (917, 2, -291, 25, 8), (917, 3, -38, 49, 8), (917, 4, -564, -53, 8)
            };
            Assert.HasCount(Indicators, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators, rows);

            // Each of 539's pools inside its own marker; Alia's whole beat inside hers.
            foreach (var (_, _, x, _, z) in MarshesPaludosNpcsV1.GasPools.Where(pool => pool.Creature == MarshesPaludosNpcsV1.SwampGrubber))
                Assert.IsLessThan(MarshesWhatsThatGasV1.FernMarkerRadius - 10, Vector2.Distance(new((float)x, (float)z), new(-120, 20)), "a Grubber pool in the fern marker");
            foreach (var (x, _, z) in MarshesPaludosNpcsV1.AliaBeat)
                Assert.IsLessThan(MarshesPaludosNpcsV1.AliaBeatRadius, Vector2.Distance(new((float)x, (float)z), new(-387, 492.2f)), "the beat in her marker");
        }

        /// <summary>No scene object of set 1 is of a class the client draws as its "Default" pyramid (mesh 776).</summary>
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
            Assert.AreEqual(2, objects, "Helka's corpse and pack");
        }

        /// <summary>
        /// Marshes_paludos takes out its four missions, the three Foreans, Alia's squad and the six pools; the upstream
        /// Paludos NPCs, Cast'ka and the Pools missions stay.
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

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual((3, 8, 3, 3, 3, 4, 3), All(), "541 has no scene");
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
            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual((3, 8, 3, 3, 3, 4, 3), All());
        }
    }
}
