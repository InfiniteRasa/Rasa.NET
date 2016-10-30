namespace Rasa.Data
{
    public enum CommOpcode : byte
    {
        LoginRequest       = 0,
        LoginResponse      = 1,
        RedirectRequest    = 2,
        RedirectResponse   = 3,
        ServerInfoRequest  = 4,
        ServerInfoResponse = 5
    }
}
