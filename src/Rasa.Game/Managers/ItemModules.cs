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
    /// What is not: a module does nothing yet. No bonus is applied for one - the tooltip is the
    /// whole of it - nothing that drops has any, and the crafting station's pages for putting
    /// them in and taking them out are declined (KraftwerksManager). A game master puts one on
    /// an item with ".module".
    /// </summary>
    public static class ItemModules
    {
        /// <summary>The module slots an item has: the client's MODULE_SLOTS.</summary>
        public const int Slots = ItemEntry.ModuleSlots;

        private static Dictionary<uint, ItemModule> _modules = new Dictionary<uint, ItemModule>();

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

            Logger.WriteLog(LogType.Initialize, $"Loaded {_modules.Count} item modules with {_modules.Values.Sum(module => module.Effects.Count)} effects");
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
