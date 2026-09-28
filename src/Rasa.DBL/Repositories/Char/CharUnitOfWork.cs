using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.Char
{
    using Auction;
    using Character;
    using CharacterAppearance;
    using Clan;
    using ClanInventory;
    using ClanLockboxLog;
    using ClanMember;
    using Context.Char;
    using GameAccount;
    using CensorWord;
    using CharacterAbilityDrawer;
    using CharacterInventory;
    using CharacterLockbox;
    using CharacterLogos;
    using CharacterMission;
    using CharacterMissionDeadline;
    using CharacterMissionProgress;
    using CharacterMissionScenario;
    using CharacterOption;
    using CharacterFlag;
    using CharacterSkills;
    using CharacterStartingExperience;
    using CharacterTeleporter;
    using CharacterTitle;
    using Friend;
    using Ignored;
    using Items;
    using Petition;
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
            ICharacterMissionDeadlineRepository characterMissionDeadlines,
            ICharacterMissionProgressRepository characterMissionProgress,
            ICharacterMissionScenarioRepository characterMissionScenario,
            ICharacterOptionRepository characterOptions,
            ICharacterFlagRepository characterFlags,
            ICharacterSkillsRepository characterSkills,
            ICharacterStartingExperienceRepository characterStartingExperience,
            ICharacterTeleporterRepository characterTeleporters,
            ICharacterTitleRepository characterTitles,
            IAuctionRepository auctions,
            IClanRepository clans,
            IClanInventoryRepository clanInventories,
            IClanMemberRepository clanMembers,
            IClanLockboxLogRepository clanLockboxLogs,
            IFriendRepository friends,
            IIgnoredRepository ignoreds,
            IItemRepository items,
            IPetitionRepository petitions,
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
            MissionOffers = new MissionOffer.MissionOfferRepository(dbContext);
            CharacterMissionItems = new CharacterMissionItem.CharacterMissionItemRepository(dbContext);
            CharacterMissionDeadlines = characterMissionDeadlines;
            CharacterMissionProgress = characterMissionProgress;
            CharacterMissionScenario = characterMissionScenario;
            CharacterOptions = characterOptions;
            CharacterFlags = characterFlags;
            CharacterSkills = characterSkills;
            CharacterStartingExperience = characterStartingExperience;
            CharacterTeleporters = characterTeleporters;
            CharacterTitles = characterTitles;
            Clans = clans;
            Auctions = auctions;
            ClanInventories = clanInventories;
            ClanMembers = clanMembers;
            ClanLockboxLogs = clanLockboxLogs;
            Friends = friends;
            Ignoreds = ignoreds;
            Items = items;
            Petitions = petitions;
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
        public MissionOffer.MissionOfferRepository MissionOffers { get; }
        public CharacterMissionItem.ICharacterMissionItemRepository CharacterMissionItems { get; }
        public ICharacterMissionDeadlineRepository CharacterMissionDeadlines { get; }
        public ICharacterMissionProgressRepository CharacterMissionProgress { get; }
        public ICharacterMissionScenarioRepository CharacterMissionScenario { get; }
        public ICharacterOptionRepository CharacterOptions { get; }
        public ICharacterFlagRepository CharacterFlags { get; }
        public ICharacterSkillsRepository CharacterSkills { get; }
        public ICharacterStartingExperienceRepository CharacterStartingExperience { get; }
        public ICharacterTeleporterRepository CharacterTeleporters { get; }
        public ICharacterTitleRepository CharacterTitles { get; }
        public IAuctionRepository Auctions { get; }
        public IClanRepository Clans { get; }
        public IClanInventoryRepository ClanInventories { get; }
        public IClanMemberRepository ClanMembers { get; }
        public IClanLockboxLogRepository ClanLockboxLogs { get; }
        public IFriendRepository Friends { get; }
        public IGameAccountRepository GameAccounts { get; }
        public IIgnoredRepository Ignoreds { get; }
        public IItemRepository Items { get; }
        public IPetitionRepository Petitions { get; }
        public IUserOptionRepository UserOptions { get; }
    }
}