using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rasa.Api;
using Rasa.Loot;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    public sealed class WeightedLootStore : IWeightedLootStore
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private static readonly object Writing = new();

        public WeightedLootStore(IGameUnitOfWorkFactory factory) =>
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public OrdinaryLootContentSnapshot Read()
        {
            lock (Writing)
            {
                using var world = _factory.CreateWorld();
                using var transaction = world.BeginTransaction();
                return world.OrdinaryLoot.Get();
            }
        }

        public StoreAnswer Replace(OrdinaryLootContentSnapshot content)
        {
            var answer = new StoreAnswer();
            if (content == null)
            {
                answer.Problems.Add("Weighted content is required; nothing was changed.");
                return answer;
            }
            var edit = content.Copy();
            lock (Writing)
            {
                using var world = _factory.CreateWorld();
                try
                {
                    using var transaction = world.BeginTransaction();
                    var previous = world.OrdinaryLoot.Get();
                    foreach (var policy in edit.MapPolicies.Where(row => row != null && !row.RequireProfiles))
                        if (previous.MapPolicies.Any(row => row.MapContextId == policy.MapContextId && row.RequireProfiles))
                            throw new LootContentException(new[]
                            {
                                $"map {policy.MapContextId}: an existing required-map policy cannot be disabled by the editor."
                            });
                    var keys = edit.Profiles.Where(row => row != null).Select(row => row.ProfileKey).ToHashSet(StringComparer.Ordinal);
                    if (edit.Groups.Any(row => row != null && !keys.Contains(row.ProfileKey)) ||
                        edit.Items.Any(row => row != null && !keys.Contains(row.ProfileKey)))
                        throw new LootContentException(new[] { "Every edited group and item must name a profile included in profiles." });
                    var candidate = new OrdinaryLootContentSnapshot(
                        previous.Profiles.Where(row => !keys.Contains(row.ProfileKey)).Concat(edit.Profiles),
                        previous.Groups.Where(row => !keys.Contains(row.ProfileKey)).Concat(edit.Groups),
                        previous.Items.Where(row => !keys.Contains(row.ProfileKey)).Concat(edit.Items),
                        Overlay(previous.Bindings, edit.Bindings, row => (row.MapContextId, row.CreatureId)),
                        Overlay(previous.MapPolicies, edit.MapPolicies, row => row.MapContextId));
                    var validated = OrdinaryLootCatalog.Load(world, candidate);
                    world.OrdinaryLoot.Replace(candidate);
                    world.Complete();
                    transaction.Commit();
                    OrdinaryLootCatalog.Publish(validated);
                }
                catch (LootContentException error)
                {
                    answer.Problems.AddRange(error.Diagnostics.Take(StoreAnswer.MostKept));
                    Logger.WriteLog(LogType.Error, $"Weighted loot edit rejected; no content published: {error.Message}");
                    world.Reject();
                }
                catch (LegacyFlatLootArchiveException error)
                {
                    answer.Problems.Add(error.Message);
                    Logger.WriteLog(LogType.Error, $"Weighted loot edit blocked by inactive archives: {error.Message}");
                    world.Reject();
                }
                catch (Exception error) when (error is DbException or DbUpdateException)
                {
                    answer.FailureStatus = 500;
                    answer.Problems.Add("The World database rejected the weighted loot save. Nothing was published; reload and retry after checking the server log.");
                    Logger.WriteLog(LogType.Error, $"Weighted loot database save failed; transaction rolled back, no publication: {error}");
                    world.Reject();
                }
            }
            return answer;
        }

        private static IEnumerable<T> Overlay<T, TKey>(IEnumerable<T> previous, IReadOnlyList<T> edit, Func<T, TKey> key)
            where T : class
        {
            var keys = edit.Where(row => row != null).Select(key).ToHashSet();
            return previous.Where(row => !keys.Contains(key(row))).Concat(edit);
        }
    }
}
