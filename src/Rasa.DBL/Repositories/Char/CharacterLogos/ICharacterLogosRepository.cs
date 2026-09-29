using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterLogos
{
    public interface ICharacterLogosRepository
    {
        List<uint> GetLogos(uint characterId);
        void SetLogos(uint characterId, uint logosId);

        /// <summary>Takes one Logos from a character; false if the row could not be removed.</summary>
        bool DeleteLogos(uint characterId, uint logosId);

        /// <summary>Takes every Logos from a character; false if the rows could not be removed.</summary>
        bool DeleteAllLogos(uint characterId);
    }
}
