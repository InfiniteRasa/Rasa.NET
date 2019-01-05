using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets;
    using Structures;
    using System;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();

        public static DynamicObjectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new DynamicObjectManager();
                    }
                }

                return _instance;
            }
        }

        private DynamicObjectManager()
        {
        }

        public void InitDynamicObjects()
        {
            InitTeleporters();
        }

        public void InitTeleporters()
        {
            var teleporters = TeleporterTable.GetTeleporters();

            foreach (var teleporter in teleporters)
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(teleporter.MapContextId);

                var newTeleporter = new DynamicObject
                {
                    Position = new Vector3(teleporter.CoordX, teleporter.CoordY,teleporter.CoordZ),
                    Rotation = Quaternion.CreateFromYawPitchRoll(teleporter.Rotation, 0f, 0f),
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = (EntityClassId)teleporter.EntityClassId
                };

                mapChannel.DynamicObjects.Add(newTeleporter);
            }      
        }

        public void ForceState(Client client, uint entityId, int state)
        {
            client.CallMethod(entityId, new ForceStatePacket(state, 200));
        }

        public void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            client.CallMethod((uint)packet.EntityId, new UsePacket(client.MapClient.Player.Actor.EntityId, 1, 0));
        }

        public void DynamicObjectWorker(MapChannel mapChannel, long delta)
        {
            foreach (var obj in mapChannel.DynamicObjects)
            {
                // spawn object
                if (!obj.IsInWorld)
                {
                    obj.RespawnTime -= delta;

                    if (obj.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, obj);
                        obj.IsInWorld = true;
                    }
                }
            }
        }

        // 1 object to n client's
        public void CellIntroduceDynamicObjectToClients(DynamicObject dynamicObject, List<Client> listOfClients)
        {
            foreach (var client in listOfClients)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        // n creatures to 1 client
        public void CellIntroduceDynamicObjectsToClient(Client client, List<DynamicObject> listOfObjects)
        {
            foreach (var dynamicObject in listOfObjects)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        public void CreateDynamicObjectOnClient(Client client, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo((EntityClassId)EntityManager.Instance.GetEntityClassId(dynamicObject.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Rotation)
            };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
        }

        public void CellDiscardDynamicObjectsToClient(Client client, List<DynamicObject> discardObjects)
        {
            foreach (var dynamicObject in discardObjects)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(dynamicObject.EntityId));
        }
    }
}
