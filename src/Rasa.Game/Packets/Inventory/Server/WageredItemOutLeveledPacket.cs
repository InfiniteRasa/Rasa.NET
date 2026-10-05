namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/prestige.py Recv_WageredItemOutLeveled(), to SysEntity.ClientPrestigeSystemId: the
    /// character is no longer within ITEM_WAGERING_LEVEL_RANGE of their wagered item. The client
    /// shows PM_WAGER_ITEM_LEVEL_BECOMES_INVALID.
    /// </summary>
    public class WageredItemOutLeveledPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WageredItemOutLeveled;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
