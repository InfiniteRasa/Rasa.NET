namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A transfer credit slip being used from the inventory: <c>(entityId,)</c>, the item's own id.
    ///
    /// <c>client/augmentations/transfercredit.py</c> sends this from <c>InventoryUse</c>, the
    /// right-click handler. The one class that carries the augmentation, 73
    /// <see cref="AugmentationType.TransferCredit"/>, is entity class 10000062 "Transfer Credit"
    /// ("A transfer credit slip."), item template 10000003.
    ///
    /// Until this existed the opcode had no handler, and an unhandled opcode fails the packet
    /// terminator check and closes the connection - so right-clicking a slip disconnected the
    /// player.
    /// </summary>
    public class RequestUseTransferCreditPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestUseTransferCredit;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
