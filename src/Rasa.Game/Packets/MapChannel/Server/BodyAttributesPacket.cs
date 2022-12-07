namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Rasa.Structures;

    public class BodyAttributesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BodyAttributes;

        public double Scale { get; set; }
        public Color Hue { get; set; }
        public int IgnoreABVs { get; set; }
        public int IgnoreWS { get; set; }      // WS is WalkableSurfaces
        public Color Hue2 { get; set; }

        public BodyAttributesPacket(double scale, Color hue, int ignoreABVs, int ignoreWS, Color hue2)
        {
            Scale = scale;
            Hue = hue;
            IgnoreABVs = ignoreABVs;
            IgnoreWS = IgnoreWS;
            Hue2 = hue2;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteDouble(Scale);
            pw.WriteStruct(Hue);
            pw.WriteInt(1);     // ToDo : IgnoreABVs unknown
            pw.WriteInt(1);     // ToDo : IgnoreWS unknown
            pw.WriteStruct(Hue2);
        }
    }
}
