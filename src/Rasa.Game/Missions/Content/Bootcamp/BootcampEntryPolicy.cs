using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static Rasa.Missions.Content.Bootcamp.BootcampEntryDefinition;

namespace Rasa.Game.Missions.Content.Bootcamp
{
    using Data;
    using Managers;
    using Misc;
    using Integration;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    internal sealed class BootcampEntryPolicy : IStartingExperiencePolicy
    {
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly MissionApplication _missionManager;
        internal BootcampEntryPolicy(IGameUnitOfWorkFactory factory, MissionApplication missions)
        { _gameUnitOfWorkFactory = factory; _missionManager = missions; }
        public string ContentRevision => Deployment11StartingExperienceRevision;
        public bool CanSkip(GameAccountEntry account) => account?.CanSkipBootcamp == true;
        public bool IsExitWaypoint(uint waypointId) => waypointId == BootcampExitPadWaypointId;
        public bool IsExitPad(uint mapContextId, uint waypointId) =>
            mapContextId == BootcampPrivateMapContextId && IsExitWaypoint(waypointId);
        public bool IsDepartureReady(Client client) =>
            client?.Player != null && IsOwnedBootcampPlayer(client.Player) &&
            !client.Player.StartingExperienceCompleted &&
            client.Player.Missions.Values.Any(mission => mission.State == MissionState.Active &&
                mission.Completeable && (_missionManager ?? MissionApplication.Instance).HasPlayerTriggeredScenario(mission.MissionId));

        public bool TrySelect(Client client, bool skip, CharacterEntry character, ICharUnitOfWork unitOfWork,
            out CharacterStartingExperienceState? state)
        {
            state = null;
            var account = unitOfWork.GameAccounts.Get(client.AccountEntry.Id);
            var startingExperience =
                unitOfWork.CharacterStartingExperience.Get(character.Id);
            if (startingExperience?.State == CharacterStartingExperienceState.Pending)
            {
                if (skip)
                {
                    if (account?.CanSkipBootcamp != true)
                    {
                        Logger.WriteLog(
                            LogType.Security,
                            $"AccountId = {client.AccountEntry.Id} tried to skip bootcamp for character {character.Id} without entitlement.");
                        return false;
                    }

                    if (unitOfWork.CharacterStartingExperience.TrySetState(
                            character.Id,
                            CharacterStartingExperienceState.Pending,
                            CharacterStartingExperienceState.Skipped))
                        ApplyBootcampSkipParity(unitOfWork, client.AccountEntry.Id, character.Id);
                }
                else if (unitOfWork.CharacterStartingExperience.TrySetState(
                             character.Id,
                             CharacterStartingExperienceState.Pending,
                             CharacterStartingExperienceState.Bootcamp))
                {
                    unitOfWork.Characters.UpdateCharacterPosition(
                        character.Id,
                        BootcampStartCoordX,
                        BootcampStartCoordY,
                        BootcampStartCoordZ,
                        BootcampStartRotation,
                        BootcampPrivateMapContextId);
                    if ((_missionManager ?? MissionApplication.Instance).LoadedMissions
                            .GetValueOrDefault(BootcampInitiationMissionId)?.IsOperational != true)
                        throw new GameplayRejectionException(
                            $"Bootcamp mission {BootcampInitiationMissionId} is unavailable.");
                }

                startingExperience =
                    unitOfWork.CharacterStartingExperience.Get(character.Id);
            }
            else
            {
                RepairInvalidBootcampReturn(unitOfWork, character, startingExperience?.State);
                character = unitOfWork.Characters.Get(character.Id);
            }

            state = startingExperience?.State;
            return true;
        }

        public void OfferStartingExperienceMission(Client client)
        {
            if (client?.Player == null ||
                !IsOwnedBootcampPlayer(client.Player) ||
                client.Player.Missions.ContainsKey(BootcampInitiationMissionId))
                return;

            (_missionManager ?? MissionApplication.Instance).Offers.TryOffer(client, BootcampInitiationMissionId,
                Rasa.Missions.Definitions.MissionOfferSourceIdentity.ServerEvent("bootcamp.arrival"));
        }

        private static bool IsOwnedBootcampPlayer(Manifestation player) =>
            player?.MapContextId == BootcampPrivateMapContextId &&
            player.MapChannel?.MapInfo.MapContextId == BootcampPrivateMapContextId &&
            player.MapChannel.IsPrivateInstance &&
            player.MapChannel.OwnerCharacterId == player.Id;

        public MapChannel ResolveMap(
            CharacterEntry character,
            CharacterStartingExperienceState? startingState)
        {
            if (character?.MapContextId == BootcampPrivateMapContextId &&
                startingState == CharacterStartingExperienceState.Bootcamp)
                return MapChannelManager.Instance.GetOrCreatePrivateInstance(
                           BootcampPrivateMapContextId,
                           character.Id) ??
                       MapChannelManager.Instance.FindByContextId(
                           BootcampPrivateMapContextId);

            return MapChannelManager.Instance.FindByContextId(character?.MapContextId ?? 0);
        }

        public bool TryDepart(Client client, DynamicObjectManager objects)
        {
            if (client?.Player == null ||
                client.State != ClientState.Ingame ||
                client.PendingTransfer != null ||
                client.Player.MapChannel == null ||
                !client.Player.MapChannel.IsPrivateInstance ||
                client.Player.MapChannel.OwnerCharacterId != client.Player.Id ||
                client.Player.MapContextId != BootcampPrivateMapContextId)
                return false;

            var destinationMap = MapChannelManager.Instance.FindByContextId(
                BootcampArrivalMapContextId);
            var destination = new System.Numerics.Vector3(
                (float)BootcampArrivalCoordX, (float)BootcampArrivalCoordY, (float)BootcampArrivalCoordZ);
            if (!objects.CanBeginDropshipTravel(client, destinationMap, destination, BootcampArrivalRotation) ||
                !client.Player.MapChannel.Teleporters.TryGetValue(BootcampExitPadWaypointId, out var pad))
                return false;
            var readyMission = client.Player.Missions.Values.FirstOrDefault(mission =>
                mission.State == MissionState.Active && mission.Completeable &&
                (_missionManager ?? MissionApplication.Instance).HasPlayerTriggeredScenario(mission.MissionId));
            if (readyMission == null)
                return false;

            var departed = false;
            IReadOnlyDictionary<uint, uint> flags = null;
            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var startingExperience =
                        unitOfWork.CharacterStartingExperience.Get(client.Player.Id);
                    if (startingExperience?.State != CharacterStartingExperienceState.Bootcamp ||
                        !unitOfWork.CharacterStartingExperience.TrySetState(
                            client.Player.Id,
                            CharacterStartingExperienceState.Bootcamp,
                            CharacterStartingExperienceState.Completed))
                        return;

                    if (!HasPlayerTriggeredDepartureMission(
                            unitOfWork,
                            client.Player.Id))
                        throw new GameplayRejectionException(
                            "Bootcamp departure is not ready.");

                    ReconcileBootcampParityProgression(unitOfWork, client.Player.Id);
                    EnsureWaypoint(
                        unitOfWork,
                        client.Player.Id,
                        BootcampAliaWaypointId,
                        WaypointType.Waypoint);
                    EnsureWaypoint(
                        unitOfWork,
                        client.Player.Id,
                        BootcampAliaHospitalId,
                        WaypointType.Hospital);
                    unitOfWork.CharacterFlags.Set(client.Player.Id, CharacterFlagIds.BootcampComplete, 1);
                    flags = unitOfWork.CharacterFlags.Get(client.Player.Id);
                    if (!unitOfWork.GameAccounts.TryUpdateCanSkipBootcamp(
                            client.AccountEntry.Id,
                            false,
                            true))
                        unitOfWork.GameAccounts.UpdateCanSkipBootcamp(
                            client.AccountEntry.Id,
                            true);
                    departed = true;
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(
                    LogType.Error,
                    $"Unable to complete bootcamp departure for character {client.Player.Id}: {error.Message}");
                return false;
            }

            if (!departed)
                return false;

            client.Player.Class = (uint)CharacterClass.Recruit;
            client.Player.Experience = ResolveBootcampParityExperience();
            client.Player.Level = BootcampParityLevel;
            client.AccountEntry.CanSkipBootcamp = true;
            client.Player.StartingExperienceCompleted = true;
            client.FlagProjection.ApplyCommitted(client, flags);
            (_missionManager ?? MissionApplication.Instance).PublishCharacterFlags(client);
            DynamicObjectManager.ConvergeWaypointGrant(
                client,
                new CharacterTeleporterEntry(
                    client.Player.Id,
                    BootcampAliaWaypointId,
                    (byte)WaypointType.Waypoint));
            DynamicObjectManager.ConvergeWaypointGrant(
                client,
                new CharacterTeleporterEntry(
                    client.Player.Id,
                    BootcampAliaHospitalId,
                    (byte)WaypointType.Hospital));

            MissionApplication.TryPublish(
                () => (_missionManager ?? MissionApplication.Instance).Scenes.MissionChanged(client, readyMission.MissionId, "Departing"),
                $"character {client.Player.Id} evacuation boarding");
            var started = objects.TryBeginDropshipTravel(client, destinationMap, destination, BootcampArrivalRotation,
                pad.Position, pad.Rotation, releaseOwnedPrivateInstancesForCharacterId: client.Player.Id);
            if (!started)
                Logger.WriteLog(LogType.Error,
                    $"Bootcamp completion committed for character {client.Player.Id}, but flight could not start; reconnect restores the completed arrival.");
            return started;
        }

