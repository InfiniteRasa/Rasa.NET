namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's ControlPoints: clan-owned control points (Managers.ControlPoints). Every
    /// value has a default, so a file without the section has them on.
    /// </summary>
    public class ControlPointConfig
    {
        /// <summary>
        /// Whether a point taken from the Bane by a player in a clan is that clan's. Off, every
        /// capture is the AFS's, as before, and a point a clan holds goes back to the AFS.
        /// </summary>
        public bool ClanOwnership { get; set; } = true;

        /// <summary>Prestige paid into a clan's lockbox for each point it holds, every ClanPrestigeMinutes. 0 or less: none.</summary>
        public int ClanPrestige { get; set; } = 100;

        /// <summary>How often a held point pays, in minutes by the clock on the wall. 0 or less: never.</summary>
        public int ClanPrestigeMinutes { get; set; } = 60;

        /// <summary>How long the use takes that takes a point from the clan holding it, for a member of a clan at feud with it, in seconds.</summary>
        public int ClanCaptureSeconds { get; set; } = 30;

        /// <summary>
        /// Whether every point a clan holds goes back to the AFS at the weekly reset: on
        /// SquadInstances' WeeklyResetDay at its WeeklyResetTime, whether or not the squad
        /// instances have their reset on.
        /// </summary>
        public bool ClanWeeklyReset { get; set; } = true;
    }
}
