using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// What a hit tells the client besides its damage: the effects it put on its target, which
    /// the client announces as it plays the hit (rawInfo's targetEffectIds, effect type ids),
    /// and a misstype for every miss.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HitResultTests
    {
        private const uint CritFire = CritEffects.CritFireTypeId;
        private const uint CritEmp = CritEffects.CritEmpTypeId;

        #region What is written

        [TestMethod]
        public void ARawInfoEndsInTheEffectsOfItsTargetAndOfItsSource()
        {
            var plain = Raw(pw => DamageInfoWriter.WriteRawInfo(pw, DamageType.Fire, 40));
            Assert.HasCount(12, plain);
            Assert.IsEmpty((List<object>)plain[10], "none, as it always was");
            Assert.IsEmpty((List<object>)plain[11]);

            var named = Raw(pw => DamageInfoWriter.WriteRawInfo(pw, DamageType.Fire, 40,
                targetEffectIds: new uint[] { CritFire, 10000018 }, sourceEffectIds: new uint[] { 6 }));
            CollectionAssert.AreEqual(new object[] { 2L, 10000018L }, (List<object>)named[10]);
            CollectionAssert.AreEqual(new object[] { 6L }, (List<object>)named[11]);
            CollectionAssert.AreEqual(plain.Take(10).ToList(), named.Take(10).ToList(), "and nothing before them moved");
        }

        [TestMethod]
        public void AWeaponsRecoveryCarriesEachHitsEffectsAndAMissTypeForEachMiss()
        {
            var missile = new Missile { ActionId = ActionId.WeaponAttack, ActionArgId = 1, DamageType = DamageType.Fire };
            var burnt = new HitData { EntityId = 501, FinalAmt = 80, IsCritical = 1 };
            var grazed = new HitData { EntityId = 502, FinalAmt = 20 };

            burnt.TargetEffectIds.Add(CritFire);
            missile.Args.HitEntities.AddRange(new ulong[] { 501, 502 });
            missile.Args.HitData.AddRange(new[] { burnt, grazed });
            missile.Args.MisstEntities.AddRange(new ulong[] { 601, 602, 603 });
            missile.Args.Missdata.AddRange(new uint[] { MissileManager.MissTypeDeflect, 0, MissileManager.MissTypeDodge });

            var sent = Decode(new WeaponAttackRecovery(missile));

            // (actionId, actionArgId, hits, misses, missdata, hitdata)
            Assert.HasCount(6, sent);
            CollectionAssert.AreEqual(new object[] { 4L, 1L, 2L }, (List<object>)sent[4], "a miss with no type is a plain miss: the client has no misstype 0");

            var hits = (List<object>)sent[5];
            var first = (List<object>)((List<object>)hits[0])[1];
            var second = (List<object>)((List<object>)hits[1])[1];

            Assert.HasCount(12, first);
            Assert.AreEqual((long)DamageType.Fire, first[0]);
            Assert.AreEqual(80L, first[5]);
            Assert.AreEqual(1L, first[6]);
            CollectionAssert.AreEqual(new object[] { 2L }, (List<object>)first[10]);
            Assert.IsEmpty((List<object>)first[11]);
            Assert.IsEmpty((List<object>)second[10]);
        }

        [TestMethod]
        public void TheGeneralRecoverySendsItsMissTypesAndItsHitsEffects()
        {
            var args = new MissileArgs();
            var hit = new HitData { EntityId = 501, FinalAmt = 60, DamageType = DamageType.EMP, IsCritical = 1 };

            hit.TargetEffectIds.Add(CritEmp);
            args.HitEntities.Add(501);
            args.HitData.Add(hit);
            args.MisstEntities.AddRange(new ulong[] { 601, 602 });
            args.Missdata.AddRange(new uint[] { MissileManager.MissTypeDodge, 0 });

            var sent = Decode(new PerformRecoveryPacket(PerformType.ListOfArgs, ActionId.WeaponPolaritygun, 2, args));

            CollectionAssert.AreEqual(new object[] { 2L, 1L }, (List<object>)sent[4], "it sent none, and every miss was a plain one");

            var raw = (List<object>)((List<object>)((List<object>)sent[5])[0])[1];
            Assert.AreEqual((long)DamageType.EMP, raw[0]);
            CollectionAssert.AreEqual(new object[] { 417L }, (List<object>)raw[10]);

            // With nothing missed there is nothing to say, as before.
            Assert.IsEmpty((List<object>)Decode(new PerformRecoveryPacket(PerformType.ListOfArgs, ActionId.WeaponPolaritygun, 2, new MissileArgs()))[4]);
        }

        [TestMethod]
        public void AConstantFirePulseCarriesEachShotsEffects()
        {
            var tick = new ConstantFireTickPacket(9, false);
            var shot = new TickEntry { EntityId = 501, Amount = 30, DamageType = DamageType.Fire, IsCritical = true };

            shot.TargetEffectIds.Add(CritFire);
            tick.Pulses.Add(new List<TickEntry> { shot, new TickEntry { EntityId = 502, Amount = 10, DamageType = DamageType.Fire } });

            var shots = Shots(Decode(tick));

            CollectionAssert.AreEqual(new object[] { 2L }, (List<object>)shots[0][10]);
            Assert.IsEmpty((List<object>)shots[1][10]);
        }

        #endregion

        #region What a hit announces

        [TestMethod]
        public void ADebuffTheHitPutsOnItsCreatureIsAttachedQuietlyAndNamedInTheHit()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var named = new List<uint>();

            using (HitEffects.On(creature, shooter.Player, named))
                CritEffects.Burn(world.Map, creature, shooter.Player, 100);

            var attached = Attaches(shooter).Single();
            Assert.AreEqual(CritEffects.CritFireTypeId, attached.EffectTypeId);
            Assert.IsFalse(attached.Announced, "the hit announces it");
            CollectionAssert.AreEqual(new[] { CritFire }, named);
            Assert.IsTrue(creature.ActiveEffects.Values.Any(effect => effect.TypeId == CritEffects.CritFireTypeId));

            // The same with no hit open: it announces itself, as it did.
            CritEffects.SuppressArmor(world.Map, creature, shooter.Player);
            Assert.IsTrue(Attaches(shooter).Single().Announced);
            CollectionAssert.AreEqual(new[] { CritFire }, named);

            // Someone who comes into view afterwards is shown it announced.
            Assert.IsTrue(GameEffectManager.EffectsForNewcomer(creature, shooter.Player)
                .Single(packet => packet.EffectTypeId == CritEffects.CritFireTypeId).Announced);
        }

        [TestMethod]
        public void OnlyWhatTheHitCanAnnounceIsTakenFromItsAttach()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var other = Watch(world, 1);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var bystander = Spawn(world, new Vector3(3, 0, -5));
            var named = new List<uint>();

            using (HitEffects.On(creature, shooter.Player, named))
            {
                // On another creature; from somebody else; a buff; one already quiet; one that says it must announce itself.
                CritEffects.Burn(world.Map, bystander, shooter.Player, 100);
                CritEffects.SuppressArmor(world.Map, creature, other.Player);
                GameEffectManager.Instance.Attach(world.Map, creature, Effect(world, shooter, 6, isBuff: true));
                GameEffectManager.Instance.Attach(world.Map, creature, Effect(world, shooter, 131, announce: false));
                GameEffectManager.Instance.Attach(world.Map, creature, Effect(world, shooter, 418, withHit: false));
            }

            Assert.IsEmpty(named);

            var sent = Attaches(shooter);
            Assert.HasCount(5, sent);
            Assert.IsTrue(sent.Where(packet => packet.EffectTypeId != 131).All(packet => packet.Announced), "each as it was");
            Assert.IsFalse(sent.Single(packet => packet.EffectTypeId == 131).Announced);

            // A hit with no list names nothing, and one by nobody is no hit.
            Assert.IsNull(HitEffects.On(creature, shooter.Player, null));
            Assert.IsNull(HitEffects.On(creature, null, named));
        }

        [TestMethod]
        public void AnEffectThatIsRefusedIsNotNamedAndOneHitInsideAnotherKeepsItsOwn()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var second = Spawn(world, new Vector3(2, 0, -5));
            var outer = new List<uint>();
            var inner = new List<uint>();

            using (HitEffects.On(creature, shooter.Player, outer))
            {
                using (HitEffects.On(second, shooter.Player, inner))
                {
                    CritEffects.Burn(world.Map, second, shooter.Player, 100);
                    CritEffects.Burn(world.Map, creature, shooter.Player, 100);
                }

                CritEffects.SuppressArmor(world.Map, creature, shooter.Player);
            }

            CollectionAssert.AreEqual(new[] { CritFire }, inner);
            CollectionAssert.AreEqual(new[] { CritEmp }, outer, "the burn that went on while the other hit was open announced itself");

            // Closed: nothing is taken any more.
            Attaches(shooter);
            CritEffects.WeakenRanged(world.Map, creature, shooter.Player);
            Assert.IsTrue(Attaches(shooter).Single().Announced);

            // A creature running home takes no debuff: nothing went on, so nothing is named.
            var named = new List<uint>();
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionReturning;

            if (BehaviorManager.IsReturning(creature))
            {
                using (HitEffects.On(creature, shooter.Player, named))
                    CritEffects.Burn(world.Map, creature, shooter.Player, 100);

                Assert.IsEmpty(named);
            }
        }

        [TestMethod]
        [DataRow(DamageType.Fire, 2u)]
        [DataRow(DamageType.Ice, 3u)]
        [DataRow(DamageType.Laser, 6u)]
        [DataRow(DamageType.Sonic, 7u)]
        [DataRow(DamageType.EMP, 417u)]
        [DataRow(DamageType.Virulent, 10000018u)]
        public void AWeaponsCritOnACreatureIsAnnouncedByItsHit(DamageType damageType, uint effectTypeId)
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            var missile = Shoot(world, shooter, creature, damageType, critChance: 100);

            Assert.IsTrue(missile.IsCritical);

            var sent = Sent(shooter);
            var attached = sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == (int)effectTypeId);
            Assert.IsFalse(attached.Announced, "quiet: the hit announces it when the shot is seen to land");

            var recovery = sent.OfType<WeaponAttackRecovery>().Single();
            var hit = recovery.Missile.Args.HitData.Single();
            Assert.AreEqual(creature.EntityId, hit.EntityId);
            CollectionAssert.AreEqual(new[] { effectTypeId }, hit.TargetEffectIds);
            Assert.IsEmpty(hit.SourceEffectIds);

            // And it is what the client is sent.
            var raw = (List<object>)((List<object>)((List<object>)Decode(recovery)[5])[0])[1];
            CollectionAssert.AreEqual(new object[] { (long)effectTypeId }, (List<object>)raw[10]);
        }

        [TestMethod]
        public void AnElectricCritsArcIsNamedInItsHitAndDrawnOnceTheHitHasPlayed()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var bystander = Spawn(world, new Vector3(3, 0, -5));

            Shoot(world, shooter, creature, DamageType.Electrical, critChance: 100);

            var sent = Sent(shooter);
            Assert.IsFalse(sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == CritEffects.CritElectricTypeId).Announced);
            CollectionAssert.AreEqual(new[] { (uint)CritEffects.CritElectricTypeId },
                sent.OfType<WeaponAttackRecovery>().Single().Missile.Args.HitData.Single().TargetEffectIds);

            // Its FX is not there until the hit is played: nothing is drawn, nobody else is hurt.
            Assert.IsEmpty(sent.OfType<GameEffectTickPacket>().ToArray());
            Assert.IsEmpty(sent.OfType<GameEffectAnnounceDamagePacket>().ToArray());
            Assert.AreEqual(10000, bystander.Attributes[Attributes.Health].Current);

            var arc = creature.ActiveEffects.Values.Single(effect => effect.TypeId == CritEffects.CritElectricTypeId);
            Assert.AreEqual(CritEffects.ArcMarginMs, arc.TickIntervalMs, "a shot the server did not time is played as it arrives");
            Assert.IsTrue(arc.ExpiresTick > arc.NextTickTick, "and it stays on past its arcs");

            // Its tick: the arcs, then their numbers - once.
            arc.NextTickTick = Environment.TickCount64;
            GameEffectManager.Instance.DoWork(world.Map, 100);

            sent = Sent(shooter);
            Assert.AreEqual(bystander.EntityId, sent.OfType<GameEffectTickPacket>().Single().Entries.Single().EntityId);
            Assert.AreEqual(bystander.EntityId, sent.OfType<GameEffectAnnounceDamagePacket>().Single().Hits.Single().EntityId);
            Assert.IsLessThan(10000, bystander.Attributes[Attributes.Health].Current);

            GameEffectManager.Instance.DoWork(world.Map, 100);
            Assert.IsEmpty(Sent(shooter).OfType<GameEffectTickPacket>().ToArray());

            // A hit with a flight waits for it; with no hit open, the arcs go at once, as they did.
            var timed = new List<uint>();

            using (HitEffects.On(bystander, shooter.Player, timed, 400))
                CritEffects.Arc(world.Map, bystander, shooter.Player, 100);

            Assert.AreEqual(400 + CritEffects.ArcMarginMs, bystander.ActiveEffects.Values.Single(effect => effect.TypeId == CritEffects.CritElectricTypeId).TickIntervalMs);
            CollectionAssert.AreEqual(new[] { (uint)CritEffects.CritElectricTypeId }, timed);
            Sent(shooter);

            CritEffects.Arc(world.Map, creature, shooter.Player, 100);

            sent = Sent(shooter);
            Assert.IsTrue(sent.OfType<GameEffectAttachedPacket>().Single().Announced);
            Assert.AreEqual(bystander.EntityId, sent.OfType<GameEffectTickPacket>().Single().Entries.Single().EntityId);
        }

        [TestMethod]
        public void AShotsFlightIsHowLongItsArcWaits()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -7));

            // 7 m at 70 m/s.
            Shoot(world, shooter, creature, DamageType.Electrical, critChance: 100, flightVelocity: 70);

            Assert.AreEqual(100 + CritEffects.ArcMarginMs,
                creature.ActiveEffects.Values.Single(effect => effect.TypeId == CritEffects.CritElectricTypeId).TickIntervalMs);
        }

        [TestMethod]
        public void AHitThatIsNoCritNamesNothing()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            Shoot(world, shooter, creature, DamageType.Fire, critChance: 0);

            var sent = Sent(shooter);
            Assert.IsEmpty(sent.OfType<GameEffectAttachedPacket>().ToArray());
            Assert.IsEmpty(sent.OfType<WeaponAttackRecovery>().Single().Missile.Args.HitData.Single().TargetEffectIds);
        }

        [TestMethod]
        public void OnAPlayerStruckTheHitNamesItsDebuffsButNotWhatHoldsThem()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var struck = Watch(world, 1);
            var named = new List<uint>();

            using (HitEffects.On(struck.Player, shooter.Player, named))
            {
                CritEffects.Burn(world.Map, struck.Player, shooter.Player, 100);
                CrowdControl.Slow(world.Map, struck.Player, shooter.Player, CrowdControl.CritVirulentTypeId, CrowdControl.VirulentCrippleSlowPercent, CrowdControl.VirulentCrippleMs, "snareMod");
                Assert.IsTrue(PlayerCrowdControl.Stun(world.Map, struck.Player, shooter.Player, 1000));
                PlayerCrowdControl.Root(world.Map, struck.Player, shooter.Player, CrowdControl.NetGunRootTypeId, 1000);
            }

            CollectionAssert.AreEqual(new[] { CritFire, 10000018u }, named, "the burn and the cripple wait for the hit");

            var sent = Attaches(struck);
            Assert.IsFalse(sent.Single(packet => packet.EffectTypeId == CritEffects.CritFireTypeId).Announced);
            Assert.IsFalse(sent.Single(packet => packet.EffectTypeId == CrowdControl.CritVirulentTypeId).Announced);
            Assert.IsTrue(sent.Single(packet => packet.EffectTypeId == Stuns.StunTypeId).Announced, "a stun stops their client at once");
            Assert.IsTrue(sent.Single(packet => packet.EffectTypeId == CrowdControl.NetGunRootTypeId).Announced, "and so does a root");
        }

        [TestMethod]
        public void AnAbilitysCritIsAnnouncedByItsHit()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var info = new ActionLevelInfo { ActionId = ActionId.AaRecruitLightning, Level = 1, MaxRange = 20 };
            info.Properties[AbilityProperty.DamageAmountMin] = 100;
            info.Properties[AbilityProperty.DamageAmountMax] = 100;
            info.Properties[AbilityProperty.DamageType] = (int)DamageType.Fire;
            var action = new ActionInfo { ActionId = ActionId.AaRecruitLightning, Module = "abilities.damagebase" };
            action.Levels[1] = info;

            AlwaysCrit(world, shooter);
            BootcampRuntimeTestHarness.InvokeResolveDirectDamage(Abilities(), world.Map, shooter, action, info,
                new ActionData(shooter.Player, ActionId.AaRecruitLightning, 1, 0) { TargetId = creature.EntityId });

            var sent = Sent(shooter);
            Assert.IsFalse(sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == CritEffects.CritFireTypeId).Announced);

            var recovery = sent.OfType<AbilityRecoveryPacket>().Single();
            var hit = recovery.Hits.Single();
            Assert.IsTrue(hit.IsCritical);
            CollectionAssert.AreEqual(new[] { CritFire }, hit.TargetEffectIds);

            // hitdata[0] = (rawInfo, onHitData), as DamageBase.DoAbility takes it.
            var raw = (List<object>)((List<object>)((List<object>)Decode(recovery)[5])[0])[0];
            CollectionAssert.AreEqual(new object[] { 2L }, (List<object>)raw[10]);
        }

        [TestMethod]
        public void ABlastsCritIsAnnouncedByTheBlast()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var bomb = new GameEffect
            {
                TypeId = 207,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = shooter.Player.EntityId,
                Source = shooter.Player,
                SourceLevel = 1,
                TickDamageMin = 100,
                TickDamageMax = 100,
                TickDamageType = DamageType.EMP,
                TickRadius = 10,
                ExpiresTick = Environment.TickCount64 + 5000
            };

            AlwaysCrit(world, shooter);
            typeof(AbilityManager).GetMethod("Detonate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, new[] { typeof(MapChannel), typeof(Actor), typeof(GameEffect) }, null)!
                .Invoke(Abilities(), new object[] { world.Map, creature, bomb });

            var sent = Sent(shooter);
            Assert.IsFalse(sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == CritEffects.CritEmpTypeId).Announced);

            var blast = sent.OfType<GameEffectAnnounceDamagePacket>().Single();
            Assert.AreEqual("DoExplosion", blast.MethodName);
            CollectionAssert.AreEqual(new[] { CritEmp }, blast.Hits.Single().TargetEffectIds);

            // (effectId, method, (damageData,)): damageData[0] = (targetId, rawInfo).
            var raw = (List<object>)((List<object>)((List<object>)((List<object>)Decode(blast)[2])[0])[0])[1];
            CollectionAssert.AreEqual(new object[] { 417L }, (List<object>)raw[10]);
        }

        [TestMethod]
        public void ANetGunsRootIsAnnouncedByItsHitToo()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            Shoot(world, shooter, creature, DamageType.Physical, critChance: 0, rootMs: 3000);

            var sent = Sent(shooter);
            Assert.IsFalse(sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == CrowdControl.NetGunRootTypeId).Announced);
            CollectionAssert.AreEqual(new[] { (uint)CrowdControl.NetGunRootTypeId },
                sent.OfType<WeaponAttackRecovery>().Single().Missile.Args.HitData.Single().TargetEffectIds);
        }

        [TestMethod]
        public void AShotAtACreatureThatHasDiedIsAPlainMiss()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            creature.State = CharacterState.Dead;
            creature.Attributes[Attributes.Health].Current = 0;

            Shoot(world, shooter, creature, DamageType.Physical, critChance: 0);

            var recovery = Sent(shooter).OfType<WeaponAttackRecovery>().Single();
            CollectionAssert.AreEqual(new[] { creature.EntityId }, recovery.Missile.Args.MisstEntities);
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, recovery.Missile.Args.Missdata, "it was misstype 0, which the client has no row for");
            Assert.IsEmpty(recovery.Missile.Args.HitData);
        }

        [TestMethod]
        public void AConstantFireWeaponsCritIsAnnouncedByItsShot()
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)6048);
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var weapon = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 145, ItemClass = 6048 })
                {
                    WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
                    {
                        Id = 145, AmmoPerShot = 1, Refire = 200, ReloadTime = 1500,
                        Windup = 0, Recovery = 1, Range = 80, ToolType = 15, AttackType = 2
                    })
                },
                ItemTemplateId = 145, StackSize = 1, Crafter = ""
            };
            var action = new ActionData(shooter.Player, ActionId.WeaponMachinegun, 4, 0) { TargetId = creature.EntityId };
            shooter.Player.Target = creature.EntityId;

            try
            {
                // A crit bonus of a hundred: every shot is one.
                ConstantFire.Pulse(world.Map, shooter, weapon, action, 40, DamageType.Fire, 100);

                var sent = Sent(shooter);
                var burn = sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == CritEffects.CritFireTypeId);
                Assert.IsFalse(burn.Announced);

                var tick = sent.OfType<ConstantFireTickPacket>().Single();
                var shot = tick.Pulses.Single().Single();
                Assert.IsTrue(shot.IsCritical);
                CollectionAssert.AreEqual(new[] { CritFire }, shot.TargetEffectIds);
                CollectionAssert.AreEqual(new object[] { 2L }, (List<object>)Shots(Decode(tick))[0][10]);
            }
            finally
            {
                ConstantFire.Stop(shooter, release: false);
            }
        }

        #endregion

        #region Fixtures

        /// <summary>A shot from the player at the creature that lands now; with a velocity, one their client flies.</summary>
        private static Missile Shoot(WorldTestContext world, Client shooter, Creature target, DamageType damageType, double critChance, int rootMs = 0, int? flightVelocity = null)
        {
            var missile = new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = 100,
                DamageType = damageType,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = critChance,
                RootMs = rootMs,
                FlightVelocity = flightVelocity
            };

            Sent(shooter);
            MissileManager.Instance.MissileTrigger(world.Map, missile);

            return missile;
        }

        private static GameEffect Effect(WorldTestContext world, Client source, int typeId, bool isBuff = false, bool announce = true, bool withHit = true) =>
            new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = source.Player.EntityId,
                Source = source.Player,
                IsBuff = isBuff,
                AnnounceOnAttach = announce,
                AnnounceWithHit = withHit,
                ExpiresTick = Environment.TickCount64 + 5000
            };

        /// <summary>Every attack of the player's a crit: a Crit Wave of 200%.</summary>
        private static void AlwaysCrit(WorldTestContext world, Client client)
        {
            var wave = new GameEffect
            {
                TypeId = 202,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                SourceId = client.Player.EntityId,
                Source = client.Player,
                IsBuff = true,
                CritChancePercent = 200,
                ExpiresTick = Environment.TickCount64 + 60000
            };

            client.Player.ActiveEffects[wave.EffectId] = wave;
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static List<GameEffectAttachedPacket> Attaches(Client client) => Sent(client).OfType<GameEffectAttachedPacket>().ToList();

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        /// <summary>A hostile creature with ten thousand health and no armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        /// <summary>The rawInfo of each shot of a constant-fire tick's first pulse.</summary>
        private static List<List<object>> Shots(List<object> tick)
        {
            // (pulses) or, with heals, (healData, damageData): the pulses are the last element.
            var pulses = (List<object>)tick[tick.Count - 1];
            var shots = (List<object>)((List<object>)pulses[0])[0];

            return shots.Select(shot => (List<object>)((List<object>)shot)[1]).ToList();
        }

        /// <summary>One rawInfo, as the client reads it.</summary>
        private static List<object> Raw(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            using var writer = new PythonWriter(binary);

            write(writer);
            binary.Flush();

            return (List<object>)Read(new PythonReader(new BinaryReader(new MemoryStream(stream.ToArray()))));
        }

        /// <summary>What a packet writes, as the client reads it: tuples and lists as lists, whole numbers as longs.</summary>
        private static List<object> Decode(PythonPacket packet) =>
            (List<object>)Read(new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet)))));

        private static object Read(PythonReader reader)
        {
            switch (reader.PeekType())
            {
                case PythonType.Tuple:
                    return Items(reader, reader.ReadTuple());
                case PythonType.List:
                    return Items(reader, reader.ReadList());
                case PythonType.Int:
                    return (long)reader.ReadInt();
                case PythonType.Long:
                    return reader.ReadLong();
                case PythonType.Double:
                    return reader.ReadDouble();
                case PythonType.String:
                    return reader.ReadString();
                case PythonType.Structs:
                    return reader.ReadUnkStruct() switch
                    {
                        PythonStruct.None => null,
                        PythonStruct.True => (object)true,
                        _ => false
                    };
                default:
                    throw new InvalidDataException($"Unexpected {reader.PeekType()} in a hit result.");
            }
        }

        private static List<object> Items(PythonReader reader, int count)
        {
            var items = new List<object>(count);

            for (var i = 0; i < count; i++)
                items.Add(Read(reader));

            return items;
        }

        #endregion
    }
}
