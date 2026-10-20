using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Persistence;
    using Missions;
    using Packets.Communicator.Server;
    using Packets.Crafting.Client;
    using Packets.Crafting.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
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

            try
            {
                if (MissionItemProtection.IsProtected(item, _gameUnitOfWorkFactory))
                {
                    Fail(client, station, "A mission's item cannot be used at a crafting station.");
                    return null;
                }
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                Logger.WriteLog(LogType.Error, $"{request} item {item.Id} protection check failed: {error}");
                Fail(client, station, "The item's mission protection could not be checked. Nothing was consumed; try again.");
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

        private bool CommitModuleCraft(Client client, DynamicObject station, Item input, uint quantity,
            uint cost, Item target = null, int slot = 0, uint moduleId = 0, string modifiedBy = null)
        {
            InventoryManager.InventoryPlan inventory = null;
            ItemModules.ModuleMutation mutation = null;
            var progress = MissionProgressPublicationPlan.Empty;
            try
            {
                using var unit = _gameUnitOfWorkFactory.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var items = new[] { input, target }.Where(item => item != null).Distinct().ToArray();
                    unit.Enlist(() => new ModuleCraftAdmission(this, client, station, unit, items,
                        quantity > 0 ? input : null, target));
                    inventory = InventoryManager.InventoryPlan.For(client, unit);
                    var events = new List<MissionProgressEvent>();
                    if (quantity > 0)
                        events.Add(inventory.ConsumeEntity(input.EntityId, quantity));
                    var remaining = cost;
                    foreach (var entityId in client.Player.Inventory.PersonalInventory.Where(id => id != 0))
                    {
                        if (remaining == 0)
                            break;
                        var fee = EntityManager.Instance.GetItem(entityId);
                        if ((uint)fee.ItemTemplate.Class != ItemModules.MimeogelClassId ||
                            MissionItemProtection.IsProtected(fee, unit))
                            continue;
                        var taken = Math.Min(remaining, fee.StackSize);
                        events.Add(inventory.ConsumeEntity(entityId, taken));
                        remaining -= taken;
                    }
                    if (remaining != 0)
                        throw new GameplayRejectionException("The Mimeomech fee is no longer available.");
                    if (target != null)
                        mutation = ItemModules.PlanSet(client, target, slot, moduleId, unit, modifiedBy);
                    progress = MissionApplication.Instance.PlanProgress(client, events, unit);
                });
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                Logger.WriteLog(LogType.Error, $"Module crafting for {client.Player.FamilyName} at station {station.EntityId} failed: {error}");
                Fail(client, station, "Crafting could not be saved, or its items or station changed. Nothing was consumed; try again.");
                return false;
            }
            inventory.Publish(client);
            mutation?.Publish();
            progress.Publish(client);
            return true;
        }

        private sealed class ModuleCraftAdmission : ITransactionParticipant
        {
            private readonly KraftwerksManager _manager;
            private readonly Client _client;
            private readonly Manifestation _player;
            private readonly DynamicObject _station;
            private readonly ICharUnitOfWork _unit;
            private readonly Item[] _items;
            private readonly ItemModules.ModuleSnapshot[] _snapshots;
            private readonly Item _consumed;
            private readonly Item _modified;
            private readonly CraftingJob[] _jobs;

            internal ModuleCraftAdmission(KraftwerksManager manager, Client client, DynamicObject station,
                ICharUnitOfWork unit, Item[] items, Item consumed, Item modified)
            {
                _manager = manager;
                _client = client;
                _player = client.Player;
                _station = station;
                _unit = unit;
                _items = items;
                _consumed = consumed;
                _modified = modified;
                _snapshots = items.Select(item => new ItemModules.ModuleSnapshot(item)).ToArray();
                _jobs = manager.JobsFor(client, station).ToArray();
                RequireAdmission();
            }

            private void RequireAdmission()
            {
                if (!ReferenceEquals(_client.Player, _player) ||
                    !EntityManager.Instance.TryGetObject(_station.EntityId, out var current) ||
                    !ReferenceEquals(current, _station) || !_station.IsEnabled ||
                    _station.DynamicObjectType != DynamicObjectType.Kraftwerks ||
                    _station.MapContextId != _player.MapContextId ||
                    Vector3.Distance(_player.Position, _station.Position) > InteractionRange + 2f ||
                    !_jobs.SequenceEqual(_manager.JobsFor(_client, _station)) ||
                    (_consumed == null || _modified == null) &&
                        (_jobs.Length >= MaxJobsPerStation || _jobs.Any(job => !job.IsFinished)))
                    throw new GameplayRejectionException("Module crafting station admission changed.");
                foreach (var item in _items)
                    if (MissionItemProtection.IsProtected(item, _unit))
                        throw new GameplayRejectionException("A mission's item cannot be used at a crafting station.");
            }

            public void Prepare()
            {
                RequireAdmission();
                foreach (var snapshot in _snapshots)
                    snapshot.RequireDurable(_unit);
            }

            public void ValidateCommitBoundary()
            {
                RequireAdmission();
                for (var index = 0; index < _items.Length; index++)
                    if (_items[index] != _modified)
                        _snapshots[index].RequireDurable(_unit, _items[index] == _consumed);
            }

            public void Committed() { }
            public void Dispose() { }
        }

        /// <summary>A finished job for the player at the station: something to take, made when it is taken.</summary>
        private void Produce(Client client, DynamicObject station, ItemTemplate result, uint count, uint page, uint qualityId)
        {
            var job = new CraftingJob
            {
                ResultClassId = (uint)result.Class,
                ResultItemTemplateId = result.ItemTemplateId,
                Count = count,
                CraftingPage = page,
                QualityId = qualityId,
                FinishTick = Environment.TickCount64,
                FinishReported = true
            };

            Persist(client, station, job);
            JobsForWriting(client, station).Add(job);
        }

        private void Succeed(Client client, DynamicObject station)
        {
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(client.Player.EntityId));
        }

        /// <summary>
        /// The transaction of a hand-over (HandOver): as many of the job's items as there is room
        /// for, made with the crafter given (null: nobody's name, onto any stack of the template),
        /// with the inbox after the backpack when overflowToInbox, and the job's row (JobClaim).
        /// </summary>
        private bool HandOver(Client client, DynamicObject station, CraftingJob job, string crafter, bool overflowToInbox)
        {
            InventoryManager.InventoryPlan inventory = null;
            JobClaim claim = null;
            var progress = MissionProgressPublicationPlan.Empty;
            var amount = 0u;
            var full = false;
            try
            {
                using var unit = _gameUnitOfWorkFactory.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    claim = unit.Enlist(() => new JobClaim(this, client, station, job, unit));
                    inventory = InventoryManager.InventoryPlan.For(client, unit);
                    var template = InventoryManager.InventoryPlan.RequireTemplate(job.ResultItemTemplateId);
                    if ((uint)template.Class != job.ResultClassId)
                        throw new GameplayRejectionException("Crafting job output class does not match its template.");
                    amount = Math.Min(job.Count, inventory.GrantCapacity(job.ResultItemTemplateId, crafter, overflowToInbox));
                    if (amount == 0)
                    {
                        full = true;
                        throw new GameplayRejectionException("There is no room for the crafting job's output.");
                    }
                    inventory.Grant(job.ResultItemTemplateId, amount, overflowToInbox: overflowToInbox, crafter: crafter);
                    claim.Take(amount);
                    progress = MissionApplication.Instance.PlanProgress(client,
                        new[] { MissionProgressEvent.ItemAcquired(job.ResultClassId, amount) }, unit);
                });
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                if (full)
                {
                    Fail(client, station, PlayerMessage.PmInventoryFull);
                    return false;
                }
                // Its row is gone: it was handed over already, or deleted. It goes from the station.
                if (claim?.RowMissing == true)
                {
                    Logger.WriteLog(LogType.Error, $"Crafting job {job.ResultItemId} of {client.Player.FamilyName} has no row any more; it is dropped without a hand-over");
                    CommunicatorManager.Instance.SystemMessage(client, "That job is no longer at the station.");
                    return true;
                }
                Logger.WriteLog(LogType.Error, $"Crafting job {job.ResultItemId} hand-over to {client.Player.FamilyName} failed: {error}");
                Fail(client, station, "The finished item could not be saved. It remains at the station; try again.");
                return false;
            }
            job.Count -= amount;
            inventory.Publish(client);
            progress.Publish(client);
            if (job.Count == 0)
                return true;
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull,
                new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            return false;
        }

        /// <summary>
        /// A finished job being handed over: still the player's, at this station, unchanged and
        /// the station in reach, from the plan to the commit; and its row (when it has one) goes,
        /// or is brought down to what is left, in the same transaction as the items.
        /// </summary>
        private sealed class JobClaim : ITransactionParticipant
        {
            private readonly KraftwerksManager _manager;
            private readonly Client _client;
            private readonly DynamicObject _station;
            private readonly CraftingJob _job;
            private readonly (ulong id, uint template, uint @class, uint count, uint page, uint quality, long finish) _original;
            private readonly ICharUnitOfWork _unit;
            private uint _taken;

            /// <summary>The job has a row no longer (Prepare): there is nothing to hand over.</summary>
            internal bool RowMissing { get; private set; }

            internal JobClaim(KraftwerksManager manager, Client client, DynamicObject station, CraftingJob job, ICharUnitOfWork unit)
            {
                _manager = manager;
                _client = client;
                _station = station;
                _job = job;
                _unit = unit;
                _original = State();
                RequireClaim();
            }

            /// <summary>How many of the job's items the transaction gives the player.</summary>
            internal void Take(uint amount)
            {
                if (amount == 0 || amount > _job.Count)
                    throw new GameplayRejectionException("Crafting job hand-over amount is invalid.");
                _taken = amount;
            }

            private (ulong, uint, uint, uint, uint, uint, long) State() =>
                (_job.ResultItemId, _job.ResultItemTemplateId, _job.ResultClassId, _job.Count,
                    _job.CraftingPage, _job.QualityId, _job.FinishTick);

            private void RequireClaim()
            {
                if (_original != State() || _job.Count == 0 || !_job.IsFinished ||
                    !_manager.JobsFor(_client, _station).Contains(_job) ||
                    !EntityManager.Instance.TryGetObject(_station.EntityId, out var current) ||
                    !ReferenceEquals(current, _station) || !_station.IsEnabled ||
                    _station.DynamicObjectType != DynamicObjectType.Kraftwerks ||
                    _station.MapContextId != _client.Player.MapContextId ||
                    Vector3.Distance(_client.Player.Position, _station.Position) > InteractionRange + 2f)
                    throw new GameplayRejectionException("Crafting job claim or station changed.");
            }

            public void Prepare()
            {
                RequireClaim();
                if (_taken == 0)
                    throw new GameplayRejectionException("Crafting job hand-over planned no items.");
                if (!_job.Persisted)
                    return;
                var id = (uint)_job.ResultItemId;
                var written = _taken == _job.Count
                    ? _unit.CraftingJobs.Delete(id)
                    : _unit.CraftingJobs.UpdateCount(id, _job.Count - _taken);
                // Gone already (taken through another station's manager, or a GM's delete): nothing
                // to give.
                if (!written)
                {
                    RowMissing = true;
                    throw new GameplayRejectionException("Crafting job row is missing.");
                }
            }

            public void ValidateCommitBoundary() => RequireClaim();
            public void Committed() { }
            public void Dispose() { }
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

            if (!CommitModuleCraft(client, station, item, item.StackSize, 0))
                return;

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

            if (!CommitModuleCraft(client, station, null, 0, module.ExtractCost, item, (int)packet.Slot))
                return;
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

            if (!CommitModuleCraft(client, station, moduleItem, 1, cost, target, (int)packet.Slot,
                module.ModuleId, client.Player.FamilyName))
                return;
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

            if (!CommitModuleCraft(client, station, item, 1, module.UpgradeCost))
                return;
            Produce(client, station, result, 1, UpgradePage, ItemModules.QualityOfModuleItem(upgrade));
            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} upgrades module {module.ModuleId} to {upgrade.ModuleId} for {module.UpgradeCost} {Mimeogel} at station {station.EntityId}");
            Succeed(client, station);
        }
    }
}
