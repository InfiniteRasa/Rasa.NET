namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// PvPEnabled (783): ClanControlPoint.Recv_PvPEnabled(isPvPEnabled), on a usable object of the
    /// CLANCONTROLPOINT kind - "Is this clan-owned control point used in PvE or PvP play?"
    /// (client/augmentations/clancontrolpoint.py). Sent with the object's entity, after UsableInfo
    /// (DynamicObjectManager.CreateDynamicObjectOnClient).
    ///
    /// On the record rather than of use: the 1.16.5.0 client stores the value (__pvpEnabled, True
    /// until told) and nothing in it reads it back - no effect, menu entry or targeting rule asks.
    /// Clan-owned control points were not finished: the one class with the augmentation is
    /// TEST_ClanControlPoint_PvE (29329), the challenge board window that would have listed and
    /// bid for them cannot open, and a clan-owned point's map marker text is a placeholder. The
    /// client's control point table (controlpointdata, with controlpointtype's PVE_OWNERSHIP and
    /// PVP_OWNERSHIP) has no PvE point either.
    ///
    /// What the server says is by the class: the PvE test point is told false, and any other
    /// class of the kind would be told what the client assumes anyway
    /// (DynamicObjectManager.IsPvPClanControlPoint). The class is what a control point of the
    /// open world is while a clan holds it (ControlPoints).
    /// </summary>
    public class PvPEnabledPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PvPEnabled;

        public bool IsPvPEnabled { get; }

        public PvPEnabledPacket(bool isPvPEnabled)
        {
            IsPvPEnabled = isPvPEnabled;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsPvPEnabled);
        }
    }
}
