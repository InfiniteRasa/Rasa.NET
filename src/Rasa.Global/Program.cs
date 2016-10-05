using System;
using System.Diagnostics;

namespace Rasa.Global
{
    using Commands;
    using Misc;
    using Networking;

    public class Program : ProgramBase
    {
        private const string Type = "Global";

        private static GlobalServer _server;

        public static void Main(string[] args)
        {
            _server = new GlobalServer();

            Logger.WriteLog(LogType.File, "Application startup!");

            InitConsole(Type);

            Logger.WriteLog(LogType.Initialize, "*** Initialized Global Server...");

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
