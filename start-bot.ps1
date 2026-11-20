[CmdletBinding()]
param(
    [switch]$NoRestart,
    [switch]$SkipRconPrompt,
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $PSScriptRoot 'ABILanBot'
$configPath = Join-Path $app 'appsettings.json'
$projectPath = Join-Path $app 'ABILanBot.csproj'

function Stop-WithError([string]$message) {
    Write-Host "[ABI LAN Bot] $message" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    Stop-WithError 'Missing ABILanBot/appsettings.json. Copy appsettings.example.json and add your bot token and guild ID.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Stop-WithError 'dotnet was not found. Install .NET 10 SDK and reopen the terminal.'
}

try {
    $sdks = @(& dotnet --list-sdks)
} catch {
    Stop-WithError 'Unable to inspect installed .NET SDKs.'
}
if ($LASTEXITCODE -ne 0 -or @($sdks | Where-Object { $_ -match '^10\.' }).Count -eq 0) {
    Stop-WithError 'Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
}

Write-Host '[ABI LAN Bot] Building Release...' -ForegroundColor Cyan
& dotnet build $projectPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    Stop-WithError 'Build failed. See build output above.'
}

# The application loads appsettings.json from the current directory.
Push-Location $app
try {
    # Invoke the actual .NET JSON configuration parser so comments/trailing commas
    # accepted by the bot are also accepted by the launcher.
    $validation = @(& dotnet run --project $projectPath --configuration Release --no-build --no-restore -- --validate-config)
    if ($LASTEXITCODE -ne 0) {
        Stop-WithError 'Configuration validation failed. See the preceding message.'
    }

    if ($ValidateOnly) {
        Write-Host '[ABI LAN Bot] SDK, build and configuration validated.' -ForegroundColor Green
        return
    }

    $cs2Configured = $validation -contains 'CS2_RCON_CONFIGURED=true'
    if ($cs2Configured -and -not $SkipRconPrompt -and
        [string]::IsNullOrWhiteSpace($env:CS2_RCON_PASSWORD)) {
        Write-Host 'CS2 RCON detected. Press Enter to skip optional CS2 controls.' -ForegroundColor Yellow
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

    Write-Host '[ABI LAN Bot] Starting. Check Discord; Ctrl+C stops the launcher.' -ForegroundColor Green
    $attempt = 0
    while ($true) {
        $attempt++
        Write-Host "[ABI LAN Bot] Start attempt $attempt" -ForegroundColor Cyan
        & dotnet run --project $projectPath --configuration Release --no-build --no-restore
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) {
            Write-Host '[ABI LAN Bot] Stopped normally. No restart needed.' -ForegroundColor Green
            break
        }
        if ($NoRestart) {
            Stop-WithError "Bot exited with code $exitCode (restarts disabled)."
        }

        Write-Host "[ABI LAN Bot] Crashed with code $exitCode. Retrying in 5 seconds. Ctrl+C cancels." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
} finally {
    Pop-Location
}
