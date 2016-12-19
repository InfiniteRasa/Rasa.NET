namespace Rasa.Managers
{
    using Game;
    using Structures;

    public class MapChannelManager
    {
        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();

        public static MapChannelManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapChannelManager();
                    }
                }

                return _instance;
            }
        }

        private MapChannelManager()
        {
        }

        public void MapLoaded(Client client)
        {
            var mapChannel = new MapChannel();
            var owner = new MapChannelClient();
            var characterData = new CharacterData       // basic constant data to enter world, read from db later
            {
                PosY = 350,
                Gender = 0
            };
            CreatePlayerCharacter(client, mapChannel, owner, characterData);
        }

        public void CreatePlayerCharacter(Client client, MapChannel mapChannel, MapChannelClient owner, CharacterData characterData)
        {
            var player = new PlayerData
            {
                Actor = new Actor
                {
                    EntityId = EntityManager.GetFreeEntityIdForPlayer(),
                    PosY = characterData.PosY,
                    EntityClassId = characterData.Gender == 0 ? (uint) 692 : 691
                }
            };

            owner.Player = player;
            owner.Client = client;
            AddNewPlayer(mapChannel, client);
            PlayerManager.AssignPlayer(mapChannel, owner, player);

        }

        public void AddNewPlayer(MapChannel mapChannel, Client client)
        {
            var mapChanelClient = new MapChannelClient
            {
                Client = client,
                ClientEntityId = EntityManager.GetFreeEntityIdForPlayer(),
                MapChannel = mapChannel,
                Player = null
            };

            //DataInterface_Character_getCharacterData(cgm->userID, cgm->mapLoadSlotId, _cb_mapChannel_addNewPlayer, mc);
            // register mapChannelClient
            EntityManager.RegisterEntity(mapChanelClient.ClientEntityId, mapChanelClient);
        }
    }
}