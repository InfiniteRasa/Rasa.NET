namespace Rasa.Initialization;

public class Initializer : IInitializer
{
    private readonly IEnumerable<IInitializable> _initializables;

    public Initializer(IEnumerable<IInitializable> initializables)
    {
        _initializables = initializables;
    }

    public void Execute()
    {
        foreach (var initializable in _initializables)
            initializable.Initialize();
    }
}