using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Structures;
    using Structures.Char;

    public class CharacterManager : ICharacterManager
    {
        public const uint SelectionPodStartEntityId = 100;
        public const byte MaxSelectionPods = 16;

        private readonly object _createLock = new object();

        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        public CharacterManager(IUnitOfWorkFactory unitOfWorkFactory)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.Characters.Any(), client.AccountEntry.Id, client.AccountEntry.CanSkipBootcamp));
            
            using var unitOfWork = _unitOfWorkFactory.CreateChar();
            var charactersBySlot = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                CharacterEntry character = null;
                if (charactersBySlot.ContainsKey(i))
                {
                    character = charactersBySlot[i];
                }
                SendCharacterInfo(client, i, character, true);
            }

            client.State = ClientState.CharacterSelection;
        }

        public void RequestCharacterName(Client client, int gender)
        {
            using var unitOfWork = _unitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNameRepository.GetFirstName((Gender)gender);
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = name
            });
        }

        public void RequestFamilyName(Client client)
        {
            using var unitOfWork = _unitOfWorkFactory.CreateWorld();
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

            bool changeFamilyName = false;
            
            using var unitOfWork = _unitOfWorkFactory.CreateChar();
            CharacterEntry characterEntry;
            // TODO to remove this lock, the family name check and update must be redesigned to be thread safe
            lock (_createLock)
            {
                if (!string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) && packet.FamilyName != client.AccountEntry.FamilyName)
                {
                    if (!client.AccountEntry.Characters.Any())
                    {
                        changeFamilyName = true;
                    }
                    else
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterName);
                        return;
                    }
                }

                if ((string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || packet.FamilyName != client.AccountEntry.FamilyName))
                {
                    if (!unitOfWork.GameAccounts.CanChangeFamilyName(client.AccountEntry.Id, packet.FamilyName))
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.FamilyNameReserved);
                        return;
                    }
                }

                characterEntry = unitOfWork.Characters.Create(client.AccountEntry, packet.SlotNum,
                    packet.CharacterName,
                    (byte)packet.RaceId,
                    packet.Scale,
                    packet.Gender);
                if (characterEntry == null)
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                    return;
                }

                var appearances = CreateCharacterAppearanceEntries(packet);
                unitOfWork.CharacterAppearances.Add(characterEntry, appearances);

                if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
                {
                    unitOfWork.GameAccounts.UpdateFamilyName(client.AccountEntry.Id, packet.FamilyName);
                }
            }

            unitOfWork.Complete();

            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, packet.FamilyName));
            
            client.ReloadGameAccountEntry();

            SendCharacterInfo(client, packet.SlotNum, characterEntry, false);
        }

        private IEnumerable<CharacterAppearanceEntry> CreateCharacterAppearanceEntries(RequestCreateCharacterInSlotPacket packet)
        {
            var defaultColor = new Color(255, 255, 255).Hue;
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Shoes, 10000068, defaultColor);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Legs, 10000069, defaultColor);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Torso, 10000070, defaultColor);

            using var worldUnitOfWork = _unitOfWorkFactory.CreateWorld();
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
            databaseEntry.Class = unitOfWork.ItemTemplateItemClassRepository.GetItemClass(appearanceData.ClassId);
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

                using (var unitOfWork = _unitOfWorkFactory.CreateChar())
                {
                    unitOfWork.Characters.Delete(charactersBySlot.Id);
                    unitOfWork.Complete();
                }

                client.ReloadGameAccountEntry();

                client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(client.AccountEntry.Characters.Any()));

                SendCharacterInfo(client, packet.Slot, null, false);
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

            var unitOfWork = _unitOfWorkFactory.CreateChar();
            client.AccountEntry.SelectedSlot = (byte)packet.SlotNum;
            unitOfWork.GameAccounts.UpdateSelectedSlot(client.AccountEntry.Id, (byte)packet.SlotNum);

            var character = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id, (byte)packet.SlotNum);
            unitOfWork.Complete();

            client.MapClient = new MapClient
            {
                Player = character
            };

            MapManager.Instance.PassClientToMap(client);
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterInfo(Client client, byte slot, [CanBeNull] CharacterEntry data, bool sendPodCreate)
        {
            var characterInfo = data == null
                ? new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName)
                : new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data);

            PythonPacket packet = characterInfo;

            if (sendPodCreate)
            {
                var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClass.CharacterSelectionPod);
                newEntityPacket.EntityData.Add(characterInfo);
                packet = newEntityPacket;
            }

            client.CallMethod(SysEntity.ClientMethodId, packet);
        }
    }
}
