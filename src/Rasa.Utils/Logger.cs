using System;
using System.IO;

namespace Rasa
{
    public enum LogType
    {
        Debug,
        AI,
        Network,
        Error,
        Test,
        Initialize,
        Command,
        File,
        Security,
        None,
        ExportData
    }

    public class Logger
    {
        public static LoggerConfig Config { get; private set; }

        private static StreamWriter _logWriter;

        public static void UpdateConfig(LoggerConfig config)
        {
            Config = config;

            if (Config.LogToFile && _logWriter == null && !string.IsNullOrWhiteSpace(Config.LogFilePath))
            {
                _logWriter = new StreamWriter(new FileStream(Config.LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };

                // Add a new line, if the file had content already
                if (_logWriter.BaseStream.Position != 0)
                    _logWriter.WriteLine();

                WriteLog(LogType.File, "Logging system startup!");
            }
            else if (!Config.LogToFile)
            {
                if (_logWriter != null)
                {
                    WriteLog(LogType.File, "Logging system shutdown!");

                    _logWriter.Flush();
                    _logWriter.Dispose();
                }

                _logWriter = null;
            }
        }

        public static void WriteLog(LogType type, object log)
        {
            WriteLog(type, log?.ToString() ?? "null");
        }

        public static void WriteLog(LogType type, string log)
        {
            string prefix;
            ConsoleColor desiredColor;

            switch (type)
            {
                case LogType.AI:
                    desiredColor = ConsoleColor.Yellow;
                    prefix = "AI";
                    break;

                case LogType.Debug:
                    desiredColor = ConsoleColor.Magenta;
                    prefix = "Debug";
                    break;

                case LogType.Network:
                    desiredColor = ConsoleColor.Green;
                    prefix = "Network";
                    break;

                case LogType.Error:
                    desiredColor = ConsoleColor.Red;
                    prefix = "Error";
                    break;

                case LogType.Test:
                    desiredColor = ConsoleColor.DarkGray;
                    prefix = "Test";
                    break;

                case LogType.Initialize:
                    desiredColor = ConsoleColor.Blue;
                    prefix = "Init";
                    break;

                case LogType.Command:
                    desiredColor = ConsoleColor.Cyan;
                    prefix = "Command";
                    break;

                case LogType.None:
                    desiredColor = ConsoleColor.White;
                    prefix = "";
                    break;

                case LogType.File: // Only logs to file, color doesn't matter
                    prefix = "FileLog";
                    desiredColor = ConsoleColor.Black;
                    break;

                case LogType.ExportData: // log data without any prefix
                    prefix = "";
                    desiredColor = ConsoleColor.DarkYellow;
                    break;

                case LogType.Security:
                    prefix = "Security";
                    desiredColor = ConsoleColor.DarkRed;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unhandled log type: {type}");
            }

            var text = "";

            if (type == LogType.ExportData)
                text = $"{log}";
            else
                text = $"[{DateTime.Now:yyyy. MM. dd. HH:mm:ss.fff}] [{prefix}] {log}";

            _logWriter?.WriteLine(text);

            if (type == LogType.File || (!Config.IsDebugMode && type == LogType.Debug))
                return;

            var color = Console.ForegroundColor;

            Console.ForegroundColor = desiredColor;
            Console.WriteLine(text);
            Console.ForegroundColor = color;
        }

        public static void WriteLog(LogType type, string format, params object[] args)
        {
            WriteLog(type, string.Format(format, args));
        }

        public class LoggerConfig
        {
            public bool IsDebugMode { get; set; }
            public string LogFilePath { get; set; }
            public bool LogToFile { get; set; }
        }
    }
}
