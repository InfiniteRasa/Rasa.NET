﻿using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AppearanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }

        public AppearanceDataPacket(Dictionary<EquipmentData, AppearanceData> appearanceData)
        {
            AppearanceData = appearanceData;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                var appearance = t.Value;
                pw.WriteInt((int)appearance.SlotId);
                pw.WriteTuple(3);
                pw.WriteUInt(appearance.Class);
                pw.WriteStruct(appearance.Color);
                pw.WriteStruct(appearance.Hue2);
            }
        }
    }
}
