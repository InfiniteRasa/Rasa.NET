namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/prestige.py Recv_ReceivedCreatureKillPrestige(entityId, amount), on the prestige
    /// system entity (SysEntity.ClientPrestigeSystemId): "You received %(amount)s prestige points
    /// for killing %(creatureName)s" (PM_PRESTIGE_POINTS_RECEIVED_CREATURE_KILL). The client
    /// takes the name from the creature, so it is sent while the corpse is still there.
    /// </summary>
    public class ReceivedCreatureKillPrestigePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReceivedCreatureKillPrestige;

        public ulong EntityId { get; set; }
        public int Amount { get; set; }

        public ReceivedCreatureKillPrestigePacket(ulong entityId, int amount)
        {
            EntityId = entityId;
            Amount = amount;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(EntityId);
            pw.WriteInt(Amount);
        }
    }
}
