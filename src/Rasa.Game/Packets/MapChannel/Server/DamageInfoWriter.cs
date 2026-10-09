using System;
using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// The client's shared/damageinfo.py rawInfo: the 12-tuple every damage announcement is
    /// built from - a weapon hit's hitdata, an ability's, a damage-over-time tick's, an effect's
    /// AnnounceDamage. One writer, so they all agree on the field order.
    /// </summary>
    public static class DamageInfoWriter
    {
        /// <param name="finalAmount">What got through to armour and health: after resistance and after a shield.</param>
        /// <param name="coverModifier">The share that got past cover: 1.0 for none in the way. The client's hit display is red at 0.8 and above, then orange, yellow, and green below 0.2 - so 0 reads as fully covered.</param>
        /// <param name="absorbed">What a shield on the target took (Shield Extender, Shield Wave). The client floats finalAmt + absorbed with the absorbed share in brackets ("-120(40)"), puts "(40 absorbed)" in the combat log, and plays the hit reaction for a hit the shield took all of.</param>
        /// <param name="wasImmune">The target was immune (Managers.DamageImmunity): the client shows "Immune" - floated over the target and a line in the combat log - in place of the damage.</param>
        /// <param name="targetEffectIds">The types of the effects the hit put on its target quietly: Actor.AnnounceDamage announces one of each on the target as the hit is played (Managers.HitEffects).</param>
        /// <param name="sourceEffectIds">The same for whoever dealt it. Sent only to a hit the client has the dealer of: it announces them on that entity without looking whether it is there.</param>
        public static void WriteRawInfo(PythonWriter pw, DamageType damageType, long finalAmount, int resisted = 0, bool isCritical = false, bool deathBlow = false, double coverModifier = 1.0, int absorbed = 0, bool wasImmune = false,
            IReadOnlyList<uint> targetEffectIds = null, IReadOnlyList<uint> sourceEffectIds = null)
        {
            pw.WriteTuple(12);
            pw.WriteUInt((uint)damageType);     // damageType
            pw.WriteUInt(0);                    // reflected
            pw.WriteUInt(0);                    // filtered
            pw.WriteUInt((uint)Math.Max(0, absorbed));  // absorbed
            pw.WriteUInt((uint)resisted);       // resisted
            pw.WriteLong(finalAmount);          // finalAmt
            pw.WriteInt(isCritical ? 1 : 0);    // isCrit
            pw.WriteInt(deathBlow ? 1 : 0);     // deathBlow
            pw.WriteDouble(coverModifier);      // coverModifier
            pw.WriteInt(wasImmune ? 1 : 0);     // wasImmune
            WriteEffectTypes(pw, targetEffectIds);
            WriteEffectTypes(pw, sourceEffectIds);
        }

        /// <summary>
        /// A weapon hit's rawInfo, from the hit as the missile path resolved it. damageType is
        /// the caller's: each recovery has its own rule for a hit that names none.
        /// </summary>
        public static void WriteRawInfo(PythonWriter pw, HitData hit, DamageType damageType)
        {
            pw.WriteTuple(12);
            pw.WriteUInt((uint)damageType);     // damageType
            pw.WriteUInt(hit.Reflected);        // reflected
            pw.WriteUInt(hit.Filtered);         // filtered
            pw.WriteUInt(hit.Absorbed);         // absorbed
            pw.WriteUInt(hit.Resisted);         // resisted
            pw.WriteLong(hit.FinalAmt);         // finalAmt: each hit its own (a launcher's splash)
            pw.WriteInt(hit.IsCritical);        // isCrit
            pw.WriteInt(hit.DeathBlow);         // deathBlow
            pw.WriteDouble(hit.CoverModifier);  // coverModifier
            pw.WriteInt(hit.WasImune);          // wasImmune
            WriteEffectTypes(pw, hit.TargetEffectIds);
            WriteEffectTypes(pw, hit.SourceEffectIds);
        }

        private static void WriteEffectTypes(PythonWriter pw, IReadOnlyList<uint> typeIds)
        {
            if (typeIds == null)
            {
                pw.WriteList(0);
                return;
            }

            pw.WriteList(typeIds.Count);

            foreach (var typeId in typeIds)
                pw.WriteUInt(typeId);
        }

        /// <summary>misstype 1, a plain miss ("Miss!"): what the client takes a miss with no type to be.</summary>
        public const uint PlainMiss = 1;

        /// <summary>
        /// A recovery's missdata: a misstype for each miss, in the order of the misses. The
        /// client looks every one up in its misstype table (TargetedAction.DoAction,
        /// combatmessages) and the table has no row 0 - a 0 here is a KeyError there, which
        /// costs the miss its message and the action its resolution - so a miss with no type is
        /// sent as the plain miss the client would have taken a missing one for.
        /// </summary>
        public static void WriteMissTypes(PythonWriter pw, IReadOnlyList<uint> missTypes)
        {
            pw.WriteList(missTypes?.Count ?? 0);

            if (missTypes == null)
                return;

            foreach (var missType in missTypes)
                pw.WriteUInt(missType == 0 ? PlainMiss : missType);
        }

        /// <summary>A tooltip or argument value of one of the types Python packets carry; a list of ints is a Python list.</summary>
        public static void WriteValue(PythonWriter pw, object value)
        {
            switch (value)
            {
                case null: pw.WriteNoneStruct(); break;
                case double d: pw.WriteDouble(d); break;
                case float f: pw.WriteDouble(f); break;
                case int i: pw.WriteInt(i); break;
                case uint u: pw.WriteUInt(u); break;
                case long l: pw.WriteLong(l); break;
                case ulong ul: pw.WriteULong(ul); break;
                case bool b: pw.WriteBool(b); break;
                case string s: pw.WriteString(s); break;
                case IList<(int, int)> pairs:
                    // a list of 2-tuples - Polymorph's abilityInfo, [(abilityId, level), ...]
                    pw.WriteList(pairs.Count);
                    foreach (var (first, second) in pairs)
                    {
                        pw.WriteTuple(2);
                        pw.WriteInt(first);
                        pw.WriteInt(second);
                    }
                    break;
                case IList<int> list:
                    pw.WriteList(list.Count);
                    foreach (var item in list)
                        pw.WriteInt(item);
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Python packet: unsupported value type {value.GetType().Name}");
                    pw.WriteNoneStruct();
                    break;
            }
        }
    }
}
