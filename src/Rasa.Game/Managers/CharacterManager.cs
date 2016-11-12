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
            {
                var createPacket = new CreatePyhsicalEntityPacket
                {
                    EntityId = 101 + i,
                    ClassId = 3543
                };

                client.SendPacket(5, createPacket);
            }

            for (var i = 0U; i < 16U; ++i)
            {
                var charPacket = new CharacterInfoPacket((int)i + 1, true);

                client.SendPacket(101 + i, charPacket);
            }
        }

        public void RequestCharacterName(Client client, int gender)
        {
            var response = new GeneratedCharacterNamePacket
            {
                Name = RandomNameTable.GetRandom(gender == 0 ? "male" : "female", "first") ?? (gender == 0 ? "Richard" : "Rachel")
            };

            client.SendPacket(5, response);
        }

        public void RequestFamilyName(Client client)
        {
            var response = new GeneratedFamilyNamePacket
            {
                Name = RandomNameTable.GetRandom("neatural", "last") ?? "Garriott"
            };

            client.SendPacket(5, response);
        }
    }
}
