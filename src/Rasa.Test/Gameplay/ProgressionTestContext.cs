using System;
using System.Linq;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.World;

    internal sealed class ProgressionTestContext : IDisposable
    {
        internal WorldTestContext World { get; } = new();

        internal Client CreateClient(byte level = 1, Race race = Race.Human)
        {
            var client = World.CreateClient();
            client.Player.Level = level;
            client.Player.Race = race;
            client.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                id => id, id => new ActorAttributes(id, 0, 0, 0, 0, 0));
            return client;
        }

        public void Dispose()
        {
            foreach (var loot in World.Map.LootDispensers.Values)
                EntityManager.Instance.FreeEntity(loot.EntityId);
            World.Map.LootDispensers.Clear();
            World.Dispose();
        }
    }
}
