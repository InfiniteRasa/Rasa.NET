﻿namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class GetPvPClanStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.GetPvPClanStatus;

        public string PvpClanName { get; }
        public uint PvpTimeoutSeconds { get; }

        public GetPvPClanStatusPacket(string pvpClanName, uint pvpTimeoutSeconds)
        {
            PvpClanName = pvpClanName;
            PvpTimeoutSeconds = pvpTimeoutSeconds;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);

            if (string.IsNullOrEmpty(PvpClanName))
            {
                // Client expects == None when empty
                pw.WriteNoneStruct();
            }
            else
            {
                pw.WriteString(PvpClanName);
            }

            pw.WriteUInt(PvpTimeoutSeconds);
        }
    }
}
