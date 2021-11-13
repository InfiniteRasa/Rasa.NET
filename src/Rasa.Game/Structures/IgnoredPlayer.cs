namespace Rasa.Structures
{
    using Memory;

    public class IgnoredPlayer : IPythonDataStruct
    {
        public ulong CharacterId { get; set; }
        public uint UserId { get; set; }
        public string CharacterName { get; set; }
        public string FamilyName { get; set; }
        public bool IsOnline { get; set; }

        public IgnoredPlayer()
        {
        }

        public IgnoredPlayer(CharacterEntry character, string familyName)
        {
            CharacterId = character.Id;
            UserId = character.AccountId;
            CharacterName = character.Name;
            FamilyName = familyName;
            IsOnline = character.IsOnline;
        }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteList(5);
            pw.WriteULong(CharacterId);
            pw.WriteUInt(UserId);
            pw.WriteUnicodeString(CharacterName);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteBool(IsOnline);
        }
    }
}
