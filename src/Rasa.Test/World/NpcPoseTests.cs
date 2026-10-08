using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// An NPC at a post in a pose (NpcPoses): what the pose is on the creature and in what a
    /// client is sent, that it keeps its post, and that it comes off for a fight and goes back
    /// on afterwards. And the pools' poses (spawnpool_pose) with the NPCs seeded with one.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NpcPoseTests
    {
        private const uint Rifle = 27220;
        private const float PostYaw = 1.25f;

        #region The pose on the creature

        [TestMethod]
        public void StandingChangesNothingAboutHowItLooks()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(guard, NpcPose.Standing, PostYaw);

            Assert.IsTrue(guard.PoseShown);
            Assert.IsTrue(NpcPoses.HoldsPost(guard));
            Assert.IsFalse(guard.WeaponDrawn);
            Assert.IsFalse(guard.IsCrouching);
            Assert.AreEqual(Rifle, guard.AppearanceData[EquipmentData.Weapon].Class);

            var info = new ActorInfoPacket(guard);

            CollectionAssert.DoesNotContain(info.StateIds, CharacterState.ToolReady);
            Assert.AreEqual(CharacterState.Standing, info.DesiredPostureId);
            Assert.IsFalse(info.CombatMode);
        }

        [TestMethod]
        public void WeaponOutIsToolReadyWithNoCombatStance()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(guard, NpcPose.WeaponOut, PostYaw);

            Assert.IsTrue(guard.WeaponDrawn);
            Assert.IsFalse(guard.InCombatMode);

            // What a player who comes upon it is given: the rifle out, and at peace.
            var info = new ActorInfoPacket(guard);

            CollectionAssert.Contains(info.StateIds, CharacterState.ToolReady);
            Assert.IsFalse(info.CombatMode);
            Assert.AreEqual(CharacterState.Standing, info.DesiredPostureId);
        }

        [TestMethod]
        public void WeaponOutIsNothingForACreatureWithNoWeapon()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10);

            NpcPoses.Assign(guard, NpcPose.WeaponOut, PostYaw);

            Assert.IsTrue(guard.PoseShown, "It still keeps its post.");
            Assert.IsFalse(guard.WeaponDrawn);
            Assert.IsFalse(NpcPoses.KeepsWeaponOut(guard));
        }

        [TestMethod]
        [DataRow(NpcPose.Crouched, false)]
        [DataRow(NpcPose.CrouchedWeaponOut, true)]
        public void CrouchedIsTheCrouchedPosture(NpcPose pose, bool weaponOut)
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(guard, pose, PostYaw);

            Assert.IsTrue(guard.IsCrouching);
            Assert.AreEqual(weaponOut, guard.WeaponDrawn);
            Assert.AreEqual(CharacterState.Crouched, new ActorInfoPacket(guard).DesiredPostureId);
        }

        [TestMethod]
        [DataRow(NpcPose.Leaning, 7641u)]
        [DataRow(NpcPose.Sitting, 7642u)]
        [DataRow(NpcPose.LyingDown, 9481u)]
        [DataRow(NpcPose.AtConsole, 10602u)]
        [DataRow(NpcPose.HandTool, 10613u)]
        public void AnAmbientPoseIsItsClassInTheWeaponSlotOfTheCreaturesOwnAppearance(NpcPose pose, uint forcer)
        {
            using var world = new WorldTestContext();
            var clerk = Spawn(world, x: 10, weapon: Rifle);

            // As a creature made from a template has it: the template's own dictionary.
            var template = clerk.AppearanceData;

            NpcPoses.Assign(clerk, pose, PostYaw);

            Assert.AreEqual(forcer, NpcPoses.AmbientClass(pose));
            Assert.AreEqual(forcer, clerk.AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(EquipmentData.Weapon, clerk.AppearanceData[EquipmentData.Weapon].SlotId);
            Assert.AreNotSame(template, clerk.AppearanceData);
            Assert.AreEqual(Rifle, template[EquipmentData.Weapon].Class, "Every other creature of the template keeps its rifle.");
            Assert.IsFalse(clerk.WeaponDrawn);
            Assert.IsFalse(clerk.IsCrouching);

            NpcPoses.Drop(world.Map, clerk);

            Assert.AreEqual(Rifle, clerk.AppearanceData[EquipmentData.Weapon].Class, "What it really holds is back.");
            Assert.IsFalse(clerk.PoseShown);
            Assert.AreEqual(pose, clerk.Pose, "The pose is still its own, to take up again.");
        }

        [TestMethod]
        public void AnAmbientPoseOnAnUnarmedCreatureLeavesItUnarmedWhenItComesOff()
        {
            using var world = new WorldTestContext();
            var clerk = Spawn(world, x: 10);

            NpcPoses.Assign(clerk, NpcPose.Sitting, PostYaw);
            Assert.AreEqual(NpcPoses.SittingClass, clerk.AppearanceData[EquipmentData.Weapon].Class);

            NpcPoses.Drop(world.Map, clerk);

            Assert.IsFalse(clerk.AppearanceData.ContainsKey(EquipmentData.Weapon));
        }

        [TestMethod]
        public void OneNotAtItsPostYetIsGivenThePoseButNotShownIt()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(guard, NpcPose.CrouchedWeaponOut, PostYaw, show: false);

            Assert.AreEqual(NpcPose.CrouchedWeaponOut, guard.Pose);
            Assert.IsFalse(guard.PoseShown);
            Assert.IsFalse(guard.IsCrouching);
            Assert.IsFalse(guard.WeaponDrawn);
        }

        [TestMethod]
        public void ShowingAPoseTellsWhoeverCanSeeTheCreatureOnlyWhatChanged()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var guard = Spawn(world, x: 10, weapon: Rifle);

            guard.Pose = NpcPose.CrouchedWeaponOut;
            NpcPoses.Show(world.Map, guard);

            var sent = Sent(watcher);

            Assert.IsTrue(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.AreEqual(CharacterState.Crouched, sent.OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState);
            Assert.AreEqual(0, sent.OfType<AppearanceDataPacket>().Count());
            Assert.AreEqual(0, sent.OfType<RequestVisualCombatModePacket>().Count());

            // Shown already: nothing more.
            NpcPoses.Show(world.Map, guard);
            Assert.AreEqual(0, Sent(watcher).Length);

            var clerk = Spawn(world, x: 12, weapon: Rifle);

            clerk.Pose = NpcPose.Sitting;
            NpcPoses.Show(world.Map, clerk);

            sent = Sent(watcher);

            Assert.AreEqual(NpcPoses.SittingClass, sent.OfType<AppearanceDataPacket>().Single().AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(0, sent.OfType<WeaponReadyPacket>().Count());
            Assert.AreEqual(0, sent.OfType<SetDesiredCrouchStatePacket>().Count());
        }

        #endregion

        #region Keeping the post

        [TestMethod]
        public void APosedNpcDoesNotStrollAndAnUnposedOneDoes()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 60);
            var posted = Spawn(world, x: 10, walkSpeed: 5);
            var loose = Spawn(world, x: 30, walkSpeed: 5);

            NpcPoses.Assign(posted, NpcPose.Standing, PostYaw);
            Drain(watcher);

            Think(world, BehaviorManager.WanderIntervalMs * 3);

            var moves = Moves(watcher);

            Assert.AreEqual(new Vector3(10, 0, 0), posted.Position);
            Assert.AreEqual(0, moves.Count(move => move.EntityId == posted.EntityId));
            Assert.IsTrue(posted.PoseShown);
            Assert.AreNotEqual(0, moves.Count(move => move.EntityId == loose.EntityId), "One with no pose sets off as before.");
        }

        [TestMethod]
        public void AFightTakesThePoseOffAndTheNpcWalksBackFacesItsWayAndTakesItUpAgain()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var guard = Spawn(world, x: 10, weapon: Rifle, walkSpeed: 5);

            NpcPoses.Assign(guard, NpcPose.Crouched, PostYaw);
            Drain(watcher);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(guard, watcher.Player.EntityId));

            var sent = Sent(watcher);

            Assert.IsFalse(guard.PoseShown);
            Assert.IsFalse(guard.IsCrouching, "It stands up to fight.");
            Assert.AreEqual(CharacterState.Standing, sent.OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState);
            Assert.IsTrue(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsTrue(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);

            // The fight carried it off its post, and is over.
            guard.Position = new Vector3(16, 0, 0);
            BehaviorManager.Instance.StopFighting(guard);
            guard.TargetCategory = TargetCategory.Friendly;
            Drain(watcher);

            Think(world, 5000);

            Assert.IsTrue(guard.PoseShown);
            Assert.IsTrue(guard.IsCrouching);
            Assert.IsLessThanOrEqualTo(NpcPoses.PostReachedDistance, Vector3.Distance(guard.Position, new Vector3(10, 0, 0)));
            Assert.AreEqual(PostYaw, (float)guard.Rotation);
            Assert.AreEqual(PostYaw, guard.LastYaw);

            var packets = WorldTestContext.Drain(watcher).Select(packet => packet.Message).ToList();
            var moves = packets.OfType<MoveObjectMessage>().Where(move => move.EntityId == guard.EntityId).Select(move => move.Movement).ToList();

            // The last thing said of where it is: standing on its post, facing the way it was placed.
            Assert.AreEqual(PostYaw, moves.Last().ViewDirection.X);
            Assert.AreEqual(0f, moves.Last().Velocity);
            Assert.AreEqual(CharacterState.Crouched,
                packets.OfType<CallMethodMessage>().Select(message => message.Packet).OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState);
        }

        [TestMethod]
        public void OneThatCannotWalkTakesItsPoseUpWhereItStands()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var clerk = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(clerk, NpcPose.Sitting, PostYaw);
            NpcPoses.Drop(world.Map, clerk);
            clerk.Position = new Vector3(13, 0, 0);
            Drain(watcher);

            Think(world, 500);

            Assert.IsTrue(clerk.PoseShown);
            Assert.AreEqual(new Vector3(13, 0, 0), clerk.Position);
            Assert.AreEqual(NpcPoses.SittingClass, clerk.AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(PostYaw, Moves(watcher).Single(move => move.EntityId == clerk.EntityId).Movement.ViewDirection.X);
        }

        [TestMethod]
        public void AWalkBackIsTriedOnceAndThePoseIsTakenUpWhereItEnded()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, walkSpeed: 5);

            NpcPoses.Assign(guard, NpcPose.Standing, PostYaw);
            NpcPoses.Drop(world.Map, guard);

            // It has walked back already and is still off its post - there was no way there.
            guard.Position = new Vector3(20, 0, 0);
            guard.PoseWalkedBack = true;

            Think(world, 250);

            Assert.IsTrue(guard.PoseShown);
            Assert.AreEqual(new Vector3(20, 0, 0), guard.Position);

            // Taken from its post again, it walks back afresh.
            NpcPoses.Drop(world.Map, guard);
            Assert.IsFalse(guard.PoseWalkedBack);
        }

        [TestMethod]
        public void OneThatWasNeverPlacedOnThisMapTakesItsPoseUpWhereItStands()
        {
            using var world = new WorldTestContext();
            var guard = Spawn(world, x: 10, walkSpeed: 5);

            // A creature a GM posed, with no spawn point of its own here.
            guard.HomePos.Position = Vector3.Zero;
            guard.HomePos.MapContextid = 0;
            NpcPoses.Assign(guard, NpcPose.Standing, null, show: false);

            Think(world, 1000);

            Assert.IsTrue(guard.PoseShown);
            Assert.AreEqual(new Vector3(10, 0, 0), guard.Position, "It does not walk to the map's origin.");
        }

        [TestMethod]
        public void AnNpcWithNoFacingOfItsPostIsNotTurned()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var guard = Spawn(world, x: 10);

            NpcPoses.Assign(guard, NpcPose.Standing, null, show: false);
            guard.Rotation = 2.5;
            Drain(watcher);

            Think(world, 250);

            Assert.IsTrue(guard.PoseShown);
            Assert.AreEqual(2.5, guard.Rotation);
            Assert.AreEqual(0, Moves(watcher).Count);
        }

        [TestMethod]
        public void TheDeadAndTheBusyHoldNoPose()
        {
            using var world = new WorldTestContext();
            var dead = Spawn(world, x: 10, weapon: Rifle);
            var dying = Spawn(world, x: 12, weapon: Rifle);
            var busy = Spawn(world, x: 14, weapon: Rifle);

            NpcPoses.Assign(dead, NpcPose.Sitting, PostYaw);
            NpcPoses.Assign(dying, NpcPose.Leaning, PostYaw);
            NpcPoses.Assign(busy, NpcPose.Crouched, PostYaw);

            dead.State = CharacterState.Dead;
            dead.Attributes[Attributes.Health].Current = 0;
            dying.State = CharacterState.Dying;

            // Run by something other than its idling: a scene's walk.
            busy.Controller.CurrentAction = BehaviorManager.BehaviorActionFollow;

            Think(world, 250);

            Assert.IsFalse(dead.PoseShown);
            Assert.AreEqual(Rifle, dead.AppearanceData[EquipmentData.Weapon].Class);
            Assert.IsFalse(dying.PoseShown);
            Assert.AreEqual(Rifle, dying.AppearanceData[EquipmentData.Weapon].Class);
            Assert.IsFalse(busy.PoseShown, "Anything but standing idle takes it off.");
            Assert.IsFalse(busy.IsCrouching);
        }

        [TestMethod]
        public void AKnockbackTakesThePoseOffAndItIsTakenUpAgainOnceTheStunIsOver()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 0);
            var clerk = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(clerk, NpcPose.Sitting, PostYaw);

            Assert.IsTrue(CrowdControl.Knockback(world.Map, clerk, attacker.Player, 6f, CrowdControl.KnockbackTypeId, DamageType.Physical), "knocked back");
            clerk.TargetCategory = TargetCategory.Friendly;

            Think(world, 250);

            Assert.IsFalse(clerk.PoseShown, "Thrown, it is not sitting.");
            Assert.AreEqual(Rifle, clerk.AppearanceData[EquipmentData.Weapon].Class);

            // Still down: nothing yet.
            Think(world, 1000);
            Assert.IsFalse(clerk.PoseShown);

            // The stun runs out (the effects' own worker, which is not running here).
            clerk.ActiveEffects.Clear();
            Think(world, 500);

            Assert.IsTrue(clerk.PoseShown);
            Assert.AreEqual(NpcPoses.SittingClass, clerk.AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(PostYaw, clerk.LastYaw, "It cannot walk back, and faces its way where it landed.");
        }

        #endregion

        #region A weapon the pose has out

        [TestMethod]
        public void AFightAddsTheStanceToAWeaponThePoseHasOutAndItsEndTakesOnlyTheStanceAway()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Assign(guard, NpcPose.WeaponOut, PostYaw);
            Drain(watcher);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(guard, watcher.Player.EntityId));

            var sent = Sent(watcher);

            Assert.AreEqual(0, sent.OfType<WeaponReadyPacket>().Count(), "It is out already.");
            Assert.IsTrue(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.IsTrue(guard.InCombatMode);

            // A change of target within the fight says nothing more.
            CreatureWeaponDraw.Draw(world.Map, guard);
            Assert.AreEqual(0, Sent(watcher).Length);

            BehaviorManager.Instance.StopFighting(guard);
            sent = Sent(watcher);

            Assert.AreEqual(0, sent.OfType<WeaponReadyPacket>().Count(), "It stays out.");
            Assert.IsFalse(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.IsTrue(guard.WeaponDrawn);
            Assert.IsFalse(guard.InCombatMode);

            // And with no stance to drop, putting away is nothing at all.
            CreatureWeaponDraw.Stow(world.Map, guard);
            Assert.AreEqual(0, Sent(watcher).Length);
            Assert.IsTrue(guard.WeaponDrawn);
        }

        [TestMethod]
        public void ACreatureWithNoPoseDrawsAndPutsAwayAsBefore()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var soldier = Spawn(world, x: 10, weapon: Rifle);

            CreatureWeaponDraw.Draw(world.Map, soldier);

            var sent = Sent(watcher);

            Assert.IsTrue(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsTrue(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);

            CreatureWeaponDraw.Draw(world.Map, soldier);
            Assert.AreEqual(0, Sent(watcher).Length);

            CreatureWeaponDraw.Stow(world.Map, soldier);
            sent = Sent(watcher);

            Assert.IsFalse(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsFalse(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.IsFalse(soldier.WeaponDrawn);
        }

        #endregion

        #region A game master's change

        [TestMethod]
        public void APoseIsReadByNameOrNumber()
        {
            Assert.IsTrue(NpcPoses.TryParse("weaponout", out var pose));
            Assert.AreEqual(NpcPose.WeaponOut, pose);
            Assert.IsTrue(NpcPoses.TryParse("Lying_Down", out pose));
            Assert.AreEqual(NpcPose.LyingDown, pose);
            Assert.IsTrue(NpcPoses.TryParse("6", out pose));
            Assert.AreEqual(NpcPose.Sitting, pose);
            Assert.IsTrue(NpcPoses.TryParse("none", out pose));
            Assert.AreEqual(NpcPose.None, pose);

            Assert.IsFalse(NpcPoses.TryParse("10", out _));
            Assert.IsFalse(NpcPoses.TryParse("kneeling", out _));
            Assert.IsFalse(NpcPoses.TryParse("", out _));

            CollectionAssert.AreEqual(
                new[] { "none", "standing", "weaponout", "crouched", "crouchedweaponout", "leaning", "sitting", "lyingdown", "atconsole", "handtool" },
                NpcPoses.Names.ToArray());
        }

        [TestMethod]
        public void AChangeOfPoseIsShownAtOnceAndNoneLetsTheWeaponGo()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var guard = Spawn(world, x: 10, weapon: Rifle);

            NpcPoses.Change(world.Map, guard, NpcPose.WeaponOut);

            Assert.IsTrue(guard.PoseShown);
            Assert.IsTrue(Sent(watcher).OfType<WeaponReadyPacket>().Single().WeaponReady);

            NpcPoses.Change(world.Map, guard, NpcPose.Sitting);

            var sent = Sent(watcher);

            Assert.IsFalse(sent.OfType<WeaponReadyPacket>().Single().WeaponReady, "The new pose does not have it out.");
            Assert.AreEqual(NpcPoses.SittingClass, sent.OfType<AppearanceDataPacket>().Last().AppearanceData[EquipmentData.Weapon].Class);

            NpcPoses.Change(world.Map, guard, NpcPose.None);

            Assert.IsFalse(NpcPoses.HoldsPost(guard));
            Assert.IsFalse(guard.PoseShown);
            Assert.AreEqual(Rifle, guard.AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(Rifle, Sent(watcher).OfType<AppearanceDataPacket>().Single().AppearanceData[EquipmentData.Weapon].Class);
        }

        #endregion

        #region The pool's pose

        [TestMethod]
        public void ARowsPoseIsThePoseOrNoneForANumberThatIsNoPose()
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());

            Assert.AreEqual(NpcPose.None, NpcPoses.FromRow(1, 0));
            Assert.AreEqual(NpcPose.WeaponOut, NpcPoses.FromRow(1, 2));
            Assert.AreEqual(NpcPose.HandTool, NpcPoses.FromRow(1, 9));
            Assert.AreEqual(NpcPose.None, NpcPoses.FromRow(1, 10));
            Assert.AreEqual(NpcPose.None, NpcPoses.FromRow(1, 255));

            // The numbers are in the database: they do not move.
            Assert.AreEqual(1, (int)NpcPose.Standing);
            Assert.AreEqual(2, (int)NpcPose.WeaponOut);
            Assert.AreEqual(3, (int)NpcPose.Crouched);
            Assert.AreEqual(4, (int)NpcPose.CrouchedWeaponOut);
            Assert.AreEqual(5, (int)NpcPose.Leaning);
            Assert.AreEqual(6, (int)NpcPose.Sitting);
            Assert.AreEqual(7, (int)NpcPose.LyingDown);
            Assert.AreEqual(8, (int)NpcPose.AtConsole);
            Assert.AreEqual(9, (int)NpcPose.HandTool);
            Assert.AreEqual(BootcampPostedNpcs.Standing, (byte)NpcPose.Standing);
            Assert.AreEqual(BootcampPostedNpcs.WeaponOut, (byte)NpcPose.WeaponOut);
        }

        [TestMethod]
        public void TheSeedIsThePostedNpcsOfTheProvingGroundsAndNoOtherPoolHasAPose()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                    // As they were seeded: Add_bootcamp_ambient_guards, after, put figures in their place.
                    context.GetService<IMigrator>().Migrate(BootcampAmbientGuardTests.Before);
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var poses = new Rasa.Repositories.World.SpawnpoolRepository(context).GetPoses().OrderBy(row => row.Id).ToList();

                    // The four of the first lot, and the six riflemen of the second (BootcampGarrisonNpcs).
                    CollectionAssert.AreEqual(new uint[] { 400001, 400002, 400003, 400004, 400101, 400102, 400103, 400104, 400105, 400106 }, poses.Select(row => row.Id).ToArray());
                    CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 2, 2, 2, 2, 2, 2, 2 }, poses.Select(row => row.Pose).ToArray());

                    var pools = context.SpawnPoolEntries.AsNoTracking().ToList();
                    var posed = pools.Where(pool => poses.Any(row => row.Id == pool.Id)).OrderBy(pool => pool.Id).ToList();

                    Assert.AreEqual(10, posed.Count, "Each pose is of a pool there is.");
                    CollectionAssert.AreEqual(new uint[] { 400001, 400002, 400003, 400003, 400003, 400003, 400003, 400003, 400003, 400003 }, posed.Select(pool => pool.Creature1Id).ToArray());

                    foreach (var pool in posed)
                    {
                        Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, pool.MapContextId);
                        Assert.AreEqual((byte)1, pool.Creature1MinCount);
                        Assert.AreEqual((byte)1, pool.Creature1MaxCount);
                        Assert.AreEqual(0u, pool.Creature2Id);
                        Assert.AreEqual(0.0, pool.Radius);
                        Assert.AreEqual((byte)0, pool.Mode);
                    }

                    // Where the game master stood and faced.
                    Assert.AreEqual(404.0625, posed[0].PosX, 0.0001);
                    Assert.AreEqual(120.7656, posed[0].PosY, 0.0001);
                    Assert.AreEqual(110.1328, posed[0].PosZ, 0.0001);
                    Assert.AreEqual(0.6549, posed[0].Rotation, 0.0001);
                    Assert.AreEqual(5.892, posed[1].Rotation, 0.0001);
                    Assert.AreEqual(6.258, posed[2].Rotation, 0.0001);
                    Assert.AreEqual(0.0659, posed[3].Rotation, 0.0001);

                    var creatures = context.CreatureEntries.AsNoTracking().Where(creature => creature.Id >= 400001 && creature.Id <= 400004).OrderBy(creature => creature.Id).ToList();
                    var classes = context.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.ClassName);

                    Assert.AreEqual(3, creatures.Count);
                    Assert.AreEqual("Redshirt_Forean_Spearman", classes[creatures[0].ClassId]);
                    Assert.AreEqual(0u, creatures[0].NameId, "The client names the class Forean Warrior.");
                    Assert.AreEqual("Redshirt_Human_Soldier_Light_Male", classes[creatures[1].ClassId]);
                    Assert.AreEqual("Redshirt_Human_Soldier_Light_Male", classes[creatures[2].ClassId]);
                    Assert.AreEqual(8716u, creatures[1].NameId);
                    Assert.AreEqual(8716u, creatures[2].NameId);

                    foreach (var creature in creatures)
                    {
                        Assert.AreEqual(1u, creature.Faction, "friendly");
                        Assert.AreEqual(0u, creature.Action1, "no attack: safe ground");
                        Assert.AreEqual(0u, creature.WalkSpeed);
                        Assert.AreEqual(0u, creature.RunSpeed);
                    }

                    var held = context.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id >= 400001 && row.Id <= 400004).ToList();

                    CollectionAssert.AreEquivalent(
                        new[] { (400001u, 13u, "Weapon_Creature_Forean_Spear"), (400003u, 13u, "Weapon_Avatar_Rifle_Physical_UNC_01_to_05") },
                        held.Select(row => (row.Id, row.SlotId, classes[row.ClassId])).ToList());

                    // What the ambient poses put in the weapon slot are classes the world has.
                    foreach (var forcer in new[] { NpcPoses.LeaningClass, NpcPoses.SittingClass, NpcPoses.LyingDownClass, NpcPoses.AtConsoleClass, NpcPoses.HandToolClass })
                        StringAssert.StartsWith(classes[forcer], "AnimCondForcer_Avatar_");
                }
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheFourStandAtTheirPostsOnTheProvingGroundsAndAPlayerIsShownThemPosed()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true, worldMigration: BootcampAmbientGuardTests.Before);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            Creature Of(uint poolId) => harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == poolId);

            var warrior = Of(BootcampPostedNpcs.ForeanWarriorPoolId);
            var infantryman = Of(BootcampPostedNpcs.InfantrymanPoolId);
            var west = Of(BootcampPostedNpcs.RiflemanWestPoolId);
            var east = Of(BootcampPostedNpcs.RiflemanEastPoolId);
            var all = new[] { warrior, infantryman, west, east };

            Assert.AreEqual(NpcPose.Standing, warrior.Pose);
            Assert.AreEqual(NpcPose.Standing, infantryman.Pose);
            Assert.AreEqual(NpcPose.WeaponOut, west.Pose);
            Assert.AreEqual(NpcPose.WeaponOut, east.Pose);

            foreach (var npc in all)
            {
                Assert.IsTrue(npc.PoseShown);
                Assert.AreEqual(TargetCategory.Friendly, npc.TargetCategory);
                Assert.IsNull(npc.Npc, "A soldier on guard, with nothing to say or sell.");
                // On the spot it was placed, at the height the GM stood at: it has no speed, so
                // it is not put on the navmesh (BehaviorManager.NeverMoves).
                Assert.IsTrue(BehaviorManager.NeverMoves(npc));
                Assert.AreEqual(npc.SpawnPool.Position, npc.Position);
                Assert.AreEqual(npc.SpawnPool.Rotation, npc.Rotation);
            }

            // The spear, nothing, and the rifle.
            Assert.AreEqual(6042u, warrior.AppearanceData[EquipmentData.Weapon].Class);
            Assert.IsFalse(infantryman.AppearanceData.ContainsKey(EquipmentData.Weapon));
            Assert.AreEqual(Rifle, west.AppearanceData[EquipmentData.Weapon].Class);
            Assert.AreEqual(Rifle, east.AppearanceData[EquipmentData.Weapon].Class);

            // What a player walking up to them is sent.
            harness.MovePlayerTo(infantryman);
            harness.Drain();
            CellManager.Instance.UpdateVisibility(harness.Client);

            var made = harness.Drain().OfType<Rasa.Packets.Game.Server.CreatePhysicalEntityPacket>()
                .Where(packet => all.Any(npc => npc.EntityId == packet.EntityId))
                .ToDictionary(packet => packet.EntityId);

            Assert.AreEqual(4, made.Count);

            foreach (var npc in all)
            {
                var data = made[npc.EntityId].EntityData;
                var actor = data.OfType<ActorInfoPacket>().Single();
                var rifleOut = npc == west || npc == east;

                Assert.AreEqual(rifleOut, actor.StateIds.Contains(CharacterState.ToolReady), npc.DbId.ToString());
                Assert.IsFalse(actor.CombatMode, "At peace: the rifle is held, not aimed.");
                Assert.AreEqual(CharacterState.Standing, actor.DesiredPostureId);
                Assert.AreEqual(npc.SpawnPool.Rotation, actor.Yaw);
                Assert.AreEqual(TargetCategory.Friendly, data.OfType<TargetCategoryPacket>().Single().TargetCategory);
            }

            // "Forean Warrior" is the client's name for the class; "Infantryman" is creature name 8716.
            Assert.AreEqual(0u, made[warrior.EntityId].EntityData.OfType<CreatureInfoPacket>().Single().CreatureNameId);
            Assert.AreEqual((EntityClasses)6043, made[warrior.EntityId].ClassId);
            Assert.AreEqual(8716u, made[infantryman.EntityId].EntityData.OfType<CreatureInfoPacket>().Single().CreatureNameId);
            Assert.AreEqual(8716u, made[west.EntityId].EntityData.OfType<CreatureInfoPacket>().Single().CreatureNameId);
            Assert.AreEqual((EntityClasses)29423, made[east.EntityId].ClassId);

            // A minute and a half on, with a player beside them: nobody has moved or changed.
            var posts = all.ToDictionary(npc => npc.EntityId, npc => npc.Position);
            harness.Drain();

            for (var tick = 0; tick < 360; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            // Whatever else is going on at the base, nothing is said of these four.
            var after = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).ToList();

            foreach (var npc in all)
            {
                Assert.AreEqual(posts[npc.EntityId], npc.Position);
                Assert.IsTrue(npc.PoseShown);
                Assert.AreEqual(BehaviorManager.BehaviorActionWander, npc.Controller.CurrentAction);
                Assert.AreEqual(0, after.OfType<CallMethodMessage>().Count(message => message.EntityId == npc.EntityId), npc.DbId.ToString());
                Assert.AreEqual(0, after.OfType<MoveObjectMessage>().Count(message => message.EntityId == npc.EntityId), npc.DbId.ToString());
            }
        }

        [TestMethod]
        public void ThePostsAreOnTheProvingGroundsWalkableGround()
        {
            var path = Path.Combine(RepositoryRoot(), "navmesh", "adv_bootcamp.nav");

            if (!File.Exists(path))
                Assert.Inconclusive($"No navmesh at {path}.");

            var query = TestNavMeshes.Query(path);

            foreach (var post in new[]
                     {
                         new Vector3(404.0625f, 120.7656f, 110.1328f),
                         new Vector3(383.5859f, 120.2812f, 118.5352f),
                         new Vector3(384.4805f, 119.5273f, 140.3789f),
                         new Vector3(392.9609f, 119.7812f, 140.5195f)
                     })
            {
                var ground = query.Nearest(post);

                Assert.IsTrue(ground.HasValue, post.ToString());
                Assert.IsLessThan(0.75f, Vector2.Distance(new Vector2(ground.Value.X, ground.Value.Z), new Vector2(post.X, post.Z)), post.ToString());
                Assert.IsLessThan(1.5f, Math.Abs(ground.Value.Y - post.Y), post.ToString());
            }
        }

        #endregion

        #region Fixture

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }

        private static PythonPacket[] Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToArray();

        private static List<MoveObjectMessage> Moves(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<MoveObjectMessage>()
                .ToList();

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static void Think(WorldTestContext world, long milliseconds)
        {
            for (var elapsed = 0L; elapsed < milliseconds; elapsed += 250)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
        }

        /// <summary>A creature standing idle in the map's cells, its home where it stands. Hostile, so it may fight a player; it scans for nobody.</summary>
        private static Creature Spawn(WorldTestContext world, float x, uint weapon = 0, float walkSpeed = 0)
        {
            var creature = new Creature
            {
                Name = "Posted",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = walkSpeed,
                RunSpeed = walkSpeed * 2,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            if (weapon != 0)
                creature.AppearanceData[EquipmentData.Weapon] = new AppearanceData { SlotId = EquipmentData.Weapon, Class = weapon, Color = new Color(1), Hue2 = new Color(1) };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client with health, standing in the map's cells, so what is sent about what is near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        #endregion
    }
}
