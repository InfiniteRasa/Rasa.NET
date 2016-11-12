namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CharacterInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;

        private readonly bool _empty;

        public int SlotId { get; set; }
        public bool IsSelected { get; set; }
        public BodyDataTuple BodyData { get; set; }
        public AppearanceDataTuple AppearanceData { get; set; }
        public object CharacterData { get; set; }
        public string UserName { get; set; }
        public int GameContextId { get; set; }
        public object LoginData { get; set; }
        public object ClanData { get; set; }

        public CharacterInfoPacket(int slotId, bool empty)
        {
            SlotId = slotId;
            _empty = empty;
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
            pw.WriteInt(SlotId);

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
                pw.WriteTuple(10); // TODO
                pw.WriteUnicodeString("CharName");
                pw.WriteInt(1); // pos
                pw.WriteInt(41); // xpptrs
                pw.WriteInt(10); // xplvl
                pw.WriteInt(111); // body
                pw.WriteInt(12); //mind
                pw.WriteInt(21); // spirit
                pw.WriteInt(2); // class id
                pw.WriteInt(3); // clone credits
                pw.WriteInt(3); // raceId
            }
            else
                pw.WriteNoneStruct();

            pw.WriteString("AppearanceData");
            if (AppearanceData != null && !_empty)
            {
                // TODO
                var count = 0;
                for (var i = 0; i < 21; ++i)
                {
                    if (true) // TODO:
                        ++count;
                }

                pw.WriteDictionary(count);

                for (var i = 0; i < 21; ++i)
                {
                    if (true)
                    {
                        pw.WriteInt(i + 1); // equipment slot id
                        pw.WriteTuple(2);
                        pw.WriteInt(0); // classId

                        pw.WriteTuple(4);
                        pw.WriteInt(0); // hueR
                        pw.WriteInt(0); // hueG
                        pw.WriteInt(0); // hueB
                        pw.WriteInt(0); // hueA
                    }// TODO: else nonstruct?
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

    public class AppearanceDataTuple
    {
    }
}
