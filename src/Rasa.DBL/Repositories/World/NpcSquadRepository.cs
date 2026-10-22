using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    /// <summary>The NPC squads' rows: their types and members, routes and points, and spawns (NpcSquads).</summary>
    public interface INpcSquadRepository
    {
        List<NpcSquadTypeEntry> GetTypes();
        List<NpcSquadMemberEntry> GetMembers();
        List<NpcSquadPathEntry> GetPaths();
        List<NpcSquadPathPointEntry> GetPathPoints();
        List<NpcSquadSpawnEntry> GetSpawns();

        /// <summary>The type's row and its members, as given: the members it had before go.</summary>
        bool SaveType(NpcSquadTypeEntry type, IReadOnlyList<NpcSquadMemberEntry> members);

        /// <summary>The type, its members and every spawn of it.</summary>
        bool DeleteType(uint id);

        /// <summary>The path's row and its points, as given: the points it had before go.</summary>
        bool SavePath(NpcSquadPathEntry path, IReadOnlyList<NpcSquadPathPointEntry> points);

        /// <summary>The path and its points; the spawns that walked it hold their spots from now on.</summary>
        bool DeletePath(uint id);

        bool SaveSpawn(NpcSquadSpawnEntry spawn);

        bool DeleteSpawn(uint id);
    }

    public class NpcSquadRepository : INpcSquadRepository
    {
        private readonly WorldContext _worldContext;

        public NpcSquadRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<NpcSquadTypeEntry> GetTypes() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.NpcSquadTypeEntries).OrderBy(e => e.Id).ToList();

        public List<NpcSquadMemberEntry> GetMembers() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.NpcSquadMemberEntries).OrderBy(e => e.TypeId).ThenBy(e => e.Slot).ToList();

        public List<NpcSquadPathEntry> GetPaths() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.NpcSquadPathEntries).OrderBy(e => e.Id).ToList();

        public List<NpcSquadPathPointEntry> GetPathPoints() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.NpcSquadPathPointEntries).OrderBy(e => e.PathId).ThenBy(e => e.Step).ToList();

        public List<NpcSquadSpawnEntry> GetSpawns() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.NpcSquadSpawnEntries).OrderBy(e => e.Id).ToList();

        public bool SaveType(NpcSquadTypeEntry type, IReadOnlyList<NpcSquadMemberEntry> members) =>
            Write("saving squad type", () =>
            {
                var row = _worldContext.NpcSquadTypeEntries.SingleOrDefault(e => e.Id == type.Id);

                if (row == null)
                    _worldContext.NpcSquadTypeEntries.Add(Copy(type));
                else
                {
                    row.Name = type.Name;
                    row.Formation = type.Formation;
                    row.Spacing = type.Spacing;
                    row.EngageRange = type.EngageRange;
                }

                _worldContext.NpcSquadMemberEntries.RemoveRange(_worldContext.NpcSquadMemberEntries.Where(e => e.TypeId == type.Id));
                _worldContext.SaveChanges();

                foreach (var member in members)
                    _worldContext.NpcSquadMemberEntries.Add(new NpcSquadMemberEntry
                    {
                        TypeId = type.Id, Slot = member.Slot, CreatureId = member.CreatureId,
                        MinCount = member.MinCount, MaxCount = member.MaxCount
                    });
            });

        public bool DeleteType(uint id) =>
            Write("deleting squad type", () =>
            {
                _worldContext.NpcSquadSpawnEntries.RemoveRange(_worldContext.NpcSquadSpawnEntries.Where(e => e.TypeId == id));
                _worldContext.NpcSquadMemberEntries.RemoveRange(_worldContext.NpcSquadMemberEntries.Where(e => e.TypeId == id));
                _worldContext.NpcSquadTypeEntries.RemoveRange(_worldContext.NpcSquadTypeEntries.Where(e => e.Id == id));
            });

        public bool SavePath(NpcSquadPathEntry path, IReadOnlyList<NpcSquadPathPointEntry> points) =>
            Write("saving squad path", () =>
            {
                var row = _worldContext.NpcSquadPathEntries.SingleOrDefault(e => e.Id == path.Id);

                if (row == null)
                    _worldContext.NpcSquadPathEntries.Add(new NpcSquadPathEntry
                    {
                        Id = path.Id, MapContextId = path.MapContextId, Name = path.Name, Mode = path.Mode
                    });
                else
                {
                    row.MapContextId = path.MapContextId;
                    row.Name = path.Name;
                    row.Mode = path.Mode;
                }

                _worldContext.NpcSquadPathPointEntries.RemoveRange(_worldContext.NpcSquadPathPointEntries.Where(e => e.PathId == path.Id));
                _worldContext.SaveChanges();

                foreach (var point in points)
                    _worldContext.NpcSquadPathPointEntries.Add(new NpcSquadPathPointEntry
                    {
                        PathId = path.Id, Step = point.Step, PosX = point.PosX, PosY = point.PosY, PosZ = point.PosZ,
                        PauseMs = point.PauseMs
                    });
            });

        public bool DeletePath(uint id) =>
            Write("deleting squad path", () =>
            {
                foreach (var spawn in _worldContext.NpcSquadSpawnEntries.Where(e => e.PathId == id))
                    spawn.PathId = 0;

                _worldContext.NpcSquadPathPointEntries.RemoveRange(_worldContext.NpcSquadPathPointEntries.Where(e => e.PathId == id));
                _worldContext.NpcSquadPathEntries.RemoveRange(_worldContext.NpcSquadPathEntries.Where(e => e.Id == id));
            });

        public bool SaveSpawn(NpcSquadSpawnEntry spawn) =>
            Write("saving squad spawn", () =>
            {
                var row = _worldContext.NpcSquadSpawnEntries.SingleOrDefault(e => e.Id == spawn.Id);

                if (row == null)
                {
                    _worldContext.NpcSquadSpawnEntries.Add(Copy(spawn));
                    return;
                }

                row.MapContextId = spawn.MapContextId;
                row.TypeId = spawn.TypeId;
                row.PathId = spawn.PathId;
                row.PosX = spawn.PosX;
                row.PosY = spawn.PosY;
                row.PosZ = spawn.PosZ;
                row.Rotation = spawn.Rotation;
                row.RespawnSeconds = spawn.RespawnSeconds;
                row.Enabled = spawn.Enabled;
                row.Comment = spawn.Comment;
            });

        public bool DeleteSpawn(uint id) =>
            Write("deleting squad spawn", () =>
                _worldContext.NpcSquadSpawnEntries.RemoveRange(_worldContext.NpcSquadSpawnEntries.Where(e => e.Id == id)));

        private static NpcSquadTypeEntry Copy(NpcSquadTypeEntry type) => new()
        {
            Id = type.Id, Name = type.Name, Formation = type.Formation, Spacing = type.Spacing, EngageRange = type.EngageRange
        };

        private static NpcSquadSpawnEntry Copy(NpcSquadSpawnEntry spawn) => new()
        {
            Id = spawn.Id, MapContextId = spawn.MapContextId, TypeId = spawn.TypeId, PathId = spawn.PathId,
            PosX = spawn.PosX, PosY = spawn.PosY, PosZ = spawn.PosZ, Rotation = spawn.Rotation,
            RespawnSeconds = spawn.RespawnSeconds, Enabled = spawn.Enabled, Comment = spawn.Comment ?? string.Empty
        };

        /// <summary>The change, saved in one transaction; false, logged and undone, if it failed.</summary>
        private bool Write(string what, Action change)
        {
            try
            {
                using var transaction = _worldContext.Database.BeginTransaction();

                change();
                _worldContext.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error {what}:");
                Logger.WriteLog(LogType.Error, e);
                _worldContext.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
