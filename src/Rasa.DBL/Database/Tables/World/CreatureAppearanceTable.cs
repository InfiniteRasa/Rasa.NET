using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureAppearanceTable
    {

        public static List<CreatureAppearanceEntry> GetCreatureAppearance(uint creatureId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.CreatureAppearance.Where(ca => ca.CreatureId == creatureId)
                    .ToList();
            }
        }

        public static List<CreatureAppearanceEntry> GetCreatureAppearance(uint creatureId, uint slotId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.CreatureAppearance.Where(ca => ca.CreatureId == creatureId && ca.Slot == slotId)
                    .ToList();
            }
        }

        public static void SetCreatureAppearance(uint creatureId, uint slot, uint classId, uint color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GameDatabaseAccess.WorldConnection.CreatureAppearance.Add(
                    new CreatureAppearanceEntry
                    {
                        CreatureId = creatureId,
                        Slot = slot,
                        Class = classId,
                        Color = color
                    });
                GameDatabaseAccess.WorldConnection.SaveChanges();
            }
        }

        public static void UpdateCreatureAppearance(uint creatureId, uint slot, uint classId, uint color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetCreatureAppearance(creatureId, slot).ForEach(ca =>
                {
                    ca.Class = classId;
                    ca.Color = color;
                });
                GameDatabaseAccess.WorldConnection.SaveChanges();
            }
        }
    }
}
