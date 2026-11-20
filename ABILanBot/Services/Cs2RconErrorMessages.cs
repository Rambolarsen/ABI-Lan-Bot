using System.Net.Sockets;

namespace ABILanBot.Services;

/// <summary>Safe RCON diagnostics without exposing credentials or raw network output.</summary>
public static class Cs2RconErrorMessages
{
    public static string ToUserMessage(Exception error) => error switch
    {
        UnauthorizedAccessException => "❌ **RCON avviste passordet.** Sjekk CS2_RCON_PASSWORD mot rcon_password på Spillvert.",
        OperationCanceledException or TimeoutException => "⏱️ **RCON fikk tidsavbrudd.** Sjekk at CS2-serveren og RCON-porten svarer.",
        SocketException => "🔌 **Kunne ikke koble til CS2-serveren.** Sjekk adresse, TCP-port og brannmur.",
        InvalidOperationException ex when ex.Message.Contains("CS2_RCON_PASSWORD", StringComparison.Ordinal) =>
            "🔑 **RCON-passord mangler i botprosessen.** Start botten med CS2_RCON_PASSWORD satt.",
        InvalidOperationException => "⚙️ **CS2-oppsettet er ufullstendig.** Kontroller Cs2:Host og Cs2:Port.",
        IOException => "📡 **Ugyldig eller avbrutt RCON-svar.** Sjekk RCON-oppsettet og prøv igjen.",
        _ => "❌ **Uventet RCON-feil.** Se konsollen for feiltype og prøv igjen."
    };
}
