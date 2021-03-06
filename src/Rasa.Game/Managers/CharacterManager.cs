﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using JetBrains.Annotations;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Structures;
    using Structures.Char;
    
    public class CharacterManager : ICharacterManager
    {
        public const uint SelectionPodStartEntityId = 100;
        public const byte MaxSelectionPods = 16;

        private readonly object _createLock = new object();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly IMapChannelManager _mapChannelManager;

        public CharacterManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, IMapChannelManager mapChannelManager)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _mapChannelManager = mapChannelManager;
        }

        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.Characters.Any(), client.AccountEntry.Id, client.AccountEntry.CanSkipBootcamp));
            
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var charactersBySlot = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                CharacterEntry character = null;
                if (charactersBySlot.ContainsKey(i))
                {
                    character = charactersBySlot[i];
                }
                SendCharacterInfoProdCreate(client, i, character);
            }

            client.State = ClientState.CharacterSelection;
        }

        public void RequestCharacterName(Client client, int gender)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNameRepository.GetFirstName((Gender)gender);
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = name
            });
        }

        public void RequestFamilyName(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNameRepository.GetLastName();
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = name
            });
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            var result = packet.Validate();
            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            uint characterId;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // TODO to remove this lock, the family name check and update must be redesigned to be thread safe
            lock (_createLock)
            {
                var createdCharacterId = InternalCreate(client, packet, unitOfWork);

                if (createdCharacterId == null)
                {
                    return;
                }

                characterId = createdCharacterId.Value;
            }

            unitOfWork.Complete();

            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, packet.FamilyName));
            
            client.ReloadGameAccountEntry();

            var character = unitOfWork.Characters.Get(characterId);
            SendCharacterInfo(client, packet.SlotNum, character);
        }

        private uint? InternalCreate(Client client, RequestCreateCharacterInSlotPacket packet, ICharUnitOfWork unitOfWork)
        {
            var changeFamilyName = false;
            if (!string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) && packet.FamilyName != client.AccountEntry.FamilyName)
            {
                if (!client.AccountEntry.Characters.Any())
                {
                    changeFamilyName = true;
                }
                else
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterName);
                    return null;
                }
            }

            if ((string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || packet.FamilyName != client.AccountEntry.FamilyName))
            {
                if (!unitOfWork.GameAccounts.CanChangeFamilyName(client.AccountEntry.Id, packet.FamilyName))
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.FamilyNameReserved);
                    return null;
                }
            }

            var characterEntry = unitOfWork.Characters.Create(client.AccountEntry, packet.SlotNum,
                packet.CharacterName,
                (byte)packet.RaceId,
                packet.Scale,
                packet.Gender);
            if (characterEntry == null)
            {
                SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                return null;
            }

            var appearances = CreateCharacterAppearanceEntries(packet);
            unitOfWork.CharacterAppearances.Add(characterEntry, appearances);

            if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
            {
                unitOfWork.GameAccounts.UpdateFamilyName(client.AccountEntry.Id, packet.FamilyName);
            }

            return characterEntry.Id;
        }

        private IEnumerable<CharacterAppearanceEntry> CreateCharacterAppearanceEntries(RequestCreateCharacterInSlotPacket packet)
        {
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Shoes, (uint)EntityClass.ArmorRecruitV01CMNBoots, 2139062144);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Torso, (uint)EntityClass.ArmorRecruitV01CMNVest, 2139062144);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Legs, (uint)EntityClass.ArmorRecruitV01CMNLegs, 2139062144);

            using var worldUnitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var appearancesFromPacket = packet.AppearanceData
                .Select(appearanceData => CreateCharacterAppearanceEntry(appearanceData.Value, worldUnitOfWork))
                .ToList();

            foreach (var characterAppearanceEntry in appearancesFromPacket)
            {
                yield return characterAppearanceEntry;
            }
        }

        private static CharacterAppearanceEntry CreateCharacterAppearanceEntry(AppearanceData appearanceData, IWorldUnitOfWork unitOfWork)
        {
            var databaseEntry = appearanceData.GetDatabaseEntry();
            databaseEntry.Class = unitOfWork.ItemTemplateItemClassRepository.GetItemClass(appearanceData.Class);
            return databaseEntry;
        }

        public void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet)
        {
            try
            {
                var charactersBySlot = client.AccountEntry.GetCharacterBySlot(packet.Slot);
                if (charactersBySlot == null)
                {
                    return;
                }

                using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                {
                    unitOfWork.CharacterAppearances.DeleteForChar(charactersBySlot.Id);
                    // TODO delete ClanMember entry
                    unitOfWork.Characters.Delete(charactersBySlot.Id);
                    unitOfWork.Complete();
                }

                client.ReloadGameAccountEntry();

                client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(client.AccountEntry.Characters.Any()));

                SendCharacterInfo(client, packet.Slot, null);
            }
            catch
            {
                client.CallMethod(SysEntity.ClientMethodId, new DeleteCharacterFailedPacket());
            }
        }

        public void RequestSwitchToCharacterInSlot(Client client, RequestSwitchToCharacterInSlotPacket packet)
        {
            if (packet.SlotNum < 1 || packet.SlotNum > 16)
                return;

            var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            client.AccountEntry.SelectedSlot = (byte)packet.SlotNum;
            unitOfWork.GameAccounts.UpdateSelectedSlot(client.AccountEntry.Id, (byte)packet.SlotNum);

            var character = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id, (byte)packet.SlotNum);
            unitOfWork.Characters.UpdateLoginData(character.Id);
            unitOfWork.Complete();

            client.Player = CreateCharacterManifestation(character);

            _mapChannelManager.PassClientToMap(client);
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterInfoProdCreate(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClass.CharacterSelectionPod);

            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            newEntityPacket.EntityData.Add(characterInfo);

            client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
        }

        private void SendCharacterInfo(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            client.CallMethod(SelectionPodStartEntityId + slot, characterInfo);
        }

        private CharacterInfoPacket CreateCharacterInfoPacket(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = data == null
                ? new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName)
                : new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data);
            return characterInfo;
        }

        private Manifestation CreateCharacterManifestation(CharacterEntry character)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var characterAppearances = unitOfWork.CharacterAppearances.GetByCharacterId(character.Id);

            var newCharacter = new Manifestation
            {
                AppearanceData = characterAppearances,
                Gender = character.Gender,
                IsRunning = character.IsRunning(),
                IsCrouching = character.IsCrouching(),
                Position = character.GetPositionVector(),
                Rotation = character.Rotation
            };

            newCharacter.GenerateGUID(EntityType.Character, character.Id);

            return newCharacter;
        }
    }
}
