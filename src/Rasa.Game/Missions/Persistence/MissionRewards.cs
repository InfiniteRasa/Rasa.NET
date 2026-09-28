using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Runtime;
using ProgressCandidate = Rasa.Missions.Runtime.MissionProgressCandidate;
using System.Text.Json;
using Rasa.Game.Missions.Content;
using Rasa.Game.Missions.Integration;
using Rasa.Repositories.World;
using Rasa.Game.Missions.Persistence;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Packets.Mission.Server;
    using Repositories.Char;
    using Repositories.Char.CharacterMissionProgress;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.Missions;
    using Structures.World;

    internal readonly struct MissionRewardItem
    {
        internal uint ItemTemplateId { get; }
        internal uint Quantity { get; }

        internal MissionRewardItem(uint itemTemplateId, uint quantity)
        {
            ItemTemplateId = itemTemplateId;
            Quantity = quantity;
        }
    }

    internal sealed class MissionRewardDefinition
    {
        internal uint Experience { get; }
        internal IReadOnlyDictionary<CurencyType, int> Currencies { get; }
        internal IReadOnlyList<MissionRewardItem> FixedItems { get; }
        internal IReadOnlyList<MissionRewardItem> SelectableItems { get; }

        internal MissionRewardDefinition(
            uint experience,
            IReadOnlyDictionary<CurencyType, int> currencies,
            IReadOnlyList<MissionRewardItem> fixedItems,
            IReadOnlyList<MissionRewardItem> selectableItems)
        {
            Experience = experience;
            Currencies = new Dictionary<CurencyType, int>(
                currencies ?? new Dictionary<CurencyType, int>());
            FixedItems = (fixedItems ?? Array.Empty<MissionRewardItem>()).ToArray();
            SelectableItems = (selectableItems ?? Array.Empty<MissionRewardItem>()).ToArray();
        }

        internal MissionRewardGrant CreateGrant(
            int? selectionIndex,
            Action<Item> beforeItemPublication = null)
        {
            if (Currencies.Any(entry =>
                    entry.Value < 0 ||
                    entry.Key is not CurencyType.Credits and not CurencyType.Prestige))
                throw new GameplayRejectionException("Mission reward contains an unsupported currency.");
            if (FixedItems.Any(item => item.ItemTemplateId == 0 || item.Quantity == 0) ||
                SelectableItems.Any(item => item.ItemTemplateId == 0 || item.Quantity == 0))
                throw new GameplayRejectionException("Mission reward contains an invalid item.");
            if (SelectableItems.Count == 0)
            {
                if (selectionIndex.HasValue)
                    throw new GameplayRejectionException("Mission reward selection is invalid.");
                return new MissionRewardGrant(
                    Experience, Currencies, FixedItems, beforeItemPublication);
            }
            if (!selectionIndex.HasValue ||
                selectionIndex.Value < 0 ||
                selectionIndex.Value >= SelectableItems.Count)
                throw new GameplayRejectionException("Mission reward selection is invalid.");

            return new MissionRewardGrant(
                Experience,
                Currencies,
                FixedItems.Concat(new[] { SelectableItems[selectionIndex.Value] }).ToArray(),
                beforeItemPublication);
        }

        internal MissionRewardGrant CreateScenarioGrant(
            Action<Item> beforeItemPublication = null)
        {
            if (Currencies.Any(entry =>
                    entry.Value < 0 ||
                    entry.Key is not CurencyType.Credits and not CurencyType.Prestige))
                throw new GameplayRejectionException("Mission reward contains an unsupported currency.");
            if (FixedItems.Any(item => item.ItemTemplateId == 0 || item.Quantity == 0) ||
                SelectableItems.Any(item => item.ItemTemplateId == 0 || item.Quantity == 0))
                throw new GameplayRejectionException("Mission reward contains an invalid item.");
            if (SelectableItems.Count > 0)
                throw new GameplayRejectionException(
                    "Scenario reward packages must not contain selectable alternatives.");

            return new MissionRewardGrant(
                Experience,
                Currencies,
                FixedItems.ToArray(),
                beforeItemPublication);
        }

        internal RewardInfo CreateInfo()
        {
            var info = new RewardInfo();
            foreach (var currency in Currencies)
            {
                if (currency.Value >= 0)
                    info.FixedReward.Credits[currency.Key] = (uint)currency.Value;
            }
            foreach (var item in FixedItems)
                info.FixedReward.FixedItems.Add(CreateItem(item));
            foreach (var item in SelectableItems)
                info.SelectableReward.Add(CreateItem(item));
            return info;

            static RewardItem CreateItem(MissionRewardItem item)
            {
                var reward = new RewardItem
                {
                    ItemTemplateId = item.ItemTemplateId,
                    Quantity = item.Quantity
                };
                if (ItemManager.Instance.ItemTemplateItemClass.TryGetValue(
                        item.ItemTemplateId, out var entityClass))
                    reward.Class = entityClass;
                return reward;
            }
        }
    }

    internal sealed class MissionRewardGrant : IDisposable
    {
        private readonly uint _experience;
        private readonly IReadOnlyDictionary<CurencyType, int> _currencies;
        private readonly InventoryManager.InventoryGrant _inventory;
        private readonly IReadOnlyList<InventoryManager.InventoryItemGrant> _items;
        private ManifestationManager.ProgressionGrant _progression;
        private int _previousCredits;
        private int _previousPrestige;
        private int _credits;
        private int _prestige;

        internal byte? PlannedLevel => _progression is { HasChanges: true } ? _progression.FinalLevel : null;

        internal MissionRewardGrant(
            uint experience,
            IReadOnlyDictionary<CurencyType, int> currencies,
            IReadOnlyList<MissionRewardItem> items,
            Action<Item> beforeItemPublication = null)
        {
            _experience = experience;
            _currencies = currencies;
            _items = items.Select(item =>
                new InventoryManager.InventoryItemGrant(item.ItemTemplateId, item.Quantity)).ToArray();
            _inventory = new InventoryManager.InventoryGrant(beforeItemPublication);
        }

        internal void PlanAndSave(
            Client client,
            CharacterEntry durableCharacter,
            ICharUnitOfWork unitOfWork,
            ManifestationManager manifestationManager)
        {
            var player = client.Player;
            if (!player.Credits.TryGetValue(CurencyType.Credits, out var runtimeCredits) ||
                !player.Credits.TryGetValue(CurencyType.Prestige, out var runtimePrestige) ||
                durableCharacter.Credit != runtimeCredits ||
                durableCharacter.Prestige != runtimePrestige)
                throw new GameplayRejectionException("Runtime currencies no longer match durable character state.");

            _previousCredits = durableCharacter.Credit;
            _previousPrestige = durableCharacter.Prestige;
            _credits = _previousCredits;
            _prestige = _previousPrestige;
            try
            {
                foreach (var currency in _currencies)
                {
                    switch (currency.Key)
                    {
                        case CurencyType.Credits:
                            _credits = checked(_credits + currency.Value);
                            break;
                        case CurencyType.Prestige:
                            _prestige = checked(_prestige + currency.Value);
                            break;
                        default:
                            throw new GameplayRejectionException("Mission reward contains an unsupported currency.");
                    }
                }
            }
            catch (OverflowException error)
            {
                throw new GameplayRejectionException(
                    "Mission reward currency exceeds the supported range.",
                    error);
            }

            if (_items.Count > 0)
                _inventory.PlanAndSave(client, _items, unitOfWork);
            if (_experience > 0)
                _progression = manifestationManager.PlanExperience(
                    client, _experience, durableCharacter, unitOfWork);
            if (_credits != durableCharacter.Credit || _prestige != durableCharacter.Prestige)
                unitOfWork.Characters.UpdateCharacterCurrencies(player.Id, _credits, _prestige);
            MissionRequirementService.ExpectLevel(unitOfWork, player.Id, PlannedLevel ?? durableCharacter.Level);
        }

        internal void ConvergeRuntime(Client client)
        {
            _inventory.ConvergeRuntime(client);
            if (_progression is { HasChanges: true })
            {
                client.Player.Experience = _progression.TotalExperience;
                client.Player.Level = _progression.FinalLevel;
                client.Player.CloneCredits = _progression.FinalCloneCredits;
            }
            client.Player.Credits[CurencyType.Credits] = _credits;
            client.Player.Credits[CurencyType.Prestige] = _prestige;
        }

        internal void Publish(Client client, ManifestationManager manifestationManager)
        {
            _inventory.Publish(client);
            MissionApplication.TryPublish(
                () => manifestationManager.PublishExperience(client, _progression),
                "mission experience");
            PublishCurrency(
                CurencyType.Credits,
                _credits,
                _previousCredits);
            PublishCurrency(
                CurencyType.Prestige,
                _prestige,
                _previousPrestige);

            void PublishCurrency(CurencyType type, int total, int previous)
            {
                if (total == previous)
                    return;
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new UpdateCreditsPacket(
                            type,
                            total,
                            (uint)(total - previous))),
                    $"mission {type} total");
            }
        }

        internal IReadOnlyList<MissionProgressEvent> CreateItemAcquisitionEvents() =>
            _items
                .Select(item =>
                    ItemManager.Instance.ItemTemplateItemClass.TryGetValue(
                        item.ItemTemplateId,
                        out var itemClass)
                        ? new
                        {
                            ItemClassId = (uint)itemClass,
                            item.Quantity
                        }
                        : null)
                .Where(item => item != null)
                .GroupBy(item => item.ItemClassId)
                .Select(group => MissionProgressEvent.ItemAcquired(
                    group.Key,
                    group.Aggregate(
                        0U,
                        (total, item) => checked(total + item.Quantity))))
                .ToArray();

        public void Dispose() => _inventory.Dispose();
    }

}
