using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Api.Ingame;

    [TestClass]
    public class CreatureEditorSnapshotTests
    {
        private static IngameCreatureDetails Example() => new()
        {
            Id = 3,
            Comment = "Bane Amoeboid",
            ClassId = 6032,
            ClassName = "Bane_Amoeboid_v1",
            MeshId = 42,
            ClassCollisionRole = 1,
            Augmentations = new List<string> { "Creature" },
            CreatureFlags = new List<string> { "BIOLOGICAL" },
            Faction = 2,
            Level = 4,
            MaxHitPoints = 100,
            NameId = 800,
            RunSpeed = 12,
            WalkSpeed = 6,
            Actions = new List<uint> { 1, 0, 0, 0, 0, 0, 0, 0 },
            RandomBodyHue = true,
            BodyHue = null,
            BodyHue2 = null,
            Appearance = new List<IngameCreatureAppearance>
            {
                new() { SlotId = 2, Slot = "Chest", ClassId = 500, ClassName = "Armor", MeshId = 23, Color = 123, Hue2 = 2139062144 }
            }
        };

        // Rust CreatureDetails/CreatureAppearance preserve the editable data, but
        // CreatureAppearance does not include the server's appearance.meshId.
        private static JsonDocument RustRoundTrip(IngameCreatureDetails details)
        {
            var json = JsonNode.Parse(ApiJson.Serialize(details)).AsObject();
            json.Remove("targetFlag");
            json.Remove("className");
            json.Remove("meshId");
            json.Remove("creatureFlags");
            json.Remove("augmentations");
            json["appearance"].AsArray()[0].AsObject().Remove("meshId");
            return JsonDocument.Parse(json.ToJsonString());
        }

        [TestMethod]
        public void RoundTrippedImGuiSnapshotDoesNotConflictWhenOnlyDisplayMetadataIsMissing()
        {
            var current = Example();
            using var rust = RustRoundTrip(current);
            using var complete = JsonDocument.Parse(ApiJson.Serialize(current));
            Assert.IsFalse(JsonElement.DeepEquals(rust.RootElement, complete.RootElement),
                "The previous full-document comparison would incorrectly return 409.");
            Assert.IsTrue(IngameCreatureUpdateEndpoint.TryReadExpectedSnapshot(rust.RootElement, out var expected, out var error), error);
            CollectionAssert.AreEquivalent(System.Array.Empty<string>(),
                IngameCreatureUpdateEndpoint.ChangedEditableFields(expected, current));

            current.CreatureFlags.Add("VULNERABLE_FIRE");
            current.Appearance[0].ClassName = "Renamed display label";
            current.Appearance[0].MeshId = 99;
            CollectionAssert.AreEquivalent(System.Array.Empty<string>(),
                IngameCreatureUpdateEndpoint.ChangedEditableFields(expected, current));
        }

        [TestMethod]
        public void GenuineEditsStillProduceAnActionableConflict()
        {
            var current = Example();
            using var rust = RustRoundTrip(current);
            Assert.IsTrue(IngameCreatureUpdateEndpoint.TryReadExpectedSnapshot(rust.RootElement, out var expected, out var error), error);

            current.Appearance[0].Color = 456;
            current.MaxHitPoints = 101;
            var changes = IngameCreatureUpdateEndpoint.ChangedEditableFields(expected, current);
            CollectionAssert.IsSubsetOf(new[] { "appearance slot 2", "maxHitPoints" }, changes);
        }

        [TestMethod]
        public void IncompleteConcurrencySnapshotIsRejectedInsteadOfAllowingBlindWrites()
        {
            var current = Example();
            using var rust = RustRoundTrip(current);
            var json = JsonNode.Parse(rust.RootElement.GetRawText()).AsObject();
            json.Remove("actions");
            using var broken = JsonDocument.Parse(json.ToJsonString());
            Assert.IsFalse(IngameCreatureUpdateEndpoint.TryReadExpectedSnapshot(broken.RootElement, out _, out var error));
            StringAssert.Contains(error, "expected.actions");
        }
    }
}
