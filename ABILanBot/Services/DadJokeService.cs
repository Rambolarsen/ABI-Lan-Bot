using System.Text.Json;
using System.Text.Json.Serialization;

namespace ABILanBot.Services
{
	public class DadJokeService
	{
		private readonly HttpClient _httpClient;
		private const string ApiUrl = "https://icanhazdadjoke.com/";

		public DadJokeService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "ABI-Lan-Bot (https://github.com/Rambolarsen/ABI-Lan-Bot)");
		}

		public async Task<string> GetRandomDadJokeAsync()
		{
			try
			{
				var response = await _httpClient.GetStringAsync(ApiUrl);
				var jokeResponse = JsonSerializer.Deserialize<DadJokeResponse>(response);
				
				return jokeResponse?.Joke ?? "Sorry, couldn't fetch a dad joke right now! 😅";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error fetching dad joke: {ex.Message}");
				return "Sorry, couldn't fetch a dad joke right now! 😅";
			}
		}

		private class DadJokeResponse
		{
			[JsonPropertyName("joke")]
			public string? Joke { get; set; }
		}
	}
}
