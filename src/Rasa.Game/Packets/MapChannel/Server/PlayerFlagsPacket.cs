using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PlayerFlagsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerFlags;

        public IReadOnlyList<uint> PlayerFlagIds { get; }

        public PlayerFlagsPacket(IReadOnlyCollection<uint> playerFlagIds)
        {
            if (playerFlagIds == null)
                throw new ArgumentNullException(nameof(playerFlagIds));
            PlayerFlagIds = Array.AsReadOnly(playerFlagIds.ToArray());
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(PlayerFlagIds.Count);
            foreach (var flagId in PlayerFlagIds)
                pw.WriteUInt(flagId);
        }
    }
}
