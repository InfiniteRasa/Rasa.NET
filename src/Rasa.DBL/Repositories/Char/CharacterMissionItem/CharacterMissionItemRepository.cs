using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Context.Char;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.CharacterMissionItem
{
    public sealed class CharacterMissionItemRepository : ICharacterMissionItemRepository
    {
        private readonly CharContext _context;
        public CharacterMissionItemRepository(CharContext context) => _context = context;

        public IReadOnlyList<CharacterMissionItemEntry> GetOwned(uint characterId) =>
            _context.CreateNoTrackingQuery(_context.CharacterMissionItemEntries)
                .Where(entry => entry.CharacterId == characterId).ToArray();

        public CharacterMissionItemEntry GetOwner(uint itemId) =>
            _context.CreateNoTrackingQuery(_context.CharacterMissionItemEntries).SingleOrDefault(entry => entry.ItemId == itemId);

        public IReadOnlyList<CharacterMissionItemEntry> GetOwners(IReadOnlyCollection<uint> itemIds)
        {
            var ids = itemIds.Distinct().ToArray();
            return ids.Length == 0 ? Array.Empty<CharacterMissionItemEntry>() :
                _context.CreateNoTrackingQuery(_context.CharacterMissionItemEntries)
                    .Where(entry => ids.Contains(entry.ItemId)).ToArray();
        }

        public void Save(CharacterMissionItemEntry entry)
        {
            var saved = _context.CharacterMissionItemEntries.Find(entry.CharacterId, entry.MissionId,
                entry.AssignmentId, entry.ItemKey, entry.ItemId);
            if (saved == null)
                _context.CharacterMissionItemEntries.Add(entry);
            else
                saved.Quantity = entry.Quantity;
            _context.SaveChanges();
        }

        public void Remove(uint itemId)
        {
            var entry = _context.CharacterMissionItemEntries.SingleOrDefault(entry => entry.ItemId == itemId);
            if (entry != null)
            {
                _context.CharacterMissionItemEntries.Remove(entry);
                _context.SaveChanges();
            }
        }

        public CharacterMissionItemReceiptEntry GetReceipt(uint characterId, string assignmentId, string operationKey) =>
            _context.CharacterMissionItemReceiptEntries.Find(characterId, assignmentId, operationKey);

        public void AddReceipt(CharacterMissionItemReceiptEntry entry)
        {
            _context.CharacterMissionItemReceiptEntries.Add(entry);
            _context.SaveChanges();
        }

        public void RemoveAll(uint characterId)
        {
            _context.CharacterMissionItemEntries.RemoveRange(
                _context.CharacterMissionItemEntries.Where(entry => entry.CharacterId == characterId));
            _context.CharacterMissionItemReceiptEntries.RemoveRange(
                _context.CharacterMissionItemReceiptEntries.Where(entry => entry.CharacterId == characterId));
            _context.CharacterMissionItemQuarantineEntries.RemoveRange(
                _context.CharacterMissionItemQuarantineEntries.Where(entry => entry.CharacterId == characterId));
            _context.SaveChanges();
        }

        public CharacterMissionItemQuarantineEntry GetQuarantine(uint characterId, string assignmentId) =>
            _context.CharacterMissionItemQuarantineEntries.Find(characterId, assignmentId);
    }
}
