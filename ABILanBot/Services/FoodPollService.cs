using Discord;

namespace ABILanBot.Services;

/// <summary>Creates native Discord food polls; Discord stores votes and closes polls.</summary>
public sealed class FoodPollService
{
    private static readonly string[] DefaultOptions = ["Pizza", "Burger", "Kebab", "Sushi"];

    public PollProperties CreatePoll(string? options = null, string meal = "dinner", int hours = 2)
    {
        if (hours is < 1 or > 24)
            throw new ArgumentException("Voting duration must be between 1 and 24 hours.");

        var question = meal?.ToLowerInvariant() switch
        {
            "lunch" => "🥪 What should we eat for lunch?",
            "dinner" => "🍕 What should we eat for dinner?",
            "snacks" => "🍿 Which snacks should we get?",
            _ => throw new ArgumentException("Choose lunch, dinner, or snacks.")
        };

        if (options?.Length > 1000)
            throw new ArgumentException("The food list is too long (maximum 1000 characters).");

        var choices = string.IsNullOrWhiteSpace(options)
            ? DefaultOptions
            : options.Split(',', StringSplitOptions.TrimEntries);

        if (choices.Length is < 2 or > 10)
            throw new ArgumentException("Provide 2 to 10 food choices, separated by commas.");

        if (choices.Any(choice =>
                string.IsNullOrWhiteSpace(choice) ||
                choice.Length > 55 ||
                choice.Any(c => char.IsControl(c) || c is '@' or '<' or '>' or '`')))
        {
            throw new ArgumentException(
                "Each choice must be 1 to 55 characters, without empty entries, control characters, or mentions.");
        }

        if (choices.Distinct(StringComparer.OrdinalIgnoreCase).Count() != choices.Length)
            throw new ArgumentException("Food choices must be unique (ignoring capitalization).");

        return new PollProperties
        {
            Question = new PollMediaProperties { Text = question },
            Answers = choices.Select(choice => new PollMediaProperties { Text = choice }).ToList(),
            Duration = (uint)hours,
            AllowMultiselect = false,
            LayoutType = PollLayout.Default
        };
    }
}
