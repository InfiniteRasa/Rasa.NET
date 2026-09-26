namespace Rasa.Game.Missions.Integration
{
    using Managers;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    internal interface IStartingExperiencePolicy
    {
        string ContentRevision { get; }
        bool CanSkip(GameAccountEntry account);
        bool TrySelect(Client client, bool skip, CharacterEntry character, ICharUnitOfWork unit,
            out CharacterStartingExperienceState? state);
        MapChannel ResolveMap(CharacterEntry character, CharacterStartingExperienceState? state);
        void OfferStartingExperienceMission(Client client);
        bool IsExitWaypoint(uint waypointId);
        bool IsExitPad(uint mapContextId, uint waypointId);
        bool IsDepartureReady(Client client);
        bool TryDepart(Client client, DynamicObjectManager objects);
    }

    internal static class StartingExperienceComposition
    {
        internal static IStartingExperiencePolicy Create(IGameUnitOfWorkFactory factory, MissionApplication missions) =>
            new Content.Bootcamp.BootcampEntryPolicy(factory, missions);
    }
}
