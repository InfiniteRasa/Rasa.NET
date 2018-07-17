namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class SetIsGMPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetIsGM;

        public bool IsGmAccount { get; set; }

        public SetIsGMPacket(bool isGm)
        {
            IsGmAccount = isGm;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsGmAccount);
        }
    }
}
