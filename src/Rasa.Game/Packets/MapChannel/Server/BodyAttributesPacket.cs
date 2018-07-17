namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class BodyAttributesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BodyAttributes;

        public double Scale { get; set; }
        public int Hue { get; set; }
        public int IgnoreABVs { get; set; }
        public int IgnoreWS { get; set; }      // WS is WalkableSurfaces
        public int Hue2 { get; set; }

        public BodyAttributesPacket(double scale, int hue, int ignoreABVs, int ignoreWS, int hue2)
        {
            Scale = scale;
            Hue = hue;
            IgnoreABVs = ignoreABVs;
            IgnoreWS = IgnoreWS;
            Hue2 = hue2;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDouble(Scale);
            pw.WriteInt(Hue);
            pw.WriteInt(IgnoreABVs);
            pw.WriteInt(IgnoreWS);
            pw.WriteInt(Hue2);
        }
    }
}
