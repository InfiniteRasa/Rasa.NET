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

        // What the client does with the entry (gameuiutil.py, tooltipwindow.py): it unpacks the
        // skill requirement as a pair, "(skillId, skillLvl) = skillInfo", and walks the
        // resistances, "for (damageType, amount) in resistInfo", for the inventory icon and the
        // tooltip alike. None for either is a TypeError, so an equipable with no skill
        // requirement row - the Space Helmet (122853), the Snowball Launcher (131482) - logged one
        // on every icon redraw and had no tooltip.
        [TestMethod]
        public void AnEquipableWithNoRequirementIsSentAPairAndAnEmptyList()
        {
            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 122853, ItemClass = 90001 });
            Assert.IsNull(itemTemplate.EquipableInfo);

            using var stream = new MemoryStream(Write(itemTemplate, AugmentationType.Equipable));
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(122853u, reader.ReadUInt());
            Assert.AreEqual(90001u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Equipable, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple(), "the skill requirement is a pair");
            reader.ReadNoneStruct();
            reader.ReadNoneStruct();
            Assert.AreEqual(0, reader.ReadList(), "the resistances are a list");
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void AnEquipableWithARequirementIsSentItAndItsResistances()
        {
            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 90002, ItemClass = 90001 })
            {
                EquipableInfo = new EquipableInfo(19, 2)
            };
            itemTemplate.EquipableInfo.ResistList.Add(new ResistanceData(DamageType.Fire, 12));

            using var stream = new MemoryStream(Write(itemTemplate, AugmentationType.Equipable));
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            reader.ReadUInt();
            reader.ReadUInt();
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Equipable, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(19, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual((int)DamageType.Fire, reader.ReadInt());
            Assert.AreEqual(12, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        // The Snowball Launcher: a weapon class (30548) with a clip and no ammo class, and no
        // itemtemplate_weapon row. The tooltip reads the alt fire with len() (GetWeaponAltFireInfo),
        // leaves the ammo line out for an ammo class of None and the range line out for a range of
        // None (_AddWeaponAmmo, _AddWeaponRange).
        [TestMethod]
        public void AWeaponWithNoWeaponRowIsSentWhatTheTooltipCanRead()
        {
            var entityClass = new EntityClass(30548, "AccountReward_Weapon_Avatar_Snowball_Launcher", 0, 0, new List<AugmentationType> { AugmentationType.Weapon }, false)
            {
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { Id = 30548, AmmoClassId = 0, ClipSize = 8, MinDamage = 0, MaxDamage = 0, DamageType = 3 })
            };
            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 131482, ItemClass = 30548 });
            Assert.IsNull(itemTemplate.WeaponInfo);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                new ItemTemplateTooltipInfoPacket(itemTemplate, entityClass).Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(131482u, reader.ReadUInt());
            Assert.AreEqual(30548u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Weapon, reader.ReadInt());
            Assert.AreEqual(16, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadInt());           // min damage
            Assert.AreEqual(0, reader.ReadInt());           // max damage
            reader.ReadNoneStruct();                        // no ammo class
            Assert.AreEqual(8u, reader.ReadUInt());         // clip size
            Assert.AreEqual(0u, reader.ReadUInt());         // ammo per shot
            Assert.AreEqual(3, reader.ReadInt());           // damage type: ice
            for (var time = 0; time < 4; time++)
                Assert.AreEqual(0u, reader.ReadUInt());     // windup, recovery, refire, reload
            reader.ReadNoneStruct();                        // range: not known
            Assert.AreEqual(0u, reader.ReadUInt());         // AE radius
            reader.ReadNoneStruct();                        // AE type
            Assert.AreEqual(0, reader.ReadTuple(), "no alt fire is an empty tuple, which has a len()");
            Assert.AreEqual(0, reader.ReadInt());           // attack type
            Assert.AreEqual(0, reader.ReadInt());           // tool type
            Assert.AreEqual(stream.Length, stream.Position);
        }

        // An item's module ids are a list the client loops over (HandleReceiveItemInfo); there are
        // none yet, and that is an empty list rather than None.
        [TestMethod]
        public void AnItemsModuleIdsAreAnEmptyList()
        {
            var entityClass = new EntityClass(90001, "fixture", 0, 0, new List<AugmentationType> { AugmentationType.Item }, false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { Id = 90001, MaxHitPoints = 200, StackSize = 1 })
            };
            var itemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 90003, ItemClass = 90001 });

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                new ItemTemplateTooltipInfoPacket(itemTemplate, entityClass).Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            reader.ReadUInt();
            reader.ReadUInt();
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Item, reader.ReadInt());
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadBool();                              // tradable
            Assert.AreEqual(200, reader.ReadInt());         // max hit points
            Assert.AreEqual(0, reader.ReadInt());           // buyback price
            Assert.AreEqual(0, reader.ReadList());          // requirements
            Assert.AreEqual(0, reader.ReadList(), "module ids");
            reader.ReadNoneStruct();                        // race ids: none
            Assert.AreEqual(stream.Length, stream.Position);
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
