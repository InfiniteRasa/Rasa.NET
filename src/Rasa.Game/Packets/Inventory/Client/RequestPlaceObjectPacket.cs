namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// An inventory item placed as an apartment decoration: <c>(itemId, destEntityId or None,
    /// socketId)</c>, from client/augmentations/decoration.py
    /// Decoration.OnPlaceItemAtSelectedLocation - the item chosen, the entity whose decoration
    /// socket it goes in, and that socket. Nothing in the retail client calls it; see
    /// ClientPacketHandler.RequestReturnItemToInventory. What placing it would send back is
    /// <see cref="Server.WorldPlacementDescriptorPacket"/>.
    ///
    /// Every argument is read leniently - an int, a long, or None (or anything else) as null -
    /// because nothing the retail client does fixes their types.
    /// </summary>
    public class RequestPlaceObjectPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestPlaceObject;

        public long? ItemId { get; set; }
        public long? DestEntityId { get; set; }
        public long? SocketId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ItemId = ReadNumber(pr);
            DestEntityId = ReadNumber(pr);
            SocketId = ReadNumber(pr);
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
