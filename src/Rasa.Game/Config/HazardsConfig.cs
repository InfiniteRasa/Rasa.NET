namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Hazards: the missions' radiation and toxic gas volumes (Managers.MissionHazards). Lava is not
    /// here: it burns as LavaDamage has it, at the client's own numbers. Every value has a default, so a file without the
    /// section has them; picked up by a config reload, from the next second on.
    /// </summary>
    public class HazardsConfig
    {
        /// <summary>Whether the volumes hurt at all. False: nobody is touched, and an effect already on comes off at its next tick.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Percent of each volume's damage that is dealt: 100 as the volume has it, 50 half, 0 none (the effect still goes
        /// on and shows). The volumes' numbers are ours - retail's are not in anything we have - so this is the knob.
        /// </summary>
        public int DamageScale { get; set; } = 100;
    }
}
