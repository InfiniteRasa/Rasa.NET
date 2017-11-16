using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Entities;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Structures;

    public class CharacterManager
    {
        public const uint SelectionPodStartEntityId = 100;
        public const uint MaxSelectionPods = 16;

        private static readonly Regex NameRegex = new Regex(@"^[\w ]+$", RegexOptions.Compiled);
        private static CharacterManager _instance;
        private static readonly object InstanceLock = new object();

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

            var getFamily = CharacterTable.GetCharacterFamily(client.AccountEntry.Id);

            client.CallMethod(SysEntity.ClientMethodId,
                    getFamily.Length > 1
                        ? new BeginCharacterSelectionPacket(getFamily, true, client.AccountEntry.Id)
                        : new BeginCharacterSelectionPacket(null, false, client.AccountEntry.Id)
                    );

            for (var i = 1U; i <= MaxSelectionPods; ++i)
            {
                var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + i, EntityClass.CharacterSelectionPod);

                newEntityPacket.EntityData.Add(new CharacterInfoPacket(i, true));

                client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
            }

            for (var i = 0U; i < 16; ++i)
                SendCharacterInfo(client, i);

            client.State = ClientState.CharacterSelection;
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = RandomNameTable.GetRandom(gender == 0 ? "male" : "female", "first") ?? (gender == 0 ? "Richard" : "Rachel")
            });
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            var result = CheckName(client, packet);
            var characterData = new Character().CreateCharacter();
            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            // insert character into DB
            var characterId = CharacterTable.CreateCharacter(
                client.AccountEntry.Id,
                packet.CharacterName,
                packet.FamilyName,
                packet.SlotId,
                packet.Gender,
                packet.Scale,
                packet.RaceId,
                characterData.ClassId,
                characterData.GameContextId,
                characterData.Position.PosX,
                characterData.Position.PosY,
                characterData.Position.PosZ,
                characterData.Rotatation,
                characterData.Level,
                characterData.Logos
                );
            // Set character appearance
            CharacterAppearanceTable.SetAppearance(characterId, 1, 10908, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 2, 7054, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 3, 10909, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 15, 7052, -2139062144);
            CharacterAppearanceTable.SetAppearance(characterId, 16, 7053, -2139062144);
            foreach (var t in packet.AppearanceData)
            {
                var v = t.Value;
                CharacterAppearanceTable.SetAppearance(characterId, v.SlotId, StarterItemsTable.GetItemTemplateId(v.ClassId), v.Color.Hue);
            }
            SendCharacterCreateSuccess(client, packet.SlotId, packet.FamilyName);
            UpdateCharacterSelection(client, packet.SlotId);
        }

        public void RequestDeleteCharacterInSlot(Client client, uint slotId)
        {
            CharacterTable.DeleteCharacter(client.AccountEntry.Id, slotId);

            client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(CharacterTable.GetCharacterCount(client.AccountEntry.Id) > 0));

            UpdateCharacterSelection(client, slotId);
        }

        public void RequestFamilyName(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = RandomNameTable.GetRandom("neutral", "last") ?? "Garriott"
            });
        }

        public void RequestSwitchToCharacterInSlot(Client client, uint slotId)
        {
            if (slotId < 1 || slotId > 16)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new SetIsGMPacket(client.AccountEntry.Level > 0));

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());

            var data = CharacterTable.GetCharacterData(client.AccountEntry.Id, slotId);
            var packet = new WonkavatePacket
            {
                GameContextId = data.GameContextId,
                MapInstanceId = 0,                  // ToDo MapInstanceId
                MapVersion = 1556,                  // ToDo
                Position = new Position
                (
                    data.PosX,
                    data.PosY,
                    data.PosZ
                ),
                Rotation = data.Rotation
            };

            client.CallMethod(SysEntity.CurrentInputStateId, packet);
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, uint slotNum, string familyName)
        {
            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(slotNum, familyName));
        }

        public CreateCharacterResult CheckName(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            if (packet.CharacterName.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (packet.CharacterName.Length > 20)
                return CreateCharacterResult.NameTooLong;

            if (CharacterTable.IsNameAvailable(packet.CharacterName) == packet.CharacterName)
                return CreateCharacterResult.NameInUse;

            if (CharacterTable.IsSlotAvailable(client.AccountEntry.Id, packet.SlotId) == packet.SlotId)
                return CreateCharacterResult.CharacterSlotInUse;

            if (CharacterTable.GetCharacterCount(client.AccountEntry.Id) == 0)
            {
                if (CharacterTable.IsFamilyNameAvailable(packet.FamilyName) == packet.FamilyName)
                    return CreateCharacterResult.FamilyNameReserved;
            }

            return !NameRegex.IsMatch(packet.CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }

        private void SendCharacterInfo(Client client, uint slotNum)
        {
            var data = CharacterTable.GetCharacterData(client.AccountEntry.Id, slotNum + 1);
            if (data != null)
            {
                var tempAppearanceData = new List<AppearanceData>();
                var appearance = CharacterAppearanceTable.GetAppearance(data.CharacterId);
                foreach (var t in appearance)
                    tempAppearanceData.Add(new AppearanceData { SlotId = t.SlotId, ClassId = t.ClassId, Color = new Color(t.Color) });

                client.CallMethod(101 + slotNum, new CharacterInfoPacket
                (
                    data.SlotId,
                    true,
                    new BodyDataTuple
                    {
                        GenderClassId = data.Gender == 0 ? 692 : 691,
                        Scale = data.Scale
                    },
                    new CharacterDataTuple
                    {
                        Name = data.Name,
                        GameContextId = data.GameContextId,
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
                    data.GameContextId,
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
                client.CallMethod(101 + slotNum, new CharacterInfoPacket(slotNum + 1, false ));
        }

        public void UpdateCharacterSelection(Client client, uint slotNum)
        {
            SendCharacterInfo(client, slotNum - 1);
        }
    }
}
