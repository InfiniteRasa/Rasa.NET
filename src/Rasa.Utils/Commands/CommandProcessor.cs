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

            if (Commands.ContainsKey(parts[0]))
            {
                Commands[parts[0]](parts);
                return;
            }

            Logger.WriteLog(LogType.Command, $"Invalid command: {command}");
        }

        private static async Task<string> ReadCommand(CancellationToken stopToken)
        {
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
                            command = command.Substring(0, command.Length - 1);
                            Console.Write("\b \b");
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
