namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class IgnoreListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IgnoreList;

        // IgnoreList does nothing on client side
        public object IgnoreList { get; set; }    // unknown

        public IgnoreListPacket(object ignoreList)
        {
            IgnoreList = ignoreList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteNoneStruct();
        }
    }
}
