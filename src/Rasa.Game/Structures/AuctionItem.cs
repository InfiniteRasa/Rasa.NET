namespace Rasa.Structures
{
    using Memory;

    public class AuctionItem : IPythonDataStruct
    {
        public uint ItemId { get; set; }
        public string Sellername { get; set; }
        public uint BidPrice { get; set; }
        public uint BuyoutPrice { get; set; }
        public uint RemainingDuration { get; set; }
        public uint ItemTemplateId { get; set; }
        public uint StackSize { get; set; }
        public uint LootModuleId1 { get; set; }
        public uint LootModuleId2 { get; set; }
        public uint LootModuleId3 { get; set; }
        public uint LootModuleId4 { get; set; }
        public uint QualitiId { get; set; }
        public uint LevelRequirement { get; set;}

        public void Read(PythonReader pr)
        {
        }
        
        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(13);
            pw.WriteUInt(ItemId);               // kItemId = 0
            pw.WriteUnicodeString(Sellername);  // kSellerName = 1
            pw.WriteUInt(BidPrice);             // kBidPrice = 2
            pw.WriteUInt(BuyoutPrice);          // kBuyoutPrice = 3
            pw.WriteUInt(RemainingDuration);    // kRemainingDur = 4
            pw.WriteUInt(ItemTemplateId);       // kItemTemplateId = 5
            pw.WriteUInt(StackSize);            // kStackSize = 6
            if (LootModuleId1 != 0)             // kLootModule1 = 7
                pw.WriteUInt(LootModuleId1);
            else
                pw.WriteNoneStruct();
            if (LootModuleId2 != 0)             // kLootModule2 = 8
                pw.WriteUInt(LootModuleId2);
            else
                pw.WriteNoneStruct();
            if (LootModuleId3 != 0)             // kLootModule3 = 9
                pw.WriteUInt(LootModuleId3);
            else
                pw.WriteNoneStruct();
            if (LootModuleId4 != 0)             // kLootModule4 = 10
                pw.WriteUInt(LootModuleId4);
            else
                pw.WriteNoneStruct();
            pw.WriteUInt(QualitiId);             // kQualityId = 11
            pw.WriteUInt(LevelRequirement);      // kLevelReq = 12
        }
    }
}
