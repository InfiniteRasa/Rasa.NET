namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class CharacterOptions : IPythonDataStruct
    {
        public CharacterOption OptionId { get; set; }
        public string Value { get; set; }

        public CharacterOptions()
        {
        }

        public CharacterOptions(CharacterOption optionId, string value)
        {
            OptionId = optionId;
            Value = value;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            OptionId = (CharacterOption)pr.ReadUInt();
            Value = pr.ReadUnicodeString();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)OptionId);
            pw.WriteUnicodeString(Value);
        }
    }
}
