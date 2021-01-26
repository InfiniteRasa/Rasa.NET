using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    using Positioning;

    public class Actor : IHasPosition
    {
        public Actor()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }
        public ulong EntityId { get; set; }
        public EntityClassId EntityClassId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public Vector3 Position { get; set; }
        public float Orientation { get; set; }
        public uint MapContextId { get; set; }
        public bool IsRunning { get; set; }
        public bool InCombatMode { get; set; }
        public CharacterState State { get; set; }
        public bool WeaponReady { get; set; }
        // action data
        public int CurrentAction { get; set; }
        public Dictionary<Attributes, ActorAttributes> Attributes = new Dictionary<Attributes, ActorAttributes>();
        public Dictionary<int, GameEffect> ActiveEffects { get; set; } = new Dictionary<int, GameEffect>();
        public uint[,] Cells = new uint[5 ,5];
        // sometimes we only have access to the actor, the owner variable allows us to access the client anyway (only if actor is a player manifestation)
        //public MapChannelClient Owner { get; set; }
    }
}
