using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_GameEffectTick(effectId, tickData) for a constant-fire weapon's effect on its shooter
    /// (client/actions/weapons/constantfire.py). The client's ConstantFireEffect.OnTick pops one
    /// pulse off tickData per shot of the interval and plays each as the weapon's hits and misses
    /// - a pulse is (shots, missData), a shot (targetId, rawInfo, onHitData) - and ticks the
    /// shooter's heat, ammo and bead. The leech gun's DensityGunEffect.OnTick first unpacks
    /// tickData as (healData, damageData), announcing healAmount as healing and repairAmount as
    /// armour repaired on each (entityId, healAmount, repairAmount), and hands damageData on as
    /// the pulses.
    /// </summary>
    public class ConstantFireTickPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectTick;

        public int EffectId { get; }

        /// <summary>The leech gun's shape, (healData, damageData); plain pulses otherwise.</summary>
        public bool WithHeals { get; }

        /// <summary>One entry per pulse, each the shots it landed - for the leech gun, one shot at the target or none.</summary>
        public List<List<TickEntry>> Pulses { get; } = new List<List<TickEntry>>();

        public List<(ulong EntityId, int Heal, int Repair)> Heals { get; } = new List<(ulong, int, int)>();

        public ConstantFireTickPacket(int effectId, bool withHeals)
        {
            EffectId = effectId;
            WithHeals = withHeals;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(EffectId);

            if (WithHeals)
            {
                pw.WriteTuple(2);                   // tickData = (healData, damageData)
                pw.WriteList(Heals.Count);

                foreach (var (entityId, heal, repair) in Heals)
                {
                    pw.WriteTuple(3);
                    pw.WriteULong(entityId);
                    pw.WriteInt(heal);
                    pw.WriteInt(repair);
                }
            }

            pw.WriteList(Pulses.Count);

            foreach (var pulse in Pulses)
            {
                pw.WriteTuple(2);                   // (shots, missData)
                pw.WriteList(pulse.Count);

                foreach (var shot in pulse)
                {
                    pw.WriteTuple(3);               // (targetId, rawInfo, onHitData)
                    pw.WriteULong(shot.EntityId);
                    DamageInfoWriter.WriteRawInfo(pw, shot.DamageType, shot.Amount, shot.Resisted, shot.IsCritical, shot.DeathBlow, absorbed: shot.Absorbed);
                    pw.WriteNoneStruct();
                }

                pw.WriteList(0);                    // missData
            }
        }
    }
}
