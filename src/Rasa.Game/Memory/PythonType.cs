namespace Rasa.Memory
{
    public enum PythonType
    {
        NoneStruct      = 0x00,
        TrueStruct      = 0x01,
        ZeroStruct      = 0x02,
        Int             = 0x10,
        Long            = 0x20,
        Double          = 0x30,
        String          = 0x40,
        UnicodeString   = 0x50,
        Dictionary      = 0x60,
        List            = 0x70,
        Tuple           = 0x80
    }
}
