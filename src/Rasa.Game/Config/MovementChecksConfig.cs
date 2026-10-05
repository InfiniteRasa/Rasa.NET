namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's MovementChecks: what the server does with a Move that passes through a
    /// force field, through the map's collision, or that leaves the player standing on nothing
    /// (Managers.MovementChecks). Each is "off", "log" or "refuse". Picked up by a config reload.
    ///
    /// Log is the setting to run a new map or a new check on first: it writes the Security line
    /// and changes nothing, so a wall the client lets players through and the server's mesh
    /// does not shows up in the log as a pattern across honest players rather than as those
    /// players being put back.
    /// </summary>
    public class MovementChecksConfig
    {
        public const string Off = "off";
        public const string Log = "log";
        public const string Refuse = "refuse";

        /// <summary>A step through an intact force field of the side that stops players. The field's own box, so no honest step crosses it.</summary>
        public string ForceFields { get; set; } = Refuse;

        /// <summary>A level step whose chest-height ray crosses a collision triangle: through a wall.</summary>
        public string Geometry { get; set; } = Log;

        /// <summary>Standing still or rising with nothing within reach underneath, for longer than a jump: in the air.</summary>
        public string Hover { get; set; } = Log;
    }
}
