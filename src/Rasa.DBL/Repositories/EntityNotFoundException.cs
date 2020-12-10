using System;

namespace Rasa.Repositories
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string entityName, object id)
            : base($"Entity of type {entityName} with id {id} does not exist.")
        {

        }
    }
}