using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Positioning;
    using System;

    public class Actor : IHasPosition
    {
        public uint EntityId { get; set; }

        public Vector3 Position => GetPositionVector();

        public Actor()
        {
            EntityId = GetEntityId();
        }

        static object locker = new object();
        private static uint GetEntityId()
        {
            uint newEntityId = 0;
            lock(locker)
            {
                newEntityId = 10001;
            }

            return newEntityId;
        }

        protected virtual Vector3 GetPositionVector()
        {
            return new Vector3();
        }
    }
}
