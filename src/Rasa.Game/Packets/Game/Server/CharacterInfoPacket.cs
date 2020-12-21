using System.Collections.Generic;

using JetBrains.Annotations;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;
    using Structures.Char;

    public class CharacterInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterInfo;

        public uint SlotId { get; set; }

        public bool IsSelected { get; set; }

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

        public CharacterInfoPacket(uint slotId, bool isSelected, string familyName, [NotNull] CharacterEntry entry)
            : this(slotId, isSelected, familyName)
        {
            CharacterData = new CharacterData(entry);
            BodyData = new BodyData(entry);
            LoginData = new LoginData(entry);
            GameContextId = entry.MapContextId;

            foreach (var appearanceEntry in entry.CharacterAppearance)
            {
                var appearanceData = new AppearanceData(appearanceEntry);
                AppearanceData.Add(appearanceData.SlotId, appearanceData);
            }

            var clanEntry = entry.Clan;
            if (clanEntry != null)
            {
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
