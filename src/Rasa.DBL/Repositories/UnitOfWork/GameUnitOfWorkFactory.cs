using System;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using World;

    public class GameUnitOfWorkFactory : UnitOfWorkFactoryBase, IGameUnitOfWorkFactory
    {
        public GameUnitOfWorkFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public ICharUnitOfWork CreateChar()
        {
            var scope = CreateServiceScope();
            var unitOfWork = Create<ICharUnitOfWork>(scope);
            return new DelegatingCharUnitOfWork(unitOfWork, scope);
        }

        public IWorldUnitOfWork CreateWorld()
        {
            var scope = CreateServiceScope();
            var unitOfWork = Create<IWorldUnitOfWork>(scope);
            return new DelegatingWorldUnitOfWork(unitOfWork, scope);
        }
    }
}