using System.Linq;

namespace Rasa.Database.Tables.World
{
    public class StarterItemsTable
    {
        public static uint GetClassId(uint itemTemplateId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return (from sti in GameDatabaseAccess.WorldConnection.StarterItems
                        where sti.ItemTemplateId == itemTemplateId
                        select sti.ClassId).First();
            }
        }       
    }
}
