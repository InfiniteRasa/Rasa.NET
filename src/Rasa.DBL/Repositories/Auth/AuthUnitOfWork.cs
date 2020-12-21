using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.Auth
{
    using Account;
    using Context.Auth;
    using UnitOfWork;

    public class AuthUnitOfWork : UnitOfWork, IAuthUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public AuthUnitOfWork(AuthContext dbContext, IAuthAccountRepository authAccountRepository) : base(dbContext)
        {
            AuthAccountRepository = authAccountRepository;
        }

        public IAuthAccountRepository AuthAccountRepository { get; }
    }
}