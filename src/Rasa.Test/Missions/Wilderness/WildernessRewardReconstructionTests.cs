using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessRewardReconstructionTests
    {
        private const string WaveABoundary = "20260929090436_WildernessLandingZone";
        private static readonly Dictionary<(uint Mission, uint Item), (uint Before, uint After)> Corrections = new()
        {
            [(1390, 1)] = (111247, 44918),
            [(422, 1)] = (111247, 44918),
            [(432, 2)] = (111022, 44918),
            [(434, 2)] = (111247, 44918),
            [(436, 2)] = (111022, 44918),
            [(549, 1)] = (47593, 44917),
            [(549, 2)] = (45125, 44918),
            [(441, 1)] = (45125, 44918)
        };

        [TestMethod]
        public void ForwardRewardCorrectionChangesOnlyTheEightOwnedTemplateRows()
        {
            RewardRow[] original;
            string[] packages;
            using (var before = WildernessRuntimeTestHarness.Create(targetWorldMigration: WaveABoundary))
            {
                original = RewardRows(before);
                packages = RewardPackages(before);
            }
            var expected = original.Select(row =>
            {
                if (row.Revision != WildernessMissionDataV1.Revision || row.Reward != 1 ||
                    !Corrections.TryGetValue((row.Mission, row.Item), out var correction))
                    return row;
                Assert.AreEqual(correction.Before, row.Template);
                return row with { Template = correction.After };
            }).ToArray();
            Assert.AreEqual(8, original.Zip(expected).Count(pair => pair.First != pair.Second));

            using var after = WildernessRuntimeTestHarness.Create(
                WildernessSupportedRewardsV1.Up, targetWorldMigration: WaveABoundary);

            CollectionAssert.AreEqual(expected, RewardRows(after));
            CollectionAssert.AreEqual(packages, RewardPackages(after));
            var evidence = after.World.MissionEvidenceEntries.AsNoTracking()
                .Where(row => row.ContentRevision == WildernessMissionDataV1.Revision && row.EvidenceId == 100)
                .ToArray();
            CollectionAssert.AreEquivalent(Corrections.Keys.Select(key => key.Mission).Distinct().ToArray(),
                evidence.Select(row => row.MissionId).ToArray());
            Assert.IsTrue(evidence.All(row => row.OwnerKind == MissionEvidenceOwnerKind.Reward &&
                row.OwnerId == 1 && row.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                row.ReconstructionNote.Contains("no original-effect parity")));
        }

        [TestMethod]
        public void RewardCorrectionDownRestoresExactKeysWithoutRewritingOtherMedpacks()
        {
            RewardRow[] original;
            using (var before = WildernessRuntimeTestHarness.Create(targetWorldMigration: WaveABoundary))
                original = RewardRows(before);
            using var after = WildernessRuntimeTestHarness.Create(migration =>
            {
                WildernessSupportedRewardsV1.Up(migration);
                migration.InsertData("mission_reward_item",
                    new[]
                    {
                        "mission_id", "content_revision", "reward_id", "item_id",
                        "kind", "item_template_id", "quantity"
                    },
                    new object[]
                    {
                        422U, WildernessMissionDataV1.Revision, 1U, 3U,
                        (byte)MissionRewardItemKind.Fixed, 44918U, 7U
                    });
                WildernessSupportedRewardsV1.Down(migration);
            }, targetWorldMigration: WaveABoundary);
            var rows = RewardRows(after);
            CollectionAssert.AreEqual(original, rows.Where(row =>
                !(row.Mission == 422 && row.Revision == WildernessMissionDataV1.Revision &&
                  row.Reward == 1 && row.Item == 3)).ToArray());
            Assert.AreEqual(new RewardRow(422, WildernessMissionDataV1.Revision, 1, 3,
                MissionRewardItemKind.Fixed, 44918, 7), rows.Single(row =>
                row.Mission == 422 && row.Revision == WildernessMissionDataV1.Revision &&
                row.Reward == 1 && row.Item == 3));
            Assert.IsFalse(after.World.MissionEvidenceEntries.Any(row =>
                row.ContentRevision == WildernessMissionDataV1.Revision && row.EvidenceId == 100));
        }

        [TestMethod]
        public void LatestRewardsUseSupportedActionsAndPreserveTheCanonicalBundles()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = RewardRows(harness).Where(row => row.Revision == WildernessMissionDataV1.Revision).ToArray();
            var unsupported = new uint[] { 111247, 111227, 111022, 45125, 45441, 111136, 47593, 47594, 123023 };
            Assert.IsFalse(rows.Any(row => unsupported.Contains(row.Template)),
                "An enabled reward must not advertise an unimplemented effect or module.");
            foreach (var correction in Corrections)
                Assert.AreEqual(correction.Value.After, rows.Single(row => row.Mission == correction.Key.Mission &&
                    row.Reward == 1 && row.Item == correction.Key.Item).Template);
            foreach (var mission in new uint[] { 422, 434 })
                CollectionAssert.AreEqual(new[] { (44918U, 1U), (44918U, 1U) },
                    rows.Where(row => row.Mission == mission).Select(row => (row.Template, row.Quantity)).ToArray());
            foreach (var (mission, bundle) in new[]
            {
                (623U, new[] { (44919U, 1U) }),
                (791U, new[] { (44918U, 1U), (44917U, 1U) }),
                (758U, new[] { (44920U, 2U), (45047U, 4U) }),
                (787U, new[] { (44919U, 2U), (44920U, 6U) }),
                (769U, new[] { (44921U, 6U), (44920U, 2U) })
            })
                CollectionAssert.AreEqual(bundle,
                    rows.Where(row => row.Mission == mission).Select(row => (row.Template, row.Quantity)).ToArray(),
                    $"Mission {mission} must preserve its fixed quantities and row order.");

            var abilities = harness.LoadAbilities();
            foreach (var template in new uint[] { 44917, 44918, 44919, 44920, 44921, 45047 })
            {
                Assert.IsTrue(abilities.TryGetItemAction(template, out var action, out var level));
                Assert.AreEqual(419U, (uint)action);
                Assert.IsTrue(abilities.TryGetAction(action, out var definition));
                Assert.AreEqual("abilities.medpack", definition.Module);
                Assert.IsTrue(abilities.TryGetLevel(action, level, out var properties));
                Assert.IsTrue(properties.Get(Rasa.Data.AbilityProperty.HealAmountMin) > 0);
            }
        }

        [TestMethod]
        [DataRow(44917U)]
        [DataRow(44918U)]
        [DataRow(44919U)]
        [DataRow(44920U)]
        [DataRow(44921U)]
        [DataRow(45047U)]
        public void NativeRewardMedpacksHealOnceAndRejectReplayedRecovery(uint template)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(template, 2) }, unit));
                grant.Publish(harness.Client);
            }
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == template);
            var abilities = harness.LoadAbilities();
            Assert.IsTrue(abilities.TryGetItemAction(template, out var action, out var level));
            Assert.AreEqual(419U, (uint)action);
            harness.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 1000, 1000, 100, 0, 0);
            harness.Drain();

            abilities.RequestPerformAbility(harness.Client, MedpackRequest(item.EntityId, level + 1));

            Assert.AreEqual(2U, item.StackSize);
            Assert.IsFalse(harness.Map.PerformRecovery.Any(entry => entry.ActionId == action));
            Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
            Assert.AreEqual(100, harness.Client.Player.Attributes[Attributes.Health].Current);

            var request = MedpackRequest(item.EntityId, level);
            abilities.RequestPerformAbility(harness.Client, request);
            var recovery = harness.Map.PerformRecovery.Single(entry => entry.ActionId == action);
            Assert.AreEqual(item.EntityId, recovery.ItemId);
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                ActorActionManager.Instance.DoWork(harness.Map, Math.Max(1, recovery.WaitTime));
                Assert.AreEqual(1U, item.StackSize);
                Assert.IsFalse(harness.Map.PerformRecovery.Contains(recovery));
                var effect = harness.Client.Player.ActiveEffects.Values.Single(entry => entry.TypeId == 280);
                effect.NextTickTick = Environment.TickCount64 - 1;
                GameEffectManager.Instance.DoWork(harness.Map, 1000);
                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current > 100,
                    $"Template {template} must heal through the actual action419 effect tick.");
                Assert.IsTrue(harness.Drain().OfType<AbilityRecoveryPacket>().Any());
                var health = harness.Client.Player.Attributes[Attributes.Health].Current;
                var effects = harness.Client.Player.ActiveEffects.Keys.ToArray();

                abilities.RequestPerformAbility(harness.Client, request);
                Assert.IsFalse(harness.Map.PerformRecovery.Any(entry => entry.ActionId == action));
                Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
                abilities.PerformRecovery(harness.Map, recovery);

                Assert.AreEqual(1U, item.StackSize, "A stale recovery cannot consume the second unit.");
                Assert.AreEqual(health, harness.Client.Player.Attributes[Attributes.Health].Current);
                CollectionAssert.AreEquivalent(effects, harness.Client.Player.ActiveEffects.Keys.ToArray());
                Assert.IsFalse(harness.Drain().OfType<AbilityRecoveryPacket>().Any());
                using var verify = harness.CreateChar();
                Assert.AreEqual(1U, verify.Items.GetItem(item.Id).StackSize);
            }
            finally
            {
                GameEffectManager.Instance.ClearEffects(harness.Map, harness.Client.Player);
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        private static RequestPerformAbilityPacket MedpackRequest(ulong item, uint level)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt(419);
                writer.WriteInt(checked((int)level));
                writer.WriteNoneStruct();
                writer.WriteULong(item);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var request = new RequestPerformAbilityPacket();
            request.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return request;
        }

        private static RewardRow[] RewardRows(WildernessRuntimeTestHarness harness) =>
            harness.World.MissionRewardItemEntries.AsNoTracking().AsEnumerable()
                .OrderBy(row => row.MissionId).ThenBy(row => row.ContentRevision)
                .ThenBy(row => row.RewardId).ThenBy(row => row.ItemId)
                .Select(row => new RewardRow(row.MissionId, row.ContentRevision, row.RewardId, row.ItemId,
                    row.Kind, row.ItemTemplateId, row.Quantity)).ToArray();

        private static string[] RewardPackages(WildernessRuntimeTestHarness harness) =>
            harness.World.MissionRewardDefinitionEntries.AsNoTracking().AsEnumerable()
                .OrderBy(row => row.MissionId).ThenBy(row => row.ContentRevision).ThenBy(row => row.RewardId)
                .Select(row => $"{row.MissionId}/{row.ContentRevision}/{row.RewardId}:" +
                    $"{row.Experience}/{row.Credits}/{row.Prestige}/{row.SelectionCount}").ToArray();

        private sealed record RewardRow(uint Mission, string Revision, uint Reward, uint Item,
            MissionRewardItemKind Kind, uint Template, uint Quantity);
    }
}
