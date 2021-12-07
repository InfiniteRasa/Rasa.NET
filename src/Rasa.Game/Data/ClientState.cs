namespace Rasa.Data
{
    public enum ClientState : byte
    {
        None               = 0,
        Connected          = 1,
        LoggedIn           = 2,
        CharacterSelection = 3,
        Loading            = 4,
        Ingame             = 5,
        Disconnected       = 6,
        Teleporting        = 7
    }
}
