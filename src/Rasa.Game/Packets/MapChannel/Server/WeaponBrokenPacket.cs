namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Tells the client a piece of its weapon has broken - worn to 0% condition.
    ///
    /// Addressed to the item's own entity id: <c>Recv_WeaponBroken(self)</c> is a method on the
    /// client's weapon augmentation. It takes no arguments, and posts PM_WEAPON_DAMAGED, "Your weapon was damaged and needs repair" and
    /// the BROKEN_EQUIPMENT tutorial ("You will no longer gain any benefit from the equipment until
    /// it is repaired"). Sent by Durability when the item reaches 0.
    /// </summary>
    public class WeaponBrokenPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponBroken;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
