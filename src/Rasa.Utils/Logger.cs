using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

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
        ExportData,
        Communicator
    }

    /// <summary>
    /// Writes log lines to the console and, if configured, a file - from a thread of its own.
    ///
    /// Every WriteLog used to write and flush the file, and write the console, on the calling
    /// thread: the world's main loop for most of them, the socket threads for the rest, with
    /// nothing keeping two threads apart on the one writer. A client that could make the server
    /// log - and many things a client sends are logged - could make the main loop wait on the
    /// disk and the console, and lines from two threads could interleave or corrupt the writer.
    /// Now a line is formatted where it is logged and queued; one thread writes them in order.
    /// The queue is bounded: past it, lines are dropped and counted rather than held.
    ///
    /// Each line is also made to be one line. Text that came from a client - a name, a version
    /// string, a petition - could carry newlines and forge whole entries. Control characters
    /// other than tab are replaced, and a newline inside a message starts an indented
    /// continuation, so a stack trace still reads as one but nothing can start a line of its own.
    /// </summary>
    public class Logger
    {
        public static LoggerConfig Config { get; private set; }

        private static StreamWriter _logWriter;

        /// <summary>Guards _logWriter between the writer thread and UpdateConfig.</summary>
        private static readonly object WriterLock = new object();

        private const int MaxQueuedLines = 65536;

        private static readonly BlockingCollection<(string Text, ConsoleColor Color, bool ToConsole)> Queue =
            new BlockingCollection<(string, ConsoleColor, bool)>(new ConcurrentQueue<(string, ConsoleColor, bool)>(), MaxQueuedLines);

        private static long _dropped;

        /// <summary>Lines taken but not yet written: queued, or being written now.</summary>
        private static long _pending;

        private static readonly Thread Writer;

        static Logger()
        {
            Writer = new Thread(Pump) { IsBackground = true, Name = "Logger" };
            Writer.Start();

            // The last lines before an exit - a crash's included - are the ones most worth having.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Drain();
            AppDomain.CurrentDomain.UnhandledException += (_, _) => Drain();
        }

        /// <summary>Stops taking lines and waits a moment for the writer to finish the queue.</summary>
        private static void Drain()
        {
            try
            {
                Queue.CompleteAdding();
                Writer.Join(TimeSpan.FromSeconds(2));
            }
            catch (Exception)
            {
                // Exiting either way.
            }
        }

        private static void Pump()
        {
            foreach (var (text, color, toConsole) in Queue.GetConsumingEnumerable())
            {
                try
                {
                    var dropped = Interlocked.Exchange(ref _dropped, 0);

                    if (dropped > 0)
                        Write($"[{DateTime.Now:yyyy. MM. dd. HH:mm:ss.fff}] [Error] {dropped} log line(s) dropped: the log could not keep up.", ConsoleColor.Red, true);

                    Write(text, color, toConsole);
                }
                catch (Exception)
                {
                    // A log that cannot be written must not take the writer thread with it.
                }
                finally
                {
                    Interlocked.Decrement(ref _pending);
                }
            }
        }

        /// <summary>
        /// Waits until every line logged so far has been written, or the timeout passes. For
        /// whoever reads the console or the log file right after logging - a test capturing
        /// Console.Out, a shutdown. Returns false on a timeout.
        /// </summary>
        public static bool Flush(int timeoutMilliseconds = 5000)
        {
            if (Thread.CurrentThread == Writer)
                return Interlocked.Read(ref _pending) == 0;

            var deadline = Environment.TickCount64 + timeoutMilliseconds;
            var spin = new SpinWait();

            while (Interlocked.Read(ref _pending) > 0)
            {
                if (Environment.TickCount64 >= deadline)
                    return false;

                spin.SpinOnce();
            }

            return true;
        }

        private static void Write(string text, ConsoleColor color, bool toConsole)
        {
            lock (WriterLock)
                _logWriter?.WriteLine(text);

            if (!toConsole)
                return;

            var previous = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = previous;
        }

        public static void UpdateConfig(LoggerConfig config)
        {
            Config = config;

            lock (WriterLock)
            {
                if (Config.LogToFile && _logWriter == null && !string.IsNullOrWhiteSpace(Config.LogFilePath))
                {
                    _logWriter = new StreamWriter(new FileStream(Config.LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };

                    // Add a new line, if the file had content already
                    if (_logWriter.BaseStream.Position != 0)
                        _logWriter.WriteLine();
                }
                else if (!Config.LogToFile)
                {
                    if (_logWriter != null)
                    {
                        _logWriter.WriteLine($"[{DateTime.Now:yyyy. MM. dd. HH:mm:ss.fff}] [FileLog] Logging system shutdown!");
                        _logWriter.Flush();
                        _logWriter.Dispose();
                    }

                    _logWriter = null;
                    return;
                }
            }

            WriteLog(LogType.File, "Logging system startup!");
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

                case LogType.Security:
                    prefix = "Security";
                    desiredColor = ConsoleColor.DarkRed;
                    break;

                case LogType.Communicator:
                    prefix = "Communicator";
                    desiredColor = ConsoleColor.DarkGreen;
                    break;

                case LogType.ExportData:
                    prefix = "";
                    desiredColor = ConsoleColor.White;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unhandled log type: {type}");
            }

            var text = type == LogType.ExportData
                ? $"{log}"
                : $"[{DateTime.Now:yyyy. MM. dd. HH:mm:ss.fff}] [{prefix}] {Sanitize(log)}";

            var toConsole = type != LogType.File && ((Config?.IsDebugMode ?? true) || type != LogType.Debug);

            Interlocked.Increment(ref _pending);

            if (!Queue.TryAdd((text, desiredColor, toConsole)))
            {
                Interlocked.Decrement(ref _pending);
                Interlocked.Increment(ref _dropped);
            }
        }

        /// <summary>
        /// One entry, however the message was made: newlines become an indented continuation,
        /// and any other control character (tab aside) a '?'.
        /// </summary>
        private static string Sanitize(string log)
        {
            if (string.IsNullOrEmpty(log))
                return log;

            var clean = true;

            foreach (var c in log)
                if (char.IsControl(c) && c != '\t')
                {
                    clean = false;
                    break;
                }

            if (clean)
                return log;

            var builder = new StringBuilder(log.Length + 16);

            for (var i = 0; i < log.Length; i++)
            {
                var c = log[i];

                if (c == '\r' && i + 1 < log.Length && log[i + 1] == '\n')
                    continue;

                if (c == '\n' || c == '\r')
                    builder.Append(Environment.NewLine).Append("    ");
                else if (char.IsControl(c) && c != '\t')
                    builder.Append('?');
                else
                    builder.Append(c);
            }

            return builder.ToString();
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
