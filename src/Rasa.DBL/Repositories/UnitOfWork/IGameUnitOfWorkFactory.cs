namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using World;

    public interface IGameUnitOfWorkFactory
    {
        ICharUnitOfWork CreateChar();

        IWorldUnitOfWork CreateWorld();
    }
}