using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Missions.Definitions;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class ToyTests
    {
        private const uint LogosFistTemplate = 111120;     // "Logos Fist" Emote: 464 level 1, flag 388, /logosfist (2, 48)
        private const uint RocketTemplate = 122834;        // Soyuz ISS Model Rocket: 509 level 1, family 1902
        private const uint GameStopRocketTemplate = 122843; // 509 level 3, family 1920
        private const uint ShadowTitleTemplate = 122272;   // Title: Shadow: 507 level 1, title 905
        private const uint BlueFireworkTemplate = 117207;  // Blue firework: 482 level 1, FIREWORK_EFFECT 364 level 1
        private const uint FlareGunTemplate = 117222;      // Flare Gun: 483 level 1, FLARE_GUN_EFFECT 374
        private const uint SnowballTemplate = 131481;      // Snowball: 528 level 1
        private const uint PineOckTemplate = 111117;       // Companion Pine-Ock: 460 level 1, variant 864
        private const uint GriselTemplate = 131950;        // Pet: Grisel: 460 level 10, variant 4976

        [TestMethod]
        public void TheEmoteItemSetsItsFlagUsesItselfUpAndTellsTheClient()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardEmoteItem);
            Land(harness, manager, pending);

            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[388]);
            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(1U, unit.CharacterFlags.GetValue(harness.Client.Player.Id, 388));
                Assert.IsNull(unit.Items.GetItem(item.Id), "The emote item is used up.");
            }

            var packets = harness.Drain();
            Assert.IsTrue(packets.OfType<PlayerFlagsPacket>().Single().PlayerFlagIds.Contains(388U));
            Assert.IsTrue(packets.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == ActionId.AccountrewardEmoteItem));
        }

        [TestMethod]
        public void AnEmoteAlreadyKnownIsRefusedAndTheItemKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);
            harness.Client.Player.PlayerFlags = new Dictionary<uint, uint>(harness.Client.Player.PlayerFlags) { [388] = 1 };

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardEmoteItem));
            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        public void AnEmoteLearnedDuringTheWindupStopsTheSecondItem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardEmoteItem);
            harness.Client.Player.PlayerFlags = new Dictionary<uint, uint>(harness.Client.Player.PlayerFlags) { [388] = 1 };
            Land(harness, manager, pending);

            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        [DataRow(RocketTemplate, 1U, "vfx_emote_model_rocket_v01")]
        [DataRow(GameStopRocketTemplate, 3U, "vfx_emote_model_rocket_v03_gamespot")]
        public void TheRocketFliesInFrontOfThePlayerIsKeptAndComesDown(uint template, uint level, string package)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, template);
            var manager = CreateManager(harness, 509, level, template);
            var player = harness.Client.Player;
            var before = harness.BootcampMap.DynamicObjects.Count;

            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.VisualByActor);
            Land(harness, manager, pending);

            var rocket = EmitterObjects(harness).Single();
            var emitter = (MapEmitter)rocket.ObjectData;
            Assert.AreEqual(package, FxPackages.NameOf(emitter.PackageId));
            Assert.IsTrue(emitter.IsOn);
            Assert.AreEqual(3.3f, System.Numerics.Vector3.Distance(player.Position, rocket.Position), 0.01f);
            Assert.IsTrue(System.Numerics.Vector3.Dot(AbilityManager.FacingOf(player), rocket.Position - player.Position) > 0, "In front.");

            using (var unit = harness.Context.CreateChar())
                Assert.IsNotNull(unit.Items.GetItem(item.Id), "The rocket is kept.");
            Assert.AreEqual(1U, item.StackSize);

            // One in the air at a time.
            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.VisualByActor));

            manager.ToyWorker(harness.BootcampMap, Environment.TickCount64 + 5_000);
            Assert.HasCount(1, EmitterObjects(harness).ToArray(), "Up for DURATION (10 s).");
            manager.ToyWorker(harness.BootcampMap, Environment.TickCount64 + 11_000);
            Assert.IsEmpty(EmitterObjects(harness).ToArray());

            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            Assert.IsTrue(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.VisualByActor), "Launchable again once down.");
        }

        [TestMethod]
        public void EveryRocketLevelHasAPackageTheClientKnows()
        {
            foreach (var packageId in AbilityManager.ModelRocketPackages.Values)
                Assert.IsTrue(FxPackages.Names.ContainsKey(packageId), packageId.ToString());
            Assert.AreEqual(AbilityManager.ModelRocketPackages.Count, AbilityManager.ModelRocketPackages.Values.Distinct().Count());
            Assert.HasCount(9, AbilityManager.ModelRocketPackages.ToArray());
        }

        [TestMethod]
        public void ARewardEmoteWantsItsFlagAndAPlainOneDoesNot()
        {
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());
            var watcher = context.CreateAdditionalClient(2);
            Clear(context, watcher);

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 48 });
            Assert.IsEmpty(MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().ToArray(), "/logosfist without flag 388.");

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 2 });
            Assert.HasCount(1, MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().ToArray(), "/clap needs no flag.");

            Clear(context, watcher);
            context.Client.Player.PlayerFlags[388] = 1;
            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 48 });
            Assert.AreEqual(48U, MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().Single().ActionArgId);

            Assert.IsTrue(Gestures.TryGetEmoteFlag(36, out var airGuitar));
            Assert.AreEqual(20000005U, airGuitar);
            Assert.IsFalse(Gestures.TryGetEmoteFlag(2, out _));
        }


        [TestMethod]
        public void ATitleItemGivesItsTitleOnceAndKeepsTheOthers()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            using (var unit = harness.Context.CreateChar())
            {
                Assert.IsTrue(unit.CharacterTitles.Add(player.Id, 362));
                Assert.IsFalse(unit.CharacterTitles.Add(player.Id, 362), "Once each.");
            }
            player.Titles = new List<uint> { 362 };

            var item = Grant(harness, ShadowTitleTemplate);
            var manager = CreateManager(harness, 507, 1, ShadowTitleTemplate);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(507, 1, item.EntityId));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardTitleItem));

            CollectionAssert.AreEquivalent(new uint[] { 362, 905 }, player.Titles.ToArray());
            using (var unit = harness.Context.CreateChar())
            {
                CollectionAssert.AreEquivalent(new uint[] { 362, 905 }, unit.CharacterTitles.Get(player.Id).ToArray(), "More than one title to a character.");
                Assert.IsNull(unit.Items.GetItem(item.Id), "The title item is used up.");
            }
            Assert.AreEqual(905U, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);

            var second = Grant(harness, ShadowTitleTemplate);
            manager.RequestPerformAbility(harness.Client, Request(507, 1, second.EntityId));
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardTitleItem), "A title held already.");
        }

        [TestMethod]
        [DataRow(BlueFireworkTemplate, 482, 364)]
        [DataRow(FlareGunTemplate, 483, 374)]
        public void AFireworkOrFlareGoesOffWhereItWasAimedAndIsCleanedUp(uint template, int actionId, int effectTypeId)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, template);
            var manager = CreateManager(harness, (uint)actionId, 1, template);
            var player = harness.Client.Player;
            var aimed = player.Position + AbilityManager.FacingOf(player) * 20f;
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(actionId, 1, item.EntityId, aimed));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)actionId));

            var proxy = Proxies(harness).Single();
            Assert.AreEqual(20f, System.Numerics.Vector2.Distance(new(proxy.Position.X, proxy.Position.Z), new(player.Position.X, player.Position.Z)), 0.5f);
            var packets = harness.Drain();
            var attached = packets.OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == effectTypeId);
            Assert.AreEqual(1U, attached.EffectLevel);
            Assert.IsFalse(attached.Announced, "The recovery's hit announces it.");
            var recovery = packets.OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == (ActionId)actionId);
            CollectionAssert.AreEqual(new[] { proxy.EntityId }, recovery.Hits.Select(hit => hit.EntityId).ToArray());
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.Items.GetItem(item.Id), "Used up.");

            manager.ToyWorker(harness.BootcampMap, Environment.TickCount64 + 16_000);
            Assert.AreEqual(attached.EffectId, harness.Drain().OfType<GameEffectDetachedPacket>().Single().EffectId, "Detached after DURATION.");
            Assert.HasCount(1, Proxies(harness).ToArray(), "Kept while the effect fades.");
            manager.ToyWorker(harness.BootcampMap, Environment.TickCount64 + 19_000);
            Assert.IsEmpty(Proxies(harness).ToArray());
        }

        [TestMethod]
        public void ASnowballThrownAtNobodyIsUsedUpAndHitsNothing()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, SnowballTemplate);
            var manager = CreateManager(harness, 528, 1, SnowballTemplate);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(528, 1, item.EntityId));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)528));

            Assert.IsEmpty(harness.Drain().OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == (ActionId)528).Hits.ToArray());
            using var unit = harness.Context.CreateChar();
            Assert.IsNull(unit.Items.GetItem(item.Id));
        }

        // nullability.py is TARGET_FRIENDLY: a friendly creature is a fair target, as it is for the
        // Snowball Launcher. A hostile one never arrives from the client, which blind-shoots
        // instead, and is refused with the snowball kept.
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ASnowballHitsAFriendlyCreatureButIsRefusedAHostileOne(bool friendly)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, SnowballTemplate);
            var manager = CreateManager(harness, 528, 1, SnowballTemplate);
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 5));
            npc.TargetCategory = friendly ? TargetCategory.Friendly : TargetCategory.Hostile;
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(528, 1, item.EntityId, targetId: npc.EntityId));

            if (!friendly)
            {
                Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == (ActionId)528));
                using var kept = harness.Context.CreateChar();
                Assert.IsNotNull(kept.Items.GetItem(item.Id), "Kept.");
                return;
            }

            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)528));
            var recovery = harness.Drain().OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == (ActionId)528);
            CollectionAssert.AreEqual(new[] { npc.EntityId }, recovery.Hits.Select(hit => hit.EntityId).ToArray());
            using var unit = harness.Context.CreateChar();
            Assert.IsNull(unit.Items.GetItem(item.Id), "Used up.");
        }

        // Snowball_stacks_not_unique: the one thing on John's list that is not Character Unique,
        // and it stacks to its class's 5000 as the client's itemclass table has it.
        [TestMethod]
        public void SnowballsStackToFiveThousandAndAreNotCharacterUnique()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var world = harness.WorldContext;
            var templates = world.Set<ItemTemplateEntry>().AsNoTracking();

            Assert.AreEqual(0, templates.Single(row => row.Id == SnowballTemplate).HasCharacterUniqueFlag);
            Assert.AreEqual(1, templates.Single(row => row.Id == 131482).HasCharacterUniqueFlag, "The launcher stays unique.");
            Assert.AreEqual(5000U, world.Set<ItemClassEntry>().AsNoTracking().Single(row => row.Id == 30547).StackSize);
        }

        [TestMethod]
        public void ThrownSnowballsAreUsedUpOneAtATime()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, SnowballTemplate, 3);
            var manager = CreateManager(harness, 528, 1, SnowballTemplate);

            manager.RequestPerformAbility(harness.Client, Request(528, 1, item.EntityId));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)528));

            Assert.AreEqual(2U, item.StackSize);
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(2U, unit.Items.GetItem(item.Id).StackSize);
        }

        // A character who already carries snowballs can buy more, by the stack, at the counter's
        // price; they merge into the stack already carried.
        [TestMethod]
        public void SnowballsAreBoughtByTheStackByAPlayerWhoAlreadyHasSome()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var carried = Grant(harness, SnowballTemplate);
            Assert.IsFalse(ItemManager.Instance.GetItemTemplateById(SnowballTemplate).HasCharacterUniqueFlag);
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(0) { ItemPrice = 1, VendorItems = { SnowballTemplate } };
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            var npcs = new NpcManager(harness.Context, harness.Manager);

            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });
            var stock = EntityManager.Instance.VendorItems[npc.EntityId].Single();
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
            {
                VendorEntityId = npc.EntityId,
                ItemEntityId = stock,
                Quantity = 500
            });

            Assert.AreEqual(credits - 500, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(501U, carried.StackSize);
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(501U, unit.Items.GetItem(carried.Id).StackSize);
        }

        // "You received N X." comes from GotLoot, which carries the class id for the client to
        // name; as a DisplayClientMessage argument the class went out as text, "You received 1 12951".
        [TestMethod]
        public void APurchaseIsAnnouncedByClassIdForTheClientToName()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var carried = Grant(harness, SnowballTemplate);
            LoadTemplate(harness, PineOckTemplate);
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(0) { ItemPrice = 1, VendorItems = { SnowballTemplate, PineOckTemplate } };
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });
            ulong Stock(uint template) => EntityManager.Instance.VendorItems[npc.EntityId]
                .Single(id => EntityManager.Instance.GetItem(id).ItemTemplate.ItemTemplateId == template);
            harness.Drain();

            // Merged into the stack already carried: the quantity bought, and that stack.
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
                { VendorEntityId = npc.EntityId, ItemEntityId = Stock(SnowballTemplate), Quantity = 5 });

            var packets = harness.Drain();
            var got = packets.OfType<Rasa.Packets.ClientMethod.Server.GotLootPacket>().Single();
            Assert.AreEqual(npc.EntityId, got.CreatureEntityId);
            Assert.AreEqual(0, got.Credits);
            Assert.AreEqual((30547U, 5U, carried.EntityId), got.Entries.Single());
            Assert.IsFalse(packets.OfType<Rasa.Packets.Communicator.Server.DisplayClientMessagePacket>()
                .Any(message => message.MsgId == PlayerMessage.PmGotLootFromUnknown));

            // A new item: its own entity.
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
                { VendorEntityId = npc.EntityId, ItemEntityId = Stock(PineOckTemplate), Quantity = 1 });

            var pet = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == PineOckTemplate);
            got = harness.Drain().OfType<Rasa.Packets.ClientMethod.Server.GotLootPacket>().Single();
            Assert.AreEqual((26361U, 1U, pet.EntityId), got.Entries.Single());

            // Recv_GotLoot(creatureEntityId, [(classId, quantity, itemId)], moneyAmount).
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
                got.Write(writer);
            using var expected = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(expected, Encoding.UTF8, true)))
            {
                writer.WriteTuple(3);
                writer.WriteULong(npc.EntityId);
                writer.WriteList(1);
                writer.WriteTuple(3);
                writer.WriteUInt(26361);
                writer.WriteUInt(1);
                writer.WriteULong(pet.EntityId);
                writer.WriteInt(0);
            }
            CollectionAssert.AreEqual(expected.ToArray(), stream.ToArray());
        }

        [TestMethod]
        public void APetFollowsItsOwnerTakesNoPartAndGoesHomeOnTheSameItem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pineOck = Grant(harness, PineOckTemplate);
            var grisel = Grant(harness, GriselTemplate);
            LoadClass(harness, 7747);
            LoadClass(harness, 30621);
            var manager = CreateManager(harness, 460, 1, PineOckTemplate);
            AddLevel(harness, manager, 460, 10, GriselTemplate);
            var player = harness.Client.Player;

            manager.RequestPerformAbility(harness.Client, Request(460, 1, pineOck.EntityId));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardPet));

            var pet = AbilityManager.PetCreatureOf(player);
            Assert.IsNotNull(pet);
            Assert.AreEqual((EntityClasses)7747, pet.EntityClass);
            Assert.AreEqual(TargetCategory.Decoration, pet.TargetCategory);
            Assert.IsFalse(TargetCategories.IsCombatant(pet.TargetCategory), "Nobody fights it.");
            Assert.AreEqual(player.EntityId, pet.Controller.ActionFollow.FollowTargetId);
            Assert.AreEqual(0UL, pet.Controller.ActionFollow.AssistTargetId, "It assists nobody.");
            Assert.IsFalse(MinionManager.Instance.MinionsOf(harness.Client).Contains(pet), "No minion commands.");
            using (var unit = harness.Context.CreateChar())
                Assert.IsNotNull(unit.Items.GetItem(pineOck.Id), "The summoner is kept.");

            // Another pet takes its place, once the 5 s reuse is over.
            manager.RequestPerformAbility(harness.Client, Request(460, 10, grisel.EntityId));
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardPet), "Still cooling down.");
            player.ActionReuseUntil.Remove(ActionId.AccountrewardPet);
            manager.RequestPerformAbility(harness.Client, Request(460, 10, grisel.EntityId));
            Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardPet));
            var second = AbilityManager.PetCreatureOf(player);
            Assert.AreEqual((EntityClasses)30621, second.EntityClass);
            Assert.IsFalse(EntityManager.Instance.Creatures.Values.Contains(pet), "The first went home.");

            // The same one again: home, and nothing performed.
            manager.RequestPerformAbility(harness.Client, Request(460, 10, grisel.EntityId));
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardPet));
            Assert.IsNull(AbilityManager.PetCreatureOf(player));
            Assert.IsFalse(EntityManager.Instance.Creatures.Values.Contains(second));
        }

        [TestMethod]
        public void EveryPetVariantIsACreatureClass()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Assert.HasCount(20, AbilityManager.PetVariants.ToArray());
            foreach (var (variant, (classId, _)) in AbilityManager.PetVariants)
                Assert.IsTrue(harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Where(entry => entry.Id == classId).AsEnumerable()
                    .Any(entry => entry.AugList.Split(',').Contains("1")), $"{variant}: {classId} is a creature class");
        }

        /// <summary>A creature class, as the server loads every class at start-up.</summary>
        internal static void LoadClass(BootcampRuntimeTestHarness.Harness harness, uint classId)
        {
            var entry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == classId);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId] = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0);
        }

        private static IEnumerable<DynamicObject> Proxies(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.EntityClassId == AbilityManager.LocationProxyClass)
                .Distinct();

        private static void Clear(MissionTestContext context, Client watcher)
        {
            context.Drain();
            MissionTestContext.Drain(watcher);
            context.Map.PerformRecovery.RemoveAll(action => action.Actor == context.Client.Player);
        }

        private static IEnumerable<DynamicObject> EmitterObjects(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.Emitter)
                .Distinct();

        /// <summary>Loads the item template's class the way the harness loads the Bootcamp loot (LoadBootcampLootContent).</summary>
        internal static void LoadTemplate(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            var world = harness.WorldContext;
            var link = world.Set<ItemTemplateItemClassEntry>().AsNoTracking().Single(row => row.ItemTemplateId == template);
            var data = world.Set<ItemTemplateEntry>().AsNoTracking().Single(row => row.Id == template);
            var entry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass);
            var classId = (EntityClasses)entry.Id;
            EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous);
            var entityClass = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',')
                    .Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0)
            {
                ItemClassInfo = new ItemClassInfo(world.Set<ItemClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass)),
                ItemTemplates = previous == null ? new Dictionary<uint, ItemTemplate>() : new Dictionary<uint, ItemTemplate>(previous.ItemTemplates)
            };
            entityClass.ItemTemplates[template] = new ItemTemplate(link)
            {
                QualityId = data.QualityId,
                InventoryCategory = (InventoryCategory)data.InventoryCategory,
                HasSellableFlag = data.HasSellableFlag != 0,
                BuyPrice = data.BuyPrice,
                SellPrice = data.SellPrice,
                HasCharacterUniqueFlag = data.HasCharacterUniqueFlag != 0
            };
            EntityClassManager.Instance.LoadedEntityClasses[classId] = entityClass;
            ItemManager.Instance.ItemTemplateItemClass[template] = classId;
        }

        internal static Item Grant(BootcampRuntimeTestHarness.Harness harness, uint template, uint quantity = 1)
        {
            LoadTemplate(harness, template);
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(template, quantity) }, unit));
                grant.Publish(harness.Client);
            }

            return harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == template);
        }

        internal static void Land(BootcampRuntimeTestHarness.Harness harness, AbilityManager manager, ActionData pending)
        {
            harness.BootcampMap.PerformRecovery.Remove(pending);
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                manager.PerformRecovery(harness.BootcampMap, pending);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        internal static RequestPerformAbilityPacket Request(int actionId, uint level, ulong itemId,
            System.Numerics.Vector3? location = null, ulong targetId = 0)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt(actionId);
                writer.WriteInt((int)level);
                if (location.HasValue)
                {
                    writer.WriteTuple(3);
                    writer.WriteDouble(location.Value.X);
                    writer.WriteDouble(location.Value.Y);
                    writer.WriteDouble(location.Value.Z);
                }
                else if (targetId != 0)
                    writer.WriteULong(targetId);
                else
                    writer.WriteNoneStruct();
                writer.WriteULong(itemId);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestPerformAbilityPacket();
            packet.Read(reader);
            return packet;
        }

        /// <summary>An AbilityManager with the one action level loaded from the seeded world tables, and the item that performs it.</summary>
        internal static AbilityManager CreateManager(BootcampRuntimeTestHarness.Harness harness, uint actionId, uint levelId, uint template)
        {
            var manager = (AbilityManager)Activator.CreateInstance(typeof(AbilityManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { harness.Context, harness.Manager }, null);
            AddLevel(harness, manager, actionId, levelId, template);
            return manager;
        }

        private static void AddLevel(BootcampRuntimeTestHarness.Harness harness, AbilityManager manager, uint actionId, uint levelId, uint template)
        {
            var actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager).GetField("_actions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            var row = harness.WorldContext.Set<ActionEntry>().Single(entry => entry.Id == actionId);
            var level = harness.WorldContext.Set<ActionLevelEntry>().Single(entry => entry.ActionId == actionId && entry.Level == levelId);
            if (!actions.TryGetValue((ActionId)row.Id, out var action))
                actions[(ActionId)row.Id] = action = new ActionInfo { ActionId = (ActionId)row.Id, Name = row.Name, Module = row.Module };
            var info = new ActionLevelInfo
            {
                ActionId = action.ActionId, Level = levelId, WindupMs = level.WindupMs, MaxRange = level.MaxRange,
                RecoveryMs = level.RecoveryMs, ReuseMs = level.ReuseMs, StartReuseOnPerform = level.StartReuseOnPerform != 0
            };
            foreach (var property in harness.WorldContext.ActionPropertyEntries.Where(entry => entry.ActionId == actionId && entry.Level == levelId))
                info.Properties[(AbilityProperty)property.PropertyId] = property.Value;
            foreach (var requirement in harness.WorldContext.Set<ActionItemRequirementEntry>().Where(entry => entry.ActionId == actionId && entry.Level == levelId))
                info.ItemRequirements.Add(new ActionItemRequirement { ItemClass = (EntityClasses)requirement.ItemClassId, Quantity = requirement.Quantity });
            action.Levels[levelId] = info;
            ((Dictionary<uint, (ActionId ActionId, uint Level)>)typeof(AbilityManager).GetField("_itemTemplateActions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager))[template] = ((ActionId)actionId, levelId);
        }
    }
}
