using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterLogos
{
    using Context.Char;
    using Rasa.Structures.Char;

    public class CharacterLogosRepository : ICharacterLogosRepository
    {
        private readonly CharContext _charContext;

        public CharacterLogosRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<uint> GetLogos(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLogosEntries);
            var entries = query.Where(e => e.CharacterId == characterId).Select(e => e.LogosId).ToList();

            return entries;
        }

        public void SetLogos(uint characterId, uint logosId)
        {
            var entry = new CharacterLogosEntry(characterId, logosId);

            try
            {
                _charContext.CharacterLogosEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding logos:");
                Logger.WriteLog(LogType.Error, e);
            }
        }
    }
}
