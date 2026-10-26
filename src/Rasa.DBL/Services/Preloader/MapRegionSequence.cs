namespace Rasa.Services.Preloader
{
    /// <summary>
    /// map_region's ids are AUTOINCREMENT on SQLite: a row taken out leaves its id used, and the
    /// row put back is given the next one. The region migrations insert their rows without ids,
    /// so a rollback and a reapply gave every one of them a new id, and the world did not come
    /// back as it was (MigrationConsolidationTests). The SQLite migrations' Down sets the counter
    /// back to the highest id left once their rows are out, so the rows come back with the same
    /// ids. MySQL has no such table; its migrations are left as they are.
    /// </summary>
    public static class MapRegionSequence
    {
        public const string Sqlite =
            "UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(id), 0) FROM map_region) WHERE name = 'map_region';";
    }
}
