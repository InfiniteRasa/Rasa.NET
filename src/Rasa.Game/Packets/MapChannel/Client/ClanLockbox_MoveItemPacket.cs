using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ClanLockbox_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanLockbox_MoveItem;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            DestSlot = pr.ReadUInt();
            if (pr.PeekType() == PythonType.Int)
                Quantity = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Long)
                Quantity = (int)pr.ReadLong();
            else
                throw new Exception("ClanLockbox_MoveItem: unsuported PythonType");
        }
    }
}
