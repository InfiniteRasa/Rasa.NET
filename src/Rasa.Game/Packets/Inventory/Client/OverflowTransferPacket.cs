namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// An item taken out of the overflow inventory: <c>(destType, entityId, quantity, slot)</c>, from
    /// client/inventory.py _TransferOverflowItem, which sends it only with destType
    /// PERSONALINVENTORY. Nothing in the retail client calls _TransferOverflowItem; see
    /// ClientPacketHandler.OverflowTransfer.
    ///
    /// Every argument is read leniently - an int, a long, or None (or anything else) as null -
    /// because nothing the retail client does fixes their types.
    /// </summary>
    public class OverflowTransferPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.OverflowTransfer;

        public long? DestType { get; set; }
        public long? EntityId { get; set; }
        public long? Quantity { get; set; }
        public long? Slot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            DestType = ReadNumber(pr);
            EntityId = ReadNumber(pr);
            Quantity = ReadNumber(pr);
            Slot = ReadNumber(pr);
        }

        private static long? ReadNumber(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.Int:
                    return pr.ReadInt();

                case PythonType.Long:
                    return pr.ReadLong();

                default:
                    pr.SkipValue();
                    return null;
            }
        }
    }
}
