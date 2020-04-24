namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class DeleteCharacterFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DeleteCharacterFailed;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
