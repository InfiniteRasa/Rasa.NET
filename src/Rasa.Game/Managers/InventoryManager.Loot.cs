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

            /// <summary>
            /// Plans the items into the backpack, in order, and returns those that fit: one that
            /// does not stays on the corpse (InventoryPlan.AcceptLoot).
            /// </summary>
            internal List<LootItem> PlanAndSave(Client client, IReadOnlyList<LootItem> loot, ICharUnitOfWork unitOfWork,
                uint? preferredSlot = null)
            {
                _plan = InventoryPlan.For(client, unitOfWork);
                var fitted = new List<LootItem>();
                foreach (var item in loot)
                    if (_plan.AcceptLoot(item, preferredSlot, _beforeItemPublication))
                        fitted.Add(item);
                return fitted;
            }

            internal void Publish(Client client) => _plan?.Publish(client);
        }
    }
}
