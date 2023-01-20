using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using Char.Character;
    using Char.CharacterAppearance;
    using Char.Clan;
    using Char.ClanInventory;
    using Char.ClanMember;
    using Char.GameAccount;
    using Char.CensorWord;
    using Char.CharacterAbilityDrawer;
    using Char.CharacterInventory;
    using Char.CharacterLockbox;
    using Char.CharacterLogos;
    using Char.CharacterMission;
    using Char.CharacterOption;
    using Char.CharacterSkills;
    using Char.CharacterTeleporter;
    using Char.CharacterTitle;
    using Char.Friend;
    using Char.Ignored;
    using Char.Items;
    using Char.UserOption;

    public class DelegatingCharUnitOfWork : DelegatingUnitOfWorkBase, ICharUnitOfWork
    {
        private readonly ICharUnitOfWork _parent;

        public DelegatingCharUnitOfWork(ICharUnitOfWork parent, IServiceScope scope)
            : base(parent, scope)
        {
            _parent = parent;
        }

        public ICensoredWordRepository CensoredWords => _parent.CensoredWords;

        public ICharacterRepository Characters => _parent.Characters;

        public ICharacterAbilityDrawerRepository CharacterAbilityDrawers => _parent.CharacterAbilityDrawers;

        public ICharacterAppearanceRepository CharacterAppearances => _parent.CharacterAppearances;

        public ICharacterInventoryRepository CharacterInventories => _parent.CharacterInventories;

        public ICharacterLockboxRepository CharacterLockboxes => _parent.CharacterLockboxes;

        public ICharacterLogosRepository CharacterLogoses => _parent.CharacterLogoses;

        public ICharacterMissionRepository CharacterMissions => _parent.CharacterMissions;

        public ICharacterOptionRepository CharacterOptions => _parent.CharacterOptions;

        public ICharacterSkillsRepository CharacterSkills => _parent.CharacterSkills;

        public ICharacterTeleporterRepository CharacterTeleporters => _parent.CharacterTeleporters;

        public ICharacterTitleRepository CharacterTitles => _parent.CharacterTitles;

        public IClanRepository Clans => _parent.Clans;

        public IClanInventoryRepository ClanInventories => _parent.ClanInventories;

        public IClanMemberRepository ClanMembers => _parent.ClanMembers;

        public IFriendRepository Friends => _parent.Friends;

        public IGameAccountRepository GameAccounts => _parent.GameAccounts;

        public IIgnoredRepository Ignoreds => _parent.Ignoreds;

        public IItemRepository Items => _parent.Items;

        public IUserOptionRepository UserOptions => _parent.UserOptions;
    }
}