namespace Rasa.Repositories.Char
{
    using Auction;
    using Character;
    using CharacterAppearance;
    using CharacterSkills;
    using Clan;
    using ClanInventory;
    using ClanLockboxLog;
    using ClanMember;
    using GameAccount;
    using CharacterAbilityDrawer;
    using UnitOfWork;
    using CensorWord;
    using CharacterInventory;
    using CharacterLockbox;
    using CharacterLogos;
    using CharacterActionReuse;
    using CharacterMission;
    using CharacterMissionDeadline;
    using CharacterMissionProgress;
    using CharacterMissionScenario;
    using CharacterOption;
    using CharacterFlag;
    using CharacterTeleporter;
    using CharacterTitle;
    using CharacterStartingExperience;
    using Friend;
    using Ignored;
    using Items;
    using Petition;
    using UserOption;

    public interface ICharUnitOfWork : IUnitOfWork
    {
        void ExecuteTransaction(System.Action operation) =>
            throw new System.NotSupportedException(
                "This character unit of work does not support transactions.");
        T Enlist<T>(System.Func<T> create) where T : class, ITransactionParticipant =>
            throw new System.NotSupportedException("This character unit of work cannot enlist transaction state.");
        bool HasEnlisted<T>() where T : class, ITransactionParticipant => false;

        IAuctionRepository Auctions { get; }
        ICensoredWordRepository CensoredWords { get; }
        ICharacterRepository Characters { get; }
        ICharacterAbilityDrawerRepository CharacterAbilityDrawers { get; }
        ICharacterAppearanceRepository CharacterAppearances { get; }
        ICharacterInventoryRepository CharacterInventories { get; }
        ICharacterLockboxRepository CharacterLockboxes { get; }
        ICharacterLogosRepository CharacterLogoses { get; }
        ICharacterActionReuseRepository CharacterActionReuses { get; }
        ICharacterMissionRepository CharacterMissions { get; }
        MissionOffer.MissionOfferRepository MissionOffers =>
            throw new System.NotSupportedException("This character unit of work has no mission offer authority store.");
        CharacterMissionItem.ICharacterMissionItemRepository CharacterMissionItems =>
            throw new System.NotSupportedException("This character unit of work has no mission item ledger.");
        ClanFeud.IClanFeudRepository ClanFeuds =>
            throw new System.NotSupportedException("This character unit of work has no clan feud store.");
        ControlPointState.IControlPointStateRepository ControlPointStates =>
            throw new System.NotSupportedException("This character unit of work has no control point state store.");
        PvpRecord.IPvpRecordRepository PvpRecords =>
            throw new System.NotSupportedException("This character unit of work has no PvP record store.");
        GmCommandLog.IGmCommandLogRepository GmCommandLogs =>
            throw new System.NotSupportedException("This character unit of work has no game master audit log.");
        ChatLog.IChatLogRepository ChatLogs =>
            throw new System.NotSupportedException("This character unit of work has no chat log.");
        CharacterBossKill.ICharacterBossKillRepository CharacterBossKills =>
            throw new System.NotSupportedException("This character unit of work has no boss kill store.");
        SquadInstance.ISquadInstanceRepository SquadInstances =>
            throw new System.NotSupportedException("This character unit of work has no squad instance store.");
        ICharacterMissionDeadlineRepository CharacterMissionDeadlines { get; }
        ICharacterMissionProgressRepository CharacterMissionProgress { get; }
        ICharacterMissionScenarioRepository CharacterMissionScenario { get; }
        ICharacterOptionRepository CharacterOptions { get; }
        ICharacterFlagRepository CharacterFlags { get; }
        ICharacterSkillsRepository CharacterSkills { get; }
        ICharacterStartingExperienceRepository CharacterStartingExperience { get; }
        ICharacterTeleporterRepository CharacterTeleporters { get; }
        ICharacterTitleRepository CharacterTitles { get; }
        IClanRepository Clans { get; }
        IClanInventoryRepository ClanInventories { get; }
        IClanMemberRepository ClanMembers { get; }
        IClanLockboxLogRepository ClanLockboxLogs { get; }
        IFriendRepository Friends { get; }
        IGameAccountRepository GameAccounts { get; }
        IIgnoredRepository Ignoreds { get; }
        IItemRepository Items { get; }
        IPetitionRepository Petitions { get; }
        IUserOptionRepository UserOptions { get; }
    }
}
