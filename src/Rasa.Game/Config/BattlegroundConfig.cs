namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Battleground: the numbers of the Edmund Range match (Managers.Battlegrounds).
    /// The client's own - a minute of preparation, ten minutes at least and twenty at most, level
    /// 45 to enter - are the defaults; the rest are ours.
    /// </summary>
    public class BattlegroundConfig
    {
        /// <summary>How many players each team needs before the preparation starts.</summary>
        public int MinPlayersPerTeam { get; set; } = 1;

        /// <summary>How many more players than the other team a team may have and still be joined.</summary>
        public int MaxImbalance { get; set; } = 0;

        /// <summary>The preparation before a match, in seconds.</summary>
        public int PrepSeconds { get; set; } = 60;

        /// <summary>The time a team must hold every control point to before it has won, in minutes.</summary>
        public int MinMinutes { get; set; } = 10;

        /// <summary>The longest a match runs, in minutes.</summary>
        public int MaxMinutes { get; set; } = 20;

        /// <summary>The level a player must have to enter the map.</summary>
        public int MinLevel { get; set; } = 45;

        /// <summary>How long a capture's use takes, in seconds.</summary>
        public int CaptureSeconds { get; set; } = 10;

        /// <summary>How far from a team's hospital its base reaches, in metres: the other team is kept out of it.</summary>
        public int BaseRadius { get; set; } = 45;

        /// <summary>Prestige for capturing a control point.</summary>
        public int CapturePrestige { get; set; } = 50;

        /// <summary>Prestige for each player of the team that wins a match.</summary>
        public int WinPrestige { get; set; } = 200;

        /// <summary>Prestige for each player of the team that loses one, or of both when nobody wins.</summary>
        public int LossPrestige { get; set; } = 50;
    }
}
