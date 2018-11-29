namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AllocateAttributePointsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AllocateAttributePoints;

        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            pr.ReadTuple();
            Body = pr.ReadInt();
            Mind = pr.ReadInt();
            Spirit = pr.ReadInt();
        }
    }
}
