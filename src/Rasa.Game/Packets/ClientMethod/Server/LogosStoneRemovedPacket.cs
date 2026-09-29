namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// LogosStoneRemoved (476), on the player's own manifestation: a Logos taken out of the Tabula.
    ///
    /// client/augmentations/manifestation.py Recv_LogosStoneRemoved(stoneId) removes the id from
    /// the Tabula list and posts UI_UPDATE_LOGOSTABULA, which redraws the Tabula window. It says
    /// nothing - unlike LogosStoneAdded there is no player message or tutorial - and ignores an id
    /// the Tabula does not hold, a None, and the call on any manifestation but the player's own.
    /// The Tabula is what HasLogosStone and HasLogosForAbility read, so an ability that needs the
    /// Logos stops being usable client-side at once.
    ///
    /// list.remove takes one occurrence, so a Logos the Tabula holds twice needs it twice.
    /// Sent by CharacterManager.RemoveLogos.
    /// </summary>
    public class LogosStoneRemovedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogosStoneRemoved;

        public uint LogosId { get; set; }

        public LogosStoneRemovedPacket(uint logosId)
        {
            LogosId = logosId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(LogosId);
        }
    }
}
