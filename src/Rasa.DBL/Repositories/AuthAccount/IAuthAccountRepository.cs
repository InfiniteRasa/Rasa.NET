namespace Rasa.Repositories.AuthAccount
{
    using System.Net;
    using Structures;

    public interface IAuthAccountRepository
    {
        void Create(string email, string userName, string password);

        AuthAccountEntry GetByUserName(string name);

        void UpdateLoginData(uint id, IPAddress remoteAddress);

        void UpdateLastServer(uint id, byte lastServerId);
    }
}