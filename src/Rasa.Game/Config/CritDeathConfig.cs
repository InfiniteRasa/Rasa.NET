namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's CritDeath: how often a player's killing blow leaves a creature staggering for a Critical Death
    /// finisher instead of dead, and whether a morphed player has Critical Deaths at all (Managers.CritDeathManager). Every
    /// value has a default, so a file without the section has them; picked up by a config reload, from the next blow on.
    ///
    /// The retail help text: "Sometimes, when an enemy is stunned and near death, a red skull icon may appear above it.
    /// During this time, you have an opportunity to get in close and perform a melee attack ... This will initiate a
    /// Critical Death." The window opens for a creature that is both stunned (a grenade, a shotgun, a Hand to Hand
    /// knockback, an Ice or Sonic crit) and at 8 percent of its health or less, and plain weapon fire stuns nothing, so in
    /// an ordinary fight with a pistol or a rifle the window almost never opens.
    /// </summary>
    public class CritDeathConfig
    {
        /// <summary>
        /// Of a hundred killing blows a player lands on a creature (the hit that would take its health to zero, not a tick
        /// of damage over time), how many leave it on one health instead, staggering, with its Critical Death window open at
        /// once: the skull, 6.9 seconds (CritDeathManager.WindowMs) for the player or their squad to get close and press
        /// melee, and the kill (experience, loot, mission credit) still theirs when it runs out. 0: none, and the window
        /// opens only for a stunned creature at or below 8 percent of its health, as without this key. 100: every one.
        /// </summary>
        public int StaggerChancePercent { get; set; } = 25;

        /// <summary>
        /// Whether a morphed player (AbilityManager.IsMorphed: a Polymorph into a creature's shape, or a Personal Armored
        /// Unit - the Angel and Vulcan activators, the AFS Mech, Grendel, Orson) opens Critical Death windows and finishes
        /// them. False: a morphed player's blows kill outright and the finisher is refused to them - kills made by a mech
        /// or anything else piloted are plain kills. True: as any other player.
        /// </summary>
        public bool FromMorphedPlayers { get; set; }
    }
}
