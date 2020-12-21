namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    // PreWonkavatePacket initialize loading screen
    public class PreWonkavatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreWonkavate;

        public int WonkType { get; set; } = 0; // wonktype isn't used by game client, so we send 0

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(WonkType);
        }
    }
}