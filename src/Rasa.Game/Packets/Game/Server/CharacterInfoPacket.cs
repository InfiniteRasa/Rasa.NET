using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class CharacterInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;
        
        public uint SlotId { get; set; }
        public int IsSelected { get; set; }
        public BodyDataTuple BodyData { get; set; }
        public CharacterDataTuple CharacterData { get; set; }
        public string UserName { get; set; }
        public int GameContextId { get; set; }
        public LoginDataTupple LoginData { get; set; }
        public ClanDataTupple ClanData { get; set; }
        public List<AppearanceData> AppearanceData { get; set; } = new List<AppearanceData>();
        public Color Color { get; set; }

        public CharacterInfoPacket(uint slotId, int isSelected)
        {
            SlotId = slotId;
            IsSelected = isSelected;
        }

        public CharacterInfoPacket(uint slotId, int isSelected, BodyDataTuple bodyData, CharacterDataTuple characterData, List<AppearanceData> appearanceData, string userName, int gameContextId, LoginDataTupple loginData, ClanDataTupple clanData)
        {
            SlotId = slotId;
            IsSelected = isSelected;
            BodyData = bodyData;
            CharacterData = characterData;
            AppearanceData = appearanceData;
            UserName = userName;
            GameContextId = gameContextId;
            LoginData = loginData;
            ClanData = clanData;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(9);

            pw.WriteString("SlotId");
            pw.WriteUInt(SlotId);

            pw.WriteString("IsSelected");
            pw.WriteInt(IsSelected);

            pw.WriteString("BodyData");
            if (BodyData != null)
            {
                pw.WriteTuple(2);
                pw.WriteInt(BodyData.GenderClassId);
                pw.WriteDouble(BodyData.Scale);
            }
            else
            {
                pw.WriteNoneStruct();
            }
            pw.WriteString("CharacterData");
            if (CharacterData != null)
            {
                pw.WriteTuple(10);
                pw.WriteUnicodeString(CharacterData.Name);
                pw.WriteInt(CharacterData.MapContextId);
                pw.WriteInt(CharacterData.Experience);
                pw.WriteInt(CharacterData.Level);
                pw.WriteInt(CharacterData.Body);
                pw.WriteInt(CharacterData.Mind);
                pw.WriteInt(CharacterData.Spirit);
                pw.WriteInt(CharacterData.ClassId);
                pw.WriteInt(CharacterData.CloneCredits);
                pw.WriteInt(CharacterData.RaceId);
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("AppearanceData");
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                pw.WriteInt((int)t.SlotId);
                pw.WriteTuple(2);
                pw.WriteInt(t.ClassId);
                pw.WriteTuple(4);
                pw.WriteInt(t.Color.Red);
                pw.WriteInt(t.Color.Green);
                pw.WriteInt(t.Color.Blue);
                pw.WriteInt(t.Color.Alpha);
            }

            pw.WriteString("UserName");
            pw.WriteUnicodeString(UserName);
           
            pw.WriteString("GameContextId");
            pw.WriteInt(GameContextId);
            
            pw.WriteString("LoginData");
            if (LoginData != null)
            {
                pw.WriteTuple(3);
                pw.WriteInt(LoginData.NumLogins);
                pw.WriteInt(LoginData.TotalTimePlayed);
                pw.WriteInt(LoginData.TimeSinceLastPlayed);
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("ClanData");
            if (ClanData != null)
            {
                pw.WriteTuple(2);
                pw.WriteInt(ClanData.ClanId);
                pw.WriteUnicodeString(ClanData.ClanName);
            }
            else
                pw.WriteNoneStruct();
        }
    }

    public class BodyDataTuple
    {
        public int GenderClassId { get; set; }
        public double Scale { get; set; }
    }

    public class CharacterDataTuple
    {
        public string Name { get; set; }
        public int MapContextId { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public int ClassId { get; set; }
        public int CloneCredits { get; set; }
        public int RaceId { get; set; }
    }

    public class LoginDataTupple
    {
        public int NumLogins { get; set; }
        public int TotalTimePlayed { get; set; }
        public int TimeSinceLastPlayed { get; set; }
    }

    public class ClanDataTupple
    {
        public int ClanId { get; set; }
        public string ClanName { get; set; }
    }
}
