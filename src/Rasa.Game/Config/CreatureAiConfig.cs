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
        /// the think interval (Managers.CreatureThinkSchedule), instead of all of them on one pass.
        ///
        /// Off, the whole map thinks on the first pass that brings its clock to 250 ms, and with
        /// the loop's 100 ms passes that is every third pass: one pass in three carried every
        /// creature's think, the other two none. On, two in five of the creatures think on every
        /// pass, each over the time since its own last think, and each every 250 ms on average -
        /// 200 and 300 ms in turn - where the whole-map think came every 300 ms: a fifth more
        /// thinks in all, and a pass's share of them two fifths of what the busiest pass had. A
        /// think comes up to one interval earlier or later than it would have. On by default.
        /// </summary>
        public bool StaggeredThink { get; set; } = true;

        /// <summary>
        /// How many groups a map's creatures are dealt into by entity id (StaggeredThink). Their
        /// beats are spread evenly over the interval, so more groups level the passes out more
        /// finely; 1 is one group, beating once an interval as the map did, but phase-locked.
        /// </summary>
        public int ThinkSlots { get; set; } = 10;
    }
}
