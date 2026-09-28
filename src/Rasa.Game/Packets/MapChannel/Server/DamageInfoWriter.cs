using System;
using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

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
        public static void WriteRawInfo(PythonWriter pw, DamageType damageType, long finalAmount, int resisted = 0, bool isCritical = false, bool deathBlow = false, double coverModifier = 1.0, int absorbed = 0)
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
            pw.WriteInt(0);                     // wasImmune
            pw.WriteList(0);                    // targetEffectIds
            pw.WriteList(0);                    // sourceEffectIds
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
