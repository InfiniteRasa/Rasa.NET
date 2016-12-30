namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;
    
    public class InventoryManager
    {
        public static void InitForClient(MapChannelClient mapClient)
        {
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.Personal, InventorySize = 250 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.WeaponDrawerInventory, InventorySize = 5 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.EquipedInventory, InventorySize = 5 });

            //var item[272], qty[277];
            //DataInterface_Character_getCharacterInventory(client->tempCharacterData->characterID, item, qty, _inventory_initForClient, client);
        }
    }
}
