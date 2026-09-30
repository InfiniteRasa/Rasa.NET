namespace Rasa.Packets.MapChannel.Client
{
    using System;

    using Data;
    using Memory;

    /// <summary>
    /// A developer's request for an entity's server skeleton: <c>(entityId,)</c>, from
    /// client/physicalentity.py GetServerSkeleton, <c>SendCallUserMethod('GetServerSkeleton',
    /// (self.entityId,))</c>. Nothing in the retail client calls it; see
    /// ClientPacketHandler.GetServerSkeleton.
    /// </summary>
    public class GetServerSkeletonPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GetServerSkeleton;

        /// <summary>The entity asked about; 0 if the client sent something that is not an id.</summary>
        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    EntityId = pr.ReadULong();
                    break;

                case PythonType.Int:
                    EntityId = (ulong)Math.Max(0, pr.ReadInt());
                    break;

                default:
                    pr.SkipValue();
                    EntityId = 0;
                    break;
            }
        }
    }
}
