using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// The customization items that change the player's own looks - hair colour, skin colour,
    /// hairstyle and face - and the choices a customization item's window is opened with
    /// (Managers.Customization). Armour paint is ArmorPaintTests'.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AppearanceChangeTests
    {
        // The items, as `.giveitem` gives them.
        private const uint HairstyleItem = 26;          // Hairstyle Modification, class 1068
        private const uint FaceItem = 27;               // Face Modification, 1069
        private const uint SkinCaucasianItem = 47;      // Melanotan for Caucasians, 3734
        private const uint HairColourItem = 48;         // Hair Color Modification 3, 3735
        private const uint SkinAfricanItem = 100;       // Melanotan for Africans, 4086
        private const uint SkinAsianItem = 101;         // Skin Color Modification, 4087

        private const uint HairstyleClass = 1068;
        private const uint FaceClass = 1069;
        private const uint HairColourClass = 3735;
        private const uint SkinCaucasianClass = 3734;
        private const uint SkinAsianClass = 4087;

        // Hair and faces, by the item template the windows name them with.
        private const uint StyleATemplate = 42;
        private const uint StyleA = 3672;
        private const uint StyleCTemplate = 1775;
        private const uint StyleC = 9355;
        private const uint BaldTemplate = 60;
        private const uint Bald = 3812;
        private const uint CaucasianV1Template = 61;
        private const uint CaucasianV1 = 3813;
        private const uint CaucasianLongV1Template = 49676;
        private const uint CaucasianLongV1 = 24008;

        private static readonly Color Brown = new Color(82, 52, 32);
        private static readonly Color Tan = new Color(214, 178, 132);

        /// <summary>A human with Style A hair and the first Caucasian face, in the creation window's colours.</summary>
        private static BootcampRuntimeTestHarness.Harness Start(Race race = Race.Human)
        {
            var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;

            foreach (var choice in Customizations.Choices.Values.SelectMany(choices => choices))
                LoadHead(harness, choice.TemplateId);

            player.Race = race;
            player.AppearanceData[EquipmentData.Hair] = new AppearanceData { SlotId = EquipmentData.Hair, Class = StyleA, Color = new Color(Brown.Hue), Hue2 = new Color(0) };
            player.AppearanceData[EquipmentData.Face] = new AppearanceData { SlotId = EquipmentData.Face, Class = CaucasianV1, Color = new Color(Tan.Hue), Hue2 = new Color(0) };
            harness.Drain();
            return harness;
        }

        /// <summary>A hair or a face of the world, with its race requirement, as ItemManager.LoadItemTemplates has it.</summary>
        private static void LoadHead(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            var world = harness.WorldContext;
            var link = world.Set<ItemTemplateItemClassEntry>().AsNoTracking().Single(row => row.ItemTemplateId == template);
            var entry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass);
            var race = world.Set<ItemTemplateRequirementRaceEntry>().AsNoTracking().SingleOrDefault(row => row.Id == template)?.RaceId ?? 0;
            var entityClass = new EntityClass(entry.Id, entry.ClassName, entry.MeshId, entry.ClassCollisionRole,
                entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0)
            {
                ItemTemplates = new Dictionary<uint, ItemTemplate>()
            };

            entityClass.ItemTemplates[template] = new ItemTemplate(link) { ItemInfo = new ItemInfo(true, 0, new List<int>(), race) };
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.Id] = entityClass;
            ItemManager.Instance.ItemTemplateItemClass[template] = (EntityClasses)entry.Id;
        }

        private static RequestCustomizationPacket Request(uint argId, Item item, Color hue = null, int? template = null) =>
            new RequestCustomizationPacket
            {
                CustomizationActionArgId = (int)argId,
                CustomizationEntityId = item.EntityId,
                SelectedClassTemplateId = template,
                Hue = hue
            };

        private static (uint Class, uint Colour) Shown(BootcampRuntimeTestHarness.Harness harness, EquipmentData slot)
        {
            var shown = harness.Client.Player.AppearanceData[slot];

            return (shown.Class, shown.Color.Hue);
        }

        private static (uint Class, uint Colour) Saved(BootcampRuntimeTestHarness.Harness harness, EquipmentData slot)
        {
            using var unit = harness.Context.CreateChar();
            var row = unit.CharacterAppearances.GetByCharacterId(harness.Client.Player.Id).Single(entry => entry.Slot == (uint)slot);

            return (row.Class, row.Color);
        }

        /// <summary>The request is carried out: the action ends, one of the item goes - the last of a stack from the pack - and everyone is shown.</summary>
        private static AppearanceDataPacket AssertDone(BootcampRuntimeTestHarness.Harness harness, Item item, uint argId, uint left)
        {
            var sent = harness.Drain();
            var recovery = sent.OfType<PerformRecoveryPacket>().Single();

            Assert.AreEqual(ActionId.Customize, recovery.ActionId);
            Assert.AreEqual(argId, recovery.ActionArgId);
            Assert.IsFalse(sent.OfType<UserActionFailedPacket>().Any());

            if (left == 0)
                Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId), "the last one is gone from the pack");
            else
                Assert.AreEqual(left, item.StackSize, "one is used up");

            var shown = sent.OfType<AppearanceDataPacket>().Single();
            Assert.IsTrue(shown.OfPlayer);
            return shown;
        }

        private static void AssertRefused(BootcampRuntimeTestHarness.Harness harness, RequestCustomizationPacket request, Item item, PlayerMessage message)
        {
            var hair = Shown(harness, EquipmentData.Hair);
            var face = Shown(harness, EquipmentData.Face);
            var held = item.StackSize;
            harness.Drain();

            Customization.Request(harness.Client, request);

            Assert.AreEqual(hair, Shown(harness, EquipmentData.Hair));
            Assert.AreEqual(face, Shown(harness, EquipmentData.Face));
            Assert.AreEqual(held, item.StackSize, "the item is kept");

            var sent = harness.Drain();
            var failed = sent.OfType<UserActionFailedPacket>().Single();
            Assert.AreEqual(ActionId.Customize, failed.ActionId);
            Assert.AreEqual((uint)request.CustomizationActionArgId, failed.ActionArgId);
            Assert.AreEqual(message, failed.MsgId);
            Assert.AreEqual(1, sent.OfType<ActionFailedPacket>().Count());
            Assert.IsFalse(sent.OfType<PerformRecoveryPacket>().Any());
            Assert.IsFalse(sent.OfType<AppearanceDataPacket>().Any());
        }

        #region The window's choices

        [TestMethod]
        public void UsingAHairstyleOrAFaceItemListsWhatTheRaceMayHave()
        {
            using var harness = Start();
            var styles = ToyTests.Grant(harness, HairstyleItem);
            var faces = ToyTests.Grant(harness, FaceItem);
            harness.Drain();

            Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = styles.EntityId });

            var answer = harness.Drain().OfType<CustomizationChoicesPacket>().Single();
            Assert.AreEqual(styles.EntityId, answer.EntityId);
            CollectionAssert.AreEqual(
                new (uint, uint)[] { (StyleA, StyleATemplate), (3663, 36), (StyleC, StyleCTemplate), (9356, 1776), (9781, 1941), (9782, 1942), (9783, 1943), (9784, 1944), (Bald, BaldTemplate) },
                answer.Choices.ToArray(), "Style A to Style H and Bald, as the creation window lists them");

            Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = faces.EntityId });

            answer = harness.Drain().OfType<CustomizationChoicesPacket>().Single();
            Assert.HasCount(48, answer.Choices);
            Assert.AreEqual((20824u, 42276u), answer.Choices[0], "Asian v3 (Average), the creation window's first");
            CollectionAssert.Contains(answer.Choices.ToArray(), (CaucasianLongV1, CaucasianLongV1Template));

            // A hybrid: bald is all the hair there is for them, and none of these faces is theirs.
            harness.Client.Player.Race = Race.Forean;

            Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = styles.EntityId });
            CollectionAssert.AreEqual(new[] { (Bald, BaldTemplate) }, harness.Drain().OfType<CustomizationChoicesPacket>().Single().Choices.ToArray());

            Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = faces.EntityId });
            Assert.IsEmpty(harness.Drain().OfType<CustomizationChoicesPacket>().Single().Choices.ToArray(), "the window says there is nothing to customize");
        }

        [TestMethod]
        public void AColourItemsWindowIsOpenedWithNoChoicesAndWhatIsNotACustomizationIsNotAnswered()
        {
            using var harness = Start();
            var dye = ToyTests.Grant(harness, HairColourItem);
            var paint = ToyTests.Grant(harness, 111128);
            var snowball = ToyTests.Grant(harness, 131481);
            harness.Drain();

            foreach (var item in new[] { dye, paint })
            {
                Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = item.EntityId });

                var answer = harness.Drain().OfType<CustomizationChoicesPacket>().Single();
                Assert.AreEqual(item.EntityId, answer.EntityId);
                Assert.IsEmpty(answer.Choices.ToArray());
            }

            foreach (var entityId in new[] { snowball.EntityId, 0UL, ulong.MaxValue - 5, harness.Client.Player.EntityId })
            {
                Customization.Choices(harness.Client, new GetCustomizationChoicesPacket { EntityId = entityId });
                Assert.IsFalse(harness.Drain().OfType<CustomizationChoicesPacket>().Any(), $"{entityId}");
            }
        }

        [TestMethod]
        public void TheChoicesAreWrittenAsTheWindowReadsThem()
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            using (var writer = new PythonWriter(binary))
                new CustomizationChoicesPacket(77, new (uint, uint)[] { (StyleA, StyleATemplate), (Bald, BaldTemplate) }).Write(writer);

            stream.Position = 0;
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
            using var python = new PythonReader(reader);

            // (entityId, [(itemClassId, itemTemplateId), ...]): for (itemClassId, itemTemplateId) in choices.
            Assert.AreEqual(2, python.ReadTuple());
            Assert.AreEqual(77UL, python.ReadULong());
            Assert.AreEqual(2, python.ReadList());

            foreach (var (classId, templateId) in new[] { (StyleA, StyleATemplate), (Bald, BaldTemplate) })
            {
                Assert.AreEqual(2, python.ReadTuple());
                Assert.AreEqual((int)classId, python.ReadInt());
                Assert.AreEqual((int)templateId, python.ReadInt());
            }

            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void APlayersHairAndFaceGoOutAsPairsForTheWindowsThatUnpackTwo()
        {
            var appearance = new Dictionary<EquipmentData, AppearanceData>
            {
                [EquipmentData.Hair] = new AppearanceData { SlotId = EquipmentData.Hair, Class = StyleA, Color = new Color(Brown.Hue), Hue2 = new Color(0) },
                [EquipmentData.Torso] = new AppearanceData { SlotId = EquipmentData.Torso, Class = 15662, Color = new Color(Tan.Hue), Hue2 = new Color(Tan.Hue) },
                [EquipmentData.Face] = new AppearanceData { SlotId = EquipmentData.Face, Class = CaucasianV1, Color = new Color(Tan.Hue), Hue2 = new Color(0) }
            };

            // (classId, color) = appearanceData[HAIR], in customizationwindow.py and customization.py;
            // (classId, oldColor, oldColor2) = appearanceData[slot] for a worn piece, in the paint window.
            CollectionAssert.AreEqual(new[] { (EquipmentData.Hair, 2), (EquipmentData.Torso, 3), (EquipmentData.Face, 2) },
                Shapes(new AppearanceDataPacket(appearance, ofPlayer: true)));

            // A creature's, as it was.
            CollectionAssert.AreEqual(new[] { (EquipmentData.Hair, 3), (EquipmentData.Torso, 3), (EquipmentData.Face, 3) },
                Shapes(new AppearanceDataPacket(appearance)));

            static (EquipmentData, int)[] Shapes(AppearanceDataPacket packet)
            {
                using var stream = new MemoryStream();
                using (var binary = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                using (var writer = new PythonWriter(binary))
                    packet.Write(writer);

                stream.Position = 0;
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
                using var python = new PythonReader(reader);
                var shapes = new List<(EquipmentData, int)>();

                Assert.AreEqual(1, python.ReadTuple());
                var count = python.ReadDictionary();

                for (var i = 0; i < count; i++)
                {
                    var slot = (EquipmentData)python.ReadInt();
                    var values = python.ReadTuple();

                    Assert.AreNotEqual(0U, python.ReadUInt());

                    for (var value = 1; value < values; value++)
                        python.SkipValue();

                    shapes.Add((slot, values));
                }

                Assert.AreEqual(stream.Length, stream.Position);
                return shapes.ToArray();
            }
        }

        #endregion

        #region Colours

        [TestMethod]
        public void AHairColourOffThePaletteIsKeptAndShownAndTheStyleStays()
        {
            using var harness = Start();
            var dye = ToyTests.Grant(harness, HairColourItem, 3);
            var red = new Color(Customizations.Palettes[HairColourClass].Select(colour => new Color(colour)).First(colour => colour.Red > 150 && colour.Green < 60 && colour.Blue < 60).Hue);
            harness.Drain();

            Customization.Request(harness.Client, Request(Customization.HueHair, dye, red));

            var shown = AssertDone(harness, dye, Customization.HueHair, 2);
            Assert.AreEqual((StyleA, red.Hue), Shown(harness, EquipmentData.Hair));
            Assert.AreEqual((StyleA, red.Hue), Saved(harness, EquipmentData.Hair), "there after a relog");
            Assert.AreEqual(red.Hue, shown.AppearanceData[EquipmentData.Hair].Color.Hue);
            Assert.AreEqual((CaucasianV1, Tan.Hue), Shown(harness, EquipmentData.Face), "the skin is another item's");
        }

        [TestMethod]
        public void ASkinColourIsTheColourOfTheFace()
        {
            using var harness = Start();
            var melanotan = ToyTests.Grant(harness, SkinAfricanItem);
            var tone = new Color(Customizations.Palettes[4086][10]);
            harness.Drain();

            Customization.Request(harness.Client, Request(Customization.HueSkin, melanotan, tone));

            var shown = AssertDone(harness, melanotan, Customization.HueSkin, 0);
            Assert.AreEqual((CaucasianV1, tone.Hue), Shown(harness, EquipmentData.Face));
            Assert.AreEqual((CaucasianV1, tone.Hue), Saved(harness, EquipmentData.Face));
            Assert.AreEqual(tone.Hue, shown.AppearanceData[EquipmentData.Face].Color.Hue);
            Assert.AreEqual((StyleA, Brown.Hue), Shown(harness, EquipmentData.Hair));
        }

        [TestMethod]
        public void AColourIsThePalettesOrNearEnoughToBeItsPixelReadAnotherWay()
        {
            using var harness = Start();
            var dye = ToyTests.Grant(harness, HairColourItem, 5);
            var tone = ToyTests.Grant(harness, SkinCaucasianItem, 5);
            var asian = ToyTests.Grant(harness, SkinAsianItem, 5);
            var swatch = new Color(Customizations.Palettes[HairColourClass][30]);

            // A few units off a palette colour, as another decoder of the texture reads it: taken, as asked.
            var read = new Color((byte)(swatch.Red - 5), (byte)(swatch.Green + 3), (byte)(swatch.Blue - 7), 0);
            harness.Drain();

            Customization.Request(harness.Client, Request(Customization.HueHair, dye, read));

            AssertDone(harness, dye, Customization.HueHair, 4);
            Assert.AreEqual(new Color(read.Red, read.Green, read.Blue).Hue, Shown(harness, EquipmentData.Hair).Colour, "the colour the window showed, opaque");

            // Not on the palette: a green, a violet, no colour at all.
            AssertRefused(harness, Request(Customization.HueHair, dye, new Color(0, 255, 0)), dye, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.HueHair, dye, new Color(120, 0, 200)), dye, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.HueHair, dye, null), dye, PlayerMessage.PmActionFailedBadData);

            // A skin palette's first swatch is the one its window will not give: the corner pixel.
            AssertRefused(harness, Request(Customization.HueSkin, asian, new Color(222, 198, 177)), asian, PlayerMessage.PmActionFailedBadData);

            // A hair colour is not on a skin palette, nor a skin item a hair colour.
            AssertRefused(harness, Request(Customization.HueSkin, tone, new Color(159, 40, 38)), tone, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.HueHair, tone, swatch), tone, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.HueSkin, dye, swatch), dye, PlayerMessage.PmActionFailedBadData);
        }

        #endregion

        #region Hairstyle and face

        [TestMethod]
        public void AHairstyleIsTheHairsClassAndItsColourStays()
        {
            using var harness = Start();
            var styles = ToyTests.Grant(harness, HairstyleItem, 2);
            harness.Drain();

            Customization.Request(harness.Client, Request(Customization.ChangeHairstyle, styles, template: (int)StyleCTemplate));

            var shown = AssertDone(harness, styles, Customization.ChangeHairstyle, 1);
            Assert.AreEqual((StyleC, Brown.Hue), Shown(harness, EquipmentData.Hair));
            Assert.AreEqual((StyleC, Brown.Hue), Saved(harness, EquipmentData.Hair));
            Assert.AreEqual(StyleC, shown.AppearanceData[EquipmentData.Hair].Class);
            Assert.AreEqual((CaucasianV1, Tan.Hue), Shown(harness, EquipmentData.Face));

            // The one they have now: nothing to do, and the item is not spent on it.
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: (int)StyleCTemplate), styles, PlayerMessage.PmCannotPerformActionNow);

            // Bald, which every race may be.
            Customization.Request(harness.Client, Request(Customization.ChangeHairstyle, styles, template: (int)BaldTemplate));

            AssertDone(harness, styles, Customization.ChangeHairstyle, 0);
            Assert.AreEqual((Bald, Brown.Hue), Saved(harness, EquipmentData.Hair));
        }

        [TestMethod]
        public void AFaceIsTheFacesClassAndTheSkinStays()
        {
            using var harness = Start();
            var faces = ToyTests.Grant(harness, FaceItem);
            harness.Drain();

            Customization.Request(harness.Client, Request(Customization.ChangeFace, faces, template: (int)CaucasianLongV1Template));

            var shown = AssertDone(harness, faces, Customization.ChangeFace, 0);
            Assert.AreEqual((CaucasianLongV1, Tan.Hue), Shown(harness, EquipmentData.Face));
            Assert.AreEqual((CaucasianLongV1, Tan.Hue), Saved(harness, EquipmentData.Face));
            Assert.AreEqual(CaucasianLongV1, shown.AppearanceData[EquipmentData.Face].Class);
            Assert.AreEqual((StyleA, Brown.Hue), Shown(harness, EquipmentData.Hair));
        }

        [TestMethod]
        public void WhatAnItemDoesNotOfferIsRefused()
        {
            using var harness = Start();
            var styles = ToyTests.Grant(harness, HairstyleItem);
            var faces = ToyTests.Grant(harness, FaceItem);
            var dye = ToyTests.Grant(harness, HairColourItem);

            // A face from the hairstyle item, hair from the face item, nothing, and things that are neither.
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: (int)CaucasianLongV1Template), styles, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeFace, faces, template: (int)StyleCTemplate), faces, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles), styles, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: (int)HairstyleItem), styles, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: -3), styles, PlayerMessage.PmActionFailedBadData);

            // An item used for what it does not do.
            AssertRefused(harness, Request(Customization.ChangeFace, styles, template: (int)CaucasianLongV1Template), styles, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeHairstyle, dye, template: (int)StyleCTemplate), dye, PlayerMessage.PmActionFailedBadData);

            // A colour of something this client has no item for.
            AssertRefused(harness, Request(Customization.HueWeapon, dye, new Color(Customizations.Palettes[HairColourClass][0])), dye, PlayerMessage.PmCannotPerformActionNow);

            // A hybrid: human hair and a human face are not theirs, though the items offer them.
            harness.Client.Player.Race = Race.Thrax;
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: (int)StyleCTemplate), styles, PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, Request(Customization.ChangeFace, faces, template: (int)CaucasianLongV1Template), faces, PlayerMessage.PmActionFailedBadData);
            harness.Client.Player.Race = Race.Human;

            // Dead.
            harness.Client.Player.State = CharacterState.Dead;
            AssertRefused(harness, Request(Customization.ChangeHairstyle, styles, template: (int)StyleCTemplate), styles, PlayerMessage.PmCannotPerformActionNow);
        }

        #endregion

        #region The data

        [TestMethod]
        public void ThePalettesAndTheChoicesAreTheClients()
        {
            // customizationClassPalette: three hair colour items on one palette, three skin colour items each on its own.
            CollectionAssert.AreEquivalent(new uint[] { 3734, 3735, 4086, 4087, 4088, 4089 }, Customizations.Palettes.Keys.ToArray());

            foreach (var (item, palette) in Customizations.Palettes)
            {
                Assert.AreEqual(item is 3735 or 4088 or 4089 ? CustomizationType.HairColor : CustomizationType.SkinColor, Customizations.TypeOf(item), $"{item}");
                Assert.HasCount(palette.Length, palette.Distinct().ToArray(), $"{item}");
                Assert.IsTrue(palette.All(colour => colour >> 24 == 0xFF), $"{item}: opaque");
            }

            Assert.AreSame(Customizations.Palettes[3735], Customizations.Palettes[4088]);
            Assert.AreSame(Customizations.Palettes[3735], Customizations.Palettes[4089]);
            Assert.HasCount(120, Customizations.Palettes[HairColourClass]);
            Assert.HasCount(40, Customizations.Palettes[SkinCaucasianClass]);

            // The hair palette's swatches are on it, and the black of its lines is not one of its
            // colours (its darkest swatch is all but black, and near enough). A skin palette's
            // first swatch is its corner pixel and not one of its colours; its second is.
            Assert.IsTrue(Customizations.IsOnPalette(HairColourClass, new Color(94, 95, 99)));
            Assert.IsTrue(Customizations.IsOnPalette(HairColourClass, new Color(225, 225, 228)));
            CollectionAssert.DoesNotContain(Customizations.Palettes[HairColourClass], new Color(0, 0, 0).Hue);
            CollectionAssert.DoesNotContain(Customizations.Palettes[SkinCaucasianClass], new Color(177, 155, 156).Hue);
            CollectionAssert.Contains(Customizations.Palettes[SkinCaucasianClass], new Color(178, 163, 156).Hue);
            Assert.IsFalse(Customizations.IsOnPalette(SkinAsianClass, new Color(222, 198, 177)));
            Assert.IsFalse(Customizations.IsOnPalette(HairColourClass, new Color(0, 255, 0)));
            Assert.IsFalse(Customizations.IsOnPalette(HairstyleClass, new Color(94, 95, 99)), "an item with no palette offers no colour");
            Assert.IsFalse(Customizations.IsOnPalette(HairColourClass, null));

            // customizationChoice: nine hairstyles, forty-eight faces.
            CollectionAssert.AreEquivalent(new[] { HairstyleClass, FaceClass }, Customizations.Choices.Keys.ToArray());
            Assert.HasCount(9, Customizations.Choices[HairstyleClass]);
            Assert.HasCount(48, Customizations.Choices[FaceClass]);
            Assert.AreEqual(CustomizationType.Hairstyle, Customizations.TypeOf(HairstyleClass));
            Assert.AreEqual(CustomizationType.FaceTexture, Customizations.TypeOf(FaceClass));

            Assert.IsTrue(Customizations.TryGetChoice(HairstyleClass, StyleCTemplate, out var style));
            Assert.AreEqual(StyleC, style);
            Assert.IsFalse(Customizations.TryGetChoice(HairstyleClass, CaucasianV1Template, out _));
            Assert.IsFalse(Customizations.TryGetChoice(HairstyleClass, 0, out _));
            Assert.IsFalse(Customizations.TryGetChoice(HairColourClass, StyleCTemplate, out _));
        }

        [TestMethod]
        public void EveryChoiceIsAHairOrAFaceOfTheWorldUnderItsTemplate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var world = harness.WorldContext;
            var classOf = world.Set<ItemTemplateItemClassEntry>().AsNoTracking().ToDictionary(row => row.ItemTemplateId, row => row.ItemClass);
            var raceOf = world.Set<ItemTemplateRequirementRaceEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.RaceId);
            var slotOf = world.Set<EquipableClassEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.SlotId);

            foreach (var (item, slot) in new[] { (HairstyleClass, EquipmentData.Hair), (FaceClass, EquipmentData.Face) })
                foreach (var choice in Customizations.Choices[item])
                {
                    Assert.AreEqual(choice.ClassId, classOf[choice.TemplateId], $"template {choice.TemplateId}");
                    Assert.AreEqual((uint)slot, slotOf[choice.ClassId], $"class {choice.ClassId}");

                    // Human, all but Bald, which has no race.
                    if (choice.ClassId == Bald)
                        Assert.IsFalse(raceOf.ContainsKey(choice.TemplateId));
                    else
                        Assert.AreEqual((byte)Race.Human, raceOf[choice.TemplateId], $"template {choice.TemplateId}");
                }

            // And the eight items are there to be given, each a customization of its kind.
            foreach (var (template, itemClass) in new (uint, uint)[] { (26, 1068), (27, 1069), (47, 3734), (48, 3735), (100, 4086), (101, 4087), (102, 4088), (103, 4089) })
                Assert.AreEqual(itemClass, classOf[template], $"template {template}");
        }

        #endregion
    }
}
