namespace Rasa.Structures
{
    using Game;
    using Memory;

    public class Friend : IPythonDataStruct
    {
        public ulong CharacterId { get; set; }
        public uint UserId { get; set; }
        public string CharacterName { get; set; }
        public string FamilyName { get; set; }
        public uint Level { get; set; }
        public uint ContextId { get; set; }
        public bool IsOnline { get; set; }
        
        public Friend()
        {
        }
        
        public Friend(Client client)
        {
            CharacterId = client.MapClient.Player.CharacterId;
            UserId = client.AccountEntry.Id;
            CharacterName = client.MapClient.Player.Actor.Name;
            FamilyName = client.MapClient.Player.Actor.FamilyName;
            Level = client.MapClient.Player.Level;
            ContextId = client.MapClient.Player.Actor.MapContextId;
            IsOnline = true;
        }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteList(7);
            pw.WriteULong(CharacterId);
            pw.WriteUInt(UserId);
            pw.WriteUnicodeString(CharacterName);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteUInt(Level);
            pw.WriteUInt(ContextId);
            pw.WriteBool(IsOnline);
        }
    }
}
