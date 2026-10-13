using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;

    /// <summary>
    /// The clan window's Promote and Demote offers (socialwindow.py: the CLAN_RANK_3 options,
    /// cumulative from the caller's rank down), as the server allows them.
    /// </summary>
    [TestClass]
    public class ClanRankTests
    {
        [TestMethod]
        public void PromoteIsOfferedToTheLeaderAndTheRankBelowOnMembersTwoRanksDown()
        {
            // (caller, target): the leader on ranks 0 and 1; the rank below on rank 0.
            var offered = new HashSet<(byte, byte)> { (3, 0), (3, 1), (2, 0) };

            foreach (var (actor, target) in Pairs())
                Assert.AreEqual(offered.Contains((actor, target)), ClanRank.MayPromote(actor, target), $"{actor} promoting {target}");
        }

        [TestMethod]
        public void DemoteIsOfferedToTheLeaderAndTheRankBelowOnMembersUnderThemAboveTheFloor()
        {
            // (caller, target): the leader on ranks 1 and 2; the rank below on rank 1.
            var offered = new HashSet<(byte, byte)> { (3, 1), (3, 2), (2, 1) };

            foreach (var (actor, target) in Pairs())
                Assert.AreEqual(offered.Contains((actor, target)), ClanRank.MayDemote(actor, target), $"{actor} demoting {target}");
        }

        [TestMethod]
        public void NobodyIsPromotedIntoLeader()
        {
            foreach (var (actor, target) in Pairs().Where(pair => pair.target + 1 >= ClanRank.Leader))
                Assert.IsFalse(ClanRank.MayPromote(actor, target), $"{actor} promoting {target}");
        }

        private static IEnumerable<(byte actor, byte target)> Pairs()
        {
            for (byte actor = ClanRank.Member; actor <= ClanRank.Leader; actor++)
                for (byte target = ClanRank.Member; target <= ClanRank.Leader; target++)
                    yield return (actor, target);
        }
    }
}
