namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Navigation: when each map's navmesh and cover files are read
    /// (Managers.LazyNavigation). Read at the start: a reload changes UnloadAfterSeconds, not Lazy.
    /// </summary>
    public class NavigationConfig
    {
        /// <summary>
        /// A map's navmesh and cover are read when a player is first on the way to the map, rather
        /// than every map's at the start. Off, NavMeshInit reads all of them at the start, as it
        /// always did: about 2.8 GB of the heap for the 77 maps that have files, most of them maps
        /// nobody is on. On, a player arriving on a map whose files are still being read waits on
        /// the loading screen until they are. On by default.
        /// </summary>
        public bool Lazy { get; set; } = true;

        /// <summary>
        /// With Lazy on, how long a map and every copy of it have had nobody on them, on the way to
        /// them or coming down on them in a dropship before their navmesh and cover are let go of,
        /// to be read again for the next player. 0 or less: kept once read.
        /// </summary>
        public int UnloadAfterSeconds { get; set; } = 900;
    }
}
