namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ArmWeaponFailed (404), on the player's own manifestation: RequestArmWeapon refused.
    /// client/augmentations/manifestation.py Recv_ArmWeaponFailed(requestedSlotId) puts the
    /// requested weapon drawer slot back to the armed one and redraws the drawer
    /// (UI_UPDATE_ARMED_WEAPON, not requested).
    /// </summary>
    public class ArmWeaponFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ArmWeaponFailed;

        public uint RequestedSlot { get; set; }

        public ArmWeaponFailedPacket(uint requestedSlot)
        {
            RequestedSlot = requestedSlot;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(RequestedSlot);
        }
    }
}
