using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The effects a hit puts on what it lands on, announced by the hit.
    ///
    /// A hit's rawInfo ends in targetEffectIds and sourceEffectIds (shared/damageinfo.py), and
    /// the client's Actor.AnnounceDamage, playing the hit, calls AnnounceGameEffectAttach for
    /// each: it announces - starts the FX of, and posts the status icon of - the first effect
    /// of that type on the entity that has not been announced yet, or, when there is none yet,
    /// the next one of that type attached within ten seconds (PhysicalEntity.__delayedAnnounce).
    /// They are effect type ids, whatever the field is called. That is how the original server
    /// kept an effect in step with the hit that caused it: attached with announce off, and named
    /// in the hit. The client plays a hit at the action's strike - when the shot is seen to
    /// land, which for a projectile is its flight after the recovery arrived
    /// (TargetedAction.SetupVariableStrike, GetDistanceDelay) - and an effect announced by its
    /// own attach was there before that: a creature burning, frozen or thrown ahead of the shot
    /// that did it.
    ///
    /// While one of these is open (<see cref="On"/>), GameEffectManager.Attach asks
    /// <see cref="Claim"/> about every effect it attaches. One the hit can announce is attached
    /// quietly and its type goes into the hit's list. That is a debuff going onto the hit's
    /// target from whoever dealt the hit, which would have announced itself and has not said
    /// it must (GameEffect.AnnounceWithHit).
    ///
    /// The hits that open one are those whose list the client plays through AnnounceDamage:
    ///  - a weapon's recovery and a constant-fire weapon's pulses and release (MissileManager,
    ///    ConstantFire);
    ///  - a damage ability's recovery: DamageBase.DoAbility, and the creature classes that take
    ///    a bare rawInfo (AbilityManager);
    ///  - a blast announced through its effect's AnnounceDamage or DoExplosion - Controlled
    ///    Fission, Self Destruct, a scatterbomb, Corpse Immolation, a Trap's strike - and a Crab
    ///    Mine's death recovery.
    ///
    /// The source has to be the hit's: a client that has not got the effect's source announces
    /// the effect at once whatever it was told (Recv_GameEffectAttached), and a client that has
    /// got it is one the hit goes to - a recovery is sent around the dealer, and to the squad
    /// mates who hold them from afar; a blast around where it goes off, which is beside the ones
    /// it hits. So nobody is left with an effect nothing will announce. A client that comes into
    /// view later is shown it announced (GameEffectManager.ShowEffectsTo).
    ///
    /// Creatures and players alike, but on a player not what holds them: a stun or a knockback
    /// (GameEffect.IsStun) and a root (IsRoot). Their announce is more than a picture - a stun's
    /// is what stops the player's own client moving them (StunEffect.OnAnnounceAttach), a web's
    /// or a net's what blocks it - and the server holds them from the attach, so the client is
    /// not left to catch up when the shot lands. A slow, a burn or a debuff waits for the hit.
    ///
    /// sourceEffectIds stay empty: nothing a hit does goes onto whoever dealt it. The client's
    /// effects that a hit sets off on its dealer are standing ones - the vampiric and damage
    /// procs - whose work is shown by their own AnnounceVamp or OnHit, not by an attach.
    ///
    /// <see cref="StrikeMs"/> is how long, at most, after the hit arrives the client plays it,
    /// for an effect that has to wait for its announce before it does anything more (the
    /// Electric crit's arcs, CritEffects.Arc).
    ///
    /// Opened and closed on the thread that resolves the hit, and nested ones are kept apart.
    /// </summary>
    public sealed class HitEffects : IDisposable
    {
        [ThreadStatic]
        private static HitEffects _open;

        private readonly HitEffects _outer;
        private readonly Actor _target;
        private readonly Actor _source;
        private readonly List<uint> _targetEffectIds;
        private readonly int _strikeMs;

        private HitEffects(Actor target, Actor source, List<uint> targetEffectIds, int strikeMs)
        {
            _target = target;
            _source = source;
            _targetEffectIds = targetEffectIds;
            _strikeMs = Math.Max(0, strikeMs);
            _outer = _open;
            _open = this;
        }

        /// <summary>
        /// Opens the hit on its target, to be closed when what the hit does to it has been done:
        /// "using (HitEffects.On(...))". targetEffectIds is the hit's own list; strikeMs how
        /// long after the hit arrives its client may play it (0 at once). Null - and nothing is
        /// opened - for anything but a creature or a player struck by someone, or with no list
        /// to name the effects in: those are announced by their attach, as they were.
        /// </summary>
        public static HitEffects On(Actor target, Actor source, List<uint> targetEffectIds, int strikeMs = 0)
        {
            if (!(target is Creature || target is Manifestation) || source == null || source.EntityId == 0 || targetEffectIds == null)
                return null;

            return new HitEffects(target, source, targetEffectIds, strikeMs);
        }

        /// <summary>
        /// The hit being resolved on <paramref name="actor"/>: how long after it arrives its
        /// client may play it, and so announce what it names. 0 with none open.
        /// </summary>
        internal static int StrikeMs(Actor actor)
        {
            var hit = _open;

            return hit != null && ReferenceEquals(hit._target, actor) ? hit._strikeMs : 0;
        }

        /// <summary>
        /// An effect is about to be told to the clients as attached to actor: whether the hit
        /// being resolved announces it. If so it has been made quiet and named in the hit.
        /// </summary>
        internal static bool Claim(Actor actor, GameEffect effect)
        {
            var hit = _open;

            if (hit == null || effect == null || !ReferenceEquals(hit._target, actor))
                return false;

            if (effect.IsBuff || !effect.AnnounceOnAttach || !effect.AnnounceWithHit
                || effect.ServerOnly || effect.OwnerOnly || effect.IsSkillPassive
                || effect.TypeId <= 0 || effect.SourceId != hit._source.EntityId)
                return false;

            // What holds a player is theirs at once.
            if (actor is Manifestation && (effect.IsStun || effect.IsRoot))
                return false;

            effect.AnnounceOnAttach = false;
            hit._targetEffectIds.Add((uint)effect.TypeId);

            return true;
        }

        public void Dispose()
        {
            if (ReferenceEquals(_open, this))
                _open = _outer;
        }
    }
}
