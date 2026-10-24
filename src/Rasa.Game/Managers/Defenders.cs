using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// The defenders of a place: the FRIENDLY creatures the world's spawn pools stand at a post -
    /// the soldiers of a control point's garrison and of the AFS's camps, the officers and vendors
    /// of a base who carry a gun, its turrets. They already go looking for HOSTILE creatures
    /// (TargetCategories.Seeks) and fight what they find with their own attacks. Every creature
    /// noticed enemies out to Creature.AggroRange, 18 m for all of them, and fought alone: a
    /// base's soldier at its post saw nothing of a fight 20 m off at the next post, a turret whose
    /// gun reaches 50 m waited until something was 18 m from it, and a raider shooting one
    /// defender was let be by the rest of the garrison until it came within 18 m of each. And a
    /// defender chased a raider across the map until the 60 m leash took it home. So, with
    /// <see cref="ControlPointDefenceConfig.Defenders"/> on:
    ///  - by role: a defender that walks notices out to its longest attack and
    ///    <see cref="ControlPointDefenceConfig.DefenderNoticeMargin"/> more, never under
    ///    <see cref="ControlPointDefenceConfig.DefenderNoticeRange"/> nor over
    ///    <see cref="ControlPointDefenceConfig.DefenderNoticeMax"/> - a shotgunner 30 m, a rifleman 30,
    ///    a grenadier 34; an AFS turret out to its gun's reach up to
    ///    <see cref="ControlPointDefenceConfig.TurretNoticeMax"/>, never under 18 m (<see cref="NoticeRange"/>,
    ///    given as it spawns: <see cref="Apply"/>). Everything else keeps its 18 m;
    ///  - a fight a defender starts is called to the base's others (<see cref="Call"/>): the defenders and
    ///    turrets of its side within <see cref="ControlPointDefenceConfig.CallRange"/> that are not at
    ///    anything else take the same target, a turret or one that cannot walk only if it can reach it.
    ///    An answer does not call on in turn, so a fight spreads one step and not across the map;
    ///  - a hit on its side's gate shield calls those near it (<see cref="Rally"/>, ControlPointFields);
    ///  - it holds its post: a target that has gone further from the post than the defender reaches - its
    ///    notice range or its longest attack - and <see cref="ControlPointDefenceConfig.HoldPostMargin"/>
    ///    more is let go (<see cref="LetsGo"/>), and when its fight is over one that walked off its post
    ///    walks back to it and faces the way it was set down facing (<see cref="FightOver"/>, <see cref="AtPost"/>).
    /// Mission scenes (their actors, escorts and defenders), minions, scripted creatures, followers,
    /// ambient creatures and private instances keep their own rules.
    ///
    /// BehaviorManager calls in at: the fight's start (TrySetActionFighting: <see cref="FightStarted"/>), the
    /// fight think once the target's position is known (<see cref="LetsGo"/>), GiveUp (<see cref="FightOver"/>)
    /// and a wander that has arrived (<see cref="AtPost"/>). CreatureManager gives the notice range at spawn.
    /// </summary>
    public static class Defenders
    {
        /// <summary>The notice range every creature has unless something says otherwise (Creature.AggroRange).</summary>
        public const float DefaultRange = 18f;

        /// <summary>How close to its post a defender has to be to count as there.</summary>
        public const float PostReachedDistance = 1.0f;

        private static ControlPointDefenceConfig Config => ControlPointFields.Config ?? new ControlPointDefenceConfig();

        [ThreadStatic]
        private static bool _calling;

        /// <summary>Whether this creature is a defender of a place: see the class.</summary>
        public static bool IsDefender(Creature creature)
        {
            if (creature == null || creature.TargetCategory != TargetCategory.Friendly || creature.MasterEntityId != 0 || creature.IsScripted)
                return false;

            var pool = creature.SpawnPool;

            if (pool == null || pool.FollowOwnerCharacterId != 0 || pool.SceneRunId != null || pool.ScenarioKey != null)
                return false;

            if (pool.RuntimeMapChannel?.IsPrivateInstance == true || creature.RuntimeMapChannel?.IsPrivateInstance == true)
                return false;

            var controller = creature.Controller;

            if (controller == null || controller.ActionFollow.FollowTargetId != 0 || controller.ActionFollow.HasAnchor
                || controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove)
                return false;

            if (AmbientCreatures.IsAmbient(creature) || Game.Missions.World.CreatureGameplayRules.IsDefender(creature))
                return false;

            return ReachOf(creature) > 0f;
        }

        /// <summary>The longest reach of an attack the creature has that does damage, 0 when it has none.</summary>
        public static float ReachOf(Creature creature)
        {
            var reach = 0f;

            foreach (var action in creature.Actions)
                if (action.MaxDamage > 0 && action.RangeMax > reach)
                    reach = (float)action.RangeMax;

            return reach;
        }

        /// <summary>Whether a creature cannot walk at all: it fights where it stands.</summary>
        public static bool CannotWalk(Creature creature) => creature.RunSpeed < 0.01f && creature.WalkSpeed < 0.01f;

        /// <summary>
        /// The notice range of a creature of this definition, by its role: a friendly turret its gun's
        /// reach (up to TurretNoticeMax, never under 18 m), a defender that walks its longest attack and
        /// the margin (DefenderNoticeRange to DefenderNoticeMax); anything else <paramref name="current"/>.
        /// </summary>
        public static float NoticeRange(Creature creature, float current, ControlPointDefenceConfig config)
        {
            if (creature == null || config == null || !config.Defenders || creature.TargetCategory != TargetCategory.Friendly)
                return current;

            var reach = ReachOf(creature);

            if (reach <= 0f)
                return current;

            if (Emplacements.Is(creature))
                return Math.Max(current, Math.Max(DefaultRange, Math.Min(reach, config.TurretNoticeMax)));

            if (CannotWalk(creature))
                return current;

            return Math.Max(current, Math.Min(config.DefenderNoticeMax, Math.Max(config.DefenderNoticeRange, reach + config.DefenderNoticeMargin)));
        }

        /// <summary>A creature just made from its template (CreatureManager.CreateCreature): a defender is given its notice range.</summary>
        public static void Apply(Creature creature)
        {
            if (creature?.SpawnPool == null || creature.TargetCategory != TargetCategory.Friendly || creature.MasterEntityId != 0)
                return;

            creature.AggroRange = NoticeRange(creature, creature.AggroRange, Config);
        }

        /// <summary>How far from its post a defender reaches: its notice range or its longest attack, whichever is further.</summary>
        public static float Reach(Creature creature) => Math.Max(creature.AggroRange, ReachOf(creature));

        #region A fight called

        /// <summary>A creature has just started fighting (BehaviorManager.TrySetActionFighting): a defender's fight is called to its base.</summary>
        internal static void FightStarted(Creature creature, ulong targetEntityId)
        {
            if (_calling || creature == null || targetEntityId == 0 || !Config.Defenders)
                return;

            var map = creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId);

            Call(map, creature, targetEntityId);
        }

        /// <summary>
        /// The defender has just started fighting <paramref name="targetEntityId"/>: the others of its
        /// base within CallRange who are free take the same target. Returns how many did.
        /// </summary>
        public static int Call(MapChannel map, Creature caller, ulong targetEntityId)
        {
            var range = Config.CallRange;

            if (_calling || map?.MapCellInfo == null || caller?.Cells == null || targetEntityId == 0 || range <= 0 || !IsDefender(caller))
                return 0;

            _calling = true;

            try
            {
                var answered = 0;
                var target = PositionOf(targetEntityId);

                foreach (var cell in CellManager.CellsIn(map, caller.Cells))
                    foreach (var other in cell.CreatureList.ToArray())
                    {
                        if (other == null || other == caller || other.TargetCategory != caller.TargetCategory || !IsFree(other) || !IsDefender(other))
                            continue;

                        if (Vector3.Distance(caller.Position, other.Position) > range)
                            continue;

                        // A gun and a creature that cannot walk answer only what they can shoot.
                        if ((Emplacements.Is(other) || CannotWalk(other)) && (!target.HasValue || Vector3.Distance(other.Position, target.Value) > Reach(other)))
                            continue;

                        if (BehaviorManager.Instance.TrySetActionFighting(other, targetEntityId))
                            answered++;
                    }

                return answered;
            }
            finally
            {
                _calling = false;
            }
        }

        /// <summary>
        /// Someone has hit something of a side - its gate shield (ControlPointFields.Hit): the creatures of
        /// that side within <paramref name="range"/> of <paramref name="position"/> that can fight have the
        /// attacker on their tables, and those free to turn on it do. Returns how many were told.
        /// </summary>
        public static int Rally(MapChannel map, Vector3 position, TargetCategory side, ulong attackerEntityId, float range)
        {
            if (map?.MapCellInfo == null || attackerEntityId == 0 || range <= 0)
                return 0;

            var cellX = (uint)(position.X / CellManager.CellSize + CellManager.CellBias);
            var cellZ = (uint)(position.Z / CellManager.CellSize + CellManager.CellBias);
            var count = 0;

            _calling = true;

            try
            {
                foreach (var cell in CellManager.CellsIn(map, CellManager.Instance.CreateCellMatrix(map, cellX, cellZ)))
                    foreach (var creature in cell.CreatureList.ToArray())
                    {
                        if (creature == null || creature.TargetCategory != side || creature.Controller == null || creature.Actions.Count == 0
                            || creature.MasterEntityId != 0 || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                            continue;

                        var dx = creature.Position.X - position.X;
                        var dz = creature.Position.Z - position.Z;

                        if (dx * dx + dz * dz > range * range || !Threat.CanFight(creature, attackerEntityId))
                            continue;

                        Threat.Noticed(creature, attackerEntityId);

                        if (IsFree(creature))
                            BehaviorManager.Instance.TrySetActionFighting(creature, attackerEntityId);

                        count++;
                    }
            }
            finally
            {
                _calling = false;
            }

            return count;
        }

        /// <summary>Whether the creature is free to take a fight up: alive, armed, and not at anything else.</summary>
        public static bool IsFree(Creature creature)
        {
            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying || creature.Actions.Count == 0)
                return false;

            if (!creature.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return false;

            var action = creature.Controller?.CurrentAction;

            return action == BehaviorManager.BehaviorActionWander || action == BehaviorManager.BehaviorActionPatrol
                || action == BehaviorManager.BehaviorActionFollowingPath;
        }

        private static Vector3? PositionOf(ulong entityId)
        {
            if (EntityManager.Instance.Players.TryGetValue(entityId, out var player))
                return player.Position;

            return EntityManager.Instance.Creatures.TryGetValue(entityId, out var creature) ? creature.Position : null;
        }

        #endregion

        #region The post held

        /// <summary>
        /// Whether a defender lets its target go (BehaviorManager's fight think): the target stands further
        /// from the post, across the ground, than the defender reaches and HoldPostMargin more.
        /// </summary>
        public static bool LetsGo(Creature creature, Vector3 targetPosition)
        {
            if (!Config.Defenders || creature == null || Emplacements.Is(creature) || !IsDefender(creature))
                return false;

            return AcrossGround(targetPosition, creature.HomePos.Position) > Reach(creature) + Config.HoldPostMargin;
        }

        /// <summary>
        /// The fight is over (BehaviorManager.GiveUp): a defender that has walked off its post walks back
        /// to it; one there, or that cannot walk, is left as it stands. One that holds a pose goes back the
        /// way its pose has it (NpcPoses).
        /// </summary>
        internal static void FightOver(Creature creature)
        {
            if (!Config.Defenders || creature == null || NpcPoses.HoldsPost(creature) || Emplacements.Is(creature) || CannotWalk(creature) || !IsDefender(creature))
                return;

            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            var post = creature.HomePos.Position;

            if (creature.HomePos.MapContextid != creature.MapContextId || AcrossGround(creature.Position, post) <= PostReachedDistance || creature.WalkSpeed < 0.01f)
                return;

            BehaviorManager.Instance.WalkIn(creature, post);
        }

        /// <summary>
        /// A walk in has ended (BehaviorManager's wander): a defender back at its post faces the way it was
        /// set down facing - out over its gate or its sandbags - told to everyone who can see it.
        /// </summary>
        internal static void AtPost(Creature creature)
        {
            if (!Config.Defenders || creature == null || NpcPoses.HoldsPost(creature) || !IsDefender(creature)
                || AcrossGround(creature.Position, creature.HomePos.Position) > PostReachedDistance * 2)
                return;

            var facing = (float)(creature.SpawnPool?.Rotation ?? creature.Rotation);

            var movement = new Models.Movement(creature.Position, 0f, 0x08, new Vector2(facing, 0f));

            creature.LastYaw = facing;
            creature.Controller.LastMovement = movement;
            CellManager.Instance.CellMoveObject(creature, movement);
        }

        private static float AcrossGround(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;

            return MathF.Sqrt(dx * dx + dz * dz);
        }

        #endregion
    }
}
