using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Services.Preloader;
using Rasa.Structures;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class SprintVisualTests
    {
        [TestMethod]
        [DataRow(1U,120,1.5,3600)]
        [DataRow(2U,130,1.35,3600)]
        [DataRow(3U,140,1.25,3600)]
        [DataRow(4U,150,1.0,3600)]
        [DataRow(5U,160,0.9,5000)]
        public void EveryPumpUsesTheNativeBootFxWithoutChangingItsMechanics(uint pump,int speed,double drain,int duration)
        {
            using var world=new WorldTestContext();
            var client=PlayerDeathTests.Player(world,0,0);
            var level=new ActionLevelInfo { ActionId=ActionId.AaRecruitSprint, Level=pump };
            foreach(var row in new ActionPropertyPreloader().Rows().Where(r=>Convert.ToUInt32(r[1])==401 && Convert.ToUInt32(r[2])==pump))
                level.Properties[(AbilityProperty)Convert.ToUInt32(row[3])]=Convert.ToInt32(row[4]);
            var effect=GameEffectManager.Instance.AttachSprint(world.Map,client.Player,level);
            Assert.AreEqual(speed,effect.MovementModifierPercent);
            Assert.AreEqual(speed/100.0,client.Player.MovementSpeed,0.0001);
            Assert.AreEqual(drain,effect.AdrenalineDrainPercentPerSecond,0.0001);
            Assert.IsTrue(effect.RemainingSeconds>=duration-5 && effect.RemainingSeconds<=duration);
            Assert.AreEqual(ActionId.AaRecruitSprint,effect.ActionId);

            var initial=WorldTestContext.Drain(client).Select(p=>p.Message).OfType<CallMethodMessage>().Select(p=>p.Packet)
                .OfType<GameEffectAttachedPacket>().Single(p=>p.EffectTypeId==247);
            Assert.AreEqual(1U,initial.EffectLevel,"The native client has boot FX 903 only at (SPRINT 247, level 1).");
            Assert.IsTrue(GameEffectFx.HasRow(247,initial.EffectLevel));
            Assert.AreEqual(1,initial.Args.Count);
            Assert.AreEqual(1.0,Convert.ToDouble(initial.Args.Single()),0.0001,"The bead modifier is independent of the visual level.");
            var newcomer=GameEffectManager.EffectsForNewcomer(client.Player,client.Player).Single(p=>p.EffectTypeId==247);
            Assert.AreEqual(1U,newcomer.EffectLevel,"A player coming into view gets the same working FX.");
            Assert.AreEqual(initial.EffectId,newcomer.EffectId);
            GameEffectManager.Instance.DettachEffect(world.Map,client.Player,effect);
            Assert.AreEqual(1.0,client.Player.MovementSpeed,0.0001);
            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(effect.EffectId));
            Assert.IsTrue(WorldTestContext.Drain(client).Select(p=>p.Message).OfType<CallMethodMessage>().Select(p=>p.Packet)
                .OfType<GameEffectDetachedPacket>().Any(p=>p.EffectId==effect.EffectId));
        }
    }
}