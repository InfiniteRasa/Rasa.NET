using System.Collections.Generic;

namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's SquadInstances: the maps that are entered as a squad's own instance
    /// (Managers.MapChannelManager's squad instances), how long one stands once it is empty, and
    /// when the weekly reset runs. Every value has a default, so a file without the section has
    /// squad instances on the built-in maps.
    /// </summary>
    public class SquadInstanceConfig
    {
        /// <summary>Off, every map is entered as its own channel, as before.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The map context ids entered as a squad's instance. Left out, the built-in list
        /// (Managers.SquadInstancePolicies.DefaultMaps: the Operations); given, it replaces
        /// that list whole.
        /// </summary>
        public List<uint> Maps { get; set; }

        /// <summary>
        /// How long an instance stands with nobody in it or on the way before it is closed.
        /// Negative, the default: it is not closed for being empty, and stands until the weekly
        /// reset. 0 closes it as it empties.
        /// </summary>
        public int EmptyCloseSeconds { get; set; } = -1;

        /// <summary>
        /// How long after the last creature of a spawn pool has died the pool comes back, in
        /// minutes by the clock on the wall, in place of the pool's own respawn time. 0 or
        /// less: not until the instance is closed.
        /// </summary>
        public int RespawnMinutes { get; set; } = 45;

        /// <summary>
        /// How long an instance stands with nobody in it or on the way before it is taken out of
        /// memory, its rows in the database kept: it is loaded again when somebody goes to it.
        /// Negative: it stays in memory.
        /// </summary>
        public int UnloadEmptySeconds { get; set; } = 300;

        /// <summary>Whether the weekly reset runs at all.</summary>
        public bool WeeklyReset { get; set; } = true;

        /// <summary>The day of the weekly reset, by its English name.</summary>
        public string WeeklyResetDay { get; set; } = "Tuesday";

        /// <summary>The time of day of the weekly reset, HH:mm on the server's own clock.</summary>
        public string WeeklyResetTime { get; set; } = "03:00";
    }
}
