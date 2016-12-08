using System;
using System.Diagnostics;

namespace Rasa
{
    using Auth;
    using Commands;
    using Misc;

    public class AuthProgram : ProgramBase
    {
        private const string Type = "Authentication";

        private static Server _server;

        public static void Main(string[] args)
        {
            _server = new Server();

            Logger.WriteLog(LogType.File, "Application startup!");

            InitConsole(Type);

            Logger.WriteLog(LogType.Initialize, "*** Initialized Authentication Server...");

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
