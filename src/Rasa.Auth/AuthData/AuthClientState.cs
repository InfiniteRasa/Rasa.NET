namespace Rasa.AuthData
{
    public enum AuthClientState : byte
    {
        None = 0,
        Connected = 1,
        LoggedIn = 2,
        ServerList = 3,
        Queued = 4,
        Redirecting = 5,
        Disconnected = 6
    }
}
