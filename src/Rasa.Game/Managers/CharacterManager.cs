using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Repositories.Char;
    using Structures;

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

            var charactersBySlot = client.AccountEntry.GetCharactersWithSlot();

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                if (charactersBySlot.ContainsKey(i))
                    SendCharacterInfo(client, i, charactersBySlot[i], true);
                else
                    SendCharacterInfo(client, i, null, true);
            }

            client.State = ClientState.CharacterSelection;
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = PlayerRandomNameTable.GetRandom((PlayerRandomNameTable.Gender)gender, PlayerRandomNameTable.NameType.First) ?? (gender == 0 ? "Richard" : "Rachel")
            });
        }

        public void RequestFamilyName(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = PlayerRandomNameTable.GetRandom(PlayerRandomNameTable.Gender.Neutral, PlayerRandomNameTable.NameType.Last) ?? "Garriott"
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
            
            using var unitOfWork = _unitOfWorkFactory.Create<ICharUnitOfWork>();
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

                var appearances = packet.AppearanceData
                    .Select(appearanceData => appearanceData.Value.GetDatabaseEntry());
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

        public void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet)
        {
            try
            {
                var charactersBySlot = client.AccountEntry.GetCharacterBySlot(packet.Slot);
                if (charactersBySlot == null)
                {
                    return;
                }

                using (var unitOfWork = _unitOfWorkFactory.Create<ICharUnitOfWork>())
                {
                    unitOfWork.Characters.Delete(charactersBySlot.Id);
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

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterInfo(Client client, byte slot, CharacterEntry data, bool sendPodCreate)
        {
            if (!sendPodCreate)
            {
                client.CallMethod(SelectionPodStartEntityId + slot, new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data));
                return;
            }

            var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClass.CharacterSelectionPod);

            newEntityPacket.EntityData.Add(new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data));

            client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
        }
    }
}
