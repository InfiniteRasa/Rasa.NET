namespace Rasa.Data
{
    public enum LoginErrorCodes : byte
    {
        VersionMismatch      = 0,
        AuthenticationFailed = 1,
        ServerNotReady       = 2,
        AccountLocked        = 3,
        AlreadyLoggedIn      = 4
    }
}
