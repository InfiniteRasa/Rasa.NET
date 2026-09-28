using System;

namespace Rasa.Missions.Scenes
{
    public sealed record ActorHandle(string RunId, string Role, uint Generation, Guid MapEpoch);
}
