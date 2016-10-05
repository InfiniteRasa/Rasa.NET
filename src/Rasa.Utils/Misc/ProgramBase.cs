using System;

namespace Rasa.Misc
{
    public class ProgramBase
    {
        public static void InitConsole(string type)
        {
            Console.Title = $"Rasa.NET - {type} Server";

            Logger.WriteLog(LogType.Initialize, @"  _____                   _   _ ______ _______ ");
            Logger.WriteLog(LogType.Initialize, @" |  __ \                 | \ | |  ____|__   __|");
            Logger.WriteLog(LogType.Initialize, @" | |__) |__ _ ___  __ _  |  \| | |__     | |   ");
            Logger.WriteLog(LogType.Initialize, @" |  _  // _` / __|/ _` | | . ` |  __|    | |   ");
            Logger.WriteLog(LogType.Initialize, @" | | \ \ (_| \__ \ (_| |_| |\  | |____   | |   ");
            Logger.WriteLog(LogType.Initialize, @" |_|  \_\__,_|___/\__,_(_)_| \_|______|  |_|   ");
            Logger.WriteLog(LogType.Initialize, $@" Tabula Rasa server - Rasa.NET - {type}");
            Logger.WriteLog(LogType.Initialize, "");
        }
    }
}
