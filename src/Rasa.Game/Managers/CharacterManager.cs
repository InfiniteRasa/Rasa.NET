using System.Text.RegularExpressions;

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

            var packet = new BeginCharacterSelectionPacket
            {
                FamilyName = null,
                HasCharacters = false,
                CanSkipBootcamp = true,
                UserId = 1
            };

            packet.EnabledRaceList.Add(1);
            packet.EnabledRaceList.Add(2);
            packet.EnabledRaceList.Add(3);
            packet.EnabledRaceList.Add(4);

            client.SendPacket(5, packet);

            for (var i = 0; i < 16; ++i)
                client.SendPacket(5, new CreatePyhsicalEntityPacket(101 + i, 3543));

            for (var i = 0; i < 16; ++i)
                client.SendPacket(101 + (uint) i, new CharacterInfoPacket(i + 1, true));
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
            //CharacterTable.LastId();
            // Give character basic items
            CharacterInventoryTable.BasicInventory((uint)id);
            // Create default entry in CharacterAbilitiesTable
            CharacterAbilitiesTable.BasicEntry((uint)id);
            // Create default entry in CharacterSkillsTable
            CharacterSkillsTable.BasicEntry((uint)id);

            SendCharacterCreateSuccess(client, packet.SlotNum, packet.FamilyName);
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
            { return CreateCharacterResult.NameTooShort;}

            if (packet.CharacterName.Length > 20)
            { return CreateCharacterResult.NameTooLong;}
            
            if (CharacterTable.IsNameAvailable(packet.CharacterName) == packet.CharacterName)
            { return CreateCharacterResult.NameInUse;}

            if (CharacterTable.IsSlotAvailable(client.Entry.Id, packet.SlotNum) == packet.SlotNum)
            { return CreateCharacterResult.CharacterSlotInUse;}
            
            return !NameRegex.IsMatch(packet.CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }
    }
}
