using System.Text.Json;
using System.Text.Json.Serialization;
using NimbusStation.Core.Session;

namespace NimbusStation.Infrastructure.Sessions;

/// <summary>
/// Handles serialization and deserialization of session data to/from JSON files.
/// </summary>
public sealed class SessionSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a session to JSON and writes it to a file.
    /// </summary>
    /// <param name="session">The session to serialize.</param>
    /// <param name="filePath">The path to write the JSON file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteSessionAsync(Session session, string filePath, CancellationToken cancellationToken = default)
    {
        var dto = SessionDto.FromSession(session);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <summary>
    /// Reads a session from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized session.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public async Task<Session> ReadSessionAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var dto = JsonSerializer.Deserialize<SessionDto>(json, JsonOptions)
            ?? throw new JsonException("Failed to deserialize session: result was null");

        return dto.ToSession();
    }

    /// <summary>
    /// DTO for JSON serialization of Session.
    /// </summary>
    private sealed class SessionDto
    {
        public required string TicketId { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required DateTimeOffset LastAccessedAt { get; set; }
        public SessionContextDto? ActiveContext { get; set; }

        public static SessionDto FromSession(Session session) => new()
        {
            TicketId = session.TicketId,
            CreatedAt = session.CreatedAt,
            LastAccessedAt = session.LastAccessedAt,
            ActiveContext = session.ActiveContext is not null
                ? SessionContextDto.FromContext(session.ActiveContext)
                : null
        };

        public Session ToSession() => new(
            TicketId,
            CreatedAt,
            LastAccessedAt,
            ActiveContext?.ToContext());
    }

    /// <summary>
    /// DTO for JSON serialization of SessionContext.
    /// </summary>
    private sealed class SessionContextDto
    {
        public string? ActiveCosmosAlias { get; set; }
        public string? ActiveBlobAlias { get; set; }

        public static SessionContextDto FromContext(SessionContext context) => new()
        {
            ActiveCosmosAlias = context.ActiveCosmosAlias,
            ActiveBlobAlias = context.ActiveBlobAlias
        };

        public SessionContext ToContext() => new(ActiveCosmosAlias, ActiveBlobAlias);
    }
}
