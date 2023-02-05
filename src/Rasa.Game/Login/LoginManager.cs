using System.Collections.Generic;

namespace Rasa.Login;

using Rasa.Networking;

public delegate void LoginDelegate(LoginClient client);

public class LoginManager
{
    public LoginDelegate OnLogin { get; set; }
    public LoginDelegate OnFail { get; set; }
    public List<LoginClient> Clients { get; } = new List<LoginClient>();

    public void LoginSocket(AsyncLengthedSocket socket)
    {
        lock (Clients)
            Clients.Add(new LoginClient(this, socket));
    }

    public void ExchangeDone(LoginClient client)
    {
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
