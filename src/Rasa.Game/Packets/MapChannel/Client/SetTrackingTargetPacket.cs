namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetTrackingTargetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTrackingTarget;

        public ulong EntityId { get; set; }

        public SetTrackingTargetPacket()
        {
        }

        public SetTrackingTargetPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        /// <summary>
        /// (entityId,) from playermovementmgr._StartTracking. An id the server gave out with
        /// WriteULong comes back as a long; a small one would be an int, and None is taken as 0.
        /// </summary>
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    EntityId = pr.ReadULong();
                    break;

                case PythonType.Structs:
                    pr.ReadUnkStruct();
                    EntityId = 0;
                    break;

                default:
                    var value = pr.ReadInt();
                    EntityId = value > 0 ? (ulong)value : 0;
                    break;
            }
        }
    }
}
