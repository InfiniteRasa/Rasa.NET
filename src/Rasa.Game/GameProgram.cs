using System;
using System.Diagnostics;

namespace Rasa
{
    using Commands;
    using Game;
    using Misc;

    public class GameProgram : ProgramBase
    {
        private const string Type = "Game";

        private static Server _server;

        public static void Main(string[] args)
        {
            _server = new Server();

            Logger.WriteLog(LogType.File, "Application startup!");

            InitConsole(Type);

            Logger.WriteLog(LogType.Initialize, "*** Initialized Game Server...");

            if (!_server.Start())
            {
                Logger.WriteLog(LogType.Error, "Unable to start server!");
                return;
            }

            while (_server.Running)
            {
                CommandProcessor.ProcessCommand();
            }

            GC.Collect();

            Process.GetCurrentProcess().WaitForExit();
        }
    }
}
