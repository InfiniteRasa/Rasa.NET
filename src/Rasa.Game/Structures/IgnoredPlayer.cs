namespace Rasa.Structures
{
    using Game;
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

        public IgnoredPlayer(Client client)
        {
            CharacterId = client.Player.Id;
            UserId = client.AccountEntry.Id;
            CharacterName = client.Player.Name;
            FamilyName = client.Player.FamilyName;
            IsOnline = true;
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
