namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCustomizationPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCustomization;

        public int CustomizationActionArgId { get; set; }
        public long CustomizationEntityId { get; set; }
        public int SelectedClassTemplateId { get; set; }
        public int TargetEntityId { get; set; }
        public int Hue { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"RequestCustomization:\n {pr.ToString()}");
            pr.ReadTuple();
            CustomizationActionArgId = pr.ReadInt();
            CustomizationEntityId = pr.ReadLong();
            SelectedClassTemplateId = pr.ReadInt();
            pr.ReadNoneStruct();
            pr.ReadNoneStruct();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
