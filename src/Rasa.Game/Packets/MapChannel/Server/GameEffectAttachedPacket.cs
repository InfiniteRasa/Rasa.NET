namespace Rasa.Packets.MapChannel.Server
{
    // ToDo need future work on this packet
    using Data;
    using Memory;

    public class GameEffectAttachedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectAttached;

        public int EffectTypeId { get; set; }
        public int EffectId { get; set; }
        public int EffectLevel { get; set; }
        public int SourceId { get; set; }
        public bool Announced { get; set; }
        // tooltip
        public int Duration { get; set; }
        public int DamageType { get; set; }
        public int AttrId { get; set; }
        public bool IsActive { get; set; }
        public bool IsBuff { get; set; }
        public bool IsDebuff { get; set; }
        public bool IsNegativeEffect { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(7);
            pw.WriteInt(EffectTypeId);      //typeId
            pw.WriteInt(EffectId);          //effectId
            pw.WriteInt(EffectLevel);       //level
            pw.WriteInt(SourceId);          //sourceId
            pw.WriteBool(Announced);        //announce
            pw.WriteDictionary(7);          //tooltipDict
            pw.WriteString("duration"); // 'duration'
            pw.WriteInt(Duration);
            pw.WriteString("damageType");// 'damageType'
            pw.WriteInt(DamageType);
            pw.WriteString("attrId");   // 'attrId'
            pw.WriteInt(AttrId);
            pw.WriteString("isActive"); // 'isActive'
            pw.WriteBool(IsActive);
            pw.WriteString("isBuff");   // 'isBuff'
            pw.WriteBool(IsBuff);
            pw.WriteString("isDebuff"); // 'isDebuff'
            pw.WriteBool(IsDebuff);
            pw.WriteString("isNegativeEffect"); // 'isNegativeEffect'
            pw.WriteBool(IsNegativeEffect);
            // args (variable)
            pw.WriteList(0);
        }
    }
}
