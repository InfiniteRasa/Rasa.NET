namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using GameAccount;

    public interface ICharUnitOfWork : IUnitOfWork
    {
        IGameAccountRepository GameAccounts { get; }
        ICharacterRepository Characters { get; }
        ICharacterAppearanceRepository CharacterAppearances { get; }
    }
}