namespace Rasa.Packets.Manifestation.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// The player's level set without ceremony: client/augmentations/manifestation.py
    /// Recv_LevelChanged(newLevel) calls SetExperienceLevel and nothing else - no ACTOR_LEVEL_UP
    /// event and no level-up tutorial, which is what LevelUp adds. "Player leveled up (or down).
    /// Most of the time, will be followed by an AvailableAllocationPoints message ... Only valid
    /// for the current user's manifestation. Should never be received for other manifestations."
    /// Everyone else is told with Level.
    /// </summary>
    public class LevelChangedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LevelChanged;

        public byte Level { get; set; }

        public LevelChangedPacket(byte level)
        {
            Level = level;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Level);
        }
    }
}
