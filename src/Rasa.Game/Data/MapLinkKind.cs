namespace Rasa.Data
{
    /// <summary>Matches map_link.kind and the client's map-marker types for links.</summary>
    public enum MapLinkKind : byte
    {
        /// <summary>A pass between two zones (client marker BATTLEFIELD_ENTRANCE).</summary>
        Border = 0,

        /// <summary>A door into an instance map, or the way back out: either end is a mission-context map.</summary>
        Instance = 1,

        /// <summary>The Red team's teleporter in a battleground's staging area: joins the team and goes to its base (Battlegrounds).</summary>
        TeamRed = 2,

        /// <summary>The Blue team's teleporter in a battleground's staging area.</summary>
        TeamBlue = 3,

        /// <summary>The way from a team's base back to the staging area, off the team.</summary>
        TeamLeave = 4,

        /// <summary>
        /// Where the winning team of a battleground's match is put when it ends: the link's
        /// arrival (Battlegrounds.WinnersArrival). Nobody walks into it; the row is disabled.
        /// </summary>
        TeamWinners = 5
    }
}
