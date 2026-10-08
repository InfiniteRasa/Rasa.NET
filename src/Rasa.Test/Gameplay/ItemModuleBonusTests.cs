extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    // What item modules do (ItemModuleBonuses): the bonuses of the modules in what a player wears
    // and holds, and the weapon modules that steal or debuff on a hit.
    [TestClass]
    [DoNotParallelize]
    public class ItemModuleBonusTests
    {
        private const string WorldBefore = "20261121000000_Add_module_crafting";
        private const string Migration = "20261122000000_Add_resist_debuff_module_effects";

        // Armor modules. The amounts beside them are at item level 20, as the client's tooltip
        // works them out.
        private const uint Body1 = 100060;              // +3
        private const uint Mind1 = 100068;              // +3
        private const uint Spirit5 = 900041;            // +10
        private const uint Health1 = 100084;            // +42
        private const uint Power1 = 100008;             // +6
        private const uint Regen1 = 100016;             // +5
        private const uint ResistFire1 = 100036;        // 3
        private const uint ResistFire5 = 900045;        // 15
        private const uint Armor1 = 900180;             // Armor Absorption [1], "Total Armor": +42
        private const uint HealthRegen1 = 900190;       // 6 a second
        private const uint PowerRegen5 = 900204;        // 5 a second
        private const uint ArmorRegen1 = 900210;        // Armor Recharge [1]: 6 a second
        private const uint ArmorPierce5 = 900420;       // 5%
        private const uint MoveSpeed5 = 900415;         // 5%
        private const uint Experience5 = 900405;        // 5%
        private const uint ResistStun5 = 900395;        // 15
        private const uint ResistKnockback1 = 900386;   // 3
        private const uint ResistRoot5 = 900400;        // 15
        private const uint ResistSnare5 = 900410;       // 15
        private const uint ResistBlind5 = 900385;       // 15

        // Weapon modules.
        private const uint Crit5 = 900051;              // 5%
        private const uint Threat5 = 900052;            // -50%
        private const uint WeaponPierce5 = 900179;      // 10%
        private const uint WeaponResistFire1 = 900301;  // 6
        private const uint WeaponRegen1 = 900331;       // Regen Bonus [1]: +10
        private const uint StealArmor1 = 900260;        // 32
        private const uint StealHealth1 = 900265;       // 11
        private const uint StealPower5 = 900274;        // 20
        private const uint StealAdrenaline1 = 900275;   // 10
        private const uint DebuffFire1 = 900240;        // -10
        private const uint DebuffFire5 = 900244;        // -30
        private const uint DebuffPhysical3 = 900222;    // -20

        private const int ItemLevel = 20;
        private const uint ArmorClass = 990501;
        private const uint WeaponClass = 990502;

        private readonly List<Item> _items = new List<Item>();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Rasa.Logger.Config == null)
                Rasa.Logger.UpdateConfig(new Rasa.Logger.LoggerConfig());
        }

        [TestInitialize]
        public void LoadTheSeed()
        {
            ItemModules.Load(ItemModuleSeed.Classes, ItemModuleSeed.Effects.Concat(ItemModuleSeed.DebuffEffects));
            ItemModuleBonuses.Config = new ItemModulesConfig();
            ItemModuleBonuses.Roll = ItemModuleBonuses.DefaultRoll;
        }

        [TestCleanup]
        public void PutBack()
        {
            ItemModuleBonuses.Config = new ItemModulesConfig();
            ItemModuleBonuses.Roll = ItemModuleBonuses.DefaultRoll;

            foreach (var item in _items)
            {
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.UnregisterEntity(item.EntityId);
                EntityManager.Instance.FreeEntity(item.EntityId);
            }

            _items.Clear();
        }

        #region The rows

        [TestMethod]
        public void EveryDebuffModuleHasItsRow()
        {
            var modules = ItemModuleSeed.Classes.ToDictionary(row => row.Id);
            var rows = ItemModuleSeed.DebuffEffects;

            Assert.AreEqual(40, rows.Count);
            CollectionAssert.AreEqual(Enumerable.Range(291, 40).Select(id => (uint)id).ToList(), rows.Select(row => row.Id).ToList(),
                "after the 290 of Add_item_modules, and no id of theirs");
            Assert.AreEqual(290u, ItemModuleSeed.Effects.Max(row => row.Id));

            // (variant, the client's DEBUFF_*_RESIST_PROC, the kind of damage)
            var kinds = new (uint Variant, uint Effect, DamageType Type)[]
            {
                (32, 9, DamageType.Physical), (29, 112, DamageType.Fire), (30, 113, DamageType.Ice), (34, 114, DamageType.Virulent),
                (28, 115, DamageType.EMP), (31, 171, DamageType.Laser), (33, 172, DamageType.Sonic), (23, 173, DamageType.Electrical)
            };

            foreach (var (variant, effect, type) in kinds)
            {
                var ofKind = rows.Where(row => modules[row.ModuleId].VariantId == variant).OrderBy(row => modules[row.ModuleId].Level).ToList();

                Assert.AreEqual(5, ofKind.Count, $"variant {variant}");
                CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4, 5 }, ofKind.Select(row => modules[row.ModuleId].Level).ToList());
                CollectionAssert.AreEqual(new double[] { -10, -15, -20, -25, -30 }, ofKind.Select(row => row.FlatValue).ToList(), "the module items' own Base Bonus");

                foreach (var row in ofKind)
                {
                    Assert.AreEqual(effect, row.EffectId);
                    Assert.AreEqual((int)type, row.Arg1);
                    Assert.AreEqual(ItemModuleSeed.DebuffSeconds, row.Arg2, "the seconds the tooltip prints");
                    Assert.AreEqual(0d, row.LinearValue);
                    Assert.AreEqual(0d, row.ExpValue);
                    Assert.AreEqual(1212u, modules[row.ModuleId].ClassSetId, "a weapon module");
                    StringAssert.StartsWith(modules[row.ModuleId].Comment, "Weapon Module: Debuff ");
                }
            }

            // With them every module that is an item a crafting station puts in does something.
            var withEffect = ItemModuleSeed.Effects.Concat(rows).Select(row => row.ModuleId).ToHashSet();

            Assert.AreEqual(330, withEffect.Count);
            Assert.IsTrue(ItemModuleSeed.Classes.Where(row => row.VariantId != 0 && row.ItemTemplateId != 0).All(row => withEffect.Contains(row.Id)));
        }

        [TestMethod]
        public void AMigratedWorldHasTheDebuffRowsAndOneTakenBackHasNot()
        {
            const string count = "select count(*) from module_effect";

            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                migrator.Migrate(WorldBefore);
                Assert.AreEqual("290", Rows(context, count).Single());
                Assert.AreEqual(Migration, context.Database.GetPendingMigrations().First());

                migrator.Migrate(Migration);
                Assert.AreEqual("330", Rows(context, count).Single());

                string Effect(ModuleEffectEntry row) => $"{row.Id}|{row.ModuleId}|{row.EffectId}|{row.SetLevel}|{row.FlatValue:R}|{row.LinearValue:R}|{row.ExpValue:R}|{row.Arg1}|{row.Arg2}|{row.Arg3}|{row.Arg4}";

                var effects = new ItemModuleRepository(context).GetModuleEffects();

                CollectionAssert.AreEqual(
                    ItemModuleSeed.Effects.Concat(ItemModuleSeed.DebuffEffects).Select(Effect).ToList(),
                    effects.Select(Effect).ToList(), "in the order of their ids");

                // What the game server makes of them: the module has its line, with the seconds in it.
                ItemModules.Load(new ItemModuleRepository(context).GetModuleClasses(), effects);
                Assert.IsTrue(ItemModules.TryGet(DebuffFire1, out var module));

                var line = module.Effects.Single();
                Assert.AreEqual(112u, line.EffectId);
                Assert.AreEqual(-10, line.Amount(ItemLevel));
                Assert.AreEqual(10, line.Arg2);

                migrator.Migrate(WorldBefore);
                Assert.AreEqual("290", Rows(context, count).Single());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        #endregion

        #region What is counted

        [TestMethod]
        public void TheModulesOfTheArmorWornAndTheWeaponInHandAddUp()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);

            Assert.AreSame(ModuleTotals.None, ItemModuleBonuses.Of(client.Player), "nothing on");

            var helmet = Wear(context, client, EquipmentData.Helmet, Body1, Health1, ResistFire1, ArmorPierce5);
            var vest = Wear(context, client, EquipmentData.Torso, Body1, ResistFire5, Armor1, Experience5);
            var weapon = Hold(context, client, Crit5, Threat5, WeaponPierce5, WeaponResistFire1);

            var totals = ItemModuleBonuses.Of(client.Player);

            Assert.AreEqual(6, totals.Body, "3 a piece");
            Assert.AreEqual(42, totals.Health);
            Assert.AreEqual(5, totals.CritChance);
            Assert.AreEqual(-50, totals.ThreatPercent);
            Assert.AreEqual(15, totals.ArmorPierce, "the weapon's 10 and the helmet's 5");
            Assert.AreEqual(5, totals.ExperiencePercent);
            Assert.AreEqual(3 + 15 + 6, totals.Resist[DamageType.Fire], "armor and weapon alike");
            Assert.AreEqual(42, totals.ArmorOf[vest.EntityId], "the piece's own");
            Assert.IsFalse(totals.ArmorOf.ContainsKey(helmet.EntityId));
            Assert.AreEqual(0, totals.Procs.Count);

            // An item of another level: the amount is the item's.
            helmet.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = 50;
            Assert.AreEqual(3 + 4, ItemModuleBonuses.Of(client.Player).Body);
            Assert.AreEqual(559, ItemModuleBonuses.Of(client.Player).Health);

            // The weapon put away: its modules with it.
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Weapon] = 0;
            totals = ItemModuleBonuses.Of(client.Player);
            Assert.AreEqual(0, totals.CritChance);
            Assert.AreEqual(5, totals.ArmorPierce);
            Assert.AreEqual(18, totals.Resist[DamageType.Fire]);

            // A weapon that is only in the drawer counts for nothing.
            client.Player.Inventory.WeaponDrawer = new List<ulong> { weapon.EntityId, 0, 0, 0, 0 };
            Assert.AreEqual(0, ItemModuleBonuses.Of(client.Player).CritChance);

            // Nor anyone who is not a player.
            Assert.AreSame(ModuleTotals.None, ItemModuleBonuses.Of(new Creature()));
            Assert.AreSame(ModuleTotals.None, ItemModuleBonuses.Of(null));
        }

        [TestMethod]
        public void ABrokenItemsModulesDoNothing()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var helmet = Wear(context, client, EquipmentData.Helmet, Body1);
            var weapon = Hold(context, client, Crit5, StealHealth1);

            Assert.AreEqual(3, ItemModuleBonuses.Of(client.Player).Body);
            Assert.AreEqual(1, ItemModuleBonuses.ProcsOf(client.Player).Count);

            helmet.CurrentHitPoints = 0;
            weapon.CurrentHitPoints = 0;

            var totals = ItemModuleBonuses.Of(client.Player);
            Assert.AreEqual(0, totals.Body);
            Assert.AreEqual(0, totals.CritChance);
            Assert.AreEqual(0, totals.Procs.Count);

            // Mended, they count again.
            helmet.CurrentHitPoints = 1;
            Assert.AreEqual(3, ItemModuleBonuses.Of(client.Player).Body);
        }

        [TestMethod]
        public void AStealOrADebuffIsTheWeaponsInHandAndOfNoOtherItem()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);

            // In a piece of armor - a game master's doing - it is no weapon's.
            Wear(context, client, EquipmentData.Helmet, StealHealth1, DebuffFire1);
            Assert.AreEqual(0, ItemModuleBonuses.ProcsOf(client.Player).Count);

            Hold(context, client, StealHealth1, StealArmor1, DebuffFire5, StealPower5);

            var procs = ItemModuleBonuses.ProcsOf(client.Player);

            Assert.AreEqual(4, procs.Count);
            Assert.AreEqual(Attributes.Health, procs[0].Steals);
            Assert.AreEqual(11, procs[0].Amount);
            Assert.AreEqual(Attributes.Armor, procs[1].Steals);
            Assert.AreEqual(32, procs[1].Amount, "6 and 5 doubling every 8 levels");
            Assert.IsNull(procs[2].Steals);
            Assert.AreEqual(DamageType.Fire, procs[2].Debuffs);
            Assert.AreEqual(313, procs[2].DebuffTypeId, "DEBUFF_FIRE_RESIST");
            Assert.AreEqual(-30, procs[2].Amount);
            Assert.AreEqual(10, procs[2].Seconds);
            Assert.AreEqual(Attributes.Power, procs[3].Steals);
            Assert.AreEqual(20, procs[3].Amount);
        }

        #endregion

        #region Attributes

        [TestMethod]
        public void BodyMindAndSpiritAreTheModifiedValueAndWhatIsDerivedFollowsIt()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var attributes = client.Player.Attributes;

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            var body = attributes[Attributes.Body].CurrentMax;
            var mind = attributes[Attributes.Mind].CurrentMax;
            var spirit = attributes[Attributes.Spirit].CurrentMax;
            var health = attributes[Attributes.Health].CurrentMax;
            var power = attributes[Attributes.Power].CurrentMax;
            var regen = attributes[Attributes.Regen].CurrentMax;

            Assert.AreEqual(body, attributes[Attributes.Body].NormalMax, "nothing on: base and modified are one");
            Assert.AreEqual(health, attributes[Attributes.Health].NormalMax);

            Wear(context, client, EquipmentData.Helmet, Body1, Mind1, Spirit5);
            ManifestationManager.Instance.UpdateStatsValues(client, true);

            Assert.AreEqual(body, attributes[Attributes.Body].NormalMax, "the base stays the base");
            Assert.AreEqual(body + 3, attributes[Attributes.Body].CurrentMax, "which the window shows as +3, in green");
            Assert.AreEqual(body + 3, attributes[Attributes.Body].Current);
            Assert.AreEqual(mind, attributes[Attributes.Mind].NormalMax);
            Assert.AreEqual(mind + 3, attributes[Attributes.Mind].CurrentMax);
            Assert.AreEqual(spirit, attributes[Attributes.Spirit].NormalMax);
            Assert.AreEqual(spirit + 10, attributes[Attributes.Spirit].CurrentMax);

            // A point of Body is a point of Body: health comes of Body and Spirit, the Regen
            // attribute of Mind and Spirit.
            Assert.IsTrue(attributes[Attributes.Health].CurrentMax > health, "more Body and Spirit, more health");
            Assert.AreEqual(attributes[Attributes.Health].CurrentMax, attributes[Attributes.Health].NormalMax, "and no Health module to set them apart");
            Assert.AreEqual(attributes[Attributes.Health].CurrentMax, attributes[Attributes.Health].Current, "a full reset fills it");
            Assert.IsTrue(attributes[Attributes.Regen].CurrentMax > regen);
            Assert.AreEqual(power, attributes[Attributes.Power].CurrentMax, "power comes of the level and the points spent");

            // Spirit over the level's base is crit chance, a percent per 15 points.
            Assert.AreEqual(CriticalHits.BaseChance + (spirit + 10 - CriticalHits.BaseSpirit(20)) / CriticalHits.SpiritDivisor,
                CriticalHits.AttackerChance(client.Player, true), 1e-9);
        }

        [TestMethod]
        public void HealthPowerAndRegenAreAddedToTheMaximumAndRegenerationToTheRate()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var attributes = client.Player.Attributes;

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            var health = attributes[Attributes.Health].CurrentMax;
            var power = attributes[Attributes.Power].CurrentMax;
            var regen = attributes[Attributes.Regen].CurrentMax;
            var rate = attributes[Attributes.Health].RefreshAmount;

            Assert.AreEqual(rate, attributes[Attributes.Power].RefreshAmount, "power regenerates at the health rate");
            Assert.AreEqual((int)Math.Round(2.0 * regen / 100), rate);

            Wear(context, client, EquipmentData.Helmet, Health1, Power1, Regen1);
            Wear(context, client, EquipmentData.Torso, HealthRegen1, PowerRegen5, ArmorRegen1);
            Hold(context, client, WeaponRegen1);

            attributes[Attributes.Health].Current = 10;
            attributes[Attributes.Power].Current = 10;
            ManifestationManager.Instance.UpdateStatsValues(client, false);

            Assert.AreEqual(health, attributes[Attributes.Health].NormalMax);
            Assert.AreEqual(health + 42, attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(10, attributes[Attributes.Health].Current, "more room, not more health: it regenerates into it");
            Assert.AreEqual(power + 6, attributes[Attributes.Power].CurrentMax);
            Assert.AreEqual(10, attributes[Attributes.Power].Current);

            // "Regen: 5%" from the helmet and 10% from the weapon in hand, on the attribute...
            Assert.AreEqual(regen, attributes[Attributes.Regen].NormalMax);
            Assert.AreEqual(regen + 15, attributes[Attributes.Regen].CurrentMax);

            // ...which is the rate health and power regenerate at; "Regen Health: 6 HP/sec" and
            // "Regen Power: 5 per sec" are a second's worth on top of it.
            var withRegen = (int)Math.Round(2.0 * (regen + 15) / 100);

            Assert.AreEqual(withRegen + 6, attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(withRegen + 5, attributes[Attributes.Power].RefreshAmount);
            Assert.AreEqual(CombatRegen.RegenPeriodSeconds, attributes[Attributes.Health].RefreshPeriod);

            // "Regen Armor: 6 HP/sec" with the armor's own 7 a piece - and none in combat, as before.
            Assert.AreEqual(7 + 7 + 6, client.Player.ArmorRegenRate);
            Assert.AreEqual(20, attributes[Attributes.Armor].RefreshAmount);

            client.Player.InCombat = true;
            ManifestationManager.Instance.UpdateStatsValues(client, false);
            Assert.AreEqual(0, attributes[Attributes.Armor].RefreshAmount);
            Assert.AreEqual(withRegen + 6, attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(CombatRegen.InCombatRegenPeriodSeconds, attributes[Attributes.Health].RefreshPeriod, "a fifth of it in combat, as the rest of the rate is");

            // Taken off, the maximum goes back and the bar is capped to it.
            client.Player.InCombat = false;
            attributes[Attributes.Health].Current = health + 42;
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Helmet] = 0;
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Torso] = 0;
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Weapon] = 0;
            ManifestationManager.Instance.UpdateStatsValues(client, false);

            Assert.AreEqual(health, attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(health, attributes[Attributes.Health].Current);
            Assert.AreEqual(power, attributes[Attributes.Power].CurrentMax);
            Assert.AreEqual(regen, attributes[Attributes.Regen].CurrentMax);
            Assert.AreEqual(rate, attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(rate, attributes[Attributes.Power].RefreshAmount);
            Assert.AreEqual(0, client.Player.ArmorRegenRate);
        }

        [TestMethod]
        public void TotalArmorIsThePiecesOwnAndTakesTheBodyBonusWithIt()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var attributes = client.Player.Attributes;
            var vest = Wear(context, client, EquipmentData.Torso);

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            // The piece's 100, with the percent Body gives armor.
            var multiplier = 1.0 + attributes[Attributes.Body].CurrentMax * 0.0066666;

            Assert.AreEqual((int)Math.Round(100 * multiplier), attributes[Attributes.Armor].CurrentMax);

            vest.SetModule(0, Armor1);
            ManifestationManager.Instance.UpdateStatsValues(client, true);
            Assert.AreEqual((int)Math.Round(142 * multiplier), attributes[Attributes.Armor].CurrentMax, "\"Total Armor: 42\"");
            Assert.AreEqual(attributes[Attributes.Armor].CurrentMax, attributes[Attributes.Armor].Current);

            // Worn down to half strength, the piece gives half of all it has.
            vest.CurrentHitPoints = 20;
            Assert.AreEqual(0.5, Durability.EffectivenessOf(vest), 1e-9);
            ManifestationManager.Instance.UpdateStatsValues(client, false);
            Assert.AreEqual((int)Math.Round(71 * multiplier), attributes[Attributes.Armor].CurrentMax);
        }

        #endregion

        #region Resistance

        [TestMethod]
        public void AResistModuleGoesIntoTheResistanceListAndComesOffThatDamage()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);

            Wear(context, client, EquipmentData.Helmet, ResistFire1, ResistStun5);
            Wear(context, client, EquipmentData.Torso, ResistFire5);
            Hold(context, client, WeaponResistFire1);
            WorldTestContext.Drain(client);

            ManifestationManager.Instance.SyncSkillPassives(client);

            var listed = client.Player.ResistanceData.Single();

            Assert.AreEqual(DamageType.Fire, listed.ResistanceType, "the stun's is no damage, and not in the list the window reads");
            Assert.AreEqual(24, listed.ResistanceAmmount);
            Assert.AreEqual(24, Sent(client).OfType<ResistanceDataPacket>().Single().ResistanceData.Single().ResistanceAmmount);

            // 24 points: damage / 1.48.
            Assert.AreEqual(68, GameEffectManager.ApplyResist(client.Player, 100, out var resisted, DamageType.Fire));
            Assert.AreEqual(32, resisted);
            Assert.AreEqual(100, GameEffectManager.ApplyResist(client.Player, 100, out _, DamageType.Ice));
        }

        [TestMethod]
        public void AResistToBeingStunnedOrKnockedBackIsAChanceWithGravitonArmors()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);

            Assert.AreEqual(0, PlayerCrowdControl.ResistPercent(client.Player, DamageType.Stun));

            Wear(context, client, EquipmentData.Helmet, ResistStun5, ResistKnockback1);
            Wear(context, client, EquipmentData.Torso, ResistStun5);

            Assert.AreEqual(30, ItemModuleBonuses.ControlResistPercent(client.Player, DamageType.Stun));
            Assert.AreEqual(3, ItemModuleBonuses.ControlResistPercent(client.Player, DamageType.KnockBack));
            Assert.AreEqual(0, ItemModuleBonuses.ControlResistPercent(client.Player, DamageType.Root));
            Assert.AreEqual(0, ItemModuleBonuses.ControlResistPercent(client.Player, DamageType.Fire), "a resistance to damage is no chance");
            Assert.AreEqual(30, PlayerCrowdControl.ResistPercent(client.Player, DamageType.Stun));
            Assert.AreEqual(3, PlayerCrowdControl.ResistPercent(client.Player, DamageType.KnockBack));

            // Graviton Armor's "Knockback / Stun Resist" is for both.
            client.Player.ActiveEffects[1] = new GameEffect { EffectId = 1, KnockbackStunResistPercent = 12 };
            Assert.AreEqual(42, PlayerCrowdControl.ResistPercent(client.Player, DamageType.Stun));
            Assert.AreEqual(15, PlayerCrowdControl.ResistPercent(client.Player, DamageType.KnockBack));

            client.Player.ActiveEffects[1].KnockbackStunResistPercent = 90;
            Assert.AreEqual(100, PlayerCrowdControl.ResistPercent(client.Player, DamageType.Stun), "at most all of them");
        }

        [TestMethod]
        public void AResistToBeingHeldSlowedOrBlindedTurnsTheEffectAway()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var asked = new List<double>();

            GameEffect Effect(Action<GameEffect> shape)
            {
                var effect = new GameEffect
                {
                    TypeId = 9000 + asked.Count,
                    EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                    SourceId = creature.EntityId,
                    Source = creature,
                    IsBuff = false,
                    ExpiresTick = Environment.TickCount64 + 60000
                };

                shape(effect);

                return effect;
            }

            bool Lands(GameEffect effect)
            {
                GameEffectManager.Instance.Attach(world.Map, client.Player, effect);

                return client.Player.ActiveEffects.ContainsKey(effect.EffectId);
            }

            ItemModuleBonuses.Roll = chance =>
            {
                asked.Add(chance);

                return chance > 0;
            };

            // Nothing on: everything lands, on a chance of none.
            Assert.IsTrue(Lands(Effect(e => e.IsRoot = true)));
            Assert.IsTrue(Lands(Effect(e => e.MovementModifierPercent = 50)));
            CollectionAssert.AreEqual(new double[] { 0, 0 }, asked);
            asked.Clear();

            Wear(world, client, EquipmentData.Helmet, ResistRoot5, ResistSnare5, ResistBlind5);
            Sent(client);

            // A root; a creature's web, which says what it is; a slow; a blinding; a creature's flash.
            var root = Effect(e => e.IsRoot = true);

            Assert.IsFalse(Lands(root));
            Assert.IsFalse(Lands(Effect(e => e.ControlKind = DamageType.Root)));
            Assert.IsFalse(Lands(Effect(e => e.MovementModifierPercent = 50)));
            Assert.IsFalse(Lands(Effect(e => e.Blinds = true)));
            Assert.IsFalse(Lands(Effect(e => e.ControlKind = DamageType.Blind)));
            CollectionAssert.AreEqual(new double[] { 15, 15, 15, 15, 15 }, asked, "the module's 15, as a chance in a hundred");

            // "Resisted" over the player, from the one who tried.
            var failed = Sent(client).OfType<GameEffectAttachFailedPacket>().ToList();

            Assert.AreEqual(5, failed.Count);
            Assert.AreEqual(root.TypeId, failed[0].EffectTypeId);
            Assert.AreEqual(GameEffectAttachFailedPacket.FailReason.Resist, failed[0].Reason);
            Assert.AreEqual(creature.EntityId, failed[0].SourceId);
            asked.Clear();

            // Not what is no such thing: a debuff of another kind, a stun (rolled where it is
            // made), a speed-up, a buff, something of their own, the world's.
            Assert.IsTrue(Lands(Effect(e => e.ResistModifier = -10)));
            Assert.IsTrue(Lands(Effect(e => { e.IsStun = true; e.IsRoot = true; })));
            Assert.IsTrue(Lands(Effect(e => e.MovementModifierPercent = 120)));
            Assert.IsTrue(Lands(Effect(e => { e.IsBuff = true; e.MovementModifierPercent = 50; })));
            Assert.IsTrue(Lands(Effect(e => { e.IsRoot = true; e.Source = client.Player; e.SourceId = client.Player.EntityId; })));
            Assert.IsTrue(Lands(Effect(e => { e.IsRoot = true; e.Environmental = true; })));
            Assert.AreEqual(0, asked.Count(chance => chance > 0));

            // And the roll that fails lets it on.
            ItemModuleBonuses.Roll = chance => false;
            Assert.IsTrue(Lands(Effect(e => e.Blinds = true)));
        }

        #endregion

        #region Crit, threat, piercing, speed, experience

        [TestMethod]
        public void CritChanceThreatAndArmorPiercingAreReadWhereTheyAreUsed()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), armor: 1000);

            Assert.AreEqual(CriticalHits.BaseChance, CriticalHits.AttackerChance(client.Player, false), 1e-9);
            Assert.AreEqual(100, Threat.ThreatModifierOf(client.Player));

            Wear(world, client, EquipmentData.Helmet, ArmorPierce5);
            Hold(world, client, Crit5, Threat5, WeaponPierce5);

            Assert.AreEqual(CriticalHits.BaseChance + 5, CriticalHits.AttackerChance(client.Player, false), 1e-9);
            Assert.AreEqual(CriticalHits.BaseChance + 5 + 3, CriticalHits.AttackerChance(client.Player, true, 3), 1e-9, "an ability's or a swing's too");
            Assert.AreEqual(50, Threat.ThreatModifierOf(client.Player), "\"Perceived Threat: -50%\"");
            Assert.AreEqual(15, ItemModuleBonuses.Of(client.Player).ArmorPierce);

            // 15% of a hit goes past the armor: constant fire brings it to the damage it deals.
            var landed = ActorManager.Instance.Damage(world.Map, creature, 200, client.Player, out _, DamageType.Physical, armorBypassPercent: 15);

            Assert.AreEqual(200, landed);
            Assert.AreEqual(1000 - 170, creature.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(10000 - 30, creature.Attributes[Attributes.Health].Current);

            // With Target Painting's on the target, and never more than all of it.
            creature.ActiveEffects[1] = new GameEffect { EffectId = 1, ArmorPiercePercent = 95 };
            ActorManager.Instance.Damage(world.Map, creature, 100, client.Player, out _, DamageType.Physical, armorBypassPercent: 15);
            Assert.AreEqual(1000 - 170, creature.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(10000 - 130, creature.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void AWeaponShotCarriesThePiercingAndWhatTheWeaponDoesOnAHit()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var client = context.Client;

            foreach (var attribute in new[] { Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);

            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Weapon] = context.Weapon.EntityId;
            client.Player.Rotation = Math.PI;

            var creature = Spawn(context.World, new Vector3(0, 0, -5), armor: 5000);

            client.Player.Target = creature.EntityId;
            context.Weapon.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = ItemLevel;
            context.Weapon.SetModule(0, WeaponPierce5);
            context.Weapon.SetModule(1, StealArmor1);
            context.Weapon.SetModule(2, DebuffFire1);
            ItemModuleBonuses.Roll = chance => true;

            Assert.IsTrue(manager.PlayerTryFireWeapon(client));

            var missile = context.World.Map.QueuedMissiles.Single();

            Assert.AreEqual(10, missile.ArmorBypassPercent, "the module's, with a skill's none");
            Assert.AreEqual(2, missile.WeaponProcs.Count);

            // It lands: the steal takes the creature's armor, the debuff goes on it.
            client.Player.Attributes[Attributes.Armor].Current = 50;
            missile.TriggerTime = 0;
            MissileManager.Instance.DoWork(context.World.Map, 1000);

            Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count);
            Assert.AreEqual(50 + 32, client.Player.Attributes[Attributes.Armor].Current, "the armor stolen");
            Assert.IsTrue(creature.Attributes[Attributes.Armor].Current < 5000 - 32, "after the shot's own share");

            var debuff = creature.ActiveEffects.Values.Single(effect => effect.TypeId == 313);

            Assert.AreEqual(-10, debuff.ResistModifier);
            Assert.AreEqual(DamageType.Fire, debuff.ResistDamageType);

            // A weapon with none carries nothing.
            for (var slot = 0; slot < ItemModules.Slots; slot++)
                context.Weapon.SetModule(slot, 0);

            client.Player.NextShotAt = 0;
            Assert.IsTrue(manager.PlayerTryFireWeapon(client));
            Assert.AreEqual(0, context.World.Map.QueuedMissiles.Single().ArmorBypassPercent);
            Assert.IsNull(context.World.Map.QueuedMissiles.Single().WeaponProcs);
        }

        [TestMethod]
        public void MovementSpeedIsAStandingEffectOnlyTheServerKnowsOf()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);

            Assert.IsNull(ItemModuleBonuses.MovementPassive(world.Map, client.Player));

            Wear(world, client, EquipmentData.Helmet, MoveSpeed5);
            Wear(world, client, EquipmentData.Torso, MoveSpeed5);
            WorldTestContext.Drain(client);

            ManifestationManager.Instance.SyncSkillPassives(client);

            var effect = client.Player.ActiveEffects.Values.Single(e => e.TypeId == ItemModuleBonuses.MovementTypeId);

            Assert.AreEqual(110, effect.MovementModifierPercent);
            Assert.IsTrue(effect.ServerOnly && effect.IsSkillPassive);
            Assert.AreEqual(1.10, client.Player.MovementSpeed, 1e-9, "what the move check measures against");

            var sent = Sent(client);

            Assert.AreEqual(1.10, sent.OfType<MovementModChangePacket>().Last().MovementMod, 1e-9);
            Assert.IsFalse(sent.OfType<GameEffectAttachedPacket>().Any(packet => packet.EffectTypeId == ItemModuleBonuses.MovementTypeId), "nobody is told of the effect");

            // Run again, it is one effect still; with the modules gone, none, and the speed back.
            ManifestationManager.Instance.SyncSkillPassives(client);
            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Count(e => e.TypeId == ItemModuleBonuses.MovementTypeId));
            Assert.AreEqual(1.10, client.Player.MovementSpeed, 1e-9);

            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Torso] = 0;
            ManifestationManager.Instance.SyncSkillPassives(client);
            Assert.AreEqual(1.05, client.Player.MovementSpeed, 1e-9);

            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Helmet] = 0;
            ManifestationManager.Instance.SyncSkillPassives(client);
            Assert.AreEqual(0, client.Player.ActiveEffects.Values.Count(e => e.TypeId == ItemModuleBonuses.MovementTypeId));
            Assert.AreEqual(1.0, client.Player.MovementSpeed, 1e-9);
        }

        [TestMethod]
        public void AKillsExperienceIsEachSharersOwnWithTheirOwnModules()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));
            var mate = context.CreateAdditionalClient(2);
            var manifestations = new ManifestationManager(context);

            mate.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            Equip(mate, EquipmentData.Helmet, Gear(ArmorClass, ItemLevel, Experience5));
            Equip(mate, EquipmentData.Torso, Gear(ArmorClass, ItemLevel, Experience5));

            Assert.AreEqual(1100u, ItemModuleBonuses.WithExperience(mate.Player, 1000));
            Assert.AreEqual(1000u, ItemModuleBonuses.WithExperience(context.Client.Player, 1000));
            Assert.AreEqual(0u, ItemModuleBonuses.WithExperience(mate.Player, 0));
            Assert.AreEqual(11u, ItemModuleBonuses.WithExperience(mate.Player, 10));
            Assert.AreEqual(6u, ItemModuleBonuses.WithExperience(mate.Player, 5), "rounded to the nearest");

            MissionTestContext.Drain(context.Client);
            MissionTestContext.Drain(mate);

            // Half each, and the mate's ten percent on the mate's half alone.
            Assert.AreEqual(50u, KillShares.AwardExperience(manifestations, new[] { context.Client, mate }, context.Client, 100), "the part before anyone's modules");
            Assert.AreEqual(50u, context.Client.Player.Experience);
            Assert.AreEqual(55u, mate.Player.Experience);
            Assert.AreEqual(55u, MissionTestContext.Drain(mate).OfType<ExperienceChangedPacket>().Single().XPInfo.Gained);
        }

        #endregion

        #region On a hit

        [TestMethod]
        public void AStealTakesWhatTheTargetHasAndGivesItToTheShooter()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), armor: 500);
            var asked = new List<double>();

            Hold(world, client, StealHealth1, StealArmor1, StealPower5, StealAdrenaline1);
            client.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 100, 100, 0, 0, 0);
            creature.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0);

            void Hurt()
            {
                client.Player.Attributes[Attributes.Health].Current = 40;
                client.Player.Attributes[Attributes.Armor].Current = 40;
                client.Player.Attributes[Attributes.Power].Current = 40;
            }

            // The chance is the setting's, asked once for each steal the weapon carries.
            ItemModuleBonuses.Config = new ItemModulesConfig { StealChancePercent = 37.5, ResistDebuffChancePercent = 1 };
            ItemModuleBonuses.Roll = chance =>
            {
                asked.Add(chance);

                return false;
            };

            Hurt();
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);
            CollectionAssert.AreEqual(new[] { 37.5, 37.5, 37.5, 37.5 }, asked);
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current, "no roll came up");
            Assert.AreEqual(40, client.Player.Attributes[Attributes.Health].Current);

            // Every roll comes up.
            ItemModuleBonuses.Roll = chance => true;
            Sent(client);
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);

            // Health 11, as damage: the creature's armor takes it first, and the shooter is
            // healed by what it lost. Armor 32, bar to bar.
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);
            Assert.AreEqual(500 - 11 - 32, creature.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(40 + 11, client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(40 + 32, client.Player.Attributes[Attributes.Armor].Current);

            // A creature has no power and no adrenaline: nothing to take, nothing given.
            Assert.AreEqual(40, client.Player.Attributes[Attributes.Power].Current);
            Assert.AreEqual(0, client.Player.Attributes[Attributes.Chi].Current);

            // Shown by the client's vampiric damage effect, on the target from the shooter.
            var announced = Sent(client).OfType<GameEffectAnnounceVampPacket>().ToList();
            Assert.AreEqual(2, announced.Count, "the two that took something");

            // From a player it is theirs to lose (.vamp's rule, VampiricDamage): power to power.
            var victim = Watch(world, 3);
            victim.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 100, 100, 25, 0, 0);
            Hurt();
            VampiricDamage.Steal(world.Map, client.Player, victim.Player, Attributes.Power, 20);
            VampiricDamage.Steal(world.Map, client.Player, victim.Player, Attributes.Chi, 10);
            Assert.AreEqual(80, victim.Player.Attributes[Attributes.Power].Current);
            Assert.AreEqual(60, client.Player.Attributes[Attributes.Power].Current);
            Assert.AreEqual(15, victim.Player.Attributes[Attributes.Chi].Current);
            Assert.AreEqual(10, client.Player.Attributes[Attributes.Chi].Current);
        }

        [TestMethod]
        public void WhatIsKilledPartWayIsLeftAloneByTheRestOfTheWeaponsModules()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), armor: 500);
            var rolls = 0;

            // A steal of health is damage and can be what kills: here the first roll is.
            Hold(world, client, StealHealth1, DebuffFire1, StealArmor1);
            ItemModuleBonuses.Roll = chance =>
            {
                rolls++;
                creature.State = CharacterState.Dead;

                return false;
            };

            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);

            Assert.AreEqual(1, rolls, "nothing is rolled for the dead");
            Assert.AreEqual(500, creature.Attributes[Attributes.Armor].Current);
            Assert.IsFalse(creature.ActiveEffects.Values.Any(effect => effect.TypeId == 313));

            // Nor is anything, for anyone already down, for oneself, or with no weapon module.
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, client.Player);
            ItemModuleBonuses.OnWeaponHit(world.Map, creature, client.Player);
            ItemModuleBonuses.OnWeaponHit(null, client.Player, creature);
            Assert.AreEqual(1, rolls);
        }

        [TestMethod]
        public void AResistDebuffCutsThatResistanceOfTheTargetForItsSeconds()
        {
            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var asked = new List<double>();
            var weapon = Hold(world, client, DebuffFire1, DebuffPhysical3);

            ItemModuleBonuses.Config = new ItemModulesConfig { StealChancePercent = 1, ResistDebuffChancePercent = 22 };
            ItemModuleBonuses.Roll = chance =>
            {
                asked.Add(chance);

                return true;
            };

            Sent(client);

            var before = Environment.TickCount64;

            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);

            CollectionAssert.AreEqual(new double[] { 22, 22 }, asked, "the setting's chance, once for each");

            var fire = creature.ActiveEffects.Values.Single(effect => effect.TypeId == 313);
            var physical = creature.ActiveEffects.Values.Single(effect => effect.TypeId == 312);

            Assert.AreEqual(-10, fire.ResistModifier);
            Assert.AreEqual(DamageType.Fire, fire.ResistDamageType);
            Assert.IsFalse(fire.IsBuff);
            Assert.AreSame(client.Player, fire.Source);
            Assert.AreEqual(-10, fire.Tooltip["amount"], "\"Resist Fire: -10%\"");
            Assert.IsTrue(fire.ExpiresTick >= before + 10000 && fire.ExpiresTick <= Environment.TickCount64 + 10000, "ten seconds, the row's arg2");
            Assert.AreEqual(-20, physical.ResistModifier);

            // The clients are shown it on the target: "Debuff: Resist - Fire".
            var attached = Sent(client).OfType<GameEffectAttachedPacket>().ToList();
            CollectionAssert.AreEquivalent(new[] { 312, 313 }, attached.Select(packet => packet.EffectTypeId).ToList());

            // Ten points down is a tenth more of that damage, from anyone; no other kind's.
            Assert.AreEqual(110, GameEffectManager.ApplyResist(creature, 100, out _, DamageType.Fire));
            Assert.AreEqual(120, GameEffectManager.ApplyResist(creature, 100, out _, DamageType.Physical));
            Assert.AreEqual(100, GameEffectManager.ApplyResist(creature, 100, out _, DamageType.Ice));

            // Again: the same one anew, not a second.
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);
            Assert.AreEqual(1, creature.ActiveEffects.Values.Count(effect => effect.TypeId == 313));
            Assert.AreNotEqual(fire.EffectId, creature.ActiveEffects.Values.Single(effect => effect.TypeId == 313).EffectId, "the clock starts over");

            // A stronger one takes its place; a weaker one after it does not.
            weapon.SetModule(0, DebuffFire5);
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);

            var deeper = creature.ActiveEffects.Values.Single(effect => effect.TypeId == 313);
            Assert.AreEqual(-30, deeper.ResistModifier);

            weapon.SetModule(0, DebuffFire1);
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);
            Assert.AreSame(deeper, creature.ActiveEffects.Values.Single(effect => effect.TypeId == 313));
            Assert.AreEqual(130, GameEffectManager.ApplyResist(creature, 100, out _, DamageType.Fire));

            // Against resistance it takes points off before it is a weakness: a player with 24.
            var victim = Watch(world, 3);
            victim.Player.ResistanceData = new List<ResistanceData> { new ResistanceData(DamageType.Fire, 24) };
            Assert.AreEqual(68, GameEffectManager.ApplyResist(victim.Player, 100, out _, DamageType.Fire));
            victim.Player.ActiveEffects[7] = new GameEffect { EffectId = 7, ResistDamageType = DamageType.Fire, ResistModifier = -30 };
            Assert.AreEqual(106, GameEffectManager.ApplyResist(victim.Player, 100, out _, DamageType.Fire));
        }

        [TestMethod]
        public void TheChancesAreTheSettingsAndNoneIsNever()
        {
            Assert.AreEqual(10, new ItemModulesConfig().StealChancePercent);
            Assert.AreEqual(10, new ItemModulesConfig().ResistDebuffChancePercent);
            Assert.AreEqual(10, new RasaGame::Rasa.Config.Config().ItemModules.StealChancePercent, "a file without the section");

            Assert.IsFalse(ItemModuleBonuses.DefaultRoll(0));
            Assert.IsFalse(ItemModuleBonuses.DefaultRoll(-5));
            Assert.IsTrue(ItemModuleBonuses.DefaultRoll(100));
            Assert.IsTrue(ItemModuleBonuses.DefaultRoll(250));

            var hits = Enumerable.Range(0, 20000).Count(_ => ItemModuleBonuses.DefaultRoll(10));
            Assert.IsTrue(hits > 1700 && hits < 2300, $"one in ten, near enough: {hits} of 20000");

            using var world = new WorldTestContext();
            var client = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), armor: 500);

            Hold(world, client, StealArmor1, DebuffFire1);
            ItemModuleBonuses.Config = new ItemModulesConfig { StealChancePercent = 0, ResistDebuffChancePercent = 0 };

            for (var hit = 0; hit < 200; hit++)
                ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);

            Assert.AreEqual(500, creature.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(0, creature.ActiveEffects.Count);

            ItemModuleBonuses.Config = new ItemModulesConfig { StealChancePercent = 100, ResistDebuffChancePercent = 100 };
            ItemModuleBonuses.OnWeaponHit(world.Map, client.Player, creature);
            Assert.AreEqual(500 - 32, creature.Attributes[Attributes.Armor].Current);
            Assert.IsTrue(creature.ActiveEffects.Values.Any(effect => effect.TypeId == 313));
        }

        #endregion

        #region Told when it changes

        [TestMethod]
        public void AChangeOfEquipmentIsLookedAtOnTheNextTickAndToldWhenItMatters()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var attributes = client.Player.Attributes;

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            var body = attributes[Attributes.Body].CurrentMax;

            WorldTestContext.Drain(client);

            // Nothing marked, nothing looked at.
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.AreEqual(0, Sent(client).Count);

            // Marked, with nothing changed: looked at, and nothing to tell.
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.IsFalse(client.Player.ModulesChanged);
            Assert.AreEqual(0, Sent(client).Count);

            // Still loading into the map: the mark is kept for when they are in it.
            client.Player.ModulesChanged = true;
            client.State = ClientState.Loading;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.IsTrue(client.Player.ModulesChanged);
            client.State = ClientState.Ingame;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.IsFalse(client.Player.ModulesChanged);

            // A weapon whose modules the client is shown nothing of: nothing to tell either.
            Hold(context, client, Crit5, StealHealth1);
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.AreEqual(0, Sent(client).Count);

            // A piece with Body and a resistance: the attributes, the bars and the resistances.
            Wear(context, client, EquipmentData.Helmet, Body1, ResistFire1);
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);

            var sent = Sent(client);

            Assert.AreEqual(body + 3, attributes[Attributes.Body].CurrentMax);
            Assert.AreEqual(1, sent.OfType<AttributeInfoPacket>().Count());
            Assert.AreEqual(1, sent.OfType<UpdateArmorPacket>().Count());
            Assert.AreEqual(3, sent.OfType<ResistanceDataPacket>().Single().ResistanceData.Single().ResistanceAmmount);
            Assert.AreEqual(3, client.Player.ResistanceData.Single().ResistanceAmmount);

            // Looked at again with nothing new: nothing.
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.AreEqual(0, Sent(client).Count);

            // A run speed module: the speed, by the standing effect, and no other effect touched.
            client.Player.ActiveEffects[77] = new GameEffect { EffectId = 77, TypeId = 77, IsSkillPassive = true };
            Wear(context, client, EquipmentData.Torso, MoveSpeed5);
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.AreEqual(1.05, client.Player.MovementSpeed, 1e-9);
            Assert.AreEqual(1.05, Sent(client).OfType<MovementModChangePacket>().Single().MovementMod, 1e-9);
            Assert.IsTrue(client.Player.ActiveEffects.ContainsKey(77), "a skill's standing effect stays where it is");

            // Taken off again: told again.
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Helmet] = 0;
            client.Player.Inventory.EquippedInventory[(int)EquipmentData.Torso] = 0;
            client.Player.ModulesChanged = true;
            ItemModuleBonuses.Worker(context.World.Map);
            Assert.AreEqual(body, attributes[Attributes.Body].CurrentMax);
            Assert.AreEqual(1.0, client.Player.MovementSpeed, 1e-9);

            sent = Sent(client);
            Assert.AreEqual(0, sent.OfType<ResistanceDataPacket>().Single().ResistanceData.Count);
            Assert.AreEqual(1.0, sent.OfType<MovementModChangePacket>().Single().MovementMod, 1e-9);
        }

        #endregion

        #region Fixtures

        private static List<string> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        /// <summary>A player of that level with every attribute, the slots of a whole character, and nothing on, standing in the map's cells.</summary>
        private static Client Player(ProgressionTestContext context, byte level)
        {
            var client = context.CreateClient(level);
            var map = context.World.Map;
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);

            client.Player.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            client.Player.State = CharacterState.Normal;
            WorldTestContext.Drain(client);

            return client;
        }

        /// <summary>
        /// A piece of armor or a weapon of item level 20 with these modules: a class with 100 hit
        /// points - the armor's with 7 regeneration - and a template giving 100 armor.
        /// </summary>
        private Item Gear(uint classId, int level, params uint[] modules)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (!classes.ContainsKey((EntityClasses)classId))
                classes.Add((EntityClasses)classId, new EntityClass(classId, "fixture", 0, 0, new List<AugmentationType>(), true));

            var classInfo = classes[(EntityClasses)classId];

            classInfo.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1, MaxHitPoints = 100 });

            if (classId == ArmorClass)
                classInfo.ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { RegenRate = 7 });

            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = classId, ItemClass = classId }) { ArmorValue = 100 };

            template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = level;

            var item = new Item { ItemTemplate = template, ItemTemplateId = classId, StackSize = 1, CurrentHitPoints = 100 };

            for (var slot = 0; slot < modules.Length; slot++)
                item.SetModule(slot, modules[slot]);

            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            _items.Add(item);

            return item;
        }

        private static Item Equip(Client client, EquipmentData slot, Item item)
        {
            client.Player.Inventory.EquippedInventory[(int)slot] = item.EntityId;

            return item;
        }

        private Item Wear(ProgressionTestContext context, Client client, EquipmentData slot, params uint[] modules) =>
            Equip(client, slot, Gear(ArmorClass, ItemLevel, modules));

        private Item Wear(WorldTestContext world, Client client, EquipmentData slot, params uint[] modules) =>
            Equip(client, slot, Gear(ArmorClass, ItemLevel, modules));

        private Item Hold(ProgressionTestContext context, Client client, params uint[] modules) =>
            Equip(client, EquipmentData.Weapon, Gear(WeaponClass, ItemLevel, modules));

        private Item Hold(WorldTestContext world, Client client, params uint[] modules) =>
            Equip(client, EquipmentData.Weapon, Gear(WeaponClass, ItemLevel, modules));

        /// <summary>A hostile creature in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, Vector3 position, int health = 10000, int armor = 0)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, health, health, health, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, armor, armor, armor, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A player standing in the map's cells with a hundred of everything, so what is sent near them reaches them.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        #endregion
    }
}
