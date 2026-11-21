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
    public async Task NomineeNamesMustBeCompatibleWithDiscordAwardPolls()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new AwardNominationService(path);
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => store.NominateAsync(1, 10, "MVP", "Alice, Bob"));
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => store.NominateAsync(1, 10, "MVP", "Alice`Bob"));
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => store.NominateAsync(1, 10, "M`VP", "Alice"));
            Assert.AreEqual(0, (await store.GetNomineesAsync(1, "MVP")).Count);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [TestMethod]
    public async Task FailedSaveDoesNotChangeMemoryOrLeakIntoLaterSave()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lan-awards-" + Guid.NewGuid().ToString("N"));
        var parked = directory + "-parked";
        var path = Path.Combine(directory, "nominations.json");
        Directory.CreateDirectory(directory);
        try
        {
            var store = new AwardNominationService(path);
            await store.NominateAsync(1, 10, "MVP", "Alice");

            // Simulate an unavailable data directory in a cross-platform way:
            // replace its path with a regular file before the next write.
            Directory.Move(directory, parked);
            File.WriteAllText(directory, "blocked");
            await Assert.ThrowsExactlyAsync<IOException>(
                () => store.NominateAsync(1, 10, "MVP", "Bob"));
            CollectionAssert.AreEquivalent(new[] { "Alice" },
                (await store.GetNomineesAsync(1, "MVP")).ToArray());

            File.Delete(directory);
            Directory.Move(parked, directory);
            await store.NominateAsync(1, 20, "MVP", "Charlie");

            var reopened = new AwardNominationService(path);
            CollectionAssert.AreEquivalent(new[] { "Alice", "Charlie" },
                (await reopened.GetNomineesAsync(1, "MVP")).ToArray());
        }
        finally
        {
            if (File.Exists(directory)) File.Delete(directory);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            if (Directory.Exists(parked)) Directory.Delete(parked, recursive: true);
        }
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
