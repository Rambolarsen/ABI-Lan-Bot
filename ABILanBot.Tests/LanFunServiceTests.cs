using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class LanFunServiceTests
{
    private readonly LanFunService _service = new();

    [TestMethod]
    public void DefaultGamesIncludeTheLanLineup()
    {
        var games = _service.GetGameOptions(null);
        Assert.AreEqual(6, games.Count);
        CollectionAssert.Contains(games.ToArray(), "Counter-Strike 2");
        CollectionAssert.Contains(games.ToArray(), "Mario Kart");
    }

    [TestMethod]
    public void CustomGamesAreTrimmedAndDeduplicatedCaseInsensitively()
    {
        var games = _service.GetGameOptions(" CS2, Rocket League ,cs2, Mario Kart ");
        CollectionAssert.AreEqual(
            new[] { "CS2", "Rocket League", "Mario Kart" }, games.ToArray());
    }

    [TestMethod]
    public void CustomGameMustContainTwoUniqueNames()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.GetGameOptions("CS2,cs2"));
    }

    [TestMethod]
    public void CustomGamesRejectMentionsAndUnexpectedCharacters()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.GetGameOptions("@everyone, CS2"));
    }

    [TestMethod]
    public void CustomGamesRejectOverlongListsAndNames()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.GetGameOptions(string.Join(",", Enumerable.Range(1, 21).Select(i => $"Game{i}"))));
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.GetGameOptions($"{new string('a', 51)}, CS2"));
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.GetGameOptions($"{new string('a', 1001)},CS2"));
    }

    [TestMethod]
    public void PickedGameAlwaysBelongsToProvidedOptions()
    {
        for (var i = 0; i < 50; i++)
            CollectionAssert.Contains(new[] { "CS2", "Mario Kart" }, _service.PickGame("CS2,Mario Kart"));
    }

    [DataTestMethod]
    [DataRow("cs2", "CS2")]
    [DataRow("aoe2", "AoE2")]
    [DataRow("rl", "Rocket League")]
    [DataRow("golf", "Golf")]
    [DataRow("overcooked", "Overcooked")]
    [DataRow("mariokart", "Mario Kart")]
    [DataRow(null, "General")]
    public void ChallengeResolvesSupportedGames(string? requested, string expected)
    {
        var (category, text) = _service.PickChallenge(requested);
        Assert.AreEqual(expected, category);
        Assert.IsFalse(string.IsNullOrWhiteSpace(text));
    }

    [TestMethod]
    public void UnknownGameChallengeIsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.PickChallenge("unknown"));
    }

    [TestMethod]
    public void BingoIsUniqueAndHasFreeCenter()
    {
        for (var i = 0; i < 25; i++)
        {
            var card = _service.CreateBingoCard();
            Assert.AreEqual(9, card.Count);
            Assert.AreEqual("FREE SPACE", card[4]);
            Assert.AreEqual(9, card.Distinct(StringComparer.Ordinal).Count());
            Assert.IsTrue(card.All(square => !string.IsNullOrWhiteSpace(square)));
        }
    }
}
