namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's ControlPoints: clan-owned control points (Managers.ControlPoints). Every
    /// value has a default, so a file without the section has them on.
    /// </summary>
    public class ControlPointConfig
    {
        /// <summary>Hourly attacks on the public Wilderness Landing Zone. Private instances and other points are unaffected.</summary>
        public bool LandingZoneAssaultEnabled { get; set; } = true;
        /// <summary>Delay after startup or AFS recapture, and between assault starts.</summary>
        public int LandingZoneAssaultPeriodSeconds { get; set; } = 3600;
        /// <summary>Fixed interval between the three waves, independent of surviving attackers.</summary>
        public int LandingZoneAssaultWaveSeconds { get; set; } = 120;
        /// <summary>Survivor cleanup deadline; at least one minute after wave three.</summary>
        public int LandingZoneAssaultLifetimeSeconds { get; set; } = 900;
        /// <summary>Uninterrupted boss claim duration after the AFS combat garrison falls.</summary>
        public int LandingZoneAssaultCaptureSeconds { get; set; } = 10;

        /// <summary>Metres from a point within which friendly service NPC pools belong to it. 0 disables proximity matching; explicit links still apply.</summary>
        public float ServiceNpcRadius { get; set; } = 130f;

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
