using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Managers;
    using Interfaces;

    public class DynamicObject : IHasPosition
    {
        public DynamicObject()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public ulong EntityId { get; set; }
        public EntityClasses EntityClassId { get; set; }
        public object ObjectData { get; set; }
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public uint MapContextId { get; set; }
        public MapChannel RuntimeMapChannel { get; set; }
        public Factions Faction { get; set; }
        public long RespawnTime { get; set; }
        public DynamicObjectType DynamicObjectType { get; set; }
        public List<Client> TriggeredByPlayers = new List<Client>();
        public string Comment { get; set; }
        public string ScenarioKey { get; set; }
        public string SceneRunId { get; set; }
        public uint SceneOwnerCharacterId { get; set; }
        public uint SceneMissionId { get; set; }
        public string SceneActorRole { get; set; }
        public uint SceneGeneration { get; set; }

        /// <summary>
        /// What keeps this shut, or null if nothing does. Only a locked object can be ciphered,
        /// and only a lock carrying a cipher level can be ciphered at all - see
        /// <see cref="UsableLock"/> for why nothing in the world has one yet.
        /// </summary>
        public UsableLock Lock { get; set; }

        public bool IsInWorld = false;
        public UseObjectState StateId = 0;
        public bool IsEnabled = true;
        public uint WindupTime { get; internal set; }
        public uint ActivateMission { get; internal set; }

        /// <summary>
        /// The LootDispenser attached to this object, or 0. Mirrors Creature.CorpseLootEntityId -
        /// same mechanism, just attached to a scripted prop instead of a kill.
        /// </summary>
        public ulong LootDispenserEntityId { get; set; }
        internal MissionLootSource MissionLootSource { get; set; }
        internal Rasa.Missions.Scenes.SceneObjectConversation MissionConversation { get; set; }
    }
}
