using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The effects a weapon hit puts on the creature it lands on, announced by the hit.
    ///
    /// A hit's rawInfo ends in targetEffectIds and sourceEffectIds (shared/damageinfo.py), and
    /// the client's Actor.AnnounceDamage, playing the hit, calls AnnounceGameEffectAttach for
    /// each: it announces - starts the FX of, and posts the status icon of - the first effect
    /// of that type on the entity that has not been announced yet. They are effect type ids,
    /// whatever the field is called. That is how the original server kept an effect in step
    /// with the hit that caused it: attached with announce off, and named in the hit. The
    /// client plays a hit at the action's strike - when the shot is seen to land, which for a
    /// projectile is its flight after the recovery arrived (TargetedAction.SetupVariableStrike)
    /// - and an effect announced by its own attach was there before that: a creature burning,
    /// frozen or thrown ahead of the shot that did it.
    ///
    /// While one of these is open (<see cref="On"/>), GameEffectManager.Attach asks
    /// <see cref="Claim"/> about every effect it attaches. One the hit can announce is attached
    /// quietly and its type goes into the hit's list. That is a debuff going onto the hit's
    /// creature from whoever dealt the hit, which would have announced itself and has not said
    /// it must (GameEffect.AnnounceWithHit).
    ///
    /// The source has to be the hit's: a client that has not got the effect's source announces
    /// the effect at once whatever it was told (Recv_GameEffectAttached), and a client that has
    /// got it is one the hit's recovery goes to - the recovery is sent around the dealer, and to
    /// the squad mates who hold them from afar. So nobody is left with an effect nothing will
    /// announce. A client that comes into view later is shown it announced
    /// (GameEffectManager.ShowEffectsTo).
    ///
    /// Only creatures: on a player an announce is more than a picture - a stun's is what stops
    /// their own client moving them (StunEffect.OnAnnounceAttach) - and it is not left to a
    /// second packet arriving. And only a hit whose list is sent in a packet the client plays
    /// through AnnounceDamage: a weapon's recovery and a constant-fire weapon's pulses.
    /// Nothing a hit does goes onto whoever dealt it, so sourceEffectIds stay empty.
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

        private HitEffects(Actor target, Actor source, List<uint> targetEffectIds)
        {
            _target = target;
            _source = source;
            _targetEffectIds = targetEffectIds;
            _outer = _open;
            _open = this;
        }

        /// <summary>
        /// Opens the hit on its target, to be closed when what the hit does to it has been done:
        /// "using (HitEffects.On(...))". targetEffectIds is the hit's own list. Null - and
        /// nothing is opened - for anything but a creature struck by someone, or with no list
        /// to name the effects in: those are announced by their attach, as they were.
        /// </summary>
        public static HitEffects On(Actor target, Actor source, List<uint> targetEffectIds)
        {
            if (!(target is Creature) || source == null || source.EntityId == 0 || targetEffectIds == null)
                return null;

            return new HitEffects(target, source, targetEffectIds);
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
