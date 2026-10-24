using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Apartment
{
    using Context.Char;
    using Structures.Char;

    /// <summary>
    /// The apartments (the game server's Managers.Apartments): each one's row, its placed items,
    /// the characters invited into it, and where each character inside one came from. Every
    /// call saves at once.
    /// </summary>
    public interface IApartmentRepository
    {
        ApartmentEntry Get(uint id);

        /// <summary>The apartment of an owner, or null.</summary>
        ApartmentEntry FindByOwner(byte ownerKind, uint ownerId);

        /// <summary>Adds the owner's apartment on a plot; the one there already, if there is one.</summary>
        ApartmentEntry Create(byte ownerKind, uint ownerId, uint plotId, long createdAt);

        /// <summary>Changes an apartment's row and saves it. False when it is gone.</summary>
        bool Update(uint id, Action<ApartmentEntry> change);

        /// <summary>Deletes an apartment, its items, its visitors and the return points of those inside it. False when it was gone.</summary>
        bool Delete(uint id);

        List<ApartmentItemEntry> GetItems(uint apartmentId);

        /// <summary>Adds an item, numbered from the apartment's next number, and saves both. Null when the apartment is gone.</summary>
        ApartmentItemEntry AddItem(uint apartmentId, ApartmentItemEntry item);

        /// <summary>Saves an item's place, turn, size and packing. False when it is gone.</summary>
        bool UpdateItem(ApartmentItemEntry item);

        /// <summary>Saves the packing of many items at once (a plot change).</summary>
        void SetPacked(uint apartmentId, IReadOnlyDictionary<uint, bool> packedById);

        bool RemoveItem(uint itemId);

        /// <summary>Deletes every item of an apartment; returns how many went.</summary>
        int RemoveItems(uint apartmentId);

        List<ApartmentVisitorEntry> GetVisitors(uint apartmentId);

        /// <summary>The apartments a character is invited into.</summary>
        List<ApartmentEntry> InvitedInto(uint characterId);

        /// <summary>Invites a character, or changes whether they may build.</summary>
        void SetVisitor(uint apartmentId, uint characterId, bool mayBuild, long addedAt);

        bool RemoveVisitor(uint apartmentId, uint characterId);

        /// <summary>The apartments of the owners named, by owner id (owner kind character).</summary>
        List<ApartmentEntry> OwnedBy(ICollection<uint> characterIds);

        ApartmentReturnEntry GetReturn(uint characterId);

        void SetReturn(ApartmentReturnEntry entry);

        void RemoveReturn(uint characterId);

        /// <summary>
        /// A character deleted: their apartment with its items and visitors, their invitations
        /// into others' and their return point. Returns the id of the apartment that went, 0 for none.
        /// </summary>
        uint DeleteForCharacter(uint characterId);

        /// <summary>A character by name, any case: (id, account id, name), or null for none.</summary>
        (uint Id, uint AccountId, string Name)? FindCharacter(string name);

        /// <summary>A character's name and account by id, or null for none.</summary>
        (uint Id, uint AccountId, string Name)? GetCharacter(uint characterId);
    }

    public class ApartmentRepository : IApartmentRepository
    {
        private readonly CharContext _charContext;

        public ApartmentRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public ApartmentEntry Get(uint id) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApartmentEntries).FirstOrDefault(e => e.Id == id);

        public ApartmentEntry FindByOwner(byte ownerKind, uint ownerId) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApartmentEntries)
                .FirstOrDefault(e => e.OwnerKind == ownerKind && e.OwnerId == ownerId);

        public ApartmentEntry Create(byte ownerKind, uint ownerId, uint plotId, long createdAt)
        {
            if (ownerKind == 0 || ownerId == 0)
                throw new ArgumentOutOfRangeException(ownerKind == 0 ? nameof(ownerKind) : nameof(ownerId));

            var existing = FindByOwner(ownerKind, ownerId);

            if (existing != null)
                return existing;

            var entry = new ApartmentEntry { OwnerKind = ownerKind, OwnerId = ownerId, PlotId = plotId, CreatedAt = createdAt };

            _charContext.ApartmentEntries.Add(entry);
            _charContext.SaveChanges();

            return entry;
        }

        public bool Update(uint id, Action<ApartmentEntry> change)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return false;

            change(row);
            _charContext.SaveChanges();

            return true;
        }

        public bool Delete(uint id)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return false;

            _charContext.ApartmentItemEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.ApartmentItemEntries).Where(e => e.ApartmentId == id).ToList());
            _charContext.ApartmentVisitorEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.ApartmentVisitorEntries).Where(e => e.ApartmentId == id).ToList());
            _charContext.ApartmentReturnEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.ApartmentReturnEntries).Where(e => e.ApartmentId == id).ToList());
            _charContext.ApartmentEntries.Remove(row);
            _charContext.SaveChanges();

            return true;
        }

        public List<ApartmentItemEntry> GetItems(uint apartmentId) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApartmentItemEntries)
                .Where(e => e.ApartmentId == apartmentId)
                .OrderBy(e => e.Number)
                .ToList();

        public ApartmentItemEntry AddItem(uint apartmentId, ApartmentItemEntry item)
        {
            var apartment = _charContext.CreateTrackingQuery(_charContext.ApartmentEntries).FirstOrDefault(e => e.Id == apartmentId);

            if (apartment == null || item == null)
                return null;

            var entry = new ApartmentItemEntry
            {
                ApartmentId = apartmentId,
                Number = Math.Max(1, apartment.NextItemNumber),
                ClassId = item.ClassId,
                PosX = item.PosX,
                PosY = item.PosY,
                PosZ = item.PosZ,
                Rotation = item.Rotation,
                Scale = item.Scale,
                Packed = item.Packed,
                PlacedBy = item.PlacedBy,
                PlacedAt = item.PlacedAt
            };

            apartment.NextItemNumber = entry.Number + 1;
            _charContext.ApartmentItemEntries.Add(entry);
            _charContext.SaveChanges();

            return entry;
        }

        public bool UpdateItem(ApartmentItemEntry item)
        {
            var row = item == null ? null : _charContext.CreateTrackingQuery(_charContext.ApartmentItemEntries).FirstOrDefault(e => e.Id == item.Id);

            if (row == null)
                return false;

            row.PosX = item.PosX;
            row.PosY = item.PosY;
            row.PosZ = item.PosZ;
            row.Rotation = item.Rotation;
            row.Scale = item.Scale;
            row.Packed = item.Packed;
            _charContext.SaveChanges();

            return true;
        }

        public void SetPacked(uint apartmentId, IReadOnlyDictionary<uint, bool> packedById)
        {
            if (packedById == null || packedById.Count == 0)
                return;

            foreach (var row in _charContext.CreateTrackingQuery(_charContext.ApartmentItemEntries).Where(e => e.ApartmentId == apartmentId).ToList())
                if (packedById.TryGetValue(row.Id, out var packed))
                    row.Packed = packed;

            _charContext.SaveChanges();
        }

        public bool RemoveItem(uint itemId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentItemEntries).FirstOrDefault(e => e.Id == itemId);

            if (row == null)
                return false;

            _charContext.ApartmentItemEntries.Remove(row);
            _charContext.SaveChanges();

            return true;
        }

        public int RemoveItems(uint apartmentId)
        {
            var rows = _charContext.CreateTrackingQuery(_charContext.ApartmentItemEntries).Where(e => e.ApartmentId == apartmentId).ToList();

            if (rows.Count == 0)
                return 0;

            _charContext.ApartmentItemEntries.RemoveRange(rows);
            _charContext.SaveChanges();

            return rows.Count;
        }

        public List<ApartmentVisitorEntry> GetVisitors(uint apartmentId) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApartmentVisitorEntries)
                .Where(e => e.ApartmentId == apartmentId)
                .OrderBy(e => e.AddedAt)
                .ToList();

        public List<ApartmentEntry> InvitedInto(uint characterId)
        {
            var ids = _charContext.CreateNoTrackingQuery(_charContext.ApartmentVisitorEntries)
                .Where(e => e.CharacterId == characterId)
                .Select(e => e.ApartmentId)
                .ToList();

            return ids.Count == 0
                ? new List<ApartmentEntry>()
                : _charContext.CreateNoTrackingQuery(_charContext.ApartmentEntries).Where(e => ids.Contains(e.Id)).OrderBy(e => e.Id).ToList();
        }

        public void SetVisitor(uint apartmentId, uint characterId, bool mayBuild, long addedAt)
        {
            if (apartmentId == 0 || characterId == 0)
                return;

            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentVisitorEntries)
                .FirstOrDefault(e => e.ApartmentId == apartmentId && e.CharacterId == characterId);

            if (row == null)
                _charContext.ApartmentVisitorEntries.Add(new ApartmentVisitorEntry
                {
                    ApartmentId = apartmentId,
                    CharacterId = characterId,
                    MayBuild = mayBuild,
                    AddedAt = addedAt
                });
            else if (row.MayBuild == mayBuild)
                return;
            else
                row.MayBuild = mayBuild;

            _charContext.SaveChanges();
        }

        public bool RemoveVisitor(uint apartmentId, uint characterId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentVisitorEntries)
                .FirstOrDefault(e => e.ApartmentId == apartmentId && e.CharacterId == characterId);

            if (row == null)
                return false;

            _charContext.ApartmentVisitorEntries.Remove(row);
            _charContext.SaveChanges();

            return true;
        }

        public List<ApartmentEntry> OwnedBy(ICollection<uint> characterIds)
        {
            if (characterIds == null || characterIds.Count == 0)
                return new List<ApartmentEntry>();

            var ids = characterIds.ToList();

            return _charContext.CreateNoTrackingQuery(_charContext.ApartmentEntries)
                .Where(e => e.OwnerKind == ApartmentEntry.OwnerKindCharacter && ids.Contains(e.OwnerId))
                .OrderBy(e => e.Id)
                .ToList();
        }

        public ApartmentReturnEntry GetReturn(uint characterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApartmentReturnEntries).FirstOrDefault(e => e.CharacterId == characterId);

        public void SetReturn(ApartmentReturnEntry entry)
        {
            if (entry == null || entry.CharacterId == 0)
                return;

            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentReturnEntries).FirstOrDefault(e => e.CharacterId == entry.CharacterId);

            if (row == null)
            {
                _charContext.ApartmentReturnEntries.Add(new ApartmentReturnEntry
                {
                    CharacterId = entry.CharacterId,
                    ApartmentId = entry.ApartmentId,
                    MapContextId = entry.MapContextId,
                    PosX = entry.PosX,
                    PosY = entry.PosY,
                    PosZ = entry.PosZ,
                    Rotation = entry.Rotation
                });
            }
            else
            {
                row.ApartmentId = entry.ApartmentId;
                row.MapContextId = entry.MapContextId;
                row.PosX = entry.PosX;
                row.PosY = entry.PosY;
                row.PosZ = entry.PosZ;
                row.Rotation = entry.Rotation;
            }

            _charContext.SaveChanges();
        }

        public void RemoveReturn(uint characterId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ApartmentReturnEntries).FirstOrDefault(e => e.CharacterId == characterId);

            if (row == null)
                return;

            _charContext.ApartmentReturnEntries.Remove(row);
            _charContext.SaveChanges();
        }

        public uint DeleteForCharacter(uint characterId)
        {
            var own = FindByOwner(ApartmentEntry.OwnerKindCharacter, characterId);

            if (own != null)
                Delete(own.Id);

            _charContext.ApartmentVisitorEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.ApartmentVisitorEntries).Where(e => e.CharacterId == characterId).ToList());
            _charContext.ApartmentReturnEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.ApartmentReturnEntries).Where(e => e.CharacterId == characterId).ToList());
            _charContext.SaveChanges();

            return own?.Id ?? 0;
        }

        public (uint Id, uint AccountId, string Name)? FindCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // ToLower on both sides: SQLite compares strings with BINARY collation, so = is
            // case-sensitive there while MySQL's default collation is not.
            var lowered = name.Trim().ToLower();
            var row = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries)
                .Where(e => e.Name.ToLower() == lowered)
                .Select(e => new { e.Id, e.AccountId, e.Name })
                .FirstOrDefault();

            return row == null ? null : (row.Id, row.AccountId, row.Name);
        }

        public (uint Id, uint AccountId, string Name)? GetCharacter(uint characterId)
        {
            var row = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries)
                .Where(e => e.Id == characterId)
                .Select(e => new { e.Id, e.AccountId, e.Name })
                .FirstOrDefault();

            return row == null ? null : (row.Id, row.AccountId, row.Name);
        }
    }
}
