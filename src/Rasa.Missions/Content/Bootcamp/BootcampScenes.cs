using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Bootcamp
{
    internal static class BootcampSequence
    {
        internal static SceneDecision Apply(SceneContext context, SceneObservation observation) =>
            new DataSequenceScript().Handle(context, observation);
        internal static SceneDecision End(SceneDecision decision) =>
            new(decision.Checkpoint, decision.WorldIntents, decision.CharacterIntents,
                decision.Signals, decision.Timers, SceneStatus.Ended);
    }

    [MissionScript("bootcamp.experience", 1)]
    public sealed class BootcampExperience : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) => BootcampSequence.Apply(context, observation);
    }

    [MissionScript("bootcamp.initiation", 1)]
    public sealed class InitiationScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) => BootcampSequence.Apply(context, observation);
    }

    [MissionScript("bootcamp.gearing-up", 1)]
    public sealed class GearingUpScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) => BootcampSequence.Apply(context, observation);
    }

    [MissionScript("bootcamp.capture-the-flag", 1)]
    public sealed class CaptureTheFlagScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) => BootcampSequence.Apply(context, observation);
    }

    [MissionScript("bootcamp.reinforcements", 1)]
    public sealed class ReinforcementsScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var decision = BootcampExtractionScene.Handle(context, observation);
            return observation.Kind == SceneEventKind.Signal &&
                context.Bindings.Names.TryGetValue("reset", out var reset) && reset == observation.SequenceId
                    ? BootcampSequence.End(decision) : decision;
        }
    }

    [MissionScript("bootcamp.bomb-retry", 1)]
    public sealed class BombRetryScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var decision = BootcampExtractionScene.Handle(context, observation);
            return observation.Kind == SceneEventKind.Signal &&
                context.Bindings.Names.TryGetValue("reset", out var reset) && reset == observation.SequenceId
                    ? BootcampSequence.End(decision) : decision;
        }
    }
}
