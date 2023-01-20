using System;

using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    public abstract class UnitOfWorkFactoryBase
    {
        private readonly IServiceProvider _serviceProvider;

        protected UnitOfWorkFactoryBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected IServiceScope CreateServiceScope()
        {
            var scope = _serviceProvider.CreateScope();
            return scope;
        }

        protected T Create<T>(IServiceScope scope)
            where T : IUnitOfWork
        {
            return scope.ServiceProvider.GetService<T>();
        }
    }
}