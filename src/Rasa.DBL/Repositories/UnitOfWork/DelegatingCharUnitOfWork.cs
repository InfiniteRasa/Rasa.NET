using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using Char.Character;
    using Char.CharacterAppearance;
    using Char.GameAccount;

    public class DelegatingCharUnitOfWork : DelegatingUnitOfWorkBase, ICharUnitOfWork
    {
        private readonly ICharUnitOfWork _parent;

        public DelegatingCharUnitOfWork(ICharUnitOfWork parent, IServiceScope scope) 
            : base(parent, scope)
        {
            _parent = parent;
        }

        public IGameAccountRepository GameAccounts => _parent.GameAccounts;

        public ICharacterRepository Characters => _parent.Characters;

        public ICharacterAppearanceRepository CharacterAppearances => _parent.CharacterAppearances;
    }
}