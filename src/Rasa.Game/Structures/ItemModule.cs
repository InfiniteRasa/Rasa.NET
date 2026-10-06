using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Memory;
    using World;

    /// <summary>
    /// An item module, as the world database has it (module_class, module_effect): what an item
    /// carries in a module slot. See Managers.ItemModules.
    /// </summary>
    public class ItemModule
    {
        public uint ModuleId { get; }

        /// <summary>The strength, 1 to 5; 0 for a module that has none.</summary>
        public uint Level { get; }

        /// <summary>The kind of bonus, 1 to 54; 0 for a module that has none.</summary>
        public uint VariantId { get; }

        /// <summary>What the client calls it; may be empty.</summary>
        public string Comment { get; }

        /// <summary>What it does, in the order the client is sent it. Empty when that is not known.</summary>
        public List<ModuleInfo> Effects { get; } = new List<ModuleInfo>();

        public ItemModule(ModuleClassEntry entry)
        {
            ModuleId = entry.Id;
            Level = entry.Level;
            VariantId = entry.VariantId;
            Comment = entry.Comment ?? "";
        }
    }

    /// <summary>
    /// One thing a module does: one of the tuples of ModuleTooltipInfo, which the client draws
    /// as a line of the item's tooltip.
    /// </summary>
    public class ModuleInfo
    {
        /// <summary>A game effect type; its tooltip is the line's text.</summary>
        public uint EffectId { get; }

        /// <summary>For a set's bonus, the pieces it takes; 0 otherwise.</summary>
        public uint SetLevel { get; }

        /// <summary>
        /// The three values of the amount, as the client reads them off the wire rather than as
        /// the table has them (PythonWriter.AsRead): 0.05 a level is 0.0500000007 there, and the
        /// client rounds what it makes of that up.
        /// </summary>
        public double FlatValue { get; }
        public double LinearValue { get; }
        public double ExpValue { get; }
        public int? Arg1 { get; }
        public int? Arg2 { get; }
        public int? Arg3 { get; }
        public int? Arg4 { get; }

        public ModuleInfo(ModuleEffectEntry entry)
        {
            EffectId = entry.EffectId;
            SetLevel = entry.SetLevel;
            FlatValue = PythonWriter.AsRead(entry.FlatValue);
            LinearValue = PythonWriter.AsRead(entry.LinearValue);
            ExpValue = PythonWriter.AsRead(entry.ExpValue);
            Arg1 = entry.Arg1;
            Arg2 = entry.Arg2;
            Arg3 = entry.Arg3;
            Arg4 = entry.Arg4;
        }

        /// <summary>
        /// The amount on an item of that level - its level requirement - worked out as the
        /// client works out the number it shows (tooltipwindow._AddModuleEffects):
        /// ceil(flat + linear * level + exp * 2^((level - 1) / 8)), from the same numbers.
        /// </summary>
        public int Amount(int itemLevel)
        {
            return (int)Math.Ceiling(FlatValue + LinearValue * itemLevel + ExpValue * Math.Pow(2, (itemLevel - 1) / 8.0));
        }
    }
}
