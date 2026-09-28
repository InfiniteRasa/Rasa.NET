using System.Collections.Generic;

namespace Rasa.Login
{
    using Networking;

    public delegate void LoginDelegate(LoginClient client);

    public class LoginManager
    {
        public LoginDelegate OnLogin { get; set; }
        public LoginDelegate OnFail { get; set; }
        public List<LoginClient> Clients { get; } = new List<LoginClient>();

        public void LoginSocket(LengthedSocket socket)
        {
            var client = new LoginClient(this, socket);

            lock (Clients)
                Clients.Add(client);

            try
            {
                client.Start();
            }
            catch
            {
                client.Close();
                throw;
            }
        }

        public void ExchangeDone(LoginClient client)
        {
            if (!client.TryCompleteExchange())
                return;

            lock (Clients)
                Clients.Remove(client);

            OnLogin?.Invoke(client);
        }

        public void Disconnect(LoginClient client)
        {
            lock (Clients)
                Clients.Remove(client);

            OnFail?.Invoke(client);
        }
    }
}
