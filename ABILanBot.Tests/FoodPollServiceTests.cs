using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class FoodPollServiceTests
{
    private readonly FoodPollService _service = new();

    [TestMethod]
    public void DefaultsCreateSingleChoiceDinnerPoll()
    {
        var poll = _service.CreatePoll();

        Assert.AreEqual("🍕 What should we eat for dinner?", poll.Question.Text);
        Assert.AreEqual((uint)2, poll.Duration);
        Assert.IsFalse(poll.AllowMultiselect);
        CollectionAssert.AreEqual(
            new[] { "Pizza", "Burger", "Kebab", "Sushi" },
            poll.Answers.Select(answer => answer.Text).ToArray());
    }

    [TestMethod]
    [DataRow("lunch", "🥪 What should we eat for lunch?")]
    [DataRow("dinner", "🍕 What should we eat for dinner?")]
    [DataRow("snacks", "🍿 Which snacks should we get?")]
    public void PollQuestionMatchesMeal(string meal, string expected)
    {
        Assert.AreEqual(expected, _service.CreatePoll(meal: meal).Question.Text);
    }

    [TestMethod]
    public void CustomChoicesAreTrimmedAndDurationIsConfigurable()
    {
        var poll = _service.CreatePoll(" Tacos , Sushi 🍣 , Fish & Chips ", "lunch", 6);

        CollectionAssert.AreEqual(
            new[] { "Tacos", "Sushi 🍣", "Fish & Chips" },
            poll.Answers.Select(answer => answer.Text).ToArray());
        Assert.AreEqual((uint)6, poll.Duration);
    }

    [TestMethod]
    public void TenChoicesAreAccepted()
    {
        var choices = string.Join(",", Enumerable.Range(1, 10).Select(i => $"Food{i}"));

        Assert.AreEqual(10, _service.CreatePoll(choices).Answers.Count);
    }

    [TestMethod]
    [DataRow("Pizza")]
    [DataRow("Pizza,pizza")]
    [DataRow("Pizza,")]
    [DataRow(",Pizza")]
    [DataRow("Pizza, ,Sushi")]
    [DataRow("Pizza,@everyone")]
    [DataRow("Pizza,<@123>")]
    [DataRow("Pizza,Line\nBreak")]
    public void InvalidOptionsAreRejected(string options)
    {
        Assert.ThrowsExactly<ArgumentException>(() => _service.CreatePoll(options));
    }

    [TestMethod]
    public void MoreThanTenChoicesAreRejected()
    {
        var choices = string.Join(",", Enumerable.Range(1, 11).Select(i => $"Food{i}"));
        Assert.ThrowsExactly<ArgumentException>(() => _service.CreatePoll(choices));
    }

    [TestMethod]
    public void ExcessivelyLongChoicesAndListsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.CreatePoll($"{new string('a', 56)},Pizza"));
        Assert.ThrowsExactly<ArgumentException>(
            () => _service.CreatePoll(new string('x', 1001)));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(25)]
    public void OutOfRangeDurationsAreRejected(int hours)
    {
        Assert.ThrowsExactly<ArgumentException>(() => _service.CreatePoll(hours: hours));
    }

    [TestMethod]
    public void UnknownMealIsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _service.CreatePoll(meal: "brunch"));
    }
}
