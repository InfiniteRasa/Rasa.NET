using System.Diagnostics;

namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class AppearanceData : IPythonDataStruct
    {
        public EquipmentData SlotId { get; set; }
        public EntityClassId Class { get; set; }
        public Color Color { get; set; }
        public Color Hue2 { get; set; }

        public AppearanceData()
        {

        }

        public AppearanceData(CharacterAppearanceEntry entry)
        {
            SlotId = (EquipmentData) entry.Slot;
            Class = (EntityClassId)entry.Class;
            Color = new Color(entry.Color);
            Hue2 = new Color(2139062144);     // ToDO: get and save hue2 to database
        }

        public void Read(PythonReader pr)
        {
            SlotId = (EquipmentData) pr.ReadUInt();

            var count = pr.ReadTuple();
            if (count != 2)
                Debugger.Break();

            Class = (EntityClassId)pr.ReadUInt();
            Color = pr.ReadStruct<Color>();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteInt((int) SlotId);

            pw.WriteTuple(3);
            pw.WriteUInt((uint)Class);
            pw.WriteStruct(Color);

            Color.WriteEmpty(pw);
        }

        public CharacterAppearanceEntry GetDatabaseEntry(uint characterId)
        {
            return new CharacterAppearanceEntry(characterId, (uint)SlotId, (uint)Class, Color.Hue);
        }
    }
}
