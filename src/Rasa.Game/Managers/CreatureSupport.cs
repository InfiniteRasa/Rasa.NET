using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.ClientMethod.Server;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The creatures that look after their own side, from their client classes:
    ///
    ///  - Heal (CaretakerHealAbility, CR_CARETAKER_HEAL 239): HEAL_AMOUNT to every creature of the
    ///    Caretaker's side within RADIUS_AROUND_SOURCE, itself included, announced as bare heal
    ///    amounts (AnnounceHealing).
    ///  - Repair (TechnicianHealAbility, CR_TECHNICIAN_HEAL 275): the same reach, hitdata
    ///    (healAmount, repairAmount): health to MECHANICAL or MACHINA allies, armour to the rest -
    ///    "Repair the body armor of an ally or the health and shields of a mechanical unit", as a
    ///    Polymorphed player's Repair does (AbilityManager.MechanicalRepair).
    ///  - Revive (CaretakerReviveAbility, CR_CARETAKER_REVIVE 242): canTargetDead, a bare heal
    ///    amount per hit; the class announces the revive and the healing. A corpse of the
    ///    Caretaker's side within the row's range, dead less than ReviveWindowMs, gets up where it
    ///    lay with HEAL_AMOUNT health: back in its spawn pool's count, its loot and harvest rights
    ///    gone, Revived sent so every client stands it up.
    ///  - Funnel (LifeforceFunnelAction, CR_FOREAN_LIFEFORCE_FUNNEL 255): a Forean shaman's heal,
    ///    a HealAbility aimed at nothing - its side within RADIUS_AROUND_SOURCE (24 m), itself
    ///    included, each healed by the argument's DAMAGE_AMOUNT_MIN..MAX (the only amount it
    ///    has), announced as bare heal amounts as the Caretaker's is.
    ///  - Jumpstart (TechnicianReviveAbility, CR_TECHNICIAN_REVIVE 400): "Perform emergency
    ///    repairs and restart a downed vehicle, turret, or other mechanical ally." The Caretaker's
    ///    revive as the data shows it - canTargetDead, a bare heal amount per hit, AnnounceRevive
    ///    - with its bolt's flight (VFX_VELOCITY 10 m/s) added to its windup, on machines only: a
    ///    MECHANICAL or MACHINA corpse of its side (a Linker, a Machina, a Shield Drone, a Bane
    ///    turret), its own summoned turret included. The client's range is 1 m, which would have
    ///    it walk to the body; the row's reach is the corpse it can bring back from where it
    ///    stands.
    ///  - Self revive (MachinaReviveAbility, CR_MACHINA_REVIVE 429): a Machina's first death in a
    ///    life is not its end. It goes down - no kill, no experience, no loot - and the recovery
    ///    (3 s) later stands up at HEAL_PERCENT of its health and goes back to its fight; the next
    ///    death is a death.
    ///
    /// A creature finished by a Critical Death is neither: the finishing move destroys the body,
    /// "Your target can't self-resuscitate ... and allies can't bring back the target" (the
    /// strategy guide). Creature.CritKilled.
    ///
    /// Heal, repair and revive are wound up first (the windup the client plays), the creature
    /// standing still, and land when it is done - on whoever is there to take them then. A heal
    /// is only started for an ally below HurtPercent of its health (or, for the repair, short of
    /// armour), a revive only for a corpse that can be revived; otherwise the fighting loop goes on
    /// to the creature's attacks.
    ///
    /// Amounts are the argument's HEAL_AMOUNT_MIN..MAX scaled to the caster's level
    /// (AbilityManager.Scale, DAMAGE_SCALE_TYPE): creature health is on the creature scale, so none
    /// of the 0.25 that brings a creature's hits down to a player's health.
    /// </summary>
    public static class CreatureSupport
    {
        public const ActionId CaretakerHeal = (ActionId)239;
        public const ActionId CaretakerRevive = (ActionId)242;
        public const ActionId TechnicianHeal = (ActionId)275;
        public const ActionId MachinaRevive = (ActionId)429;
        public const ActionId ForeanFunnel = (ActionId)255;
        public const ActionId TechnicianRevive = (ActionId)400;

        /// <summary>Ours: an ally below this share of its health is worth a heal.</summary>
        public const int HurtPercent = 80;

        /// <summary>Ours: how long a corpse can still be revived.</summary>
        public const long ReviveWindowMs = 30000;

        /// <summary>How long a Machina lies down before getting up, when its data gives no recovery.</summary>
        public const long SelfReviveDefaultMs = 3000;

        public enum Kind { None, Heal, Repair, Revive, SelfRevive }

        public static Kind KindOf(ActionId actionId)
        {
            switch (actionId)
            {
                case CaretakerHeal:
                case ForeanFunnel: return Kind.Heal;
                case TechnicianHeal: return Kind.Repair;
                case CaretakerRevive:
                case TechnicianRevive: return Kind.Revive;
                case MachinaRevive: return Kind.SelfRevive;
                default: return Kind.None;
            }
        }

        public static bool Is(CreatureAction action) => action != null && KindOf(action.ActionId) != Kind.None;

        private sealed class Cast
        {
            public MapChannel MapChannel;
            public Creature Caster;
            public CreatureAction Action;
            public Kind Kind;
            public Creature Target;
            public ulong FightingId;
            public long LandsAt;
        }

        private static readonly List<Cast> Casts = new List<Cast>();
        private static readonly object CastsLock = new object();
        private static readonly HashSet<ulong> SelfRevived = new HashSet<ulong>();
        private static readonly Random Random = new Random();

        /// <summary>Whether the creature is winding up a heal or a revive, or lying down before its self revive: it does nothing else.</summary>
        public static bool IsCasting(Creature creature)
        {
            // A loop, not Any(): this is asked of every creature on every think, and Any with a
            // lambda that captures the creature allocates a closure and a delegate each time.
            lock (CastsLock)
            {
                foreach (var cast in Casts)
                    if (cast.Caster == creature)
                        return true;

                return false;
            }
        }

        private static ActionLevelInfo LevelOf(CreatureAction action)
        {
            if (AbilityManager.Instance == null || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return null;

            return info;
        }

        /// <summary>The heal an argument gives: HEAL_AMOUNT_MIN..MAX, or DAMAGE_AMOUNT_MIN..MAX where it has no other (a Lifeforce Funnel).</summary>
        public static (int Min, int Max) HealRangeOf(ActionLevelInfo info)
        {
            var heal = info.Has(AbilityProperty.HealAmountMin) || info.Has(AbilityProperty.HealAmountMax);
            var minProperty = heal ? AbilityProperty.HealAmountMin : AbilityProperty.DamageAmountMin;
            var maxProperty = heal ? AbilityProperty.HealAmountMax : AbilityProperty.DamageAmountMax;
            var min = info.Get(minProperty);

            return (min, Math.Max(min, info.Get(maxProperty, min)));
        }

        /// <summary>HEAL_AMOUNT_MIN..MAX (HealRangeOf) scaled to the creature's level.</summary>
        public static int RollHeal(Creature caster, ActionLevelInfo info)
        {
            var (min, max) = HealRangeOf(info);
            int rolled;

            lock (Random)
                rolled = Random.Next(min, max + 1);

            return Math.Max(0, AbilityManager.Scale((int)caster.Level, rolled, info.Get(AbilityProperty.DamageScaleType)));
        }

        public static bool IsHurt(Actor actor)
        {
            return actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0 && health.CurrentMax > 0
                && health.Current * 100 < health.CurrentMax * HurtPercent;
        }

        private static bool ShortOfArmor(Actor actor)
        {
            return actor.Attributes.TryGetValue(Attributes.Armor, out var armor) && armor.CurrentMax > 0 && armor.Current < armor.CurrentMax;
        }

        private static bool IsMachine(Creature creature)
        {
            if (creature == null)
                return false;

            var flags = CreatureManager.CreatureFlagsOf(creature);

            return flags.Contains((int)CreatureFlag.Mechanical) || flags.Contains((int)CreatureFlag.Machina);
        }

        /// <summary>The creature and its living allies within radius.</summary>
        private static List<Creature> Flock(MapChannel mapChannel, Creature caster, float radius)
        {
            var flock = CreatureBuffs.AlliesWithin(mapChannel, caster, caster.Position, radius);

            flock.Insert(0, caster);

            return flock;
        }

        /// <summary>
        /// Whether a corpse can be revived by this caster: of its side, dead less than
        /// ReviveWindowMs, a spawned creature (not a minion or a scripted object), not claimed by a
        /// corpse ability, and not one the client has taken away (a Howler's death bomb).
        /// </summary>
        public static bool IsRevivable(Creature caster, Creature corpse, bool summonsToo = false)
        {
            if (corpse == null || caster == null || corpse == caster || corpse.State != CharacterState.Dead || corpse.IsScripted)
                return false;

            // A minion is its master's; a creature's summon (a turret) only where the revive allows it.
            if (corpse.MasterEntityId != 0 && !(summonsToo && CreatureSummons.IsSummoned(corpse)))
                return false;

            // A Machina lying down before its own self revive gets itself up.
            if (IsCasting(corpse))
                return false;

            // A Necromite is blowing the body up.
            if (CreatureBombs.IsCorpseClaimed(corpse))
                return false;

            if (corpse.TargetCategory != caster.TargetCategory || corpse.Controller.DeadTime > ReviveWindowMs)
                return false;

            if (corpse.Actions.Any(a => CreatureBombs.KindOf(a.ActionId) == CreatureBombs.Kind.DeathBomb))
                return false;

            // A finishing move destroyed the body: "allies can't bring back the target".
            if (corpse.CritKilled)
                return false;

            // Claimed by Reanimation, Cadaver Immolation, a Hortimonculus.
            return !AbilityManager.IsBiologicalCorpse(corpse) || AbilityManager.IsUsableCorpse(corpse);
        }

        /// <summary>Whether this caster's revive may bring this corpse back: a Technician's jumpstart machines alone, summoned turrets included.</summary>
        public static bool CanRevive(Creature caster, CreatureAction action, Creature corpse)
        {
            if (action != null && action.ActionId == TechnicianRevive)
                return IsMachine(corpse) && IsRevivable(caster, corpse, summonsToo: true);

            return IsRevivable(caster, corpse);
        }

        /// <summary>
        /// From the fighting loop: starts the first support action the creature has ready that
        /// would do something now. Whether one was started.
        /// </summary>
        public static bool TryStart(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature?.Actions == null || IsCasting(creature))
                return false;

            foreach (var action in BehaviorManager.ByReadiness(creature.Actions))
            {
                if (action.CooldownTimer > 0)
                    continue;

                var kind = KindOf(action.ActionId);

                if (kind == Kind.None || kind == Kind.SelfRevive)
                    continue;

                var info = LevelOf(action);

                if (info == null)
                    continue;

                Creature target = null;

                switch (kind)
                {
                    case Kind.Heal:
                        if (!Flock(mapChannel, creature, info.Get(AbilityProperty.RadiusAroundSource, 20)).Any(IsHurt))
                            continue;
                        break;

                    case Kind.Repair:
                        if (!Flock(mapChannel, creature, info.Get(AbilityProperty.RadiusAroundSource, 20)).Any(c => IsMachine(c) ? IsHurt(c) : ShortOfArmor(c)))
                            continue;
                        break;

                    case Kind.Revive:
                    {
                        var reach = (float)Math.Max(1, action.RangeMax);

                        target = CorpsesWithin(mapChannel, creature, reach).FirstOrDefault(c => CanRevive(creature, action, c));

                        if (target == null)
                            continue;
                        break;
                    }
                }

                Start(mapChannel, creature, action, kind, target, info.WindupMs + BoltFlightMs(creature, target, info));
                action.CooldownTimer = BehaviorManager.NextCooldown(creature, action);

                return true;
            }

            return false;
        }

        /// <summary>A bolt's flight to the one it is cast at, where the argument gives it a VFX_VELOCITY (a Technician's jumpstart); 0 otherwise.</summary>
        public static int BoltFlightMs(Creature caster, Actor target, ActionLevelInfo info)
        {
            if (caster == null || target == null || info == null || info.Get(AbilityProperty.VfxVelocity) <= 0)
                return 0;

            return CreatureWindups.FlightMs(info, Vector3.Distance(caster.Position, target.Position));
        }

        /// <summary>Dead creatures within reach of the caster, nearest first.</summary>
        private static List<Creature> CorpsesWithin(MapChannel mapChannel, Creature caster, float reach)
        {
            var found = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, caster.Cells))
                foreach (var creature in cell.CreatureList)
                    if (creature.State == CharacterState.Dead && Vector3.DistanceSquared(creature.Position, caster.Position) <= reach * reach)
                        found.Add(creature);

            return found.Distinct().OrderBy(c => Vector3.DistanceSquared(c.Position, caster.Position)).ToList();
        }

        private static void Start(MapChannel mapChannel, Creature caster, CreatureAction action, Kind kind, Creature target, long windupMs)
        {
            BehaviorManager.Instance.StopMoving(caster);
            caster.Controller.Path.Clear();

            CellManager.Instance.CellCallMethod(mapChannel, caster,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, (target ?? caster).EntityId));

            lock (CastsLock)
                Casts.Add(new Cast
                {
                    MapChannel = mapChannel,
                    Caster = caster,
                    Action = action,
                    Kind = kind,
                    Target = target,
                    LandsAt = Environment.TickCount64 + Math.Max(0, windupMs)
                });
        }

        /// <summary>
        /// A creature finished by a Critical Death: it does not get up again, and whatever it had
        /// or had not spent of its self revive is forgotten, so its next life gets one of its own
        /// just as it would after an ordinary second death.
        /// </summary>
        public static void ForgetSelfRevive(Creature creature)
        {
            if (creature == null)
                return;

            lock (CastsLock)
                SelfRevived.Remove(creature.EntityId);
        }

        /// <summary>
        /// A creature is about to die: if it is a Machina whose self revive is unspent this life,
        /// it goes down instead, and gets up again. Whether the death was put off. Not asked for
        /// a Critical Death finish, which destroys the body (ForgetSelfRevive).
        /// </summary>
        public static bool DefersDeath(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature?.Actions == null || creature.IsScripted || creature.MasterEntityId != 0)
                return false;

            var action = creature.Actions.FirstOrDefault(a => KindOf(a.ActionId) == Kind.SelfRevive);

            if (action == null)
                return false;

            lock (CastsLock)
            {
                if (SelfRevived.Contains(creature.EntityId))
                {
                    // This is the death that counts; the next life gets its own revive.
                    SelfRevived.Remove(creature.EntityId);
                    return false;
                }

                SelfRevived.Add(creature.EntityId);
            }

            var info = LevelOf(action);
            var fighting = creature.Controller.ActionFighting.TargetEntityId;

            creature.State = CharacterState.Dead;
            creature.KnockbackTo = null;
            creature.HarvestAttemptsLeft = 0;
            creature.Attributes[Attributes.Health].Current = 0;
            BehaviorManager.Instance.StopMoving(creature);
            CellManager.Instance.CellCallMethod(mapChannel, creature, new StateChangePacket(new List<CharacterState> { CharacterState.Dead }));

            lock (CastsLock)
                Casts.Add(new Cast
                {
                    MapChannel = mapChannel,
                    Caster = creature,
                    Action = action,
                    Kind = Kind.SelfRevive,
                    FightingId = fighting,
                    LandsAt = Environment.TickCount64 + (info != null && info.RecoveryMs > 0 ? info.RecoveryMs : SelfReviveDefaultMs)
                });

            return true;
        }

        /// <summary>Lands the heals, repairs and revives whose windup is up on this map. Run every map tick.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Cast> due;
            var now = Environment.TickCount64;

            lock (CastsLock)
            {
                due = Casts.Where(c => c.MapChannel == mapChannel && now >= c.LandsAt).ToList();

                foreach (var cast in due)
                    Casts.Remove(cast);
            }

            foreach (var cast in due)
            {
                var caster = cast.Caster;

                if (caster.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                // Killed, or put in its Critical Death window, during the windup: nothing lands.
                if (cast.Kind != Kind.SelfRevive && (caster.State == CharacterState.Dead || caster.State == CharacterState.Dying))
                    continue;

                var info = LevelOf(cast.Action);

                if (info == null)
                    continue;

                switch (cast.Kind)
                {
                    case Kind.Heal:
                        Heal(mapChannel, caster, cast.Action, info);
                        break;
                    case Kind.Repair:
                        Repair(mapChannel, caster, cast.Action, info);
                        break;
                    case Kind.Revive:
                        ReviveOther(mapChannel, caster, cast.Action, info, cast.Target);
                        break;
                    case Kind.SelfRevive:
                        GetUp(mapChannel, caster, cast.Action, info, cast.FightingId);
                        break;
                }
            }
        }

        private static void Heal(MapChannel mapChannel, Creature caster, CreatureAction action, ActionLevelInfo info)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Heal);

            foreach (var ally in Flock(mapChannel, caster, info.Get(AbilityProperty.RadiusAroundSource, 20)))
            {
                var healed = ActorManager.Instance.Heal(ally, RollHeal(caster, info), caster.EntityId);

                recovery.Hits.Add(new AbilityHit { EntityId = ally.EntityId, Amount = healed });
            }

            CellManager.Instance.CellCallMethod(mapChannel, caster, recovery);
        }

        private static void Repair(MapChannel mapChannel, Creature caster, CreatureAction action, ActionLevelInfo info)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.HealRepair);

            foreach (var ally in Flock(mapChannel, caster, info.Get(AbilityProperty.RadiusAroundSource, 20)))
            {
                if (IsMachine(ally))
                    recovery.Hits.Add(new AbilityHit { EntityId = ally.EntityId, Amount = ActorManager.Instance.Heal(ally, RollHeal(caster, info), caster.EntityId) });
                else
                    recovery.Hits.Add(new AbilityHit { EntityId = ally.EntityId, Repair = ActorManager.Instance.RestoreArmor(ally, RollHeal(caster, info), caster.EntityId) });
            }

            CellManager.Instance.CellCallMethod(mapChannel, caster, recovery);
        }

        private static void ReviveOther(MapChannel mapChannel, Creature caster, CreatureAction action, ActionLevelInfo info, Creature corpse)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Heal);

            // Gone, taken, or looted away in the windup: the Caretaker performs and nobody gets up.
            if (corpse != null && corpse.MapContextId == mapChannel.MapInfo.MapContextId && CanRevive(caster, action, corpse))
            {
                var health = Revive(mapChannel, corpse, RollHeal(caster, info), caster);

                recovery.Hits.Add(new AbilityHit { EntityId = corpse.EntityId, Amount = health });
            }

            CellManager.Instance.CellCallMethod(mapChannel, caster, recovery);
        }

        private static void GetUp(MapChannel mapChannel, Creature machina, CreatureAction action, ActionLevelInfo info, ulong fightingId)
        {
            if (machina.State != CharacterState.Dead)
                return;

            var max = machina.Attributes[Attributes.Health].CurrentMax;
            var percent = info.Get(AbilityProperty.HealPercentMin, 50);
            var health = Revive(mapChannel, machina, Math.Max(1, max * percent / 100), machina, recount: false);

            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Heal);
            recovery.Hits.Add(new AbilityHit { EntityId = machina.EntityId, Amount = health });
            CellManager.Instance.CellCallMethod(mapChannel, machina, recovery);

            // Back to the fight it was in, if its enemy is still there.
            var enemy = fightingId != 0 ? EntityManager.Instance.GetActor(fightingId) : null;

            if (enemy != null && enemy.State != CharacterState.Dead && enemy.MapContextId == machina.MapContextId)
            {
                machina.Hate.Ensure(fightingId, 1);
                BehaviorManager.Instance.SetActionFighting(machina, fightingId);
            }
        }

        /// <summary>
        /// Stands a dead creature up where it lies with this much health: alive, its corpse's loot
        /// and harvest rights gone, its death clock stopped, and (unless it never left it) back in
        /// its spawn pool's living count. Returns the health it got up with.
        /// </summary>
        public static int Revive(MapChannel mapChannel, Creature creature, int amount, Actor source, bool recount = true)
        {
            var health = creature.Attributes[Attributes.Health];
            var restored = Math.Max(1, Math.Min(health.CurrentMax, amount));

            LootDispenserManager.Instance.RemoveForCreature(mapChannel, creature);

            creature.State = CharacterState.Idle;
            creature.Controller.DeadTime = 0;
            creature.HarvestOwnerEntityId = 0;
            creature.HarvestAttemptsLeft = 0;
            creature.Hate.Clear();
            health.Current = restored;

            if (recount && creature.SpawnPool != null)
            {
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(creature.SpawnPool);
                SpawnPoolManager.Instance.DecreaseDeadCreatureCount(creature.SpawnPool);
            }

            CellManager.Instance.CellCallMethod(mapChannel, creature, new RevivedPacket(source?.EntityId ?? creature.EntityId));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateHealthPacket(health, creature.EntityId));

            BehaviorManager.StartWandering(creature, false);

            return restored;
        }
    }
}
