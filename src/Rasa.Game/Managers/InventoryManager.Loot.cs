using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Game;
    using Repositories.Char;
    using Structures;

    public partial class InventoryManager
    {
        internal sealed class LootGrant
        {
            private readonly Action<Item> _beforeItemPublication;
            private InventoryPlan _plan;
            internal LootGrant(Action<Item> beforeItemPublication = null) => _beforeItemPublication = beforeItemPublication;

            internal void PlanAndSave(Client client, IReadOnlyList<LootItem> loot, ICharUnitOfWork unitOfWork,
                uint? preferredSlot = null)
            {
                _plan = InventoryPlan.For(client, unitOfWork);
                foreach (var item in loot)
                    _plan.AcceptLoot(item, preferredSlot, _beforeItemPublication);
            }

            internal void Publish(Client client) => _plan?.Publish(client);
        }
    }
}
