using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Commands
{
    public static class CommandProcessor
    {
        private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();

        public static async Task ProcessCommand(CancellationToken stopToken)
        {
            var command = await ReadCommand(stopToken);
            if (string.IsNullOrWhiteSpace(command))
                return;

            var parts = command.Split(' ');
            if (parts.Length < 1)
                return;

            if (Commands.TryGetValue(parts[0], out var handler))
            {
                // The host awaits this loop with no catch of its own, so an exception out of a
                // handler ("exit abc" reaching int.Parse, say) used to end the console loop for
                // the life of the process: the server kept running, but nothing typed at it was
                // read any more. A bad command is worth one error line, not the console.
                try
                {
                    handler(parts);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Command failed: {command}");
                    Logger.WriteLog(LogType.Error, e);
                }

                return;
            }

            Logger.WriteLog(LogType.Command, $"Invalid command: {command}");
        }

        private static async Task<string> ReadCommand(CancellationToken stopToken)
        {
            if (Console.IsInputRedirected)
            {
                // Console.KeyAvailable throws when input is redirected or no console is
                // attached (headless/backgrounded launches, Docker without a tty, etc.).
                // Without a real console there is nothing to read, so just idle.
                await Task.Delay(1000, stopToken);
                return null;
            }

            var command = string.Empty;
            while (!stopToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            return command;
                        case ConsoleKey.Backspace:
                            // Backspace on an empty line has nothing to remove; Substring(0, -1)
                            // threw here, and the throw took the console loop with it.
                            if (command.Length > 0)
                            {
                                command = command.Substring(0, command.Length - 1);
                                Console.Write("\b \b");
                            }
                            break;
                        default:
                            command += key.KeyChar;
                            break;
                    }
                }

                await Task.Delay(25, stopToken);
            }
            return null;
        }

        public static void RegisterCommand(string name, Action<string[]> handler)
        {
            Commands.Add(name, handler);
        }

        public static void RemoveCommand(string name)
        {
            if (Commands.ContainsKey(name))
                Commands.Remove(name);
        }
    }
}
