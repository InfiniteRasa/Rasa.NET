using System.Collections.Generic;

namespace Rasa.Managers
{
    using Game;
    using Repositories.Char;
    using Structures;

    public partial class InventoryManager
    {
        internal sealed class InventoryConsumption
        {
            private readonly List<MissionProgressEvent> _events = new();
            private InventoryPlan _plan;
            internal IEnumerable<MissionProgressEvent> ProgressEvents => _events;

            internal void PlanAndSave(Client client, IReadOnlyDictionary<ulong, uint> quantities, ICharUnitOfWork unit)
            {
                _plan = InventoryPlan.For(client, unit);
                foreach (var entry in quantities)
                    _events.Add(_plan.ConsumeEntity(entry.Key, entry.Value));
            }

            internal void Publish(Client client) => _plan?.Publish(client);
        }
    }
}
