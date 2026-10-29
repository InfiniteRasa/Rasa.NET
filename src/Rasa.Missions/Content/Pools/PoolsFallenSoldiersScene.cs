using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Pools
{
    /// <summary>
    /// Death Patrol (1315) and More Clips and Tags (1321), Valverde Pools: the player's own fallen AFS soldiers. Sequence 0
    /// lays them out. Searching body <c>n</c> signals sequence <c>n</c>: that body closes and gives one clip (its
    /// sequence). While the player has fewer than <see cref="Tags"/> tags, it also gives a tag (sequence
    /// <see cref="TagFrom"/> + n) by chance: the tags still needed over the clips still needed. So the tags lie on random
    /// bodies, each run its own, and the last of them comes no later than the <see cref="Clips"/>th clip, which ends the
    /// search (the user's choice of 2026-10-25). The draw is a hash of the run and the search, the same on a retry.
    /// </summary>
    [MissionScript("pools.fallen-soldiers", 1)]
    public sealed class PoolsFallenSoldiersScene : ISceneScript
    {
        public const uint Tags = 8, Clips = 16;

        /// <summary>The tag of body n is sequence <see cref="TagFrom"/> + n; bodies are 1..<see cref="TagFrom"/> - 1.</summary>
        public const uint TagFrom = 100;

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", Array.Empty<uint>(), 0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Fallen soldiers checkpoint is missing.");
            if (state.Phase is not ("new" or "searching" or "ended") || state.Searched == null || state.TagsFound > Tags)
                throw new InvalidOperationException($"Fallen soldiers checkpoint has an invalid phase {state.Phase}.");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
            {
                var start = context.Bindings.Sequences[0];
                return new SceneDecision(Serialize(state with { Phase = "searching" }), start.WorldIntents,
                    start.CharacterIntents, start.Signals);
            }
            if (observation.Kind is SceneEventKind.Cancelled or SceneEventKind.MissionRewarded)
                return new SceneDecision(Serialize(state with { Phase = "ended" }), status: SceneStatus.Ended);

            var body = observation.SequenceId;
            if (observation.Kind != SceneEventKind.Signal || state.Phase != "searching" || body is 0 or >= TagFrom ||
                state.Searched.Contains(body) || state.Searched.Length >= Clips ||
                !context.Bindings.Sequences.TryGetValue(body, out var search) ||
                !context.Bindings.Sequences.TryGetValue(TagFrom + body, out var tag))
                return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);

            var drawn = HasTag(context.Run.Id, body, (uint)state.Searched.Length, state.TagsFound);
            var next = state with
            {
                Searched = state.Searched.Append(body).OrderBy(id => id).ToArray(),
                TagsFound = state.TagsFound + (drawn ? 1U : 0U)
            };
            return new SceneDecision(Serialize(next), search.WorldIntents,
                drawn ? search.CharacterIntents.Concat(tag.CharacterIntents) : search.CharacterIntents,
                drawn ? search.Signals.Concat(tag.Signals) : search.Signals);
        }

        /// <summary>
        /// Whether the body searched after <paramref name="searched"/> others carries a tag: by chance the tags still
        /// needed over the clips still needed (this one's included), so certain when they are as many.
        /// </summary>
        public static bool HasTag(string runId, uint body, uint searched, uint tagsFound)
        {
            if (tagsFound >= Tags || searched >= Clips)
                return false;
            var needed = Tags - tagsFound;
            var left = Clips - searched;
            return Roll(runId, body, searched) * left < needed;
        }

        /// <summary>A number in [0, 1) from the run, the body and the search count (SHA-256: the same in every process).</summary>
        internal static double Roll(string runId, uint body, uint searched)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{runId}:{body}:{searched}"));
            return (BitConverter.ToUInt64(hash, 0) >> 11) * (1.0 / (1UL << 53));
        }

        private static string Serialize(Checkpoint state) => JsonSerializer.Serialize(state);

        public sealed record Checkpoint(string Phase, uint[] Searched, uint TagsFound);
    }
}
