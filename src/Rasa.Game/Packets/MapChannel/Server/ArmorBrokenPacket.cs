namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Tells the client a piece of its armor has broken - worn to 0% condition.
    ///
    /// Addressed to the item's own entity id: <c>Recv_ArmorBroken(self)</c> is a method on the
    /// client's armor augmentation. It takes no arguments, and posts PM_ARMOR_DAMAGED and
    /// the BROKEN_EQUIPMENT tutorial ("You will no longer gain any benefit from the equipment until
    /// it is repaired"). Sent by Durability when the item reaches 0.
    /// </summary>
    public class ArmorBrokenPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ArmorBroken;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
