namespace Rasa.Data
{
    public enum ClientMessageOpcode : short
    {
        None                 = 0,
        Login                = 1,
        LoginResponse        = 2,
        Move                 = 3,
        MoveObject           = 4,
        NegotiateMoveChannel = 5,
        CallServerMethod     = 6,
        CallMethod           = 7,
        Ping                 = 8
    }
}
