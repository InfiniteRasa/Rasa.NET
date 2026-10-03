using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Player death (PlayerDeath) and the hospitals they go back to (Hospitals): dying at zero
    // health, a GM who may not die, the hospital window and ReviveMe, Rez Trauma, gaining a
    // hospital by walking up to it, a medic's revive offer, and PvP deaths.
    [TestClass]
    [DoNotParallelize]
    public class PlayerDeathTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map

        // Alia Das (103) is known by name, Twin Pillars (105) is a safe zone, Ranja Gorge (106) is
        // neither, and 9001 has no name in the client.
        private static readonly (uint, uint, Vector3, string)[] TestHospitals =
        {
            (103, MapId, new Vector3(100, 0, 0), "Hospital: Alia Das"),
            (105, MapId, new Vector3(-300, 0, 0), "Hospital: Twin Pillars"),
            (106, MapId, new Vector3(40, 0, 40), "Hospital: Ranja Gorge"),
            (9001, MapId, new Vector3(0, 0, 400), "Hospital: Nowhere"),
            (9002, 1148, new Vector3(5, 0, 5), "Hospital: Elsewhere")
        };

        private Func<IEnumerable<(uint, uint, Vector3, string)>> _source;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;
        private Func<long> _now;

        [TestInitialize]
        public void UseTestHospitals()
        {
            _source = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;
            _now = PlayerDeath.Now;

            Hospitals.Source = () => TestHospitals;
            Hospitals.IsSafeZone = id => id == 105;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();
        }

        [TestCleanup]
        public void RestoreHospitals()
        {
            Hospitals.Source = _source;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            PlayerDeath.Now = _now;
            Hospitals.Reset();
        }

        [TestMethod]
        public void EachHospitalHasAGraveyardIdOfItsOwnOnItsMap()
        {
            var onMap = Hospitals.OnMap(MapId);

            Assert.AreEqual(4, onMap.Count);
            Assert.AreEqual(3u, onMap.Single(h => h.TeleporterId == 103).GraveyardId, "Alia Das Hospital");
            Assert.AreEqual(5u, onMap.Single(h => h.TeleporterId == 106).GraveyardId, "Ranja Gorge Hospital");
            Assert.AreEqual(HospitalGraveyards.GenericIds[0], onMap.Single(h => h.TeleporterId == 9001).GraveyardId, "a generic name");
            Assert.AreEqual(onMap.Count, onMap.Select(h => h.GraveyardId).Distinct().Count());

            Assert.IsTrue(onMap.Single(h => h.TeleporterId == 105).IsSafe);
            Assert.IsTrue(onMap.Single(h => h.TeleporterId == 105).IsFree);
            Assert.IsFalse(onMap.Single(h => h.TeleporterId == 103).IsFree);

            Assert.IsTrue(Hospitals.IsBase("Hospital: Foreas Base"));
            Assert.IsFalse(Hospitals.IsBase("Hospital: River-base Krymm (Control Point)"));
            Assert.IsFalse(Hospitals.IsBase("Hospital: Alia Das"));
        }

        [TestMethod]
        public void WalkingUpToAHospitalGainsItOnce()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 95, 0);
            var saved = new List<CharacterTeleporterEntry>();
            Hospitals.Persist = (c, entry) => saved.Add(entry);

            Hospitals.Worker(world.Map);

            Assert.IsTrue(Hospitals.Knows(client.Player, 103));
            Assert.AreEqual(103u, saved.Single().WaypointId);
            Assert.AreEqual((byte)WaypointType.Hospital, saved.Single().WaypointType);
            Assert.AreEqual(103u, MissionTestContext.Drain(client).OfType<GraveyardGainedPacket>().Single().WaypointId);

            Hospitals.Worker(world.Map);
            Assert.AreEqual(1, saved.Count, "gained once");
        }

        [TestMethod]
        public void AHospitalIsGainedUnderAWaypointIdTheClientHasANameFor()
        {
            Assert.AreEqual(103u, HospitalGraveyards.GainedAs(103), "the client's own id");
            Assert.AreEqual(500u, HospitalGraveyards.GainedAs(598), "the Bootcamp hospital: Refugee Base Medic");
            Assert.AreEqual(176u, HospitalGraveyards.GainedAs(592), "Temple of the Proud Patriarch");
            Assert.AreEqual(HospitalGraveyards.GenericWaypoint, HospitalGraveyards.GainedAs(602), "no name anywhere");
            Assert.AreEqual(HospitalGraveyards.GenericWaypoint, HospitalGraveyards.GainedAs(9001));
            Assert.AreEqual(10000005u, HospitalGraveyards.GainedAs(10000005), "the client's test ids");
            Assert.IsTrue(HospitalGraveyards.ClientNames(HospitalGraveyards.GenericWaypoint));

            using var world = new WorldTestContext();
            var client = Player(world, 0, 395);
            var saved = new List<CharacterTeleporterEntry>();
            Hospitals.Persist = (c, entry) => saved.Add(entry);

            Hospitals.Worker(world.Map);

            Assert.IsTrue(Hospitals.Knows(client.Player, 9001));
            Assert.AreEqual(9001u, saved.Single().WaypointId, "kept under its own id");
            Assert.AreEqual(HospitalGraveyards.GenericWaypoint, MissionTestContext.Drain(client).OfType<GraveyardGainedPacket>().Single().WaypointId);
        }

        [TestMethod]
        public void TheNamesHospitalsAreGainedUnderAreTheClientsAndTheHospitalsAreOurs()
        {
            var rows = new TeleporterRows().All().ToDictionary(r => Convert.ToUInt32(r[0]));

            foreach (var name in HospitalGraveyards.WaypointNames)
            {
                Assert.IsTrue(rows.ContainsKey(name.Key), $"{name.Key} is a teleporter");
                Assert.AreEqual((int)WaypointType.Hospital, Convert.ToInt32(rows[name.Key][2]), $"{name.Key} is a hospital");
                Assert.IsFalse(HospitalGraveyards.ClientNames(name.Key), $"{name.Key} has no name of its own");
                Assert.IsTrue(rows.ContainsKey(name.Value), $"{name.Value} is a waypoint id");
                Assert.IsTrue(HospitalGraveyards.ClientNames(name.Value), $"{name.Value} has a name");
            }

            // Every hospital there is goes out under an id the client names.
            foreach (var row in rows.Values.Where(r => Convert.ToInt32(r[2]) == (int)WaypointType.Hospital))
                Assert.IsTrue(HospitalGraveyards.ClientNames(HospitalGraveyards.GainedAs(Convert.ToUInt32(row[0]))), $"hospital {row[0]}");
        }

        private sealed class TeleporterRows : Rasa.Services.Preloader.TeleporterPreloader
        {
            public IEnumerable<object[]> All() => GetRows();
        }

        [TestMethod]
        public void TheHospitalsOfferedAreTheGainedAndTheFreeOrElseTheNearest()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 0, 0);

            CollectionAssert.AreEqual(new uint[] { 105 }, Ids(Hospitals.AvailableTo(client.Player, MapId)), "the safe zone alone");

            Hospitals.Gain(client, Hospitals.OnMap(MapId).Single(h => h.TeleporterId == 103));
            CollectionAssert.AreEquivalent(new uint[] { 103, 105 }, Ids(Hospitals.AvailableTo(client.Player, MapId)));

            Hospitals.IsSafeZone = id => false;
            Hospitals.Reset();
            var stranger = Player(world, 30, 30);
            CollectionAssert.AreEqual(new uint[] { 106 }, Ids(Hospitals.AvailableTo(stranger.Player, MapId)), "nothing known or free: the nearest");
        }

        [TestMethod]
        public void APlayerBroughtToZeroByACreatureDiesAndIsOfferedTheirHospitals()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 0, 0);
            var onlooker = Player(world, 5, 0);
            var creature = Monster(world, 3);

            try
            {
                WorldTestContext.Drain(client);
                WorldTestContext.Drain(onlooker);

                ActorManager.Instance.Damage(world.Map, client.Player, 5000, creature);

                Assert.AreEqual(CharacterState.Dead, client.Player.State);
                Assert.AreEqual(0, client.Player.Attributes[Attributes.Health].Current);

                var dead = MissionTestContext.Drain(client).OfType<PlayerDeadPacket>().Single();
                Assert.AreEqual(creature.EntityId, dead.SourceId);
                CollectionAssert.AreEqual(new uint[] { 6 }, dead.Graveyards.Select(g => g.Id).ToArray(), "Twin Pillars Hospital, the safe zone");
                Assert.IsTrue(dead.Graveyards.Single().IsSafe);
                Assert.IsFalse(dead.CanRevive);

                Assert.IsTrue(MissionTestContext.Drain(onlooker).OfType<ActorKilledPacket>().Any());
                Assert.IsTrue(ManifestationManager.Instance.CreatePlayerEntityData(client, onlooker).OfType<DeadOnArrivalPacket>().Any());
                Assert.IsFalse(ManifestationManager.Instance.CreatePlayerEntityData(onlooker, client).OfType<DeadOnArrivalPacket>().Any());

                Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, client.Player, 100, creature), "nothing more to take");
            }
            finally
            {
                Unregister(world, creature);
            }
        }

        [TestMethod]
        public void AGmStandsBackUpUnlessDeathIsAllowed()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0);
            GameMaster(gm);

            Assert.IsTrue(PlayerDeath.IsDeathless(gm));
            AtZero(world, gm);
            Assert.AreNotEqual(CharacterState.Dead, gm.Player.State);
            Assert.AreEqual(1000, gm.Player.Attributes[Attributes.Health].Current, "back on full");

            gm.Player.AllowDeath = true;
            Assert.IsFalse(PlayerDeath.IsDeathless(gm));
            AtZero(world, gm);
            Assert.AreEqual(CharacterState.Dead, gm.Player.State);
        }

        [TestMethod]
        public void ReviveMeGoesToTheHospitalChosenOrTheNearestOnFullHealth()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 90, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 200, 200, 0, 0, 0);
            Hospitals.Gain(client, Hospitals.OnMap(MapId).Single(h => h.TeleporterId == 103));

            AtZero(world, client);
            WorldTestContext.Drain(client);

            PlayerDeath.ReviveMe(client, 6);

            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
            Assert.AreEqual(-300f, client.Player.Position.X, 0.01f, "Twin Pillars, as chosen");
            Assert.AreEqual(1000, client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(200, client.Player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(client.Player.EntityId, MissionTestContext.Drain(client).OfType<RevivedPacket>().Single().SourceId);

            client.Player.PlaceAt(new Vector3(90, 0, 0));
            AtZero(world, client);
            PlayerDeath.ReviveMe(client, 5);   // Ranja Gorge: not theirs
            Assert.AreEqual(100f, client.Player.Position.X, 0.01f, "the nearest of theirs, Alia Das");

            PlayerDeath.ReviveMe(client, 6);
            Assert.AreEqual(100f, client.Player.Position.X, 0.01f, "alive: nothing to do");
        }

        [TestMethod]
        public void FromLevelFiveComingBackCostsRezTraumaNoHealingAndItStacks()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 0, 0);
            client.Player.Level = 5;

            AtZero(world, client);
            PlayerDeath.ReviveMe(client, null);

            var trauma = client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(1, trauma.Stacks);
            Assert.AreEqual(-20, trauma.PrimaryAttributesPercent);
            AssertLeft(120000, trauma);
            Assert.AreEqual(-20, GameEffectManager.AttributePercentOf(client.Player, Attributes.Body));
            Assert.AreEqual(-20, GameEffectManager.AttributePercentOf(client.Player, Attributes.Spirit));
            Assert.IsTrue(GameEffectManager.HealingBlocked(client.Player), "REZ_SICKNESS_NO_HEAL");
            AssertLeft(30000, client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessNoHealTypeId));

            AtZero(world, client);
            Assert.IsTrue(client.Player.ActiveEffects.Values.Any(e => e.TypeId == PlayerDeath.RezSicknessTypeId), "Rez Trauma stays through a death");
            PlayerDeath.ReviveMe(client, null);

            trauma = client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(2, trauma.Stacks);
            Assert.AreEqual(-40, trauma.PrimaryAttributesPercent);
            AssertLeft(240000, trauma);

            for (var i = 0; i < 3; i++)
            {
                AtZero(world, client);
                PlayerDeath.ReviveMe(client, null);
            }

            trauma = client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(3, trauma.Stacks, "REZ_SICKNESS_MAX_STACK");
            Assert.AreEqual(-60, trauma.PrimaryAttributesPercent, "REZ_SICKNESS_MAX_PENALTY");
            AssertLeft(360000, trauma);
        }

        [TestMethod]
        public void BelowLevelFiveComingBackIsFree()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 0, 0);
            client.Player.Level = 4;

            AtZero(world, client);
            PlayerDeath.ReviveMe(client, null);

            Assert.IsFalse(client.Player.ActiveEffects.Values.Any(e => e.TypeId == PlayerDeath.RezSicknessTypeId || e.TypeId == PlayerDeath.RezSicknessNoHealTypeId));
        }

        [TestMethod]
        public void AMedicsReviveIsOfferedAndTakenWhereTheyLie()
        {
            using var world = new WorldTestContext();
            var dead = Player(world, 20, 0);
            var medic = Player(world, 22, 0);

            AtZero(world, dead);
            WorldTestContext.Drain(dead);

            Assert.IsTrue(PlayerDeath.OfferRevive(world.Map, medic.Player, dead.Player, 500));
            var offer = MissionTestContext.Drain(dead).OfType<ReviveRequestInfoPacket>().Single();
            Assert.AreEqual(medic.Player.EntityId, offer.ReviverId);
            Assert.AreEqual(PlayerDeath.ReviveRequestSeconds, offer.SecondsToEnd);

            PlayerDeath.RequestRevive(dead, medic.Player.EntityId);

            Assert.AreNotEqual(CharacterState.Dead, dead.Player.State);
            Assert.AreEqual(500, dead.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(20f, dead.Player.Position.X, 0.01f, "where they lay");
            Assert.AreEqual(medic.Player.EntityId, MissionTestContext.Drain(dead).OfType<RevivedPacket>().Single().SourceId);

            Assert.IsFalse(PlayerDeath.OfferRevive(world.Map, medic.Player, dead.Player, 500), "alive: nothing to offer");
        }

        [TestMethod]
        public void GroupResuscitateOffersTheSquadsDeadHalfTheirHealth()
        {
            using var world = new WorldTestContext();
            var medic = Player(world, 0, 0);
            var mate = Player(world, 10, 0);
            var farMate = Player(world, 200, 0);
            var stranger = Player(world, 5, 0);
            medic.Player.PartyId = mate.Player.PartyId = farMate.Player.PartyId = 31;

            AtZero(world, mate);
            AtZero(world, farMate);
            AtZero(world, stranger);

            var info = new ActionLevelInfo { ActionId = ActionId.AaRecruitLightning, Level = 5 };
            info.Properties[AbilityProperty.RadiusAroundSource] = 25;
            info.Properties[AbilityProperty.AttributeMaxChange] = 50;
            var abilities = (AbilityManager)typeof(AbilityManager)
                .GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

            typeof(AbilityManager).GetMethod("Cure", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(abilities, new object[] { world.Map, medic.Player, new ActionData(medic.Player, ActionId.AaRecruitLightning, 5, 0, 0), info });

            Assert.IsTrue(PlayerDeath.HasOffer(mate.Player, medic.Player.EntityId));
            Assert.IsFalse(PlayerDeath.HasOffer(farMate.Player, medic.Player.EntityId), "out of reach");
            Assert.IsFalse(PlayerDeath.HasOffer(stranger.Player, medic.Player.EntityId), "not in the squad");

            PlayerDeath.RequestRevive(mate, medic.Player.EntityId);
            Assert.AreEqual(500, mate.Player.Attributes[Attributes.Health].Current, "ATTRIBUTE_MAX_CHANGE 50%");
        }

        [TestMethod]
        public void ARefusedOrLapsedReviveIsNotTaken()
        {
            using var world = new WorldTestContext();
            var dead = Player(world, 20, 0);
            var medic = Player(world, 22, 0);
            var clock = 1000L;
            PlayerDeath.Now = () => clock;

            AtZero(world, dead);

            PlayerDeath.OfferRevive(world.Map, medic.Player, dead.Player, 500);
            PlayerDeath.RefuseRevive(dead, medic.Player.EntityId);
            PlayerDeath.RequestRevive(dead, medic.Player.EntityId);
            Assert.AreEqual(CharacterState.Dead, dead.Player.State, "refused");

            PlayerDeath.OfferRevive(world.Map, medic.Player, dead.Player, 500);
            clock += PlayerDeath.ReviveRequestSeconds * 1000L + 1;
            PlayerDeath.RequestRevive(dead, medic.Player.EntityId);
            Assert.AreEqual(CharacterState.Dead, dead.Player.State, "lapsed");
        }

        [TestMethod]
        public void LeavingDeadGoesToTheHospitalFirst()
        {
            using var world = new WorldTestContext();
            var client = Player(world, -280, 0);

            AtZero(world, client);
            PlayerDeath.PlayerLeaving(client);

            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
            Assert.AreEqual(-300f, client.Player.Position.X, 0.01f);
        }

        [TestMethod]
        public void AFallThatTakesTheLastOfTheirHealthKills()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 0, 0);
            client.Player.Attributes[Attributes.Health].Current = 10;

            FallDamage.Apply(world.Map, client.Player, 200f);

            Assert.AreEqual(CharacterState.Dead, client.Player.State);
            Assert.IsTrue(MissionTestContext.Drain(client).OfType<AnnounceMapDamagePacket>().Single().DeathBlow);
        }

        private static uint[] Ids(IEnumerable<Hospitals.Hospital> hospitals) => hospitals.Select(h => h.TeleporterId).OrderBy(id => id).ToArray();

        private static void AtZero(WorldTestContext world, Client client)
        {
            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
        }

        private static void AssertLeft(int expectedMs, GameEffect effect)
        {
            var left = effect.ExpiresTick - Environment.TickCount64;
            Assert.IsTrue(Math.Abs(left - expectedMs) < 1000, $"{left} ms left, expected about {expectedMs}");
        }

        internal static Client Player(WorldTestContext world, float x, float z)
        {
            var client = world.CreateClient(x, z);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        internal static void GameMaster(Client client)
        {
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry
            {
                Id = 7,
                FamilyName = "Fixture",
                Level = (byte)GmLevel.GameMaster,
                Characters = new List<CharacterEntry>()
            });
        }

        private static Creature Monster(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Monster",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            return creature;
        }

        private static void Unregister(WorldTestContext world, Creature creature)
        {
            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                cell.CreatureList.Remove(creature);

            EntityManager.Instance.UnregisterEntity(creature.EntityId);
            EntityManager.Instance.UnregisterCreature(creature.EntityId);
        }
    }
}
