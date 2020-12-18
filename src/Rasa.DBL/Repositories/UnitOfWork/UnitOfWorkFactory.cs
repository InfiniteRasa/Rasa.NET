using System;

using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using World;

    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

        private IServiceScope CreateServiceScope()
        {
            var scope = _serviceProvider.CreateScope();
            return scope;
        }

        private T Create<T>(IServiceScope scope)
            where T : IUnitOfWork
        {
            return scope.ServiceProvider.GetService<T>();
        }
    }
}