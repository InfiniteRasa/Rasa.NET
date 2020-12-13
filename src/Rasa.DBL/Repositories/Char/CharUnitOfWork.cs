using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using Context.Char;
    using GameAccount;

    public interface ICharUnitOfWork : IUnitOfWork
    {
        IGameAccountRepository GameAccounts { get; }
        ICharacterRepository Characters { get; }
        ICharacterAppearanceRepository CharacterAppearances { get; }
    }

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

    public interface IUnitOfWorkFactory
    {
        T Create<T>() where T : IUnitOfWork;
    }

    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>()
            where T : IUnitOfWork
        {
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<T>();
        }
    }
}