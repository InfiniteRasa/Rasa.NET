using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Persistence;
    using Packets.Communicator.Server;
    using Packets.Crafting.Client;
    using Packets.Crafting.Server;
    using Structures;

    /// <summary>
    /// The crafting window's module pages: salvage, extraction, integration and upgrade.
    ///
    /// The client decides what each page offers from its own data, by shared/craftingnew.py -
    /// which button is lit, what the Mimeogel beside it says - and sends a request only for what
    /// it lit. The server goes by the same rules (ItemModules) and so refuses only what that
    /// window would not have asked for, or what changed between the look and the click:
    ///  - salvage destroys an item, or a stack of modules or salvage, for Mimeogel;
    ///  - extraction takes the module out of one slot of an item for a Mimeogel fee, and makes
    ///    the module an item again;
    ///  - integration puts a module item into an empty slot of an item of the kind it is for,
    ///    which has no module of that kind yet, for a Mimeogel fee;
    ///  - upgrade makes a module item the next strength of itself for a Mimeogel fee.
    /// Everything named has to be in the player's pack, and nothing rolls dice: the 1.16.5
    /// pages show a cost and no chance.
    ///
    /// What is made - Mimeogel, a module - waits at the station under "Items Created" as a
    /// fabricated item does, a finished job of the page that made it, and is created when it is
    /// taken. The item worked on never leaves the pack: its modules change where it is, and the
    /// client is sent its ItemInfo again. That is a choice of safe over faithful. A status entry
    /// has a place for the modules of what it shows (lootModuleIds), which Mimeogel and module
    /// items have no use for, so the item itself very likely went through "Items Created" too -
    /// but a job is in memory, and a weapon should not be somewhere a restart forgets.
    /// </summary>
    public partial class KraftwerksManager
    {
        /// <summary>The window's pages after fabrication: CRAFTACTION_SALVAGE, _EXTRACTION, _INSERTION and _UPGRADE.</summary>
        public const uint SalvagePage = 2;
        public const uint ExtractionPage = 3;
        public const uint IntegrationPage = 4;
        public const uint UpgradePage = 5;

        /// <summary>What the player is told Mimeogel is called: the client's own word for it.</summary>
        private const string Mimeogel = "Mimeomech";

        /// <summary>
        /// An item a request names, when it is in the player's pack and theirs to use up. Null
        /// otherwise, with the request failed: the window only offers what is in the pack
        /// (CheckInputSlots), so anything else is not a request it made.
        /// </summary>
        private Item PackItem(Client client, DynamicObject station, ulong entityId, string request)
        {
            var item = entityId != 0 && client.Player.Inventory.PersonalInventory.Contains(entityId)
                ? EntityManager.Instance.GetItem(entityId)
                : null;

            if (item?.ItemTemplate == null)
            {
                Logger.WriteLog(LogType.Security, $"{request} from {client.Player.FamilyName} names item {entityId}, which is not in their inventory");
                Fail(client, station, null);
                return null;
            }

            if (MissionItemProtection.IsProtected(item, _gameUnitOfWorkFactory))
            {
                Fail(client, station, "A mission's item cannot be used at a crafting station.");
                return null;
            }

            return item;
        }

        /// <summary>Whether the station can hold one more finished item for the player; the request is failed when not.</summary>
        private bool HasRoom(Client client, DynamicObject station)
        {
            var jobs = JobsFor(client, station);

            if (jobs.Count >= MaxJobsPerStation)
            {
                Fail(client, station, "This station is holding too many finished items for you; take some first.");
                return false;
            }

            if (jobs.Any(job => !job.IsFinished))
            {
                Fail(client, station, "This station is still working on something for you.");
                return false;
            }

            return true;
        }

        /// <summary>Whether the player carries the Mimeogel; the request is failed when not.</summary>
        private bool CanPay(Client client, DynamicObject station, uint cost)
        {
            var have = InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)ItemModules.MimeogelClassId);

            if (have >= cost)
                return true;

            Fail(client, station, $"You need {cost} {Mimeogel} and have {have}.");
            return false;
        }

        private void Pay(Client client, uint cost)
        {
            if (cost == 0)
                return;

            var missing = InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)ItemModules.MimeogelClassId, cost);

            if (missing != 0)
                Logger.WriteLog(LogType.Error, $"{client.Player.FamilyName}: {missing} of {cost} {Mimeogel} could not be taken after the count passed");
        }

        /// <summary>
        /// Takes items off a stack in the pack - all of it destroys the item - and says whether
        /// they went: ReduceStackCount refuses quietly.
        /// </summary>
        private static bool Consume(Client client, Item item, uint quantity)
        {
            var before = item.StackSize;
            var entityId = item.EntityId;

            InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, item, quantity);

            return quantity >= before
                ? !client.Player.Inventory.PersonalInventory.Contains(entityId)
                : item.StackSize == before - quantity;
        }

        /// <summary>A finished job for the player at the station: something to take, made when it is taken.</summary>
        private void Produce(Client client, DynamicObject station, ItemTemplate result, uint count, uint page, uint qualityId)
        {
            JobsForWriting(client, station).Add(new CraftingJob
            {
                ResultItemId = _nextJobId++,
                ResultClassId = (uint)result.Class,
                ResultItemTemplateId = result.ItemTemplateId,
                Count = count,
                CraftingPage = page,
                QualityId = qualityId,
                FinishTick = Environment.TickCount64,
                FinishReported = true
            });
        }

        private void Succeed(Client client, DynamicObject station)
        {
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(client.Player.EntityId));
        }

        /// <summary>RequestSalvageItem(kraftwerksId, itemId, CRAFTACTION_SALVAGE): the item, all of its stack, for Mimeogel.</summary>
        internal void RequestSalvageItem(Client client, RequestSalvageItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestSalvageItem");

            if (station == null)
                return;

            var item = PackItem(client, station, packet.ItemId, "RequestSalvageItem");

            if (item == null)
                return;

            var value = ItemModules.SalvageValue(item);

            if (value == 0)
            {
                Fail(client, station, "That item cannot be salvaged.");
                return;
            }

            var mimeogel = ItemManager.Instance.GetItemTemplateById(ItemModules.MimeogelTemplateId);

            if (mimeogel == null)
            {
                Fail(client, station, null);
                return;
            }

            if (!HasRoom(client, station))
                return;

            var templateId = item.ItemTemplate.ItemTemplateId;

            if (!Consume(client, item, item.StackSize))
            {
                Logger.WriteLog(LogType.Error, $"{client.Player.FamilyName}: item {item.Id} could not be taken for salvage");
                Fail(client, station, null);
                return;
            }

            Produce(client, station, mimeogel, value, SalvagePage, (uint)mimeogel.QualityId);
            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} salvages item {item.Id} (template {templateId}) for {value} {Mimeogel} at station {station.EntityId}");
            Succeed(client, station);
        }

        /// <summary>RequestExtractModule(kraftwerksId, itemId, slot, CRAFTACTION_EXTRACTION): the module in one slot, 0 to 3, out of the item and an item again.</summary>
        internal void RequestExtractModule(Client client, RequestExtractModulePacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestExtractModule");

            if (station == null)
                return;

            var item = PackItem(client, station, packet.ItemId, "RequestExtractModule");

            if (item == null)
                return;

            var moduleId = packet.Slot < ItemModules.Slots ? item.ModuleIds[(int)packet.Slot] : 0;

            if (moduleId == 0 || !ItemModules.IsModifiable(item))
            {
                Fail(client, station, "There is no module there to extract.");
                return;
            }

            // A cost of 0 is the client's "Cannot Extract": the Exceptional modules, and those
            // that are no item.
            var result = ItemModules.TryGet(moduleId, out var module) && module.ExtractCost != 0
                ? ItemManager.Instance.GetItemTemplateById(module.ItemTemplateId)
                : null;

            if (result == null)
            {
                Fail(client, station, "That module cannot be extracted.");
                return;
            }

            if (!CanPay(client, station, module.ExtractCost) || !HasRoom(client, station))
                return;

            Pay(client, module.ExtractCost);
            ItemModules.Set(client, item, (int)packet.Slot, 0, _gameUnitOfWorkFactory);
            Produce(client, station, result, 1, ExtractionPage, ItemModules.QualityOfModuleItem(module));
            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} extracts module {moduleId} from slot {packet.Slot} of item {item.Id} for {module.ExtractCost} {Mimeogel} at station {station.EntityId}");
            Succeed(client, station);
        }

        /// <summary>
        /// RequestIntegrateItem(kraftwerksId, targetItemId, moduleItemId, slot,
        /// CRAFTACTION_INSERTION): one of a stack of module items into the slot, 0 to 3, of the
        /// target. Nothing is made, so nothing waits at the station; the player is told
        /// "Crafting success!" and sees the item in their pack under its new name. The item is
        /// theirs by name too from then on: its tooltip reads "Modified By" and their family
        /// name, as a fabricated item reads its maker's.
        /// </summary>
        internal void RequestIntegrateItem(Client client, RequestIntegrateItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestIntegrateItem");

            if (station == null)
                return;

            var target = PackItem(client, station, packet.TargetItemId, "RequestIntegrateItem");

            if (target == null)
                return;

            var moduleItem = PackItem(client, station, packet.ModuleItemId, "RequestIntegrateItem");

            if (moduleItem == null)
                return;

            var problem = ItemModules.CanIntegrate(moduleItem, target);

            if (problem == ItemModules.IntegrationProblem.None && (packet.Slot >= ItemModules.Slots || target.ModuleIds[(int)packet.Slot] != 0))
                problem = ItemModules.IntegrationProblem.NoEmptySlot;

            if (problem != ItemModules.IntegrationProblem.None)
            {
                Fail(client, station, problem switch
                {
                    ItemModules.IntegrationProblem.NotAModule => "That is not a module.",
                    ItemModules.IntegrationProblem.NotModifiable => "That item takes no modules.",
                    ItemModules.IntegrationProblem.NoEmptySlot => "That module slot is not empty.",
                    ItemModules.IntegrationProblem.WrongKindOfItem => "That module is for another kind of item.",
                    _ => "The item already has a module of that kind."
                });
                return;
            }

            var cost = ItemModules.IntegrationCost(moduleItem, target);

            if (!CanPay(client, station, cost))
                return;

            ItemModules.TryGetModuleItem(moduleItem, out var module, out _);

            // The module first: it is the one thing here that can still be refused.
            if (!Consume(client, moduleItem, 1))
            {
                Logger.WriteLog(LogType.Error, $"{client.Player.FamilyName}: module item {moduleItem.Id} could not be taken for integration");
                Fail(client, station, null);
                return;
            }

            Pay(client, cost);
            ItemModules.Set(client, target, (int)packet.Slot, module.ModuleId, _gameUnitOfWorkFactory, client.Player.FamilyName);
            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} integrates module {module.ModuleId} into slot {packet.Slot} of item {target.Id} for {cost} {Mimeogel} at station {station.EntityId}");
            Succeed(client, station);
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmCraftingSuccess, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        /// <summary>RequestUpgradeItem(kraftwerksId, itemId, CRAFTACTION_UPGRADE): one of a stack of module items, for the next strength of it.</summary>
        internal void RequestUpgradeItem(Client client, RequestUpgradeItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestUpgradeItem");

            if (station == null)
                return;

            var item = PackItem(client, station, packet.ItemId, "RequestUpgradeItem");

            if (item == null)
                return;

            var upgrade = ItemModules.UpgradeOf(item);
            var result = upgrade != null ? ItemManager.Instance.GetItemTemplateById(upgrade.ItemTemplateId) : null;

            if (result == null)
            {
                Fail(client, station, "That module cannot be upgraded.");
                return;
            }

            ItemModules.TryGetModuleItem(item, out var module, out _);

            if (!CanPay(client, station, module.UpgradeCost) || !HasRoom(client, station))
                return;

            if (!Consume(client, item, 1))
            {
                Logger.WriteLog(LogType.Error, $"{client.Player.FamilyName}: module item {item.Id} could not be taken for an upgrade");
                Fail(client, station, null);
                return;
            }

            Pay(client, module.UpgradeCost);
            Produce(client, station, result, 1, UpgradePage, ItemModules.QualityOfModuleItem(upgrade));
            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} upgrades module {module.ModuleId} to {upgrade.ModuleId} for {module.UpgradeCost} {Mimeogel} at station {station.EntityId}");
            Succeed(client, station);
        }
    }
}
