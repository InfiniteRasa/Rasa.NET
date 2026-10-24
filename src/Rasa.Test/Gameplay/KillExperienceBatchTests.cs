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
    using Rasa.Missions;
    using Rasa.Missions.Definitions;
    using Rasa.Packets;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A kill's experience is saved for everyone sharing it in one transaction
    /// (ManifestationManager.GainExperience over a list), and what each of them is paid, saved
    /// and told is what paying them one at a time did.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KillExperienceBatchTests
    {
        private Func<Client, Vector3, List<Client>> _sharers;

        [TestInitialize]
        public void KeepTheSquad() => _sharers = KillShares.SharersOf;

        [TestCleanup]
        public void RestoreTheSquad() => KillShares.SharersOf = _sharers;

        /// <summary>Where each of the squad starts: one near level 5 (a clone credit), one near 2, one at 4, two fresh.</summary>
        private static readonly (byte Level, uint Experience)[] Starts =
        {
            (4, 42000), (1, 950), (4, 30000), (1, 0), (1, 0)
        };

        [TestMethod]
        public void TheWholeSquadIsSavedInOneWrite()
        {
            using var context = Start();
            var clients = Squad(context, 5);
            var manifestations = new ManifestationManager(context);
            KillShares.SharersOf = (killer, corpse) => clients.Skip(1).ToList();
            var saves = context.SaveAttempts;
            context.ResetCharUnitCount();

            KillShares.AwardExperience(manifestations, KillShares.SharersFor(context.Client, Vector3.Zero), context.Client, 5000);

            Assert.AreEqual(1, context.CharUnitsCreated, "one unit of work for the kill");
            Assert.AreEqual(saves + 1, context.SaveAttempts, "and one save");
            foreach (var client in clients)
            {
                Assert.IsTrue(client.Player.Experience > Starts[clients.IndexOf(client)].Experience, $"character {client.Player.Id} paid");
                Assert.AreEqual(client.Player.Experience, Saved(context, client).Experience, $"character {client.Player.Id} saved");
                Assert.AreEqual(client.Player.Level, Saved(context, client).Level);
                Assert.AreEqual(client.Player.CloneCredits, Saved(context, client).CloneCredits);
            }
        }

        /// <summary>
        /// A sequence of kills, crit kills among them, paid to a squad of five through the batch,
        /// against the same paid to an identical squad one player at a time: the same experience,
        /// levels and clone credits, in memory and saved, and the same lines to each client.
        /// </summary>
        [TestMethod]
        public void ASquadIsPaidWhatItWasPaidOneAtATime()
        {
            var kills = new (uint Experience, CritKill Crit)[]
            {
                (5000, CritKill.None), (800, CritKill.Own), (12000, CritKill.None), (1, CritKill.None), (40000, CritKill.Own)
            };

            using var batched = Start();
            using var single = Start();
            var batchedSquad = Squad(batched, 5);
            var singleSquad = Squad(single, 5);
            var batchedPackets = batchedSquad.ToDictionary(client => client.Player.Id, _ => new List<string>());
            var singlePackets = singleSquad.ToDictionary(client => client.Player.Id, _ => new List<string>());
            var batchedManager = new ManifestationManager(batched);
            var singleManager = new ManifestationManager(single);

            foreach (var kill in kills)
            {
                KillShares.SharersOf = (killer, corpse) => batchedSquad.Skip(1).ToList();
                KillShares.AwardExperience(batchedManager, KillShares.SharersFor(batched.Client, Vector3.Zero),
                    batched.Client, kill.Experience, kill.Crit);

                // The same parts, the old way: each sharer paid by a GainExperience of their own.
                var paid = singleSquad.Where(KillShares.GainsExperience).ToList();
                var share = KillShares.GroupPartOf(kill.Experience, paid.Count);
                foreach (var sharer in paid)
                {
                    var streak = KillStreaks.MultiplierOf(sharer);
                    var boost = ExperienceBoost.PercentOf(sharer.Player);
                    var part = ExperienceBoost.Boosted(sharer.Player, ItemModuleBonuses.WithExperience(sharer.Player, share));
                    singleManager.GainExperience(sharer, (uint)Math.Min(uint.MaxValue, (ulong)part * (ulong)streak),
                        sharer == single.Client || kill.Crit == CritKill.None ? kill.Crit : CritKill.Team, streak,
                        KillShares.GroupModOf(paid.Count), (100.0 + boost) / 100.0, KillShares.ShareOf(kill.Experience, paid.Count));
                }

                foreach (var client in batchedSquad)
                    batchedPackets[client.Player.Id].AddRange(Lines(client));
                foreach (var client in singleSquad)
                    singlePackets[client.Player.Id].AddRange(Lines(client));
            }

            for (var i = 0; i < batchedSquad.Count; i++)
            {
                var a = batchedSquad[i].Player;
                var b = singleSquad[i].Player;
                Assert.AreEqual(b.Experience, a.Experience, $"character {a.Id}'s experience");
                Assert.AreEqual(b.Level, a.Level, $"character {a.Id}'s level");
                Assert.AreEqual(b.CloneCredits, a.CloneCredits, $"character {a.Id}'s clone credits");

                var savedA = Saved(batched, batchedSquad[i]);
                var savedB = Saved(single, singleSquad[i]);
                Assert.AreEqual(savedB.Experience, savedA.Experience);
                Assert.AreEqual(savedB.Level, savedA.Level);
                Assert.AreEqual(savedB.CloneCredits, savedA.CloneCredits);
                Assert.AreEqual(a.Experience, savedA.Experience);

                for (var line = 0; line < Math.Max(singlePackets[b.Id].Count, batchedPackets[a.Id].Count); line++)
                    Assert.AreEqual(singlePackets[b.Id].ElementAtOrDefault(line), batchedPackets[a.Id].ElementAtOrDefault(line), $"what character {a.Id}'s client was told, line {line}: {singlePackets[b.Id].ElementAtOrDefault(line)} / {batchedPackets[a.Id].ElementAtOrDefault(line)}");
            }

            Assert.IsTrue(batchedSquad[0].Player.Level >= 5, "the sequence reaches a clone credit level");
            Assert.IsTrue(batchedPackets[1].Any(line => line.StartsWith(nameof(CloneCreditsPacket))));
        }

        [TestMethod]
        public void ARowChangedUnderneathIsRefusedWithoutCostingTheOthers()
        {
            using var context = Start();
            var clients = Squad(context, 3);
            var manifestations = new ManifestationManager(context);
            KillShares.SharersOf = (killer, corpse) => clients.Skip(1).ToList();

            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 2).Experience = 999;
                database.SaveChanges();
            }

            KillShares.AwardExperience(manifestations, KillShares.SharersFor(context.Client, Vector3.Zero), context.Client, 5000);

            Assert.AreEqual(Starts[1].Experience, clients[1].Player.Experience, "refused in memory");
            Assert.AreEqual(999u, Saved(context, clients[1]).Experience, "and the row left as it was");
            Assert.AreEqual(0, MissionTestContext.Drain(clients[1]).OfType<ExperienceChangedPacket>().Count(), "and nothing said");

            foreach (var paid in new[] { clients[0], clients[2] })
            {
                Assert.IsTrue(paid.Player.Experience > Starts[clients.IndexOf(paid)].Experience);
                Assert.AreEqual(paid.Player.Experience, Saved(context, paid).Experience);
                Assert.AreEqual(1, MissionTestContext.Drain(paid).OfType<ExperienceChangedPacket>().Count());
            }
        }

        [TestMethod]
        public void ASaveThatFailsPaysNobody()
        {
            using var context = Start();
            var clients = Squad(context, 3);
            var manifestations = new ManifestationManager(context);
            KillShares.SharersOf = (killer, corpse) => clients.Skip(1).ToList();
            context.BeforeSave = _ => throw new DbUpdateException("Injected experience failure.");

            KillShares.AwardExperience(manifestations, KillShares.SharersFor(context.Client, Vector3.Zero), context.Client, 5000);

            context.BeforeSave = null;
            for (var i = 0; i < clients.Count; i++)
            {
                Assert.AreEqual(Starts[i].Experience, clients[i].Player.Experience);
                Assert.AreEqual(Starts[i].Level, clients[i].Player.Level);
                Assert.AreEqual(Starts[i].Experience, Saved(context, clients[i]).Experience);
                Assert.AreEqual(0, MissionTestContext.Drain(clients[i]).Count);
            }
        }

        [TestMethod]
        public void APlayerTwiceInOneBatchIsPaidTwice()
        {
            using var context = Start();
            var clients = Squad(context, 2);
            var manifestations = new ManifestationManager(context);

            manifestations.GainExperience(new[]
            {
                new ManifestationManager.ExperienceAward { Client = clients[1], Experience = 10 },
                new ManifestationManager.ExperienceAward { Client = clients[0], Experience = 10 },
                new ManifestationManager.ExperienceAward { Client = clients[1], Experience = 20 }
            });

            Assert.AreEqual(Starts[1].Experience + 30, clients[1].Player.Experience);
            Assert.AreEqual(clients[1].Player.Experience, Saved(context, clients[1]).Experience);
            Assert.AreEqual(Starts[0].Experience + 10, clients[0].Player.Experience);
            Assert.AreEqual(2, MissionTestContext.Drain(clients[1]).OfType<ExperienceChangedPacket>().Count());
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        /// <summary>The context's player and the others, each at their start (Starts), saved so, with stats a level-up can work out.</summary>
        private static List<Client> Squad(MissionTestContext context, int size)
        {
            var clients = new List<Client> { context.Client };

            for (var i = 2; i <= size; i++)
                clients.Add(context.CreateAdditionalClient((uint)i));

            using (var database = context.Open())
            {
                for (var i = 0; i < clients.Count; i++)
                {
                    var player = clients[i].Player;
                    player.Level = Starts[i].Level;
                    player.Experience = Starts[i].Experience;
                    player.CloneCredits = 0;
                    player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                        attribute => attribute,
                        attribute => new ActorAttributes(attribute, 100, 100, 100, 0, 0));

                    var row = database.CharacterEntries.Single(entry => entry.Id == player.Id);
                    row.Level = player.Level;
                    row.Experience = player.Experience;
                    row.CloneCredits = 0;
                }

                database.SaveChanges();
            }

            foreach (var client in clients)
                MissionTestContext.Drain(client);

            return clients;
        }

        private static Rasa.Structures.Char.CharacterEntry Saved(MissionTestContext context, Client client)
        {
            using var database = context.Open();
            return database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == client.Player.Id);
        }

        /// <summary>
        /// What the client was sent since last asked, each packet by its type and encoding - but
        /// a chat channel joined by its type alone: its id is the communicator's numbering, which
        /// goes on from one test world to the next.
        /// </summary>
        private static List<string> Lines(Client client) =>
            MissionTestContext.Drain(client)
                .Select(packet => packet is Rasa.Packets.Communicator.Server.ChatChannelJoinedPacket
                    ? packet.GetType().Name
                    : packet.GetType().Name + ":" + Convert.ToHexString(MissionTestContext.Encode(packet)))
                .ToList();
    }
}
