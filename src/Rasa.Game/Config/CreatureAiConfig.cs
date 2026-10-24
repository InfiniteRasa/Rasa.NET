namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's CreatureAi: how the creature think (Managers.BehaviorManager) is spread
    /// over the world loop's passes, and how far a creature looks for a route it may not have.
    /// Every value has a default, so a file without the section has them; picked up by a config
    /// reload.
    /// </summary>
    public class CreatureAiConfig
    {
        /// <summary>
        /// Each map's creatures think in <see cref="ThinkSlots"/> groups, each on its own beat of
        /// <see cref="ThinkPeriodMs"/> (Managers.CreatureThinkSchedule), instead of all of them on
        /// one pass.
        ///
        /// Off, the whole map thinks on the first pass that brings its clock to 250 ms, and the
        /// clock starts again at zero: with the loop's 100 ms passes, every third pass, so one pass
        /// in three carried every creature's think and the other two none. On, a third of the
        /// creatures think on every pass, each every 300 ms over the time since its own last think,
        /// as many thinks in all as before. A creature's think comes up to one period earlier or
        /// later than it would have, once, and keeps its cadence from there. On by default.
        /// </summary>
        public bool StaggeredThink { get; set; } = true;

        /// <summary>
        /// How many groups a map's creatures are dealt into by entity id (StaggeredThink). Their
        /// beats are spread evenly over the period; a multiple of the passes in a period (3 at
        /// 300 ms) gives every pass the same share. 1 is one group, beating once a period.
        /// </summary>
        public int ThinkSlots { get; set; } = 9;

        /// <summary>
        /// How often each creature thinks with StaggeredThink on, in milliseconds: 300, the cadence
        /// the whole-map think had in practice on 100 ms passes. Less makes creatures think more
        /// often than they did. A pass of 250 ms or more still thinks everyone, as it did.
        /// </summary>
        public int ThinkPeriodMs { get; set; } = 300;

        /// <summary>
        /// Every route a creature asks for is searched as a chase's is (NavMeshQuery
        /// .FindPathBounded): a goal no route reaches is searched only so far, and what the search
        /// found toward it is remembered by the map's navmesh for the next creature that asks the
        /// same. A goal that can be reached gets the route it always did.
        ///
        /// Off, only chases are bounded: a minion following its master onto ground no route reaches,
        /// a creature going home to a point it cannot get to, walk the whole of their walkable
        /// region before they give up - up to 110,000 polygons and 780 ms on Howling Maw, 320 ms on
        /// Wilderness, for a follower every 500 ms. On by default.
        /// </summary>
        public bool BoundedPathSearches { get; set; } = true;
    }
}
