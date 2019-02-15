namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class BodyAttributesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BodyAttributes;

        public double Scale { get; set; }
        public uint Hue { get; set; }
        public bool IgnoreABVs { get; set; }
        public bool IgnoreWS { get; set; }      // WS is WalkableSurfaces
        public uint Hue2 { get; set; }

        public BodyAttributesPacket(double scale, uint hue, bool ignoreABVs, bool ignoreWS, uint hue2)
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
            pw.WriteUInt(Hue);
            pw.WriteBool(IgnoreABVs);
            pw.WriteBool(IgnoreWS);
            pw.WriteUInt(Hue2);
        }
    }
}
