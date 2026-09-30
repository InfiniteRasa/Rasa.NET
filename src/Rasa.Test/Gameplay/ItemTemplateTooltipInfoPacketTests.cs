using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Data;
    using Rasa.Memory;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    [TestClass]
    public class ItemTemplateTooltipInfoPacketTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Rasa.Logger.Config == null)
                Rasa.Logger.UpdateConfig(new Rasa.Logger.LoggerConfig());
        }

        [TestMethod]
        public void WeaponAugmentationWithoutWeaponInfoDoesNotThrow()
        {
            var entityClass = new EntityClass(29365, "arch_hum_practice_target_v01", 0, 0, new List<AugmentationType> { AugmentationType.Weapon }, false)
            {
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
                {
                    Id = 1,
                    AmmoClassId = 0,
                    ClipSize = 1,
                    MinDamage = 1,
                    MaxDamage = 2,
                    DamageType = 0,
                })
            };

            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 17131, ItemClass = 29365 });
            Assert.IsNull(itemTemplate.WeaponInfo);

            var packet = new ItemTemplateTooltipInfoPacket(itemTemplate, entityClass);

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);

            Assert.IsTrue(stream.Length > 0);
        }

        // ItemTemplateTooltipInfoPacket.Written: the dictionary's count goes out before its
        // entries, so an augmentation the tooltip has no entry for (a Recipe's, a Shrine's) must
        // leave the packet exactly as if the class did not have it.
        [TestMethod]
        [DataRow(AugmentationType.Recipe)]
        [DataRow(AugmentationType.Shrine)]
        public void UnsupportedAugmentationsAreLeftOutOfTheCount(AugmentationType unsupported)
        {
            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 90001, ItemClass = 90001 });

            var without = Write(itemTemplate, AugmentationType.Customization);
            var with = Write(itemTemplate, unsupported, AugmentationType.Customization, unsupported);

            CollectionAssert.AreEqual(without, with);
        }

        private static byte[] Write(ItemTemplate itemTemplate, params AugmentationType[] augmentations)
        {
            var entityClass = new EntityClass(90001, "fixture", 0, 0, new List<AugmentationType>(augmentations), false);
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                new ItemTemplateTooltipInfoPacket(itemTemplate, entityClass).Write(writer);
            return stream.ToArray();
        }
    }
}
