namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'UpdateLevel', (moduleId, newLevel)) on a player holding
    /// SET_LEVEL_EFFECT (gameeffectdata 10000068): how many pieces of an armor set they now wear.
    /// Wired so that it can be sent, and deliberately sent by nothing: items here carry no
    /// modules, and the set bonuses themselves were never in the client.
    ///
    /// UpdateLevel is a method of the effect, not of the entity: the UpdateLevel (771) opcode names
    /// it in the client's method table, but no entity class has a Recv_UpdateLevel to answer one
    /// sent to it directly.
    ///
    /// client/lootmodule.py SetLevelEffect - no icon, tooltip or FX:
    ///  - OnAttach(target, moduleId): the set's module id, the one attach argument after the
    ///    tooltip dictionary. One effect per set worn;
    ///  - Recv_UpdateLevel(target, moduleId, newLevel): effect.level = newLevel. moduleId is not
    ///    read. It moves the count without a detach and attach.
    ///
    /// The one reader is the item tooltip (tooltipwindow._AddModuleEffects), on the viewer's own
    /// avatar only - so only the wearer's client needs the effect. For each module on the item it
    /// finds the SET_LEVEL_EFFECT whose moduleId matches (its level, or 0), shows the set's name
    /// (modulesetnamelanguage, "Set: Scout Suit Mk I") and lists the module's effects from
    /// ModuleTooltipInfo, each prefixed "(n)" with the pieces it needs and drawn active when the
    /// level is at least n, greyed (TT_Module_Set_Inactive) otherwise.
    ///
    /// The client has the 225 set names and their item-name prefixes, and nothing else: which
    /// items carry which set module, each bonus's effect, piece threshold and values came from the
    /// retail server in ModuleTooltipInfo (RequestTooltipForModuleId), which this server does not
    /// answer. Five armor slots make five the highest count. The C++ server only listed the id.
    /// </summary>
    public class GameEffectUpdateLevelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }

        /// <summary>The set's module id, as the effect was attached with; the client ignores it here.</summary>
        public int ModuleId { get; }

        /// <summary>Pieces of the set worn.</summary>
        public int NewLevel { get; }

        public GameEffectUpdateLevelPacket(int effectId, int moduleId, int newLevel)
        {
            EffectId = effectId;
            ModuleId = moduleId;
            NewLevel = newLevel;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("UpdateLevel");
            pw.WriteTuple(2);                   // args = (moduleId, newLevel)
            pw.WriteInt(ModuleId);
            pw.WriteInt(NewLevel);
        }
    }
}
