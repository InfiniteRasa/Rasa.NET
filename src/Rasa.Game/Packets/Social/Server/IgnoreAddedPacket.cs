namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;
    using Structures;

    public class IgnoreAddedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IgnoreAdded;

        public IgnoredPlayer IgnoredPlayer { get; set; }

        public IgnoreAddedPacket(IgnoredPlayer ignoredPlayer)
        {
            IgnoredPlayer = ignoredPlayer;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteStruct(IgnoredPlayer);
        }
    }
}
