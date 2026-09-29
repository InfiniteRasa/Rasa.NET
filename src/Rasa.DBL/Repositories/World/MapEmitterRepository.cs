using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IMapEmitterRepository
    {
        List<MapEmitterEntry> GetMapEmitters();

        /// <returns>The new row's id, or 0 when the insert failed.</returns>
        uint AddMapEmitter(MapEmitterEntry entry);

        /// <returns>false when no row has that id or the update failed.</returns>
        bool UpdateMapEmitter(MapEmitterEntry entry);

        /// <returns>false when no row has that id or the delete failed.</returns>
        bool DeleteMapEmitter(uint id);
    }

    /// <summary>
    /// The writes report failure instead of throwing: they run from GM chat commands, inside a
    /// packet handler, where an exception would cost the GM their connection over a bad row.
    /// </summary>
    public class MapEmitterRepository : IMapEmitterRepository
    {
        private readonly WorldContext _worldContext;

        public MapEmitterRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<MapEmitterEntry> GetMapEmitters()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.MapEmitterEntries);

            return query.ToList();
        }

        public uint AddMapEmitter(MapEmitterEntry entry)
        {
            try
            {
                _worldContext.MapEmitterEntries.Add(entry);
                _worldContext.SaveChanges();

                return entry.Id;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding map emitter:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }
        }

        public bool UpdateMapEmitter(MapEmitterEntry entry)
        {
            try
            {
                var row = _worldContext.GetWritable(_worldContext.MapEmitterEntries, entry.Id);

                if (row == null)
                    return false;

                row.MapContextId = entry.MapContextId;
                row.PosX = entry.PosX;
                row.PosY = entry.PosY;
                row.PosZ = entry.PosZ;
                row.Rotation = entry.Rotation;
                row.PackageId = entry.PackageId;
                row.IsOn = entry.IsOn;
                row.Comment = entry.Comment;

                _worldContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error updating map emitter:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        public bool DeleteMapEmitter(uint id)
        {
            try
            {
                var row = _worldContext.GetWritable(_worldContext.MapEmitterEntries, id);

                if (row == null)
                    return false;

                _worldContext.MapEmitterEntries.Remove(row);
                _worldContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error deleting map emitter:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }
    }
}
