using System.Collections.Generic;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static partial class BootcampMissionDataV1
    {
        public static MissionSceneDefinition Mission1990() => new MissionSceneDefinition
            {
                Script = "bootcamp.initiation",
                StateVersion = 1,
                Actors = new()
                {
                },
                Sequences = new()
                {
                },
            };
    }
}
