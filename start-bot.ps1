[CmdletBinding()]
param(
    [switch]$NoRestart,
    [switch]$SkipRconPrompt
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$app = Join-Path $root 'ABILanBot'
$configPath = Join-Path $app 'appsettings.json'
$projectPath = Join-Path $app 'ABILanBot.csproj'

function Stop-WithError([string]$message) {
    Write-Host "[ABI LAN Bot] $message" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    Stop-WithError "Missing ABILanBot\appsettings.json. Copy appsettings.example.json and add your bot token and guild ID."
}

try {
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
} catch {
    Stop-WithError 'appsettings.json is not valid JSON.'
}

$token = [string]$config.Discord.Token
$guildId = [string]$config.Discord.GuildId
$parsedGuildId = [ulong]0
if ([string]::IsNullOrWhiteSpace($token) -or
    $token -in @('YOUR_BOT_TOKEN_HERE', 'PASTE_YOUR_TOKEN_HERE')) {
    Stop-WithError 'Set Discord.Token to your actual Discord bot token.'
}
if (-not [ulong]::TryParse($guildId, [ref]$parsedGuildId) -or $parsedGuildId -eq 0) {
    Stop-WithError 'Set Discord.GuildId to a valid Discord server ID.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Stop-WithError 'dotnet was not found. Install .NET 10 SDK and reopen your terminal.'
}
try {
    $sdks = @(& dotnet --list-sdks)
} catch {
    Stop-WithError 'Unable to inspect installed .NET SDKs.'
}
if ($LASTEXITCODE -ne 0 -or @($sdks | Where-Object { $_ -match '^10\.' }).Count -eq 0) {
    Stop-WithError 'Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
}

# Do not persist passwords. RCON is optional; other bot features work without it.
if (-not $SkipRconPrompt -and
    -not [string]::IsNullOrWhiteSpace([string]$config.Cs2.Host) -and
    [string]::IsNullOrWhiteSpace($env:CS2_RCON_PASSWORD)) {
    Write-Host 'CS2 RCON is configured; press Enter to skip CS2 controls.' -ForegroundColor Yellow
    $secure = Read-Host 'CS2 RCON password (hidden)' -AsSecureString
    if ($secure.Length -gt 0) {
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try {
            $env:CS2_RCON_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
    Remove-Variable secure -ErrorAction SilentlyContinue
}

Write-Host 'Building ABI LAN Bot (Release)...' -ForegroundColor Cyan
& dotnet build $projectPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    Stop-WithError 'Build failed. See the errors above.'
}

Write-Host "Configuration OK. Guild ID: $guildId" -ForegroundColor Green
Write-Host 'Check Discord for the bot to come online. Ctrl+C stops it.' -ForegroundColor Cyan

# Program.cs reads appsettings.json from the process working directory.
Push-Location $app
try {
    $attempt = 0
    while ($true) {
        $attempt++
        Write-Host "[ABI LAN Bot] Starting (attempt $attempt)..." -ForegroundColor Cyan
        & dotnet run --project $projectPath --configuration Release --no-build --no-restore
        $exitCode = $LASTEXITCODE

        if ($NoRestart) {
            Write-Host "[ABI LAN Bot] Exited with code $exitCode (restart disabled)."
            if ($exitCode -ne 0) { exit $exitCode }
            break
        }

        Write-Host "[ABI LAN Bot] Process exited ($exitCode). Restarting in 5s; Ctrl+C cancels." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
} finally {
    Pop-Location
}
