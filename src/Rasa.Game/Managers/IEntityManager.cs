namespace Rasa.Managers
{
    public interface IEntityManager
    {
        ulong GetEntityId();
        void ReturnEntityId(ulong entityId);
    }
}