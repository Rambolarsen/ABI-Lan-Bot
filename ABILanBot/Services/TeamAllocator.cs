namespace ABILanBot.Services;

/// <summary>
/// Pure team allocation: randomize a copy of the supplied members and assign
/// them round-robin to the requested number of teams. No Discord objects or
/// network access are required, and the caller can supply a seeded Random
/// instance for reproducible tests.
/// </summary>
public static class TeamAllocator
{
    public static List<List<T>> Split<T>(
        IReadOnlyList<T> members,
        int teamCount,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (teamCount < 2)
            throw new ArgumentOutOfRangeException(nameof(teamCount), "At least two teams are required.");

        random ??= Random.Shared;

        // Fisher-Yates on a copy: don't reorder the caller's members.
        var shuffled = members.ToList();
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        var teams = new List<List<T>>(teamCount);
        for (var i = 0; i < teamCount; i++)
            teams.Add([]);

        for (var i = 0; i < shuffled.Count; i++)
            teams[i % teamCount].Add(shuffled[i]);

        return teams;
    }
}
