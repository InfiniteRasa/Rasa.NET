using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Database.Tables.Character;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class CharacterManager
    {
        private static readonly Regex NameRegex = new Regex(@"^[\w ]+$", RegexOptions.Compiled);
        private static CharacterManager _instance;
        private static readonly object InstanceLock = new object();

        public static CharacterManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CharacterManager();
                    }
                }

                return _instance;
            }
        }

        private CharacterManager()
        {
        }

        #region Character Selection
        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            var getFamily = CharacterTable.GetCharacterFamily(client.Entry.Id);
            client.SendPacket(5,
                getFamily.Length > 1
                    ? new BeginCharacterSelectionPacket(getFamily, true, client.Entry.Id)
                    : new BeginCharacterSelectionPacket(null, false, client.Entry.Id));

            for (var i = 0; i < 16; ++i)
                client.SendPacket(5, new CreatePhysicalEntityPacket(101 + (uint)i, 3543));

            for (var i = 0; i < 16; ++i)
                SendCharacterInfo(client, (uint)i);
            
            // get userOptions
            var optionsList = UserOptionsTable.GetUserOptions(client.Entry.Id);

            foreach (var userOption in optionsList)
                client.UserOptions.Add(new UserOptions((UserOption)userOption.OptionId, userOption.Value));

            client.SendPacket(5, new UserOptionsPacket(client.UserOptions));
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.SendPacket(5, new GeneratedCharacterNamePacket
            {
                Name = RandomNameTable.GetRandom(gender == 0 ? "male" : "female", "first") ?? (gender == 0 ? "Richard" : "Rachel")
            });
        }

        public void RequestFamilyName(Client client)
        {
            client.SendPacket(5, new GeneratedFamilyNamePacket
            {
                Name = RandomNameTable.GetRandom("neatural", "last") ?? "Garriott"
            });
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            var result = CheckName(client, packet);

            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }
            // insert character into DB
            CharacterTable.CreateCharacter(client.Entry.Id, packet.SlotNum, packet.CharacterName, packet.FamilyName, packet.Gender, packet.Scale, packet.RaceId);
            
            // Give character basic items
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.Personal, 0, ItemsTable.CreateItem(17131, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[17131]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.Personal, 50, ItemsTable.CreateItem(28, 100, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[28]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 1, ItemsTable.CreateItem(13126, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13126]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 2, ItemsTable.CreateItem(13066, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13066]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 3, ItemsTable.CreateItem(13096, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13096]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 15, ItemsTable.CreateItem(13186, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13186]].ItemClassInfo.MaxHitPoints, -2139062144));
            CharacterInventoryTable.AddInvItem(client.Entry.Id, packet.SlotNum, (int)InventoryType.EquipedInventory, 16, ItemsTable.CreateItem(13156, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13156]].ItemClassInfo.MaxHitPoints, -2139062144));
            
            // Set character appearance
            CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)EquipmentSlots.Helmet, 10908, -2139062144);
            CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)EquipmentSlots.Shoes, 7054, -2139062144);
            CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)EquipmentSlots.Gloves, 10909, -2139062144);
            CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)EquipmentSlots.Torso, 7052, -2139062144);
            CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)EquipmentSlots.Legs, 7053, -2139062144);
            foreach (var t in packet.AppearanceData)
            {
                var v = t.Value;
                CharacterAppearanceTable.SetAppearance(client.Entry.Id, packet.SlotNum, (int)v.SlotId, StarterItemsTable.GetClassId(v.ClassId), v.Color.Hue);
            }
            
            // Create default entry in CharacterAbilitiesTable
            for (var i = 0; i < 25; i++)
                CharacterAbilityDrawerTable.SetCharacterAbility(client.Entry.Id, packet.SlotNum, i, 0, 0);
            
            // Give character first lockbox tab, if dont exist already
            if (CharacterLockboxTable.GetLockboxInfo(client.Entry.Id).Count < 2)
                CharacterLockboxTable.AddLockboxInfo(client.Entry.Id);

            SendCharacterCreateSuccess(client, packet.SlotNum, packet.FamilyName);
            UpdateCharacterSelection(client, packet.SlotNum);
        }

        public void RequestDeleteCharacterInSlot(Client client, uint slotNum )
        {
            CharacterTable.DeleteCharacter(client.Entry.Id, slotNum);

            client.SendPacket(5, new CharacterDeleteSuccessPacket(CharacterTable.GetCharacterCount(client.Entry.Id) > 0));

            UpdateCharacterSelection(client, slotNum);
        }

        public void RequestSwitchToCharacterInSlot(Client client, uint slotNum)
        {
            if (slotNum < 1 || slotNum > 16)
                return;

            client.SendPacket(5, new SetIsGMPacket(client.Entry.Level > 0 ));

            client.SendPacket(5, new PreWonkavatePacket());

            var data = CharacterTable.GetCharacterData(client.Entry.Id, slotNum);
            var mapData = MapChannelManager.Instance.MapChannelArray[data.MapContextId];
            var packet = new WonkavatePacket
            {
                MapContextId = mapData.MapInfo.MapId,
                MapInstanceId = 0,                  // ToDo MapInstanceId
                MapVersion = mapData.MapInfo.MapVersion,
                Position = new Position(data.PosX, data.PosY, data.PosZ),
                Rotation = data.Rotation
            };

            client.SendPacket(6, packet);

            client.LoadingSlot = slotNum;
            client.LoadingMap = mapData.MapInfo.MapId;
            // early pass client to mapChannel
            var mapChannel = MapChannelManager.Instance.FindByContextId(mapData.MapInfo.MapId);
            MapChannelManager.Instance.PassClientToMapChannel(client, mapChannel);
        }

        private void SendCharacterInfo(Client client, uint slotNum)
        {
            var data = CharacterTable.GetCharacterData(client.Entry.Id, slotNum + 1);

            if (data != null)
            {
                var tempAppearanceData = new List<AppearanceData>();
                var appearance = CharacterAppearanceTable.GetAppearance(client.Entry.Id, slotNum + 1);

                foreach (var t in appearance)
                    tempAppearanceData.Add(new AppearanceData { SlotId = (EquipmentSlots)t.SlotId, ClassId = t.ClassId, Color = new Color(t.Color) });

                client.SendPacket(101U + slotNum, new CharacterInfoPacket
                (
                    slotNum + 1,
                    1,
                    new BodyDataTuple
                    {
                        GenderClassId = data.Gender == 0 ? 692 : 691,
                        Scale = data.Scale
                    },
                    new CharacterDataTuple
                    {
                        Name = data.Name,
                        MapContextId = data.MapContextId,
                        Experience = data.Experience,
                        Level = data.Level,
                        Body = data.Body,
                        Mind = data.Mind,
                        Spirit = data.Spirit,
                        ClassId = data.ClassId,
                        CloneCredits = data.CloneCredits,
                        RaceId = data.RaceId
                    },
                    tempAppearanceData,
                    data.FamilyName,
                    data.MapContextId,
                    new LoginDataTupple
                    {
                        NumLogins = data.NumLogins,
                        TotalTimePlayed = data.TotalTimePlayed,
                        TimeSinceLastPlayed = data.TimeSinceLastPlayed
                    },
                    new ClanDataTupple
                    {
                        ClanId = data.ClanId,
                        ClanName = data.ClanName
                    }
                ));
            }
            else
                client.SendPacket(101 + slotNum, new CharacterInfoPacket(slotNum + 1, 0));
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.SendPacket(5, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, uint slotNum, string familyName)
        {
            client.SendPacket(5, new CharacterCreateSuccessPacket(slotNum, familyName));
        }
        
        public CreateCharacterResult CheckName(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            if (packet.CharacterName.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (packet.CharacterName.Length > 20)
                return CreateCharacterResult.NameTooLong;
            
            if (CharacterTable.IsNameAvailable(packet.CharacterName) == packet.CharacterName)
                return CreateCharacterResult.NameInUse;
            
            if (CharacterTable.IsSlotAvailable(client.Entry.Id, packet.SlotNum) == false)
                return CreateCharacterResult.CharacterSlotInUse;

            if (CharacterTable.GetCharacterCount(client.Entry.Id) == 0)
            {
                if (CharacterTable.IsFamilyNameAvailable(packet.FamilyName) == packet.FamilyName)
                    return CreateCharacterResult.FamilyNameReserved;
            }
            
            return !NameRegex.IsMatch(packet.CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }

        public void UpdateCharacterSelection(Client client, uint slotNum)
        {
            SendCharacterInfo(client, slotNum - 1);
        }

        #endregion

        #region InGame
        public void UpdateCharacter(Client client, int job)
        {
            switch (job)
            {
                case 1: // update level
                    break;
                case 2: // updarte credits
                    CharacterTable.UpdateCharacterCredits(client.Entry.Id, client.MapClient.Player.CharacterSlot, client.MapClient.Player.Credits);
                    break;
                case 3: // update prestige
                    break;
                case 4: // update experience
                    break;
                case 5: // update possition
                    CharacterTable.UpdateCharacterPos(
                        client.Entry.Id,
                        client.MapClient.Player.CharacterSlot,
                        client.MapClient.Player.Actor.Position.PosX,
                        client.MapClient.Player.Actor.Position.PosY,
                        client.MapClient.Player.Actor.Position.PosZ,
                        client.MapClient.Player.Actor.Rotation.W ,  // ToDo rotation
                        client.MapClient.Player.Actor.MapContextId
                        );   
                    break;
                case 6: // update stats
                    break;
                case 7: // update login
                    CharacterTable.UpdateCharacterLogin(client.Entry.Id, client.MapClient.Player.CharacterSlot, client.MapClient.Player.LoginTime); // ToDO LoginTime need to be changed with proper value
                    break;
                case 8: // update logos
                    break;
                case 9: // update class
                    break;
                default:
                    break;

            }
        }
        #endregion
    }
}
