using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Buffs and debuffs on actors. An effect has a type (the client's gameeffectdata id, which
    /// picks the client class and visuals), an instance id, a level, and whatever the server
    /// needs to apply it: when it wears off, what it does on a tick, what it changes while it is
    /// on. See GameEffect for the fields; this is the machinery that runs them.
    ///
    /// Effects live on players and creatures alike. The map keeps the set of actors carrying
    /// any (MapChannel.ActorsWithEffects) and the worker walks that, not every creature.
    ///
    /// Timing is by Environment.TickCount64. The worker runs on a 500 ms timer but was handed
    /// the world-loop delta, so an effect aged by one loop tick every half second and a "5
    /// second" sprint ran for the better part of a minute.
    /// </summary>
    public class GameEffectManager
    {
        private static GameEffectManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>gameeffectdata.SPRINT.</summary>
        public const int SprintTypeId = 247;

        private readonly Random _random = new Random();

        public static GameEffectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new GameEffectManager();
                    }
                }

                return _instance;
            }
        }

        private GameEffectManager()
        {
        }

        #region Bookkeeping

        public void AddToList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects[gameEffect.EffectId] = gameEffect;
            gameEffect.Holder = actor;
            MapOf(actor)?.ActorsWithEffects.Add(actor);
        }

        // The map channel the actor is in: its own when it has one (a private instance), else
        // the map of its context.
        private static MapChannel MapOf(Actor actor)
            => actor.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(actor.MapContextId);

        public void RemoveFromList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Remove(gameEffect.EffectId);

            if (actor.ActiveEffects.Count == 0)
                MapOf(actor)?.ActorsWithEffects.Remove(actor);
        }

        /// <summary>Allocates the next effect id on the map. Effect ids are per map, like entity cells.</summary>
        public int NextEffectId(MapChannel mapChannel)
        {
            mapChannel.CurrentEffectId++;
            return mapChannel.CurrentEffectId;
        }

        /// <summary>The client of a player on this map, or null for a creature or a player who has gone.</summary>
        internal static Client ClientOf(MapChannel mapChannel, Actor actor)
        {
            if (!(actor is Manifestation player))
                return null;

            foreach (var client in mapChannel.ClientList)
                if (client?.Player == player)
                    return client;

            // Not on the list yet - a player still in the queue for the map they are loading into.
            return Server.Clients.Find(c => c?.Player == player);
        }

        #endregion

        #region Attach and detach

        /// <summary>
        /// Registers an effect on an actor and tells everyone who can see them. An effect of the
        /// same type already on the actor is replaced: a second Rage does not stack with the
        /// first, a fresh Ruin starts the clock over. attachArgs are passed to the client
        /// effect's OnAttach; the sprint effect wants its bead modifier there, most effects want
        /// nothing. The tooltip dictionary is the effect's own Tooltip plus the fixed keys.
        /// </summary>
        public void Attach(MapChannel mapChannel, Actor actor, GameEffect effect, params object[] attachArgs)
        {
            // Cure P4 keeps debuffs off whoever it was cast on for its duration - and says so:
            // the client floats "Immune" over them (COMBAT_IMMUNE_ANNOUNCED).
            // So does a creature running home after a leash (BehaviorManager.Leash): a slow would
            // keep it from getting there, a DoT would hurt what it is immune to.
            // Not the world's own (GameEffect.Environmental): no immunity to debuffs is one to lava.
            if (!effect.IsBuff && !effect.Environmental && (DebuffsBlocked(actor) || actor is Creature returning && BehaviorManager.IsReturning(returning)))
            {
                CellManager.Instance.CellCallMethod(mapChannel, actor,
                    new GameEffectAttachFailedPacket(effect.TypeId, GameEffectAttachFailedPacket.FailReason.Immune, effect.SourceId));
                return;
            }

            // A buff from a player under an enemy's Mind Control P4-P5 goes on nobody but them
            // (Pvp.MayNotAssist).
            if (effect.IsBuff && effect.Source is Manifestation helper && !ReferenceEquals(helper, actor) && Pvp.MayNotAssist(helper))
                return;

            // Nor does a buff from anyone outside their side go on a player in a duel, a squad
            // wargame or a team's match (Pvp.MayHelp).
            if (effect.IsBuff && effect.Source != null && !Pvp.MayHelp(effect.Source, actor))
                return;

            // Nor does a debuff from a player on one who holds PvP Safety, or from a player an
            // enemy's Traitor holds back from that side (Pvp.Shielded). Putting one on an enemy
            // player ends the attacker's own Safety (Pvp.Attack).
            if (!effect.IsBuff && effect.Source != null && Pvp.Attack(mapChannel, effect.Source, actor))
            {
                CellManager.Instance.CellCallMethod(mapChannel, actor,
                    new GameEffectAttachFailedPacket(effect.TypeId, GameEffectAttachFailedPacket.FailReason.Immune, effect.SourceId));
                return;
            }

            // A debuff from an enemy player (or their creature) lasts PVP_EFFECT_DURATION_MODIFIER
            // less (Pvp). Stuns and knockbacks are scaled where they are made, their flight and
            // getup being worked out from the time; a bomb's clock is its fuse, not a duration.
            if (!effect.IsBuff && !effect.IsStun && effect.OnTick == null && effect.ExpiresTick != long.MaxValue
                && Pvp.IsPvp(effect.Source, actor) && Pvp.AreEnemies(Pvp.Controller(effect.Source), (Manifestation)actor))
            {
                var now = Environment.TickCount64;
                var left = effect.ExpiresTick - now;

                if (left > 0)
                    effect.ExpiresTick = now + Pvp.ScaleDuration(effect.Source, actor, (int)Math.Min(int.MaxValue, left));
            }

            // A skill's standing effects are one per skill and share a type (two heat bonuses
            // are two SKILL_LIMITED_COOL_RATE_MODIFIER_EFFECTs); the rest replace their own kind.
            if (!effect.IsSkillPassive)
                foreach (var existing in actor.ActiveEffects.Values.Where(e => e.TypeId == effect.TypeId && !e.IsSkillPassive).ToList())
                    DettachEffect(mapChannel, actor, existing);

            AddToList(actor, effect);
            mapChannel.ActorsWithEffects.Add(actor);
            if (effect.TickDamageMax > 0 && actor is Creature attackedCreature)
                CreatureManager.RecordOwnerAttack(mapChannel, effect.Source, attackedCreature);

            // A player's attributes are worked out in one place, UpdateStatsValues, which reads
            // the effects; anything else would be undone by the next time it runs - putting on a
            // piece of armour, spending a point. A creature has no such place, and takes a
            // maximum-health or attribute change directly.
            if (actor is Manifestation && ChangesStats(effect))
                ManifestationManager.Instance.RefreshStats(actor as Manifestation);
            else
            {
                if (effect.MaxHealthPercent != 0)
                    ApplyMaxHealth(mapChannel, actor, effect);

                if (!(actor is Manifestation) && effect.AttributeId.HasValue && effect.AttributePercent != 0)
                    ApplyAttribute(mapChannel, actor, effect);
            }

            effect.AttachArgs = attachArgs.ToList();

            // A weapon hit being resolved on this creature announces what it puts on it: the
            // effect goes on quietly and the hit names it (HitEffects).
            HitEffects.Claim(actor, effect);

            var attached = AttachedPacket(effect, effect.AnnounceOnAttach);

            if (effect.ServerOnly)
            {
                // nobody is told
            }
            else if (effect.IsSkillPassive || effect.OwnerOnly)
                ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, attached);
            else
                CellManager.Instance.CellCallMethod(mapChannel, actor, attached);

            if (effect.MovementModifierPercent != 0)
                UpdateMovementMod(mapChannel, actor);

            if (effect.ChangesRegen)
                SyncRegen(mapChannel, actor);

            // Out of sight: after the attach has gone out, so that the clients about to lose the
            // entity have been told what is on it first, and their own copy of the player goes
            // with the cloak's own visuals rather than before them.
            if (effect.Hides && actor is Manifestation hidden)
                Detection.Hide(mapChannel, hidden);

            // Blinded: it drops what it was fighting, and the scan will pass over everything
            // until this wears off.
            if (effect.Blinds && actor is Creature blinded)
                BehaviorManager.Instance.StopFighting(blinded);
        }

        /// <summary>
        /// A client has just been given an entity that already carries effects - it walked into
        /// range, arrived on the map, or the entity came out of a cloak - and every attach went
        /// out before it was there: without this a squad mate's Rage, a DoT on a mob, a turret's
        /// look or a Target Painting were simply not there for anyone who turned up later. Sent
        /// straight after the entity is created: a polymorphed player's weapon entity first,
        /// since the morph's announce looks it up, then each effect in the order it went on, so
        /// an aura comes before its children.
        ///
        /// One Recv_GameEffectAttached per effect rather than Recv_GameEffects (279), which does
        /// the same attach for a whole list but announces every one of them unconditionally and
        /// has no guard against an effect the client already holds. Announced as the effect
        /// asks (AnnounceToNewcomers). A skill's standing effects only ever go to their own
        /// player, and an effect whose time has run out is left for the worker to take off. An
        /// effect whose clock is stopped is followed by its OnPaused, so the newcomer's tooltip
        /// says so too; its attach already carries the time it has left.
        /// </summary>
        public static void ShowEffectsTo(Client viewer, Actor actor)
        {
            if (viewer == null || actor == null)
                return;

            if (actor is Manifestation player)
                AbilityManager.ShowMorphWeaponTo(viewer, player);

            foreach (var packet in NewcomerPackets(actor, viewer.Player))
                viewer.CallMethod(actor.EntityId, packet);
        }

        /// <summary>What ShowEffectsTo sends of the actor's effects to a client whose player is viewer, in order: each attach, and the pause of a paused one straight after it.</summary>
        public static List<PythonPacket> NewcomerPackets(Actor actor, Actor viewer)
        {
            var packets = new List<PythonPacket>();

            foreach (var effect in actor.ActiveEffects.Values.OrderBy(e => e.EffectId))
            {
                if (effect.IsExpired)
                    continue;

                if (effect.ServerOnly || (effect.IsSkillPassive || effect.OwnerOnly) && viewer != actor)
                    continue;

                packets.Add(AttachedPacket(effect, effect.AnnounceToNewcomers));

                if (effect.IsPaused)
                    packets.Add(new GameEffectPausePacket(effect.EffectId, true));
            }

            return packets;
        }

        /// <summary>The attaches of <see cref="NewcomerPackets"/>.</summary>
        public static List<GameEffectAttachedPacket> EffectsForNewcomer(Actor actor, Actor viewer) =>
            NewcomerPackets(actor, viewer).OfType<GameEffectAttachedPacket>().ToList();

        /// <summary>
        /// The Recv_GameEffectAttached for an effect already on its holder: what Attach sent,
        /// with the time that is left - and announced or not, for a client meeting the holder
        /// after the effect went on.
        /// </summary>
        public static GameEffectAttachedPacket AttachedPacket(GameEffect effect, bool announced)
        {
            return new GameEffectAttachedPacket
            {
                EffectTypeId = effect.TypeId,
                EffectId = effect.EffectId,
                EffectLevel = effect.EffectLevel,
                SourceId = effect.SourceId,
                Announced = announced,
                Duration = effect.HasDuration && effect.ShowsDuration ? effect.RemainingSeconds : (int?)null,
                DamageType = effect.TickDamageMax > 0 ? (int)effect.TickDamageType : 0,
                AttrId = effect.TooltipAttrId,
                IsActive = true,
                IsBuff = effect.IsBuff,
                IsDebuff = !effect.IsBuff,
                IsNegativeEffect = !effect.IsBuff,
                Extras = effect.Tooltip,
                Args = effect.AttachArgs.ToList()
            };
        }

        /// <summary>
        /// Sprint, from its action_property row: EFFECT_MOVEMENT_MODIFIER is the speed as a
        /// percent (120 at level 1, 160 at 5), DURATION the cap in seconds, and
        /// DRAIN_PER_TICK_ADRENALINE over INTERVAL the adrenaline it burns. The client's tooltip
        /// shows that as drain / (interval * 10) and calls it "-1.5% every second"
        /// (abilities/sprint.py, uielement 2578): a percent of the adrenaline bar, so the same
        /// pump costs the same share of the bar at every level. This takes the same. It ends when
        /// the duration is up, the bar is empty, the player right-clicks the buff away (a detach
        /// request), or the player presses sprint again - AbilityManager turns a second request
        /// into ending the first. There is no cost to start or stop it; the drain is the cost.
        /// </summary>
        public GameEffect AttachSprint(MapChannel mapChannel, Actor actor, ActionLevelInfo level)
        {
            var durationSeconds = level.Get(AbilityProperty.Duration, 3600);
            var interval = Math.Max(1, level.Get(AbilityProperty.Interval, 2));
            var drainPerTick = level.Get(AbilityProperty.DrainPerTickAdrenaline, 0);
            var now = Environment.TickCount64;

            var effect = new GameEffect
            {
                TypeId = SprintTypeId,
                EffectId = NextEffectId(mapChannel),
                EffectLevel = level.Level,
                ActionId = level.ActionId,
                SourceId = actor.EntityId,
                Source = actor,
                ExpiresTick = now + durationSeconds * 1000L,
                TickIntervalMs = 1000,
                NextTickTick = now + 1000,
                MovementModifierPercent = level.Get(AbilityProperty.EffectMovementModifier, 100),
                AdrenalineDrainPercentPerSecond = drainPerTick / (interval * 10.0),
                AllowDetach = true
            };

            // EFFECT_FAST_MAXBEAD_MODIFIER, as a fraction: the client's SprintEffect.OnAttach(beadModifier).
            var beadModifier = level.Get(AbilityProperty.EffectFastMaxbeadModifier, 100) / 100.0;

            Attach(mapChannel, actor, effect, beadModifier);

            return effect;
        }

        /// <summary>
        /// Ends an effect and tells everyone who can see its holder. An aura takes its copies
        /// off the squad with it; a copy leaves its aura's list. Detaching an effect that is
        /// already gone does nothing, so callers need not check.
        /// </summary>
        public void DettachEffect(MapChannel mapChannel, Actor actor, GameEffect gameEffect)
        {
            if (!actor.ActiveEffects.ContainsKey(gameEffect.EffectId))
                return;

            foreach (var child in gameEffect.Children.ToList())
                if (child.Holder != null)
                    DettachEffect(mapChannel, child.Holder, child);

            gameEffect.Children.Clear();
            gameEffect.Parent?.Children.Remove(gameEffect);

            // inform clients (Recv_GameEffectDetached 75)
            // Told to whoever was told of it.
            if (gameEffect.ServerOnly)
            {
                // nobody was told of it
            }
            else if (gameEffect.IsSkillPassive || gameEffect.OwnerOnly)
                ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });
            else
                CellManager.Instance.CellCallMethod(mapChannel, actor, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });

            RemoveFromList(actor, gameEffect);

            if (actor.ActiveEffects.Count == 0)
                mapChannel.ActorsWithEffects.Remove(actor);

            if (gameEffect.MaxHealthApplied != 0)
                RevertMaxHealth(mapChannel, actor, gameEffect, true);
            else if (actor is Manifestation && ChangesStats(gameEffect))
                ManifestationManager.Instance.RefreshStats(actor as Manifestation);

            if (gameEffect.AttributeApplied != 0)
                RevertAttribute(mapChannel, actor, gameEffect, true);

            if (gameEffect.MovementModifierPercent != 0)
                UpdateMovementMod(mapChannel, actor);

            if (gameEffect.ChangesRegen)
                SyncRegen(mapChannel, actor);

            // Back in sight, unless something else is still hiding them.
            if (gameEffect.Hides && actor is Manifestation seen && !Detection.IsHidden(seen))
                Detection.Reveal(mapChannel, seen);

            gameEffect.OnDetached?.Invoke(mapChannel, actor, gameEffect);
        }

        /// <summary>
        /// Ends every effect on an actor without telling anyone about them: the actor is leaving
        /// the map, or a creature has died and its client-side entity goes with it. What is
        /// told is the rest of the squad, whose copies of any aura the actor carried are taken
        /// off properly - they are staying.
        ///
        /// The speed they gave goes with them. MovementSpeed is what the effects make it (see
        /// <see cref="UpdateMovementMod"/>), and the ActorInfo a player is sent on arriving at a
        /// map hands it to their client as its movement modifier. Clearing the effects but not
        /// the speed sent a player who crossed a zone line mid-sprint into the next map running at
        /// sprint speed with no sprint behind it: nothing drained adrenaline, nothing ran out, and
        /// no buff was there to turn off, until a later sprint ended and worked the speed out again.
        /// The same goes for a maximum health an effect raised: it is put back here.
        /// </summary>
        public void ClearEffects(MapChannel mapChannel, Actor actor)
        {
            var cleared = actor.ActiveEffects.Values.ToList();

            foreach (var effect in cleared)
            {
                foreach (var child in effect.Children.ToList())
                    if (child.Holder != null && mapChannel != null)
                        DettachEffect(mapChannel, child.Holder, child);

                effect.Children.Clear();
                effect.Parent?.Children.Remove(effect);

                if (effect.MaxHealthApplied != 0)
                    RevertMaxHealth(mapChannel, actor, effect, false);

                if (effect.AttributeApplied != 0)
                    RevertAttribute(mapChannel, actor, effect, false);
            }

            actor.ActiveEffects.Clear();
            actor.MovementSpeed = 1.0d;
            mapChannel?.ActorsWithEffects.Remove(actor);

            foreach (var effect in cleared)
                effect.OnDetached?.Invoke(mapChannel, actor, effect);
        }

        #endregion

        #region Pause and restart

        /// <summary>
        /// Stops an effect's clock: it neither runs out nor ticks until it is restarted, and
        /// whatever it changes while it is on stays changed. Told to whoever was told of the
        /// effect (OnPaused), whose tooltip then says "Paused" in place of its timer. An aura's
        /// copies on the squad stop with it - they share its end, and its tick is what keeps
        /// them - and no new ones go out while it is stopped. False when the effect is not on
        /// the actor or was already stopped.
        /// </summary>
        public bool Pause(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (actor == null || effect == null || !actor.ActiveEffects.ContainsKey(effect.EffectId))
                return false;

            if (!effect.Freeze(Environment.TickCount64))
                return false;

            if (mapChannel != null)
                TellHolder(mapChannel, actor, effect, new GameEffectPausePacket(effect.EffectId, true));

            foreach (var child in effect.Children.ToList())
                if (child.Holder != null)
                    Pause(mapChannel, child.Holder, child);

            return true;
        }

        /// <summary>
        /// Starts a stopped effect's clock again with the time it had left when it stopped, and
        /// its next tick as far off as it was. Told as OnRestart, and then - for an effect that
        /// runs out - a GameEffectUpdateTooltip carrying that time: the client's timer is set
        /// from a tooltip and from nothing else, and without one would run on from the moment
        /// the effect was attached as if the pause had never been. The aura's copies go with it.
        /// False when the effect is not on the actor or was not stopped.
        /// </summary>
        public bool Restart(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (actor == null || effect == null || !actor.ActiveEffects.ContainsKey(effect.EffectId))
                return false;

            if (!effect.Thaw(Environment.TickCount64))
                return false;

            if (mapChannel != null)
            {
                TellHolder(mapChannel, actor, effect, new GameEffectPausePacket(effect.EffectId, false));

                if (effect.HasDuration && effect.ShowsDuration)
                    UpdateTooltip(mapChannel, actor, effect);
            }

            foreach (var child in effect.Children.ToList())
                if (child.Holder != null)
                    Restart(mapChannel, child.Holder, child);

            return true;
        }

        /// <summary>
        /// Tells whoever was told of an effect what its tooltip says now - the time it has left and
        /// its own values - after either changed on the server (GameEffectUpdateTooltip, 660). The
        /// client replaces the whole dictionary, so it goes out whole, written as the attach writes
        /// it. What that reaches is the tooltip: the timer the icons on the buff bar, the target
        /// window and the party window draw is copied from the effect only when an effect on that
        /// actor comes or goes, and stays on the old time until then. False when the effect is
        /// not on the actor.
        /// </summary>
        public bool UpdateTooltip(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (mapChannel == null || actor == null || effect == null || !actor.ActiveEffects.ContainsKey(effect.EffectId))
                return false;

            TellHolder(mapChannel, actor, effect, new GameEffectUpdateTooltipPacket(AttachedPacket(effect, false)));

            return true;
        }

        /// <summary>Sends something about an effect to whoever its attach went to: nobody for a server-only one, its own player for a skill's, everyone who can see the holder otherwise.</summary>
        private static void TellHolder(MapChannel mapChannel, Actor actor, GameEffect effect, PythonPacket packet)
        {
            if (effect.ServerOnly)
                return;

            if (effect.IsSkillPassive)
                ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, packet);
            else
                CellManager.Instance.CellCallMethod(mapChannel, actor, packet);
        }

        #endregion

        #region Worker

        public void DoWork(MapChannel mapChannel, long passedTime)
        {
            if (mapChannel.ActorsWithEffects.Count == 0)
                return;

            // The set changes under a detach, so work from a copy.
            foreach (var actor in mapChannel.ActorsWithEffects.ToList())
            {
                if (actor.ActiveEffects.Count == 0)
                {
                    mapChannel.ActorsWithEffects.Remove(actor);
                    continue;
                }

                // In another channel of the same map (a private instance): that channel ticks it.
                if (actor.RuntimeMapChannel != null && !ReferenceEquals(actor.RuntimeMapChannel, mapChannel))
                {
                    mapChannel.ActorsWithEffects.Remove(actor);
                    continue;
                }

                // Dead, or not on this map any more: nothing to tick and nobody to tell.
                if (actor.State == CharacterState.Dead || actor.MapContextId != mapChannel.MapInfo.MapContextId)
                {
                    ClearEffects(mapChannel, actor);
                    continue;
                }

                foreach (var effect in actor.ActiveEffects.Values.ToList())
                {
                    // Taken off by an earlier effect's tick on this pass.
                    if (!actor.ActiveEffects.ContainsKey(effect.EffectId))
                        continue;

                    // A creature held in its Critical Death window keeps only the window and
                    // its animations: a Ruin ticking on something that takes no damage would float
                    // numbers over it for nothing. Taken off here, on the pass after the window
                    // opened, so the tick that brought it down is told before its effect goes.
                    if (actor.State == CharacterState.Dying && actor is Creature && !CritDeathManager.IsCritDeathType(effect.TypeId))
                    {
                        DettachEffect(mapChannel, actor, effect);
                        continue;
                    }

                    if (effect.IsExpired)
                    {
                        DettachEffect(mapChannel, actor, effect);
                        effect.OnExpired?.Invoke(mapChannel, actor, effect);
                        continue;
                    }

                    if (effect.TickDue)
                        Tick(mapChannel, actor, effect);
                }
            }
        }

        /// <summary>One scheduled tick of an effect: its drain, its aura, its healing, its damage - whichever it has.</summary>
        private void Tick(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            var now = Environment.TickCount64;

            effect.NextTickTick += effect.TickIntervalMs;

            // A worker that fell behind does not make up the missed ticks in a burst.
            if (effect.NextTickTick <= now)
                effect.NextTickTick = now + effect.TickIntervalMs;

            if (effect.OnTick != null)
            {
                effect.OnTick(mapChannel, actor, effect);
                return;
            }

            if (effect.AdrenalineDrainPercentPerSecond > 0)
            {
                if (!TickDrain(mapChannel, actor, effect))
                    return;
            }

            if (effect.AuraRadius > 0)
                TickAura(mapChannel, actor, effect);

            if (effect.TickHealMax > 0 || effect.TickAdrenaline > 0)
                TickHeal(mapChannel, actor, effect);

            // Last: it can kill the holder, and the holder's effects go with it.
            if (effect.TickDamageMax > 0)
                TickDamage(mapChannel, actor, effect);
        }

        /// <summary>The adrenaline drain of a sustained effect. False when the bar ran dry and the effect ended with it.</summary>
        private bool TickDrain(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (!actor.Attributes.TryGetValue(Attributes.Chi, out var chi))
                return true;

            var take = DrainThisTick(effect, chi.CurrentMax);

            if (take <= 0)
                return true;

            // "will end once all Adrenaline has been consumed": the last tick takes what is
            // left and the effect goes with it.
            var ended = chi.Current <= take;

            chi.Current = Math.Max(0, chi.Current - take);
            ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, new UpdateChiPacket(chi, 0));

            if (ended)
                DettachEffect(mapChannel, actor, effect);

            return !ended;
        }

        /// <summary>
        /// The whole points of adrenaline one tick of a drain takes, out of a bar of the given
        /// maximum: a percent of the bar per second, with the fraction carried on the effect so
        /// 1.5% of 100 takes 3 every two seconds - 1, then 2 - rather than rounding down to 1
        /// every second and running at two thirds of the advertised rate.
        /// </summary>
        public static int DrainThisTick(GameEffect effect, int currentMax)
        {
            var due = currentMax * effect.AdrenalineDrainPercentPerSecond / 100.0 * effect.TickIntervalMs / 1000.0 + effect.DrainCarry;

            // Twenty carries of 0.3 sum to 5.999..., not 6; the nudge keeps a whole point that
            // floating-point arithmetic has left a hair short from slipping a tick.
            var take = (int)Math.Floor(due + 1e-6);

            effect.DrainCarry = due - take;

            return take;
        }

        /// <summary>
        /// Healing (and adrenaline) to the holder. The heal goes through ActorManager.Heal with
        /// the source named, which keeps the holder's client quiet about the health change; the
        /// tick then carries the amount and the client's HealOverTime.OnTick floats it as
        /// healing from the source, which is what the original did.
        /// </summary>
        private void TickHeal(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            var healed = 0;

            if (effect.TickHealMax > 0)
            {
                var amount = AbilityManager.Scale(effect.SourceLevel, _random.Next(effect.TickHealMin, effect.TickHealMax + 1), effect.TickScaleType);
                healed = ActorManager.Instance.Heal(actor, amount, effect.SourceId);
            }

            if (effect.TickAdrenaline > 0)
            {
                var client = ClientOf(mapChannel, actor);

                if (client != null)
                    ManifestationManager.Instance.GainAdrenaline(client, effect.TickAdrenaline);
            }

            var tick = new GameEffectTickPacket(effect.EffectId, GameEffectTickPacket.TickKind.Heal);

            if (healed > 0)
                tick.Entries.Add(new TickEntry { EntityId = actor.EntityId, Amount = healed });

            CellManager.Instance.CellCallMethod(mapChannel, actor, tick);
        }

        /// <summary>
        /// Damage from a tick: to the holder (a damage-over-time), or to every hostile within
        /// TickRadius of the holder (Scourge). Rolled and scaled to the source's level like any
        /// ability damage, with the victim's resistance applied. A damage-over-time whose source
        /// has left the map ends: there is nobody to credit a kill to, and a creature brought to
        /// zero with no killer is left standing.
        /// </summary>
        private void TickDamage(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            var source = effect.Source;

            if (source == null || source.MapContextId != mapChannel.MapInfo.MapContextId)
            {
                DettachEffect(mapChannel, actor, effect);
                return;
            }

            var targets = new List<Actor>();

            if (effect.TickRadius > 0)
            {
                if (actor is Manifestation holder)
                    targets.AddRange(AbilityManager.VictimsWithin(mapChannel, holder, actor.Position, effect.TickRadius));
                else if (actor is Creature creatureHolder)
                    targets.AddRange(CreatureBombs.Caught(mapChannel, creatureHolder, actor.Position, effect.TickRadius));   // a Thrax's Scourge
            }
            else
                targets.Add(actor);

            var hits = new List<TickEntry>();

            foreach (var target in targets)
            {
                var rolled = AbilityManager.Scale(effect.SourceLevel, _random.Next(effect.TickDamageMin, effect.TickDamageMax + 1), effect.TickScaleType);
                var amount = ApplyResist(target, rolled, out var resisted, effect.TickDamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, target, amount, source, out var outcome, effect.TickDamageType, isPeriodic: true);

                hits.Add(new TickEntry
                {
                    EntityId = target.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = effect.TickDamageType,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                });
            }

            if (effect.TickRadius > 0 && !effect.TickRadiusAsTick)
            {
                // The holder is not the one hurt, so the effect announces the damage on their
                // behalf; the client's ScourgeEffect ticks on damage, so this is its tick too.
                if (hits.Count > 0)
                {
                    var announce = new GameEffectAnnounceDamagePacket(effect.EffectId);
                    announce.Hits.AddRange(hits);
                    CellManager.Instance.CellCallMethod(mapChannel, actor, announce);
                }
            }
            else
            {
                // Sent after the damage so a death blow is known - and to an entity the client
                // still has, dead or not; the server-side effect is already gone with a kill.
                var tick = new GameEffectTickPacket(effect.EffectId, GameEffectTickPacket.TickKind.Damage);
                tick.Entries.AddRange(hits);
                CellManager.Instance.CellCallMethod(mapChannel, actor, tick);
            }
        }

        /// <summary>
        /// An aura's tick: the squad within its radius carries a copy of it, and only they. A
        /// member who walked in gets one, a member who walked out (or left, or died) loses it. A
        /// member already carrying the copy type from someone else's aura is left to that aura,
        /// so two Soldiers' Rages do not fight over the squad every five seconds.
        /// </summary>
        private void TickAura(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            // A player's aura reaches their squad; a creature's (a Thrax boss's Rage), its own side.
            var members = actor is Manifestation holder
                ? AbilityManager.SquadWithin(mapChannel, holder, effect.AuraRadius).Where(m => m != actor).Cast<Actor>().ToList()
                : actor is Creature creatureHolder
                    ? CreatureBuffs.AlliesWithin(mapChannel, creatureHolder, creatureHolder.Position, effect.AuraRadius).Cast<Actor>().ToList()
                    : new List<Actor>();

            foreach (var child in effect.Children.ToList())
            {
                var carrier = child.Holder;

                if (carrier == null || !carrier.ActiveEffects.ContainsKey(child.EffectId))
                {
                    effect.Children.Remove(child);
                    continue;
                }

                if (!members.Contains(carrier))
                    DettachEffect(mapChannel, carrier, child);
            }

            var reached = new GameEffectTickPacket(effect.EffectId, GameEffectTickPacket.TickKind.EntityIds);

            foreach (var member in members)
            {
                if (member.ActiveEffects.Values.Any(e => e.TypeId == effect.AuraChildTypeId))
                    continue;

                var child = new GameEffect
                {
                    TypeId = effect.AuraChildTypeId,
                    EffectId = NextEffectId(mapChannel),
                    EffectLevel = effect.EffectLevel,
                    ActionId = effect.ActionId,
                    SourceId = effect.SourceId,
                    Source = effect.Source,
                    SourceLevel = effect.SourceLevel,
                    ExpiresTick = effect.ExpiresTick,
                    IsBuff = effect.IsBuff,
                    AnnounceOnAttach = !effect.AuraTickAnnounces,
                    AllowDetach = effect.AllowDetach,
                    DamageDealtPercent = effect.DamageDealtPercent,
                    ResistModifier = effect.ResistModifier,
                    RegenPercent = effect.RegenPercent,
                    ArmorRegenPercent = effect.ArmorRegenPercent,
                    HealthRegenPercent = effect.HealthRegenPercent,
                    AbsorbPercent = effect.AbsorbPercent,
                    AbsorbPool = effect.AbsorbPool,
                    PowerRegenPercent = effect.PowerRegenPercent,
                    MissPercent = effect.MissPercent,
                    Parent = effect
                };

                foreach (var (key, value) in effect.Tooltip)
                    child.Tooltip[key] = value;

                effect.Children.Add(child);
                Attach(mapChannel, member, child);
                reached.Entries.Add(new TickEntry { EntityId = member.EntityId });
            }

            if (effect.AuraTickAnnounces && reached.Entries.Count > 0)
                CellManager.Instance.CellCallMethod(mapChannel, actor, reached);
        }

        #endregion

        #region Modifiers

        /// <summary>
        /// shared/damageresistance.py GetDamageMultiplierFromResistValue: what a resistance value
        /// does to incoming damage. 50 halves it, 100 cuts it to a third; a negative value is a
        /// vulnerability, -50 is half again as much.
        /// </summary>
        public static double ResistMultiplier(int resist)
        {
            return resist < 0 ? (100.0 - resist) / 100.0 : 1.0 / ((100.0 + 2.0 * resist) / 100.0);
        }

        /// <param name="damageType">The hit's type; an effect limited to one type (Polarity Field) counts only against that type, and not against an untyped hit.</param>
        public static int ResistModifierOf(Actor actor, DamageType damageType = 0)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.ResistDamageType == 0 || effect.ResistDamageType == damageType)
                    total += effect.ResistModifier;

            return total;
        }

        /// <summary>How much of the actor's cover still counts, in percent: the least any effect on it allows (Target Painting), 100 with none.</summary>
        public static int CoverCountsPercentOf(Actor actor)
        {
            var percent = 100;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.CoverCountsPercent.HasValue)
                    percent = Math.Min(percent, effect.CoverCountsPercent.Value);

            return Math.Max(0, percent);
        }

        /// <summary>Percent of each hit on the actor that goes past its armour (Target Painting), at most 100.</summary>
        public static int ArmorPiercePercentOf(Actor actor)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                total += effect.ArmorPiercePercent;

            return Math.Max(0, Math.Min(100, total));
        }

        /// <summary>Whether an effect changes what UpdateStatsValues works out for a player.</summary>
        private static bool ChangesStats(GameEffect effect)
        {
            return effect.MaxHealthPercent != 0 || (effect.AttributeId.HasValue && effect.AttributePercent != 0) || effect.PrimaryAttributesPercent != 0;
        }

        /// <summary>
        /// The percent the effects on an actor add to an attribute: Bio Augmentation's
        /// AttributePercent for its attribute, and for Health also Reconstruction's
        /// MaxHealthPercent. Read by ManifestationManager.UpdateStatsValues.
        /// </summary>
        public static int AttributePercentOf(Actor actor, Attributes attribute)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
            {
                if (effect.AttributeId == attribute)
                    total += effect.AttributePercent;

                if (attribute == Attributes.Health)
                    total += effect.MaxHealthPercent;

                // Rez Trauma: Body, Mind and Spirit alike.
                if (attribute == Attributes.Body || attribute == Attributes.Mind || attribute == Attributes.Spirit)
                    total += effect.PrimaryAttributesPercent;
            }

            return total;
        }

        /// <summary>
        /// Shields: what of an incoming hit the effects on the victim take instead of them. The
        /// first effect with something left in its pool takes AbsorbPercent of the hit, up to
        /// what the pool holds; a pool emptied ends its shield - the aura, and with it every copy
        /// it put on the squad. Returns what is left of the hit; absorbed is what the shield took.
        /// </summary>
        public int ApplyAbsorb(MapChannel mapChannel, Actor victim, int amount, out int absorbed)
        {
            absorbed = 0;

            if (victim == null || amount <= 0)
                return amount;

            foreach (var effect in victim.ActiveEffects.Values.ToList())
            {
                var pool = effect.AbsorbPool;

                if (pool == null || pool.Remaining <= 0 || effect.AbsorbPercent <= 0)
                    continue;

                absorbed = Math.Min(pool.Remaining, (int)Math.Round(amount * Math.Min(100, effect.AbsorbPercent) / 100.0));
                pool.Remaining -= absorbed;

                if (pool.Remaining <= 0)
                {
                    var shield = effect.Parent ?? effect;

                    if (shield.Holder != null)
                        DettachEffect(mapChannel, shield.Holder, shield);
                }

                break;
            }

            return amount - absorbed;
        }

        /// <summary>Percent chance, summed over the effects on an actor, that a stun or knockback is resisted (Graviton Armor).</summary>
        public static int KnockbackStunResistOf(Actor actor)
        {
            if (actor == null)
                return 0;

            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                total += effect.KnockbackStunResistPercent;

            return Math.Max(0, total);
        }

        /// <summary>Percent of damage taken that the effects on an actor reflect back at the attacker (Reflective Armor).</summary>
        /// <param name="damageType">The hit's type; an effect that reflects only some types (Reflection) answers its own alone.</param>
        public static int ReflectPercentOf(Actor actor, DamageType damageType = 0)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.ReflectTypes.Count == 0 || effect.ReflectTypes.Contains(damageType))
                    total += effect.ReflectPercent;

            return total;
        }

        /// <summary>Whether a debuff guard on the actor is keeping debuffs off them (Cure P4).</summary>
        public static bool DebuffsBlocked(Actor actor)
        {
            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.BlocksDebuffs)
                    return true;

            return false;
        }

        /// <summary>Whether an EMP crit is suppressing the actor's armour.</summary>
        public static bool ArmorSuppressed(Actor actor)
        {
            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.SuppressesArmor && !effect.IsExpired)
                    return true;

            return false;
        }

        /// <summary>The actor's ranged damage after the effects on it (Laser crit): amount x (100 + sum of RangedDamagePercent) / 100, never below 0.</summary>
        public static int ApplyRangedDamage(Actor actor, int amount)
        {
            var percent = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                percent += effect.RangedDamagePercent;

            return percent == 0 ? amount : Math.Max(0, amount * (100 + percent) / 100);
        }

        /// <summary>
        /// What the actor's action cooldowns are multiplied by, the effects on it multiplied
        /// together (Called Shot: Arm); 1 when nothing slows it.
        /// </summary>
        public static double AttackRateModifierOf(Actor actor)
        {
            var modifier = 1.0;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.AttackRateModifier > 0)
                    modifier *= effect.AttackRateModifier;

            return modifier;
        }

        /// <summary>Ranged damage landing on the actor after the smoke screens on it: amount x (100 - the sum) / 100, never below 0.</summary>
        public static int ApplyIncomingRanged(Actor actor, int amount)
        {
            var percent = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                percent += effect.IncomingRangedPercent;

            if (percent <= 0 || amount <= 0)
                return amount;

            return Math.Max(0, amount * Math.Max(0, 100 - percent) / 100);
        }

        /// <summary>The share of shots at the actor that miss it for the effects on it (Chaff): the strongest, 0 to 100.</summary>
        public static int MissPercentOf(Actor actor)
        {
            var percent = 0;

            if (actor == null)
                return 0;

            foreach (var effect in actor.ActiveEffects.Values)
                if (!effect.IsExpired && effect.MissPercent > percent)
                    percent = effect.MissPercent;

            return Math.Min(100, percent);
        }

        /// <summary>Percent added to the actor's chance of a critical hit by the effects on them (Crit Wave).</summary>
        public static int CritChancePercentOf(Actor actor)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                total += effect.CritChancePercent;

            return total;
        }

        public static int DamageDealtPercentOf(Actor actor)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                total += effect.DamageDealtPercent;

            return total;
        }

        /// <summary>
        /// Damage an attacker deals, after the effects on them that raise or lower it (Rage,
        /// Sacrifice) and any other percent the caller brings - a weapon skill's bonus. The
        /// percents add: a Rage of +30 under a +40 weapon skill is +70, not 1.3 x 1.4.
        /// </summary>
        public static int ApplyDamageDealt(Actor source, int amount, int extraPercent = 0)
        {
            if (source == null || amount <= 0)
                return amount;

            var percent = DamageDealtPercentOf(source) + extraPercent;

            return percent == 0 ? amount : Math.Max(0, (int)Math.Round(amount * (100 + percent) / 100.0));
        }

        /// <summary>
        /// Damage a victim takes, after the resistance the effects on them add up to (Rage,
        /// Resistance, Sacrifice, Base Wave) and, for damage of a known type, a player's own
        /// resistance to that type (armour and Hazmat Armor, Manifestation.ResistanceData);
        /// resisted is what came off. A creature's attack carries the type of the weapon it plays
        /// (Managers.CreatureAttacks), so a player's own resistances meet it as well.
        /// </summary>
        public static int ApplyResist(Actor target, int amount, out int resisted, DamageType damageType = 0)
        {
            resisted = 0;

            if (target == null || amount <= 0)
                return amount;

            var resist = ResistModifierOf(target, damageType);

            if (damageType != 0 && target is Manifestation player)
                foreach (var own in player.ResistanceData)
                    if (own.ResistanceType == damageType)
                        resist += own.ResistanceAmmount;

            if (resist == 0)
                return amount;

            var taken = (int)Math.Round(amount * ResistMultiplier(resist));
            resisted = amount - taken;

            return taken;
        }

        /// <summary>
        /// What an attribute regenerates per period with the effects on its owner: health and
        /// power take RegenPercent (Regeneration Wave) and their own HealthRegenPercent (Bio
        /// Armor) or PowerRegenPercent (Mech Armor), armour takes ArmorRegenPercent (Base Wave,
        /// Graviton Armor); 400 is five times the base.
        /// </summary>
        public static int RegenAmount(Actor actor, ActorAttributes attribute)
        {
            var percent = 0;

            foreach (var effect in actor.ActiveEffects.Values)
            {
                switch (attribute.AttributeId)
                {
                    case Attributes.Health:
                        percent += effect.RegenPercent + effect.HealthRegenPercent;
                        break;
                    case Attributes.Power:
                        percent += effect.RegenPercent + effect.PowerRegenPercent;
                        break;
                    case Attributes.Armor:
                        percent += effect.ArmorRegenPercent;
                        break;
                }
            }

            return percent == 0 ? attribute.RefreshAmount : Math.Max(0, (int)Math.Round(attribute.RefreshAmount * (100 + percent) / 100.0));
        }

        /// <summary>
        /// Tells a player's client the regeneration rates the effects make. The client predicts
        /// its bars from the RefreshAmount it was last given, so a rate the server alone knew
        /// would show as a bar that jumps on every real update; the attribute itself keeps the
        /// base rate, since UpdateStatsValues rebuilds it from the stats.
        /// </summary>
        public static void SyncRegen(MapChannel mapChannel, Actor actor)
        {
            // A creature's bars are predicted by everyone who sees it from the rates it was last
            // given; Disease P4 stopping its regeneration has to reach them too.
            if (actor is Creature)
            {
                if (actor.Attributes.TryGetValue(Attributes.Health, out var creatureHealth))
                    CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(WithRegen(actor, creatureHealth), actor.EntityId));

                // Its armour's too: Target Painting stops it regenerating (CreatureArmor).
                if (actor.Attributes.TryGetValue(Attributes.Armor, out var creatureArmor))
                    CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(WithRegen(actor, creatureArmor), actor.EntityId));

                return;
            }

            var client = ClientOf(mapChannel, actor);

            if (client == null)
                return;

            if (actor.Attributes.TryGetValue(Attributes.Health, out var health))
                CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(WithRegen(actor, health), 0));

            if (actor.Attributes.TryGetValue(Attributes.Armor, out var armor))
                CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(WithRegen(actor, armor), 0));

            if (actor.Attributes.TryGetValue(Attributes.Power, out var power))
                client.CallMethod(actor.EntityId, new UpdatePowerPacket(WithRegen(actor, power), 0));
        }

        /// <summary>A copy of the attribute carrying the regeneration rate the effects on the actor make, for sending.</summary>
        public static ActorAttributes WithRegen(Actor actor, ActorAttributes attribute)
        {
            return new ActorAttributes(attribute.AttributeId, attribute.NormalMax, attribute.CurrentMax, attribute.Current, RegenAmount(actor, attribute), attribute.RefreshPeriod);
        }

        /// <summary>
        /// MaxHealthPercent, applied: the maximum moves by that share of what it was, and so
        /// does the current health, so a full bar stays full and a lowered maximum does not
        /// leave health above it. The points moved are kept on the effect and are exactly what
        /// <see cref="RevertMaxHealth"/> puts back.
        /// </summary>
        private static void ApplyMaxHealth(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (!actor.Attributes.TryGetValue(Attributes.Health, out var health) || health.CurrentMax <= 0)
                return;

            var delta = (int)Math.Round(health.CurrentMax * effect.MaxHealthPercent / 100.0);

            // A maximum has to stay a maximum: at least one point.
            delta = Math.Max(delta, 1 - health.CurrentMax);

            health.CurrentMax += delta;

            // A raised maximum raises the health with it, so a full bar stays full; a lowered
            // one only caps - taking the points off would be damage, and damage has a path of
            // its own with a death at the end of it.
            if (delta > 0 && health.Current > 0)
                health.Current += delta;

            health.Current = Math.Min(health.CurrentMax, health.Current);
            effect.MaxHealthApplied = delta;

            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(health, 0));
        }

        /// <summary>
        /// AttributePercent on an actor that is not a player (a creature under Disease): the
        /// attribute's maximum moves by that share, at least 1 left; the current value is capped
        /// to it, or raised with it for a raise - the same rules as ApplyMaxHealth. The points
        /// moved are kept on the effect and are exactly what RevertAttribute puts back. A health
        /// change is told to the clients; the others are the server's own figures.
        /// </summary>
        private static void ApplyAttribute(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (!actor.Attributes.TryGetValue(effect.AttributeId.Value, out var attribute) || attribute.CurrentMax <= 0)
                return;

            var delta = (int)Math.Round(attribute.CurrentMax * effect.AttributePercent / 100.0);

            delta = Math.Max(delta, 1 - attribute.CurrentMax);

            attribute.CurrentMax += delta;

            if (delta > 0 && attribute.Current > 0)
                attribute.Current += delta;

            attribute.Current = Math.Min(attribute.CurrentMax, attribute.Current);
            effect.AttributeApplied = delta;

            if (attribute.AttributeId == Attributes.Health)
                CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(attribute, actor.EntityId));
        }

        private static void RevertAttribute(MapChannel mapChannel, Actor actor, GameEffect effect, bool announce)
        {
            var delta = effect.AttributeApplied;
            effect.AttributeApplied = 0;

            if (!effect.AttributeId.HasValue || !actor.Attributes.TryGetValue(effect.AttributeId.Value, out var attribute))
                return;

            attribute.CurrentMax = Math.Max(1, attribute.CurrentMax - delta);

            // A cut coming off gives the maximum back, and the value as far as it was cut.
            if (delta < 0 && attribute.AttributeId != Attributes.Health)
                attribute.Current -= delta;

            attribute.Current = Math.Min(attribute.CurrentMax, attribute.Current);

            if (announce && mapChannel != null && attribute.AttributeId == Attributes.Health)
                CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(attribute, actor.EntityId));
        }

        /// <summary>
        /// Whether something on the actor stops it being healed (Disease P5, "All Healing:
        /// Disabled"). ActorManager.Heal asks; so should anything else that heals, creatures
        /// included.
        /// </summary>
        public static bool HealingBlocked(Actor actor)
        {
            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.BlocksHealing && !effect.IsExpired)
                    return true;

            return false;
        }

        private static void RevertMaxHealth(MapChannel mapChannel, Actor actor, GameEffect effect, bool announce)
        {
            if (!actor.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            var delta = effect.MaxHealthApplied;
            effect.MaxHealthApplied = 0;

            health.CurrentMax = Math.Max(1, health.CurrentMax - delta);

            // The maximum goes back; the health stays where it is, capped - so a raise coming
            // off never costs more than what it gave, and a cut coming off gives nothing back.
            health.Current = Math.Min(health.CurrentMax, health.Current);

            if (announce && mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(health, 0));
        }

        /// <summary>
        /// Movement speed from the effects on an actor, as the client's Recv_MovementModChange
        /// wants it: 1.0 is normal. Modifiers multiply, so two 120s make 1.44.
        /// </summary>
        public void UpdateMovementMod(MapChannel mapChannel, Actor actor)
        {
            var movementMod = 1.0d;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.MovementModifierPercent > 0)
                    movementMod *= effect.MovementModifierPercent / 100.0;

            // ActorInfo carries MovementSpeed later - to whoever a creature comes into view for, and
            // to a player's own client each time they arrive on a map - so it has to say the same
            // thing as the change everyone present is told about now.
            actor.MovementSpeed = movementMod;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new MovementModChangePacket(movementMod));
        }

        #endregion
    }
}
