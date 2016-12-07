using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class CharacterInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;
        
        public int SlotId { get; set; }
        public int IsSelected { get; set; }
        public BodyDataTuple BodyData { get; set; }
        public CharacterDataTuple CharacterData { get; set; }
        public string UserName { get; set; }
        public int GameContextId { get; set; }
        public LoginDataTupple LoginData { get; set; }
        public ClanDataTupple ClanData { get; set; }
        public Dictionary<int, AppearanceData> AppearanceData { get; set; } = new Dictionary<int, AppearanceData>();
        public Color Color { get; set; }

        public override void Read(PythonReader pr ){ }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(9);

            pw.WriteString("SlotId");
            pw.WriteInt(SlotId);

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
            if (AppearanceData.Count != 0)
            {
                pw.WriteDictionary(21);
                for (var i = 0; i < 21; ++i)
                {
                    pw.WriteInt(i+1);
                    pw.WriteTuple(2);
                    pw.WriteInt(AppearanceData[i+1].ClassId);
                    pw.WriteTuple(4);
                    pw.WriteInt(AppearanceData[i+1].Color.Red);
                    pw.WriteInt(AppearanceData[i+1].Color.Green);
                    pw.WriteInt(AppearanceData[i+1].Color.Blue);
                    pw.WriteInt(AppearanceData[i+1].Color.Alpha);
                }
            }
            else
                pw.WriteDictionary(0);

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
                pw.WriteString(LoginData.TimeSinceLastPlayed);
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
        public string TimeSinceLastPlayed { get; set; }
    }

    public class ClanDataTupple
    {
        public int ClanId { get; set; }
        public string ClanName { get; set; }
    }
}
