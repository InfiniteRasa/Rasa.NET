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
    }
}
