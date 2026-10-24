using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// The shields in the gates of the control points' outposts. Each is a placed force field - a
    /// world_destructible row PlacedDestructibles puts down, a ForceFields.Field on the map - that
    /// is linked to its point (control_point_link kind 7, Control_point_defences). On the point's own
    /// channel, and only there, it is more than a GM's or a map's field:
    ///  - it is its holder's (<see cref="ControlPointDefenceConfig.FieldsFollowOwner"/>): the AFS's while
    ///    they hold the point, letting players and friendly creatures through and stopping the hostile;
    ///    the Bane's while they do, stopping players and friendly creatures. When the point changes hands
    ///    every one of its shields stands again whole, on the new holder's side (<see cref="HolderChanged"/>);
    ///  - brought down, it stands again whole <see cref="ControlPointDefenceConfig.FieldRebuildSeconds"/>
    ///    later - two minutes, not the 45 to 60 seconds of any other placed object: a raid that breaks a
    ///    gate has the time to get through it, and a defence that loses one has it back once the fight
    ///    has moved on. Our own server first had a destroyed field power up from nothing a minute on,
    ///    which put it back at a few hit points for the next strike to take down again, and nobody
    ///    fighting at the gate ever saw it come back at all;
    ///  - damaged and left alone for <see cref="ControlPointDefenceConfig.FieldRechargeAfterSeconds"/>, it
    ///    powers up again, from nothing to full in <see cref="ControlPointDefenceConfig.FieldRechargeFullSeconds"/>;
    ///  - a fighting creature it stops strikes at it with its own attacks (<see cref="Strike"/>), through the
    ///    ordinary missile path - MissileManager at the field's entity, ForceFields.TakeHit - so the clients
    ///    see it shoot and the hit points go down as any player's shot takes them (who may strike is
    ///    <see cref="StrikesFields"/>, which the raids widen to their raiders);
    ///  - a hit on it by someone it stops calls its side's defenders near it to arms (<see cref="Hit"/>):
    ///    the wiki's "attacking a force field alerts the NPCs of its faction near it to the attacker".
    /// Its going down and standing again are told to whoever listens (<see cref="Broken"/>, <see cref="Rebuilt"/>).
    /// A copy of the map - private or shared - has the rows as ordinary placed fields, on their rows' side.
    /// </summary>
    public static class ControlPointFields
    {
        /// <summary>appsettings.json's ControlPointDefence. Replaced whole when the file changes.</summary>
        public static ControlPointDefenceConfig Config { get; set; } = new ControlPointDefenceConfig();

        /// <summary>The least time between two calls to arms from the same shield.</summary>
        public const long AlertEveryMs = 1500;

        /// <summary>A point's shield has gone down: the shield, its point, and who struck the last blow (0 if nobody known). On the game loop.</summary>
        public static event Action<ForceFields.Field, ControlPoints.Point, ulong> Broken;

        /// <summary>A point's shield stands again whole: rebuilt, or made anew for a new holder. On the game loop.</summary>
        public static event Action<ForceFields.Field, ControlPoints.Point> Rebuilt;

        /// <summary>
        /// Which creatures that a point's shield stops strike at it (<see cref="Strike"/>), when
        /// <see cref="ControlPointDefenceConfig.CreaturesStrikeFields"/> is on: by default
        /// <see cref="DefaultStrikesFields"/>. The raids widen it to their raiders, who are sent at the
        /// gate whether or not they are fighting yet.
        /// </summary>
        public static Func<Creature, bool> StrikesFields { get; set; } = DefaultStrikesFields;

        private sealed class Calls
        {
            public long At;
        }

        private sealed class Charge
        {
            public long At;
        }

        private static readonly ConditionalWeakTable<ForceFields.Field, Calls> LastAlert = new ConditionalWeakTable<ForceFields.Field, Calls>();
        private static readonly ConditionalWeakTable<MapChannel, Charge> LastCharge = new ConditionalWeakTable<MapChannel, Charge>();

        #region The point's shields

        /// <summary>The point of a world_destructible row on this map channel: a point's shield on the point's own channel, or null.</summary>
        public static ControlPoints.Point PointFor(MapChannel mapChannel, uint rowId)
        {
            if (mapChannel?.MapInfo == null || mapChannel.IsCopy || rowId == 0)
                return null;

            var point = ControlPoints.Instance.FieldPointOf(rowId);

            return point != null && !point.IsBattleground && point.MapContextId == mapChannel.MapInfo.MapContextId ? point : null;
        }

        /// <summary>The side a point's shield stands on: its holder's, or with the rule off its row's.</summary>
        public static ForceFields.Side SideFor(ControlPoints.Point point, ForceFields.Side rowSide) =>
            point != null && Config.FieldsFollowOwner ? ControlPoints.SideOf(point.Owner) : rowSide;

        /// <summary>The point's shields on its map's own channel, as they stand now; none until that map is up.</summary>
        public static IReadOnlyList<ForceFields.Field> Of(ControlPoints.Point point) => LivesOf(point).Select(live => live.Field).ToList();

        public static IReadOnlyList<ForceFields.Field> Of(uint controlPointId) => Of(ControlPoints.Instance.ById(controlPointId));

        /// <summary>Whether at least one of the point's shields stands.</summary>
        public static bool AnyUp(ControlPoints.Point point) => Of(point).Any(field => !field.IsDestroyed);

        /// <summary>Whether every one of the point's shields stands: true for a point with none.</summary>
        public static bool AllUp(ControlPoints.Point point) => Of(point).All(field => !field.IsDestroyed);

        /// <summary>The point whose gate the field closes, or null for a field of no point.</summary>
        public static ControlPoints.Point PointOf(ForceFields.Field field) =>
            field == null || field.ControlPointId == 0 ? null : ControlPoints.Instance.ById(field.ControlPointId);

        /// <summary>How long until a point's shield that is down stands again, in ms; 0 while it stands.</summary>
        public static long RebuildsInMs(ForceFields.Field field)
        {
            if (field == null || !field.IsDestroyed)
                return 0;

            var live = PlacedDestructibles.LiveOf(field.Object);

            if (live == null || live.RespawnAt == 0)
                return RebuildMsFor(field) ?? PlacedDestructibles.RespawnMaxMs;

            return Math.Max(0, live.RespawnAt - PlacedDestructibles.Now());
        }

        /// <summary>
        /// How long a destroyed field stays down (PlacedDestructibles.Worker): a point's
        /// <see cref="ControlPointDefenceConfig.FieldRebuildSeconds"/>, or null for the roll every
        /// other placed object has.
        /// </summary>
        public static long? RebuildMsFor(ForceFields.Field field)
        {
            if (field == null || field.ControlPointId == 0 || Config.FieldRebuildSeconds <= 0)
                return null;

            return Config.FieldRebuildSeconds * 1000L;
        }

        /// <summary>
        /// The gate a field closes, as whoever comes to it meets it: the middle of its sheet (a field's
        /// own origin is not always its middle: the Bane's minor entrance hangs to one side of it) and the
        /// level direction straight through it. Which way is out is the asker's to say.
        /// </summary>
        public static (Vector3 Middle, Vector3 Through) Opening(ForceFields.Field field)
        {
            var turn = Quaternion.CreateFromYawPitchRoll(field.Yaw, 0f, 0f);
            var middle = new Vector3((field.Class.Min.X + field.Class.Max.X) * 0.5f, 0f, (field.Class.Min.Z + field.Class.Max.Z) * 0.5f);

            return (field.Position + Vector3.Transform(middle, turn), Vector3.Transform(Vector3.UnitZ, turn));
        }

        private static IEnumerable<PlacedDestructibles.Live> LivesOf(ControlPoints.Point point)
        {
            var map = point?.Map;

            if (map == null || point.FieldRows.Count == 0)
                return Enumerable.Empty<PlacedDestructibles.Live>();

            return PlacedDestructibles.On(map).Where(live => live.Field != null && !live.Placement.IsPutDown && point.FieldRows.Contains(live.Placement.Id));
        }

        #endregion

        #region Changing hands, going down, standing again

        /// <summary>
        /// The point has changed hands (ControlPoints.SetHolder): each of its shields stands again whole,
        /// on the new holder's side, and everyone meets it anew. One that was down is told as rebuilt.
        /// </summary>
        public static int HolderChanged(ControlPoints.Point point)
        {
            if (point == null || !Config.FieldsFollowOwner)
                return 0;

            var side = ControlPoints.SideOf(point.Owner);
            var count = 0;

            foreach (var live in LivesOf(point).ToList())
            {
                var field = live.Field;
                var wasDown = field.IsDestroyed;

                ForceFields.Reset(field, side);

                // Moved means a new object: the placement keeps the one on the map.
                live.Object = field.Object;
                live.RespawnAt = 0;
                count++;

                if (wasDown)
                    Raise(Rebuilt, field, point);
            }

            return count;
        }

        /// <summary>A point's shield has changed state (ForceFields.UpdateState): down, or standing again, is told.</summary>
        internal static void StateChanged(ForceFields.Field field, UseObjectState before, ulong byEntityId)
        {
            var point = PointOf(field);

            if (point == null)
                return;

            var wasDown = before == ForceFields.StateOf(field.Class.Kind, field.Side, 0, field.MaxHealth);

            if (!wasDown && field.IsDestroyed)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): gate shield {field.Id} is down, by {byEntityId}; back in {RebuildMsFor(field) ?? PlacedDestructibles.RespawnMaxMs} ms.");

                var broken = Broken;

                if (broken != null)
                    foreach (Action<ForceFields.Field, ControlPoints.Point, ulong> handler in broken.GetInvocationList())
                        try
                        {
                            handler(field, point, byEntityId);
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLog(LogType.Error, $"ControlPointFields.Broken handler failed: {e.Message}");
                        }
            }
            else if (wasDown && !field.IsDestroyed)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): gate shield {field.Id} stands again.");
                Raise(Rebuilt, field, point);
            }
        }

        private static void Raise(Action<ForceFields.Field, ControlPoints.Point> handlers, ForceFields.Field field, ControlPoints.Point point)
        {
            if (handlers == null)
                return;

            foreach (Action<ForceFields.Field, ControlPoints.Point> handler in handlers.GetInvocationList())
                try
                {
                    handler(field, point);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"ControlPointFields.Rebuilt handler failed: {e.Message}");
                }
        }

        /// <summary>
        /// From the map channel worker (ControlPoints.Worker): the point shields on the map that are damaged
        /// and have been left alone power up, by the time since the last pass.
        /// </summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            var now = ForceFields.Clock();
            var charge = LastCharge.GetOrCreateValue(mapChannel);
            var elapsed = charge.At == 0 ? 0 : now - charge.At;

            charge.At = now;

            if (elapsed <= 0)
                return;

            foreach (var field in ForceFields.OnMap(mapChannel))
                if (field.ControlPointId != 0)
                    Recharge(field, now, elapsed);
        }

        /// <summary>
        /// A point's damaged shield powers up, <paramref name="elapsedMs"/> worth, once nothing has hit it
        /// for <see cref="ControlPointDefenceConfig.FieldRechargeAfterSeconds"/>. What it gained.
        /// </summary>
        public static int Recharge(ForceFields.Field field, long now, long elapsedMs)
        {
            var after = Config.FieldRechargeAfterSeconds * 1000L;
            var full = Math.Max(1, Config.FieldRechargeFullSeconds) * 1000L;

            if (field == null || field.IsDestroyed || after <= 0 || elapsedMs <= 0 || field.Health >= field.MaxHealth)
                return 0;

            if (field.LastDamagedAt != 0 && now - field.LastDamagedAt < after)
                return 0;

            return ForceFields.Recharge(field, (int)Math.Max(1, (long)field.MaxHealth * elapsedMs / full));
        }

        #endregion

        #region Under attack

        /// <summary>
        /// Someone the shield stops has hit it (ForceFields.TakeHit): the defenders of its side near it -
        /// within <see cref="ControlPointDefenceConfig.FieldAlertRange"/> - have the attacker on their
        /// tables, and those not fighting already turn on it (Defenders.Rally). At most every
        /// <see cref="AlertEveryMs"/>. Returns how many were called.
        /// </summary>
        public static int Hit(ForceFields.Field field, Actor attacker, long now = 0)
        {
            if (field?.MapChannel == null || attacker == null || Config.FieldAlertRange <= 0)
                return 0;

            now = now != 0 ? now : ForceFields.Clock();

            var calls = LastAlert.GetOrCreateValue(field);

            if (calls.At != 0 && now - calls.At < AlertEveryMs)
                return 0;

            calls.At = now;

            var side = field.Side == ForceFields.Side.A ? TargetCategory.Friendly : TargetCategory.Hostile;
            var count = Defenders.Rally(field.MapChannel, field.Position, side, attacker.EntityId, Config.FieldAlertRange);

            if (count > 0)
                Logger.WriteLog(LogType.Debug, $"Gate shield {field.Id} of control point {field.ControlPointId} is hit by {attacker.EntityId}: {count} of its side called to arms.");

            return count;
        }

        /// <summary>Who strikes at a point's shield that stops it, by default: a creature of no player's that is fighting.</summary>
        public static bool DefaultStrikesFields(Creature creature) =>
            creature != null && creature.MasterEntityId == 0 && !Emplacements.Is(creature)
            && creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting;

        /// <summary>
        /// A creature's step was stopped by a field (BehaviorManager.UpdateEntityMovement): at a point's
        /// shield, one that <see cref="StrikesFields"/> says strikes at it does (<see cref="Strike"/>).
        /// </summary>
        internal static void Blocked(MapChannel mapChannel, Creature creature, ForceFields.Field field)
        {
            if (field == null || field.ControlPointId == 0 || !Config.CreaturesStrikeFields)
                return;

            var strikes = StrikesFields ?? DefaultStrikesFields;

            if (strikes(creature))
                Strike(mapChannel, creature, field);
        }

        /// <summary>
        /// The creature strikes at the shield with its best attack that is ready: the field's entity is the
        /// target of an ordinary missile (MissileManager), whose damage ForceFields.TakeHit puts on it, and
        /// the attack's cooldown runs as if it had been at its enemy. True if it struck.
        /// </summary>
        public static bool Strike(MapChannel mapChannel, Creature creature, ForceFields.Field field)
        {
            if (mapChannel == null || creature?.Controller == null || field?.Object == null || field.IsDestroyed
                || field.MapChannel != mapChannel || !ForceFields.MayShoot(field, creature))
                return false;

            var action = AttackFor(creature);

            if (action == null)
                return false;

            var damage = (int)(action.MinDamage + Random.Shared.Next() % (action.MaxDamage - action.MinDamage + 1));

            damage = GameEffectManager.ApplyDamageDealt(creature, damage);

            MissileManager.Instance.MissileLaunch(mapChannel, new ActionData(creature, action.ActionId, action.ActionArgId, field.Object.EntityId, 0), damage,
                CreatureAttacks.ArmorPierceOf(action), damageType: CreatureAttacks.DamageTypeOf(action), melee: CreatureAttacks.IsMelee(action), creatureAction: action);

            action.CooldownTimer = BehaviorManager.NextCooldown(creature, action);

            // Striking is pressing its fight: it is not stalled at the gate (BehaviorManager.Stalled).
            BehaviorManager.MadeProgress(creature);

            return true;
        }

        /// <summary>
        /// The attack a creature strikes a shield with: of its ready attacks that land a hit - not a heal,
        /// a habit, a buff, a summons, a bomb, a charge or the like - the one that does the most.
        /// </summary>
        public static CreatureAction AttackFor(Creature creature)
        {
            CreatureAction best = null;

            foreach (var action in creature.Actions)
            {
                if (action.CooldownTimer > 0 || action.MaxDamage <= 0 || action.MaxDamage < action.MinDamage)
                    continue;

                if (CreatureSupport.Is(action) || CreatureHabits.Is(action) || action.ActionId == ShieldDrone.HealAction || CreatureBuffs.Is(action)
                    || CreatureDebuffs.Is(action) || CreatureSummons.IsSummon(action) || CreatureBombs.IsOvulate(action) || CreatureBombs.IsCorpseExplosion(action)
                    || CreatureBombs.IsSelfDestruct(action) || CreatureMiasma.Is(action) || AmoeboidVomit.IsVomit(action) || KaelRushingBlow.Is(action))
                    continue;

                if (!CreatureEffectAttacks.Hits(CreatureEffectAttacks.KindOf(action.ActionId, action.ActionArgId)))
                    continue;

                if (best == null || action.MinDamage + action.MaxDamage > best.MinDamage + best.MaxDamage)
                    best = action;
            }

            return best;
        }

        #endregion
    }
}
