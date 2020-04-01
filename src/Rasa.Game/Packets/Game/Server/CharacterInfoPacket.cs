using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Database.Tables.Character;
    using Memory;
    using Structures;

    public class CharacterInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;

        public uint SlotId { get; set; }
        public bool IsSelected { get; set; }
        public CharacterEntry Entry { get; set; }
        public BodyData BodyData { get; set; }
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; } = new Dictionary<EquipmentData, AppearanceData>();
        public CharacterData CharacterData { get; set; }
        public string FamilyName { get; set; }
        public uint GameContextId { get; set; }
        public LoginData LoginData { get; set; }
        public ClanData ClanData { get; set; }

        public CharacterInfoPacket(uint slotId, bool isSelected, string familyName)
        {
            SlotId = slotId;
            IsSelected = isSelected;
            FamilyName = familyName;
        }

        public CharacterInfoPacket(uint slotId, bool isSelected, string familyName, CharacterEntry entry)
            : this(slotId, isSelected, familyName)
        {
            Entry = entry;
  
            if (Entry != null)
            {
                CharacterData = new CharacterData(Entry);
                BodyData = new BodyData(Entry);
                LoginData = new LoginData(Entry);
                GameContextId = CharacterData.MapContextId;

                foreach (var appearanceEntry in CharacterAppearanceTable.GetAppearances(entry.Id))
                {
                    var appearanceData = new AppearanceData(appearanceEntry.Value);

                    AppearanceData.Add(appearanceData.SlotId, appearanceData);
                }

                var clanEntry = ClanTable.GetClanByCharacterId(Entry.Id);
                if (clanEntry != null)
                    ClanData = new ClanData(clanEntry);
            }
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(9);

            pw.WriteString("SlotId");
            pw.WriteUInt(SlotId);

            pw.WriteString("IsSelected");
            pw.WriteInt(IsSelected ? 1 : 0);

            pw.WriteString("BodyData");
            pw.WriteStruct(BodyData);

            pw.WriteString("CharacterData");
            pw.WriteStruct(CharacterData);

            pw.WriteString("AppearanceData");
            pw.WriteDictionary(AppearanceData.Count);

            foreach (var appearance in AppearanceData)
                appearance.Value.Write(pw);

            pw.WriteString("UserName");
            if (FamilyName != null)
                pw.WriteUnicodeString(FamilyName);
            else
                pw.WriteNoneStruct();

            pw.WriteString("GameContextId");
            if (GameContextId == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteUInt(GameContextId);

            pw.WriteString("LoginData");
            pw.WriteStruct(LoginData);

            pw.WriteString("ClanData");
            pw.WriteStruct(ClanData);
        }
    }
}
