extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// The maps the map channel worker goes through (MapChannelManager.MapsToTick): kept from tick
    /// to tick, and always the list the expression it replaced gives - the loaded maps, then the
    /// instances, each once - as maps are loaded and instances open and close.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MapsToTickTests
    {
        private static MapChannel Map(uint id) => new()
        {
            MapInfo = new MapInfo(id, $"map{id}", 1556, 0),
            ClientList = new List<Client>()
        };

        private static MapChannel[] AsBefore(MapChannelManager maps, PrivateMapInstanceService instances) =>
            maps.MapChannelArray.Values.Concat(instances.Snapshot()).Distinct().ToArray();

        [TestMethod]
        public void TheSameMapsAsTheExpressionItReplaced()
        {
            var instances = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: instances);
            var a = Map(1);
            var b = Map(2);

            void Check(string when) => CollectionAssert.AreEqual(AsBefore(maps, instances), maps.MapsToTick(), when);

            Check("nothing");

            maps.MapChannelArray.Add(1, a);
            Check("a map loaded");

            maps.MapChannelArray.Add(2, b);
            Check("another");

            var mine = instances.GetOrCreate(a, 7);
            Check("a private instance");

            var shared = instances.CreateShared(b);
            Check("a shared copy");

            var squad = instances.CreateSquad(a, 9);
            Check("a squad instance");

            var apartment = instances.CreateApartment(b, 11);
            Check("an apartment's copy");

            // A map listed twice is gone through once, as Distinct had it.
            maps.MapChannelArray.Add(3, a);
            Check("a map under two ids");

            maps.MapChannelArray.Remove(3);
            Check("taken off again");

            // Replaced under its id: the same count, another map.
            maps.MapChannelArray[2] = Map(2);
            Check("a map replaced");

            Assert.IsTrue(instances.ReleaseShared(shared));
            Check("the shared copy closed");

            Assert.IsTrue(instances.ReleaseSquad(squad));
            Check("the squad instance closed");

            Assert.IsTrue(instances.ReleaseApartment(apartment));
            Check("the apartment's copy closed");

            CollectionAssert.AreEqual(new[] { mine }, instances.ReleaseOwned(7).ToArray());
            Check("the private instance released");
        }

        [TestMethod]
        public void KeptWhileNothingChanges()
        {
            var instances = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: instances);
            maps.MapChannelArray.Add(1, Map(1));
            instances.GetOrCreate(maps.MapChannelArray[1], 7);

            var first = maps.MapsToTick();

            var before = System.GC.GetAllocatedBytesForCurrentThread();
            var again = maps.MapsToTick();
            var bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreSame(first, again);
            Assert.AreEqual(0L, bytes);

            // A list handed out stays as it was when the maps change.
            instances.CreateShared(maps.MapChannelArray[1]);
            var after = maps.MapsToTick();

            Assert.AreNotSame(first, after);
            Assert.HasCount(2, first);
            Assert.HasCount(3, after);
        }
    }
}
