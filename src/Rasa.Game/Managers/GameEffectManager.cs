using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
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
            MapChannelManager.Instance.FindByContextId(actor.MapContextId)?.ActorsWithEffects.Add(actor);
        }

        public void RemoveFromList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Remove(gameEffect.EffectId);

            if (actor.ActiveEffects.Count == 0)
                MapChannelManager.Instance.FindByContextId(actor.MapContextId)?.ActorsWithEffects.Remove(actor);
        }

        /// <summary>Allocates the next effect id on the map. Effect ids are per map, like entity cells.</summary>
        public int NextEffectId(MapChannel mapChannel)
        {
            mapChannel.CurrentEffectId++;
            return mapChannel.CurrentEffectId;
        }

        /// <summary>The client of a player on this map, or null for a creature or a player who has gone.</summary>
        private static Client ClientOf(MapChannel mapChannel, Actor actor)
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
            // A skill's standing effects are one per skill and share a type (two heat bonuses
            // are two SKILL_LIMITED_COOL_RATE_MODIFIER_EFFECTs); the rest replace their own kind.
            if (!effect.IsSkillPassive)
                foreach (var existing in actor.ActiveEffects.Values.Where(e => e.TypeId == effect.TypeId && !e.IsSkillPassive).ToList())
                    DettachEffect(mapChannel, actor, existing);

            AddToList(actor, effect);
            mapChannel.ActorsWithEffects.Add(actor);
            if (effect.TickDamageMax > 0 && actor is Creature attackedCreature)
                CreatureManager.RecordOwnerAttack(mapChannel, effect.Source, attackedCreature);

            if (effect.MaxHealthPercent != 0)
                ApplyMaxHealth(mapChannel, actor, effect);

            var attached = new GameEffectAttachedPacket
            {
                EffectTypeId = effect.TypeId,
                EffectId = effect.EffectId,
                EffectLevel = effect.EffectLevel,
                SourceId = effect.SourceId,
                Announced = effect.AnnounceOnAttach,
                Duration = effect.HasDuration ? effect.RemainingSeconds : (int?)null,
                DamageType = effect.TickDamageMax > 0 ? (int)effect.TickDamageType : 0,
                AttrId = 1,
                IsActive = true,
                IsBuff = effect.IsBuff,
                IsDebuff = !effect.IsBuff,
                IsNegativeEffect = !effect.IsBuff,
                Extras = effect.Tooltip,
                Args = attachArgs.ToList()
            };

            if (effect.IsSkillPassive)
                ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, attached);
            else
                CellManager.Instance.CellCallMethod(mapChannel, actor, attached);

            if (effect.MovementModifierPercent != 0)
                UpdateMovementMod(mapChannel, actor);

            if (effect.RegenPercent != 0 || effect.ArmorRegenPercent != 0)
                SyncRegen(mapChannel, actor);
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
            if (gameEffect.IsSkillPassive)
                ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });
            else
                CellManager.Instance.CellCallMethod(mapChannel, actor, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });

            RemoveFromList(actor, gameEffect);

            if (actor.ActiveEffects.Count == 0)
                mapChannel.ActorsWithEffects.Remove(actor);

            if (gameEffect.MaxHealthApplied != 0)
                RevertMaxHealth(mapChannel, actor, gameEffect, true);

            if (gameEffect.MovementModifierPercent != 0)
                UpdateMovementMod(mapChannel, actor);

            if (gameEffect.RegenPercent != 0 || gameEffect.ArmorRegenPercent != 0)
                SyncRegen(mapChannel, actor);
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
            foreach (var effect in actor.ActiveEffects.Values.ToList())
            {
                foreach (var child in effect.Children.ToList())
                    if (child.Holder != null && mapChannel != null)
                        DettachEffect(mapChannel, child.Holder, child);

                effect.Children.Clear();
                effect.Parent?.Children.Remove(effect);

                if (effect.MaxHealthApplied != 0)
                    RevertMaxHealth(mapChannel, actor, effect, false);
            }

            actor.ActiveEffects.Clear();
            actor.MovementSpeed = 1.0d;
            mapChannel?.ActorsWithEffects.Remove(actor);
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

                    if (effect.IsExpired)
                    {
                        DettachEffect(mapChannel, actor, effect);
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
                    targets.AddRange(AbilityManager.HostilesWithin(mapChannel, holder, actor.Position, effect.TickRadius));
            }
            else
                targets.Add(actor);

            var hits = new List<TickEntry>();

            foreach (var target in targets)
            {
                var rolled = AbilityManager.Scale(effect.SourceLevel, _random.Next(effect.TickDamageMin, effect.TickDamageMax + 1), effect.TickScaleType);
                var amount = ApplyResist(target, rolled, out var resisted);
                var taken = ActorManager.Instance.Damage(mapChannel, target, amount, source, isPeriodic: true);

                hits.Add(new TickEntry
                {
                    EntityId = target.EntityId,
                    Amount = amount,
                    Resisted = resisted,
                    DamageType = effect.TickDamageType,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                });
            }

            if (effect.TickRadius > 0)
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
            var members = actor is Manifestation holder
                ? AbilityManager.SquadWithin(mapChannel, holder, effect.AuraRadius).Where(m => m != actor).ToList()
                : new List<Manifestation>();

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

        public static int ResistModifierOf(Actor actor)
        {
            var total = 0;

            foreach (var effect in actor.ActiveEffects.Values)
                total += effect.ResistModifier;

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
        /// Resistance, Sacrifice, Base Wave); resisted is what came off. The armour's own
        /// resistance list is not in this yet - incoming damage does not carry a type.
        /// </summary>
        public static int ApplyResist(Actor target, int amount, out int resisted)
        {
            resisted = 0;

            if (target == null || amount <= 0)
                return amount;

            var resist = ResistModifierOf(target);

            if (resist == 0)
                return amount;

            var taken = (int)Math.Round(amount * ResistMultiplier(resist));
            resisted = amount - taken;

            return taken;
        }

        /// <summary>
        /// What an attribute regenerates per period with the effects on its owner: health and
        /// power take RegenPercent (Regeneration Wave), armour takes ArmorRegenPercent (Base
        /// Wave); 400 is five times the base.
        /// </summary>
        public static int RegenAmount(Actor actor, ActorAttributes attribute)
        {
            var percent = 0;

            foreach (var effect in actor.ActiveEffects.Values)
            {
                switch (attribute.AttributeId)
                {
                    case Attributes.Health:
                    case Attributes.Power:
                        percent += effect.RegenPercent;
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
        private static void SyncRegen(MapChannel mapChannel, Actor actor)
        {
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

        private static ActorAttributes WithRegen(Actor actor, ActorAttributes attribute)
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
