using System.Collections.Generic;

namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    public class InventoryCreatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryCreate;

        public InventoryType InventoryType { get; set; }
        public List<ulong> ListOfItems = new List<ulong>();
        public int InventorySize { get; set; }

        public InventoryCreatePacket(InventoryType inventoryType, List<ulong> listOfItems, int inventorySize)
        {
            InventoryType = inventoryType;
            ListOfItems = listOfItems;
            InventorySize = inventorySize;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)InventoryType);

            // (entityId, slot): client/inventory.py CreateInventory reads each entry as
            // "for (entityId, slot) in itemList". This once wrote (slot, entityId); CreateInventory
            // resets the inventory first, throwing away the items the InventoryAddItems just before
            // it had placed, then filed each slot number as an entity sitting in the slot an entity
            // id named - so the clan lockbox showed nothing after a login or map change until a
            // lockbox change reloaded it. Empty slots are left out, as InventoryReload leaves them:
            // see InventoryReloadPacket.
            InventoryReloadPacket.WriteItems(pw, InventoryType, ListOfItems);

            pw.WriteInt(InventorySize);
        }
    }
}
