using System;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
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

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.CharacterCount > 0, client.AccountEntry.Id));

            var charactersBySlot = CharacterTable.ListCharactersBySlot(client.AccountEntry.Id);

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                if (charactersBySlot.ContainsKey(i))
                    SendCharacterInfo(client, i, charactersBySlot[i], true);
                else
                    SendCharacterInfo(client, i, null, true);
            }

            client.State = ClientState.CharacterSelection;
			
			// get userOptions
            var optionsList = UserOptionsTable.GetUserOptions(client.AccountEntry.Id);

            foreach (var userOption in optionsList)
                client.UserOptions.Add(new UserOptions((UserOption)userOption.OptionId, userOption.Value));

            client.CallMethod(SysEntity.ClientMethodId, new UserOptionsPacket(client.UserOptions));
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = PlayerRandomNameTable.GetRandom((PlayerRandomNameTable.Gender) gender, PlayerRandomNameTable.NameType.First) ?? (gender == 0 ? "Richard" : "Rachel")
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
                    Race = (byte) packet.RaceId,
                    Class = 1,
                    Scale = packet.Scale,
                    Gender = packet.Gender,
                    Experience = 0,
                    Level = 1,
                    Body = 0,
                    Mind = 0,
                    Spirit = 0,
                    MapContextId = 1220,
                    CoordX = 894.9,
                    CoordY = 347.1,
                    CoordZ = 307.9,
                    Rotation = 0
                };

                if (!CharacterTable.CreateCharacter(entry))
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                    return;
                }

                // Set character appearance
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Helmet, 10908, 2139062144));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Shoes, 7054, 2139062144));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Gloves, 10909, 2139062144));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Torso, 7052, 2139062144));
                CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Legs, 7053, 2139062144));

                foreach (var data in packet.AppearanceData)
                {
                    data.Value.Class = (EntityClassId)StarterItemsTable.GetClassId((uint)data.Value.Class);
                    CharacterAppearanceTable.AddAppearance(entry.Id, data.Value.GetDatabaseEntry(entry.Id));
                }

                if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
                {
                    client.AccountEntry.FamilyName = packet.FamilyName;

                    GameAccountTable.UpdateAccount(client.AccountEntry);
                }
            }

            // Give character basic items
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 0, ItemsTable.CreateItem(17131, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[17131]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 50, ItemsTable.CreateItem(28, 100, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[28]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 1, ItemsTable.CreateItem(13126, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13126]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 2, ItemsTable.CreateItem(13066, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13066]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 3, ItemsTable.CreateItem(13096, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13096]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 15, ItemsTable.CreateItem(13186, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13186]].ItemClassInfo.MaxHitPoints, 2139062144));
            CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 16, ItemsTable.CreateItem(13156, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13156]].ItemClassInfo.MaxHitPoints, 2139062144));

            // Create default entry in CharacterAbilitiesTable
            for (var i = 0; i < 25; i++)
                CharacterAbilityDrawerTable.SetCharacterAbility(client.AccountEntry.Id, packet.SlotNum, i, 0, 0);

            // Give character first lockbox tab, if dont exist already
            if (CharacterLockboxTable.GetLockboxInfo(client.AccountEntry.Id).Count < 2)
                CharacterLockboxTable.AddLockboxInfo(client.AccountEntry.Id);

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

            client.CallMethod(SysEntity.ClientMethodId, new SetIsGMPacket(client.AccountEntry.Level > 0));

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());

            var data = CharacterTable.GetCharacter(client.AccountEntry.Id, packet.SlotNum);

            // can character skip boot camp?    -- ToDo
            if (packet.SkipBootcamp)
                data.MapContextId = 1220;
            else
                data.MapContextId = 1220;

            var mapData = MapChannelManager.Instance.MapChannelArray[data.MapContextId];

            client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket
                (
                    mapData.MapInfo.MapContextId,
                    0,                          // ToDo MapInstanceId
                    mapData.MapInfo.MapVersion,
                    new Position(data.CoordX, data.CoordY, data.CoordZ),
                    data.Rotation
                )
                );

            client.AccountEntry.SelectedSlot = packet.SlotNum;
            client.LoadingMap = mapData.MapInfo.MapContextId;
            
            // early pass client to mapChannel
            var mapChannel = MapChannelManager.Instance.FindByContextId(mapData.MapInfo.MapContextId);
            MapChannelManager.Instance.PassClientToMapChannel(client, mapChannel);
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

            var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClassId.CharacterSelectionPod);

            newEntityPacket.EntityData.Add(new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data));

            client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
        }

        #region InGame
        public void UpdateCharacter(Client client, CharacterUpdate job, object value)
        {
            switch (job)
            {
                case CharacterUpdate.Level:
                    break;
                case CharacterUpdate.Credits:
                    //CharacterTable.UpdateCharacterCredits(client.AccountEntry.Id, client.MapClient.Player.CharacterSlot, client.MapClient.Player.Credits);
                    break;
                case CharacterUpdate.Prestige:
                    break;
                case CharacterUpdate.Expirience:
                    break;
                case CharacterUpdate.Position:
                    /*CharacterTable.UpdateCharacterPos(
                        client.AccountEntry.Id,
                        client.MapClient.Player.CharacterSlot,
                        client.MapClient.Player.Actor.Position.PosX,
                        client.MapClient.Player.Actor.Position.PosY,
                        client.MapClient.Player.Actor.Position.PosZ,
                        client.MapClient.Player.Actor.Rotation.W ,  // ToDo rotation
                        client.MapClient.Player.Actor.MapContextId
                        ); */
                    break;
                case CharacterUpdate.Stats:
                    break;
                case CharacterUpdate.Login:
                    var totalTimePlayed = (DateTime.Now - client.MapClient.Player.LoginTime).Minutes + client.MapClient.Player.TotalTimePlayed;

                    CharacterTable.UpdateCharacterLogin(client.MapClient.Player.CharacterId, (uint)totalTimePlayed, client.MapClient.Player.NumLogins);
                    break;
                case CharacterUpdate.Logos:
                    client.MapClient.Player.Logos.Add((int)value);
                    CharacterLogosTable.SetLogos(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, (int)value);
                    client.CallMethod(client.MapClient.Player.Actor.EntityId, new LogosStoneTabulaPacket(client.MapClient.Player.Logos));
                    break;
                case CharacterUpdate.Class:
                    break;
                case CharacterUpdate.FullUpdate:
                    break;
                default:
                    break;

            }
        }
        #endregion
    }
}
