using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class OptionsSaveTests
    {
        private const UserOption ArmAbility1 = (UserOption)6;     // User.Input.MappedCommands.ArmAbility1
        private const UserOption ArmAbility2 = (UserOption)7;     // User.Input.MappedCommands.ArmAbility2

        [TestMethod]
        public void AnUnboundKeyIsAnEmptyValueNotANull()
        {
            var packet = ReadUserOptions((6, "keyboard.q"), (7, ""));

            Assert.AreEqual("keyboard.q", packet.OptionsList[0].Value);
            Assert.AreEqual("", packet.OptionsList[1].Value);
        }

        [TestMethod]
        public void SavingAnUnboundKeyStoresItAsEmpty()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            manager.SaveUserOptions(context.Client, ReadUserOptions((6, "keyboard.q"), (7, "")));

            var saved = Saved(context);
            Assert.AreEqual("keyboard.q", saved[6]);
            Assert.AreEqual("", saved[7]);
            Assert.AreEqual("", context.Client.UserOptions.Single(o => o.OptionId == ArmAbility2).Value);
        }

        [TestMethod]
        public void ABindingPutBackToItsDefaultIsNoLongerSaved()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            manager.SaveUserOptions(context.Client, ReadUserOptions((6, "keyboard.q"), (7, "keyboard.e")));
            manager.SaveUserOptions(context.Client, ReadUserOptions((7, "keyboard.e")));

            var saved = Saved(context);
            Assert.IsFalse(saved.ContainsKey(6), "ArmAbility1 is back to its default: the client leaves it out");
            Assert.AreEqual("keyboard.e", saved[7]);
            Assert.IsFalse(context.Client.UserOptions.Any(o => o.OptionId == ArmAbility1));
        }

        [TestMethod]
        public void EveryCharacterOptionBackToItsDefaultClearsThem()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            manager.SaveCharacterOptions(context.Client, ReadCharacterOptions((1, "1")));
            using (var unit = context.CreateChar())
                Assert.HasCount(1, unit.CharacterOptions.Get(context.Client.Player.Id));

            manager.SaveCharacterOptions(context.Client, ReadCharacterOptions());

            using (var unit = context.CreateChar())
                Assert.HasCount(0, unit.CharacterOptions.Get(context.Client.Player.Id));
            Assert.HasCount(0, context.Client.Player.CharacterOptions);
        }

        private static Dictionary<uint, string> Saved(WeaponAmmoContext context)
        {
            using var unit = context.CreateChar();
            return unit.UserOptions.Get(context.Client.AccountEntry.Id).ToDictionary(e => e.OptionId, e => e.Value);
        }

        private static SaveUserOptionsPacket ReadUserOptions(params (uint Id, string Value)[] options)
        {
            var packet = new SaveUserOptionsPacket();
            packet.Read(Reader(options));
            return packet;
        }

        private static SaveCharacterOptionsPacket ReadCharacterOptions(params (uint Id, string Value)[] options)
        {
            var packet = new SaveCharacterOptionsPacket();
            packet.Read(Reader(options));
            return packet;
        }

        /// <summary>The client's (optionsToSave,): a list of (optionId, unicode value), an empty value written as the client writes u''.</summary>
        private static BinaryReader Reader((uint Id, string Value)[] options)
        {
            var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                writer.WriteList(options.Length);
                foreach (var (id, value) in options)
                {
                    writer.WriteTuple(2);
                    writer.WriteUInt(id);
                    writer.WriteUnicodeString(value == "" ? null : value);     // u'' is 0x50 on the wire
                }
            }
            stream.Position = 0;
            return new BinaryReader(stream);
        }
    }
}
