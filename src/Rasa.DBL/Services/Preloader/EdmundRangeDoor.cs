namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The way into Edmund Range (2374, adv_wargame_edmundrange2), which nothing in the client's
    /// map data leads to: a door in the CELLAR Arena (20000009) where the client draws the link
    /// to the Edmund Range it shipped before this one, and the way back out of the range's
    /// staging area. The range's two places are set by hand from its navmesh - the alcove at
    /// the south end of the staging area - and want a walk-through.
    ///
    /// The rows are 9001 and 9002: the links shipped first are 1-141 and a game master's own
    /// (.linkhere) take the ids after the highest there is, so the ones just above 141 may
    /// already be somebody's.
    /// </summary>
    public static class EdmundRangeDoor
    {
        public const uint InId = 9001;
        public const uint OutId = 9002;

        private const string Columns =
            "id, map_context_id, pos_x, pos_y, pos_z, radius, dest_map_context_id, dest_pos_x, dest_pos_y, dest_pos_z, dest_rotation, kind, enabled, comment";

        public static readonly string[] InsertStatements =
        {
            $"insert into {MapLinkEntry.TableName} ({Columns}) values ({InId}, 20000009, 9.2683, 40.5004, 136.06, 4.0, 2374, -62.5, 364.2, -412.0, 3.1416, 1, 1, 'afs_arena -> edmundrange2');",
            $"insert into {MapLinkEntry.TableName} ({Columns}) values ({OutId}, 2374, -62.5, 364.2, -421.5, 3.5, 20000009, 9.2683, 40.5004, 136.06, 0.0344, 1, 1, 'edmundrange2 -> afs_arena');"
        };

        public static readonly string DeleteStatement =
            $"delete from {MapLinkEntry.TableName} where id in ({InId}, {OutId});";
    }
}
