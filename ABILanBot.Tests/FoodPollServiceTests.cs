using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class FoodPollServiceTests
{
    private readonly FoodPollService _service = new();

    [TestMethod]
    public void StandardPollIsAboutDinnerWithOneVotePerPerson()
    {
        var poll = _service.CreatePoll();

        Assert.AreEqual("🍽️ Hvor skal vi bestille middag fra?", poll.Question.Text);
        Assert.AreEqual((uint)2, poll.Duration);
        Assert.IsFalse(poll.AllowMultiselect);
        CollectionAssert.AreEqual(
            FoodPollService.Restaurants.Select(restaurant => restaurant.Name).ToArray(),
            poll.Answers.Select(answer => answer.Text).ToArray());
    }

    [TestMethod]
    public void RestaurantsAreUniqueAndHaveSandvikaDeliveryMenuLinks()
    {
        var restaurants = FoodPollService.Restaurants;
        Assert.IsTrue(restaurants.Count is >= 2 and <= 10);
        Assert.AreEqual(restaurants.Count,
            restaurants.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var restaurant in restaurants)
        {
            Assert.IsTrue(restaurant.Name.Length is >= 1 and <= 55);
            Assert.IsTrue(restaurant.OrderingUrl.StartsWith(
                "https://wolt.com/nb/nor/", StringComparison.Ordinal));
            Assert.IsFalse(restaurant.OrderingUrl.Contains('>'));
            Assert.IsTrue(_service.GetOrderingLinks().Contains(
                $"{restaurant.Name}: <{restaurant.OrderingUrl}>", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void PollDurationCanBeCustomized()
    {
        Assert.AreEqual((uint)6, _service.CreatePoll(6).Duration);
        Assert.AreEqual((uint)24, _service.CreatePoll(24).Duration);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(25)]
    public void InvalidDurationsAreRejected(int timer)
    {
        Assert.ThrowsExactly<ArgumentException>(() => _service.CreatePoll(timer));
    }
}
