namespace Rasa.Repositories.UnitOfWork
{
    using Char;

    public interface IUnitOfWorkFactory
    {
        ICharUnitOfWork CreateCharUnitOfWork();
    }
}