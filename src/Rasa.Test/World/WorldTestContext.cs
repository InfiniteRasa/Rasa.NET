extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    internal sealed class WorldTestContext : IDisposable
    {
        private readonly List<Client> _clients = new();
        private readonly List<EntityClasses> _addedClasses = new();

        internal MapChannel Map { get; } = new()
        {
            MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0),
            ClientList = new List<Client>(),
            PlayerLimit = 128
        };

        internal WorldTestContext()
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            AddClass(EntityClasses.HumanBaseMale);
        }

        internal void AddClass(EntityClasses id)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            if (!classes.ContainsKey(id))
            {
                classes.Add(id, new EntityClass((uint)id, "fixture", 0, 0, new List<AugmentationType>(), true));
                _addedClasses.Add(id);
            }
        }

        internal Client CreateClient(float x = 0, float z = 0, IGameUnitOfWorkFactory factory = null)
        {
            var client = new Client(factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            var player = client.Player;
            player.Id = (uint)(_clients.Count + 1);
            player.Name = $"Player{player.Id}";
            player.FamilyName = "Fixture";
            player.Level = 1;
            player.EntityClass = EntityClasses.HumanBaseMale;
            player.AppearanceData = new Dictionary<EquipmentData, AppearanceData>();
            player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 17).ToList();
            player.MapChannel = Map;
            player.MapContextId = Map.MapInfo.MapContextId;
            player.Position = new Vector3(x, 0, z);
            Map.ClientList.Add(client);
            _clients.Add(client);
            EntityManager.Instance.RegisterEntity(player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(player.EntityId, player);
            EntityManager.Instance.RegisterActor(player.EntityId, player);
            return client;
        }

        internal static List<ProtocolPacket> Drain(Client client)
        {
            var packets = new List<ProtocolPacket>();
            while (client.DequeueOutgoingPacket() is ProtocolPacket packet)
                packets.Add(packet);
            return packets;
        }

        public void Dispose()
        {
            foreach (var client in _clients)
            {
                Drain(client);
                EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
                EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
                EntityManager.Instance.UnregisterActor(client.Player.EntityId);
                EntityManager.Instance.FreeEntity(client.Player.EntityId);
            }
            Map.ClientList.Clear();
            Map.MapCellInfo.Cells.Clear();
            foreach (var id in _addedClasses)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);
        }
    }
}
