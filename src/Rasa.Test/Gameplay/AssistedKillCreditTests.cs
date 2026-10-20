using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A kill whose last blow is a friendly NPC's or a control point turret's is the last player's
    /// to hit the creature, while they are still in the fight (CreatureManager.HandleCreatureKill,
    /// Creature.LastPlayerHit). It used to be nobody's: no experience, adrenaline, loot or mission
    /// credit for anyone.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AssistedKillCreditTests
    {
        [TestMethod]
        public void AFriendlyNpcsLastBlowIsTheKillOfTheLastPlayerToHit()
        {
            using var context = Start();
            var first = context.CreateAdditionalClient(2);
            var creature = AddCreature(context, TargetCategory.Hostile);
            var soldier = AddCreature(context, TargetCategory.Friendly);

            Ready(first);
            Ready(context.Client);

            try
            {
                Hit(context, creature, first.Player);
                Hit(context, creature, context.Client.Player);
                Hit(context, creature, soldier);
                MissionTestContext.Drain(first);
                MissionTestContext.Drain(context.Client);

                Creatures(context).HandleCreatureKill(context.Map, creature, soldier);

                var gained = Gains(context.Client).Single();
                Assert.IsTrue(gained.Gained >= 90 && gained.Gained <= 110, $"a level 1 kill, not {gained.Gained}");
                Assert.AreEqual(gained.Gained, context.Client.Player.Experience);
                Assert.IsEmpty(Gains(first), "the last to hit has it, not the first");
                Assert.AreEqual(0u, first.Player.Experience);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, soldier);
            }
        }

        [TestMethod]
        public void NobodyHasItWhenNoPlayerHitOrTheFightWasGivenUp()
        {
            using var context = Start();
            var untouched = AddCreature(context, TargetCategory.Hostile);
            var leashed = AddCreature(context, TargetCategory.Hostile);
            var turret = AddCreature(context, TargetCategory.Friendly);
            Ready(context.Client);

            try
            {
                // Hit, then given up: a leash clears its grudges, and the player is out of it.
                Hit(context, leashed, context.Client.Player);
                Threat.Clear(leashed);
                MissionTestContext.Drain(context.Client);

                Creatures(context).HandleCreatureKill(context.Map, untouched, turret);
                Creatures(context).HandleCreatureKill(context.Map, leashed, turret);

                Assert.IsEmpty(Gains(context.Client));
                Assert.AreEqual(0u, context.Client.Player.Experience);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, untouched);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, leashed);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, turret);
            }
        }

        [TestMethod]
        public void ACreatureOfItsOwnSideCreditsNobody()
        {
            using var context = Start();
            var creature = AddCreature(context, TargetCategory.Hostile);
            var confused = AddCreature(context, TargetCategory.Hostile);
            Ready(context.Client);

            try
            {
                Hit(context, creature, context.Client.Player);
                MissionTestContext.Drain(context.Client);

                Creatures(context).HandleCreatureKill(context.Map, creature, confused);

                Assert.IsEmpty(Gains(context.Client));
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, confused);
            }
        }

        /// <summary>A player in the world and alive, as one in a fight is.</summary>
        private static void Ready(Client client)
        {
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            CellManager.Instance.AddToWorld(client);
        }

        /// <summary>A hit that lands, as the damage paths record it: the grudge, and who hit.</summary>
        private static void Hit(MissionTestContext context, Creature creature, Actor attacker)
        {
            Threat.FromDamage(creature, attacker, 10);
            CreatureManager.RecordCombatDamage(context.Map, creature, attacker, 10);
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        private static CreatureManager Creatures(MissionTestContext context) =>
            new CreatureManager(context, new ManifestationManager(context), context.Manager);

        private static List<XPInfo> Gains(Client client) =>
            MissionTestContext.Drain(client).OfType<ExperienceChangedPacket>().Select(packet => packet.XPInfo).ToList();

        /// <summary>A level 1 creature standing where the player does, which no mission counts.</summary>
        private static Creature AddCreature(MissionTestContext context, TargetCategory category)
        {
            var creature = new Creature
            {
                DbId = 9001,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = context.Client.Player.Position,
                Level = 1,
                State = CharacterState.Idle,
                TargetCategory = category,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool
                {
                    MapContextId = context.Map.MapInfo.MapContextId,
                    AliveCreatures = 1,
                    RespawnTime = 1000,
                    UpdateTimer = 1000
                }
            };

            CellManager.Instance.AddToWorld(context.Map, creature);
            return creature;
        }
    }
}
