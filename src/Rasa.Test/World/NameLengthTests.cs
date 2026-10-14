using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Client;

    /// <summary>
    /// A name is 2 to 20 letters. The minimum was 3, and the random names the character creation
    /// screen hands out include An, Ed, Li, My and Xu: offered, then refused.
    /// </summary>
    [TestClass]
    public class NameLengthTests
    {
        [TestMethod]
        [DataRow("An", true)]
        [DataRow("Ed", true)]
        [DataRow("Li", true)]
        [DataRow("My", true)]
        [DataRow("Xu", true)]
        [DataRow("Abcdefghijklmnopqrst", true)]
        [DataRow("E", false)]
        [DataRow("", false)]
        [DataRow("Abcdefghijklmnopqrstu", false)]
        public void ANameIsTwoToTwentyLetters(string name, bool valid)
        {
            Assert.AreEqual(valid, CharacterManager.IsValidName(name, out var error));

            if (!valid)
                Assert.AreEqual(name.Length < CharacterManager.MinNameLength ? PlayerMessage.PmNameTooShort : PlayerMessage.PmNameTooLong, error);
        }

        [TestMethod]
        [DataRow("Ed", "Xu", CreateCharacterResult.Success)]
        [DataRow("E", "Xu", CreateCharacterResult.NameTooShort)]
        [DataRow("Ed", "X", CreateCharacterResult.NameTooShort)]
        [DataRow("Abcdefghijklmnopqrstu", "Xu", CreateCharacterResult.NameTooLong)]
        public void CreationTakesTheSameLengths(string first, string family, CreateCharacterResult result)
        {
            var create = new RequestCreateCharacterInSlotPacket
            {
                CharacterName = first, FamilyName = family, Gender = 0, Scale = 1.0, RaceId = Race.Human
            };

            Assert.AreEqual(result, create.Validate());
        }

        [TestMethod]
        [DataRow("Ed", CreateCharacterResult.Success)]
        [DataRow("E", CreateCharacterResult.NameTooShort)]
        [DataRow("Abcdefghijklmnopqrstu", CreateCharacterResult.NameTooLong)]
        public void ACloneTakesTheSameLengths(string first, CreateCharacterResult result)
        {
            var clone = new RequestCloneCharacterToSlotPacket
            {
                CharacterName = first, Gender = 0, Scale = 1.0, RaceId = Race.Human
            };

            Assert.AreEqual(result, clone.Validate());
        }
    }
}
