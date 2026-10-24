extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Structures.Char;

    /// <summary>
    /// The apartment tables (ApartmentCatalogue): the shipped plots are on retail maps with their
    /// entries on the maps' navmeshes, the catalogue's working items are the retail classes that
    /// work, caps scale down with the budget, and a bad row is said and left out.
    /// </summary>
    [TestClass]
    public class ApartmentCatalogueTests
    {
        /// <summary>The retail maps the apartment plots stand on, with their names (map_info, navmesh files).</summary>
        internal static readonly Dictionary<uint, string> RetailMaps = new Dictionary<uint, string>
        {
            [2259] = "adv_wargame_indoorarena",
            [1737] = "test_nexus2",
            [2278] = "adv_zepic_pve_arena",
            [2034] = "adv_arieki_torden_plains_penalresearch",
            [20000009] = "adv_afs_arena",
            [1465] = "adv_foreas_valverde_pools_test_weapons_center"
        };

        internal static ApartmentCatalogue Shipped() =>
            ApartmentCatalogue.Load(Path.Combine(AppContext.BaseDirectory, "apartments"));

        private static string RepositoryRoot()
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);

            while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln")))
                root = root.Parent;

            return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }

        [TestMethod]
        public void TheShippedTablesReadWithoutAProblem()
        {
            var catalogue = Shipped();

            Assert.AreEqual(0, catalogue.Problems.Count, string.Join("\n", catalogue.Problems));
            Assert.AreEqual(23, catalogue.Plots.Count);
            Assert.AreEqual(499, catalogue.Items.Count);
            Assert.AreEqual(11, catalogue.Categories.Count, "the Supply Kiosk, which sells nothing here, is Tech");
            CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 15, 16, 19, 20, 21 },
                catalogue.ApartmentPlots.Select(p => p.Id).ToArray(), "kinds 1 and 3, lowest first");
            Assert.AreEqual(1u, catalogue.ApartmentPlots.First().Id, "a new apartment stands on plot 1");
            Assert.IsTrue(catalogue.Plots.All(p => RetailMaps.ContainsKey(p.MapContextId)), "no custom map");
        }

        [TestMethod]
        public void EveryPlotIsOnARetailMapWithItsEntryOnTheMapsNavmeshInsideItsBuildBox()
        {
            var catalogue = Shipped();
            var root = RepositoryRoot();

            foreach (var plot in catalogue.Plots)
            {
                var nav = TestNavMeshes.Query(Path.Combine(root, "navmesh", RetailMaps[plot.MapContextId] + ".nav"));
                var nearest = nav.Nearest(plot.Entry);

                Assert.IsTrue(plot.Contains(plot.Entry), $"plot {plot}: entry inside the box");
                Assert.IsNotNull(nearest, $"plot {plot}: the navmesh is near the entry");
                Assert.IsTrue(Vector3.Distance(nearest.Value, plot.Entry) < 1.5f,
                    $"plot {plot}: entry {plot.Entry} is {Vector3.Distance(nearest.Value, plot.Entry):0.00} m from the navmesh ({nearest})");
                Assert.IsTrue(plot.BoxMax.X - plot.BoxMin.X >= 20 && plot.BoxMax.Z - plot.BoxMin.Z >= 20 && plot.BoxMax.Y - plot.BoxMin.Y >= 10,
                    $"plot {plot}: a box to build in");
                Assert.IsTrue(plot.Budget >= 150, $"plot {plot}: budget");
            }
        }

        [TestMethod]
        public void TheWorkingItemsAreTheRetailClassesThatWork()
        {
            var catalogue = Shipped();

            Assert.AreEqual(DecorFunction.Storage, catalogue.DecorOf(21030).Function, "UsableLockBoxHumFootlockerV01: a world footlocker's class");
            Assert.AreEqual(DecorFunction.Crafting, catalogue.DecorOf(9595).Function, "UsableCraftingHumStationV01: the kraftwerks' class");
            Assert.AreEqual(DecorFunction.None, catalogue.DecorOf(29686).Function, "PropHumFootlockerV02 is only a prop");
            Assert.AreEqual(DecorFunction.None, catalogue.DecorOf(29671).Function, "PropHumCraftstationV02 is only a prop");
            Assert.AreEqual(DecorFunction.Medbay, catalogue.DecorOf(29690).Function);
            Assert.IsNull(catalogue.DecorOf(10000063), "the clan lockbox is a clan's");
            Assert.AreEqual(1, catalogue.Items.Count(d => d.Function == DecorFunction.Storage));
            Assert.AreEqual(1, catalogue.Items.Count(d => d.Function == DecorFunction.Crafting));
            Assert.IsTrue(catalogue.Items.All(d => d.ScaleMin > 0 && d.ScaleMin <= d.ScaleMax));
        }

        [TestMethod]
        public void CapsAreThePlotsAtItsBudgetAndScaleDownWithASmallerOne()
        {
            var catalogue = Shipped();
            var small = catalogue.PlotOf(1);    // 150: Lighting 24, Medical 1
            var keep = catalogue.PlotOf(4);     // 500: Lighting 48, Medical 2

            Assert.AreEqual(24, catalogue.CapOf(small, "Lighting", 150));
            Assert.AreEqual(24, catalogue.CapOf(small, "lighting", 500), "never more than the plot's own");
            Assert.AreEqual(1, catalogue.CapOf(small, "Medical", 150));
            Assert.AreEqual(48, catalogue.CapOf(keep, "Lighting", 500));
            Assert.AreEqual(14, catalogue.CapOf(keep, "Lighting", 150), "48 x 150/500");
            Assert.AreEqual(1, catalogue.CapOf(keep, "Medical", 150), "never under one");
            Assert.IsNull(catalogue.CapOf(keep, "Furniture", 150), "not capped");
            Assert.AreEqual(150, ApartmentCatalogue.BudgetOf(keep, 150));
            Assert.AreEqual(150, ApartmentCatalogue.BudgetOf(small, 1000));
        }

        [TestMethod]
        public void ItemsAreFoundByIdByWholeNameAndByPartOfIt()
        {
            var catalogue = Shipped();

            Assert.AreEqual(8075u, catalogue.Find("8075").Single().ClassId);
            Assert.AreEqual(8075u, catalogue.Find("floor lamp").Single().ClassId, "the whole name, any case");
            Assert.IsTrue(catalogue.Find("lamp").Count > 5, "part of a name: every such item");
            Assert.AreEqual(0, catalogue.Find("1").Count, "an id the catalogue does not have");
            Assert.AreEqual(0, catalogue.Find("  ").Count);
        }

        [TestMethod]
        public void AnItemsBoxTurnsAndSizesWithIt()
        {
            var decor = new ApartmentCatalogue.Decor { ClassId = 1, Name = "Plank", Category = "Walls", ScaleMin = 1, ScaleMax = 2, HalfExtents = new Vector3(2, 0.5f, 0.25f), CentreY = 0.5f };

            var (min, max) = decor.BoundsAt(new Vector3(10, 0, 10), 0, 1);
            Assert.AreEqual(new Vector3(8, 0, 9.75f), min);
            Assert.AreEqual(new Vector3(12, 1, 10.25f), max);

            (min, max) = decor.BoundsAt(new Vector3(10, 0, 10), Math.PI / 2, 2);
            Assert.AreEqual(9.5f, min.X, 0.001f, "a quarter turn lays the long side along z");
            Assert.AreEqual(6f, min.Z, 0.001f);
            Assert.AreEqual(2f, max.Y, 0.001f, "the box centre is raised with the scale");
        }

        [TestMethod]
        public void ABadRowIsSaidAndLeftOut()
        {
            var catalogue = new ApartmentCatalogue();

            catalogue.Parse(
                "id\tname\tkind\tmap\tx\ty\tz\tX\tY\tZ\tex\tey\tez\teyaw\tbudget\n" +
                "1\tGood\t1\t2259\t-10\t0\t-10\t10\t10\t10\t0\t1\t0\t0\t150\n" +
                "2\tOff the map\t1\t9999\t-10\t0\t-10\t10\t10\t10\t0\t1\t0\t0\t150\n" +
                "3\tOutside\t1\t2259\t-10\t0\t-10\t10\t10\t10\t50\t1\t0\t0\t150\n" +
                "4\tNumbers\t1\t2259\tx\t0\t-10\t10\t10\t10\t0\t1\t0\t0\t150\n" +
                "1\tAgain\t1\t2259\t-10\t0\t-10\t10\t10\t10\t0\t1\t0\t0\t150\n" +
                "# a comment\n5\tshort\n",
                "plot_id\tcategory\tcap\n1\tLighting\t2\n1\tMedical\t-1\n",
                "class\tcat\tsurface\tfunction\tmin\tmax\tname\thx\thy\thz\toy\n" +
                "8075\tLighting\t1\t0\t0.5\t3\tFloor Lamp\t0.1\t0.4\t0.3\t0.4\n" +
                "9999\tLighting\t1\t0\t0.5\t3\tUnknown Class\t0.1\t0.4\t0.3\t0.4\n" +
                "8076\tLighting\t1\t0\t0\t3\tNo Scale\t0.1\t0.4\t0.3\t0.4\n",
                map => map == 2259,
                classId => classId != 9999);

            CollectionAssert.AreEqual(new uint[] { 1 }, catalogue.Plots.Select(p => p.Id).ToArray());
            CollectionAssert.AreEqual(new uint[] { 8075 }, catalogue.Items.Select(d => d.ClassId).ToArray());
            Assert.AreEqual(2, catalogue.CapOf(catalogue.PlotOf(1), "Lighting", 150));
            Assert.IsNull(catalogue.CapOf(catalogue.PlotOf(1), "Medical", 150));
            Assert.AreEqual(8, catalogue.Problems.Count, string.Join("\n", catalogue.Problems));
            Assert.IsTrue(catalogue.Problems.Any(p => p.Contains("map 9999")));
            Assert.IsTrue(catalogue.Problems.Any(p => p.Contains("entry is outside")));
            Assert.IsTrue(catalogue.Problems.Any(p => p.Contains("world database")));
        }

        [TestMethod]
        public void SendMeToMyApartmentIsReadWithOrWithoutArguments()
        {
            foreach (var arguments in new[] { 0, 2 })
            {
                using var stream = new MemoryStream();
                var writer = new PythonWriter(new BinaryWriter(stream));

                writer.WriteTuple(arguments);
                for (var i = 0; i < arguments; i++)
                    writer.WriteInt(7 + i);

                stream.Position = 0;
                new SendMeToMyApartmentPacket().Read(new PythonReader(new BinaryReader(stream)));

                Assert.AreEqual(stream.Length, stream.Position, $"{arguments} arguments: all read");
            }
        }

        [TestMethod]
        public void PasscodesNamesAndYawsFollowTheirRules()
        {
            Assert.AreEqual(Apartments.PasscodeHash(7, "Banana"), Apartments.PasscodeHash(7, "banana"), "any case");
            Assert.AreNotEqual(Apartments.PasscodeHash(7, "banana"), Apartments.PasscodeHash(8, "banana"), "seeded by the apartment");
            Assert.AreEqual(64, Apartments.PasscodeHash(7, "banana").Length);
            Assert.IsTrue(Apartments.IsPasscodeWord("abcd"));
            Assert.IsFalse(Apartments.IsPasscodeWord("abc"));
            Assert.IsFalse(Apartments.IsPasscodeWord("ab cd"));
            Assert.IsFalse(Apartments.IsPasscodeWord(new string('a', 33)));
            Assert.IsTrue(Apartments.IsName("The Den"));
            Assert.IsFalse(Apartments.IsName(" "));
            Assert.IsFalse(Apartments.IsName("bad\nname"));
            Assert.IsFalse(Apartments.IsName(new string('a', ApartmentEntry.NameLength + 1)));
            Assert.AreEqual(-Math.PI / 2, Apartments.Wrap(3 * Math.PI / 2), 1e-9);
            Assert.AreEqual(0, Apartments.YawTowards(Vector3.Zero, new Vector3(0, 0, -5)), 1e-9, "yaw 0 faces -z (Facing)");
            Assert.AreEqual(0, Vector3.Distance(Apartments.Forward(Math.PI / 2), new Vector3(-1, 0, 0)), 1e-6);
        }
    }
}
