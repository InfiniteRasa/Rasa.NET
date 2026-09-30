namespace Rasa.Packets.Crafting.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// RequestDisassembleItem(kraftwerksId, itemId): reverse engineering an item into the component
    /// items of its modules, from client/augmentations/kraftwerks.py DisassembleItem. Only the old
    /// crafting window calls it, and nothing opens that window; see
    /// ClientPacketHandler.RequestDisassembleItem.
    ///
    /// Read leniently - an int, a long, or anything else as null - because nothing the retail
    /// client does fixes the types.
    /// </summary>
    public class RequestDisassembleItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestDisassembleItem;

        public long? KraftwerksId { get; set; }
        public long? ItemId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            KraftwerksId = ReadNumber(pr);
            ItemId = ReadNumber(pr);
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
