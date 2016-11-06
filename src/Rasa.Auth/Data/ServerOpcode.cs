namespace Rasa.Data
{
    public enum ServerOpcode : byte
    {
        ProtocolVersion           = 0x00,
        LoginFail                 = 0x01,
        BlockedAccount            = 0x02,
        LoginOk                   = 0x03,
        SendServerListExt         = 0x04,
        SendServerListFail        = 0x05,
        PlayFail                  = 0x06,
        PlayOk                    = 0x07,
        AccountKicked             = 0x08,
        BlockedAccountWithMessage = 0x09,
        SCCheckReq                = 0x0A, // null
        Unknown1                  = 0x0B, // null, or in different vTable
        HandOffToQueue            = 0x0C,
        HandoffToGame             = 0x0E
    }
}
