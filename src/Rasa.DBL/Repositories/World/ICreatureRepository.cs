using Rasa.Structures.World;
using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    public interface ICreatureRepository
    {
        List<CreatureEntry> Get();
        List<CreatureClassFlagEntry> GetClassFlags();

        /// <summary>
        /// Puts these flags in place of the ones each of these classes has (creature_class_flag),
        /// in one transaction; a class with no flags given is left with none, and a class not
        /// named is not touched.
        /// </summary>
        void ReplaceClassFlags(IReadOnlyDictionary<uint, IReadOnlyCollection<uint>> flagsByClass) => throw new System.NotSupportedException();
        CreatureStatEntry GetCreatureStats(uint creatureId);
        CreatureActionEntry GetCreatureActionById(uint id);
        Dictionary<uint, CreatureActionEntry> GetCreatureActions();
        void CreateOrUpdateAppearance(uint dbId, uint slotId, uint classId, uint hue);
        List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId);
        List<VendorItemEntry> GetVendorItems();
        List<VendorEntry> GetVendors();
        List<VendorPriceEntry> GetVendorPrices();
        List<CreatureActorNameEntry> GetActorNames();

        /// <summary>The greeting each NPC has been given (npc_greeting). An NPC with no row has the default.</summary>
        List<NpcGreetingEntry> GetNpcGreetings() => new List<NpcGreetingEntry>();

        /// <summary>The battle cry package each class and creature row has been given (creature_battlecry). One with no row is silent.</summary>
        List<CreatureBattlecryEntry> GetBattlecries() => new List<CreatureBattlecryEntry>();

        /// <summary>Gives a creature row its own battle cry package, or changes the one it has (creature_battlecry, scope creature).</summary>
        void SaveCreatureBattlecry(uint creatureId, uint packageId) => throw new System.NotSupportedException();

        /// <summary>Gives a creature row a greeting, or changes the one it has.</summary>
        void SaveNpcGreeting(uint creatureId, uint greetingId) => throw new System.NotSupportedException();

        /// <summary>Marks a creature row's greeting as important, or not. False if the row has no greeting.</summary>
        bool SaveNpcGreetingImportant(uint creatureId, bool important) => throw new System.NotSupportedException();

        /// <summary>Takes a creature row's greeting away. False if it had none.</summary>
        bool DeleteNpcGreeting(uint creatureId) => throw new System.NotSupportedException();
    }
}
