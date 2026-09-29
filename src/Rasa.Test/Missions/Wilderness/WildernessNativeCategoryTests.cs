using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    public class WildernessNativeCategoryTests
    {
        [TestMethod]
        public void NativeWildernessCategoryIsNotTruncatedInContentOrPackets()
        {
            var definition = JsonSerializer.Deserialize<MissionContentDefinitionEntry>(
                "{\"CategoryId\":10000044}");
            Assert.AreEqual(10000044U, definition.CategoryId);
            var constants = JsonSerializer.Deserialize<MissionConstantData>(
                "{\"Level\":4,\"GroupType\":1,\"CategoryId\":10000044}");
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                constants.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(4U, reader.ReadUInt());
            Assert.AreEqual(1U, reader.ReadUInt());
            Assert.AreEqual(10000044U, reader.ReadUInt());
        }
    }
}
