﻿namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PersonalInventory_DestroyItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PersonalInventory_DestroyItem;

        public long EntityId { get; set; }
        public uint Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadLong();
            Quantity = (uint)pr.ReadLong();
        }
    }
}
