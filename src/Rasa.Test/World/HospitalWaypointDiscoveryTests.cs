using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class HospitalWaypointDiscoveryTests
    {
        private Func<IEnumerable<(uint,uint,Vector3,string)>> _source;
        private Func<IEnumerable<(uint,uint,Vector3)>> _waypoints;
        private Func<uint,bool> _safe,_open;
        private Action<Client,CharacterTeleporterEntry> _persist;
        private static readonly Vector3 Alia = new(750.0742f,294.21094f,382.28906f);
        private static readonly Vector3 AliaWaypoint = new(807.734f,294.105f,391.145f);
        [TestInitialize]
        public void Start()
        {
            _source=Hospitals.Source; _waypoints=Hospitals.WaypointSource; _safe=Hospitals.IsSafeZone; _open=Hospitals.IsOpen; _persist=Hospitals.Persist;
            Hospitals.Source=()=>new[] { (103U,1220U,Alia,"Hospital: Alia Das"), (105U,1220U,new Vector3(-129.46875f,220.7539f,-480.16406f),"Hospital: Twin Pillars") };
            Hospitals.WaypointSource=()=>new[] { (57U,1220U,AliaWaypoint) };
            Hospitals.IsSafeZone=id=>id==105; Hospitals.IsOpen=_=>true; Hospitals.Persist=(_,_)=>{}; Hospitals.Reset();
        }
        [TestCleanup]
        public void End()
        {
            Hospitals.Source=_source; Hospitals.WaypointSource=_waypoints; Hospitals.IsSafeZone=_safe; Hospitals.IsOpen=_open; Hospitals.Persist=_persist; Hospitals.Reset();
        }
        private static Client Player(WorldTestContext world, WaypointType type=WaypointType.Waypoint)
        {
            var client=PlayerDeathTests.Player(world,600,350);
            client.Player.Position=new Vector3(600,288,350);
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id,57,(byte)type));
            return client;
        }
        [TestMethod]
        public void AliaWaypointMakesItsHospitalAvailableBeforeTheHospitalWorkerRuns()
        {
            using var world=new WorldTestContext(); var client=Player(world);
            Assert.IsFalse(Hospitals.Knows(client.Player,103));
            var available=Hospitals.AvailableTo(client.Player,1220);
            CollectionAssert.AreEquivalent(new[] {103U,105U},available.Select(h=>h.TeleporterId).ToArray());
            Assert.AreEqual(103U,Hospitals.Nearest(available,client.Player.Position).TeleporterId);
            client.Player.Attributes[Attributes.Health].Current=0;
            PlayerDeath.AtZero(world.Map,client.Player,null);
            PlayerDeath.ReviveMe(client,null);
            Assert.AreEqual(Alia.X,client.Player.Position.X,0.01f);
            Assert.AreEqual(Alia.Z,client.Player.Position.Z,0.01f);
            Assert.AreNotEqual(CharacterState.Dead,client.Player.State);
        }
        [TestMethod]
        public void DiscoveryIsPersistedOnceWithoutVisitingTheMedicalTent()
        {
            using var world=new WorldTestContext(); var client=Player(world);
            var saved=new List<CharacterTeleporterEntry>(); Hospitals.Persist=(_,entry)=>saved.Add(entry);
            Hospitals.Worker(world.Map); Hospitals.Worker(world.Map);
            Assert.IsTrue(Hospitals.Knows(client.Player,103));
            Assert.AreEqual(1,saved.Count); Assert.AreEqual(103U,saved[0].WaypointId);
            Assert.AreEqual((byte)WaypointType.Hospital,saved[0].WaypointType);
        }
        [TestMethod]
        public void AWaypointDoesNotUnlockABaneHeldHospital()
        {
            using var world=new WorldTestContext(); var client=Player(world);
            Hospitals.IsOpen=id=>id!=103;
            CollectionAssert.AreEqual(new[] {105U},Hospitals.AvailableTo(client.Player,1220).Select(h=>h.TeleporterId).ToArray());
            Hospitals.Worker(world.Map); Assert.IsFalse(Hospitals.Knows(client.Player,103));
        }
        [TestMethod]
        [DataRow(100f,true)]
        [DataRow(100.01f,false)]
        public void UnlockRadiusHasAnExactBoundary(float distance,bool expected)
        {
            using var world=new WorldTestContext(); var client=Player(world);
            Hospitals.WaypointSource=()=>new[] { (57U,1220U,Alia+new Vector3(distance,0,0)) };
            Assert.AreEqual(expected,Hospitals.AvailableTo(client.Player,1220).Any(h=>h.TeleporterId==103));
        }
        [TestMethod]
        [DataRow((int)WaypointType.LocalTeleporter)]
        [DataRow((int)WaypointType.Dropship)]
        [DataRow((int)WaypointType.Hospital)]
        public void OtherTeleporterTypesDoNotCountAsASettlementWaypoint(int type)
        {
            using var world=new WorldTestContext(); var client=Player(world,(WaypointType)type);
            Assert.IsFalse(Hospitals.AvailableTo(client.Player,1220).Any(h=>h.TeleporterId==103));
        }
        [TestMethod]
        public void AWaypointOnAnotherMapCannotUnlockAHospitalHere()
        {
            using var world=new WorldTestContext(); var client=Player(world);
            Hospitals.WaypointSource=()=>new[] { (57U,1148U,AliaWaypoint) };
            Assert.IsFalse(Hospitals.AvailableTo(client.Player,1220).Any(h=>h.TeleporterId==103));
        }
    }
}
