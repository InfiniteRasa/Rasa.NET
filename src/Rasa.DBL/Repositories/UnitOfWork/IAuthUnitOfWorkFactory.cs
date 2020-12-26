namespace Rasa.Repositories.UnitOfWork
{
    using Auth;

    public interface IAuthUnitOfWorkFactory
    {
        IAuthUnitOfWork Create();
    }
}