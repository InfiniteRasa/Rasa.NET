using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using World;

    public class DelegatingWorldUnitOfWork : DelegatingUnitOfWorkBase, IWorldUnitOfWork
    {
        private readonly IWorldUnitOfWork _parent;

        public DelegatingWorldUnitOfWork(IWorldUnitOfWork parent, IServiceScope scope) : base(parent, scope)
        {
            _parent = parent;
        }

        public IItemTemplateItemClassRepository ItemTemplateItemClassRepository => _parent.ItemTemplateItemClassRepository;

        public IPlayerRandomNameRepository RandomNameRepository => _parent.RandomNameRepository;
    }
}