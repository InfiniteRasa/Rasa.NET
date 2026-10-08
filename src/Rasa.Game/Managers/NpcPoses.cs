using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Models;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// An NPC standing at its post in a pose (Data.NpcPose): a guard with his rifle across his
    /// chest, a clerk at a console, someone sitting on a crate.
    ///
    /// The pose is the pool's (spawnpool_pose) and each creature the pool makes is given it as it
    /// is made, so whoever comes upon it is shown it posed: TOOL_READY and the crouched posture
    /// go out in its ActorInfo, the ambient class in its appearance.
    ///
    /// A posed creature keeps its post. It does not stroll. The pose comes off when it stops
    /// standing idle - a fight, a knockback, a stun, a scripted walk, its death - and it then
    /// looks and acts as any creature does: it stands up, and what it really holds is back in
    /// its hand. When it is idle again it walks back to its post if it has a walk speed and is
    /// more than <see cref="PostReachedDistance"/> away, once, faces the way it was placed, and
    /// takes the pose up again (BehaviorManager.TakePost).
    ///
    /// A pose with the weapon out keeps it out between fights: the fight adds the combat stance
    /// and its end takes only that away (CreatureWeaponDraw).
    ///
    /// An ambient pose is a class in the weapon slot, so the creature gets its own copy of its
    /// appearance - a creature's is otherwise its template's, shared by every one made from it -
    /// and nothing is written to the database.
    /// </summary>
    public static class NpcPoses
    {
        /// <summary>The client's AnimCondForcer_Avatar_* classes: weapon classes with an AMBIENT_* animation code and no model, bar the hand tool's.</summary>
        public const uint LeaningClass = 7641;
        public const uint SittingClass = 7642;
        public const uint LyingDownClass = 9481;
        public const uint AtConsoleClass = 10602;
        public const uint HandToolClass = 10613;

        /// <summary>How near its post counts as on it, across the ground.</summary>
        public const float PostReachedDistance = 0.5f;

        /// <summary>The pose a spawnpool_pose row names; None, and an error logged, for a number that is no pose.</summary>
        public static NpcPose FromRow(uint poolId, byte value)
        {
            if (Enum.IsDefined(typeof(NpcPose), value))
                return (NpcPose)value;

            Logger.WriteLog(LogType.Error, $"SpawnPool {poolId}: pose {value} is not a pose. It has none.");

            return NpcPose.None;
        }

        /// <summary>A pose by its name or number, as a GM types it.</summary>
        public static bool TryParse(string text, out NpcPose pose)
        {
            pose = NpcPose.None;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (byte.TryParse(text, out var number))
            {
                pose = (NpcPose)number;
                return Enum.IsDefined(typeof(NpcPose), number);
            }

            return Enum.TryParse(text.Replace("_", "").Replace("-", ""), true, out pose) && Enum.IsDefined(typeof(NpcPose), pose);
        }

        public static IEnumerable<string> Names => Enum.GetNames(typeof(NpcPose)).Select(name => name.ToLowerInvariant());

        /// <summary>Whether it has a post to keep: it does not stroll, and comes back after a fight.</summary>
        public static bool HoldsPost(Creature creature) => creature != null && creature.Pose != NpcPose.None;

        /// <summary>Whether its pose has its weapon out, which it then keeps out between fights.</summary>
        public static bool KeepsWeaponOut(Creature creature) =>
            creature != null && HasWeaponOut(creature.Pose) && CreatureWeaponDraw.IsArmed(creature);

        public static bool HasWeaponOut(NpcPose pose) => pose == NpcPose.WeaponOut || pose == NpcPose.CrouchedWeaponOut;

        public static bool IsCrouched(NpcPose pose) => pose == NpcPose.Crouched || pose == NpcPose.CrouchedWeaponOut;

        /// <summary>The class an ambient pose puts in the weapon slot; 0 for a pose that is not one.</summary>
        public static uint AmbientClass(NpcPose pose) => pose switch
        {
            NpcPose.Leaning => LeaningClass,
            NpcPose.Sitting => SittingClass,
            NpcPose.LyingDown => LyingDownClass,
            NpcPose.AtConsole => AtConsoleClass,
            NpcPose.HandTool => HandToolClass,
            _ => 0
        };

        /// <summary>Standing idle, where a pose is held: not fighting, walking, following or being run by a scene.</summary>
        public static bool AtRest(Creature creature) =>
            creature?.Controller != null
            && creature.Controller.CurrentAction == BehaviorManager.BehaviorActionWander
            && creature.Controller.ActionWander.State == BehaviorManager.WanderIdle;

        /// <summary>
        /// Gives a creature its pose as it is made, before anyone is shown it. <paramref name="show"/>
        /// false for one that is not at its post yet - off a dropship, through a teleporter - which
        /// takes the pose up when it gets there.
        /// </summary>
        public static void Assign(Creature creature, NpcPose pose, float? postYaw, bool show = true)
        {
            if (creature == null)
                return;

            creature.Pose = pose;
            creature.PostYaw = postYaw;
            creature.PoseShown = false;
            creature.PoseWalkedBack = false;

            if (show && pose != NpcPose.None)
                Put(creature);
        }

        /// <summary>
        /// A GM's change of pose for a creature in the world, kept for as long as the creature
        /// lives and nowhere else. It is shown at once if the creature is standing idle, and when
        /// it next is otherwise. None takes the pose off and lets it stroll again.
        /// </summary>
        public static void Change(MapChannel mapChannel, Creature creature, NpcPose pose)
        {
            if (creature == null)
                return;

            var keptWeaponOut = KeepsWeaponOut(creature);

            Drop(mapChannel, creature);
            creature.Pose = pose;

            // Out for the old pose and not for the new one: away, unless it is fighting with it.
            if (keptWeaponOut && !KeepsWeaponOut(creature))
                CreatureWeaponDraw.Stow(mapChannel, creature);

            if (pose != NpcPose.None && AtRest(creature) && IsAlive(creature))
                Show(mapChannel, creature);
        }

        /// <summary>Puts the pose on a creature in the world and tells everyone who can see it.</summary>
        public static void Show(MapChannel mapChannel, Creature creature)
        {
            if (creature == null || creature.Pose == NpcPose.None || creature.PoseShown)
                return;

            var (weapon, crouch, look) = Put(creature);

            if (mapChannel == null)
                return;

            if (look)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new AppearanceDataPacket(creature.AppearanceData));

            if (weapon)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new WeaponReadyPacket(true));

            if (crouch)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new SetDesiredCrouchStatePacket(CharacterState.Crouched));
        }

        /// <summary>
        /// Back on its post, or as near as its walk got it: it faces the way it was placed and
        /// takes its pose up again.
        /// </summary>
        public static void StandAtPost(MapChannel mapChannel, Creature creature)
        {
            if (creature == null || creature.Pose == NpcPose.None || creature.PoseShown)
                return;

            if (creature.PostYaw is float yaw)
            {
                creature.Rotation = yaw;
                creature.LastYaw = yaw;

                if (mapChannel != null)
                    CellManager.Instance.CellMoveObject(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(yaw, 0f)));
            }

            Show(mapChannel, creature);
        }

        /// <summary>
        /// Takes the pose off a creature that is no longer standing idle at its post: it stands
        /// up, and what it really holds is back in its weapon slot. A weapon the pose had out
        /// stays out (KeepsWeaponOut). The pose is still the creature's, to take up again.
        /// </summary>
        public static void Drop(MapChannel mapChannel, Creature creature)
        {
            if (creature == null)
                return;

            // Whatever took it from its post, it walks back afresh afterwards.
            creature.PoseWalkedBack = false;

            if (!creature.PoseShown)
                return;

            creature.PoseShown = false;

            if (IsCrouched(creature.Pose) && creature.IsCrouching)
            {
                creature.IsCrouching = false;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, creature, new SetDesiredCrouchStatePacket(CharacterState.Standing));
            }

            if (creature.PoseHoldsItsClass)
            {
                // A new dictionary, as Put made one: a packet already queued still holds the old.
                var appearance = new Dictionary<EquipmentData, AppearanceData>(creature.AppearanceData);

                if (creature.PoseHeldWeapon != null)
                    appearance[EquipmentData.Weapon] = creature.PoseHeldWeapon;
                else
                    appearance.Remove(EquipmentData.Weapon);

                creature.AppearanceData = appearance;

                creature.PoseHoldsItsClass = false;
                creature.PoseHeldWeapon = null;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, creature, new AppearanceDataPacket(creature.AppearanceData));
            }
        }

        /// <summary>The pose on the creature itself, with nobody told: what changed, for Show to send.</summary>
        private static (bool Weapon, bool Crouch, bool Look) Put(Creature creature)
        {
            var pose = creature.Pose;
            var weapon = false;
            var crouch = false;
            var look = false;

            creature.PoseShown = true;

            if (HasWeaponOut(pose) && CreatureWeaponDraw.IsArmed(creature) && !creature.WeaponDrawn)
            {
                creature.WeaponDrawn = true;
                weapon = true;
            }

            if (IsCrouched(pose) && !creature.IsCrouching)
            {
                creature.IsCrouching = true;
                crouch = true;
            }

            var ambient = AmbientClass(pose);

            if (ambient != 0)
            {
                // Its own appearance from here on: the template's is every such creature's.
                creature.AppearanceData = creature.AppearanceData == null
                    ? new Dictionary<EquipmentData, AppearanceData>()
                    : new Dictionary<EquipmentData, AppearanceData>(creature.AppearanceData);

                creature.AppearanceData.TryGetValue(EquipmentData.Weapon, out var held);
                creature.PoseHeldWeapon = held;
                creature.PoseHoldsItsClass = true;
                creature.AppearanceData[EquipmentData.Weapon] = new AppearanceData
                {
                    SlotId = EquipmentData.Weapon,
                    Class = ambient,
                    Color = new Color(1),
                    Hue2 = new Color(2139062144)
                };
                look = true;
            }

            return (weapon, crouch, look);
        }

        private static bool IsAlive(Creature creature) =>
            creature.State != CharacterState.Dead && creature.State != CharacterState.Dying;
    }
}
