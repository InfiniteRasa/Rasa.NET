using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using Clan;
    using ClanInventory;
    using ClanMember;
    using Context.Char;
    using GameAccount;
    using CensorWord;
    using CharacterAbilityDrawer;
    using CharacterInventory;
    using CharacterLockbox;
    using CharacterLogos;
    using CharacterMission;
    using CharacterOption;
    using CharacterSkills;
    using CharacterTeleporter;
    using CharacterTitle;
    using Friend;
    using Ignored;
    using Items;
    using UserOption;
    using UnitOfWork;

    public class CharUnitOfWork : UnitOfWork, ICharUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public CharUnitOfWork(CharContext dbContext,
            IGameAccountRepository gameAccounts,
            ICensoredWordRepository censoredWords,
            ICharacterRepository characters,
            ICharacterAbilityDrawerRepository characterAbilityDrawers,
            ICharacterAppearanceRepository characterAppearances,
            ICharacterInventoryRepository characterInventories,
            ICharacterLockboxRepository characterLockboxes,
            ICharacterLogosRepository characterLogoses,
            ICharacterMissionRepository characterMissions,
            ICharacterOptionRepository characterOptions,
            ICharacterSkillsRepository characterSkills,
            ICharacterTeleporterRepository characterTeleporters,
            ICharacterTitleRepository characterTitles,
            IClanRepository clans,
            IClanInventoryRepository clanInventories,
            IClanMemberRepository clanMembers,
            IFriendRepository friends,
            IIgnoredRepository ignoreds,
            IItemRepository items,
            IUserOptionRepository userOptions
            ) : base(dbContext)
        {
            GameAccounts = gameAccounts;
            CensoredWords = censoredWords;
            Characters = characters;
            CharacterAbilityDrawers = characterAbilityDrawers;
            CharacterAppearances = characterAppearances;
            CharacterInventories = characterInventories;
            CharacterLockboxes = characterLockboxes;
            CharacterLogoses = characterLogoses;
            CharacterMissions = characterMissions;
            CharacterOptions = characterOptions;
            CharacterSkills = characterSkills;
            CharacterTeleporters = characterTeleporters;
            CharacterTitles = characterTitles;
            Clans = clans;
            ClanInventories = clanInventories;
            ClanMembers = clanMembers;
            Friends = friends;
            Ignoreds = ignoreds;
            Items = items;
            UserOptions = userOptions;
        }

        public ICensoredWordRepository CensoredWords { get; }
        public ICharacterRepository Characters { get; }
        public ICharacterAbilityDrawerRepository CharacterAbilityDrawers { get; }
        public ICharacterAppearanceRepository CharacterAppearances { get; }
        public ICharacterInventoryRepository CharacterInventories { get; }
        public ICharacterLockboxRepository CharacterLockboxes { get; }
        public ICharacterLogosRepository CharacterLogoses { get; }
        public ICharacterMissionRepository CharacterMissions { get; }
        public ICharacterOptionRepository CharacterOptions { get; }
        public ICharacterSkillsRepository CharacterSkills { get; }
        public ICharacterTeleporterRepository CharacterTeleporters { get; }
        public ICharacterTitleRepository CharacterTitles { get; }
        public IClanRepository Clans { get; }
        public IClanInventoryRepository ClanInventories { get; }
        public IClanMemberRepository ClanMembers { get; }
        public IFriendRepository Friends { get; }
        public IGameAccountRepository GameAccounts { get; }
        public IIgnoredRepository Ignoreds { get; }
        public IItemRepository Items { get; }
        public IUserOptionRepository UserOptions { get; }
    }
}