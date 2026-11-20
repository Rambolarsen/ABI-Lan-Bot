using Discord;

namespace ABILanBot.Services;

/// <summary>
/// Faste middagsalternativer med publiserte leveringsmenyer for Sandvika.
/// Tilgjengelighet på konkret adresse må kontrolleres ved bestilling.
/// </summary>
public sealed class FoodPollService
{
    public sealed record Restaurant(string Name, string OrderingUrl);

    public static IReadOnlyList<Restaurant> Restaurants { get; } = Array.AsReadOnly<Restaurant>(
    [
        new("Domino's Pizza Sandvika", "https://wolt.com/nb/nor/sandvika/restaurant/dominos-pizza-sandvika"),
        new("Sandvika Pizza og Kebab", "https://wolt.com/nb/nor/sandvika/restaurant/sandvika-pizza-kebab"),
        new("EL GRECO Sandvika", "https://wolt.com/nb/nor/sandvika/restaurant/el-greco-sandvika"),
        new("WU Restaurant Sandvika", "https://wolt.com/nb/nor/sandvika/restaurant/wu-restaurant-sandvika"),
        new("Zouq Sandvika", "https://wolt.com/nb/nor/sandvika/restaurant/zouq-sandvika"),
        new("El Camino Sandvika", "https://wolt.com/nb/nor/sandvika/restaurant/el-camino-sandvika"),
        new("Steam & Stir", "https://wolt.com/nb/nor/asker-og-sandvika/restaurant/steam-stir")
    ]);

    public PollProperties CreatePoll(int timer = 2)
    {
        if (timer is < 1 or > 24)
            throw new ArgumentException("Avstemningen må vare mellom 1 og 24 timer.");

        return new PollProperties
        {
            Question = new PollMediaProperties { Text = "🍽️ Hvor skal vi bestille middag fra?" },
            Answers = Restaurants
                .Select(restaurant => new PollMediaProperties { Text = restaurant.Name })
                .ToList(),
            Duration = (uint)timer,
            AllowMultiselect = false,
            LayoutType = PollLayout.Default
        };
    }

    public string GetOrderingLinks() =>
        string.Join("\n", Restaurants.Select(restaurant =>
            $"• {restaurant.Name}: <{restaurant.OrderingUrl}>"));
}
