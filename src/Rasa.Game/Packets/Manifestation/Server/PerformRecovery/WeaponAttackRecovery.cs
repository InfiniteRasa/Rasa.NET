namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using Data;
    using Memory;
    using Structures;

    public class WeaponAttackRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public Missile Missile { get; set; }

        public WeaponAttackRecovery(Missile missile)
        {
            Missile = missile;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)Missile.ActionId);   // actionId
            pw.WriteUInt(Missile.ActionArgId);      // actionargId
            pw.WriteList(Missile.Args.HitEntities.Count);    // Hits
            foreach (var entity in Missile.Args.HitEntities)
                pw.WriteULong(entity);
            pw.WriteList(Missile.Args.MisstEntities.Count);  // misses
            foreach (var entity in Missile.Args.MisstEntities)
                pw.WriteULong(entity);
            DamageInfoWriter.WriteMissTypes(pw, Missile.Args.Missdata);  // missdata: a misstype for each miss
            pw.WriteList(Missile.Args.HitData.Count);
            foreach (var hit in Missile.Args.HitData)
            {
                pw.WriteTuple(3);
                pw.WriteULong(hit.EntityId);         // target entityid
                DamageInfoWriter.WriteRawInfo(pw, hit, TypeOf(hit));    // rawInfo, with the effects the hit put on its target
                pw.WriteTuple(1);                   // OnHitData
                pw.WriteList(0);
            }
        }

        private DamageType TypeOf(HitData hit)
        {
            var type = hit.DamageType != 0 ? hit.DamageType : Missile.DamageType;

            return type == 0 ? DamageType.Physical : type;
        }
    }
}
