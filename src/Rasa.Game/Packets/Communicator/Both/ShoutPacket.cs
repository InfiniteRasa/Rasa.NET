﻿namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class ShoutPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Shout;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
