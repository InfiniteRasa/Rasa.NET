using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class CharacterInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;

        private readonly bool _empty;

        public uint SlotId { get; set; }
        public bool IsSelected { get; set; }
        public BodyDataTuple BodyData { get; set; }
        public List<AppearanceData> AppearanceData { get; set; }
        public CharacterDataTuple CharacterData { get; set; }
        public string UserName { get; set; }
        public int GameContextId { get; set; }
        public object LoginData { get; set; }
        public object ClanData { get; set; }

        public CharacterInfoPacket(uint slotId, bool empty)
        {
            SlotId = slotId;
            _empty = empty;
        }

        public CharacterInfoPacket(uint slotId, bool isSelected, BodyDataTuple bodyData, CharacterDataTuple characterData, List<AppearanceData> appearanceData, string userName, int gameContextId, LoginDataTupple loginData, ClanDataTupple clanData)
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
            throw new System.NotImplementedException(); // todo
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(9);

            pw.WriteString("SlotId");
            pw.WriteUInt(SlotId);

            pw.WriteString("IsSelected");
            pw.WriteInt(SlotId == 0 ? 1 : 0);

            pw.WriteString("BodyData");
            if (BodyData != null && !_empty)
            {
                pw.WriteTuple(2);
                pw.WriteInt(BodyData.GenderClassId);
                pw.WriteDouble(BodyData.Scale);
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("CharacterData");
            if (CharacterData != null && !_empty)
            {
                pw.WriteTuple(10);
                pw.WriteUnicodeString(CharacterData.Name);
                pw.WriteInt(CharacterData.GameContextId);
                pw.WriteInt(CharacterData.Experience); // xpptrs
                pw.WriteInt(CharacterData.Level); // xplvl
                pw.WriteInt(CharacterData.Body); // body
                pw.WriteInt(CharacterData.Mind); //mind
                pw.WriteInt(CharacterData.Spirit); // spirit
                pw.WriteInt(CharacterData.ClassId); // class id
                pw.WriteInt(CharacterData.CloneCredits); // clone credits
                pw.WriteInt(CharacterData.RaceId); // raceId
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("AppearanceData");
            if (AppearanceData != null && !_empty)
            {
                pw.WriteDictionary(AppearanceData.Count);
                foreach (var t in AppearanceData)
                {
                    pw.WriteInt(t.SlotId);
                    pw.WriteTuple(2);
                    pw.WriteInt(t.ClassId);
                    pw.WriteTuple(4);
                    pw.WriteInt(t.Color.Red);
                    pw.WriteInt(t.Color.Green);
                    pw.WriteInt(t.Color.Blue);
                    pw.WriteInt(t.Color.Alpha);
                }
            }
            else
                pw.WriteTuple(0);

            pw.WriteString("UserName");
            if (UserName != null && !_empty)
            {
                pw.WriteUnicodeString(UserName);
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("GameContextId");
            if (!_empty)
                pw.WriteInt(GameContextId);
            else
                pw.WriteNoneStruct();

            pw.WriteString("LoginData");
            if (LoginData != null && !_empty)
            {
                pw.WriteTuple(3);
                pw.WriteInt(0); // num logins
                pw.WriteInt(0); // total time played
                pw.WriteInt(0); // time since last played
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("ClanData");
            if (ClanData != null && !_empty)
            {
                pw.WriteTuple(2);
                pw.WriteInt(0); // clan id
                pw.WriteUnicodeString("Clan name"); // clan name
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
        public int GameContextId { get; set; }
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
