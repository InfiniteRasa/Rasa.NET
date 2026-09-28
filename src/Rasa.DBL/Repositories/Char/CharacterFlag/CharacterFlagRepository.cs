using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Context.Char;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.CharacterFlag
{
    public sealed class CharacterFlagRepository : ICharacterFlagRepository
    {
        private readonly CharContext _context;
        public CharacterFlagRepository(CharContext context) => _context = context;

        public IReadOnlyDictionary<uint, uint> Get(uint characterId) =>
            _context.CreateNoTrackingQuery(_context.CharacterFlagEntries)
                .Where(entry => entry.CharacterId == characterId)
                .ToDictionary(entry => entry.FlagId, entry => entry.Value);

        public uint? GetValue(uint characterId, uint flagId) =>
            _context.CreateNoTrackingQuery(_context.CharacterFlagEntries)
                .Where(entry => entry.CharacterId == characterId && entry.FlagId == flagId)
                .Select(entry => (uint?)entry.Value).SingleOrDefault();

        public bool HasValue(uint characterId, uint flagId, uint value = 1) =>
            GetValue(characterId, flagId) == value;

        public void Set(uint characterId, uint flagId, uint value)
        {
            Validate(characterId, flagId);
            var entry = _context.CreateTrackingQuery(_context.CharacterFlagEntries)
                .SingleOrDefault(entry => entry.CharacterId == characterId && entry.FlagId == flagId);
            if (entry == null)
                _context.CharacterFlagEntries.Add(new CharacterFlagEntry(characterId, flagId, value));
            else
                entry.Value = value;
            _context.SaveChanges();
        }

        public void Add(CharacterFlagEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            Validate(entry.CharacterId, entry.FlagId);
            _context.CharacterFlagEntries.Add(entry);
            _context.SaveChanges();
        }

        public void Remove(uint characterId, uint flagId)
        {
            Validate(characterId, flagId);
            var entry = _context.CreateTrackingQuery(_context.CharacterFlagEntries)
                .SingleOrDefault(entry => entry.CharacterId == characterId && entry.FlagId == flagId);
            if (entry == null)
                return;
            _context.CharacterFlagEntries.Remove(entry);
            _context.SaveChanges();
        }

        private static void Validate(uint characterId, uint flagId)
        {
            if (characterId == 0)
                throw new ArgumentOutOfRangeException(nameof(characterId));
            if (flagId == 0)
                throw new ArgumentOutOfRangeException(nameof(flagId));
        }
    }
}
