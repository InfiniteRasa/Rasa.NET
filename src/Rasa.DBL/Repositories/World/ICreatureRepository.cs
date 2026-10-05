using Rasa.Structures.World;
using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    public interface ICreatureRepository
    {
        List<CreatureEntry> Get();
        List<CreatureClassFlagEntry> GetClassFlags();
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

        /// <summary>Gives a creature row a greeting, or changes the one it has.</summary>
        void SaveNpcGreeting(uint creatureId, uint greetingId) => throw new System.NotSupportedException();

        /// <summary>Takes a creature row's greeting away. False if it had none.</summary>
        bool DeleteNpcGreeting(uint creatureId) => throw new System.NotSupportedException();
    }
}
