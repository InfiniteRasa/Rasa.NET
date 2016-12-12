using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Rasa.Structures;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Database.Tables.Character;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;

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
                client.SendPacket(5, new CreatePyhsicalEntityPacket(101 + i, 3543));

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
            var id = CharacterTable.CreateCharacter(client.Entry.Id, packet.CharacterName, packet.FamilyName, packet.SlotNum, packet.Gender, packet.Scale, packet.RaceId);
            // Give character basic items
            CharacterInventoryTable.BasicInventory((uint)id);
            // Give character basic equipment
            //CharacterEquipmentTable.UpdateEquipment((uint)id, 10908, -2139062144, 7054, -2139062144, 10909, -2139062144, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, packet.AppearanceData[13].ClassId, packet.AppearanceData[13].Color.Hue, 7052, -2139062144, 7053, -2139062144, packet.AppearanceData[16].ClassId, packet.AppearanceData[16].Color.Hue, packet.AppearanceData[17].ClassId, packet.AppearanceData[17].Color.Hue, packet.AppearanceData[18].ClassId, packet.AppearanceData[18].Color.Hue, packet.AppearanceData[19].ClassId, packet.AppearanceData[19].Color.Hue, packet.AppearanceData[20].ClassId, packet.AppearanceData[20].Color.Hue);

            CharacterEquipmentTable.BasicEquipment((uint) id, 10908, -2139062144, 7054, -2139062144, 10909, -2139062144, ItemTemplateItemClassTable.GetClassId(packet.AppearanceData[13].ClassId), packet.AppearanceData[13].Color.Hue, 7052, -2139062144, 7053, -2139062144, ItemTemplateItemClassTable.GetClassId(packet.AppearanceData[16].ClassId), packet.AppearanceData[16].Color.Hue, ItemTemplateItemClassTable.GetClassId(packet.AppearanceData[18].ClassId), packet.AppearanceData[18].Color.Hue, ItemTemplateItemClassTable.GetClassId(packet.AppearanceData[19].ClassId), packet.AppearanceData[19].Color.Hue);
            // Create default entry in CharacterAbilitiesTable
            CharacterAbilitiesTable.BasicEntry((uint)id);
            // Create default entry in CharacterSkillsTable
            CharacterSkillsTable.BasicEntry((uint)id);

            
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

            client.SendPacket(5, new PreWonkavatePacket(0)); // ToDo need to see what is this

            var data = CharacterTable.GetCharacterData(client.Entry.Id, slotNum);
            var packet = new WonkavatePacket
            {
                MapContextId = data.MapContextId,
                MapInstanceId = 0,                  // ToDo MapInstanceId / MapVersion
                MapVersion = 0,
                PosX = data.PosX,
                PosY = data.PosY,
                PosZ = data.PosZ,
                Rotation = data.Rotation
            };

            client.SendPacket(5, packet);
            Console.WriteLine("Connecting to mapChannel...");
        }

        private void SendCharacterInfo(Client client, int slotNum)
        {
            var data = CharacterTable.GetCharacterData(client.Entry.Id, slotNum + 1);
            
            if (data != null)
            {
                var equipment = CharacterEquipmentTable.GetEquipment(data.Id);
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
                    AppearanceData = new Dictionary<int, AppearanceData>
                    {
                        { 1, new AppearanceData{ SlotId = 1, ClassId = equipment.Helmet, Color = new Color(equipment.HelmetHue) } },
                        { 2, new AppearanceData{ SlotId = 2, ClassId = equipment.Shoes, Color = new Color(equipment.ShoesHue) } },
                        { 3, new AppearanceData{ SlotId = 3, ClassId = equipment.Gloves, Color = new Color(equipment.GlovesHue) } },
                        { 4, new AppearanceData{ SlotId = 4, ClassId = equipment.Slot4,  Color = new Color(equipment.Slot4Hue) } },
                        { 5, new AppearanceData{ SlotId = 5, ClassId = equipment.Slot5, Color = new Color(equipment.Slot5Hue) } },
                        { 6, new AppearanceData{ SlotId = 6, ClassId = equipment.Slot6, Color = new Color(equipment.Slot6Hue) } },
                        { 7, new AppearanceData{ SlotId = 7, ClassId = equipment.Slot7, Color = new Color(equipment.Slot7Hue) } },
                        { 8, new AppearanceData{ SlotId = 8, ClassId = equipment.Slot8, Color = new Color(equipment.Slot8Hue) } },
                        { 9, new AppearanceData{ SlotId = 9, ClassId = equipment.Slot9, Color = new Color(equipment.Slot9Hue) } },
                        { 10, new AppearanceData{ SlotId = 10, ClassId = equipment.Slot10, Color = new Color(equipment.Slot10Hue) } },
                        { 11, new AppearanceData{ SlotId = 11, ClassId = equipment.Slot11, Color = new Color(equipment.Slot11Hue) } },
                        { 12, new AppearanceData{ SlotId = 12, ClassId = equipment.Slot12, Color = new Color(equipment.Slot12Hue) } },
                        { 13, new AppearanceData{ SlotId = 13, ClassId = equipment.Weapon, Color = new Color(equipment.WeaponHue) } },
                        { 14, new AppearanceData{ SlotId = 14, ClassId = equipment.Hair, Color = new Color(equipment.HairHue) } },
                        { 15, new AppearanceData{ SlotId = 15, ClassId = equipment.Torso, Color = new Color(equipment.TorsoHue) } },
                        { 16, new AppearanceData{ SlotId = 16, ClassId = equipment.Legs, Color = new Color(equipment.LegsHue) } },
                        { 17, new AppearanceData{ SlotId = 17, ClassId = equipment.Face, Color = new Color(equipment.FaceHue) } },
                        { 18, new AppearanceData{ SlotId = 18, ClassId = equipment.Wing, Color = new Color(equipment.Wing) } },
                        { 19, new AppearanceData{ SlotId = 19, ClassId = equipment.EyeWeare, Color = new Color(equipment.EyeWeareHue) } },
                        { 20, new AppearanceData{ SlotId = 20, ClassId = equipment.Beard, Color = new Color(equipment.BeardHue) } },
                        { 21, new AppearanceData{ SlotId = 21, ClassId = equipment.Mask, Color = new Color(equipment.MaskHue) } }
                    },
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

            if (CharacterTable.IsFamilyNameAvailable(packet.FamilyName) == packet.FamilyName)
                return CreateCharacterResult.FamilyNameReserved;

            return !NameRegex.IsMatch(packet.CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }

        public void UpdateCharacterSelection(Client client, int slotNum)
        {
            SendCharacterInfo(client, slotNum - 1);
        }
    }
}
