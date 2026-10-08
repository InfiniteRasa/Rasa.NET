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

        /// <summary>
        /// Where the object is filed in its map's cells when that is not where it stands: scenery
        /// that is in sight from further off than the two cells around its own (SecretPassages).
        /// Null for everything else.
        /// </summary>
        public Vector3? CellAnchor { get; set; }

        /// <summary>The position that picks the object's cell, and with it who is shown the object.</summary>
        public Vector3 CellPosition => CellAnchor ?? Position;

        public uint MapContextId { get; set; }
        public MapChannel RuntimeMapChannel { get; set; }

        /// <summary>
        /// For a control point or a dropship, whose side it is - FRIENDLY for the AFS, HOSTILE for
        /// the Bane; OBJECT for an ability's usable. Objects are not sent it; the client's usable
        /// classes work out their own.
        /// </summary>
        public TargetCategory TargetCategory { get; set; }
        public long RespawnTime { get; set; }
        public DynamicObjectType DynamicObjectType { get; set; }
        public List<Client> TriggeredByPlayers = new List<Client>();

        /// <summary>
        /// The actor partway through a timed use of this object - a control point capture, a logos
        /// tablet being taken - or null. Clients are told with LockToActor; see
        /// DynamicObjectManager.TryLockForUse.
        /// </summary>
        public Actor UsedBy { get; set; }

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
        /// The name the client shows in place of its class's (UsableInfo's nameOverrideId, a row
        /// of the client's usablenameoverridelanguage), or 0 for the class's own.
        /// </summary>
        public uint NameOverrideId { get; internal set; }

        /// <summary>
        /// The LootDispenser attached to this object, or 0. Mirrors Creature.CorpseLootEntityId -
        /// same mechanism, just attached to a scripted prop instead of a kill.
        /// </summary>
        public ulong LootDispenserEntityId { get; set; }
        internal MissionLootSource MissionLootSource { get; set; }
        internal Rasa.Missions.Scenes.SceneObjectConversation MissionConversation { get; set; }
        internal Rasa.Missions.Scenes.SceneObjectAction MissionUseAction { get; set; }
        internal Rasa.Missions.Scenes.SceneObjectDestruction MissionDestruction { get; set; }
        internal uint CurrentHitPoints { get; set; }
    }
}
