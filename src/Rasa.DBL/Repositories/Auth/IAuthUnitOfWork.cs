namespace Rasa.Repositories.Auth
{
    using Account;
    using UnitOfWork;

    public interface IAuthUnitOfWork : IUnitOfWork
    {
        IAuthAccountRepository AuthAccountRepository { get; }
    }
}