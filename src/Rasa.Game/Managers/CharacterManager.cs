﻿namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Game;
    using Managers;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Structures;

    public class CharacterManager
    {
        public const uint SelectionPodStartEntityId = 100;
        public const byte MaxSelectionPods = 16;

        private static CharacterManager _instance;
        private static readonly object InstanceLock = new object();

        private readonly object CreateLock = new object();

        public static CharacterManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (InstanceLock)
                {
                    if (_instance == null)
                        _instance = new CharacterManager();
                }

                return _instance;
            }
        }

        private CharacterManager()
        {
        }

        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.CharacterCount > 0, client.AccountEntry.Id, client.AccountEntry.CanSkipBootcamp));

            var charactersBySlot = CharacterTable.ListCharactersBySlot(client.AccountEntry.Id);

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

            CharacterEntry entry;
            bool changeFamilyName = false;

            lock (CreateLock)
            {
                if (!string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) && packet.FamilyName != client.AccountEntry.FamilyName)
                {
                    if (client.AccountEntry.CharacterCount == 0)
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
                    var familyNameOwnerAccount = GameAccountTable.GetAccountByFamilyName(packet.FamilyName);

                    if (familyNameOwnerAccount != null && client.AccountEntry.Id != familyNameOwnerAccount.Id)
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.FamilyNameReserved);
                        return;
                    }

                }

                entry = new CharacterEntry
                {
                    AccountId = client.AccountEntry.Id,
                    Slot = packet.SlotNum,
                    Name = packet.CharacterName,
                    Race = (byte)packet.RaceId,
                    Class = 1,
                    Scale = packet.Scale,
                    Gender = packet.Gender,
                    Experience = 0,
                    Level = 1,
                    Body = 0,
                    Mind = 0,
                    Spirit = 0,
                    ContextId = 1220,
                    CoordX = 894.9d,
                    CoordY = 307.9d,
                    CoordZ = 347.1d,
                    Rotation = 0,
                };

                if (!CharacterTable.CreateCharacter(entry))
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                    return;
                }

                // Set character default appearance
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Shoes, 10000068, new Color(255, 255, 255, 255).Hue));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Legs, 10000069, new Color(255, 255, 255, 255).Hue));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Torso, 10000070, new Color(255, 255, 255, 255).Hue));

                foreach (var data in packet.AppearanceData)
                {
                    data.Value.ClassId = ItemTemplateItemClassTable.GetItemClassId(data.Value.ClassId);
                    CharacterAppearanceTable.AddAppearance(entry.Id, data.Value.GetDatabaseEntry(entry.Id));
                }

                if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
                {
                    client.AccountEntry.FamilyName = packet.FamilyName;

                    GameAccountTable.UpdateAccount(client.AccountEntry);
                }
            }

            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, packet.FamilyName));
            ++client.AccountEntry.CharacterCount;

            SendCharacterInfo(client, packet.SlotNum, entry, false);
        }

        public void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet)
        {
            try
            {
                var charactersBySlot = CharacterTable.ListCharactersBySlot(client.AccountEntry.Id);
                if (charactersBySlot.ContainsKey(packet.Slot))
                {
                    CharacterTable.DeleteCharacter(charactersBySlot[packet.Slot].Id);

                    client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(--client.AccountEntry.CharacterCount > 0));

                    SendCharacterInfo(client, packet.Slot, null, false);

                    return;
                }
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

            client.AccountEntry.SelectedSlot = packet.SlotNum;
            GameAccountTable.UpdateAccount(client.AccountEntry);

            var character = CharacterTable.GetCharacter(client.AccountEntry.Id, packet.SlotNum);

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
