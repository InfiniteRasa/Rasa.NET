namespace Rasa.Data
{
    public enum QueueState
    {
        Authenticating = 0,
        Authenticated  = 1,
        InQueue        = 2,
        Redirecting    = 3,
        Disconnected   = 4
    }
}
