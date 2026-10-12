using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The player's own pain and critical-hit lines (PlayerVoices): which hits are cried at, how
    // often, the crit over the pain, the voice-overs they keep out of, and the sets by sex.
    [TestClass]
    [DoNotParallelize]
    public class PlayerVoiceTests
    {
        private Func<long> _now;
        private long _clock;

        [TestInitialize]
        public void Initialize()
        {
            _now = PlayerVoices.Now;
            _clock = 1_000_000;
            PlayerVoices.Now = () => _clock;
        }

        [TestCleanup]
        public void Cleanup() => PlayerVoices.Now = _now;

        [TestMethod]
        public void TheSetsAreTheClientsPainAndGetHitVoicesBySex()
        {
            Assert.AreEqual((24u, 466u), (PlayerVoices.MalePain, PlayerVoices.FemalePain));
            Assert.AreEqual((142u, 137u), (PlayerVoices.MaleCritical, PlayerVoices.FemaleCritical));

            var man = new Manifestation { Gender = 0 };
            var woman = new Manifestation { Gender = 1 };

            Assert.AreEqual(PlayerVoices.MalePain, PlayerVoices.PainSet(man));
            Assert.AreEqual(PlayerVoices.FemalePain, PlayerVoices.PainSet(woman));
            Assert.AreEqual(PlayerVoices.MaleCritical, PlayerVoices.CriticalSet(man));
            Assert.AreEqual(PlayerVoices.FemaleCritical, PlayerVoices.CriticalSet(woman));
        }

        [TestMethod]
        public void AHitOfAtLeastTheShareIsPainOnceInTheGapAndASmallerOneIsNot()
        {
            var player = Standing(gender: 1);

            // 5 % of 1000 is 50.
            PlayerVoices.Hurt(player, 49);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "too little");
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "and taken, not kept for the next");

            // What lands in one tick is added up.
            PlayerVoices.Hurt(player, 30);
            PlayerVoices.Hurt(player, 20);
            Assert.AreEqual(PlayerVoices.FemalePain, PlayerVoices.LineFor(player));

            _clock += PlayerVoices.PainGapMs - 1;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "within the gap");

            _clock += 1;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.FemalePain, PlayerVoices.LineFor(player));
        }

        [TestMethod]
        public void ACriticalHitIsItsOwnLineOverThePainAndStartsThePainGapOver()
        {
            var player = Standing(gender: 0);

            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.MalePain, PlayerVoices.LineFor(player));

            // In the pain gap, a crit - however small - is said, and over a hit's pain in the same tick.
            _clock += 1000;
            PlayerVoices.Hurt(player, 1, critical: true);
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.MaleCritical, PlayerVoices.LineFor(player));

            // Its own gap.
            _clock += PlayerVoices.CritGapMs - 1;
            PlayerVoices.Critical(player);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player));

            _clock += 1;
            PlayerVoices.Critical(player);
            Assert.AreEqual(PlayerVoices.MaleCritical, PlayerVoices.LineFor(player));

            // And the pain gap runs from it.
            _clock += PlayerVoices.PainGapMs - 1;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player));

            _clock += 1;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.MalePain, PlayerVoices.LineFor(player));
        }

        [TestMethod]
        public void NothingIsSaidByTheFallenOrOverAVoiceOver()
        {
            var player = Standing(gender: 0);

            player.Attributes[Attributes.Health].Current = 0;
            PlayerVoices.Hurt(player, 500, critical: true);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "brought down");

            player.Attributes[Attributes.Health].Current = 600;
            player.State = CharacterState.Dead;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "dead");

            player.State = CharacterState.Idle;
            PlayerVoices.VoiceOverSent(player, stopped: false);
            _clock += PlayerVoices.VoiceOverHoldMs - 1;
            PlayerVoices.Hurt(player, 500, critical: true);
            Assert.AreEqual(0u, PlayerVoices.LineFor(player), "a mission's voice-over is playing");

            _clock += 1;
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.MalePain, PlayerVoices.LineFor(player));

            // A voice-over stopped holds nothing back.
            _clock += PlayerVoices.PainGapMs;
            PlayerVoices.VoiceOverSent(player, stopped: false);
            PlayerVoices.VoiceOverSent(player, stopped: true);
            PlayerVoices.Hurt(player, 500);
            Assert.AreEqual(PlayerVoices.MalePain, PlayerVoices.LineFor(player));
        }

        [TestMethod]
        public void ACreaturesShotIsCriedAtTheEndOfTheTickAndAKillingOneIsNot()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var bane = Creature(world, 5);

            Shoot(world, bane, client, 300, critical: false);
            Assert.IsEmpty(Lines(client), "not until the tick ends");

            PlayerVoices.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { PlayerVoices.MalePain }, Lines(client));

            _clock += PlayerVoices.CritGapMs;
            Shoot(world, bane, client, 100, critical: true);
            PlayerVoices.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { PlayerVoices.MaleCritical }, Lines(client));

            // The shot that brings them down says nothing.
            _clock += 60_000;
            Shoot(world, bane, client, 5000, critical: true);
            PlayerVoices.Worker(world.Map);
            Assert.IsEmpty(Lines(client));
        }

        [TestMethod]
        public void AHitThroughTheActorManagerIsCriedAndATickIsNot()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Gender = 1;
            var bane = Creature(world, 5);

            ActorManager.Instance.Damage(world.Map, client.Player, 200, bane, DamageType.Fire, isPeriodic: true);
            PlayerVoices.Worker(world.Map);
            Assert.IsEmpty(Lines(client), "a tick");

            ActorManager.Instance.Damage(world.Map, client.Player, 200, bane, DamageType.Fire);
            PlayerVoices.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { PlayerVoices.FemalePain }, Lines(client));

            // A crit's side effect on them says it was one.
            _clock += PlayerVoices.CritGapMs;
            CritEffects.OnCritical(world.Map, client.Player, bane, DamageType.Physical, 100);
            PlayerVoices.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { PlayerVoices.FemaleCritical }, Lines(client));
        }

        [TestMethod]
        public void AMissionsVoiceOverHoldsThemBack()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var bane = Creature(world, 5);

            CommunicatorManager.Instance.PlayTutorialAudio(client, 3001);
            WorldTestContext.Drain(client);

            Shoot(world, bane, client, 300, critical: true);
            PlayerVoices.Worker(world.Map);
            Assert.IsEmpty(Lines(client));

            CommunicatorManager.Instance.StopTutorialAudio(client);
            WorldTestContext.Drain(client);

            Shoot(world, bane, client, 300, critical: false);
            PlayerVoices.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { PlayerVoices.MalePain }, Lines(client));
        }

        private static Manifestation Standing(uint gender)
        {
            var player = new Manifestation { Gender = gender, State = CharacterState.Idle };
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            return player;
        }

        private static Creature Creature(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Bane",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(world.Map, creature);

            foreach (var client in world.Map.ClientList)
                WorldTestContext.Drain(client);

            return creature;
        }

        private static void Shoot(WorldTestContext world, Creature source, Client target, int damage, bool critical)
        {
            var missile = new Missile
            {
                Source = source,
                TargetEntityId = target.Player.EntityId,
                TargetActor = target.Player,
                DamageA = damage,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = critical ? 100 : 0,
                Missed = false
            };

            MissileManager.Instance.MissileTrigger(world.Map, missile);
            Assert.AreEqual(critical, missile.IsCritical, "the roll");
        }

        /// <summary>The audio sets the client has been told to play since it was last asked.</summary>
        private static List<uint> Lines(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<PlayTutorialAudioPacket>()
                .Select(packet => packet.AudioSetId ?? 0).ToList();
    }
}
