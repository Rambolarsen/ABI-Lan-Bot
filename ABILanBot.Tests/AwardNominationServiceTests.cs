using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class AwardNominationServiceTests
{
    [TestMethod]
    public async Task NominationsSurviveRestartAndAreIsolatedByGuild()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new AwardNominationService(path);
            await store.NominateAsync(1, 10, "MVP", "Alice");
            await store.NominateAsync(1, 20, "MVP", "Bob");
            await store.NominateAsync(1, 10, "MVP", "Charlie");
            var reopened = new AwardNominationService(path);
            CollectionAssert.AreEquivalent(new[] { "Bob", "Charlie" },
                (await reopened.GetNomineesAsync(1, "mvp")).ToArray());
            Assert.AreEqual(0, (await reopened.GetNomineesAsync(2, "MVP")).Count);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [TestMethod]
    public async Task UnsafeNomineesAreRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = new AwardNominationService(path);
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => store.NominateAsync(1, 10, "MVP", "@everyone"));
    }
}
