namespace Rasa.Managers
{
    using Game;

    public interface IManifestationManager
    {
        void RequestToggleRun(Client client);

        void SetDesiredCrouchState(Client client, bool crouching);
    }
}