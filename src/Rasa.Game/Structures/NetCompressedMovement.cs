namespace Rasa.Structures
{
    public class NetCompressedMovement
    {
        public uint EntityId { get; set; }
        public uint PosX24b { get; set; }
        public uint PosY24b { get; set; }
        public uint PosZ24b { get; set; }
        public uint Velocity { get; set; }
        public uint Flag { get; set; }
        public uint ViewX { get; set; }
        public uint ViewY { get; set; }
    }
}
