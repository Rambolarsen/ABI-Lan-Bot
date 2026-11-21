using ABILanBot.Services;
using ABILanBot.Modules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class LanPollServiceTests
{
    private readonly LanPollService _polls = new(new LanFunService());

    [TestMethod]
    public void GamePollUsesAllDefaultGames()
    {
        var poll = _polls.CreateGamePoll();
        Assert.AreEqual(LanFunService.DefaultGames.Count, poll.Answers.Count);
        Assert.IsFalse(poll.AllowMultiselect);
        Assert.AreEqual(1u, poll.Duration);
    }

    [TestMethod]
    public void CustomGamePollDeduplicatesAndValidates()
    {
        var poll = _polls.CreateGamePoll(3, "CS2, Rocket League, cs2");
        Assert.AreEqual(2, poll.Answers.Count);
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateGamePoll(25));
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateGamePoll(1, "CS2"));
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateGamePoll(1, "A,B,C,D,E,F,G,H,I,J,K"));
    }

    [TestMethod]
    public void OnlyAwardIsAaretsGamerAndCommandsRequireNoCategory()
    {
        Assert.AreEqual("Årets gamer", AwardModule.AwardName);
        var nominate = typeof(AwardModule).GetMethod(nameof(AwardModule.NominateAsync))!;
        var vote = typeof(AwardModule).GetMethod(nameof(AwardModule.VoteAsync))!;
        CollectionAssert.AreEqual(new[] { "deltaker" },
            nominate.GetParameters().Select(p => p.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "nominerte", "timer" },
            vote.GetParameters().Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void AwardPollNeedsDistinctSafeNominees()
    {
        var poll = _polls.CreateAwardPoll(AwardModule.AwardName, "Alice, Bob, Charlie");
        Assert.AreEqual(3, poll.Answers.Count);
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateAwardPoll("MVP", "Alice, alice"));
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateAwardPoll("MVP", "@everyone, Bob"));
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateAwardPoll("MVP", "Alice"));
        Assert.ThrowsExactly<ArgumentException>(() => _polls.CreateAwardPoll("@everyone", "Alice, Bob"));
    }
}
