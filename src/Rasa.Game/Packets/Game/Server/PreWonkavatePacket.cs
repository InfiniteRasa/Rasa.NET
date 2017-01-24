namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    // PreWonkavatePacket initialize loading screen
    public class PreWonkavatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreWonkavate;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(0); // wonktype isnt used by game client, so we send 0
        }
    }
}
