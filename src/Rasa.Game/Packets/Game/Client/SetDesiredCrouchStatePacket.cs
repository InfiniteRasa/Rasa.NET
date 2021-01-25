namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class SetDesiredCrouchStatePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetDesiredCrouchState;

        public uint DesiredCrouchState { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            DesiredCrouchState = pr.ReadUInt();
        }
    }
}