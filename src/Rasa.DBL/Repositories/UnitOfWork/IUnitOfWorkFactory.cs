namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using World;

    public interface IUnitOfWorkFactory
    {
        ICharUnitOfWork CreateChar();

        IWorldUnitOfWork CreateWorld();
    }
}