using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Gameplay
{
    [TestClass, DoNotParallelize]
    public class PlayerHealthRecoveryTests
    {
        [TestMethod]
        [DataRow(9)]
        [DataRow(286)]
        [DataRow(1001)]
        [DataRow(20000)]
        [DataRow(int.MaxValue)]
        public void CombatExitRefillsFromOneHpWithinTenSecondsAndMatchesClientPrediction(int maximum)
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient();
            var player = client.Player;
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health,maximum,maximum,1,2,1);
            player.HealthRegenRate = 2;
            player.InCombat = true;
            ManifestationManager.Instance.ExitCombat(client);
            var sent = new AttributeInfoPacket(player).ActorAttributes[Attributes.Health];
            Assert.AreEqual(1,sent.RefreshPeriod);
            var predicted = 1L;
            for(var second=1;second<=10;second++)
            {
                ActorManager.Instance.Regenerate(context.World.Map);
                predicted = Math.Min(maximum,predicted+sent.RefreshAmount);
                Assert.AreEqual((int)predicted,player.Attributes[Attributes.Health].Current,"second "+second);
            }
            Assert.AreEqual(maximum,player.Attributes[Attributes.Health].Current);
            var update = new UpdateHealthPacket(player.Attributes[Attributes.Health],0);
            Assert.AreEqual(sent.RefreshAmount,update.Health.RefreshAmount,"ordinary health updates preserve the client's rate");
        }

        [TestMethod]
        public void ReenteringCombatRestoresTheOrdinaryRateAndRecalculationDoesNotMakeItFast()
        {
            using var context = new ProgressionTestContext(); var client=context.CreateClient(20);
            ManifestationManager.Instance.UpdateStatsValues(client,true);
            var player=client.Player; var health=player.Attributes[Attributes.Health]; var ordinary=player.HealthRegenRate.Value;
            var powerRate=player.Attributes[Attributes.Power].RefreshAmount;
            health.Current=1;
            ManifestationManager.Instance.EnterCombat(client);
            ManifestationManager.Instance.UpdateStatsValues(client,false);
            Assert.AreEqual(ordinary,health.RefreshAmount);
            Assert.AreEqual(5,health.RefreshPeriod);
            player.RegenSeconds=0;
            for(var i=0;i<4;i++)ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(1,health.Current);
            ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(1+ordinary,health.Current);
            Assert.AreEqual(powerRate,player.Attributes[Attributes.Power].RefreshAmount);
            player.CombatExpiresAt=0;
            ManifestationManager.Instance.CombatWorker(context.World.Map);
            Assert.IsFalse(player.InCombat);
            for(var i=0;i<10;i++)ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(health.CurrentMax,health.Current);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void MaximumHealthBuffChangesTheRestingRateAndRevertsIt(bool percent)
        {
            using var context=new ProgressionTestContext(); var client=context.CreateClient(20);
            ManifestationManager.Instance.UpdateStatsValues(client,true);
            var player=client.Player;var health=player.Attributes[Attributes.Health];var before=health.RefreshAmount;
            Rasa.Game.Server.Clients.Add(client);
            try {
            var effect=Effect(context,player);if(percent)effect.MaxHealthPercent=100;else effect.MaxHealthPoints=1000;
            GameEffectManager.Instance.Attach(context.World.Map,player,effect);
            Assert.IsTrue(health.RefreshAmount>before);
            health.Current=1;
            for(var i=0;i<10;i++)ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(health.CurrentMax,health.Current);
            GameEffectManager.Instance.DettachEffect(context.World.Map,player,effect);
            Assert.AreEqual(before,health.RefreshAmount);
            } finally { Rasa.Game.Server.Clients.Remove(client); }
        }

        [TestMethod]
        public void RegenEffectsStillScaleRecoveryAndTheDeadDoNotRegenerate()
        {
            using var context=new ProgressionTestContext(); var client=context.CreateClient(20);
            ManifestationManager.Instance.UpdateStatsValues(client,true);
            var player=client.Player;var health=player.Attributes[Attributes.Health];var baseline=health.RefreshAmount;
            var effect=Effect(context,player); effect.HealthRegenPercent=100;
            GameEffectManager.Instance.Attach(context.World.Map,player,effect);
            Assert.AreEqual(2*baseline,new AttributeInfoPacket(player).ActorAttributes[Attributes.Health].RefreshAmount);
            health.Current=1; ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(1+2*baseline,health.Current);
            GameEffectManager.Instance.DettachEffect(context.World.Map,player,effect);
            effect=Effect(context,player);effect.HealthRegenPercent=-100;
            GameEffectManager.Instance.Attach(context.World.Map,player,effect);
            var before=health.Current;ActorManager.Instance.Regenerate(context.World.Map);Assert.AreEqual(before,health.Current);
            GameEffectManager.Instance.DettachEffect(context.World.Map,player,effect);
            player.State=CharacterState.Dead;health.Current=0;
            ManifestationManager.Instance.ApplyHealthRegen(player);
            for(var i=0;i<10;i++)ActorManager.Instance.Regenerate(context.World.Map);
            Assert.AreEqual(0,health.Current);Assert.AreEqual(0,health.RefreshAmount);
            var creature=new Creature();creature.Attributes[Attributes.Health]=new ActorAttributes(Attributes.Health,1000,1000,1,7,1);
            Assert.AreEqual(7,GameEffectManager.WithRegen(creature,creature.Attributes[Attributes.Health]).RefreshAmount);
        }
        private static GameEffect Effect(ProgressionTestContext context, Manifestation player)=>new()
        {
            TypeId=10000019,EffectId=GameEffectManager.Instance.NextEffectId(context.World.Map),EffectLevel=1,
            SourceId=player.EntityId,Source=player,SourceLevel=player.Level,ExpiresTick=long.MaxValue,AnnounceOnAttach=true
        };
    }
}
