namespace Rasa.Structures
{
    using Memory;

    internal static class MissionWire
    {
        internal static void WriteBool(PythonWriter writer, bool value)
        {
            writer.WriteBool(value);
        }
    }
}
