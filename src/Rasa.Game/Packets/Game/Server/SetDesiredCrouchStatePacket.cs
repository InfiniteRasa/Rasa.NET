namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class SetDesiredCrouchStatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetDesiredCrouchState;

        public uint DesiredCrouchState { get; }

        public SetDesiredCrouchStatePacket(Posture posture)
        {
            DesiredCrouchState = (uint)posture;
        }
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(DesiredCrouchState);
        }
    }
}