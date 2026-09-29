using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_GameEffectAttached(typeId, effectId, level, sourceId, announce, tooltipDict, *args).
    /// The client creates the effect class for typeId and calls its Attach(target, sourceId,
    /// *args), so whatever follows the tooltip dictionary in the tuple is passed straight to that
    /// effect's OnAttach. The sprint effect, for one, takes a single bead-modifier float there;
    /// most effects take nothing. Args holds those trailing values, each written as its own
    /// tuple element - a list in that position would arrive as one argument, a list.
    ///
    /// The tooltip dictionary is read by BaseGameEffect.SetTooltipDict: duration (seconds; left
    /// out for an effect with no end, so the client shows no timer), damageType and attrId
    /// (turned into words), isActive, isBuff, isDebuff, isNegativeEffect, and then whatever the
    /// effect's tooltip format string names - Extras carries those.
    /// </summary>
    public class GameEffectAttachedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectAttached;

        public int EffectTypeId { get; set; }
        public int EffectId { get; set; }
        public uint EffectLevel { get; set; }
        public ulong SourceId { get; set; }
        public bool Announced { get; set; }
        // tooltip
        /// <summary>Seconds to run, or null for an effect that stays until detached.</summary>
        public int? Duration { get; set; }
        public int DamageType { get; set; }
        public int AttrId { get; set; }
        public bool IsActive { get; set; }
        public bool IsBuff { get; set; }
        public bool IsDebuff { get; set; }
        public bool IsNegativeEffect { get; set; }

        /// <summary>The effect's own tooltip values, by the names its format string uses.</summary>
        public Dictionary<string, object> Extras { get; set; } = new Dictionary<string, object>();

        /// <summary>Extra OnAttach arguments; double, int, uint, long, ulong, bool, string or null.</summary>
        public List<object> Args { get; set; } = new List<object>();

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6 + Args.Count);
            pw.WriteInt(EffectTypeId);      //typeId
            pw.WriteInt(EffectId);          //effectId
            pw.WriteUInt(EffectLevel);      //level
            pw.WriteULong(SourceId);        //sourceId
            pw.WriteBool(Announced);        //announce

            WriteTooltip(pw);               //tooltipDict

            foreach (var arg in Args)
                DamageInfoWriter.WriteValue(pw, arg);
        }

        /// <summary>
        /// The tooltip dictionary alone - also what Recv_GameEffectUpdateTooltip takes
        /// (GameEffectUpdateTooltipPacket), which the client reads the same way and puts in place
        /// of the whole of the old one.
        /// </summary>
        public void WriteTooltip(PythonWriter pw)
        {
            pw.WriteDictionary(5 + (Duration.HasValue ? 1 : 0) + (DamageType != 0 ? 1 : 0) + Extras.Count);

            if (Duration.HasValue)
            {
                pw.WriteString("duration");
                pw.WriteInt(Duration.Value);
            }

            // Turned into the damage type's name by the client; 0 is no type and no line.
            if (DamageType != 0)
            {
                pw.WriteString("damageType");
                pw.WriteInt(DamageType);
            }

            pw.WriteString("attrId");
            pw.WriteInt(AttrId);
            pw.WriteString("isActive");
            pw.WriteBool(IsActive);
            pw.WriteString("isBuff");
            pw.WriteBool(IsBuff);
            pw.WriteString("isDebuff");
            pw.WriteBool(IsDebuff);
            pw.WriteString("isNegativeEffect");
            pw.WriteBool(IsNegativeEffect);

            foreach (var (key, value) in Extras)
            {
                pw.WriteString(key);
                DamageInfoWriter.WriteValue(pw, value);
            }
        }
    }
}
