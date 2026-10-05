using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Encounters;

    /// <summary>
    /// "Wilderness Assassin: Assassinated all 6 Thrax Officers in Wilderness." A character who
    /// has killed every boss on a battlefield's list has its title; each kill is recorded when
    /// it is made, and no mission is asked.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BossTitlesTests
    {
        private const uint WildernessAssassin = 367;
        private const uint PlateauAssassin = 407;
        private const uint DescentAssassin = 514;
        private const uint HuntersHunter = 473;

        private const uint WildernessMap = BootcampRuntimeTestHarness.WildernessMapContextId;
        private const uint PlateauMap = 1497;
        private const uint UstorYard = 1502;            // an instance off Plateau
        private const uint PoolsMap = 1304;

        // Glognar, Phlegg, Rankash, Horntail, Old Scratch, Archfiend, by the names their rows have.
        private static readonly uint[] Officers = { 6751, 9312, 9313, 6719, 6717, 6716 };
        private const uint GlognarByHisOtherName = 7022;
        private const uint MoxAlphaMale = 8797;

        private Func<Client, Vector3, List<Client>> _sharers;

        [TestInitialize]
        public void KeepTheSquad() => _sharers = BossTitles.SharersOf;

        [TestCleanup]
        public void RestoreTheSquad() => BossTitles.SharersOf = _sharers;

        #region The lists

        [TestMethod]
        public void TheListsAreTheFourteenTitlesAndABossIsOneTitles()
        {
            var zones = BossTitles.Zones;

            Assert.HasCount(14, zones);
            Assert.HasCount(14, zones.Select(zone => zone.TitleId).Distinct().ToArray());
            Assert.HasCount(14, zones.Select(zone => zone.Maps[0]).Distinct().ToArray());

            CollectionAssert.AreEquivalent(
                new Dictionary<string, int>
                {
                    ["Wilderness"] = 6, ["Divide"] = 5, ["Palisades"] = 8, ["Plateau"] = 5, ["Pools"] = 10, ["Marshes"] = 9,
                    ["Descent"] = 8, ["Plains"] = 4, ["Mires"] = 7, ["Incline"] = 4, ["Ashen Desert"] = 6, ["Thunderhead"] = 9,
                    ["Abyss"] = 9, ["Crucible"] = 6
                }.ToArray(),
                zones.ToDictionary(zone => zone.Name, zone => zone.Bosses.Count).ToArray());

            foreach (var zone in zones)
            {
                Assert.HasCount(zone.Maps.Count, zone.Maps.Distinct().ToArray(), zone.Name);
                Assert.IsFalse(zone.Bosses.Any(boss => boss.NameIds.Count == 0 || boss.NameIds.Contains(0u)), zone.Name);

                // All of them, but for Descent's seven of eight.
                Assert.AreEqual(zone.TitleId == DescentAssassin ? 7 : zone.Bosses.Count, zone.Needed, zone.Name);

                foreach (var map in zone.Maps)
                    foreach (var nameId in zone.Bosses.SelectMany(boss => boss.NameIds))
                        Assert.AreSame(zone, BossTitles.ZoneOf(map, nameId), $"{zone.Name}: {nameId} on {map}");
            }

            // A name is one boss's, and a map one battlefield's.
            var names = zones.SelectMany(zone => zone.Bosses.SelectMany(boss => boss.NameIds)).ToArray();
            var maps = zones.SelectMany(zone => zone.Maps).ToArray();

            Assert.HasCount(names.Length, names.Distinct().ToArray());
            Assert.HasCount(maps.Length, maps.Distinct().ToArray());
        }

        [TestMethod]
        public void TheWorldHasTheBossesItHasAndTheListsAskForTheRest()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            var named = harness.WorldContext.Set<CreatureEntry>().AsNoTracking().ToList()
                .Where(row => row.NameId != 0).ToLookup(row => row.NameId, row => row.Id);

            // The Wilderness six are the rows of the server's recovered objective (mission 1449, objective 6).
            CollectionAssert.AreEquivalent(new uint[] { 82, 83, 84, 79, 80, 75 }, Officers.SelectMany(nameId => named[nameId]).ToArray());

            // How many of each list have a creature row today. The rest are in the list for the
            // day they have one. (Descent's one is Fortuna and Merrick, who stand there as NPCs.)
            CollectionAssert.AreEquivalent(
                new Dictionary<string, int>
                {
                    ["Wilderness"] = 6, ["Divide"] = 5, ["Palisades"] = 8, ["Plateau"] = 4, ["Pools"] = 7, ["Marshes"] = 9,
                    ["Descent"] = 1, ["Plains"] = 3, ["Mires"] = 0, ["Incline"] = 4, ["Ashen Desert"] = 5, ["Thunderhead"] = 2,
                    ["Abyss"] = 1, ["Crucible"] = 1
                }.ToArray(),
                BossTitles.Zones.ToDictionary(
                    zone => zone.Name,
                    zone => zone.Bosses.Count(boss => boss.NameIds.Any(nameId => named.Contains(nameId)))).ToArray());
        }

        [TestMethod]
        public void SevenOfDescentsEightAreEnoughAndFortunaAndMerrickAreOneOfThem()
        {
            var descent = BossTitles.Zones.Single(zone => zone.TitleId == DescentAssassin);
            var seven = new uint[] { 9339, 9670, 9338, 9337, 9737, 9738, 9324 };
            const uint Fortuna = 9331, Merrick = 9333;

            Assert.IsTrue(BossTitles.Earned(descent, seven));
            Assert.IsFalse(BossTitles.Earned(descent, seven.Take(6).ToArray()));
            Assert.IsTrue(BossTitles.Earned(descent, seven.Take(6).Append(Fortuna).ToArray()));
            Assert.IsTrue(BossTitles.Earned(descent, seven.Take(6).Append(Merrick).ToArray()));
            Assert.IsFalse(BossTitles.Earned(descent, seven.Take(5).Append(Fortuna).Append(Merrick).ToArray()), "the two are one boss between them");

            // Every other list is all of it.
            var wilderness = BossTitles.Zones.Single(zone => zone.TitleId == WildernessAssassin);

            Assert.IsFalse(BossTitles.Earned(wilderness, Officers.Take(5).ToArray()));
            Assert.IsTrue(BossTitles.Earned(wilderness, Officers));
            Assert.IsFalse(BossTitles.Earned(wilderness, Officers.Take(5).Append(GlognarByHisOtherName).ToArray()), "Glognar twice is not Archfiend");
            Assert.IsTrue(BossTitles.Earned(wilderness, Officers.Skip(1).Append(GlognarByHisOtherName).ToArray()), "either of his names is Glognar");
        }

        #endregion

        #region A kill

        [TestMethod]
        public void TheLastBossOfAListGivesItsTitle()
        {
            using var harness = Start();
            var wilderness = harness.Maps.MapChannelArray[WildernessMap];

            foreach (var nameId in Officers.Take(5))
                BossTitles.Killed(harness.Client, wilderness, Boss(nameId));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "five of six");
            Assert.IsFalse(harness.Client.Player.Titles.Contains(WildernessAssassin));
            CollectionAssert.AreEquivalent(Officers.Take(5).ToArray(), Recorded(harness));
            CollectionAssert.AreEquivalent(Officers.Take(5).ToArray(), harness.Client.Player.BossKills.ToArray());

            // One of the five again is nothing new.
            BossTitles.Killed(harness.Client, wilderness, Boss(Officers[0]));

            Assert.HasCount(5, Recorded(harness));
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());

            BossTitles.Killed(harness.Client, wilderness, Boss(Officers[5]));

            Assert.AreEqual(WildernessAssassin, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(harness.Client.Player.Titles.Contains(WildernessAssassin));

            using (var unit = harness.Context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), WildernessAssassin);

            // And again: the title is theirs already.
            BossTitles.Killed(harness.Client, wilderness, Boss(Officers[5]));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
            Assert.HasCount(6, Recorded(harness));
        }

        [TestMethod]
        public void ACreatureKilledByThePlayerIsRecordedThroughTheKillItself()
        {
            using var harness = Start();

            // The map the player stands on is made Wilderness.
            harness.BootcampMap.MapInfo.MapContextId = WildernessMap;
            harness.Client.Player.MapContextId = WildernessMap;

            var horntail = InTheWorld(harness, Officers[3]);
            var nobody = InTheWorld(harness, 4242);

            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, nobody, harness.Client.Player);

            Assert.AreEqual(CharacterState.Dead, nobody.State);
            Assert.IsEmpty(Recorded(harness), "not a boss of any list");

            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, horntail, harness.Client.Player);

            CollectionAssert.AreEqual(new[] { Officers[3] }, Recorded(harness));

            // Killed by nobody, it is nobody's.
            var scratch = InTheWorld(harness, Officers[4]);

            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, scratch, null);

            Assert.AreEqual(CharacterState.Dead, scratch.State);
            CollectionAssert.AreEqual(new[] { Officers[3] }, Recorded(harness));
        }

        [TestMethod]
        public void ABossIsItsNameOnItsBattlefieldsMapsOnly()
        {
            using var harness = Start();

            // The ordinary Mox of Pools carry the name of Plateau's boss.
            BossTitles.Killed(harness.Client, Map(PoolsMap), Boss(MoxAlphaMale));
            Assert.IsNull(BossTitles.ZoneOf(PoolsMap, MoxAlphaMale));
            Assert.IsEmpty(Recorded(harness));

            // An officer's name off Wilderness, a name of no list on it, and no name at all.
            BossTitles.Killed(harness.Client, Map(PlateauMap), Boss(Officers[0]));
            BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(4242));
            BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(0));
            Assert.IsEmpty(Recorded(harness));

            // On Plateau, and in an instance off it, it is the boss.
            Assert.AreEqual(PlateauAssassin, BossTitles.ZoneOf(UstorYard, MoxAlphaMale).TitleId);
            BossTitles.Killed(harness.Client, Map(UstorYard), Boss(MoxAlphaMale));
            CollectionAssert.AreEqual(new[] { MoxAlphaMale }, Recorded(harness));
        }

        [TestMethod]
        public void TheSquadNearTheCorpseSharesTheKill()
        {
            using var harness = Start();
            var wilderness = harness.Maps.MapChannelArray[WildernessMap];
            var mate = harness.Context.CreateAdditionalClient(2);
            var stranger = harness.Context.CreateAdditionalClient(3);
            Vector3 asked = default;

            BossTitles.SharersOf = (killer, corpse) =>
            {
                asked = corpse;
                return new List<Client> { killer, mate };
            };

            // The mate has five already; the killer has none.
            foreach (var nameId in Officers.Take(5))
                Assert.IsTrue(ManifestationManager.Instance.RecordBossKill(mate, nameId));

            var archfiend = Boss(Officers[5]);
            archfiend.Position = new Vector3(3, 0, 0);

            BossTitles.Killed(harness.Client, wilderness, archfiend);

            Assert.AreEqual(archfiend.Position, asked, "measured from the corpse");
            CollectionAssert.AreEqual(new[] { Officers[5] }, Recorded(harness));
            Assert.HasCount(6, Recorded(harness, mate.Player.Id));
            Assert.IsEmpty(Recorded(harness, stranger.Player.Id));

            Assert.IsTrue(mate.Player.Titles.Contains(WildernessAssassin), "the last of the mate's six");
            Assert.IsFalse(harness.Client.Player.Titles.Contains(WildernessAssassin));

            using (var unit = harness.Context.CreateChar())
            {
                CollectionAssert.Contains(unit.CharacterTitles.Get(mate.Player.Id), WildernessAssassin);
                Assert.IsEmpty(unit.CharacterTitles.Get(harness.Client.Player.Id));
            }
        }

        [TestMethod]
        public void APlayerInNoSquadSharesWithNobody()
        {
            using var harness = Start();
            var other = harness.Context.CreateAdditionalClient(2);

            BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(Officers[0]));

            CollectionAssert.AreEqual(new[] { Officers[0] }, Recorded(harness));
            Assert.IsEmpty(Recorded(harness, other.Player.Id));

            // A squad that cannot be found leaves the killer their kill.
            BossTitles.SharersOf = (_, _) => throw new InvalidOperationException("no squads");
            BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(Officers[1]));

            CollectionAssert.AreEquivalent(Officers.Take(2).ToArray(), Recorded(harness));
        }

        [TestMethod]
        public void AKillThatCannotBeSavedIsNotCounted()
        {
            using var harness = Start();
            var wilderness = harness.Maps.MapChannelArray[WildernessMap];

            foreach (var nameId in Officers.Take(5))
                BossTitles.Killed(harness.Client, wilderness, Boss(nameId));

            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected boss kill failure.");

            try
            {
                BossTitles.Killed(harness.Client, wilderness, Boss(Officers[5]));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }

            Assert.IsFalse(harness.Client.Player.BossKills.Contains(Officers[5]));
            Assert.IsFalse(harness.Client.Player.Titles.Contains(WildernessAssassin));
            Assert.HasCount(5, Recorded(harness));

            // The next kill of it is the one that counts.
            BossTitles.Killed(harness.Client, wilderness, Boss(Officers[5]));

            Assert.IsTrue(harness.Client.Player.Titles.Contains(WildernessAssassin));
        }

        #endregion

        #region Kept

        [TestMethod]
        public void TheKillsAreTheCharactersWhenTheyComeBack()
        {
            // A character loaded as a login loads it (CharacterManager), and loaded again.
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var characterId = harness.Client.Player.Id;

            harness.Drain();
            Assert.IsEmpty(harness.Client.Player.BossKills);

            foreach (var nameId in Officers.Take(3))
                BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(nameId));

            harness.ReconnectFromSelection();

            Assert.AreEqual(characterId, harness.Client.Player.Id);
            CollectionAssert.AreEquivalent(Officers.Take(3).ToArray(), harness.Client.Player.BossKills.ToArray());

            foreach (var nameId in Officers.Skip(3))
                BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(nameId));

            Assert.AreEqual(WildernessAssassin, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
        }

        [TestMethod]
        public void ACharacterWhoseKillsAreAllRecordedIsGivenTheTitleOnComingOntoAMap()
        {
            using var harness = Start();

            // Recorded, with no title for them.
            using (var unit = harness.Context.CreateChar())
                foreach (var nameId in Officers)
                    Assert.IsTrue(unit.CharacterBossKills.Add(harness.Client.Player.Id, nameId));

            harness.Client.Player.BossKills.UnionWith(Officers);
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var packets = harness.Drain();
            Assert.AreEqual(WildernessAssassin, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<TitlesPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "added to the list the client has been given");

            // The next map: it is in the list, and not gained again.
            ManifestationManager.Instance.AssignPlayer(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void ACharacterWithNoKillsIsGivenNothing()
        {
            using var harness = Start();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
            Assert.IsEmpty(harness.Client.Player.Titles);
        }

        [TestMethod]
        public void TheRecordIsOneRowABossACharacter()
        {
            using var harness = Start();
            var characterId = harness.Client.Player.Id;

            using (var unit = harness.Context.CreateChar())
            {
                Assert.IsTrue(unit.CharacterBossKills.Add(characterId, 6751));
                Assert.IsFalse(unit.CharacterBossKills.Add(characterId, 6751), "recorded already");
                Assert.IsTrue(unit.CharacterBossKills.Add(characterId, 9312));
                Assert.IsTrue(unit.CharacterBossKills.Add(characterId + 1000, 6751), "another character's is their own");

                CollectionAssert.AreEquivalent(new uint[] { 6751, 9312 }, unit.CharacterBossKills.Get(characterId));
                CollectionAssert.AreEqual(new uint[] { 6751 }, unit.CharacterBossKills.Get(characterId + 1000));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unit.CharacterBossKills.Add(0, 6751));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unit.CharacterBossKills.Add(characterId, 0));
            }

            // A player who is no saved character has nothing recorded, and the kill goes on.
            harness.Client.Player.Id = 0;

            Assert.IsFalse(ManifestationManager.Instance.RecordBossKill(harness.Client, 6719));
            BossTitles.Killed(harness.Client, harness.Maps.MapChannelArray[WildernessMap], Boss(6717));
            Assert.IsEmpty(harness.Client.Player.BossKills);

            harness.Client.Player.Id = characterId;

            using var database = harness.Context.Open();
            var key = database.Model.FindEntityType(typeof(CharacterBossKillEntry)).FindPrimaryKey();

            Assert.AreEqual("character_boss_kill", database.Model.FindEntityType(typeof(CharacterBossKillEntry)).GetTableName());
            CollectionAssert.AreEqual(new[] { "character_id", "creature_name_id" }, key.Properties.Select(property => property.GetColumnName()).ToArray());
        }

        #endregion

        private static BootcampRuntimeTestHarness.Harness Start()
        {
            var harness = BootcampRuntimeTestHarness.Create();

            harness.Drain();
            return harness;
        }

        /// <summary>A creature of a name, as a kill is told of it.</summary>
        private static Creature Boss(uint nameId) => new Creature { NameId = nameId, EntityClass = EntityClasses.HumanBaseMale };

        /// <summary>A map of a battlefield the harness has not got.</summary>
        private static MapChannel Map(uint mapContextId) => new MapChannel
        {
            MapInfo = new MapInfo(mapContextId, $"boss_titles_{mapContextId}", 1556, 0),
            ClientList = new List<Client>()
        };

        /// <summary>A living creature of a name on the map the player stands on.</summary>
        private static Creature InTheWorld(BootcampRuntimeTestHarness.Harness harness, uint nameId)
        {
            var map = harness.BootcampMap;
            var creature = new Creature
            {
                DbId = 990000 + nameId % 1000,
                NameId = nameId,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = map.MapInfo.MapContextId,
                Level = 1,
                State = CharacterState.Idle,
                Position = harness.Client.Player.Position,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool { MapContextId = map.MapInfo.MapContextId, AliveCreatures = 1, RespawnTime = 1000, UpdateTimer = 1000 }
            };

            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        /// <summary>The boss kills the database has for a character: the harness's own by default.</summary>
        private static uint[] Recorded(BootcampRuntimeTestHarness.Harness harness, uint? characterId = null)
        {
            using var unit = harness.Context.CreateChar();

            return unit.CharacterBossKills.Get(characterId ?? harness.Client.Player.Id).ToArray();
        }

        private static int IndexOf<T>(IReadOnlyList<PythonPacket> packets)
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }
    }
}
