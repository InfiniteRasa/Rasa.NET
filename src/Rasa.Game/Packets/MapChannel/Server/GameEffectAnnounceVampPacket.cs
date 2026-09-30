using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'AnnounceVamp', (sourceData, targetData)) on the entity
    /// holding a vampiric damage effect (VAMPIRIC_HEALTH/POWER/CHI/ARMOR_DAMAGE, 324-327).
    /// BaseVampiricDamageEffect.Recv_AnnounceVamp floats each (attributeId, amount) over the
    /// effect's source and over its holder, announces the attribute on each, and writes the combat
    /// log for whichever of the two is the player at that client.
    /// </summary>
    public class GameEffectAnnounceVampPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public List<(Attributes Attribute, int Amount)> SourceData { get; } = new();
        public List<(Attributes Attribute, int Amount)> TargetData { get; } = new();

        public GameEffectAnnounceVampPacket(int effectId)
        {
            EffectId = effectId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("AnnounceVamp");
            pw.WriteTuple(2);                   // args = (sourceData, targetData)
            WriteList(pw, SourceData);
            WriteList(pw, TargetData);
        }

        private static void WriteList(PythonWriter pw, List<(Attributes Attribute, int Amount)> data)
        {
            pw.WriteList(data.Count);

            foreach (var (attribute, amount) in data)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)attribute);
                pw.WriteInt(amount);
            }
        }
    }
}
