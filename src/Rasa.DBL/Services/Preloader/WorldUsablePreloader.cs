using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// A lot of world_usable rows for a migration to put in (the game server's PlacedUsables), as
    /// WorldDestructiblePreloader does for world_destructible: a class that yields its rows with
    /// the helpers below, Preload in the migration's Up and <see cref="Remove"/> in its Down.
    ///
    /// The columns are spelled out, so a migration written against them keeps inserting what the
    /// table had when it was written if a later one adds a column.
    /// </summary>
    public abstract class WorldUsablePreloader : PreloaderBase, IPreloader
    {
        public static readonly string[] Columns =
        {
            "id", "map_context_id", "class_id", "pos_x", "pos_y", "pos_z", "rotation", "state", "usable",
            "item_template_id", "item_class_id", "item_count", "reset_seconds", "interval_min_seconds", "interval_max_seconds",
            "damage_min", "damage_max", "radius", "action_arg", "comment"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, WorldUsableEntry.TableName, Columns);
        }

        /// <summary>Deletes this lot's rows, by id.</summary>
        public void Remove(MigrationBuilder migrationBuilder)
        {
            var ids = GetRows().Select(row => (uint)row[0]).ToList();

            if (ids.Count > 0)
                migrationBuilder.Sql($"delete from {WorldUsableEntry.TableName} where id in ({string.Join(", ", ids)});");
        }

        /// <summary>
        /// Any object: its id (unique across every lot), map, class, where it stands and which way it
        /// faces, and what it is for; the rest are 0 for its kind's own (WorldUsableEntry).
        /// </summary>
        protected static object[] Row(uint id, uint mapContextId, uint classId, double x, double y, double z, double rotation, string comment,
            uint state = 0, bool usable = false, uint itemTemplateId = 0, uint itemClassId = 0, uint itemCount = 0, uint resetSeconds = 0,
            uint intervalMinSeconds = 0, uint intervalMaxSeconds = 0, uint damageMin = 0, uint damageMax = 0, double radius = 0, uint actionArg = 0) => new object[]
        {
            id, mapContextId, classId, x, y, z, rotation, state, usable ? 1u : 0u, itemTemplateId, itemClassId, itemCount, resetSeconds,
            intervalMinSeconds, intervalMaxSeconds, damageMin, damageMax, radius, actionArg, comment ?? ""
        };

        /// <summary>A switch or a door: its start state (0 for its kind's), and whether a player may use it.</summary>
        protected static object[] Switch(uint id, uint mapContextId, uint classId, double x, double y, double z, double rotation, string comment,
            uint state = 0, bool usable = true, uint resetSeconds = 0) =>
            Row(id, mapContextId, classId, x, y, z, rotation, comment, state: state, usable: usable, resetSeconds: resetSeconds);

        /// <summary>An item dispenser that gives <paramref name="count"/> of an item template, active again <paramref name="resetSeconds"/> later.</summary>
        protected static object[] Dispenser(uint id, uint mapContextId, uint classId, double x, double y, double z, double rotation, string comment,
            uint itemTemplateId, uint count = 1, uint resetSeconds = 0) =>
            Row(id, mapContextId, classId, x, y, z, rotation, comment, usable: true, itemTemplateId: itemTemplateId, itemCount: count, resetSeconds: resetSeconds);

        /// <summary>An item acceptor that takes <paramref name="count"/> items of an entity class, ready again <paramref name="resetSeconds"/> later.</summary>
        protected static object[] Acceptor(uint id, uint mapContextId, uint classId, double x, double y, double z, double rotation, string comment,
            uint itemClassId, uint count = 1, uint resetSeconds = 0) =>
            Row(id, mapContextId, classId, x, y, z, rotation, comment, usable: true, itemClassId: itemClassId, itemCount: count, resetSeconds: resetSeconds);

        /// <summary>A random explosion going off every <paramref name="minSeconds"/> to <paramref name="maxSeconds"/>.</summary>
        protected static object[] Explosion(uint id, uint mapContextId, uint classId, double x, double y, double z, string comment,
            uint minSeconds = 0, uint maxSeconds = 0, uint damageMin = 0, uint damageMax = 0, double radius = 0, uint actionArg = 0) =>
            Row(id, mapContextId, classId, x, y, z, 0, comment, intervalMinSeconds: minSeconds, intervalMaxSeconds: maxSeconds,
                damageMin: damageMin, damageMax: damageMax, radius: radius, actionArg: actionArg);
    }
}
