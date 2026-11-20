using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class TeamAllocatorTests
{
    [TestMethod]
    public void SplittingPreservesEveryMemberExactlyOnce()
    {
        var members = Enumerable.Range(1, 23).ToArray();

        var teams = TeamAllocator.Split(members, 4, new Random(1234));
        var allMembers = teams.SelectMany(team => team).ToArray();

        Assert.AreEqual(4, teams.Count);
        Assert.AreEqual(members.Length, allMembers.Length);
        CollectionAssert.AreEquivalent(members, allMembers);
        Assert.IsTrue(teams.Max(team => team.Count) - teams.Min(team => team.Count) <= 1);
    }

    [TestMethod]
    public void SeededRandomProducesRepeatableAllocation()
    {
        var members = Enumerable.Range(1, 15).ToArray();

        var first = TeamAllocator.Split(members, 3, new Random(42));
        var second = TeamAllocator.Split(members, 3, new Random(42));

        for (var i = 0; i < first.Count; i++)
            CollectionAssert.AreEqual(first[i], second[i]);
    }

    [TestMethod]
    public void AllocationDoesNotMutateCallerInput()
    {
        var members = Enumerable.Range(1, 12).ToList();
        var original = members.ToArray();

        _ = TeamAllocator.Split(members, 4, new Random(8));

        CollectionAssert.AreEqual(original, members);
    }

    [TestMethod]
    public void MoreTeamsThanMembersKeepsEmptyTeams()
    {
        var teams = TeamAllocator.Split(new[] { "A", "B" }, 4, new Random(9));

        Assert.AreEqual(4, teams.Count);
        CollectionAssert.AreEquivalent(new[] { "A", "B" }, teams.SelectMany(x => x).ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 1, 0, 0 }, teams.Select(x => x.Count).ToArray());
    }

    [TestMethod]
    public void EmptyLobbyStillReturnsRequestedTeams()
    {
        var teams = TeamAllocator.Split(Array.Empty<int>(), 3, new Random(0));

        Assert.AreEqual(3, teams.Count);
        Assert.IsTrue(teams.All(team => team.Count == 0));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    public void RejectsFewerThanTwoTeams(int teamCount)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => TeamAllocator.Split(new[] { 1, 2 }, teamCount, new Random(0)));
    }

    [TestMethod]
    public void RejectsNullMembers()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => TeamAllocator.Split<int>(null!, 2, new Random(0)));
    }
}
