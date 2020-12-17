namespace Rasa.Repositories
{
    using Char;

    public interface IUnitOfWorkFactory
    {
        ICharUnitOfWork CreateCharUnitOfWork();
    }
}