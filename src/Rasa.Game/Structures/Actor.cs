using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    using Interfaces;

    public class Actor : IHasPosition
    {
        /// <summary>
        /// The actor's entity id, taken from the pool (EntityManager.GetEntityId) the first time it
        /// is read, unless one was given first. It used to be taken in a field initialiser, so every
        /// Actor built took one - each creature definition loaded, each clone's source, each
        /// throwaway - and one given afterwards left the first taken for good.
        /// </summary>
        public ulong EntityId
        {
            get
            {
                var id = Volatile.Read(ref _entityId);

                if (id != NotTaken)
                    return id;

                // Two first reads at once: one id is kept and the other goes back to the pool.
                var taken = EntityManager.Instance.GetEntityId;
                var kept = Interlocked.CompareExchange(ref _entityId, taken, NotTaken);

                if (kept == NotTaken)
                    return taken;

                EntityManager.Instance.FreeEntity(taken);
                return kept;
            }
            set => Volatile.Write(ref _entityId, value);
        }

        /// <summary>No id taken or given yet: the pool hands out ids counting up from 1000, never this.</summary>
        private const ulong NotTaken = ulong.MaxValue;
        private ulong _entityId = NotTaken;

        /// <summary>Whether the actor has an entity id yet (read or given), without taking one.</summary>
        internal bool HasEntityId => Volatile.Read(ref _entityId) != NotTaken;
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public bool IsCrouching { get; set; }

        /// <summary>Where the actor was when it last crouched (ManifestationManager.StandIfMoved).</summary>
        public Vector3 CrouchedAt { get; set; }
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

        /// <summary>Takes nothing from any hit: every one shows "Immune" (Managers.DamageImmunity).</summary>
        public bool ImmuneToAllDamage { get; set; }

        /// <summary>The damage types a hit of which shows "Immune" and takes nothing (Managers.DamageImmunity).</summary>
        public HashSet<DamageType> DamageImmunities { get; } = new HashSet<DamageType>();

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
