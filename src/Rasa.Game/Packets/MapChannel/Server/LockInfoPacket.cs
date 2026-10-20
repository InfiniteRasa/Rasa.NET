using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// What keeps a usable shut - <c>Recv_LockInfo(unlocked, lockStateId, missionId, logosIds,
    /// keyItemTemplateId, cipherLevel, playerFlagReqs, playerFlagExs)</c>.
    ///
    /// Eight arguments, not the seven the opcode tables list: the client's own signature carries
    /// playerFlagExs after the requirements, and CanUnlock() reads it first of all.
    ///
    /// Until this is sent every usable on the client sits at cipher level 0, which
    /// <c>IsCipherable()</c> reads as "not cipherable" - so without it the cipher tool has
    /// nothing in the world it can legitimately be pointed at, and the overhead icon never shows
    /// a lock either.
    /// </summary>
    public class LockInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockInfo;

        public bool Unlocked { get; }
        public UseObjectState LockStateId { get; }
        public uint MissionId { get; }
        public List<uint> LogosIds { get; }
        public uint KeyItemTemplateId { get; }
        public int CipherLevel { get; }
        public List<uint> PlayerFlagReqs { get; }
        public List<uint> PlayerFlagExs { get; }

        /// <param name="openForViewer">
        /// Open for the player it is sent to although not for everyone: a lock whose mission they
        /// have completed (MissionLocks).
        /// </param>
        public LockInfoPacket(UsableLock usableLock, bool openForViewer = false)
        {
            Unlocked = usableLock.Unlocked || openForViewer;
            LockStateId = usableLock.LockStateId;
            MissionId = usableLock.MissionId;
            LogosIds = usableLock.LogosIds;
            KeyItemTemplateId = usableLock.KeyItemTemplateId;
            CipherLevel = usableLock.CipherLevel;
            PlayerFlagReqs = usableLock.PlayerFlagReqs;
            PlayerFlagExs = usableLock.PlayerFlagExs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(8);
            pw.WriteBool(Unlocked);
            pw.WriteUInt((uint)LockStateId);
            pw.WriteUInt(MissionId);
            WriteIds(pw, LogosIds);
            pw.WriteUInt(KeyItemTemplateId);
            pw.WriteInt(CipherLevel);
            WriteIds(pw, PlayerFlagReqs);
            WriteIds(pw, PlayerFlagExs);
        }

        /// <summary>
        /// A list rather than None even when empty: CanUnlock() iterates all three of these
        /// without a null check, and the logos branch tests the length before the contents.
        /// </summary>
        private static void WriteIds(PythonWriter pw, List<uint> ids)
        {
            pw.WriteList(ids.Count);

            foreach (var id in ids)
                pw.WriteUInt(id);
        }
    }
}
