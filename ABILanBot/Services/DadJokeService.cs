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
		}

		public async Task<string> GetRandomDadJokeAsync()
		{
			try
			{
				using var requestMessage = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
				requestMessage.Headers.Add("Accept", "application/json");
				requestMessage.Headers.Add("User-Agent", "ABI-Lan-Bot (https://github.com/Rambolarsen/ABI-Lan-Bot)");

				var response = await _httpClient.SendAsync(requestMessage);
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var jokeResponse = JsonSerializer.Deserialize<DadJokeResponse>(content);
				
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
