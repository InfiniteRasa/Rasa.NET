namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A placed apartment decoration taken back into the inventory: <c>(entityId,)</c>, from
    /// client/augmentations/decoration.py Decoration.OnReturnItemToInventory. Nothing in the retail
    /// client calls it; see ClientPacketHandler.RequestReturnItemToInventory.
    ///
    /// Read leniently - an int, a long, or anything else as null - because nothing the retail
    /// client does fixes its type.
    /// </summary>
    public class RequestReturnItemToInventoryPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestReturnItemToInventory;

        public long? EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = ReadNumber(pr);
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
