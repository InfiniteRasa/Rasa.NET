namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_SetHue(r, g, b, a) on an OwnableForceField: its colour, 0-255 each, which it divides by
    /// 255 and puts on its body (HueBody) now and whenever its blocking changes.
    /// </summary>
    public class SetHuePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetHue;

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public SetHuePacket(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(R);
            pw.WriteInt(G);
            pw.WriteInt(B);
            pw.WriteInt(A);
        }
    }
}
