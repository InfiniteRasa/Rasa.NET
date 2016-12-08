namespace Rasa.Data
{
    public enum ClientState : byte
    {
        None = 0,
        Connected = 1,
        LoggedIn = 2,
        CharacterSelection = 3,
        Ingame = 4,
        Disconnected = 5
    }
}
