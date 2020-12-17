using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using Char.Character;
    using Char.CharacterAppearance;
    using Char.GameAccount;

    public class DelegatingCharUnitOfWork : ICharUnitOfWork
    {
        private readonly ICharUnitOfWork _parent;
        private readonly IServiceScope _scope;

        public DelegatingCharUnitOfWork(ICharUnitOfWork parent, IServiceScope scope)
        {
            _parent = parent;
            _scope = scope;
        }

        public IGameAccountRepository GameAccounts => _parent.GameAccounts;

        public ICharacterRepository Characters => _parent.Characters;

        public ICharacterAppearanceRepository CharacterAppearances => _parent.CharacterAppearances;

        public void Complete()
        {
            _parent.Complete();
        }

        public void Reject()
        {
            _parent.Reject();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}