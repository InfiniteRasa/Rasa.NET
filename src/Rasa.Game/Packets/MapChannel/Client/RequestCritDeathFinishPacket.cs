namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/actions/critdeathfinish.py - RequestCritDeathFinish(actionId, actionArgId, targetId):
    /// the melee key pressed next to a creature in its Critical Death window
    /// (CRITICAL_DEATH_FINISHER 10000002, arg 1). The client picks the target itself, the nearest
    /// one within the action's range carrying a CRIT_PREDEATH_EFFECT from the player or a squad
    /// mate.
    /// </summary>
    public class RequestCritDeathFinishPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCritDeathFinish;

        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public ulong TargetId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = (uint)pr.ReadInt();

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    TargetId = (ulong)pr.ReadLong();
                    break;
                case PythonType.Int:
                    TargetId = (ulong)pr.ReadInt();
                    break;
                default:
                    pr.ReadNoneStruct();
                    break;
            }
        }
    }
}
