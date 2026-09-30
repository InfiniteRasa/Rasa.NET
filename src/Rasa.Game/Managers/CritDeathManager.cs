using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Critical Death: a creature a player brings to near death is held on its feet for a few
    /// seconds, a red skull over it, and a melee press next to it finishes it for bonus
    /// experience. Left alone, it dies as it would have.
    ///
    /// The client side (gameeffects/deatheffects.py, actions/critdeathfinish.py) runs on three
    /// effects, each at a level that is the damage type the creature was brought down with -
    /// critdeathdata keys its animations by damage type:
    ///
    ///  - CRIT_PREDEATH_EFFECT 116, announced on attach: the creature goes to its dying state and
    ///    stagger animation, stops being targetable, and shows the skull to whoever the effect's
    ///    source is and their squad (Actor.IsOverkillable). Detached with no CRIT_DEATH_EFFECT on
    ///    the creature, the client plays an ordinary death.
    ///  - CRIT_DEATH_EFFECT 117, attached quietly; the finisher's recovery announces it
    ///    (CritDeathFinishAction.DoHits), which plays the finishing animation. Detached, the
    ///    client plays the death without its death FX and announces
    ///  - CRIT_POSTDEATH_EFFECT 193, also attached quietly beforehand, the corpse pose.
    ///
    /// The press itself is RequestCritDeathFinish(actionId, argId, targetId) with
    /// CRITICAL_DEATH_FINISHER 10000002 (windup 299 ms, range 3 m).
    ///
    /// The reward, from the strategy guide: "You get full experience for killing the enemy, and
    /// you get full experience again at the end of the Finishing Move. This means you get double
    /// the experience and Adrenaline for the kill." CreatureManager.HandleCreatureKill pays the
    /// kill, then the same experience again with the XPInfo flag wasCritKill (or
    /// wasTeamCritKill for a squad mate's setup) set, which the client words as "You gained
    /// %(xp)s experience points by Crit Killing"; the adrenaline comes doubled. "Another
    /// interesting point about Finishing Moves is that they destroy the enemy's body": a
    /// finished creature is marked (Creature.CritKilled) and neither self revives nor can be
    /// revived (CreatureSupport).
    ///
    /// The help text: "when an enemy is stunned and near death, a red skull icon may appear".
    /// The window opens when a creature a player is fighting is both stunned (Stuns) and alive at
    /// HealthThresholdPercent of its health or less - checked when the player's damage lands
    /// and when a stun is put on it, so either may come first - unless its class carries
    /// NO_CRIT_KILL. Not in the client, and chosen here: the threshold. The window length
    /// (6900 ms) and each death animation's length are critdeathdata's.
    /// </summary>
    public class CritDeathManager
    {
        private static CritDeathManager _instance;
        private static readonly object InstanceLock = new object();

        public static CritDeathManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (InstanceLock)
                {
                    if (_instance == null)
                        _instance = new CritDeathManager();
                }

                return _instance;
            }
        }

        private CritDeathManager()
        {
        }

        public const int PreDeathTypeId = 116;      // CRIT_PREDEATH_EFFECT
        public const int DeathTypeId = 117;         // CRIT_DEATH_EFFECT
        public const int PostDeathTypeId = 193;     // CRIT_POSTDEATH_EFFECT

        /// <summary>Percent of its maximum health at or below which a creature a player hits enters its Critical Death window.</summary>
        public const int HealthThresholdPercent = 8;

        /// <summary>critdeathdata[1]: how long the window stays open, the same for every damage type.</summary>
        public const int WindowMs = 6900;

        /// <summary>CRITICAL_DEATH_FINISHER's own numbers, for when the action table does not have it.</summary>
        private const int DefaultWindupMs = 299;
        private const float DefaultRange = 3f;

        /// <summary>Metres past the finisher's range the creature may be: the client measures before it asks, and the creature or the player may have moved.</summary>
        private const float RangeSlack = 2f;

        /// <summary>critdeathdata[3], by damage type: how long the finishing animation plays before the creature is dead.</summary>
        private static readonly Dictionary<int, int> DeathAnimationMs = new Dictionary<int, int>
        {
            [1] = 6000, [2] = 3000, [3] = 3000, [4] = 3000, [5] = 3000, [6] = 6000, [7] = 6000, [13] = 3000
        };

        /// <summary>The effect level for a damage type: the type itself where critdeathdata has animations for it, physical otherwise.</summary>
        public static uint LevelFor(DamageType damageType)
        {
            return DeathAnimationMs.ContainsKey((int)damageType) ? (uint)damageType : (uint)DamageType.Physical;
        }

        public static int DeathAnimationFor(uint level) => DeathAnimationMs.TryGetValue((int)level, out var ms) ? ms : 3000;

        /// <summary>Alive, and at or below the threshold.</summary>
        public static bool IsNearDeath(int current, int max)
        {
            return current > 0 && max > 0 && current * 100L <= (long)max * HealthThresholdPercent;
        }

        /// <summary>Whether a player may finish a window someone opened: their own, or a squad mate's.</summary>
        public static bool MayFinish(Manifestation player, Actor opener)
        {
            if (opener == player)
                return true;

            return opener is Manifestation other && player.PartyId != 0 && other.PartyId == player.PartyId;
        }

        /// <summary>One of the three effects Critical Death runs on; everything else on a dying creature is taken off (GameEffectManager.DoWork).</summary>
        public static bool IsCritDeathType(int typeId) => typeId == PreDeathTypeId || typeId == DeathTypeId || typeId == PostDeathTypeId;

        public static GameEffect PreDeathOf(Actor actor)
        {
            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.TypeId == PreDeathTypeId)
                    return effect;

            return null;
        }

        /// <summary>
        /// After a player's damage has landed on a creature that is still alive, or a stun has been
        /// put on it: if it is now stunned and near death, holds it there and opens the window. Returns whether it did - the creature is then
        /// out of the fight and takes no more damage.
        /// </summary>
        public bool TryEnterPreDeath(MapChannel mapChannel, Creature creature, Actor source, DamageType damageType)
        {
            if (!(source is Manifestation player) || creature == null)
                return false;

            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return false;

            // NO_CRIT_KILL: a creature its class says is never finished just dies. The client does
            // not read the flag - it only draws the skull the window's effect tells it to - so the
            // server is the only place it can take effect.
            if (CreatureManager.CreatureFlagsOf(creature).Contains((int)CreatureFlag.NoCritKill))
                return false;

            if (!creature.Attributes.TryGetValue(Attributes.Health, out var health) || !IsNearDeath(health.Current, health.CurrentMax))
                return false;

            if (!Stuns.IsStunned(creature))
                return false;

            creature.State = CharacterState.Dying;

            // Stopped where it is, mid-knockback or mid-stride: nothing moves it from here.
            creature.KnockbackTo = null;
            BehaviorManager.Instance.StopMoving(creature);

            // Nothing comes back while it is held: no health, no armour.
            health.RefreshAmount = 0;
            health.RefreshPeriod = 0;

            if (creature.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                armor.RefreshAmount = 0;
                armor.RefreshPeriod = 0;
            }

            var window = new GameEffect
            {
                TypeId = PreDeathTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = LevelFor(damageType),
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = false,
                ExpiresTick = Environment.TickCount64 + WindowMs,
                OnExpired = WindowClosed
            };

            GameEffectManager.Instance.Attach(mapChannel, creature, window);

            return true;
        }

        /// <summary>Nobody finished it: it dies to whoever opened the window, as it would have.</summary>
        private static void WindowClosed(MapChannel mapChannel, Actor actor, GameEffect window)
        {
            if (actor is Creature creature && creature.State == CharacterState.Dying)
                CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, window.Source);
        }

        public void RequestCritDeathFinish(Client client, RequestCritDeathFinishPacket packet)
        {
            var player = client.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return;

            if (packet.ActionId != ActionId.CriticalDeathFinisher)
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} sent RequestCritDeathFinish for action {packet.ActionId}");
                return;
            }

            if (player.State == CharacterState.Dead || !player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
            {
                Fail(client, packet.ActionArgId, null);
                return;
            }

            var creature = EntityManager.Instance.GetEntityType(packet.TargetId) == EntityType.Creature
                ? EntityManager.Instance.GetCreature(packet.TargetId)
                : null;

            var window = creature != null && creature.State == CharacterState.Dying && creature.MapContextId == mapChannel.MapInfo.MapContextId
                ? PreDeathOf(creature)
                : null;

            if (window == null || window.FinisherId != 0 || !MayFinish(player, window.Source))
            {
                Fail(client, packet.ActionArgId, PlayerMessage.PmTargetInvalid);
                return;
            }

            AbilityManager.Instance.TryGetLevel(ActionId.CriticalDeathFinisher, packet.ActionArgId, out var level);

            var range = level != null && level.MaxRange > 0 ? level.MaxRange : DefaultRange;

            if (Vector3.Distance(player.Position, creature.Position) > range + RangeSlack)
            {
                Fail(client, packet.ActionArgId, PlayerMessage.PmTargetOutOfRange);
                return;
            }

            var windupMs = level != null && level.WindupMs > 0 ? level.WindupMs : DefaultWindupMs;

            // Claimed: a squad mate pressing at the same moment is turned away, and the window is
            // kept open at least until this windup has run.
            window.FinisherId = player.EntityId;

            var until = Environment.TickCount64 + windupMs + 1000;

            // Held open past its time: the clients' tooltip timer is told, or it would read
            // zero while the finisher winds up. The target window's icon keeps its first time
            // until the finishing animation's effect goes on and it re-reads them all.
            //
            // Once per window. A claim that was interrupted and claimed again extended it each
            // time, so the creature could be kept Dying - untargetable, and nobody's kill - for as
            // long as one player kept pressing and cancelling.
            if (until > window.ExpiresTick && !window.FinisherExtended)
            {
                window.FinisherExtended = true;
                window.ExpiresTick = until;
                GameEffectManager.Instance.UpdateTooltip(mapChannel, creature, window);
            }

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var other in cell.ClientList)
                    if (other.Player != player)
                        other.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.ThreeArgs, ActionId.CriticalDeathFinisher, packet.ActionArgId, creature.EntityId));

            mapChannel.PerformRecovery.Add(new ActionData(player, ActionId.CriticalDeathFinisher, packet.ActionArgId, creature.EntityId, windupMs));
        }

        /// <summary>Called from ActorActionManager when the finisher's windup has run.</summary>
        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            if (!(action.Actor is Manifestation player))
                return;

            var client = ClientOf(mapChannel, player);
            var creature = EntityManager.Instance.GetEntityType(action.TargetId) == EntityType.Creature
                ? EntityManager.Instance.GetCreature(action.TargetId)
                : null;
            var window = creature != null && creature.State == CharacterState.Dying ? PreDeathOf(creature) : null;

            if (window == null || window.FinisherId != player.EntityId || action.IsInrerrupted)
            {
                if (window != null && window.FinisherId == player.EntityId)
                    window.FinisherId = 0;

                if (client != null)
                {
                    if (action.IsInrerrupted)
                        ActorManager.ResolveInterruptedRequest(client, ActionId.CriticalDeathFinisher, action.ActionArgId);
                    else
                        Fail(client, action.ActionArgId, PlayerMessage.PmTargetInvalid);
                }

                return;
            }

            var opener = window.Source;
            var now = Environment.TickCount64;

            // The finishing animation, announced by the recovery below; when it has played the
            // creature is dead and the kill is the finisher's.
            var death = new GameEffect
            {
                TypeId = DeathTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = window.EffectLevel,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = false,
                AnnounceOnAttach = false,
                ExpiresTick = now + DeathAnimationFor(window.EffectLevel),
                OnExpired = (m, a, e) => Finished(m, a, player, opener)
            };

            // The corpse pose the client announces when the finishing animation ends.
            var postDeath = new GameEffect
            {
                TypeId = PostDeathTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = window.EffectLevel,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = false,
                AnnounceOnAttach = false
            };

            GameEffectManager.Instance.Attach(mapChannel, creature, death);
            GameEffectManager.Instance.Attach(mapChannel, creature, postDeath);

            // Taken off directly, not left to run out, so WindowClosed does not kill it; with a
            // CRIT_DEATH_EFFECT already on the creature the client does not play a death for it.
            GameEffectManager.Instance.DettachEffect(mapChannel, creature, window);

            var args = new MissileArgs();
            args.HitEntities.Add(creature.EntityId);

            CellManager.Instance.CellCallMethod(mapChannel, player, new PerformRecoveryPacket(PerformType.ListOfArgs, ActionId.CriticalDeathFinisher, action.ActionArgId, args));
        }

        private static void Finished(MapChannel mapChannel, Actor actor, Manifestation finisher, Actor opener)
        {
            if (actor is Creature creature && creature.State == CharacterState.Dying)
                CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, finisher, KindOf(finisher, opener));
        }

        public static CritKill KindOf(Manifestation finisher, Actor opener) => opener == null || opener == finisher ? CritKill.Own : CritKill.Team;

        private static Client ClientOf(MapChannel mapChannel, Manifestation player)
        {
            foreach (var client in mapChannel.ClientList)
                if (client?.Player == player)
                    return client;

            return null;
        }

        private static void Fail(Client client, uint argId, PlayerMessage? message)
        {
            ActorManager.RefuseRequest(client, ActionId.CriticalDeathFinisher, argId, message);
        }
    }

    /// <summary>How a kill was made, for the experience it is worth and how the client words it.</summary>
    public enum CritKill
    {
        None,
        /// <summary>Finished by the player who opened the window: "by Crit Killing".</summary>
        Own,
        /// <summary>Finished by a squad mate of the player who opened it: "by Team Crit Killing".</summary>
        Team
    }
}
