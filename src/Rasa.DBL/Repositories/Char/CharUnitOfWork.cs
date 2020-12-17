using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using Context.Char;
    using GameAccount;
    using UnitOfWork;

    public class CharUnitOfWork : UnitOfWork, ICharUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public CharUnitOfWork(CharContext dbContext,
            IGameAccountRepository gameAccounts, 
            ICharacterRepository characters, 
            ICharacterAppearanceRepository characterAppearances) : base(dbContext)
        {
            GameAccounts = gameAccounts;
            Characters = characters;
            CharacterAppearances = characterAppearances;
        }

        public IGameAccountRepository GameAccounts { get; }

        public ICharacterRepository Characters { get; }

        public ICharacterAppearanceRepository CharacterAppearances { get; }
    }
}