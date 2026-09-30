using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// How much a creature hates whom, and who it fights because of it. Every creature carries a
    /// HateTable; this is what fills it, empties it and reads it.
    ///
    /// The weights are the client's own, from shared/gameconstants.py - the module the server
    /// shared with the client, whose DamageInfo carries doHate, threatMod and extraThreat on every
    /// hit: DAMAGE_THREAT_MOD 3.0 for each point that landed, ABSORBED_THREAT_MOD 2.0 for each a
    /// shield soaked up, RESISTED_THREAT_MOD 1.0 for each resisted. So a hit is hated for
    /// (landed x 3 + absorbed x 2 + resisted x 1), times the attacker's perceived-threat
    /// modifier - THREAT_MODIFIER_PERCENT on the effects on them (Sacrifice: +30 / +75 / +300 %,
    /// "Perceived Threat: X%").
    ///
    /// The rest the client does not say, and is chosen:
    ///  - noticing someone in the aggro scan puts them on the table with a token 1, so the first
    ///    enemy seen is fought until somebody actually hurts it;
    ///  - healing a player is hated by every creature that already hates that player, one point
    ///    per point healed - the healing tools' "Threat Reduction" modules say healing draws
    ///    threat, and not how much;
    ///  - a new attacker takes the creature over from its current target only when it is hated
    ///    10 % more, so two players doing about the same do not have it turning every think.
    ///
    /// What empties it: an entry that can no longer be fought is dropped as the creature looks
    /// (dead, gone, off the map, cloaked); a cloak (Detection) and Tactical Evasion's mag flash
    /// ("Clear Enemy Hate") take entries off on purpose; and a creature that gives up the fight -
    /// leashed home, or nobody left it can reach - forgets the lot.
    /// </summary>
    public static class Threat
    {
        public const double DamageThreatMod = 3.0;      // shared/gameconstants.py DAMAGE_THREAT_MOD
        public const double AbsorbedThreatMod = 2.0;    // ABSORBED_THREAT_MOD
        public const double ResistedThreatMod = 1.0;    // RESISTED_THREAT_MOD

        /// <summary>What being noticed in the aggro scan is worth: enough to be on the table.</summary>
        public const double NoticedThreat = 1.0;

        /// <summary>Hate per point of healing done to someone a creature hates.</summary>
        public const double HealThreatMod = 1.0;

        /// <summary>How much more a rival has to be hated to take the creature off its current target.</summary>
        public const int TakeoverPercent = 110;

        /// <summary>The attacker's perceived threat, in percent: 100 plus the THREAT_MODIFIER_PERCENT on the effects on them.</summary>
        public static int ThreatModifierOf(Actor actor)
        {
            if (actor == null)
                return 100;

            var percent = 100;

            foreach (var effect in actor.ActiveEffects.Values)
                percent += effect.ThreatModifierPercent;

            return Math.Max(0, percent);
        }

        /// <summary>The hate a hit is worth before the attacker's modifier: landed x 3 + absorbed x 2 + resisted x 1.</summary>
        public static double OfHit(int landed, int absorbed, int resisted)
        {
            return Math.Max(0, landed) * DamageThreatMod + Math.Max(0, absorbed) * AbsorbedThreatMod + Math.Max(0, resisted) * ResistedThreatMod;
        }

        /// <summary>
        /// A creature was hit and survived: the attacker is hated for it, and a creature that was
        /// not fighting anyone turns on them.
        /// </summary>
        public static void FromDamage(Creature victim, Actor attacker, int landed, int absorbed = 0, int resisted = 0)
        {
            if (victim == null || attacker == null || attacker == victim)
                return;

            var hate = OfHit(landed, absorbed, resisted) * ThreatModifierOf(attacker) / 100.0;

            hate = ToSummon(victim, attacker, hate);
            hate = ToMaster(victim, attacker, hate);

            // A hit for nothing still says who is shooting.
            victim.Hate.Ensure(attacker.EntityId, NoticedThreat);
            victim.Hate.Add(attacker.EntityId, hate);

            if (victim.Controller != null
                && (victim.Controller.CurrentAction == BehaviorManager.BehaviorActionWander
                    || victim.Controller.CurrentAction == BehaviorManager.BehaviorActionFollowingPath))
                BehaviorManager.Instance.SetActionFighting(victim, attacker.EntityId);
        }

        /// <summary>The aggro scan picked somebody out: they are on the table.</summary>
        public static void Noticed(Creature creature, ulong entityId)
        {
            creature?.Hate.Ensure(entityId, NoticedThreat);
        }

        /// <summary>
        /// A healer healed a player: every creature near the healed one that already hates them
        /// hates the healer for it.
        /// </summary>
        public static void FromHealing(MapChannel mapChannel, Actor healer, Actor healed, int amount)
        {
            if (mapChannel == null || healer == null || healed == null || amount <= 0)
                return;

            var hate = amount * HealThreatMod * ThreatModifierOf(healer) / 100.0;

            foreach (var cell in CellManager.CellsIn(mapChannel, healed.Cells))
                foreach (var creature in cell.CreatureList.ToList())
                    if (creature.State != CharacterState.Dead && creature.Hate.Contains(healed.EntityId))
                        creature.Hate.Add(healer.EntityId, ToSummon(creature, healer, hate));
        }

        /// <summary>
        /// MINION_HATE_TO_MASTER_PERCENT (Spotter): of the hate a summon earns, its share goes to
        /// its master instead - when the master is a player on the same map. Returns what is left
        /// for the summon.
        /// </summary>
        public static double ToMaster(Creature victim, Actor attacker, double hate)
        {
            if (hate <= 0 || !(attacker is Creature summon) || summon.HateToMasterPercent <= 0 || summon.MasterEntityId == 0)
                return hate;

            if (!EntityManager.Instance.Players.TryGetValue(summon.MasterEntityId, out var master) || master.MapContextId != victim.MapContextId)
                return hate;

            var share = AbilityManager.HateTransferred(hate, summon.HateToMasterPercent);

            victim.Hate.Ensure(master.EntityId, NoticedThreat);
            victim.Hate.Add(master.EntityId, share);

            return hate - share;
        }

        /// <summary>
        /// HATE_TRANSFER_PERCENT: a player with a summon - trap, turret, reality ripper - near the
        /// creature has that share of the hate they have just earned go to the summon instead
        /// (AbilityManager.HateSinkFor). Returns what is left for the player. Only new hate is
        /// split; what the creature already held against the player stays where it is.
        /// </summary>
        public static double ToSummon(Creature victim, Actor attacker, double hate)
        {
            if (hate <= 0 || !(attacker is Manifestation owner))
                return hate;

            var (summon, percent) = AbilityManager.HateSinkFor(owner, victim);

            if (summon == null)
                return hate;

            var share = AbilityManager.HateTransferred(hate, percent);

            victim.Hate.Ensure(summon.EntityId, NoticedThreat);
            victim.Hate.Add(summon.EntityId, share);

            return hate - share;
        }

        /// <summary>
        /// An actor has gone out of reach of every creature around - cloaked: it is taken off their
        /// tables, and those that were fighting it turn to whoever they hate next, or give up.
        /// </summary>
        public static void Forget(MapChannel mapChannel, Actor actor)
        {
            foreach (var cell in CellManager.CellsIn(mapChannel, actor.Cells))
                foreach (var creature in cell.CreatureList.ToList())
                {
                    if (!creature.Hate.Contains(actor.EntityId))
                        continue;

                    creature.Hate.Remove(actor.EntityId);

                    if (creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting
                        && creature.Controller.ActionFighting.TargetEntityId == actor.EntityId
                        && !Retarget(creature))
                        BehaviorManager.Instance.StopFighting(creature);
                }
        }

        /// <summary>"Clear Enemy Hate": the creature forgets everyone and stops fighting.</summary>
        public static void Clear(Creature creature)
        {
            if (creature == null)
                return;

            creature.Hate.Clear();

            if (creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting)
                BehaviorManager.Instance.StopFighting(creature);
        }

        /// <summary>
        /// Whether a creature can fight this entity at all right now: a living player it can see
        /// on its own map, or a living creature on it - either one its target category allows
        /// (BehaviorManager.MayFight), so a table never hands it a target SetActionFighting refuses.
        /// </summary>
        public static bool CanFight(Creature creature, ulong entityId)
        {
            if (!BehaviorManager.MayFight(creature, entityId))
                return false;

            switch (EntityManager.Instance.GetEntityType(entityId))
            {
                case EntityType.Character:
                {
                    var player = EntityManager.Instance.GetPlayer(entityId);

                    // Disconected: a connection that has gone, whose character is waiting for the
                    // map worker to take it out. It cannot fight back or be healed, and a creature
                    // that kept at it would be beating on an empty body.
                    return player != null && !player.Disconected && player.MapContextId == creature.MapContextId
                        && player.State != CharacterState.Dead && player.Attributes[Attributes.Health].Current > 0
                        && !Detection.IsHidden(player) && !CameraScripts.IsWatching(player);
                }

                case EntityType.Creature:
                {
                    var other = EntityManager.Instance.GetCreature(entityId);

                    return other != null && other != creature && other.MapContextId == creature.MapContextId
                        && other.State != CharacterState.Dead && other.State != CharacterState.Dying
                        && other.Attributes[Attributes.Health].Current > 0;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Who the creature should be fighting now: the most hated it can still fight, the current
        /// target kept unless a rival is hated TakeoverPercent more. Entries that can never be
        /// fought again - dead, gone, off the map - are dropped as it looks; a cloaked one is
        /// only passed over. 0 when nobody on the table can be fought.
        /// </summary>
        public static ulong ChooseTarget(Creature creature)
        {
            // Not Ranked(): nothing here needs an order, and this runs every think.
            creature.Hate.RemoveWhere(id => EntityManager.Instance.GetEntityType(id) == 0);

            var current = creature.Controller?.ActionFighting.TargetEntityId ?? 0;

            return creature.Hate.Top(id => CanFight(creature, id), current, TakeoverPercent);
        }

        /// <summary>
        /// The creature's current target is lost: it is taken off the table and the creature turns
        /// to whoever it hates next. False when there is nobody, and the creature should give up.
        /// </summary>
        public static bool Retarget(Creature creature)
        {
            var lost = creature.Controller?.ActionFighting.TargetEntityId ?? 0;

            if (lost != 0)
                creature.Hate.Remove(lost);

            var next = ChooseTarget(creature);

            if (next == 0)
                return false;

            BehaviorManager.Instance.SetActionFighting(creature, next);
            return true;
        }
    }
}
