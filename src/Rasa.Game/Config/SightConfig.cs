namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Sight: what a line of sight between two actors is tested against (Managers.Sight). Without
    /// the section every answer is the cover mesh's own, as before. Picked up by a config reload, from the next line.
    /// </summary>
    public class SightConfig
    {
        /// <summary>
        /// Whether the terrain's height grid is left out of a line of sight that has an end underground - in a cave
        /// or a tunnel the surface runs over, by the map's own navmesh - so that only the collision counts there.
        /// Off, the grid blocks every line longer than a grid cell that runs under it: in a cave a ranged hit does a
        /// tenth of its damage, a creature never shoots and a selected target is wholly behind cover.
        /// </summary>
        public bool UnderTerrain { get; set; }
    }
}
