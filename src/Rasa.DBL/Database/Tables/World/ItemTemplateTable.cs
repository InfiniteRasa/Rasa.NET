using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateTable
    {
        public static int GetClassId(int itemTemplateId)
        {
            throw new NotImplementedException();
        }

        public static List<ItemTemplateEntry> GetItemTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Itemtemplate.Where(_ => true).ToList();
            }
        }
    }
}
