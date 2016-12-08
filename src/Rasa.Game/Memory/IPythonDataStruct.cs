namespace Rasa.Memory
{
    public interface IPythonDataStruct
    {
        void Read(PythonReader pr);
        void Write(PythonWriter pw);
    }
}
