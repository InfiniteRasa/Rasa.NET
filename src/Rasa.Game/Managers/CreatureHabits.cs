using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// What some creatures do between fights, from the strategy guide's Enemy Intel and the
    /// client's animation-only classes for them (no DoAbility, no effect: the class plays, the
    /// server decides):
    ///
    ///  - Filcher loot (FilcherLootAbility, CR_FILCHER_LOOT 434): Filchers "fly over battlefields
    ///    and look for loot to steal" - "There is absolutely no reason to let them loot the Bane
    ///    soldiers and deprive the AFS of necessary bounty". An idle Filcher that sees a corpse
    ///    with loot on it that nobody has open, within SightRange, flies to it and takes the loot:
    ///    the dispenser is gone. It takes it when its windup is done (0.8 s, the client's
    ///    animation), not as it starts: a player who opens the corpse, or kills or stuns the
    ///    Filcher, before then keeps the loot.
    ///  - Xanx devour (XanxDevourAbility, CR_XANX_DEVOUR 438): Xanx "eat dead Xanx to regain HP".
    ///    A hurt idle Xanx that sees a Xanx corpse within SightRange goes and eats it; one in a
    ///    fight below DevourInFightPercent of its health eats one it is standing by. It regains
    ///    HEAL_AMOUNT scaled to its level, the corpse is gone, and it stands eating for the
    ///    recovery (5.3 s). A corpse with unlooted loot is left alone (ours: the loot is the
    ///    player's). What it regains is shown through XANX_FORTIFY (302), the client's effect for
    ///    it: put on quietly for the meal, then told Heal with the amount (GameEffectHealPacket),
    ///    which floats it over the Xanx and logs it.
    ///  - Predator scan (PredatorScanAbility, CR_PREDATOR_SCAN 425): the Predator is "used to scan
    ///    locations from the air, gathering enemy intel ... equipped with forward-scanning
    ///    search". An idle Predator scans its CONE_RADIUS (45 degrees either side) ahead to the
    ///    action's range every ScanEveryMs: a cloaked player caught in it is revealed, and it
    ///    turns on the first player it finds.
    ///
    /// Distances and intervals marked ours below are not in the client.
    /// </summary>
    public static class CreatureHabits
    {
        public const ActionId FilcherLoot = (ActionId)434;
        public const ActionId XanxDevour = (ActionId)438;
        public const ActionId PredatorScan = (ActionId)425;

        /// <summary>
        /// XANX_FORTIFY: the Xanx's heal, shown. No icon, tooltip or FX; its one method, Heal,
        /// announces healing on its holder. Its docstring says it raises a Xanx's maximum health,
        /// which nothing in the client does and nothing says by how much, so this leaves it alone.
        /// </summary>
        public const int XanxFortifyTypeId = 302;

        /// <summary>Ours: how far an idle scavenger looks for a corpse.</summary>
        public const float SightRange = 30f;

        /// <summary>Ours: how close it has to be to the corpse, past the action's own reach.</summary>
        public const float ReachSlack = 2.5f;

        /// <summary>Ours: how often an idle scavenger looks.</summary>
        public const long LookEveryMs = 2000;

        /// <summary>Ours: how long a Xanx corpse stays edible.</summary>
        public const long EdibleWindowMs = 60000;

        /// <summary>Ours: below this share of its health a fighting Xanx eats a corpse it stands by.</summary>
        public const int DevourInFightPercent = 50;

        /// <summary>Ours: how often an idle Predator scans.</summary>
        public const long ScanEveryMs = 8000;

        /// <summary>The Xanx entity classes: Creature_Xanx_Standard, _Boss, _Albino, Bane_Xanx, Bane_Xanx_Boss.</summary>
        public static readonly HashSet<uint> XanxClasses = new HashSet<uint> { 7510, 10522, 23124, 7586, 10523 };

        private static readonly Dictionary<ulong, long> NextLook = new Dictionary<ulong, long>();
        private static readonly Dictionary<ulong, Creature> Errands = new Dictionary<ulong, Creature>();
        private static readonly Dictionary<ulong, long> BusyUntil = new Dictionary<ulong, long>();
        private static readonly object Lock = new object();

        public static bool Is(CreatureAction action) =>
            action != null && (action.ActionId == FilcherLoot || action.ActionId == XanxDevour || action.ActionId == PredatorScan);

        private static CreatureAction HabitOf(Creature creature) => creature?.Actions?.FirstOrDefault(Is);

        /// <summary>
        /// A creature leaving the world: its entries go. The three tables are keyed by entity id
        /// and were only ever written - an errand only cleared on arrival - so every creature
        /// with a habit that ever spawned stayed in them, one that died on an errand kept the
        /// corpse it was going for alive with it, and a new creature given a recycled id
        /// inherited the old one's busy time, next look and errand.
        /// </summary>
        public static void Forget(Creature creature)
        {
            if (creature == null)
                return;

            lock (Lock)
            {
                NextLook.Remove(creature.EntityId);
                Errands.Remove(creature.EntityId);
                BusyUntil.Remove(creature.EntityId);
            }
        }

        /// <summary>Whether the creature is in the middle of a habit's animation - eating, looting, scanning.</summary>
        public static bool IsBusy(Creature creature)
        {
            lock (Lock)
                return BusyUntil.TryGetValue(creature.EntityId, out var until) && Environment.TickCount64 < until;
        }

        private static void Busy(Creature creature, long ms)
        {
            lock (Lock)
                BusyUntil[creature.EntityId] = Environment.TickCount64 + Math.Max(0, ms);
        }

        private static ActionLevelInfo LevelOf(CreatureAction action)
        {
            if (AbilityManager.Instance == null || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return null;

            return info;
        }

        private static LootDispenser LootOn(MapChannel mapChannel, Creature corpse)
        {
            return corpse.CorpseLootEntityId != 0 && mapChannel.LootDispensers.TryGetValue(corpse.CorpseLootEntityId, out var loot) ? loot : null;
        }

        /// <summary>A corpse a Filcher would steal from: loot on it, and nobody with it open.</summary>
        public static bool IsWorthStealing(MapChannel mapChannel, Creature corpse)
        {
            if (corpse == null || corpse.State != CharacterState.Dead)
                return false;

            var loot = LootOn(mapChannel, corpse);

            return loot != null && loot.HasLoot && loot.CurrentLooter == 0;
        }

        /// <summary>A corpse a Xanx would eat: a Xanx, dead within EdibleWindowMs, with no loot waiting for a player.</summary>
        public static bool IsEdible(MapChannel mapChannel, Creature eater, Creature corpse)
        {
            if (corpse == null || corpse == eater || corpse.State != CharacterState.Dead || corpse.IsScripted)
                return false;

            if (!XanxClasses.Contains((uint)corpse.EntityClass) || corpse.Controller.DeadTime > EdibleWindowMs)
                return false;

            var loot = mapChannel != null ? LootOn(mapChannel, corpse) : null;

            return loot == null || !loot.HasLoot;
        }

        private static List<Creature> CorpsesNear(MapChannel mapChannel, Creature creature, float range)
        {
            var found = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
                foreach (var other in cell.CreatureList)
                    if (other.State == CharacterState.Dead && Vector3.DistanceSquared(other.Position, creature.Position) <= range * range)
                        found.Add(other);

            return found.Distinct().OrderBy(c => Vector3.DistanceSquared(c.Position, creature.Position)).ToList();
        }

        /// <summary>
        /// From the wander think, while the creature stands idle: somewhere its habit sends it (a
        /// corpse to loot or eat), or null. A Predator scans here instead, where it stands.
        /// </summary>
        public static Vector3? WhereTo(MapChannel mapChannel, Creature creature)
        {
            var habit = HabitOf(creature);

            if (mapChannel == null || habit == null || habit.CooldownTimer > 0 || IsBusy(creature))
                return null;

            var now = Environment.TickCount64;

            lock (Lock)
            {
                if (NextLook.TryGetValue(creature.EntityId, out var next) && now < next)
                    return null;

                NextLook[creature.EntityId] = now + (habit.ActionId == PredatorScan ? ScanEveryMs : LookEveryMs);
            }

            Creature corpse = null;

            switch (habit.ActionId)
            {
                case FilcherLoot:
                    corpse = CorpsesNear(mapChannel, creature, SightRange).FirstOrDefault(c => IsWorthStealing(mapChannel, c));
                    break;

                case XanxDevour:
                    if (creature.Attributes[Attributes.Health].Current < creature.Attributes[Attributes.Health].CurrentMax)
                        corpse = CorpsesNear(mapChannel, creature, SightRange).FirstOrDefault(c => IsEdible(mapChannel, creature, c));
                    break;

                case PredatorScan:
                    Scan(mapChannel, creature, habit);
                    return null;
            }

            if (corpse == null)
                return null;

            lock (Lock)
                Errands[creature.EntityId] = corpse;

            return corpse.Position;
        }

        /// <summary>From the wander think, when the creature has walked where WhereTo sent it: the habit, if the corpse is still there for it.</summary>
        public static void Arrived(MapChannel mapChannel, Creature creature)
        {
            Creature corpse;

            lock (Lock)
            {
                if (!Errands.TryGetValue(creature.EntityId, out corpse))
                    return;

                Errands.Remove(creature.EntityId);
            }

            var habit = HabitOf(creature);

            if (habit == null || corpse == null || corpse.MapContextId != creature.MapContextId)
                return;

            var reach = Math.Max(1f, (float)habit.RangeMax) + ReachSlack;

            if (Vector3.DistanceSquared(corpse.Position, creature.Position) > reach * reach)
                return;

            if (habit.ActionId == FilcherLoot && IsWorthStealing(mapChannel, corpse))
                Steal(mapChannel, creature, habit, corpse);
            else if (habit.ActionId == XanxDevour && IsEdible(mapChannel, creature, corpse))
                Devour(mapChannel, creature, habit, corpse);
        }

        /// <summary>From the fighting think: a Xanx brought low eats a Xanx corpse it is standing by. Whether it did.</summary>
        public static bool TryInFight(MapChannel mapChannel, Creature creature)
        {
            var habit = HabitOf(creature);

            if (mapChannel == null || habit == null || habit.ActionId != XanxDevour || habit.CooldownTimer > 0 || IsBusy(creature))
                return false;

            var health = creature.Attributes[Attributes.Health];

            if (health.CurrentMax <= 0 || health.Current * 100 >= health.CurrentMax * DevourInFightPercent)
                return false;

            var reach = Math.Max(1f, (float)habit.RangeMax) + ReachSlack;
            var corpse = CorpsesNear(mapChannel, creature, reach).FirstOrDefault(c => IsEdible(mapChannel, creature, c));

            if (corpse == null)
                return false;

            Devour(mapChannel, creature, habit, corpse);
            return true;
        }

        private static void Show(MapChannel mapChannel, Creature creature, CreatureAction habit, Actor target, IEnumerable<Actor> hits)
        {
            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new PerformWindupPacket(PerformType.ThreeArgs, habit.ActionId, habit.ActionArgId, (target ?? creature).EntityId));

            var recovery = new AbilityRecoveryPacket(habit.ActionId, habit.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            foreach (var hit in hits)
                recovery.Hits.Add(new AbilityHit { EntityId = hit.EntityId });

            CellManager.Instance.CellCallMethod(mapChannel, creature, recovery);
        }

        private static void Hold(Creature creature, CreatureAction habit, ActionLevelInfo info)
        {
            BehaviorManager.Instance.StopMoving(creature);
            creature.Controller.Path.Clear();
            Busy(creature, (info?.WindupMs ?? 0) + (info?.RecoveryMs ?? 0));
            habit.CooldownTimer = BehaviorManager.NextCooldown(creature, habit);
        }

        private static void Steal(MapChannel mapChannel, Creature filcher, CreatureAction habit, Creature corpse)
        {
            var info = LevelOf(habit);

            Hold(filcher, habit, info);

            CellManager.Instance.CellCallMethod(mapChannel, filcher,
                new PerformWindupPacket(PerformType.ThreeArgs, habit.ActionId, habit.ActionArgId, corpse.EntityId));

            // The loot goes when the windup is done (CreatureWindups), if it is still there to take.
            CreatureWindups.After(mapChannel, filcher, info?.WindupMs ?? 0, () => TakeLoot(mapChannel, filcher, habit, corpse), habit);
        }

        /// <summary>The Filcher's windup is done: the loot, if nobody has opened the corpse or cleared it meanwhile, and the recovery either way.</summary>
        private static void TakeLoot(MapChannel mapChannel, Creature filcher, CreatureAction habit, Creature corpse)
        {
            var taken = corpse.MapContextId == filcher.MapContextId && IsWorthStealing(mapChannel, corpse);

            if (taken)
                LootDispenserManager.Instance.RemoveForCreature(mapChannel, corpse);

            var recovery = new AbilityRecoveryPacket(habit.ActionId, habit.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            if (taken)
                recovery.Hits.Add(new AbilityHit { EntityId = corpse.EntityId });

            CellManager.Instance.CellCallMethod(mapChannel, filcher, recovery);
        }

        private static void Devour(MapChannel mapChannel, Creature xanx, CreatureAction habit, Creature corpse)
        {
            var info = LevelOf(habit);

            if (info != null)
            {
                var min = info.Get(AbilityProperty.HealAmountMin);
                var max = Math.Max(min, info.Get(AbilityProperty.HealAmountMax, min));
                var amount = AbilityManager.Scale((int)xanx.Level, (min + max) / 2, info.Get(AbilityProperty.DamageScaleType));

                var healed = ActorManager.Instance.Heal(xanx, amount, xanx.EntityId);

                if (healed > 0)
                    ShowHeal(mapChannel, xanx, info, healed);
            }

            // Eaten: nothing left to loot, harvest, revive or eat again, and gone on the next pass.
            LootDispenserManager.Instance.RemoveForCreature(mapChannel, corpse);
            corpse.HarvestAttemptsLeft = 0;
            corpse.Controller.DeadTime = long.MaxValue / 8;

            Hold(xanx, habit, info);
            Show(mapChannel, xanx, habit, corpse, new[] { corpse });
        }

        /// <summary>
        /// What a Xanx regained, over its head and in the combat log: XANX_FORTIFY on it for the
        /// meal - put on quietly, as it has nothing to show, and replacing one from an earlier meal
        /// - then Heal on it. The UpdateHealth that ActorManager.Heal sent already moved its bar.
        /// </summary>
        private static void ShowHeal(MapChannel mapChannel, Creature xanx, ActionLevelInfo info, int healed)
        {
            var fortify = new GameEffect
            {
                TypeId = XanxFortifyTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = info.Level,
                ActionId = info.ActionId,
                SourceId = xanx.EntityId,
                Source = xanx,
                SourceLevel = (int)xanx.Level,
                ExpiresTick = Environment.TickCount64 + Math.Max(1000, info.WindupMs + info.RecoveryMs),
                AnnounceOnAttach = false,
                AnnounceToNewcomers = false
            };

            GameEffectManager.Instance.Attach(mapChannel, xanx, fortify);

            // Attach turns away only debuffs, but a method call to an effect the client lacks is logged as an error there.
            if (xanx.ActiveEffects.ContainsKey(fortify.EffectId))
                CellManager.Instance.CellCallMethod(mapChannel, xanx, new GameEffectHealPacket(fortify.EffectId, healed));
        }

        /// <summary>The Predator's scan: every player in its cone ahead, cloaked ones revealed, and a fight with the first.</summary>
        private static void Scan(MapChannel mapChannel, Creature predator, CreatureAction habit)
        {
            var info = LevelOf(habit);
            var halfAngle = info?.Get(AbilityProperty.ConeRadius, 45) ?? 45;
            var range = Math.Max(1f, (float)habit.RangeMax) + CreatureArea.ConeRangeSlack;
            var facing = AbilityManager.FacingOf(predator);

            var seen = mapChannel.ClientList
                .Select(c => c?.Player)
                .Where(p => p != null && p.State != CharacterState.Dead && p.MapContextId == predator.MapContextId
                    && AbilityManager.InCone(predator.Position, facing, p.Position, range, halfAngle)
                    && TargetCategories.MayFightPlayer(predator.TargetCategory, p.CombatCategory))
                .OrderBy(p => Vector3.DistanceSquared(p.Position, predator.Position))
                .ToList();

            Show(mapChannel, predator, habit, null, seen);
            habit.CooldownTimer = BehaviorManager.NextCooldown(predator, habit);

            foreach (var player in seen.Where(Detection.IsHidden))
                Detection.Reveal(mapChannel, player);

            var first = seen.FirstOrDefault();

            if (first != null)
            {
                predator.Hate.Ensure(first.EntityId, 1);
                BehaviorManager.Instance.SetActionFighting(predator, first.EntityId);
            }
        }
    }
}
