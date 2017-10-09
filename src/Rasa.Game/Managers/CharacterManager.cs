namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;

    public class CharacterManager
    {
        public const uint SelectionPodStartEntityId = 100;

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

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(null, false, client.AccountEntry.Id));

            for (var i = 1U; i <= 16U; ++i)
            {
                var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + i, EntityClass.CharacterSelectionPod);

                newEntityPacket.EntityData.Add(new CharacterInfoPacket(i, true));

                client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
            }

            client.State = ClientState.CharacterSelection;
        }

        public void RequestCharacterName(Client client, int gender)
        {
            client.CallMethod(5, new GeneratedCharacterNamePacket
            {
                Name = RandomNameTable.GetRandom(gender == 0 ? "male" : "female", "first") ?? (gender == 0 ? "Richard" : "Rachel")
            });
        }

        public void RequestFamilyName(Client client)
        {
            client.CallMethod(5, new GeneratedFamilyNamePacket
            {
                Name = RandomNameTable.GetRandom("neatural", "last") ?? "Garriott"
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
            client.CallMethod(5, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, int slotNum, string familyName)
        {
            client.CallMethod(5, new CharacterCreateSuccessPacket(slotNum, familyName));
        }
    }
}
