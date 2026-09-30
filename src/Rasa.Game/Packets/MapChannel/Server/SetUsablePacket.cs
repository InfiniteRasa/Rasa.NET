namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SetUsable (203), on a usable object: puts it in or out of service. Sent by
    /// DynamicObjectManager.SetEnabled when DynamicObject.IsEnabled changes; a client meeting the
    /// object later has the same flag as UsableInfo's first argument.
    ///
    /// client/augmentations/usable.py Recv_SetUsable(isEnabled) -> _SetEnabled, as UsableInfo
    /// does; the value the object already has changes nothing. A change:
    ///  - IsUsable() answers it. Out of service there is no Use - the base _GetUseAction offers
    ///    nothing, so no right-click entry and nothing on the use key; lockbox, clan lockbox,
    ///    shrine and challenge board ask again in their own - and the base IsMouseTargetable is
    ///    false, so the mouse passes over it (an item dispenser asks too);
    ///  - a mission-activated object gets its MISSION_USABLE_INDICATOR back on, or loses it;
    ///  - OnEnabledChanged: a tesla coil stops or starts being targetable, an inert destroyable
    ///    and a destroyable stateless switch work their targetable setting out again. A wormhole
    ///    or a switch shows its own target category in place of OBJECT while out of service.
    /// It does not touch the object's state, mesh, FX or a use already under way.
    ///
    /// The C++ server only listed the id, with a to-do in controlpoint.cpp to send it so a side
    /// cannot take back a point it holds.
    /// </summary>
    public class SetUsablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetUsable;

        public bool IsEnabled { get; }

        public SetUsablePacket(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsEnabled);
        }
    }
}
