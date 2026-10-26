using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// A mission's object can be called what its scene says (SceneActorDefinition.NameOverrideId):
    /// the client names a usable by UsableInfo's nameOverrideId from usablenameoverridelanguage
    /// (usable.py GetName) in place of its class's name, and its 342 names are nearly all for
    /// mission objects.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SceneObjectNameTests
    {
        private const string CrateRole = "bootcamp-equipment-crate";
        private const uint CrateClass = 29877;

        /// <summary>RETREAD_CAVE_AFS_SUPPLY_CRATE, "AFS Supply Crate": a name the client has, for the test.</summary>
        private const uint AfsSupplyCrate = 3;

        /// <summary>
        /// The bootcamp experience's scene puts the equipment crate down when the instance is made;
        /// named there, the crate is made with the name and its UsableInfo carries it. With no
        /// name it goes as None, which GetName reads as the class's own.
        /// </summary>
        [TestMethod]
        public void AnObjectItsSceneNamesIsSentWithItsNameAndAnUnnamedOneWithNone()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            var unnamed = Crate(harness);
            Assert.AreEqual(0u, unnamed.NameOverrideId, "no name: the class's own");
            Assert.IsNull(Rasa.Test.World.HitResultTests.Decode(Introduced(harness, unnamed))[2], "sent as None");

            // Named in the content, read again, and an instance made anew from it.
            var row = harness.WorldContext.Set<Rasa.Structures.World.MissionExperienceBindingEntry>().Single(entry => entry.ExperienceKey == "bootcamp");
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(row.Bindings, MissionContentCodec.Options);
            experience.Scene.Actors[CrateRole] = experience.Scene.Actors[CrateRole] with { NameOverrideId = AfsSupplyCrate };
            row.Bindings = JsonSerializer.Serialize(experience, MissionContentCodec.Options);
            harness.WorldContext.SaveChanges();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            harness.Reconnect();

            var named = Crate(harness);
            Assert.AreEqual(AfsSupplyCrate, named.NameOverrideId);
            var info = Introduced(harness, named);
            Assert.AreEqual(AfsSupplyCrate, info.NameOverrideId);
            Assert.AreEqual((long)AfsSupplyCrate, Rasa.Test.World.HitResultTests.Decode(info)[2], "written as the id: (enabled, state, 3, ...)");
        }

        private static DynamicObject Crate(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.DynamicObjects.Single(obj => (uint)obj.EntityClassId == CrateClass);

        private static UsableInfoPacket Introduced(BootcampRuntimeTestHarness.Harness harness, DynamicObject crate)
        {
            harness.Drain();
            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, crate);
            return harness.Drain().OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == crate.EntityId)
                .EntityData.OfType<UsableInfoPacket>().Single();
        }

        [TestMethod]
        public void TheNameIsKeptInTheScenesJsonAndLeftOutWhenThereIsNone()
        {
            var actor = new SceneActorDefinition("crate", SceneActorKind.Object, CrateClass, new ScenePosition(1, 2, 3));

            var plain = JsonSerializer.Serialize(actor, MissionContentCodec.Options);
            StringAssert.DoesNotMatch(plain, new System.Text.RegularExpressions.Regex("nameOverrideId"), "no name, no field: bindings written before stay as they were");
            Assert.AreEqual(0u, JsonSerializer.Deserialize<SceneActorDefinition>(plain, MissionContentCodec.Options).NameOverrideId);

            var named = JsonSerializer.Serialize(actor with { NameOverrideId = AfsSupplyCrate }, MissionContentCodec.Options);
            StringAssert.Contains(named, "\"nameOverrideId\":3");
            Assert.AreEqual(AfsSupplyCrate, JsonSerializer.Deserialize<SceneActorDefinition>(named, MissionContentCodec.Options).NameOverrideId);
        }
    }
}
