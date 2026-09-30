using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

        /// <summary>
        /// Closes connections that have not finished the key exchange within the timeout. The real
        /// client answers the server key at once; one that does not is holding a receive buffer
        /// from the pool every connection shares, and nothing used to let it go.
        /// </summary>
        public int ExpireStalled(TimeSpan timeout)
        {
            List<LoginClient> stalled;
            var cutoff = DateTime.UtcNow - timeout;

            lock (Clients)
                stalled = Clients.Where(c => c.ConnectedTime < cutoff).ToList();

            foreach (var client in stalled)
            {
                Disconnect(client);
                client.Close();
            }

            return stalled.Count;
        }

        /// <summary>Connections from this address still in the key exchange.</summary>
        public int CountFrom(IPAddress address)
        {
            lock (Clients)
                return Clients.Count(c => address.Equals(c.Socket.RemoteAddress));
        }

        public void Disconnect(LoginClient client)
        {
            lock (Clients)
                Clients.Remove(client);

            OnFail?.Invoke(client);
        }
    }
}
