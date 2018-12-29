namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestWeaponAttackPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestWeaponAttack;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public long TargetId { get; set; }
        public bool IsAltAction { get; set; }       // ToDo

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ActionId = pr.ReadInt();
            ActionArgId = pr.ReadInt();
            if (pr.PeekType() == PythonType.Long)   // has target
                TargetId = pr.ReadLong();
            else
                pr.ReadNoneStruct();                // no target
            pr.ReadBool();
        }
    }
}
