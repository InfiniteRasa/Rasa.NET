using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.World;

    /// <summary>
    /// Item modules: what a weapon, a piece of armor or a tool carries in its four module slots.
    ///
    /// What is here is what the client can be shown:
    ///  - an item has four slots, each empty or holding a module (Item.ModuleIds), kept with the
    ///    item's row (items.module_1 to module_4) and so through every hand it passes to;
    ///  - ItemInfo names them to the client (lootModuleIds), which names the item by them - a
    ///    Rifle that carries a Body module is a "Titan Rifle" - and asks for each one's tooltip;
    ///  - RequestTooltipForModuleId is answered from the world database's module_class and
    ///    module_effect (ModuleTooltipInfo), and the client draws a line of the item's tooltip
    ///    for each effect: "[1] Body: +4".
    ///
    /// And what the crafting station does with them (KraftwerksManager), by the client's own
    /// rules (shared/craftingnew.py) - the window works its buttons and its costs out from the
    /// same ones, so the server's have to be those to the unit:
    ///  - which items take modules, and which module goes into which (modifiable_class);
    ///  - which items are modules (module_item);
    ///  - what an item is salvaged for, and what it costs to put a module in.
    ///
    /// What is not: a module does nothing yet. No bonus is applied for one - the tooltip is the
    /// whole of it - and nothing that drops has any. A game master puts one on an item with
    /// ".module".
    /// </summary>
    public static class ItemModules
    {
        /// <summary>The module slots an item has: the client's MODULE_SLOTS.</summary>
        public const int Slots = ItemEntry.ModuleSlots;

        /// <summary>Mimeogel, "Mimeomech" to the player: what the module pages are paid in. MIMEOGEL_CLASSID and MIMEOGEL_TEMPLATEID.</summary>
        public const uint MimeogelClassId = 29908;
        public const uint MimeogelTemplateId = 123339;

        private static Dictionary<uint, ItemModule> _modules = new Dictionary<uint, ItemModule>();

        /// <summary>Item template to the module it is and its strength.</summary>
        private static Dictionary<uint, (uint ModuleId, uint Strength)> _items = new Dictionary<uint, (uint, uint)>();

        /// <summary>Item class to the class set it is a member of, for the classes that take modules.</summary>
        private static Dictionary<uint, uint> _classSets = new Dictionary<uint, uint>();

        public static int Count => _modules.Count;

        public static IEnumerable<ItemModule> All => _modules.Values;

        public static bool TryGet(uint moduleId, out ItemModule module)
        {
            return _modules.TryGetValue(moduleId, out module);
        }

        /// <summary>Loads the tables from the world database.</summary>
        public static void Init(IGameUnitOfWorkFactory factory)
        {
            using var unitOfWork = factory.CreateWorld();

            Load(unitOfWork.ItemModules.GetModuleClasses(), unitOfWork.ItemModules.GetModuleEffects());
            LoadCrafting(unitOfWork.ItemModules.GetModuleItems(), unitOfWork.ItemModules.GetModifiableClasses());

            Logger.WriteLog(LogType.Initialize, $"Loaded {_modules.Count} item modules with {_modules.Values.Sum(module => module.Effects.Count)} effects, {_items.Count} module items and {_classSets.Count} item classes that take modules");
        }

        /// <summary>The modules and what they do; an effect of no module is logged and left out.</summary>
        public static void Load(IEnumerable<ModuleClassEntry> classes, IEnumerable<ModuleEffectEntry> effects)
        {
            var modules = classes.ToDictionary(entry => entry.Id, entry => new ItemModule(entry));

            foreach (var entry in effects.OrderBy(entry => entry.Id))
            {
                if (!modules.TryGetValue(entry.ModuleId, out var module))
                {
                    Logger.WriteLog(LogType.Error, $"module_effect {entry.Id}: module {entry.ModuleId} is not in module_class; skipped");
                    continue;
                }

                module.Effects.Add(new ModuleInfo(entry));
            }

            _modules = modules;
        }

        /// <summary>What the crafting station goes by: the items that are modules, and the item classes that take them.</summary>
        public static void LoadCrafting(IEnumerable<ModuleItemEntry> items, IEnumerable<ModifiableClassEntry> modifiableClasses)
        {
            _items = items.ToDictionary(entry => entry.Id, entry => (entry.ModuleId, entry.Strength));
            _classSets = modifiableClasses.ToDictionary(entry => entry.Id, entry => entry.ClassSetId);
        }

        /// <summary>The item's slots as its row has them.</summary>
        internal static void Read(Item item, ItemEntry row)
        {
            var modules = row.Modules;

            for (var slot = 0; slot < Slots; slot++)
                item.SetModule(slot, modules[slot]);
        }

        /// <summary>
        /// The level a module's bonus is worked out for: the item's level requirement, and 0 for
        /// an item with none - client.augmentations.item.GetReqLevel, which is what the tooltip
        /// of an item in a pack uses.
        /// </summary>
        public static int LevelOf(Item item)
        {
            var requirements = item?.ItemTemplate?.ItemInfo?.Requirements;

            return requirements != null && requirements.TryGetValue(RequirementsType.ReqXpLevel, out var level) ? level : 0;
        }

        /// <summary>The first empty slot, 0 to 3, or -1 when all four are full.</summary>
        public static int FreeSlot(Item item)
        {
            for (var slot = 0; slot < Slots; slot++)
                if (item.ModuleIds[slot] == 0)
                    return slot;

            return -1;
        }

        /// <summary>
        /// Puts a module in one of an item's slots, or empties the slot with 0: the item's row is
        /// written, and its owner's client is sent the item's ItemInfo again - which is what
        /// makes it name the item anew and ask for the module's tooltip (gameui.
        /// OnItemInfoReceived); Recv_ItemModuleModified only stores the list.
        /// </summary>
        public static void Set(Client client, Item item, int slot, uint moduleId, IGameUnitOfWorkFactory factory)
        {
            item.SetModule(slot, moduleId);
            Save(client, item, factory);
        }

        /// <summary>Empties every slot of the item.</summary>
        public static void Clear(Client client, Item item, IGameUnitOfWorkFactory factory)
        {
            for (var slot = 0; slot < Slots; slot++)
                item.SetModule(slot, 0);

            Save(client, item, factory);
        }

        private static void Save(Client client, Item item, IGameUnitOfWorkFactory factory)
        {
            using (var unitOfWork = factory.CreateChar())
                unitOfWork.Items.UpdateModules(item);

            var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);

            if (client != null && classInfo != null)
                client.CallMethod(item.EntityId, new ItemInfoPacket(item, classInfo));
        }

        #region The crafting station's rules (shared/craftingnew.py)

        /// <summary>What an integration cannot be done for.</summary>
        public enum IntegrationProblem
        {
            None,

            /// <summary>The item to put in is not a module.</summary>
            NotAModule,

            /// <summary>The item to put it into is of no class that takes modules.</summary>
            NotModifiable,

            /// <summary>Its four slots are full.</summary>
            NoEmptySlot,

            /// <summary>The module is for another kind of item: an armor module and a weapon.</summary>
            WrongKindOfItem,

            /// <summary>The item already has a module of that variant.</summary>
            SameKindPresent
        }

        /// <summary>The class set an item's class is a member of, 0 for an item that takes no modules.</summary>
        public static uint ClassSetOf(Item item)
        {
            return item?.ItemTemplate != null && _classSets.TryGetValue((uint)item.ItemTemplate.Class, out var classSetId) ? classSetId : 0;
        }

        /// <summary>IsModifiableItemClass: the item is of a class that takes modules.</summary>
        public static bool IsModifiable(Item item) => ClassSetOf(item) != 0;

        /// <summary>The module an item is, and its strength, when its template is a module's (IsModuleItemTemplate).</summary>
        public static bool TryGetModuleItem(Item item, out ItemModule module, out uint strength)
        {
            module = null;
            strength = 0;

            if (item?.ItemTemplate == null || !_items.TryGetValue(item.ItemTemplate.ItemTemplateId, out var row))
                return false;

            strength = row.Strength;

            return _modules.TryGetValue(row.ModuleId, out module);
        }

        /// <summary>
        /// GetEquipmentTotalSalvageValue: the Mimeogel an item is salvaged for, 0 for one that
        /// cannot be.
        ///  - a module, or a piece of salvage: its module's salvage gain, for each of the stack;
        ///  - a weapon, a piece of armor or a tool: 5, 10, 25 or 100 for its quality - nothing
        ///    for Normal - and the salvage gain of each module in it, the whole of it times a
        ///    fifth of its level requirement, rounded down, and at least once.
        /// </summary>
        public static uint SalvageValue(Item item)
        {
            if (item?.ItemTemplate == null)
                return 0;

            if (!IsModifiable(item))
                return TryGetModuleItem(item, out var itself, out _) ? itself.SalvageGain * item.StackSize : 0;

            var total = (LootQuality)item.ItemTemplate.QualityId switch
            {
                LootQuality.Uncommon => 5u,
                LootQuality.Rare => 10u,
                LootQuality.Epic => 25u,
                LootQuality.Legendary => 100u,
                _ => 0u
            };

            foreach (var moduleId in item.ModuleIds)
                if (moduleId != 0 && _modules.TryGetValue(moduleId, out var module))
                    total += module.SalvageGain;

            return total * (uint)Math.Max(1, LevelOf(item) / 5);
        }

        /// <summary>CanIntegrateModuleIntoItem: why a module item cannot be put into an item, or None.</summary>
        public static IntegrationProblem CanIntegrate(Item moduleItem, Item target)
        {
            if (!TryGetModuleItem(moduleItem, out var module, out _))
                return IntegrationProblem.NotAModule;

            var classSetId = ClassSetOf(target);

            if (classSetId == 0)
                return IntegrationProblem.NotModifiable;

            if (FreeSlot(target) < 0)
                return IntegrationProblem.NoEmptySlot;

            if (module.ClassSetId != classSetId)
                return IntegrationProblem.WrongKindOfItem;

            if (module.VariantId != 0)
                foreach (var moduleId in target.ModuleIds)
                    if (moduleId != 0 && _modules.TryGetValue(moduleId, out var present) && present.VariantId == module.VariantId)
                        return IntegrationProblem.SameKindPresent;

            return IntegrationProblem.None;
        }

        /// <summary>
        /// GetModuleIntegrationMimeogelCost: the Mimeogel it costs to put a module item into an
        /// item, as the window prints it beside each empty slot. 0 when it cannot be put in -
        /// and for the weakest module in the lowest item, which the client's arithmetic rounds
        /// down to nothing.
        ///
        ///     int(quality * (2^n + 2n) * int(1000 * 2^(level / 5 - 10) + 0.5) * strength / 5)
        ///
        /// quality is 1 for a Normal item to 5 for a Legendary one, n the modules already in
        /// it, level its level requirement with level / 5 rounded down, strength the module's.
        ///
        /// The 2n is the client's and is not what its code set out to do: for each module in
        /// the item it means to add a fifth of that module's strength, looks the module's item
        /// template up among the module ids, where no item template is, and adds 2 instead.
        /// The window shows the cost that makes, so it is the cost.
        /// </summary>
        public static uint IntegrationCost(Item moduleItem, Item target)
        {
            if (CanIntegrate(moduleItem, target) != IntegrationProblem.None)
                return 0;

            TryGetModuleItem(moduleItem, out _, out var strength);

            var baseCost = (LootQuality)target.ItemTemplate.QualityId switch
            {
                LootQuality.Normal => 1.0,
                LootQuality.Uncommon => 2.0,
                LootQuality.Rare => 3.0,
                LootQuality.Epic => 4.0,
                LootQuality.Legendary => 5.0,
                LootQuality.Junk => 1.0,
                _ => 0.0
            };

            var moduleMultiplier = 1.0;
            var strengthMultiplier = 0.0;

            foreach (var moduleId in target.ModuleIds)
            {
                if (moduleId == 0)
                    continue;

                moduleMultiplier *= 2;

                // "if moduleTemplateId in moduleClassTable.keys()": true only for a module whose
                // item template is also some module's id.
                if (_modules.TryGetValue(moduleId, out var present) && _modules.ContainsKey(present.ItemTemplateId)
                    && _items.TryGetValue(present.ItemTemplateId, out var presentItem))
                    strengthMultiplier += presentItem.Strength / 5.0;
                else
                    strengthMultiplier += 2;
            }

            var itemMultiplier = moduleMultiplier + strengthMultiplier;
            var itemBracket = LevelOf(target) / 5;
            var levelMultiplier = (int)(1000 * Math.Pow(2, itemBracket - 10) + 0.5);
            var integrationMultiplier = strength / 5.0;

            return (uint)(baseCost * itemMultiplier * levelMultiplier * integrationMultiplier);
        }

        /// <summary>
        /// GetModuleItemUpgradeData: the module a module item is upgraded to - the same kind,
        /// one strength up - or null for one that cannot be: an Exceptional one, a piece of
        /// salvage, anything that is no module.
        /// </summary>
        public static ItemModule UpgradeOf(Item moduleItem)
        {
            if (!TryGetModuleItem(moduleItem, out var module, out _) || module.UpgradeModuleId == 0 || module.UpgradeCost == 0)
                return null;

            return _modules.TryGetValue(module.UpgradeModuleId, out var upgrade) && upgrade.ItemTemplateId != 0 && _items.ContainsKey(upgrade.ItemTemplateId)
                ? upgrade
                : null;
        }

        /// <summary>The quality the client draws a module item with: one above its strength.</summary>
        public static uint QualityOfModuleItem(ItemModule module)
        {
            return 1 + (_items.TryGetValue(module.ItemTemplateId, out var row) ? row.Strength : module.Level);
        }

        #endregion

        /// <summary>RequestTooltipForModuleId: answered for any id, with nothing for one that is not a module.</summary>
        public static void RequestTooltip(Client client, int moduleId)
        {
            ItemModule module = null;

            if (moduleId > 0)
                _modules.TryGetValue((uint)moduleId, out module);

            client.CallMethod(SysEntity.ClientGameUIManagerId, new ModuleTooltipInfoPacket((uint)moduleId, module));
        }

        /// <summary>One line for a game master: the module, what the client calls it, and what it comes to on the item.</summary>
        public static string Describe(uint moduleId, Item item)
        {
            if (!_modules.TryGetValue(moduleId, out var module))
                return $"{moduleId} (not in module_class)";

            var text = module.Comment.Length > 0 ? $"{moduleId} {module.Comment}" : moduleId.ToString();

            if (module.Effects.Count == 0)
                return $"{text} - no effect known";

            var level = LevelOf(item);
            var amounts = module.Effects.Select(effect =>
            {
                var amount = effect.Amount(level);

                return $"effect {effect.EffectId}: {(amount > 0 ? "+" : "")}{amount}";
            });

            return $"{text} - {string.Join(", ", amounts)} at item level {level}";
        }
    }
}
