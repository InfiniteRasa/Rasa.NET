using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    using Structures.World;

    public interface IItemTemplateItemClassRepository
    {
        uint GetItemClass(uint itemTemplateId);
        List<ItemTemplateItemClassEntry> Get();
    }
}