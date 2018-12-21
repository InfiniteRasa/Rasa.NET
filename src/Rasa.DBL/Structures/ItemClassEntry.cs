﻿using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemClassEntry
    {
        public uint ClassId { get; set; }
        public int InventoryIconStringId { get; set; }
        public int LootValue { get; set; }
        public bool HiddenInventoryFlag { get; set; }
        public bool IsConsumableFlag { get; set; }
        public int MaxHitPoints { get; set; }
        public uint StackSize { get; set; }
        public int DragAudioSetId { get; set; }
        public int DropAudioSetId { get; set; }

        public static ItemClassEntry Read(MySqlDataReader reader)
        {
            return new ItemClassEntry
            {
                ClassId = reader.GetUInt32("classId"),
                InventoryIconStringId = reader.GetInt32("inventoryIconStringId"),
                LootValue = reader.GetInt32("lootValue"),
                HiddenInventoryFlag = reader.GetBoolean("hiddenInventoryFlag"),
                IsConsumableFlag = reader.GetBoolean("isConsumableFlag"),
                MaxHitPoints = reader.GetInt32("maxHitPoints"),
                StackSize = reader.GetUInt32("stackSize"),
                DragAudioSetId = reader.GetInt32("dragAudioSetId"),
                DropAudioSetId = reader.GetInt32("dropAudioSetId")
            };
        }
    }
}
