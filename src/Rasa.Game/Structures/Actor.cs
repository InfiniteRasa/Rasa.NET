using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    using Interfaces;

    public class Actor : IHasPosition
    {
        public ulong EntityId = EntityManager.Instance.GetEntityId;
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public bool IsCrouching { get; set; }
        public EntityClasses EntityClass { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public uint MapContextId { get; set; }
        public bool IsRunning { get; set; }
        public bool InCombatMode { get; set; }
        public CharacterState State { get; set; }
        public ulong Target { get; set; }
        public double MovementSpeed { get; set; }
        public bool WeaponReady { get; set; }
        public MapChannel RuntimeMapChannel { get; set; }
        // action data
        public int CurrentAction { get; set; }
        public Dictionary<Attributes, ActorAttributes> Attributes = new Dictionary<Attributes, ActorAttributes>();
        public Dictionary<int, GameEffect> ActiveEffects { get; set; } = new Dictionary<int, GameEffect>();

        /// <summary>
        /// Environment.TickCount64 at which each action comes off cooldown, by action id. An
        /// action not in here, or past its tick, is ready. Kept per action rather than per
        /// action and level, as the client does (SetActionReuseTime keys by actionId).
        /// </summary>
        public Dictionary<ActionId, long> ActionReuseUntil { get; } = new Dictionary<ActionId, long>();

        public uint[,] Cells = new uint[5 ,5];
        // sometimes we only have access to the actor, the owner variable allows us to access the client anyway (only if actor is a player manifestation)
        //public MapChannelClient Owner { get; set; }
    }
}
