using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class LightningConsolidationTests
    {
        [TestMethod]
        public void LightningArcSelectionIsBoundedDistinctAndDeterministic()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            PrepareDirectDamageClient(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var farther = AddTarget(world, new Vector3(16, 0, 0));
            var tiedA = AddTarget(world, new Vector3(13, 4, 0));
            var tiedB = AddTarget(world, new Vector3(13, -4, 0));
            var expected = tiedA.EntityId < tiedB.EntityId ? tiedA : tiedB;

            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
            {
                cell.CreatureList.Reverse();
                if (cell.CreatureList.Contains(expected))
                    cell.CreatureList.Add(expected);
            }

            var selected = AbilityManager.SelectLightningArcTargets(
                world.Map, client.Player, primary, 5, 1);

            Assert.AreEqual(1, selected.Count);
            Assert.AreSame(expected, selected.Single());
            Assert.IsFalse(selected.Contains(primary));
            Assert.IsFalse(selected.Contains(farther));
            Cleanup(world, primary, farther, tiedA, tiedB);
        }

        [TestMethod]
        public void LightningArcSelectionRejectsInvalidTargetsAndNonfiniteRange()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            PrepareDirectDamageClient(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var friendly = AddTarget(world, new Vector3(11, 0, 0));
            friendly.Faction = Factions.AFS;
            var dead = AddTarget(world, new Vector3(12, 0, 0));
            dead.State = CharacterState.Dead;
            var otherMap = AddTarget(world, new Vector3(13, 0, 0));
            otherMap.MapContextId++;
            var missingHealth = AddTarget(world, new Vector3(14, 0, 0));
            missingHealth.Attributes.Remove(Attributes.Health);

            Assert.AreEqual(0, AbilityManager.SelectLightningArcTargets(
                world.Map, client.Player, primary, float.NaN, 1).Count);
            Assert.AreEqual(0, AbilityManager.SelectLightningArcTargets(
                world.Map, client.Player, primary, 12, 4).Count);
            Cleanup(world, primary, friendly, dead, otherMap, missingHealth);
        }

        [TestMethod]
        public void AbilityRecoveryEncodesArcHitsUnderTheirPrimaryAndSnapshotsThem()
        {
            var packet = new AbilityRecoveryPacket(
                ActionId.AaRecruitLightning, 2, AbilityRecoveryPacket.HitDataKind.Damage)
            {
                ArcData = true
            };
            var primary = new AbilityHit
            {
                EntityId = 0x100000007,
                Amount = 240,
                DamageType = DamageType.Electrical
            };
            primary.Arcs.Add(new AbilityHit
            {
                EntityId = 0x200000009,
                Amount = 210,
                DamageType = DamageType.Electrical
            });
            packet.Hits.Add(primary);
            var expected = Encode(packet);

            primary.Amount = 9999;
            primary.Arcs.Single().Amount = 9999;
            primary.Arcs.Clear();

            CollectionAssert.AreEqual(expected, Encode(packet));
            using var stream = new MemoryStream(expected);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)ActionId.AaRecruitLightning, reader.ReadUInt());
            Assert.AreEqual(2u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(0x100000007UL, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            ReadRaw(reader, 240);
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(0x200000009UL, reader.ReadULong());
            ReadRaw(reader, 210);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ActionTableArcPropertiesDriveRadiusAndDamage()
        {
            var level = new ActionLevelInfo();
            level.Properties[AbilityProperty.ArcRadius] = 12;
            level.Properties[AbilityProperty.ArcDamage] = 210;
            level.Properties[AbilityProperty.DamageScaleType] = 0;

            var spec = AbilityManager.GetLightningArcSpec(level, 15);

            Assert.AreEqual(12f, spec.Radius);
            Assert.AreEqual(210, spec.Damage);
            Assert.AreEqual(1, spec.MaximumTargets);
        }

        [TestMethod]
        public void LightningLandingRejectsPrimaryThatBecameInvalidDuringWindup()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            PrepareDirectDamageClient(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var arc = AddTarget(world, new Vector3(11, 0, 0));
            var manager = (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { null });
            var actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(manager);
            var info = new ActionLevelInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Level = 1,
                MaxRange = 20
            };
            info.Properties[AbilityProperty.DamageAmountMin] = 10;
            info.Properties[AbilityProperty.DamageAmountMax] = 10;
            info.Properties[AbilityProperty.ArcRadius] = 5;
            info.Properties[AbilityProperty.ArcDamage] = 5;
            info.Properties[AbilityProperty.RadiusAroundTarget] = 5;
            var actionInfo = new ActionInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Module = "abilities.lightning"
            };
            actionInfo.Levels[1] = info;
            actions[actionInfo.ActionId] = actionInfo;
            client.Player.Skills[(SkillId)1] =
                new SkillsData((SkillId)1, (int)ActionId.AaRecruitLightning, 1);
            var action = new ActionData(client.Player, ActionId.AaRecruitLightning, 1,
                primary.EntityId, 0);
            lock (Rasa.Game.Server.Clients)
                Rasa.Game.Server.Clients.Add(client);

            try
            {
                var primaryHealth = primary.Attributes[Attributes.Health];
                var arcHealth = arc.Attributes[Attributes.Health];
                EntityManager.Instance.UnregisterCreature(primary.EntityId);
                manager.PerformRecovery(world.Map, action);

                Assert.AreEqual(100, primaryHealth.Current);
                Assert.AreEqual(100, arcHealth.Current);
                Assert.AreEqual(0, WorldTestContext.Drain(client)
                    .Select(packet => packet.Message)
                    .OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                    .Select(message => message.Packet)
                    .OfType<AbilityRecoveryPacket>()
                    .Count());
            }
            finally
            {
                lock (Rasa.Game.Server.Clients)
                    Rasa.Game.Server.Clients.Remove(client);
                if (!EntityManager.Instance.Creatures.ContainsKey(primary.EntityId))
                    EntityManager.Instance.RegisterCreature(primary);
                Cleanup(world, primary, arc);
            }
        }

        [TestMethod]
        public void LightningSecondPrimaryValidationDoesNotFallThroughToRadiusTargets()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            PrepareDirectDamageClient(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var nearby = AddTarget(world, new Vector3(11, 0, 0));
            var fallback = AddTarget(world, new Vector3(4, 0, 0));
            var manager = CreateManager();
            var info = LightningInfo(primaryDamage: 10, arcDamage: 5);
            var actionInfo = LightningAction(info);
            var action = new ActionData(client.Player, ActionId.AaRecruitLightning, 1,
                primary.EntityId, 0);

            Assert.AreEqual(1, AbilityManager.SelectLightningArcTargets(
                world.Map, client.Player, primary, 5, 1).Count);
            EntityManager.Instance.UnregisterCreature(primary.EntityId);

            try
            {
                InvokeResolveDirectDamage(
                    manager, world.Map, client, actionInfo, info, action);

                Assert.AreEqual(100,
                    primary.Attributes[Attributes.Health].Current);
                Assert.AreEqual(100,
                    nearby.Attributes[Attributes.Health].Current);
                Assert.AreEqual(100,
                    fallback.Attributes[Attributes.Health].Current);
            }
            finally
            {
                EntityManager.Instance.RegisterCreature(primary);
                Cleanup(world, primary, nearby, fallback);
            }
        }

        [TestMethod]
        public void FatalLightningPrimaryStillArcsToTargetsValidAtLanding()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            PrepareDirectDamageClient(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            primary.Attributes[Attributes.Health].Current = 5;
            var arc = AddTarget(world, new Vector3(11, 0, 0));
            var info = LightningInfo(primaryDamage: 10, arcDamage: 7);
            var actionInfo = LightningAction(info);
            var action = new ActionData(client.Player, ActionId.AaRecruitLightning, 1,
                primary.EntityId, 0);
            CellManager.Instance.RemoveFromWorld(client);

            InvokeResolveDirectDamage(
                CreateManager(), world.Map, client, actionInfo, info, action);

            Assert.AreEqual(0, primary.Attributes[Attributes.Health].Current);
            Assert.AreEqual(93, arc.Attributes[Attributes.Health].Current);
            Cleanup(world, primary, arc);
        }

        [TestMethod]
        public void LightningArcIsRevalidatedAfterLandingSnapshotBeforeDamage()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var primary = AddTarget(world, new Vector3(10, 0, 0));
            var arc = AddTarget(world, new Vector3(11, 0, 0));
            var snapshot = AbilityManager.SelectLightningArcTargets(
                world.Map, client.Player, primary, 5, 1);
            Assert.AreEqual(1, snapshot.Count);
            arc.State = CharacterState.Dead;
            var hit = new AbilityHit { EntityId = primary.EntityId };
            var apply = typeof(AbilityManager).GetMethod(
                "ApplyLightningArcs",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(apply);
            apply.Invoke(null, new object[]
            {
                world.Map,
                client.Player,
                primary.EntityId,
                primary.Position,
                5f,
                7,
                DamageType.Electrical,
                snapshot,
                hit
            });

            Assert.AreEqual(100, arc.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0, hit.Arcs.Count);
            Cleanup(world, primary, arc);
        }

        private static AbilityManager CreateManager() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { null });

        private static void PrepareDirectDamageClient(Rasa.Game.Client client)
        {
            client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] =
                new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.Attributes[Attributes.Power] =
                new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
        }

        private static ActionLevelInfo LightningInfo(int primaryDamage, int arcDamage)
        {
            var info = new ActionLevelInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Level = 1,
                MaxRange = 20
            };
            info.Properties[AbilityProperty.DamageAmountMin] = primaryDamage;
            info.Properties[AbilityProperty.DamageAmountMax] = primaryDamage;
            info.Properties[AbilityProperty.ArcRadius] = 5;
            info.Properties[AbilityProperty.ArcDamage] = arcDamage;
            info.Properties[AbilityProperty.RadiusAroundTarget] = 5;
            return info;
        }

        private static ActionInfo LightningAction(ActionLevelInfo info)
        {
            var action = new ActionInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Module = "abilities.lightning"
            };
            action.Levels[info.Level] = info;
            return action;
        }

        private static void InvokeResolveDirectDamage(
            AbilityManager manager,
            MapChannel map,
            Rasa.Game.Client client,
            ActionInfo actionInfo,
            ActionLevelInfo info,
            ActionData action)
        {
            var method = typeof(AbilityManager).GetMethod(
                "ResolveDirectDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var arguments = new List<object>
            {
                map,
                client,
                client.Player,
                actionInfo,
                info,
                action
            };
            if (method.GetParameters().Length == 7)
                arguments.Add(null);
            method.Invoke(manager, arguments.ToArray());
        }

        private static Creature AddTarget(WorldTestContext world, Vector3 position)
        {
            var target = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = position,
                State = CharacterState.Normal,
                Faction = Factions.Bane,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(world.Map, target);
            return target;
        }

        private static void Cleanup(WorldTestContext world, params Creature[] targets)
        {
            foreach (var target in targets)
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, target);
        }

        private static byte[] Encode(AbilityRecoveryPacket packet)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            return stream.ToArray();
        }

        private static void ReadRaw(PythonReader reader, long amount)
        {
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual((uint)DamageType.Electrical, reader.ReadUInt());
            Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual(amount, reader.ReadLong());
            Assert.AreEqual(0, reader.ReadInt());
            reader.ReadInt();
            Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
        }
    }
}
