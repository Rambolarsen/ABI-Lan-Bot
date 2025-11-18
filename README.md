# ABI-Lan-Bot

A Discord bot built with .NET 8.0 and Discord.Net.

## Features

- **Ping Command**: Check if the bot is responsive with `!ping`
- **Help Command**: Display available commands with `!help`
- **Info Command**: Show bot information with `!info`

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A Discord Bot Token (see setup instructions below)

## Getting Your Discord Bot Token

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Go to the "Bot" section in the left sidebar
4. Click "Add Bot" and confirm
5. Under the TOKEN section, click "Copy" to copy your bot token
6. Enable "MESSAGE CONTENT INTENT" under Privileged Gateway Intents
7. Go to OAuth2 > URL Generator
8. Select scopes: `bot`
9. Select bot permissions: `Send Messages`, `Read Messages/View Channels`, `Read Message History`
10. Copy the generated URL and open it in your browser to invite the bot to your server

## Setup

1. Clone this repository:
   ```bash
   git clone https://github.com/Rambolarsen/ABI-Lan-Bot.git
   cd ABI-Lan-Bot
   ```

2. Navigate to the project directory:
   ```bash
   cd ABILanBot
   ```

3. Create an `appsettings.json` file from the example:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

4. Edit `appsettings.json` and add your Discord bot token:
   ```json
   {
     "Discord": {
       "Token": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```

5. Build the project:
   ```bash
   dotnet build
   ```

6. Run the bot:
   ```bash
   dotnet run
   ```

## Usage

Once the bot is running and invited to your server, you can use these commands:

- `!ping` - The bot will respond with "Pong! 🏓"
- `!help` - Displays all available commands
- `!info` - Shows information about the bot

## Project Structure

```
ABILanBot/
├── ABILanBot.csproj          # Project file with dependencies
├── Program.cs                 # Main bot implementation
├── appsettings.example.json  # Configuration template
└── appsettings.json          # Your configuration (not in git)
```

## Dependencies

- **Discord.Net** (3.16.0) - Discord API wrapper for .NET
- **Microsoft.Extensions.Configuration** (8.0.0) - Configuration framework
- **Microsoft.Extensions.Configuration.Json** (8.0.0) - JSON configuration provider

## Development

To modify the bot:

1. Edit `Program.cs` to add new commands or features
2. Build and test your changes:
   ```bash
   dotnet build
   dotnet run
   ```

## Security

**Important**: Never commit your `appsettings.json` file with your actual bot token to version control. The token is like a password to your bot. The `.gitignore` file is configured to exclude this file.

## License

This project is open source and available under the MIT License.
