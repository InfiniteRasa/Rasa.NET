using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Rasa.Structures.Char;
    using Structures;
    using System;

    public class AppearanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }

        public AppearanceDataPacket(List<CharacterAppearanceEntry> appearanceData)
        {
            AppearanceData = ToPythonAppearanceData(appearanceData);
        }

        private Dictionary<EquipmentData, AppearanceData> ToPythonAppearanceData(List<CharacterAppearanceEntry> appearanceData)
        {
            var data = new Dictionary<EquipmentData, AppearanceData>();

            foreach(var entry in appearanceData)
            {
                data.Add((EquipmentData)entry.Slot, new AppearanceData(entry));
            }

            return data;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                var appearance = t.Value;
                pw.WriteInt((int)appearance.SlotId);
                pw.WriteTuple(2);
                pw.WriteUInt((uint)appearance.Class);
                pw.WriteTuple(4);
                pw.WriteInt(appearance.Color.Red);
                pw.WriteInt(appearance.Color.Green);
                pw.WriteInt(appearance.Color.Blue);
                pw.WriteInt(appearance.Color.Alpha);
            }
        }
    }
}
