using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories
{
    using Char;

    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICharUnitOfWork CreateCharUnitOfWork()
        {
            return Create<ICharUnitOfWork>();
        }

        private T Create<T>()
            where T : IUnitOfWork
        {
            var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<T>();
            return unitOfWork;
        }
    }
}