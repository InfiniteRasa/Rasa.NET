using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Auth;
    using Auth.Account;

    public class DelegatingAuthUnitOfWork : DelegatingUnitOfWorkBase, IAuthUnitOfWork
    {
        private readonly IAuthUnitOfWork _parent;

        public DelegatingAuthUnitOfWork(IAuthUnitOfWork parent, IServiceScope scope) 
            : base(parent, scope)
        {
            _parent = parent;
        }

        public IAuthAccountRepository AuthAccountRepository => _parent.AuthAccountRepository;
    }
}