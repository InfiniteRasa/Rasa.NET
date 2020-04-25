using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AppearanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<uint, CharacterAppearanceEntry> AppearanceData { get; set; }

        public AppearanceDataPacket(Dictionary<uint, CharacterAppearanceEntry> appearanceData)
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

                pw.WriteUInt(appearance.Slot);
                pw.WriteTuple(2);
                pw.WriteUInt(appearance.Class);
                pw.WriteStruct(new Color(appearance.Color));
            }
        }
    }
}
