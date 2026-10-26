using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// An effect put on at a pump or ability level is sent at a level the client has visuals for
    /// (GameEffectFx.LevelFor): the client looks its FX up by (type, level) exactly.
    /// </summary>
    [TestClass]
    public class GameEffectFxTests
    {
        [TestMethod]
        [DataRow(AbilityManager.LightningStormTypeId, 3u, 1u, "a P3 storm has the P1 cloud")]
        [DataRow(AbilityManager.LightningStormTypeId, 1u, 1u, "P1 as it was")]
        [DataRow(394, 5u, 1u, "the P5 napalm pool")]
        [DataRow(396, 5u, 1u, "the P5 napalm bomb")]
        [DataRow(396, 6u, 6u, "level 6 has its own row")]
        [DataRow(393, 2u, 1u, "a P2 air strike beacon")]
        [DataRow(393, 3u, 3u, "the P3 one has its own")]
        [DataRow(AbilityManager.HortimonculusUseTypeId, 4u, 1u, "a P4 plant's heal")]
        [DataRow(CreatureEffectAttacks.HunterNetTypeId, 3u, 1u, "CR_HUNTER_NET level 3")]
        [DataRow(CreatureEffectAttacks.HunterNetTypeId, 13u, 13u, "and its electric row")]
        [DataRow(CreatureEffectAttacks.XanxWebTypeId, 2u, 1u, "CR_XANX_V2_WEB level 2")]
        [DataRow(CreatureBombs.LinkerGroundBlastTypeId, 4u, 1u, "a Linker's ground blast")]
        [DataRow(CreatureEffectAttacks.PheromoneTypeId, 2u, 1u, "a Phuumz pheromone")]
        [DataRow(CreatureEffectAttacks.DecayTypeId, 3u, 3u, "Decay has every pump")]
        [DataRow(1, 3u, 3u, "DING level 3 is the Eloh takeover, not level 1")]
        [DataRow(1, 2u, 1u, "DING level 2 has none: the level-up flash")]
        [DataRow(10000016, 2u, 2u, "the net gun DoT has no level 1 to fall back to")]
        [DataRow(CreatureBombs.HowlerDeathTypeId, 5u, 5u, "nor has the Howler's death")]
        [DataRow(424242, 4u, 4u, "a type the client has no rows for")]
        public void ALevelWithNoRowIsSentAtTheClientsLevelOne(int typeId, uint level, uint sent, string why)
        {
            Assert.AreEqual(sent, GameEffectFx.LevelFor(typeId, level), why);
        }

        [TestMethod]
        public void EveryTypeInTheTableHasLevelOneAndNoLevelTwice()
        {
            Assert.IsGreaterThan(100, GameEffectFx.Levels.Count);

            foreach (var (typeId, levels) in GameEffectFx.Levels)
            {
                Assert.AreEqual(1u, levels[0], $"type {typeId}");
                CollectionAssert.AreEqual(levels.Distinct().OrderBy(l => l).ToArray(), levels, $"type {typeId}");
                Assert.IsTrue(GameEffectFx.HasRow(typeId, 1));
            }

            Assert.IsFalse(GameEffectFx.HasRow(AbilityManager.LightningStormTypeId, 2));
        }

        [TestMethod]
        public void ACreaturesNetAndWebAreSentAtTheLevelTheyAreDrawnAt()
        {
            var attacker = new Creature { Level = 20 };
            var missile = new Missile { Source = attacker, CreatureAction = new CreatureAction { MinDamage = 10, MaxDamage = 20 } };

            var net = CreatureEffectAttacks.Build(null, attacker, missile, "abilities.ai.hunternetability", CreatureEffectAttacks.Kind.Hold,
                new ActionLevelInfo { ActionId = ActionId.CrHunterNet, Level = 3 });
            var web = CreatureEffectAttacks.Build(null, attacker, missile, "abilities.ai.xanxwebability", CreatureEffectAttacks.Kind.Hold,
                new ActionLevelInfo { ActionId = ActionId.CrHunterNet, Level = 2 });

            Assert.AreEqual((CreatureEffectAttacks.HunterNetTypeId, 1u), (net.TypeId, net.EffectLevel));
            Assert.AreEqual((CreatureEffectAttacks.XanxWebTypeId, 1u), (web.TypeId, web.EffectLevel));
        }
    }
}
