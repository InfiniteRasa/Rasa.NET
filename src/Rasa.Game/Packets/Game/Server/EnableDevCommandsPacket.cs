namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Switches on the client's developer commands: <c>()</c>.
    ///
    /// client/clientmethod.py's Recv_EnableDevCommands imports client_nca_internal.developerkeys
    /// and calls its EnableDevCommands(). The same package's developercommands.HandleDevSlashCommand
    /// is what ProcessSlashCommand asks first about every slash command, and its developerkeys
    /// InsertDeveloperKeyMappings is called whenever the key bindings are rebuilt. None of it
    /// ships: a retail client logs "Tried to enable dev commands, but could not find
    /// developerkeys module!" with the ImportError's traceback, and nothing else happens.
    /// tabula_rasa.exe puts 'python_nca_internal' (relative to its working directory) first on
    /// sys.path, which is where the package was found in the builds that had it.
    /// </summary>
    public class EnableDevCommandsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EnableDevCommands;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
