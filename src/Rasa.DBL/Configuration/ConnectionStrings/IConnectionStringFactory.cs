namespace Rasa.Configuration.ConnectionStrings
{
    public interface IConnectionStringFactory
    {
        string Create(DatabaseConnectionConfiguration configuration);
    }
}