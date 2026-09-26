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

        /// <summary>
        /// The entity the actor's movement is locked on (the client's SetTrackingTarget), 0 for
        /// none: what a player's client tracks while following someone or walking up to use
        /// something, or what .track set. Not the selected Target. See Managers.TrackingTargets.
        /// </summary>
        public ulong TrackingTargetEntityId { get; set; }

        /// <summary>
        /// What the effects on the actor make its speed: 1.0 is normal, a slow below it
        /// (GameEffectManager.UpdateMovementMod). BehaviorManager moves a creature at its speed
        /// times this, so it starts at 1.0 for every actor: it used to start at 0 for creatures,
        /// which only a movement effect or ClearEffects ever raised, and a creature with neither -
        /// every one fresh from a spawn pool or a summon - could turn to face its target but never
        /// took a step towards it, nor wandered.
        /// </summary>
        public double MovementSpeed { get; set; } = 1.0d;
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
