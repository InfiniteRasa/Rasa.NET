using System.Collections.Generic;
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
        public const uint SelectionPodStartEntityId = 100;
        public const uint MaxSelectionPods = 16;

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

        #region Character Selection
        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(null, false, client.AccountEntry.Id));

            for (var i = 1U; i <= MaxSelectionPods; ++i)
            {
                var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + i, EntityClass.CharacterSelectionPod);

                newEntityPacket.EntityData.Add(new CharacterInfoPacket(i, true));

                client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
            }

            client.State = ClientState.CharacterSelection;
                SendCharacterInfo(client, (uint)i);
            
            // get userOptions
            var optionsList = UserOptionsTable.GetUserOptions(client.Entry.Id);

            foreach (var userOption in optionsList)
                client.UserOptions.Add(new UserOptions((UserOption)userOption.OptionId, userOption.Value));

            client.SendPacket(5, new UserOptionsPacket(client.UserOptions));
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = RandomNameTable.GetRandom(gender == 0 ? "male" : "female", "first") ?? (gender == 0 ? "Richard" : "Rachel")
            });
        }

        public void RequestFamilyName(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = RandomNameTable.GetRandom("neutral", "last") ?? "Garriott"
            });
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            var result = packet.CheckName();
            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            // todo: save the character to the DB

            SendCharacterCreateSuccess(client, packet.SlotNum, packet.FamilyName);
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, uint slotNum, string familyName)
        {
            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(slotNum, familyName));
        }
    }
}
