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
                client.SendPacket(5, new CreatePhysicalEntityPacket(101 + i, 3543));

            for (var i = 0; i < 16; ++i)
                SendCharacterInfo(client, i);
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
            var characterId = CharacterTable.CreateCharacter(client.Entry.Id, packet.CharacterName, packet.FamilyName, packet.SlotNum, packet.Gender, packet.Scale, packet.RaceId);
            // Give character basic items
            ItemManager.Instance.CreateFromTemplateId(characterId, 50, 28, 100);
            ItemManager.Instance.CreateFromTemplateId(characterId, 251, 13126, 1);
            ItemManager.Instance.CreateFromTemplateId(characterId, 252, 13066, 1);
            ItemManager.Instance.CreateFromTemplateId(characterId, 253, 13096, 1);
            ItemManager.Instance.CreateFromTemplateId(characterId, 265, 13186, 1);
            ItemManager.Instance.CreateFromTemplateId(characterId, 266, 13156, 1);
            ItemManager.Instance.CreateFromTemplateId(characterId, 272, 17131, 1);
            // Set character appearance
            CharacterAppearanceTable.SetAppearance(characterId, 1, 10908, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 2, 7054, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 3, 10909, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 14, StarterItemsTable.GetItemTemplateId(packet.AppearanceData[13].ClassId), packet.AppearanceData[13].Color.Hue);
            CharacterAppearanceTable.SetAppearance(characterId, 15, 7052, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 16, 7053, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 17, StarterItemsTable.GetItemTemplateId(packet.AppearanceData[16].ClassId), packet.AppearanceData[16].Color.Hue);
            CharacterAppearanceTable.SetAppearance(characterId, 19, StarterItemsTable.GetItemTemplateId(packet.AppearanceData[18].ClassId), packet.AppearanceData[18].Color.Hue);
            CharacterAppearanceTable.SetAppearance(characterId, 20, StarterItemsTable.GetItemTemplateId(packet.AppearanceData[19].ClassId), packet.AppearanceData[19].Color.Hue);
            CharacterAppearanceTable.SetAppearance(characterId, 21, StarterItemsTable.GetItemTemplateId(packet.AppearanceData[19].ClassId), packet.AppearanceData[20].Color.Hue);
            // Create default entry in CharacterAbilitiesTable
            for (var i = 0; i < 25; i++)
                CharacterAbilityDrawerTable.SetCharacterAbility(characterId, i, 0, 0);
            // Create default entry in CharacterSkillsTable
            for (var i = 0; i < 73; i++)
                CharacterSkillsTable.SetCharacterSkill(characterId, PlayerManager.Instance.SkillIById[i], PlayerManager.Instance.SkillIdx2AbilityID[i], 0);

            SendCharacterCreateSuccess(client, packet.SlotNum, packet.FamilyName);
            UpdateCharacterSelection(client, packet.SlotNum);
        }

        public void RequestDeleteCharacterInSlot(Client client, int slotNum )
        {
            CharacterTable.DeleteCharacter(client.Entry.Id, slotNum);

            client.SendPacket(5, new CharacterDeleteSuccessPacket(CharacterTable.GetCharacterCount(client.Entry.Id) > 0));

            UpdateCharacterSelection(client, slotNum);
        }

        public void RequestSwitchToCharacterInSlot(Client client, int slotNum)
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
                Position = new Position
                {
                    PosX = data.PosX,
                    PosY = data.PosY,
                    PosZ = data.PosZ
                },
                Rotation = data.Rotation
            };

            client.SendPacket(6, packet);

            client.LoadingSlot = slotNum;
            client.LoadingMap = mapData.MapInfo.MapId;
            // early pass client to mapChannel
            var mapChannel = MapChannelManager.Instance.FindByContextId(mapData.MapInfo.MapId);
            MapChannelManager.Instance.PassClientToMapChannel(client, mapChannel);
        }

        private void SendCharacterInfo(Client client, int slotNum)
        {
            var data = CharacterTable.GetCharacterData(client.Entry.Id, slotNum + 1);
            if (data != null)
            {
                var appearanceData = new Dictionary<int, AppearanceData>();
                for (var i = 1; i < 22; i++)
                {
                    var appearance = CharacterAppearanceTable.GetAppearance(data.CharacterId, i);
                    if (appearance.Count == 0)
                    {
                        appearanceData.Add(i, new AppearanceData { SlotId = i, ClassId = 0, Color = new Color(0) });
                        continue;
                    }
                    appearanceData.Add(i, new AppearanceData { SlotId = i, ClassId = appearance[0], Color = new Color(appearance[1]) });
                }

                var packet = new CharacterInfoPacket
                {
                    SlotId = data.SlotId,
                    IsSelected = 1,
                    BodyData = new BodyDataTuple
                    {
                        GenderClassId = data.Gender == 0 ? 692 : 691,
                        Scale = data.Scale
                    },
                    CharacterData = new CharacterDataTuple
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
                    AppearanceData = appearanceData,                    
                    UserName = data.FamilyName,
                    GameContextId = data.MapContextId,
                    LoginData = new LoginDataTupple
                    {
                        NumLogins = data.NumLogins,
                        TotalTimePlayed = data.TotalTimePlayed,
                        TimeSinceLastPlayed = data.TimeSinceLastPlayed
                    },
                    ClanData = new ClanDataTupple
                    {
                        ClanId = data.ClanId,
                        ClanName = data.ClanName
                    }
                };
                
                client.SendPacket(101 + (uint) slotNum, packet);
            }
            else
            {
                var packet = new CharacterInfoPacket
                {
                    SlotId = slotNum + 1,
                    IsSelected = 0  
                };

                client.SendPacket(101 + (uint) slotNum, packet);
            }
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.SendPacket(5, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, int slotNum, string familyName)
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
            
            if (CharacterTable.IsSlotAvailable(client.Entry.Id, packet.SlotNum) == packet.SlotNum)
                return CreateCharacterResult.CharacterSlotInUse;

            if (CharacterTable.GetCharacterCount(client.Entry.Id) == 0)
            {
                if (CharacterTable.IsFamilyNameAvailable(packet.FamilyName) == packet.FamilyName)
                    return CreateCharacterResult.FamilyNameReserved;
            }
            
            return !NameRegex.IsMatch(packet.CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }

        public void UpdateCharacterSelection(Client client, int slotNum)
        {
            SendCharacterInfo(client, slotNum - 1);
        }

        #endregion

        #region InGame
        public static void UpdateCharacter(PlayerData player, int job)
        {
            switch (job)
            {
                case 1: // update level
                    break;
                case 2: // updarte credits
                    break;
                case 3: // update prestige
                    break;
                case 4: // update experience
                    break;
                case 5: // update possition
                    CharacterTable.UpdateCharacterPos(player.CharacterId, player.Actor.Position.PosX, player.Actor.Position.PosY, player.Actor.Position.PosZ, player.Actor.Rotation, player.Actor.MapContextId);
                    break;
                case 6: // update stats
                    break;
                case 7: // update login
                    CharacterTable.UpdateCharacterLogin(player.CharacterId, player.LoginTime); // ToDO LoginTime need to be changed with proper value
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
