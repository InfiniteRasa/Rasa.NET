namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class SetControlledActorIdPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetControlledActorId;

        public uint EntetyId { get; set; }
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)EntetyId);
        }
    }
}
