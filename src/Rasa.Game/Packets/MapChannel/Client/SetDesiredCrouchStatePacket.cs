namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetDesiredCrouchStatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetDesiredCrouchState;

        public int DesiredStateId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            DesiredStateId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
