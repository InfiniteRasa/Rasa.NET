namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ArmAbilityFailed (403), on the player's own manifestation: RequestArmAbility refused.
    /// client/augmentations/manifestation.py Recv_ArmAbilityFailed(requestedSlotId) puts the
    /// requested ability drawer slot back to the armed one and redraws the drawer
    /// (UI_UPDATE_ARMED_ABILITY, not requested), so it stops showing a slot that was never armed.
    /// </summary>
    public class ArmAbilityFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ArmAbilityFailed;

        public int RequestedSlot { get; set; }

        public ArmAbilityFailedPacket(int requestedSlot)
        {
            RequestedSlot = requestedSlot;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(RequestedSlot);
        }
    }
}
