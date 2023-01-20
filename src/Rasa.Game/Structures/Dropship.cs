using System;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Managers;

    public class Dropship : DynamicObject
    {
        public long PhaseTimeleft { get; set; }
        public byte Phase { get; set; }
        public SpawnPool SpawnPool { get; set; }
        public DropshipType DropshipType { get; set; }
        internal Client Client { get; set; }
        internal Vector3 Destination { get; set; }
        internal uint DestinationMapId { get; set; }

        public Dropship(Factions faction, DropshipType dropshipType, SpawnPool spawnPool = null)
        {
            EntityId = EntityManager.Instance.GetEntityId;
            EntityClassId = faction == Factions.AFS ? Data.EntityClasses.UsableCrSpawnerHumDropshipV01 : Data.EntityClasses.UsableCrSpawnerBaneDropshipV01;
            Faction = faction;
            StateId = UseObjectState.CsStateBegin;
            PhaseTimeleft = 5000;
            Phase = 0;
            DropshipType = dropshipType;
            SpawnPool = spawnPool;
            MapContextId = spawnPool.MapContextId;
            Position = new Vector3(
                spawnPool.Position.X + (2.0f - (new Random().Next() % 100) * 0.04f),
                spawnPool.Position.Y,
                spawnPool.Position.Z + (2.0f - (new Random().Next() % 100) * 0.04f)
                );
            Rotation = (new Random().Next() % 640) * 0.01f;
        }
        
        public Dropship(Factions faction, DropshipType dropshipType, Client client, Vector3 destination = new Vector3(), uint destinationMapId = 0)
        {
            EntityId = EntityManager.Instance.GetEntityId;
            EntityClassId = faction == Factions.AFS ? Data.EntityClasses.UsableCrSpawnerHumDropshipV01 : Data.EntityClasses.UsableCrSpawnerBaneDropshipV01;
            Faction = faction;
            StateId = UseObjectState.CsStateBegin;
            PhaseTimeleft = 5000;
            Phase = 0;
            DropshipType = dropshipType;
            Client = client;

            if (client.State == ClientState.Teleporting)
                MapContextId = client.LoadingMap;
            else
                MapContextId = client.Player.MapContextId;

            Position = client.Player.Position;
            Rotation = (new Random().Next() % 640) * 0.01f;
            DynamicObjectType = DynamicObjectType.DropshipTeleporter;
            Destination = destination;
            DestinationMapId = destinationMapId;
        }
    }
}
