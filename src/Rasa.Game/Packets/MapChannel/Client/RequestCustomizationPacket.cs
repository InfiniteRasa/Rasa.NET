namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCustomizationPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCustomization;

        public int CustomizationActionArgId { get; set; }
        public ulong CustomizationEntityId { get; set; }
        public int SelectedClassTemplateId { get; set; }
        public ulong TargetEntityId { get; set; }
        public int Hue { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CustomizationActionArgId = pr.ReadInt();
            CustomizationEntityId = pr.ReadULong();
            SelectedClassTemplateId = pr.ReadInt();
            pr.ReadNoneStruct();
            pr.ReadNoneStruct();
        }
    }
}
