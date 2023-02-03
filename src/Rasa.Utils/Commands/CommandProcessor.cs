using System.Threading;

namespace Rasa.Commands;

public static class CommandProcessor
{
    private static readonly Dictionary<string, Action<string[]>> Commands = new();

    public static void ProcessCommand(CancellationToken stopToken)
    {
        var command = ReadCommand(stopToken);
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Nice to have TODO: Handle parameters escaped by quotes, which could contain spaces!
        // Example: <command> test "this is a test" 10
        // Expected parameters:
        //   - "test"
        //   - "this is a test"
        //   - "10"
        var parts = command.Split(' ');
        if (parts.Length < 1)
            return;

        if (Commands.TryGetValue(parts[0], out var value))
        {
            value(parts);
            return;
        }

        Logger.WriteLog(LogType.Command, $"Invalid command: {command}");
    }

    private static string ReadCommand(CancellationToken stopToken)
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
                        command = command[..^1];
                        break;

                    default:
                        command += key.KeyChar;
                        break;
                }
            }
        }
        return string.Empty;
    }

    public static void RegisterCommand(string name, Action<string[]> handler) => Commands.Add(name, handler);
    public static void RemoveCommand(string name) => Commands.Remove(name);
}
