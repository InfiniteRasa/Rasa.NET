using System;

namespace Rasa.Repositories
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string entityName, string identifierName, object identifier)
            : base($"Entity of type {entityName} identified by identifier {identifierName} with value {identifier} does not exist.")
        {
        }
    }
}