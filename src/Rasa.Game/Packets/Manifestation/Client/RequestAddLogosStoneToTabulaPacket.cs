namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A Logos stone being used from the inventory: <c>(entityId,)</c>, the item's own id.
    ///
    /// client/augmentations/logosstone.py sends this from InventoryUse, the right-click handler of
    /// an item with the LOGOSSTONE augmentation (54), when the manifestation does not already have
    /// the stone's Logos; if it has, the client says PM_ALREADY_HAVE_LOGOS_STONE itself and sends
    /// nothing.
    /// </summary>
    public class RequestAddLogosStoneToTabulaPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestAddLogosStoneToTabula;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
