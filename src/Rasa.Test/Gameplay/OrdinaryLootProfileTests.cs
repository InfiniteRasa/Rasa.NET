using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Loot;
using Rasa.Structures.World;

namespace Rasa.Test.Gameplay
{
    [TestClass]
    public class OrdinaryLootProfileTests
    {
        [TestMethod]
        public void MinimumDrawSelectsTheFirstJunkCandidate()
        {
            var profile = new OrdinaryLootProfile("test", new[]
            {
                new OrdinaryLootGroup("junk", LootGroupKind.Junk, 10000, 1, 1, new[]
                {
                    new OrdinaryLootCandidate(41665, 1, 1, 1),
                    new OrdinaryLootCandidate(41666, 1, 1, 1)
                })
            });

            CollectionAssert.AreEqual(new[] { (41665U, 1U) },
                profile.Roll((minimum, maximum) => minimum).ToArray());
        }

        [TestMethod]
        [DataRow(0, 0, false)]
        [DataRow(0, 9999, false)]
        [DataRow(10000, 0, true)]
        [DataRow(10000, 9999, true)]
        [DataRow(7000, 0, true)]
        [DataRow(7000, 6999, true)]
        [DataRow(7000, 7000, false)]
        [DataRow(7000, 9999, false)]
        public void AdmissionUsesAnExclusiveBasisPointThreshold(int chance, int draw, bool admitted)
        {
            var profile = Profile(Group(chance: chance));
            var random = admitted
                ? new ScriptedRandom((0, 10000, draw), (1, 2, 1), (0, 1, 0), (1, 2, 1))
                : new ScriptedRandom((0, 10000, draw));

            CollectionAssert.AreEqual(admitted ? new[] { (1U, 1U) } : Array.Empty<(uint, uint)>(),
                profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        [DataRow(0, 90U)]
        [DataRow(1, 90U)]
        [DataRow(2, 10U)]
        [DataRow(4, 10U)]
        [DataRow(5, 70U)]
        [DataRow(9, 70U)]
        public void EachCumulativeWeightBoundaryUsesSuppliedCandidateOrder(int draw, uint templateId)
        {
            var profile = Profile(Group(candidates: new[]
            {
                new OrdinaryLootCandidate(90, 2, 1, 1),
                new OrdinaryLootCandidate(10, 3, 1, 1),
                new OrdinaryLootCandidate(70, 5, 1, 1)
            }));
            var random = new ScriptedRandom((0, 10000, 9999), (1, 2, 1), (0, 10, draw), (1, 2, 1));

            CollectionAssert.AreEqual(new[] { (templateId, 1U) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void RollCountIncludesBothEndpointsAndCanSelectZero(int count)
        {
            var profile = Profile(Group(minimumRolls: 0, maximumRolls: 2));
            var draws = new List<(int, int, int)> { (0, 10000, 0), (0, 3, count) };
            for (var index = 0; index < count; index++)
            {
                draws.Add((0, 1, 0));
                draws.Add((1, 2, 1));
            }
            var random = new ScriptedRandom(draws.ToArray());

            CollectionAssert.AreEqual(Enumerable.Repeat((1U, 1U), count).ToArray(),
                profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void FixedZeroCountProducesNoSelections()
        {
            var profile = Profile(Group(minimumRolls: 0, maximumRolls: 0));
            var random = new ScriptedRandom((0, 10000, 0), (0, 1, 0));

            CollectionAssert.AreEqual(Array.Empty<(uint, uint)>(), profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        [DataRow(4)]
        [DataRow(8)]
        public void QuantityIncludesBothEndpoints(int quantity)
        {
            var profile = Profile(Group(candidates: new[] { new OrdinaryLootCandidate(1, 1, 4, 8) }));
            var random = new ScriptedRandom((0, 10000, 0), (1, 2, 1), (0, 1, 0), (4, 9, quantity));

            CollectionAssert.AreEqual(new[] { (1U, (uint)quantity) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void GroupsAreIndependentAndRolledInSuppliedOrder()
        {
            var profile = Profile(
                Group(key: "zeta", kind: LootGroupKind.Equipment,
                    candidates: new[] { new OrdinaryLootCandidate(90, 1, 2, 2) }),
                Group(key: "middle", chance: 0),
                Group(key: "alpha", kind: LootGroupKind.Ammunition, chance: 7000,
                    candidates: new[] { new OrdinaryLootCandidate(10, 1, 3, 3) }));
            var random = new ScriptedRandom(
                (0, 10000, 9999), (1, 2, 1), (0, 1, 0), (2, 3, 2),
                (0, 10000, 0),
                (0, 10000, 6999), (1, 2, 1), (0, 1, 0), (3, 4, 3));

            CollectionAssert.AreEqual(new[] { (90U, 2U), (10U, 3U) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void JunkSelectsOneTypeWithQuantityAboveOne()
        {
            var profile = Profile(Group(kind: LootGroupKind.Junk, candidates: new[]
            {
                new OrdinaryLootCandidate(41665, 1, 1, 1),
                new OrdinaryLootCandidate(41666, 1, 3, 3)
            }));
            var random = new ScriptedRandom((0, 10000, 0), (1, 2, 1), (0, 2, 1), (3, 4, 3));

            CollectionAssert.AreEqual(new[] { (41666U, 3U) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void RepeatedSelectionsUseReplacementWithoutCoalescing()
        {
            var profile = Profile(Group(minimumRolls: 2, maximumRolls: 2, candidates: new[]
            {
                new OrdinaryLootCandidate(1, 1, 1, 1),
                new OrdinaryLootCandidate(2, 1, 2, 2)
            }));
            var random = new ScriptedRandom((0, 10000, 0), (2, 3, 2),
                (0, 2, 1), (2, 3, 2), (0, 2, 1), (2, 3, 2));

            CollectionAssert.AreEqual(new[] { (2U, 2U), (2U, 2U) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void ExplicitEmptyProfileConsumesNoRandomness()
        {
            var profile = new OrdinaryLootProfile("empty", Array.Empty<OrdinaryLootGroup>());
            var random = new ScriptedRandom();

            CollectionAssert.AreEqual(Array.Empty<(uint, uint)>(), profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void MaximumSafeQuantityUsesTheLargestIntegerExclusiveEndpoint()
        {
            var maximum = (uint)int.MaxValue - 1;
            var profile = Profile(Group(candidates: new[]
            {
                new OrdinaryLootCandidate(1, 1, maximum - 1, maximum)
            }));
            var random = new ScriptedRandom((0, 10000, 0), (1, 2, 1), (0, 1, 0),
                (int.MaxValue - 2, int.MaxValue, int.MaxValue - 1));

            CollectionAssert.AreEqual(new[] { (1U, maximum) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void MaximumSafeWeightTotalIncludesTheLastCandidate()
        {
            var profile = Profile(Group(candidates: new[]
            {
                new OrdinaryLootCandidate(1, int.MaxValue - 1, 1, 1),
                new OrdinaryLootCandidate(2, 1, 1, 1)
            }));
            var random = new ScriptedRandom((0, 10000, 0), (1, 2, 1),
                (0, int.MaxValue, int.MaxValue - 1), (1, 2, 1));

            CollectionAssert.AreEqual(new[] { (2U, 1U) }, profile.Roll(random.Next).ToArray());
            random.AssertConsumed();
        }

        [TestMethod]
        public void RollReturnsAnImmutableSnapshotThatDoesNotRedrawWhenEnumerated()
        {
            var profile = Profile(Group());
            var random = new ScriptedRandom((0, 10000, 0), (1, 2, 1), (0, 1, 0), (1, 2, 1));

            var drops = profile.Roll(random.Next);

            random.AssertConsumed();
            CollectionAssert.AreEqual(new[] { (1U, 1U) }, drops.ToArray());
            CollectionAssert.AreEqual(new[] { (1U, 1U) }, drops.ToArray());
            Assert.IsFalse(drops is Array, "The rolled snapshot must not expose its backing array.");
            if (drops is IList<(uint TemplateId, uint Quantity)> list)
            {
                Assert.IsTrue(list.IsReadOnly);
                Assert.ThrowsExactly<NotSupportedException>(() => list[0] = (2U, 2U));
            }
        }

        [TestMethod]
        public void NullRandomDelegateIsRejectedEvenForAnEmptyProfile()
        {
            var profile = new OrdinaryLootProfile("empty", Array.Empty<OrdinaryLootGroup>());

            Assert.ThrowsExactly<ArgumentNullException>(() => profile.Roll(null));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GroupSnapshotsCallerOwnedCandidateListsAndArrays(bool useArray)
        {
            var original = new OrdinaryLootCandidate(1, 1, 1, 1);
            IReadOnlyList<OrdinaryLootCandidate> candidates = useArray
                ? new[] { original }
                : new List<OrdinaryLootCandidate> { original };
            var group = Group(candidates: candidates);

            ((IList<OrdinaryLootCandidate>)candidates)[0] = new OrdinaryLootCandidate(2, 100, 2, 2);
            if (!useArray)
                ((IList<OrdinaryLootCandidate>)candidates).Clear();

            Assert.AreEqual(1, group.Candidates.Count, "Caller mutation must not change snapshot membership.");
            Assert.AreEqual(original, group.Candidates.Single());
            Assert.AreNotSame(candidates, group.Candidates);
            AssertReadOnly(group.Candidates, original);
            CollectionAssert.AreEqual(new[] { (1U, 1U) },
                Profile(group).Roll((minimum, maximum) => minimum).ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ProfileSnapshotsCallerOwnedGroupListsAndArrays(bool useArray)
        {
            var original = Group();
            IReadOnlyList<OrdinaryLootGroup> groups = useArray
                ? new[] { original }
                : new List<OrdinaryLootGroup> { original };
            var profile = new OrdinaryLootProfile("test", groups);

            ((IList<OrdinaryLootGroup>)groups)[0] = Group(chance: 0);
            if (!useArray)
                ((IList<OrdinaryLootGroup>)groups).Clear();

            Assert.AreEqual(1, profile.Groups.Count, "Caller mutation must not change snapshot membership.");
            Assert.AreEqual(original, profile.Groups.Single());
            Assert.AreNotSame(groups, profile.Groups);
            AssertReadOnly(profile.Groups, original);
            CollectionAssert.AreEqual(new[] { (1U, 1U) },
                profile.Roll((minimum, maximum) => minimum).ToArray());
        }

        [TestMethod]
        public void CandidateRecordCopiesCannotChangeAnExistingGroup()
        {
            var candidate = new OrdinaryLootCandidate(1, 2, 3, 4);
            var group = Group(candidates: new[] { candidate });
            var changed = candidate with { TemplateId = 9, Weight = 10, MinimumQuantity = 11, MaximumQuantity = 12 };
            var (templateId, weight, minimumQuantity, maximumQuantity) = candidate;

            Assert.AreEqual(1U, templateId);
            Assert.AreEqual(2, weight);
            Assert.AreEqual(3U, minimumQuantity);
            Assert.AreEqual(4U, maximumQuantity);
            Assert.AreEqual(candidate, group.Candidates.Single());
            Assert.AreNotEqual(candidate, changed);
        }

        [TestMethod]
        public void ConstructorsExposeTheirExactReadOnlyContract()
        {
            var group = new OrdinaryLootGroup("ammo", LootGroupKind.Ammunition, 3000, 0, 2,
                new[] { new OrdinaryLootCandidate(28, 7, 8, 16) });
            var profile = new OrdinaryLootProfile("test_profile-v1", new[] { group });

            Assert.AreEqual("test_profile-v1", profile.ProfileKey);
            Assert.AreEqual("ammo", group.Key);
            Assert.AreEqual(LootGroupKind.Ammunition, group.Kind);
            Assert.AreEqual(3000, group.ChanceBasisPoints);
            Assert.AreEqual(0, group.MinimumRolls);
            Assert.AreEqual(2, group.MaximumRolls);
            Assert.AreEqual(new OrdinaryLootCandidate(28, 7, 8, 16), group.Candidates.Single());
            AssertReadOnly(profile.Groups, group);
            AssertReadOnly(group.Candidates, group.Candidates.Single());
            Assert.IsTrue(typeof(OrdinaryLootCandidate).IsSealed);
            Assert.IsTrue(typeof(OrdinaryLootGroup).GetProperties().All(property => property.SetMethod == null));
            Assert.IsTrue(typeof(OrdinaryLootProfile).GetProperties().All(property => property.SetMethod == null));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("Uppercase")]
        [DataRow("bad.key")]
        [DataRow("bad/key")]
        [DataRow("bad\\key")]
        [DataRow("bad:key")]
        [DataRow("bad key")]
        [DataRow("bad\tkey")]
        [DataRow("bad\nkey")]
        [DataRow("caf\u00e9")]
        [DataRow("bad\0key")]
        public void MalformedProfileKeysAreRejected(string key)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new OrdinaryLootProfile(key, Array.Empty<OrdinaryLootGroup>()));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("Uppercase")]
        [DataRow("bad.key")]
        [DataRow("bad/key")]
        [DataRow("bad\\key")]
        [DataRow("bad:key")]
        [DataRow("bad key")]
        [DataRow("bad\tkey")]
        [DataRow("bad\nkey")]
        [DataRow("caf\u00e9")]
        [DataRow("bad\0key")]
        public void MalformedGroupKeysAreRejected(string key)
        {
            Assert.ThrowsExactly<ArgumentException>(() => Group(key: key));
        }

        [TestMethod]
        public void KeysAcceptTheirExactLengthLimitsAndLowercaseAsciiTokens()
        {
            var group = Group(key: new string('g', 32));
            var profile = new OrdinaryLootProfile(new string('p', 64), new[] { group });
            var tokenProfile = new OrdinaryLootProfile("abc_012-def", new[] { Group(key: "abc_012-def") });

            Assert.AreEqual(32, group.Key.Length);
            Assert.AreEqual(64, profile.ProfileKey.Length);
            Assert.AreEqual("abc_012-def", tokenProfile.ProfileKey);
            Assert.AreEqual("abc_012-def", tokenProfile.Groups.Single().Key);
        }

        [TestMethod]
        public void ProfileKeysAbove64CharactersAreRejected()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new OrdinaryLootProfile(new string('p', 65), Array.Empty<OrdinaryLootGroup>()));
        }

        [TestMethod]
        public void GroupKeysAbove32CharactersAreRejected()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Group(key: new string('g', 33)));
        }

        [TestMethod]
        public void NullGroupCollectionIsRejectedInsteadOfBecomingAnEmptyProfile()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OrdinaryLootProfile("test", null));
        }

        [TestMethod]
        public void NullGroupEntriesAreRejected()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Profile((OrdinaryLootGroup)null));
        }

        [TestMethod]
        public void NullCandidateCollectionIsRejected()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new OrdinaryLootGroup("group", LootGroupKind.Equipment, 10000, 1, 1, null));
        }

        [TestMethod]
        public void NullCandidateEntriesAreRejected()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Group(candidates: new OrdinaryLootCandidate[] { null }));
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(10000, 1)]
        public void EmptyCandidateGroupsAreRejectedEvenWhenDisabled(int chance, int maximumRolls)
        {
            Assert.ThrowsExactly<ArgumentException>(() => Group(chance: chance, minimumRolls: 0,
                maximumRolls: maximumRolls, candidates: Array.Empty<OrdinaryLootCandidate>()));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        [DataRow(255)]
        public void UndefinedGroupKindsAreRejected(int kind)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Group(kind: (LootGroupKind)kind));
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        [DataRow(10001)]
        [DataRow(int.MaxValue)]
        public void InvalidAdmissionBoundsAreRejected(int chance)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Group(chance: chance));
        }

        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(-1, -1)]
        [DataRow(0, -1)]
        [DataRow(2, 1)]
        [DataRow(1, 0)]
        [DataRow(0, int.MaxValue)]
        public void InvalidRollBoundsAreRejected(int minimum, int maximum)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Group(minimumRolls: minimum, maximumRolls: maximum));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void NonpositiveCandidateWeightsAreRejected(int weight)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                Group(candidates: new[] { new OrdinaryLootCandidate(1, weight, 1, 1) }));
        }

        [TestMethod]
        public void ZeroTemplateIdentifiersAreStructurallyInvalid()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                Group(candidates: new[] { new OrdinaryLootCandidate(0, 1, 1, 1) }));
        }

        [TestMethod]
        [DataRow(0U, 0U)]
        [DataRow(0U, 1U)]
        [DataRow(2U, 1U)]
        [DataRow(1U, 2147483647U)]
        [DataRow(2147483647U, 2147483647U)]
        [DataRow(1U, 2147483648U)]
        [DataRow(1U, uint.MaxValue)]
        [DataRow(uint.MaxValue, uint.MaxValue)]
        public void InvalidOrUnsafeQuantityBoundsAreRejected(uint minimum, uint maximum)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                Group(candidates: new[] { new OrdinaryLootCandidate(1, 1, minimum, maximum) }));
        }

        [TestMethod]
        [DataRow(int.MaxValue, 1)]
        [DataRow(int.MaxValue, int.MaxValue)]
        public void WeightedSumOverflowIsRejectedAtConstruction(int firstWeight, int secondWeight)
        {
            Assert.ThrowsExactly<ArgumentException>(() => Group(candidates: new[]
            {
                new OrdinaryLootCandidate(1, firstWeight, 1, 1),
                new OrdinaryLootCandidate(2, secondWeight, 1, 1)
            }));
        }

        [TestMethod]
        [DataRow(0, 2)]
        [DataRow(1, 2)]
        [DataRow(2, 2)]
        public void JunkMaximumRollsAboveOneAreRejected(int minimum, int maximum)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Group(kind: LootGroupKind.Junk,
                minimumRolls: minimum, maximumRolls: maximum));
        }

        [TestMethod]
        public void JunkCanHaveAFixedZeroRollCount()
        {
            var profile = Profile(Group(kind: LootGroupKind.Junk, minimumRolls: 0, maximumRolls: 0));

            CollectionAssert.AreEqual(Array.Empty<(uint, uint)>(),
                profile.Roll((minimum, maximum) => minimum).ToArray());
        }

        [TestMethod]
        [DataRow(10000, 1)]
        [DataRow(0, 0)]
        public void MultipleJunkGroupsAreRejectedEvenWhenDisabled(int chance, int maximumRolls)
        {
            var first = Group(key: "first", kind: LootGroupKind.Junk, chance: chance,
                minimumRolls: 0, maximumRolls: maximumRolls);
            var second = Group(key: "second", kind: LootGroupKind.Junk, chance: chance,
                minimumRolls: 0, maximumRolls: maximumRolls);

            Assert.ThrowsExactly<ArgumentException>(() => Profile(first, second));
        }

        [TestMethod]
        public void Exactly32SelectionsAreAllowed()
        {
            var profile = Profile(Group(minimumRolls: 32, maximumRolls: 32));

            CollectionAssert.AreEqual(Enumerable.Repeat((1U, 1U), 32).ToArray(),
                profile.Roll((minimum, maximum) => maximum - 1).ToArray());
        }

        [TestMethod]
        public void AggregateMaximumOf32RollsAcrossGroupsIsAllowed()
        {
            var profile = Profile(Group(key: "first", minimumRolls: 16, maximumRolls: 16),
                Group(key: "second", minimumRolls: 16, maximumRolls: 16));

            CollectionAssert.AreEqual(Enumerable.Repeat((1U, 1U), 32).ToArray(),
                profile.Roll((minimum, maximum) => minimum).ToArray());
        }

        [TestMethod]
        [DataRow(10000)]
        [DataRow(0)]
        public void AGroupExceedingThe32RollBudgetIsRejectedEvenWhenDisabled(int chance)
        {
            var group = Group(chance: chance, minimumRolls: 0, maximumRolls: 33);

            Assert.ThrowsExactly<ArgumentException>(() => Profile(group));
        }

        [TestMethod]
        [DataRow(16, 17)]
        [DataRow(int.MaxValue - 1, int.MaxValue - 1)]
        public void ExcessiveAggregateMaximumRollsAreRejectedWithoutIntegerOverflow(int first, int second)
        {
            var groups = new[]
            {
                Group(key: "first", minimumRolls: 0, maximumRolls: first),
                Group(key: "second", minimumRolls: 0, maximumRolls: second)
            };

            Assert.ThrowsExactly<ArgumentException>(() => Profile(groups));
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values, T replacement)
        {
            Assert.IsFalse(values is Array, "The snapshot must not expose its backing array.");
            var list = values as IList<T>;
            Assert.IsNotNull(list);
            Assert.IsTrue(list.IsReadOnly);
            Assert.ThrowsExactly<NotSupportedException>(() => list[0] = replacement);
            Assert.ThrowsExactly<NotSupportedException>(() => list.Add(replacement));
            Assert.ThrowsExactly<NotSupportedException>(() => list.Clear());
        }

        private static OrdinaryLootProfile Profile(params OrdinaryLootGroup[] groups) => new("test", groups);

        private static OrdinaryLootGroup Group(
            string key = "group",
            LootGroupKind kind = LootGroupKind.Consumable,
            int chance = 10000,
            int minimumRolls = 1,
            int maximumRolls = 1,
            IReadOnlyList<OrdinaryLootCandidate> candidates = null) =>
            new(key, kind, chance, minimumRolls, maximumRolls,
                candidates ?? new[] { new OrdinaryLootCandidate(1, 1, 1, 1) });

        private sealed class ScriptedRandom
        {
            private readonly Queue<(int Minimum, int Maximum, int Value)> _draws;

            public ScriptedRandom(params (int Minimum, int Maximum, int Value)[] draws) =>
                _draws = new Queue<(int Minimum, int Maximum, int Value)>(draws);

            public int Next(int minimum, int maximum)
            {
                Assert.IsTrue(_draws.Count > 0, $"Unexpected draw [{minimum}, {maximum}).");
                var draw = _draws.Dequeue();
                Assert.AreEqual(draw.Minimum, minimum, "Inclusive RNG minimum.");
                Assert.AreEqual(draw.Maximum, maximum, "Exclusive RNG maximum.");
                Assert.IsTrue(draw.Value >= minimum && draw.Value < maximum, "Scripted draw is in range.");
                return draw.Value;
            }

            public void AssertConsumed() => Assert.AreEqual(0, _draws.Count, "Unconsumed RNG draws.");
        }
    }
}
