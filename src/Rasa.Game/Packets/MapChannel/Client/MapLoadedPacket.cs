namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class MapLoadedPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MapLoaded;
        
        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}