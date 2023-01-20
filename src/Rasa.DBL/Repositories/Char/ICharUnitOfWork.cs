namespace Rasa.Repositories.Char
{
    using Character;
    using CharacterAppearance;
    using CharacterSkills;
    using Clan;
    using ClanInventory;
    using ClanMember;
    using GameAccount;
    using CharacterAbilityDrawer;
    using UnitOfWork;
    using CensorWord;
    using CharacterInventory;
    using CharacterLockbox;
    using CharacterLogos;
    using CharacterMission;
    using CharacterOption;
    using CharacterTeleporter;
    using CharacterTitle;
    using Friend;
    using Ignored;
    using Items;
    using UserOption;

    public interface ICharUnitOfWork : IUnitOfWork
    {
        ICensoredWordRepository CensoredWords { get; }
        ICharacterRepository Characters { get; }
        ICharacterAbilityDrawerRepository CharacterAbilityDrawers { get; }
        ICharacterAppearanceRepository CharacterAppearances { get; }
        ICharacterInventoryRepository CharacterInventories { get; }
        ICharacterLockboxRepository CharacterLockboxes { get; }
        ICharacterLogosRepository CharacterLogoses { get; }
        ICharacterMissionRepository CharacterMissions { get; }
        ICharacterOptionRepository CharacterOptions { get; }
        ICharacterSkillsRepository CharacterSkills { get; }
        ICharacterTeleporterRepository CharacterTeleporters { get; }
        ICharacterTitleRepository CharacterTitles { get; }
        IClanRepository Clans { get; }
        IClanInventoryRepository ClanInventories { get; }
        IClanMemberRepository ClanMembers { get; }
        IFriendRepository Friends { get; }
        IGameAccountRepository GameAccounts { get; }
        IIgnoredRepository Ignoreds { get; }
        IItemRepository Items { get; }
        IUserOptionRepository UserOptions { get; }
    }
}