namespace Rasa.Data;

public enum FailReason : byte
{
    UnexpectedError = 0,
    UserNameOrPassword = 2,
    SSNInformationUnavailable = 5,
    NoAvailableServers = 6,
    AlreadyLoggedIn = 7,
    ServerIsDown = 8,
    Kicked = 11,
    AgeRestricted = 12,
    ServerIsFull = 15,
    MustChangePassword = 17,
    OutOfTime = 18
}
