namespace Rasa.AuthData
{
    public enum ClientOpcode : byte
    {
        Login         = 0x00,
        AboutToPlay   = 0x02,
        Logout        = 0x03,
        ServerListExt = 0x05,
        SCCheck       = 0x06,
    }
}
