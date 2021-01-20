using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Positioning;
    using Rasa.Managers;
    using System;

    public class Actor : IHasPosition
    {
        public ulong EntityId { get; set; }

        public Vector3 Position => GetPositionVector();

        public Actor(IEntityManager entityManager)
        {
            EntityId = entityManager.GetEntityId();
        }

        protected virtual Vector3 GetPositionVector()
        {
            return new Vector3();
        }
    }
}
