using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/prestige.py Recv_ReceivedItemLootPrestige(amount, qualityId, itemTemplateId,
    /// moduleIds), on the prestige system entity (SysEntity.ClientPrestigeSystemId): "You received
    /// %(amount)s prestige points for obtaining Experimental item %(itemName)s" for quality 4
    /// (PM_PRESTIGE_POINTS_RECEIVED_ITEMEXPERIMENTAL), "... Prototype item ..." for quality 5
    /// (PM_PRESTIGE_POINTS_RECEIVED_ITEMPROTOTYPE). Any other quality is an error in the client's
    /// log and nothing on screen. A notice only: the balance is UpdateCredits'.
    ///
    /// The name is the template's (gameuiutil.GetItemName), from the client's own data, or the
    /// name of the highest-priority module if there are any.
    /// </summary>
    public class ReceivedItemLootPrestigePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReceivedItemLootPrestige;

        public int Amount { get; set; }
        public int QualityId { get; set; }
        public uint ItemTemplateId { get; set; }
        public IReadOnlyList<int> ModuleIds { get; set; }

        public ReceivedItemLootPrestigePacket(int amount, int qualityId, uint itemTemplateId, IReadOnlyList<int> moduleIds = null)
        {
            Amount = amount;
            QualityId = qualityId;
            ItemTemplateId = itemTemplateId;
            ModuleIds = moduleIds ?? new List<int>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Amount);
            pw.WriteInt(QualityId);
            pw.WriteUInt(ItemTemplateId);
            pw.WriteList(ModuleIds.Count);
            foreach (var moduleId in ModuleIds)
                pw.WriteInt(moduleId);
        }
    }
}
