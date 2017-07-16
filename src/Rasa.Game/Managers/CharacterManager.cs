using System;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;

    public class CharacterManager
    {
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

            var packet = new BeginCharacterSelectionPacket(null, false, 1);

            packet.EnabledRaceList.Add(1);
            packet.EnabledRaceList.Add(2);
            packet.EnabledRaceList.Add(3);
            packet.EnabledRaceList.Add(4);

            client.SendPacket(5, packet);

            for (var i = 0U; i < 16U; ++i)
                client.SendPacket(5, new CreatePhysicalEntityPacket(101U + i, 3543));

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
            client.SendPacket(5, new UserCreationFailedPacket(result));
        }

        private void SendCharacterCreateSuccess(Client client, int slotNum, string familyName)
        {
            client.SendPacket(5, new CharacterCreateSuccessPacket(slotNum, familyName));
        }
    }
}
