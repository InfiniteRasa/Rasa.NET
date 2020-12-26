using System;

namespace Rasa.Repositories.UnitOfWork
{
    using Auth;

    public class AuthUnitOfWorkFactory : UnitOfWorkFactoryBase, IAuthUnitOfWorkFactory
    {
        public AuthUnitOfWorkFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public IAuthUnitOfWork Create()
        {
            var scope = CreateServiceScope();
            var unitOfWork = Create<IAuthUnitOfWork>(scope);
            return new DelegatingAuthUnitOfWork(unitOfWork, scope);
        }
    }
}