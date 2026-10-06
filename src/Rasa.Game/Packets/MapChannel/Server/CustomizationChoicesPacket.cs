using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// CustomizationChoices(entityId, choices): the answer to GetCustomizationChoices, which a
    /// customization item sends when it is used from the pack (customization.py InventoryUse).
    /// It is what opens the item's window (clientmethod.py Recv_CustomizationChoices posts
    /// UI_SHOW_CUSTOMIZATION), whatever the item changes.
    ///
    /// The choices are (itemClassId, itemTemplateId) pairs, listed in the order sent. Only the
    /// hairstyle and the face windows read them (customizationwindow.py _SetupHairStyleSelect,
    /// _SetupFaceSelect), and they keep the template id: it names the row, by the class the
    /// client has for it, and it is what RequestCustomization sends back. The colour windows
    /// take theirs from the item's palette or swatches.
    /// </summary>
    public class CustomizationChoicesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CustomizationChoices;

        public ulong EntityId { get; set; }
        public IReadOnlyList<(uint ClassId, uint TemplateId)> Choices { get; set; }

        public CustomizationChoicesPacket(ulong entityId, IReadOnlyList<(uint ClassId, uint TemplateId)> choices)
        {
            EntityId = entityId;
            Choices = choices;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(EntityId);
            pw.WriteList(Choices.Count);
            foreach (var choice in Choices)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)choice.ClassId);
                pw.WriteInt((int)choice.TemplateId);
            }
        }
    }
}
