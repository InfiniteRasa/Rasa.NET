using System.Collections.Generic;

namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py Recv_PartyMemberLoot(userId, creatureEntityId, lootClassIds, moneyAmount):
    /// a squad mate took loot. lootClassIds is [(entityClassId, quantity, itemId)], each printed as
    /// "X looted N Y." For a userId the recipient does not have in its squad list - its own - the
    /// client prints "You looted" and posts the pick-up as its own, which GotLoot already does, so
    /// this goes to the others only. moneyAmount is read but, for a squad mate, never printed.
    /// </summary>
    public class PartyMemberLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyMemberLoot;

        public uint UserId { get; }
        public ulong CreatureEntityId { get; }
        public List<(uint ClassId, uint Quantity, ulong ItemId)> Loot { get; }
        public int Money { get; }

        public PartyMemberLootPacket(uint userId, ulong creatureEntityId, List<(uint ClassId, uint Quantity, ulong ItemId)> loot, int money)
        {
            UserId = userId;
            CreatureEntityId = creatureEntityId;
            Loot = loot;
            Money = money;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(UserId);
            pw.WriteULong(CreatureEntityId);
            pw.WriteList(Loot.Count);

            foreach (var (classId, quantity, itemId) in Loot)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(classId);
                pw.WriteUInt(quantity);
                pw.WriteULong(itemId);
            }

            pw.WriteInt(Money);
        }
    }
}
