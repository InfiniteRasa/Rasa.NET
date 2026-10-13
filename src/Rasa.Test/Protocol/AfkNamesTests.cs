using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Protocol
{
    using Rasa.Packets.Party.Client;

    [TestClass]
    public class AfkNamesTests
    {
        [TestMethod]
        public void TheClientsAfkDecorationComesOffInEveryLanguage()
        {
            Assert.AreEqual("Kupper", AfkNames.Strip("Kupper(AFK)"), "English and German, appended with nothing between");
            Assert.AreEqual("Kupper", AfkNames.Strip("Kupper(Absent)"), "French");
            Assert.AreEqual("Kupper", AfkNames.Strip("Kupper(離席中)"), "Japanese");
            Assert.AreEqual("Kupper", AfkNames.Strip("Kupper (AFK) "), "with the whitespace some paths add");
        }

        [TestMethod]
        public void AnUndecoratedNameIsLeftAlone()
        {
            Assert.AreEqual("Kupper", AfkNames.Strip("Kupper"));
            Assert.AreEqual("Kupper", AfkNames.Strip(" Kupper "));
            Assert.AreEqual("", AfkNames.Strip(""));
            Assert.IsNull(AfkNames.Strip(null));
        }
    }
}
