using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Parses Server-Sent Events (text/event-stream) emitted by Atus AI chat endpoints into typed domain events.
/// </summary>
public static class AtusSseStreamParser
{
    public static async IAsyncEnumerable<AtusStreamEvent> ParseAsync(
        Stream sseStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sseStream);

        using var reader = new StreamReader(sseStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line[5..].Trim();
            if (string.IsNullOrEmpty(payload) || string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseEvent(payload, out var evt) && evt != null)
            {
                yield return evt;
            }
        }
    }

    public static bool TryParseEvent(string jsonPayload, out AtusStreamEvent? streamEvent)
    {
        streamEvent = null;
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload);
            var root = doc.RootElement;
            var eventTypeRaw = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            Guid convId = Guid.Empty;
            if (root.TryGetProperty("conversationId", out var convProp) &&
                convProp.ValueKind == JsonValueKind.String &&
                Guid.TryParse(convProp.GetString(), out var parsedId))
            {
                convId = parsedId;
            }

            bool persisted = root.TryGetProperty("persisted", out var persistedProp) &&
                             persistedProp.ValueKind == JsonValueKind.True;

            switch (eventTypeRaw?.ToLowerInvariant())
            {
                case "init":
                    streamEvent = new AtusStreamEvent
                    {
                        Type = AtusStreamEventType.Init,
                        ConversationId = convId,
                        Persisted = persisted
                    };
                    return true;

                case "chunk":
                    streamEvent = new AtusStreamEvent
                    {
                        Type = AtusStreamEventType.Chunk,
                        Text = root.TryGetProperty("text", out var textProp) ? (textProp.GetString() ?? string.Empty) : string.Empty
                    };
                    return true;

                case "done":
                    streamEvent = new AtusStreamEvent
                    {
                        Type = AtusStreamEventType.Done,
                        ConversationId = convId,
                        Title = root.TryGetProperty("title", out var titleProp) ? (titleProp.GetString() ?? string.Empty) : string.Empty,
                        Persisted = persisted
                    };
                    return true;

                case "error":
                    streamEvent = new AtusStreamEvent
                    {
                        Type = AtusStreamEventType.Error,
                        Message = root.TryGetProperty("message", out var msgProp) ? (msgProp.GetString() ?? "Unknown stream error.") : "Unknown stream error."
                    };
                    return true;

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
