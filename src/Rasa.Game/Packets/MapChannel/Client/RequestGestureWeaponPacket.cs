namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/actions/gestureweapon.py GestureWeapon.SendServerRequest:
    /// SendCallActorMethod('RequestGestureWeapon', (self.actionArgId, self.targetId)) - the weapon
    /// hand signals /eyes (63), /quiet (65) and /crouch (66) of action GESTURE_WEAPON (474).
    /// The same arguments as RequestGesture, read the same way: targetId is the entity under the
    /// mouse, None when there is none, a long when the server sent it, an int if typed.
    ///
    /// Until this existed the opcode had no packet, and an unhandled opcode fails the packet
    /// terminator check and closes the connection: a player holding one of the three emote flags
    /// was disconnected by the signal.
    /// </summary>
    public class RequestGestureWeaponPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestGestureWeapon;

        public uint GestureId { get; set; }

        /// <summary>0 when the signal has no target.</summary>
        public ulong TargetEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GestureId = pr.ReadUInt();

            switch (pr.PeekType())
            {
                case PythonType.Structs:
                    pr.ReadUnkStruct();
                    TargetEntityId = 0;
                    break;

                case PythonType.Long:
                    TargetEntityId = pr.ReadULong();
                    break;

                default:
                    var value = pr.ReadInt();
                    TargetEntityId = value > 0 ? (ulong)value : 0;
                    break;
            }
        }
    }
}
