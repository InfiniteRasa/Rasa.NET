using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using Char;
    using Char.Auction;
    using Char.Character;
    using Char.CharacterAppearance;
    using Char.Clan;
    using Char.ClanInventory;
    using Char.ClanLockboxLog;
    using Char.ClanMember;
    using Char.GameAccount;
    using Char.CensorWord;
    using Char.CharacterAbilityDrawer;
    using Char.CharacterInventory;
    using Char.CharacterLockbox;
    using Char.CharacterLogos;
    using Char.CharacterMission;
    using Char.CharacterMissionDeadline;
    using Char.CharacterMissionProgress;
    using Char.CharacterMissionScenario;
    using Char.CharacterOption;
    using Char.CharacterFlag;
    using Char.CharacterSkills;
    using Char.CharacterStartingExperience;
    using Char.CharacterTeleporter;
    using Char.CharacterTitle;
    using Char.Friend;
    using Char.Ignored;
    using Char.Items;
    using Char.Petition;
    using Char.UserOption;

    public class DelegatingCharUnitOfWork : DelegatingUnitOfWorkBase, ICharUnitOfWork
    {
        private readonly ICharUnitOfWork _parent;

        public DelegatingCharUnitOfWork(ICharUnitOfWork parent, IServiceScope scope)
            : base(parent, scope)
        {
            _parent = parent;
        }

        public IAuctionRepository Auctions => _parent.Auctions;

        public ICensoredWordRepository CensoredWords => _parent.CensoredWords;

        public void ExecuteTransaction(System.Action operation) => _parent.ExecuteTransaction(operation);
        public T Enlist<T>(System.Func<T> create) where T : class, ITransactionParticipant => _parent.Enlist(create);
        public bool HasEnlisted<T>() where T : class, ITransactionParticipant => _parent.HasEnlisted<T>();

        public ICharacterRepository Characters => _parent.Characters;

        public ICharacterAbilityDrawerRepository CharacterAbilityDrawers => _parent.CharacterAbilityDrawers;

        public ICharacterAppearanceRepository CharacterAppearances => _parent.CharacterAppearances;

        public ICharacterInventoryRepository CharacterInventories => _parent.CharacterInventories;

        public ICharacterLockboxRepository CharacterLockboxes => _parent.CharacterLockboxes;

        public ICharacterLogosRepository CharacterLogoses => _parent.CharacterLogoses;

        public ICharacterMissionRepository CharacterMissions => _parent.CharacterMissions;
        public Char.MissionOffer.MissionOfferRepository MissionOffers => _parent.MissionOffers;
        public Char.CharacterMissionItem.ICharacterMissionItemRepository CharacterMissionItems => _parent.CharacterMissionItems;

        public ICharacterMissionDeadlineRepository CharacterMissionDeadlines =>
            _parent.CharacterMissionDeadlines;

        public ICharacterMissionProgressRepository CharacterMissionProgress =>
            _parent.CharacterMissionProgress;

        public ICharacterMissionScenarioRepository CharacterMissionScenario =>
            _parent.CharacterMissionScenario;

        public ICharacterOptionRepository CharacterOptions => _parent.CharacterOptions;

        public ICharacterFlagRepository CharacterFlags =>
            _parent.CharacterFlags;

        public ICharacterSkillsRepository CharacterSkills => _parent.CharacterSkills;

        public ICharacterStartingExperienceRepository CharacterStartingExperience =>
            _parent.CharacterStartingExperience;

        public ICharacterTeleporterRepository CharacterTeleporters => _parent.CharacterTeleporters;

        public ICharacterTitleRepository CharacterTitles => _parent.CharacterTitles;

        public IClanRepository Clans => _parent.Clans;

        public IClanInventoryRepository ClanInventories => _parent.ClanInventories;

        public IClanMemberRepository ClanMembers => _parent.ClanMembers;
        public IClanLockboxLogRepository ClanLockboxLogs => _parent.ClanLockboxLogs;

        public IFriendRepository Friends => _parent.Friends;

        public IGameAccountRepository GameAccounts => _parent.GameAccounts;

        public IIgnoredRepository Ignoreds => _parent.Ignoreds;

        public IItemRepository Items => _parent.Items;

        public IPetitionRepository Petitions => _parent.Petitions;

        public IUserOptionRepository UserOptions => _parent.UserOptions;
    }
}
