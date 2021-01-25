using Rasa.Game;

namespace Rasa.Managers
{
    using Data;

    public interface IManifestationManager
    {
        void RequestToggleRun(Client client);

        void SetDesiredCrouchState(Client client, Posture posture);
    }
}