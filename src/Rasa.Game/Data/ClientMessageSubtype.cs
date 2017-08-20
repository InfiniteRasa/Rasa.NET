using System;

namespace Rasa.Data
{
    [Flags]
    public enum ClientMessageSubtypeFlag : byte
    {
        None       = 0x00,
        Unk1       = 0x01,
        HasSubtype = 0x02,
        Unk4       = 0x04
    }

    public enum LoginMessageSubtype : byte
    {
        Type1 = 1,
        Type2 = 2
    }

    public enum LoginResponseMessageSubtype : byte
    {
        Handoff       = 1,
        HandoffFailed = 2,
        WaitForLogout = 3,
        Success       = 4,
        Failed        = 5
    }

    public enum CallMethodMessageSubtype : byte
    {
        MethodId          = 1,
        MethodName        = 2,
        UnkPlusMethodId   = 3,
        UnkPlusMethodName = 4
    }
}
