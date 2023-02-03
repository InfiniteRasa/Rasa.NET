namespace Rasa.Data;

public enum ClientState : byte
{
    None = 0,
    Connected = 1,
    LoggedIn = 2,
    ServerList = 3,
    Queued = 4,
    Redirecting = 5,
    Disconnected = 6
}
