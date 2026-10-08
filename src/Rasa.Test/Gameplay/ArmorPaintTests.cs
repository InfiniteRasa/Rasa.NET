using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// Armour paint: RequestCustomization as the client's colour window sends it, and what the
    /// server does with it (Managers.Customization).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ArmorPaintTests
    {
        // Bane Blood Red Armor Paint (AccountReward_Dye_V1): one to a stack.
        private const uint RewardPaintTemplate = 111128;
        private const uint RewardPaintClass = 26380;

        // Standard Armor Paint: stacks.
        private const uint StandardPaintTemplate = 50251;

        // Motor Assist Armor Vest, levels 1-2.
        private const uint VestTemplate = 13186;

        private const uint SnowballTemplate = 131481;

        private static Color Swatch(int index) => new Color(Customizations.Hues[RewardPaintClass][index]);

        private static RequestCustomizationPacket Read(System.Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            using (var writer = new PythonWriter(binary))
                write(writer);

            stream.Position = 0;
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
            using var python = new PythonReader(reader);
            var packet = new RequestCustomizationPacket();
            packet.Read(python);
            Assert.AreEqual(stream.Length, stream.Position, "every argument is read");
            return packet;
        }

        private static void WriteHue(PythonWriter pw, int red, int green, int blue, int alpha)
        {
            pw.WriteTuple(4);
            pw.WriteInt(red);
            pw.WriteInt(green);
            pw.WriteInt(blue);
            pw.WriteInt(alpha);
        }

        [TestMethod]
        public void AnArmourPaintRequestIsRead()
        {
            var packet = Read(pw =>
            {
                pw.WriteTuple(5);
                pw.WriteInt(4);
                pw.WriteULong(0x1_0000_0001UL);
                pw.WriteNoneStruct();
                pw.WriteULong(0x1_0000_0002UL);
                WriteHue(pw, 218, 54, 46, 255);
            });

            Assert.AreEqual(4, packet.CustomizationActionArgId);
            Assert.AreEqual(0x1_0000_0001UL, packet.CustomizationEntityId);
            Assert.IsNull(packet.SelectedClassTemplateId);
            Assert.AreEqual(0x1_0000_0002UL, packet.TargetEntityId);
            Assert.AreEqual(new Color(218, 54, 46).Hue, packet.Hue.Hue);
        }

        [TestMethod]
        public void TheOtherShapesAreRead()
        {
            // A hairstyle: the template picked, no target, no hue.
            var style = Read(pw =>
            {
                pw.WriteTuple(5);
                pw.WriteInt(5);
                pw.WriteULong(77);
                pw.WriteInt(36);
                pw.WriteNoneStruct();
                pw.WriteNoneStruct();
            });

            Assert.AreEqual(36, style.SelectedClassTemplateId);
            Assert.AreEqual(0UL, style.TargetEntityId);
            Assert.IsNull(style.Hue);

            // A hair colour: the hue alone, its channels as the floats a widget may hand back.
            var hair = Read(pw =>
            {
                pw.WriteTuple(5);
                pw.WriteInt(1);
                pw.WriteULong(77);
                pw.WriteNoneStruct();
                pw.WriteNoneStruct();
                pw.WriteTuple(4);
                pw.WriteDouble(113.0);
                pw.WriteDouble(57.0);
                pw.WriteDouble(57.0);
                pw.WriteDouble(255.0);
            });

            Assert.IsNull(hair.SelectedClassTemplateId);
            Assert.AreEqual(new Color(113, 57, 57).Hue, hair.Hue.Hue);

            // Not a colour: read past, and no hue.
            var broken = Read(pw =>
            {
                pw.WriteTuple(5);
                pw.WriteInt(4);
                pw.WriteULong(77);
                pw.WriteNoneStruct();
                pw.WriteULong(78);
                pw.WriteTuple(3);
                pw.WriteInt(1);
                pw.WriteInt(2);
                pw.WriteInt(900);
            });

            Assert.IsNull(broken.Hue);
        }

        private static Item Wear(BootcampRuntimeTestHarness.Harness harness, Item piece, EquipmentData slot)
        {
            EntityClassManager.Instance.LoadedEntityClasses[piece.ItemTemplate.Class].EquipableClassInfo = new EquipableClassInfo(slot);

            var inventory = harness.Client.Player.Inventory;
            inventory.PersonalInventory[inventory.PersonalInventory.IndexOf(piece.EntityId)] = 0;

            while (inventory.EquippedInventory.Count <= (int)slot)
                inventory.EquippedInventory.Add(0);

            inventory.EquippedInventory[(int)slot] = piece.EntityId;
            return piece;
        }

        private static RequestCustomizationPacket Paint(Item paint, ulong pieceId, Color hue, int argId = 4) =>
            new RequestCustomizationPacket
            {
                CustomizationActionArgId = argId,
                CustomizationEntityId = paint.EntityId,
                TargetEntityId = pieceId,
                Hue = hue
            };

        [TestMethod]
        public void AWornPieceTakesTheColourForEveryoneAndThePaintIsUsedUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var paint = ToyTests.Grant(harness, RewardPaintTemplate);
            var vest = Wear(harness, ToyTests.Grant(harness, VestTemplate), EquipmentData.Torso);
            var before = vest.Color;
            var swatch = Swatch(7);
            Assert.AreNotEqual(before, swatch.Hue);
            harness.Drain();

            Customization.Request(harness.Client, Paint(paint, vest.EntityId, swatch));

            Assert.AreEqual(swatch.Hue, vest.Color);
            Assert.AreEqual(swatch.Hue, harness.Client.Player.AppearanceData[EquipmentData.Torso].Color.Hue);
            Assert.AreEqual((uint)vest.ItemTemplate.Class, harness.Client.Player.AppearanceData[EquipmentData.Torso].Class);
            Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Contains(paint.EntityId), "the paint is used up");

            var sent = harness.Drain();
            var recovery = sent.OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual(ActionId.Customize, recovery.ActionId);
            Assert.AreEqual(4U, recovery.ActionArgId);
            Assert.AreEqual(swatch.Hue, sent.OfType<AppearanceDataPacket>().Single().AppearanceData[EquipmentData.Torso].Color.Hue);
            Assert.IsFalse(sent.OfType<UserActionFailedPacket>().Any());

            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(swatch.Hue, unit.Items.GetItem(vest.Id).Color);
        }

        [TestMethod]
        public void AColourOneOffASwatchIsTakenForThatSwatch()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var paint = ToyTests.Grant(harness, RewardPaintTemplate);
            var vest = Wear(harness, ToyTests.Grant(harness, VestTemplate), EquipmentData.Torso);
            var swatch = Swatch(12);

            Customization.Request(harness.Client, Paint(paint, vest.EntityId,
                new Color((byte)(swatch.Red - 1), swatch.Green, (byte)(swatch.Blue + 1))));

            Assert.AreEqual(swatch.Hue, vest.Color);
        }

        [TestMethod]
        public void APieceInThePackKeepsTheColourAndAStackLosesOne()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var paint = ToyTests.Grant(harness, StandardPaintTemplate, 3);
            var vest = ToyTests.Grant(harness, VestTemplate);
            var hue = Customizations.Hues[(uint)paint.ItemTemplate.Class][3];
            harness.Drain();

            Customization.Request(harness.Client, Paint(paint, vest.EntityId, new Color(hue)));

            Assert.AreEqual(hue, vest.Color);
            Assert.AreEqual(2U, paint.StackSize);

            var sent = harness.Drain();
            Assert.AreEqual(1, sent.OfType<PerformRecoveryPacket>().Count());
            Assert.IsFalse(sent.OfType<AppearanceDataPacket>().Any(), "nothing worn changed");

            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(hue, unit.Items.GetItem(vest.Id).Color);
            Assert.AreEqual(2U, unit.Items.GetItem(paint.Id).StackSize);
        }

        private static void AssertRefused(BootcampRuntimeTestHarness.Harness harness, Item paint, Item vest, uint colour,
            RequestCustomizationPacket request, PlayerMessage message)
        {
            harness.Drain();

            Customization.Request(harness.Client, request);

            Assert.AreEqual(colour, vest.Color);
            Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.Contains(paint.EntityId), "the paint is kept");
            Assert.AreEqual(1U, paint.StackSize);

            var sent = harness.Drain();
            var failed = sent.OfType<UserActionFailedPacket>().Single();
            Assert.AreEqual(ActionId.Customize, failed.ActionId);
            Assert.AreEqual((uint)request.CustomizationActionArgId, failed.ActionArgId);
            Assert.AreEqual(message, failed.MsgId);
            Assert.AreEqual(1, sent.OfType<ActionFailedPacket>().Count());
            Assert.IsFalse(sent.OfType<PerformRecoveryPacket>().Any());
            Assert.IsFalse(sent.OfType<AppearanceDataPacket>().Any());
        }

        [TestMethod]
        public void WhatTheClientWouldNotSendIsRefusedAndAnswered()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var paint = ToyTests.Grant(harness, RewardPaintTemplate);
            var snowball = ToyTests.Grant(harness, SnowballTemplate);
            var vest = Wear(harness, ToyTests.Grant(harness, VestTemplate), EquipmentData.Torso);
            var colour = vest.Color;
            var swatch = Swatch(0);

            // A colour the paint does not offer, and none at all.
            AssertRefused(harness, paint, vest, colour, Paint(paint, vest.EntityId, new Color(1, 2, 3)), PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, paint, vest, colour, Paint(paint, vest.EntityId, null), PlayerMessage.PmActionFailedBadData);

            // Something the paint may not colour.
            AssertRefused(harness, paint, vest, colour, Paint(paint, snowball.EntityId, swatch), PlayerMessage.PmCannotUseCustomizationOnItem);

            // A piece that is not theirs, and no piece.
            AssertRefused(harness, paint, vest, colour, Paint(paint, ulong.MaxValue - 5, swatch), PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, paint, vest, colour, Paint(paint, 0, swatch), PlayerMessage.PmActionFailedBadData);

            // An item that is not a paint, used as one.
            AssertRefused(harness, paint, vest, colour, Paint(snowball, vest.EntityId, swatch), PlayerMessage.PmActionFailedBadData);

            // A paint used as a hair colour, which it is not (AppearanceChangeTests), and a
            // weapon colour, which this client has no item for.
            AssertRefused(harness, paint, vest, colour, Paint(paint, 0, swatch, argId: 1), PlayerMessage.PmActionFailedBadData);
            AssertRefused(harness, paint, vest, colour, Paint(paint, vest.EntityId, swatch, argId: 3), PlayerMessage.PmCannotPerformActionNow);

            // Dead.
            harness.Client.Player.State = CharacterState.Dead;
            AssertRefused(harness, paint, vest, colour, Paint(paint, vest.EntityId, swatch), PlayerMessage.PmCannotPerformActionNow);
        }

        [TestMethod]
        public void ThePaintDataIsTheClients()
        {
            Assert.HasCount(65, Customizations.Types);
            Assert.HasCount(51, Customizations.Hues);
            Assert.IsTrue(Customizations.Hues.Values.All(hues => hues.Length == Customizations.HuesPerItem));
            Assert.AreEqual(CustomizationType.ClothingColor, Customizations.TypeOf(RewardPaintClass));
            Assert.IsNull(Customizations.TypeOf(1));

            // customizationClassHueChoice[26380][0] is (0, 218, 54, 46).
            Assert.AreEqual(new Color(218, 54, 46).Hue, Customizations.Hues[RewardPaintClass][0]);

            // Armor_T1_MotorAssist_V01_CMN_Vest_01_to_02 is on every armour paint's list; the AFS
            // police vest only on the standard paints'; the boxing kit colours the boxing gear alone.
            Assert.IsTrue(Customizations.MayApply(RewardPaintClass, 15662));
            Assert.IsFalse(Customizations.MayApply(RewardPaintClass, RewardPaintClass));
            Assert.IsFalse(Customizations.MayApply(29218, 15662));

            // Hairstyle Modification has no list: anything.
            Assert.IsTrue(Customizations.MayApply(1068, 15662));
        }
    }
}
