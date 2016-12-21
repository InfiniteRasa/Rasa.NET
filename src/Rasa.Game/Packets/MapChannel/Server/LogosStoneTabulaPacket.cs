namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LogosStoneTabulaPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogosStoneTabula;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(409);
            for (var i = 0; i < 409; i++)
            {
                pw.WriteInt(i);
            }
        }
    }
}
