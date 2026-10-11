using System;
using System.Reflection;
using Rasa.Loot;
using Rasa.Repositories.World;

namespace Rasa.Test.Gameplay
{
    internal sealed class OrdinaryLootCatalogScope : IDisposable
    {
        private readonly FieldInfo _current = typeof(OrdinaryLootCatalog)
            .GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly object _previous;

        internal OrdinaryLootCatalogScope(OrdinaryLootCatalog catalog)
        {
            _previous = _current.GetValue(null);
            if (catalog == null)
                _current.SetValue(null, null);
            else
                OrdinaryLootCatalog.Publish(catalog);
        }

        internal OrdinaryLootCatalogScope(IWorldUnitOfWork world) : this(OrdinaryLootCatalog.Load(world)) { }

        public void Dispose() => _current.SetValue(null, _previous);
    }
}