        private void ApplyBootcampSkipParity(
            ICharUnitOfWork unitOfWork,
            uint accountId,
            uint characterId)
        {
            ReconcileBootcampParityProgression(unitOfWork, characterId);
            unitOfWork.CharacterFlags.Set(characterId, CharacterFlagIds.BootcampComplete, 1);
            unitOfWork.CharacterSkills.AddOrUpdate(
                characterId,
                (uint)SkillId.Lightning,
                (int)ActionId.AaRecruitLightning,
                1);
            unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                characterId,
                BootcampAbilitySlot,
                (int)ActionId.AaRecruitLightning,
                1);
            EnsureWaypoint(
                unitOfWork,
                characterId,
                BootcampAliaWaypointId,
                WaypointType.Waypoint);
            EnsureWaypoint(
                unitOfWork,
                characterId,
                BootcampAliaHospitalId,
                WaypointType.Hospital);
            GrantSkipInventory(unitOfWork, accountId, characterId);
        }

        private void GrantSkipInventory(
            ICharUnitOfWork unitOfWork,
            uint accountId,
            uint characterId)
        {
            var inventoryRows = unitOfWork.CharacterInventories.GetItems(accountId)
                .Where(entry =>
                    entry.CharacterId == characterId &&
                    entry.InventoryType == (uint)InventoryType.Personal)
                .OrderBy(entry => entry.SlotId)
                .ToArray();
            var usedSlots = inventoryRows.Select(entry => entry.SlotId).ToHashSet();
            var existingTemplateIds = inventoryRows
                .Select(entry => unitOfWork.Items.GetItem(entry.ItemId)?.ItemTemplateId ?? 0)
                .Where(itemTemplateId => itemTemplateId != 0)
                .ToHashSet();

            using var world = _gameUnitOfWorkFactory.CreateWorld();
            var templateIds = BootcampSkipEquipmentTemplateIds
                .Concat(new[] { BootcampSkipAmmoTemplateId })
                .Distinct()
                .ToArray();
            var templates = world.Equipment.GetItemTemplates()
                .Where(entry => templateIds.Contains(entry.Id))
                .ToDictionary(entry => entry.Id);
            var classesByTemplate = world.Equipment.GetItemTemplateClasses()
                .Where(entry => templateIds.Contains(entry.ItemTemplateId))
                .ToDictionary(entry => entry.ItemTemplateId, entry => entry.ItemClass);
            var itemClasses = world.Equipment.GetItemClasses()
                .Where(entry => classesByTemplate.Values.Contains(entry.Id))
                .ToDictionary(entry => entry.Id);

            foreach (var templateId in BootcampSkipEquipmentTemplateIds
                         .Concat(new[] { BootcampSkipAmmoTemplateId }))
            {
                if (existingTemplateIds.Contains(templateId))
                    continue;

                if (!templates.TryGetValue(templateId, out var template) ||
                    !classesByTemplate.TryGetValue(templateId, out var classId) ||
                    !itemClasses.TryGetValue(classId, out var itemClass))
                    throw new GameplayRejectionException(
                        $"Bootcamp skip item template {templateId} is unavailable.");

                var slot = FindNextPersonalSlot(usedSlots, (InventoryCategory)template.InventoryCategory);
                var item = new Item(
                    templateId,
                    templateId == BootcampSkipAmmoTemplateId
                        ? BootcampSkipAmmoQuantity
                        : 1,
                    itemClass.MaxHitPoints,
                    2139062144)
                {
                    OwnerId = characterId,
                    OwnerSlotId = slot,
                    Crafter = string.Empty
                };
                var itemId = unitOfWork.Items.CreateItem(item);
                unitOfWork.CharacterInventories.AddInvItem(
                    accountId,
                    characterId,
                    (uint)InventoryType.Personal,
                    slot,
                    itemId);
                usedSlots.Add(slot);
            }
        }

        private static uint ResolveBootcampParityExperience()
        {
            if (BootcampParityLevel < 1 || BootcampParityLevel > ExpPerLevel.ExpRequred.Count)
                throw new InvalidOperationException(
                    $"Bootcamp parity level {BootcampParityLevel} is outside the experience table.");

            return checked((uint)ExpPerLevel.ExpRequred[BootcampParityLevel - 1]);
        }

        private static void RepairInvalidBootcampReturn(
            ICharUnitOfWork unitOfWork,
            CharacterEntry character,
            CharacterStartingExperienceState? startingState)
        {
            if (character?.MapContextId != BootcampPrivateMapContextId)
                return;

            if (startingState != CharacterStartingExperienceState.Completed &&
                startingState != CharacterStartingExperienceState.Skipped)
                return;

            unitOfWork.Characters.UpdateCharacterPosition(
                character.Id,
                BootcampArrivalCoordX,
                BootcampArrivalCoordY,
                BootcampArrivalCoordZ,
                BootcampArrivalRotation,
                BootcampArrivalMapContextId);
        }

        private static void ReconcileBootcampParityProgression(
            ICharUnitOfWork unitOfWork,
            uint characterId)
        {
            unitOfWork.Characters.ReconcileBootcampCharacter(
                characterId,
                ResolveBootcampParityExperience(),
                BootcampParityLevel,
                (uint)CharacterClass.Recruit,
                BootcampArrivalCoordX,
                BootcampArrivalCoordY,
                BootcampArrivalCoordZ,
                BootcampArrivalRotation,
                BootcampArrivalMapContextId);
        }

        private bool HasPlayerTriggeredDepartureMission(
            ICharUnitOfWork unitOfWork,
            uint characterId)
        {
            var manager = _missionManager ?? MissionApplication.Instance;
            return unitOfWork.CharacterMissions.Get(characterId)
                .Any(mission =>
                    mission.MissionState == (uint)MissionState.Active &&
                    mission.Completeable &&
                    manager.HasPlayerTriggeredScenario(mission.MissionId));
        }

        private static void EnsureWaypoint(
            ICharUnitOfWork unitOfWork,
            uint characterId,
            uint waypointId,
            WaypointType type)
        {
            if (unitOfWork.CharacterTeleporters.Get(characterId).Any(entry =>
                    entry.WaypointId == waypointId &&
                    entry.WaypointType == (byte)type))
                return;

            unitOfWork.CharacterTeleporters.Add(
                new CharacterTeleporterEntry(
                    characterId,
                    waypointId,
                    (byte)type));
        }

        private static uint FindNextPersonalSlot(
            ISet<uint> usedSlots,
            InventoryCategory category)
        {
            var start = ((int)category - 1) * InventoryManager.PersonalCategorySize;
            var end = start + InventoryManager.PersonalCategorySize;
            for (uint slot = (uint)start; slot < end; slot++)
                if (!usedSlots.Contains(slot))
                    return slot;

            throw new GameplayRejectionException(
                $"No personal inventory slot is available for {category}.");
        }

    }
}
