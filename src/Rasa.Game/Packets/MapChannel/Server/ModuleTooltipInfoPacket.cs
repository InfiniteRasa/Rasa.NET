namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// The answer to RequestTooltipForModuleId: gameui.Recv_ModuleTooltipInfo(moduleId,
    /// moduleLevel, info), sent to the client's game UI. info is a list of
    /// (effectId, setLevel, flatValue, linearValue, expValue, arg1, arg2, arg3, arg4), and each
    /// one whose effect has a tooltip is a line under the name of every item that carries the
    /// module (tooltipwindow._AddModuleEffects):
    ///  - the text is the effect's tooltip, with %(amount)s the three values worked out for the
    ///    item's level and %(arg1)s to %(arg4)s the arguments;
    ///  - before it, "(n)" for a set bonus that takes n pieces, else "[n]" with the module's
    ///    strength - which the client reads from its own crafting data, not from moduleLevel.
    ///
    /// A module the server has no effects for is answered with an empty list. The client asks
    /// again every time it draws the item until it has been answered, and an empty list is an
    /// answer: the item shows no line for that module.
    /// </summary>
    public class ModuleTooltipInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ModuleTooltipInfo;

        public uint ModuleId { get; }

        /// <summary>The module, or null for one the server does not know.</summary>
        public ItemModule Module { get; }

        public ModuleTooltipInfoPacket(uint moduleId, ItemModule module)
        {
            ModuleId = moduleId;
            Module = module;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ModuleId);
            pw.WriteUInt(Module?.Level ?? 0);

            if (Module == null)
            {
                pw.WriteList(0);
                return;
            }

            pw.WriteList(Module.Effects.Count);

            foreach (var effect in Module.Effects)
            {
                pw.WriteTuple(9);
                pw.WriteUInt(effect.EffectId);
                pw.WriteUInt(effect.SetLevel);
                pw.WriteDouble(effect.FlatValue);
                pw.WriteDouble(effect.LinearValue);
                pw.WriteDouble(effect.ExpValue);
                WriteArgument(pw, effect.Arg1);
                WriteArgument(pw, effect.Arg2);
                WriteArgument(pw, effect.Arg3);
                WriteArgument(pw, effect.Arg4);
            }
        }

        private static void WriteArgument(PythonWriter pw, int? argument)
        {
            if (argument.HasValue)
                pw.WriteInt(argument.Value);
            else
                pw.WriteNoneStruct();
        }
    }
}
