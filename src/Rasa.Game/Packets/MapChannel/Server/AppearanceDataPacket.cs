using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// AppearanceData({slotId: (classId, hue, hue2)}): what an actor shows in each equipment slot.
    ///
    /// A player's hair and face are written as (classId, hue) pairs. The client takes either
    /// shape wherever it draws an actor (actor.py _ProcessAppearanceData, _ProcessSwapsets,
    /// GetSkinColor), and logs a warning for a pair. But the windows of the hairstyle, face,
    /// hair colour and skin colour items are older than the second hue and unpack two values
    /// from the player's own hair and face entries (customizationwindow.py _SetupHairStyleSelect
    /// and _SetupFaceSelect, customization.py PreviewHairStyle, PreviewFace, PreviewHairColor,
    /// PreviewSkinColor): given three, the hairstyle and the face window raise before they
    /// open, and the colour windows show no preview. Nothing in the world unpacks three from
    /// those two slots; the creation window does, from a character pod's (CharacterInfo), which
    /// is not this packet. A worn piece keeps three: the armour paint window
    /// (colorcustomizationwindow.py) and the class select (tierselect.py) unpack three from
    /// the slots they dress.
    /// </summary>
    public class AppearanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }

        /// <summary>A player's appearance: its hair and face go out as pairs.</summary>
        public bool OfPlayer { get; }

        public AppearanceDataPacket(Dictionary<EquipmentData, AppearanceData> appearanceData, bool ofPlayer = false)
        {
            AppearanceData = appearanceData;
            OfPlayer = ofPlayer;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                var appearance = t.Value;
                pw.WriteInt((int)appearance.SlotId);

                if (OfPlayer && appearance.SlotId is EquipmentData.Hair or EquipmentData.Face)
                {
                    pw.WriteTuple(2);
                    pw.WriteUInt(appearance.Class);
                    pw.WriteStruct(appearance.Color);
                    continue;
                }

                pw.WriteTuple(3);
                pw.WriteUInt(appearance.Class);
                pw.WriteStruct(appearance.Color);
                pw.WriteStruct(appearance.Hue2);
            }
        }
    }
}
