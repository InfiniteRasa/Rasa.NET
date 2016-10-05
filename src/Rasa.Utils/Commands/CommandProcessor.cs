using System;
using System.Collections.Generic;

namespace Rasa.Commands
{
    public static class CommandProcessor
    {
        private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();

        public static void ProcessCommand()
        {
            var command = Console.ReadLine();
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
