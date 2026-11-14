using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using V2 = AshenFixV2V1;
    using Queen = AshenSaveTheQueenV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen fix job V2 (AshenFixV2V1, migration 20271110002190, to-fix row 64, second try): 1501's objective 4 use moves from the
    /// hanging Life Support System (26423, its DAMAGE1 8.8-19.8 m up, past the client's 6 m) to a Bane console (7468, name 153) on
    /// the floor, on the device's scene role; the device stays as a visual role; marker 4 on the console. The playthrough
    /// (AshenConscriptionQueenTests.SaveTheQueenEndsHerSufferingAndTakesTheEggToJawan) checks it in play, also after a reload of a
    /// run from before the fix; the floor and walk checks there hold the console and its stand. [TestCategory]: none, as
    /// PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixV2Tests
    {
        private const string FixV2 = "20271110002190_Ashen_fix_v2";
        private const string Now = "8.500,78.110,160.500 r6 | evidence 1";
        private const string Before = "6.500,78.110,162.500 r6 | evidence 0";

        private static string Scene(DbContext db) => db.Set<MissionSceneBindingEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == V2.Queen && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;

        private static string Mine(DbContext db)
        {
            var marker = db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == V2.Queen &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == V2.Objective && entry.IndicatorId == V2.Marker);
            return FormattableString.Invariant($"{marker.PosX:0.000},{marker.PosY:0.000},{marker.PosZ:0.000} r{marker.Radius:0.###} | ") +
                   $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == V2.Source)}";
        }

        private static Vector3 V(ScenePosition at) => new(at.X, at.Y, at.Z);

        /// <summary>
        /// Fix V2's scene: the device role is the console (7468 at its place, ready 55, the role's use, name and "not usable"
        /// kept); the visual role is the device (26423 at its place, 10000002, never usable, name 153), made in sequence 0 and
        /// again when 4 is done; 3 done only switches the console on; 4 done sets the console (56), switches it off, takes the
        /// queen's talk object and the device to 10000003. The rest of fix V's scene kept; marker 4 on the console.
        /// </summary>
        [TestMethod]
        public void TheConsoleTakesTheDevicesUse()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(V2.QueenNow(), Scene(harness.World), "1501's scene: fix V2's");
            Assert.AreEqual(Now, Mine(harness.World), "marker 4 on the console, fix V2's evidence");
            Assert.AreEqual((7468u, 55u, 56u), (V2.ConsoleClass, V2.ConsoleReady, V2.ConsoleSet), "UsableTwoStateBaneConsoleSwitchV01, as 1499's and 1979's");

            var scene = V2.QueenScene();
            var before = V2.QueenSceneBefore();
            Assert.AreEqual(AshenFixVV1.QueenNow(), V2.QueenBefore(), "on fix V's scene");
            var role = Queen.DeviceRole;
            Assert.AreEqual(before.Actors[role] with
            {
                TemplateId = V2.ConsoleClass, Position = V2.ConsoleAt, Orientation = V2.ConsoleOrientation, InitialObjectState = V2.ConsoleReady
            }, scene.Actors[role], "the device role: the console, otherwise as fix V's");
            var console = scene.Actors[role];
            Assert.AreEqual((false, Queen.DeviceName, Queen.MissionId, Queen.EndTheSuffering, Queen.UseArgument),
                (console.InitiallyInteractable, console.NameOverrideId, console.UseAction.MissionId, console.UseAction.ObjectiveId, console.UseAction.ActionArgId),
                "the console: not usable at first, name 153, objective 4's use, argument 1");
            var visual = scene.Actors[V2.VisualRole];
            Assert.AreEqual((Queen.DeviceClass, V(Queen.DeviceAt), false, AshenFixVV1.DeviceActive, (SceneObjectAction)null),
                (visual.TemplateId, V(visual.Position), visual.InitiallyInteractable, visual.InitialObjectState, visual.UseAction), "the device: a visual, lowered");

            CollectionAssert.AreEqual(before.Sequences[0].World.Append(new EnsureActorIntent(V2.VisualCreateKey, V2.VisualRole)).ToList(),
                scene.Sequences[0].World, "sequence 0: the device made after the rest");
            CollectionAssert.AreEqual(new WorldIntent[] { new SetInteractionIntent("life-support-usable", role, true) },
                scene.Sequences[Queen.DeviceReady].World, "3 done: the console switched on only (fix V's 10000002 step out)");
            CollectionAssert.AreEqual(new WorldIntent[]
            {
                new TransitionObjectStateIntent("life-support-off", role, V2.ConsoleSet),
                new SetInteractionIntent("life-support-done", role, false),
                new RemoveActorIntent("the-queen-at-rest", Queen.QueenRole),
                new EnsureActorIntent(V2.VisualAtRestKey, V2.VisualRole),
                new TransitionObjectStateIntent(V2.VisualEndKey, V2.VisualRole, AshenFixVV1.DeviceEnd)
            }, scene.Sequences[Queen.QueenAtRest].World, "4 done");

            foreach (var id in before.Sequences.Keys.Where(id => id != 0 && id != Queen.DeviceReady && id != Queen.QueenAtRest))
                Assert.AreEqual(Json(before.Sequences[id]), Json(scene.Sequences[id]), $"sequence {id} kept");
            foreach (var other in before.Actors.Keys.Where(other => other != role))
                Assert.AreEqual(before.Actors[other], scene.Actors[other], $"{other} kept");
            CollectionAssert.AreEquivalent(before.Actors.Keys.Append(V2.VisualRole).ToList(), scene.Actors.Keys.ToList(), "one actor more");
            CollectionAssert.AreEquivalent(before.Sequences.Keys.ToList(), scene.Sequences.Keys.ToList(), "the same sequences");
        }

        private static string Json(Rasa.Missions.Content.SceneSequenceDefinition sequence) =>
            System.Text.Json.JsonSerializer.Serialize(sequence, Rasa.Missions.Content.MissionContentCodec.Options);

        /// <summary>
        /// The console's use point is within the client's reach (3D distance to DAMAGE1 against 6 m, useobject.pyo _InUseRadiusOf).
        /// 7468's model prop_bane_console_switch_v01.geo (mesh04.glm) holds its HPS_Damage1 connection point (a CPDF chunk: type,
        /// bone, rotation, translation, then "cpName" and the name) on no bone (-1), at (0, 0, -0.05): at the console's base, so no
        /// pose moves it. It is under ConsoleUseHeight and within 6 m (3D) of a player's feet at the walk's stand (the walk ends
        /// there, AshenConscriptionQueenTests). For contrast 26423's (creature_atta_queen_control_device_v01.geo) is on bone 58, so
        /// its pose (19.8 m / 8.8 m up in the real client) moves it. The console stands 2-3 m in front of the hanging device, toward
        /// the queen, and off her talk object.
        /// </summary>
        [TestMethod]
        public void TheConsolesUsePointIsWithinTheClientsReach()
        {
            var (bone, damage, boxTop) = Damage1(F.ClientData(), "prop_bane_console_switch_v01.geo");
            var (deviceBone, _, _) = Damage1(F.ClientData(), "creature_atta_queen_control_device_v01.geo");
            var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)V2.ConsoleOrientation);
            var point = Vector3.Transform(damage, turn) + V(V2.ConsoleAt);
            var reach = Vector3.Distance(point, V(V2.ConsoleStand));
            Console.WriteLine(FormattableString.Invariant(
                $"7468 HPS_Damage1 bone {bone} at {damage} (model box top {boxTop:0.00}): {point.Y - V2.ConsoleAt.Y:0.00} m up, {reach:0.00} m 3D from the stand; 26423's on bone {deviceBone}"));
            Assert.AreEqual(-1, bone, "the console's use point on no bone: no pose moves it");
            Assert.AreEqual(58, deviceBone, "the device's use point on a bone (its pose moves it)");
            Assert.IsLessThanOrEqualTo(V2.ConsoleUseHeight, point.Y - V2.ConsoleAt.Y, "the console's use point is low");
            Assert.IsLessThanOrEqualTo(V2.ConsoleUseHeight, boxTop, "the console's model is low");
            Assert.IsLessThanOrEqualTo(6f, reach, "the use point within 6 m of the stand");
            var fromDevice = Vector2.Distance(new(V2.ConsoleAt.X, V2.ConsoleAt.Z), new(Queen.DeviceAt.X, Queen.DeviceAt.Z));
            Assert.IsTrue(fromDevice is >= 2f and <= 3f, $"2-3 m in front of the device ({fromDevice:0.00})");
            Assert.IsLessThan(Queen.DeviceAt.Z, V2.ConsoleAt.Z, "toward the queen (south)");
            Assert.IsGreaterThan(2f, Vector2.Distance(new(V2.ConsoleAt.X, V2.ConsoleAt.Z), new(Queen.QueenAt.X, Queen.QueenAt.Z)), "off the queen's talk object");
            Assert.AreEqual(V2.ConsoleAt.Y, Queen.DeviceAt.Y, "on the device's floor");
        }

        /// <summary>GeoMesh.Scan (internal there): chunks of a tag (stored reversed) in [start, end) as (version, payload range).</summary>
        private static System.Collections.Generic.IEnumerable<(uint version, int start, int end)> Scan(byte[] data, int start, int end, string tag)
        {
            var pattern = new byte[4];
            for (var i = 0; i < 4; i++)
                pattern[i] = (byte)tag[3 - i];
            int Find(int from)
            {
                for (var i = from; i + 4 <= end; i++)
                    if (data[i] == pattern[0] && data[i + 1] == pattern[1] && data[i + 2] == pattern[2] && data[i + 3] == pattern[3])
                        return i;
                return -1;
            }
            var o = Find(start);
            while (o >= 0 && o + 16 <= end)
            {
                var size = BitConverter.ToUInt32(data, o + 4);
                var version = BitConverter.ToUInt32(data, o + 8);
                if (BitConverter.ToUInt32(data, o + 12) == 0 && size < int.MaxValue && o + 16 + (long)size <= end)
                {
                    yield return (version, o + 16, o + 16 + (int)size);
                    o = Find(o + 16 + (int)size);
                }
                else
                    o = Find(o + 1);
            }
        }

        /// <summary>
        /// A client model's HPS_Damage1 connection point: its bone (-1: none) and its translation (in the object's frame when on no
        /// bone), and the top of the model's BBOX.
        /// </summary>
        internal static (int Bone, Vector3 At, float BoxTop) Damage1(string dataDir, string meshName)
        {
            byte[] data = null;
            foreach (var path in Directory.EnumerateFiles(dataDir, "mesh*.glm"))
            {
                var archive = new GlmArchive(path);
                if (archive.Entries.ContainsKey(meshName))
                {
                    data = archive.Read(meshName);
                    break;
                }
            }
            Assert.IsNotNull(data, $"{meshName} in the client's mesh archives");
            var body = Scan(data, 8, data.Length, "GBOD").First();
            var box = Scan(data, body.start, body.end, "BBOX").First();
            var boxTop = BitConverter.ToSingle(data, box.start + 1 + 16);
            var name = System.Text.Encoding.ASCII.GetBytes("cpName\0HPS_Damage1\0");
            foreach (var (_, start, end) in Scan(data, 8, data.Length, "CPDF"))
                if (end - start >= 40 + name.Length && data.AsSpan(start + 40, name.Length).SequenceEqual(name))
                    return (BitConverter.ToInt32(data, start + 4),
                        new Vector3(BitConverter.ToSingle(data, start + 24), BitConverter.ToSingle(data, start + 28), BitConverter.ToSingle(data, start + 32)), boxTop);
            Assert.Fail($"{meshName}: no HPS_Damage1 connection point");
            return default;
        }

        /// <summary>
        /// Fix V2's migration rolled back and forth, baselined at its own migration: Down gives back fix V's scene and marker 4 on
        /// the device, and no fix V2 evidence, as a database migrated only to fix V holds them; nothing else changes; a second Down
        /// and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixV2RollsBackToFixV()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixV2);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.MissionId != V2.Queen)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => !(entry.MissionId == V2.Queen && entry.IndicatorId == V2.Marker))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != V2.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix V2");
            Assert.AreEqual(V2.QueenNow(), Scene(world), "fix V2's scene");

            var before = AshenMissionTests.Previous(world, FixV2);
            Assert.AreEqual("20271110002180_Ashen_fix_v", before, "after fix V");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
            {
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix V");
                Assert.AreEqual(Scene(fresh.World), Scene(world), "Down: the scene as at fix V");
            }
            Assert.AreEqual(Before, Mine(world), "marker 4 on the device");
            Assert.AreEqual(V2.QueenBefore(), Scene(world), "fix V's scene");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in V2.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");
            Assert.AreEqual(V2.QueenBefore(), Scene(world), "a second Down: the scene");

            world.GetService<IMigrator>().Migrate(FixV2);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in V2.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(V2.QueenNow(), Scene(world), "Up: fix V2's scene");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
