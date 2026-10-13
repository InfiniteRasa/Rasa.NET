using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// A lot of world_destructible rows for a migration to put in (the game server's
    /// PlacedDestructibles), as the spawn pools of NPCs are put in: a class that yields its rows
    /// with <see cref="Row"/>, Preload in the migration's Up and <see cref="Remove"/> in its Down.
    ///
    /// The columns are spelled out, so a migration written against them keeps inserting what the
    /// table had when it was written if a later one adds a column.
    /// </summary>
    public abstract class WorldDestructiblePreloader : PreloaderBase, IPreloader
    {
        public static readonly string[] Columns =
        {
            "id", "map_context_id", "class_id", "pos_x", "pos_y", "pos_z", "rotation",
            "hit_points", "creature_id", "creature_count", "side", "comment"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, WorldDestructibleEntry.TableName, Columns);
        }

        /// <summary>Deletes this lot's rows, by id.</summary>
        public void Remove(MigrationBuilder migrationBuilder)
        {
            var ids = GetRows().Select(row => (uint)row[0]).ToList();

            if (ids.Count > 0)
                migrationBuilder.Sql($"delete from {WorldDestructibleEntry.TableName} where id in ({string.Join(", ", ids)});");
        }

        /// <summary>
        /// One object: its id (unique across every lot), map, class, where it stands and which way
        /// it faces, and what it is for. The rest are 0 for its class's own: hit points; a creature
        /// spawner's creature (creature.id) and how many come out at a time; a force field's side
        /// (WorldDestructibleEntry.SideAfs or SideBane).
        /// </summary>
        protected static object[] Row(uint id, uint mapContextId, uint classId, double x, double y, double z, double rotation, string comment,
            uint hitPoints = 0, uint creatureId = 0, uint creatureCount = 0, uint side = WorldDestructibleEntry.SideDefault) => new object[]
        {
            id, mapContextId, classId, x, y, z, rotation, hitPoints, creatureId, creatureCount, side, comment ?? ""
        };
    }
}
