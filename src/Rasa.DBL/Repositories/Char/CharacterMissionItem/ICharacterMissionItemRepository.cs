using System.Collections.Generic;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.CharacterMissionItem
{
    public interface ICharacterMissionItemRepository
    {
        IReadOnlyList<CharacterMissionItemEntry> GetOwned(uint characterId);
        CharacterMissionItemEntry GetOwner(uint itemId);
        IReadOnlyList<CharacterMissionItemEntry> GetOwners(IReadOnlyCollection<uint> itemIds);
        void Save(CharacterMissionItemEntry entry);
        void Remove(uint itemId);
        CharacterMissionItemReceiptEntry GetReceipt(uint characterId, string assignmentId, string operationKey);
        void AddReceipt(CharacterMissionItemReceiptEntry entry);
        CharacterMissionItemQuarantineEntry GetQuarantine(uint characterId, string assignmentId);
        void RemoveAll(uint characterId);
    }
}
