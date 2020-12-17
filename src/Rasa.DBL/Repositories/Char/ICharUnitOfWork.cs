namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using GameAccount;
    using UnitOfWork;

    public interface ICharUnitOfWork : IUnitOfWork
    {
        IGameAccountRepository GameAccounts { get; }
        ICharacterRepository Characters { get; }
        ICharacterAppearanceRepository CharacterAppearances { get; }
    }
}