using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'AnnounceHealing', (healData,)): asks the effect on
    /// this entity to announce healing it did to others - the client's ConversionEffect takes
    /// [(entityId, amount), ...], floats each as healing from the effect's source and marks the
    /// effect as having ticked on them. This is how an effect that heals somebody other than its
    /// holder shows its numbers, since a tick heals the holder.
    /// </summary>
    public class GameEffectAnnounceHealingPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public List<(ulong EntityId, int Amount)> Heals { get; } = new List<(ulong, int)>();

        public GameEffectAnnounceHealingPacket(int effectId)
        {
            EffectId = effectId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("AnnounceHealing");
            pw.WriteTuple(1);                   // args = (healData,)
            pw.WriteList(Heals.Count);

            foreach (var heal in Heals)
            {
                pw.WriteTuple(2);
                pw.WriteULong(heal.EntityId);
                pw.WriteInt(heal.Amount);
            }
        }
    }
}
